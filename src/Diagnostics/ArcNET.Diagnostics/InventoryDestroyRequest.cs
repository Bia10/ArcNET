namespace ArcNET.Diagnostics;

public sealed record class InventoryDestroyRequest(
    AttachedSessionSnapshot Session,
    string ItemHandleToken,
    string TimeoutMillisecondsText
);
