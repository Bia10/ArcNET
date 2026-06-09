namespace ArcNET.Diagnostics;

public readonly record struct BlessingCurseLogbookEntrySnapshot(
    string Kind,
    int Id,
    GameDateTimeSnapshot DateTime,
    string Name
);
