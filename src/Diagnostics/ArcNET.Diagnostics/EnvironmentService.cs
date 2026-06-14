using ArcNET.Diagnostics.Contracts;
using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public sealed class EnvironmentService(IEnvironmentBackend backend)
{
    public EnvironmentSnapshot Create(EnvironmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedProcessNames = ResolveRequestedProcessNames(request.RequestedProcessNames);
        var runningProcesses = backend.GetRunningProcesses(requestedProcessNames);
        var liveRuntimes = CreateLiveRuntimes(runningProcesses);
        var processCandidates = CreateProcessCandidates(requestedProcessNames, liveRuntimes);
        var attachSummary = CreateAttachSummary(liveRuntimes, requestedProcessNames);
        var launchPreview = CreateLaunchPreview(request);
        var notes = CreateNotes(liveRuntimes, launchPreview);

        return new EnvironmentSnapshot(
            DateTimeOffset.UtcNow,
            requestedProcessNames,
            processCandidates,
            liveRuntimes,
            liveRuntimes.Count == 1,
            attachSummary,
            launchPreview,
            notes
        );
    }

    private IReadOnlyList<LiveRuntimeSnapshot> CreateLiveRuntimes(IReadOnlyList<RunningProcessInfo> runningProcesses) =>
        [.. runningProcesses.Select(CreateLiveRuntime)];

    private LiveRuntimeSnapshot CreateLiveRuntime(RunningProcessInfo process)
    {
        var fingerprint = RuntimeFingerprintReader.Create(
            process.ProcessName,
            process.ProcessId,
            process.ModulePath,
            process.ModuleBase,
            process.ModuleSize
        );
        _ = backend.TryComputeModuleSha256(process.ModulePath, out var moduleSha256, out var hashError);
        var profile = RuntimeProfileMatcher.Match(fingerprint, moduleSha256, hashError);
        var capabilities = DiagnosticsCapabilityPolicy.Create(profile, hasModuleSymbols: false);
        var displayName = $"{process.ProcessName}.exe (PID {process.ProcessId})";
        var summary =
            $"{profile.DisplayName} · {profile.SupportLevel} · {fingerprint.ModuleFileName} @ {fingerprint.ModuleBase}";

        return new LiveRuntimeSnapshot(
            $"live-{NormalizeProcessName(process.ProcessName)}-{process.ProcessId}",
            displayName,
            summary,
            process.ProcessName,
            process.ProcessId,
            fingerprint,
            profile,
            capabilities,
            process.RuntimeWorkspacePathHint
        );
    }

    private static IReadOnlyList<ProcessCandidateSnapshot> CreateProcessCandidates(
        IReadOnlyList<string> requestedProcessNames,
        IReadOnlyList<LiveRuntimeSnapshot> liveRuntimes
    ) =>
        [
            .. requestedProcessNames.Select(processName =>
            {
                var count = liveRuntimes.Count(runtime =>
                    runtime.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)
                );
                return new ProcessCandidateSnapshot(
                    processName,
                    $"{processName}.exe",
                    count > 0,
                    count,
                    count switch
                    {
                        0 => "Not running",
                        1 => "Running",
                        _ => $"Running ({count} instances)",
                    }
                );
            }),
        ];

    private static string CreateAttachSummary(
        IReadOnlyList<LiveRuntimeSnapshot> liveRuntimes,
        IReadOnlyList<string> requestedProcessNames
    ) =>
        liveRuntimes.Count switch
        {
            0 =>
                $"No requested Arcanum runtime is currently running ({string.Join(" / ", requestedProcessNames.Select(static name => $"{name}.exe"))}).",
            1 => $"Ready to attach to {liveRuntimes[0].DisplayName} using the detected runtime profile.",
            _ =>
                $"Multiple requested runtimes are running ({string.Join(", ", liveRuntimes.Select(static runtime => runtime.DisplayName))}).",
        };

    private LaunchPreviewSnapshot? CreateLaunchPreview(EnvironmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InstallPath))
            return null;

        try
        {
            var options = new ArcanumLaunchOptions
            {
                ExecutableKind = request.LaunchExecutableKind,
                Windowed = request.LaunchWindowed,
            };
            var plan = backend.CreateLaunchPlan(request.InstallPath, options);
            return new LaunchPreviewSnapshot(
                true,
                $"Launch plan resolves to {Path.GetFileName(plan.ExecutablePath)}.",
                null,
                plan.ExecutableKind,
                plan.ExecutablePath,
                plan.WorkingDirectory,
                plan.Arguments,
                [.. plan.EnvironmentVariables.Select(static pair => $"{pair.Key}={pair.Value}")]
            );
        }
        catch (Exception ex)
        {
            return new LaunchPreviewSnapshot(
                false,
                "Launch plan is not ready yet.",
                ex.Message,
                null,
                null,
                null,
                [],
                []
            );
        }
    }

    private static IReadOnlyList<string> CreateNotes(
        IReadOnlyList<LiveRuntimeSnapshot> liveRuntimes,
        LaunchPreviewSnapshot? launchPreview
    )
    {
        List<string> notes = [];
        if (liveRuntimes.Count == 0)
            notes.Add("Live attach stays dormant until a supported runtime is actually running on the local machine.");

        foreach (var runtime in liveRuntimes)
        {
            notes.Add(
                $"{runtime.DisplayName}: {runtime.RuntimeProfile.DisplayName} with support level {runtime.RuntimeProfile.SupportLevel}."
            );
        }

        if (launchPreview is { CanLaunch: false, Error: { } error })
            notes.Add($"Launch preview: {error}");
        else if (launchPreview is { CanLaunch: true, ExecutablePath: { } executablePath })
            notes.Add($"Launch preview resolves to {executablePath}.");

        return notes;
    }

    private static IReadOnlyList<string> ResolveRequestedProcessNames(IReadOnlyList<string> requestedProcessNames) =>
        requestedProcessNames.Count > 0
            ? [.. requestedProcessNames.Select(NormalizeProcessName)]
            : s_defaultProcessNames;

    private static string NormalizeProcessName(string processName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName.Trim());
        if (normalized.Length == 0)
            throw new InvalidOperationException("Process name must not be empty.");

        return normalized;
    }

    private static readonly string[] s_defaultProcessNames = ["Arcanum", "arcanum-ce"];
}
