namespace ArcNET.Diagnostics;

public sealed record class CeSourceSymbolCoverageSnapshot(
    bool UniqueSymbolMatch,
    int MatchCount,
    IReadOnlyList<string> SampleSites
);
