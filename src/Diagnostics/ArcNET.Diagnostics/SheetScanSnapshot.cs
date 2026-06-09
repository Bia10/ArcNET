namespace ArcNET.Diagnostics;

public sealed record class SheetScanSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string TargetHandleText,
    string TargetText,
    SheetDataSnapshot Data,
    IReadOnlyList<string> Notes
);
