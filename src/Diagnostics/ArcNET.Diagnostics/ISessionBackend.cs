using ArcNET.Diagnostics.Contracts;
using ArcNET.Patch;

namespace ArcNET.Diagnostics;

public interface ISessionBackend
{
    ISessionConnection Attach(int processId);

    string? TryResolveWorkspacePathHint(ISessionConnection connection, RuntimeProfileSnapshot runtimeProfile);

    bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error);

    ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options);

    ISessionConnection LaunchAndAttach(ArcanumLaunchPlan plan, TimeSpan attachTimeout);
}
