namespace ArcNET.GameData.Workspace;

/// <summary>
/// One map jump point projected from a loaded <c>map.jmp</c> file.
/// </summary>
public sealed record class WorkspaceJumpPointCatalogEntry(
    string SourceAssetPath,
    string SourceMapName,
    uint Flags,
    long SourcePackedLocation,
    int SourceTileX,
    int SourceTileY,
    int DestinationMapId,
    long DestinationPackedLocation,
    int DestinationTileX,
    int DestinationTileY,
    string SummaryText
);
