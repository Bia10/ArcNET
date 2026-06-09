namespace ArcNET.Diagnostics;

public readonly record struct KillLogbookSummaryEntrySnapshot(
    string Key,
    string Label,
    int DescriptionId,
    string? Name,
    int Value
);
