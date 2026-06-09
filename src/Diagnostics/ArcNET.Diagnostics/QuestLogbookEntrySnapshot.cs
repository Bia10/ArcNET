namespace ArcNET.Diagnostics;

public readonly record struct QuestLogbookEntrySnapshot(
    int QuestId,
    GameDateTimeSnapshot DateTime,
    int State,
    string StateName,
    string Label,
    string? Description,
    string? NormalDescription,
    string? DumbDescription
);
