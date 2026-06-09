using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class ReadBackend : IReadBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live debugger reads currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live debugger reads currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public NativeReadSnapshot InvokeInt32(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        string functionKey,
        IReadOnlyList<uint> stackArguments,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(functionKey);
        ArgumentNullException.ThrowIfNull(stackArguments);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live debugger reads currently require Windows.");

        using var memory = ProcessMemory.Attach(processId);
        using var dispatcher = RuntimeCallDispatcher.Install(memory, runtimeProfile);
        return NativeInvoker.Invoke(dispatcher, memory, functionKey, stackArguments, timeout).Snapshot;
    }
}
