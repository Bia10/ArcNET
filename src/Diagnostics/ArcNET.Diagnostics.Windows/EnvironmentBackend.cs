using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Windows;
using ArcNET.Patch;

namespace ArcNET.Diagnostics.Windows;

public sealed class EnvironmentBackend : IEnvironmentBackend
{
    public IReadOnlyList<RunningProcessInfo> GetRunningProcesses(IReadOnlyList<string> processNames) =>
        ProcessCatalog.GetRunningProcesses(processNames);

    public bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error) =>
        RuntimeProfileMatcher.TryComputeModuleSha256(modulePath, out moduleSha256, out error);

    public ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options) =>
        ArcanumLauncher.CreatePlan(gamePath, options);
}
