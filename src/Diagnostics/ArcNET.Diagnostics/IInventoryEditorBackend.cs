using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public interface IInventoryEditorBackend : IHandleBackend
{
    InventoryCreateExecutionResult CreateItem(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong ownerHandle,
        ulong prototypeHandle,
        int inventoryLocation,
        TimeSpan timeout
    );

    InventoryDestroyExecutionResult DestroyItem(
        int processId,
        RuntimeProfileSnapshot runtimeProfile,
        ulong itemHandle,
        TimeSpan timeout
    );
}
