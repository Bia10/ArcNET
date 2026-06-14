namespace ArcNET.GameData.Workspace;

/// <summary>
/// Cached local game-data catalog built from a resolved Arcanum install.
/// </summary>
public sealed record class WorkspaceGameDataCatalog(
    IReadOnlyList<WorkspacePrototypeCatalogEntry> PrototypeEntries,
    WorkspaceWorldAreaCatalog WorldAreaCatalog,
    IReadOnlyList<WorkspaceTileArtCatalogEntry> TileArtEntries,
    IReadOnlyList<WorkspaceStaticObjectCatalogEntry> StaticObjectEntries
);
