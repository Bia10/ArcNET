namespace ArcNET.Diagnostics;

public sealed record SaveExploredAreaCoverageSnapshot(
    string Area,
    int RevealedTiles,
    int TotalTiles,
    double CoveragePercent
);
