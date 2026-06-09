namespace ArcNET.Diagnostics;

public sealed record SaveTownMapFogSnapshot(
    int FileCount,
    int RevealedTiles,
    IReadOnlyDictionary<string, SaveTownMapFogFileSnapshot> Files
);
