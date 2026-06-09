namespace ArcNET.Diagnostics;

public readonly record struct RumorLogbookEntrySnapshot(
    int RumorId,
    GameDateTimeSnapshot DateTime,
    bool Quelled,
    string? Text,
    string? NormalText,
    string? DumbText
);
