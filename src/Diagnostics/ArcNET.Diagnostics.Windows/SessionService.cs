using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;
using ArcNET.Patch;

namespace ArcNET.Diagnostics.Windows;

public sealed class SessionService(ISessionBackend backend)
{
    public static SessionService Default { get; } = new(new SessionBackend());

    public SessionHandle Attach(LiveRuntimeSnapshot runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var connection = backend.Attach(runtime.ProcessId);
        try
        {
            var snapshot = CreateSnapshot(connection, SessionOrigin.Attach, launchPreview: null);
            return new SessionHandle(connection, SessionOrigin.Attach, null, snapshot);
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
        try
        {
            var snapshot = CreateSnapshot(connection, SessionOrigin.Launch, launchPreview);
            return new SessionHandle(connection, SessionOrigin.Launch, launchPreview, snapshot);
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

        var snapshot = CreateSnapshot(session.Connection, session.Origin, session.LaunchPreview);
        session.UpdateSnapshot(snapshot);
        return snapshot;
    }

    private AttachedSessionSnapshot CreateSnapshot(
        ISessionConnection connection,
        SessionOrigin origin,
        LaunchPreviewSnapshot? launchPreview
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
        var displayName = $"{connection.ProcessName}.exe (PID {connection.ProcessId})";
        var summary = CreateSummary(profile, connection, origin);
        var detail = $"{connection.ModulePath} @ {fingerprint.ModuleBase}";
        var notes = CreateNotes(connection, profile, capabilities, launchPreview);

        return new AttachedSessionSnapshot(
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
            notes
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
        LaunchPreviewSnapshot? launchPreview
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

        notes.AddRange(capabilities.Warnings);
        return [.. notes.Where(static note => !string.IsNullOrWhiteSpace(note)).Distinct(StringComparer.Ordinal)];
    }
}
