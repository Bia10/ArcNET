using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics;

public interface IGameDataCatalogBackend
{
    Task<IReadOnlyList<PrototypePaletteEntry>> LoadPrototypePaletteAsync(string workspacePath);

    Task<IReadOnlyList<WorldMapCatalogEntry>> LoadWorldMapCatalogAsync(string workspacePath);

    Task<IReadOnlyList<TileArtCatalogEntry>> LoadTileArtCatalogAsync(string workspacePath);

    Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath);
}
