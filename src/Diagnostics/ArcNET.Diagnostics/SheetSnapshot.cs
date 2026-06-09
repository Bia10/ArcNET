namespace ArcNET.Diagnostics;

public sealed record class SheetSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string TargetHandleText,
    string TargetText,
    string SheetLabel,
    SheetRoute Route,
    IReadOnlyList<ReadValueSnapshot> Values,
    IReadOnlyList<string> Notes
);
