namespace ArcNET.Diagnostics;

public sealed record SaveGlobalNonZeroSummarySnapshot(
    int Count,
    int TotalInts,
    double Density,
    bool IsDense,
    IReadOnlyList<SaveGlobalIndexedIntSnapshot> Entries,
    IReadOnlyList<SaveGlobalIndexedIntSnapshot> FirstEntries,
    IReadOnlyList<SaveGlobalIndexedIntSnapshot> LastEntries
);
