namespace ArcNET.Diagnostics;

public readonly record struct ReputationLogbookEntrySnapshot(
    int ReputationId,
    GameDateTimeSnapshot DateTime,
    string Name
);
