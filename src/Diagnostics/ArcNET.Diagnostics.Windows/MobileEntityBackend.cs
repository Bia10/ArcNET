using System.Runtime.Versioning;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class MobileEntityBackend : IMobileEntityBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public IReadOnlyList<LiveObjectIdentity> ListLiveMobiles(int processId, int maxEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveMobileRosterScanner.Scan(memory, maxEntries);
    }

    public MobileMutationExecutionResult SetMobileStat(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        int statId,
        int value,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SetMobileStat(memory, runtimeProfile, handle, statId, value, timeout);
    }

    public MobileMutationExecutionResult KillMobile(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.KillMobile(memory, runtimeProfile, handle, timeout);
    }

    public MobileMutationExecutionResult DespawnMobile(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong handle,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.DespawnMobile(memory, runtimeProfile, handle, timeout);
    }

    public MobileMutationExecutionResult SpawnMobile(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong prototypeHandle,
        ulong anchorHandle,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live mobile editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.SpawnMobile(memory, runtimeProfile, prototypeHandle, anchorHandle, timeout);
    }
}
