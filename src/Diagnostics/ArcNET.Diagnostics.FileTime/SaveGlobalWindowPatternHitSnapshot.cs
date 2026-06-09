namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalWindowPatternHitSnapshot(
    int StartInt,
    int RemovedInts,
    int AddedInts,
    int Hits
);
