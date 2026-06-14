using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcNET.Diagnostics.Windows;

internal sealed class GameDataCatalogBackend : IGameDataCatalogBackend
{
    public async Task<IReadOnlyList<PrototypePaletteEntry>> LoadPrototypePaletteAsync(string workspacePath) =>
        WorkspaceGameDataCatalogProjector.ToPrototypePaletteEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );

    public async Task<IReadOnlyList<WorldMapCatalogEntry>> LoadWorldMapCatalogAsync(string workspacePath) =>
        WorkspaceGameDataCatalogProjector.ToWorldMapEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );

    public async Task<IReadOnlyList<TileArtCatalogEntry>> LoadTileArtCatalogAsync(string workspacePath) =>
        WorkspaceGameDataCatalogProjector.ToTileArtEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );

    public async Task<IReadOnlyList<StaticObjectCatalogEntry>> LoadStaticObjectCatalogAsync(string workspacePath) =>
        WorkspaceGameDataCatalogProjector.ToStaticObjectEntries(
            await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(workspacePath).ConfigureAwait(false)
        );
}
