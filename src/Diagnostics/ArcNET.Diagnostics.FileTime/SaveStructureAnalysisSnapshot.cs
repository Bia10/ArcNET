namespace ArcNET.Diagnostics;

public sealed record SaveStructureAnalysisSnapshot(
    string DisplayName,
    string LeaderName,
    int LeaderLevel,
    int LeaderPortraitId,
    string ModuleName,
    int MapId,
    int LeaderTileX,
    int LeaderTileY,
    SaveGameClockSnapshot GameTime,
    int TotalFileCount,
    IReadOnlyList<SaveEmbeddedFileExtensionSnapshot> Extensions,
    IReadOnlyList<SaveExploredAreaCoverageSnapshot> ExploredAreas,
    IReadOnlyList<SaveMapWorldStateSnapshot> Maps
);
