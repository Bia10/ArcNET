using ArcNET.GameObjects.Metadata;

namespace ArcNET.Diagnostics;

public static class RuntimeSemanticCatalog
{
    public static string StatName(int stat) => CharacterSheetMetadata.StatName(stat);

    public static string AttachmentPointName(int attachmentPoint) =>
        GameObjectRuntimeMetadata.AttachmentPointName(attachmentPoint);

    public static string InventoryLocationName(int inventoryLocation) =>
        GameObjectRuntimeMetadata.InventoryLocationName(inventoryLocation);

    public static string InventoryLocationContext(int inventoryLocation) =>
        GameObjectRuntimeMetadata.InventoryLocationContext(inventoryLocation);

    public static string FormatHandle(ulong handle) => handle == 0 ? "null" : $"0x{handle:X16}";

    public static bool LooksLikeObjectHandle(ulong handle) =>
        (handle & RuntimeOffsets.ObjHandleMarkerMask) == RuntimeOffsets.ObjHandleMarkerValue;
}
