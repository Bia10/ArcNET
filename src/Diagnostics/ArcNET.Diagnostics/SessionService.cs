using ArcNET.Diagnostics.Contracts;
using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public sealed class SessionService(ISessionBackend backend)
{
    public SessionHandle Attach(LiveRuntimeSnapshot runtime, string? workspacePathHint = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var connection = backend.Attach(runtime.ProcessId);
        var normalizedWorkspacePathHint = NormalizeWorkspacePathHint(workspacePathHint);
        try
        {
            var (snapshot, runtimeWorkspacePathHint) = CreateSnapshot(
                connection,
                SessionOrigin.Attach,
                launchPreview: null,
                normalizedWorkspacePathHint,
                runtime.RuntimeWorkspacePathHint
            );
            return new SessionHandle(
                connection,
                SessionOrigin.Attach,
                null,
                snapshot,
                normalizedWorkspacePathHint,
                runtimeWorkspacePathHint
            );
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public SessionHandle LaunchAndAttach(LaunchSessionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new ArcanumLaunchOptions
        {
            ExecutableKind = request.ExecutableKind,
            Windowed = request.LaunchWindowed,
        };
        var launchPlan = backend.CreateLaunchPlan(request.InstallPath, options);
        var launchPreview = new LaunchPreviewSnapshot(
            true,
            $"Launched {Path.GetFileName(launchPlan.ExecutablePath)} and prepared an attached session.",
            null,
            launchPlan.ExecutableKind,
            launchPlan.ExecutablePath,
            launchPlan.WorkingDirectory,
            launchPlan.Arguments,
            [.. launchPlan.EnvironmentVariables.Select(static pair => $"{pair.Key}={pair.Value}")]
        );
        var connection = backend.LaunchAndAttach(launchPlan, request.AttachTimeout ?? TimeSpan.FromSeconds(10));
        var normalizedWorkspacePathHint = NormalizeWorkspacePathHint(request.WorkspacePathHint);
        try
        {
            var (snapshot, runtimeWorkspacePathHint) = CreateSnapshot(
                connection,
                SessionOrigin.Launch,
                launchPreview,
                normalizedWorkspacePathHint,
                fallbackRuntimeWorkspacePathHint: null
            );
            return new SessionHandle(
                connection,
                SessionOrigin.Launch,
                launchPreview,
                snapshot,
                normalizedWorkspacePathHint,
                runtimeWorkspacePathHint
            );
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public AttachedSessionSnapshot Refresh(SessionHandle session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var (snapshot, runtimeWorkspacePathHint) = CreateSnapshot(
            session.Connection,
            session.Origin,
            session.LaunchPreview,
            session.WorkspacePathHint,
            session.RuntimeWorkspacePathHint
        );
        session.UpdateRuntimeWorkspacePathHint(runtimeWorkspacePathHint);
        session.UpdateSnapshot(snapshot);
        return snapshot;
    }

    public AttachedSessionSnapshot SetWorkspacePathHint(SessionHandle session, string? workspacePathHint)
    {
        ArgumentNullException.ThrowIfNull(session);

        var normalizedWorkspacePathHint = NormalizeWorkspacePathHint(workspacePathHint);
        if (string.Equals(session.WorkspacePathHint, normalizedWorkspacePathHint, StringComparison.OrdinalIgnoreCase))
            return session.Snapshot;

        session.UpdateWorkspacePathHint(normalizedWorkspacePathHint);
        var (snapshot, runtimeWorkspacePathHint) = CreateSnapshot(
            session.Connection,
            session.Origin,
            session.LaunchPreview,
            normalizedWorkspacePathHint,
            session.RuntimeWorkspacePathHint
        );
        session.UpdateRuntimeWorkspacePathHint(runtimeWorkspacePathHint);
        session.UpdateSnapshot(snapshot);
        return snapshot;
    }

    private (AttachedSessionSnapshot Snapshot, string? RuntimeWorkspacePathHint) CreateSnapshot(
        ISessionConnection connection,
        SessionOrigin origin,
        LaunchPreviewSnapshot? launchPreview,
        string? workspacePathHint,
        string? fallbackRuntimeWorkspacePathHint
    )
    {
        var fingerprint = RuntimeFingerprintReader.Create(
            connection.ProcessName,
            connection.ProcessId,
            connection.ModulePath,
            connection.ModuleBase,
            connection.ModuleSize
        );
        _ = backend.TryComputeModuleSha256(connection.ModulePath, out var moduleSha256, out var hashError);
        var profile = RuntimeProfileMatcher.Match(fingerprint, moduleSha256, hashError);
        var capabilities = DiagnosticsCapabilityPolicy.Create(profile, hasModuleSymbols: false);
        var runtimeWorkspacePathHint =
            NormalizeWorkspacePathHint(backend.TryResolveWorkspacePathHint(connection, profile))
            ?? NormalizeWorkspacePathHint(fallbackRuntimeWorkspacePathHint);
        var displayName = $"{connection.ProcessName}.exe (PID {connection.ProcessId})";
        var summary = CreateSummary(profile, connection, origin);
        var detail = $"{connection.ModulePath} @ {fingerprint.ModuleBase}";
        var notes = CreateNotes(
            connection,
            profile,
            capabilities,
            launchPreview,
            workspacePathHint,
            runtimeWorkspacePathHint
        );

        return (
            new AttachedSessionSnapshot(
                DateTimeOffset.UtcNow,
                origin,
                displayName,
                summary,
                detail,
                connection.ProcessName,
                connection.ProcessId,
                connection.HasExited,
                fingerprint,
                profile,
                capabilities,
                launchPreview,
                notes,
                workspacePathHint,
                runtimeWorkspacePathHint
            ),
            runtimeWorkspacePathHint
        );
    }

    private static string CreateSummary(
        RuntimeProfileSnapshot profile,
        ISessionConnection connection,
        SessionOrigin origin
    )
    {
        var originText = origin == SessionOrigin.Launch ? "Launch-attached" : "Attached";
        var statusText = connection.HasExited ? "exited" : "live";
        return $"{originText} {statusText} session · {profile.DisplayName} · {profile.SupportLevel}.";
    }

    private static IReadOnlyList<string> CreateNotes(
        ISessionConnection connection,
        RuntimeProfileSnapshot profile,
        RuntimeCapabilityReport capabilities,
        LaunchPreviewSnapshot? launchPreview,
        string? workspacePathHint,
        string? runtimeWorkspacePathHint
    )
    {
        List<string> notes =
        [
            profile.Notes,
            $"{connection.ProcessName}.exe PID {connection.ProcessId} module path: {connection.ModulePath}",
        ];

        if (connection.HasExited)
            notes.Add(
                "The attached process has exited. The session stays visible for post-mortem context but should be reattached."
            );

        if (launchPreview is { ExecutablePath: { } executablePath })
            notes.Add($"Launch origin: {executablePath}");

        if (!string.IsNullOrWhiteSpace(workspacePathHint))
            notes.Add($"Workspace hint: {workspacePathHint.Trim()}");

        if (!string.IsNullOrWhiteSpace(runtimeWorkspacePathHint))
            notes.Add($"Runtime workspace hint: {runtimeWorkspacePathHint.Trim()}");

        notes.AddRange(capabilities.Warnings);
        return [.. notes.Where(static note => !string.IsNullOrWhiteSpace(note)).Distinct(StringComparer.Ordinal)];
    }

    private static string? NormalizeWorkspacePathHint(string? workspacePathHint)
    {
        if (string.IsNullOrWhiteSpace(workspacePathHint))
            return null;

        return workspacePathHint.Trim();
    }
}
