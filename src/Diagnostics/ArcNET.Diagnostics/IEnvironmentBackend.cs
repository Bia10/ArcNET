using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public interface IEnvironmentBackend
{
    IReadOnlyList<RunningProcessInfo> GetRunningProcesses(IReadOnlyList<string> processNames);

    bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error);

    ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options);
}
