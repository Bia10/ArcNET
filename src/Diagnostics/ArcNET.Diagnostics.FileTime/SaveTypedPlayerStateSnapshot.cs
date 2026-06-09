namespace ArcNET.Diagnostics;

public sealed record SaveTypedPlayerStateSnapshot(
    int QuestCount,
    int RumorsCount,
    int Blessings,
    int Curses,
    int Schematics,
    IReadOnlyDictionary<int, int>? Reputation
);
