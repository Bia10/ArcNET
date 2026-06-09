namespace ArcNET.Diagnostics;

public sealed record SaveTownMapFogFileAnalysisSnapshot(
    int ByteLength,
    int RevealedTiles,
    int TotalTiles,
    double CoveragePercent
);
