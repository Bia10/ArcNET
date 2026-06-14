namespace ArcNET.Diagnostics;

public sealed record class InventoryEditorSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string OwnerHandleText,
    string OwnerTargetText,
    string ItemHandleText,
    string PrototypeHandleText,
    string InventoryLocationText,
    string DispatcherText,
    string ExecutionDetailText,
    string ResultText,
    IReadOnlyList<string> Notes
);
