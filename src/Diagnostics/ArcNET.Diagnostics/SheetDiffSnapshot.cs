namespace ArcNET.Diagnostics;

public sealed record class SheetDiffSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string TargetHandleText,
    string TargetText,
    int DelayMilliseconds,
    bool Changed,
    IReadOnlyList<SheetChangeSnapshot> Changes,
    SheetDataSnapshot Before,
    SheetDataSnapshot After,
    IReadOnlyList<string> Notes
);
