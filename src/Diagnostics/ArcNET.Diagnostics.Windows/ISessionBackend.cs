using ArcNET.Patch;

namespace ArcNET.Diagnostics.Windows;

public interface ISessionBackend
{
    ISessionConnection Attach(int processId);

    bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error);

    ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options);

    ISessionConnection LaunchAndAttach(ArcanumLaunchPlan plan, TimeSpan attachTimeout);
}
