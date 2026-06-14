using System.Runtime.Versioning;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics.Windows;

[SupportedOSPlatform("windows")]
public sealed class InventoryEditorBackend : IInventoryEditorBackend
{
    public LivePlayerLocatorResult LocatePlayers(int processId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live inventory editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LivePlayerLocator.Locate(memory);
    }

    public LiveObjectIdentity InspectHandle(int processId, ulong handle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live inventory editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return LiveObjectInspector.Inspect(memory, handle);
    }

    public InventoryCreateExecutionResult CreateItem(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong ownerHandle,
        ulong prototypeHandle,
        int inventoryLocation,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live inventory editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.CreateInventoryItem(
            memory,
            runtimeProfile,
            ownerHandle,
            prototypeHandle,
            inventoryLocation,
            timeout
        );
    }

    public InventoryDestroyExecutionResult DestroyItem(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong itemHandle,
        TimeSpan timeout
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);
        ArgumentNullException.ThrowIfNull(runtimeProfile);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Live inventory editing currently requires Windows.");

        using var memory = ProcessMemory.Attach(processId);
        return RuntimeActionInvoker.DestroyInventoryItem(memory, runtimeProfile, itemHandle, timeout);
    }
}
