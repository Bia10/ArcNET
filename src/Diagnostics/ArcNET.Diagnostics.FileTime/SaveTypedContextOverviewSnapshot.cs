namespace ArcNET.Diagnostics;

public sealed record SaveTypedContextOverviewSnapshot(
    bool HasPlayer,
    int QuestCount,
    int RumorsCount,
    int Blessings,
    int Curses,
    int Schematics,
    int? ReputationCount,
    int TownMapFogFileCount,
    int RevealedTiles
);
