namespace ArcNET.Diagnostics;

public sealed record class InventoryCreateRequest(
    AttachedSessionSnapshot Session,
    string OwnerToken,
    ulong PrototypeHandle,
    string InventoryLocationText,
    string TimeoutMillisecondsText
);
