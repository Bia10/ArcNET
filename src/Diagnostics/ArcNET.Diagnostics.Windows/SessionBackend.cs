using System.ComponentModel;
using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.Diagnostics.Windows;
using ArcNET.Patch;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class SessionBackend : ISessionBackend
{
    public ISessionConnection Attach(int processId)
    {
        EnsureWindows();
        return new SessionConnection(ProcessMemory.Attach(processId));
    }

    public string? TryResolveWorkspacePathHint(ISessionConnection connection, RuntimeProfileSnapshot runtimeProfile)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        EnsureWindows();
        return RuntimeWorkspacePathHintResolver.TryResolveForAttachedProcess(
            connection.ProcessId,
            connection.ModulePath,
            runtimeProfile
        );
    }

    public bool TryComputeModuleSha256(string modulePath, out string? moduleSha256, out string? error) =>
        RuntimeProfileMatcher.TryComputeModuleSha256(modulePath, out moduleSha256, out error);

    public ArcanumLaunchPlan CreateLaunchPlan(string gamePath, ArcanumLaunchOptions options) =>
        ArcanumLauncher.CreatePlan(gamePath, options);

    public ISessionConnection LaunchAndAttach(ArcanumLaunchPlan plan, TimeSpan attachTimeout)
    {
        EnsureWindows();
        ArgumentNullException.ThrowIfNull(plan);

        using var launchedProcess = ArcanumLauncher.Launch(plan);
        var deadline = DateTimeOffset.UtcNow + attachTimeout;
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow <= deadline)
        {
            try
            {
                return new SessionConnection(ProcessMemory.Attach(launchedProcess.Id));
            }
            catch (Exception ex) when (IsAttachRetryable(ex))
            {
                lastError = ex;
                if (launchedProcess.HasExited)
                    break;

                Thread.Sleep(100);
            }
        }

        throw new InvalidOperationException(
            $"Unable to attach to launched process {launchedProcess.Id} within {attachTimeout.TotalSeconds:0.#} second(s).",
            lastError
        );
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live attach currently requires Windows.");
    }

    private static bool IsAttachRetryable(Exception exception) =>
        exception is InvalidOperationException or Win32Exception;

    private sealed class SessionConnection(ProcessMemory memory) : ISessionConnection
    {
        private readonly ProcessMemory _memory = memory;

        public int ProcessId => _memory.ProcessId;

        public string ProcessName => _memory.ProcessName;

        public string ModulePath => _memory.ModulePath;

        public nint ModuleBase => _memory.ModuleBase;

        public int ModuleSize => _memory.ModuleSize;

        public bool HasExited => _memory.HasExited;

        public void Dispose() => _memory.Dispose();
    }
}
