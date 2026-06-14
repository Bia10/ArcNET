namespace ArcNET.Diagnostics;

public sealed class GameDataCatalogService(IGameDataCatalogBackend backend)
{
    public async Task<GameDataCatalogSnapshot> LoadAsync(GameDataCatalogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workspacePath = request.WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return CreateUnavailableSnapshot(
                "Game-data catalog unavailable",
                "The request does not expose a usable local workspace path, so ArcNET cannot load the local install workspace."
            );
        }

        try
        {
            var prototypeEntries = await backend.LoadPrototypePaletteAsync(workspacePath).ConfigureAwait(false);
            var worldMapEntries = await backend.LoadWorldMapCatalogAsync(workspacePath).ConfigureAwait(false);
            var tileArtEntries = await backend.LoadTileArtCatalogAsync(workspacePath).ConfigureAwait(false);
            var staticObjectEntries = await backend.LoadStaticObjectCatalogAsync(workspacePath).ConfigureAwait(false);
            List<string> notes =
            [
                "Prototype entries come from the local ArcNET object palette and can seed lookup, inventory, or spawn requests.",
                "World-map entries come from the local ArcNET world-area catalog.",
                "Tile-art entries come from the local tile palette and can resolve art ids without manual bit-twiddling.",
                "Static object entries come from loaded mob and sector assets and expose placed object ids, GUIDs, prototypes, and source paths.",
            ];
            if (request.Session.HasExited)
                notes.Add("The process has exited, but the local install catalog remains browsable.");

            return new GameDataCatalogSnapshot(
                DateTimeOffset.UtcNow,
                IsAvailable: true,
                "Game-data catalog loaded",
                $"Loaded {prototypeEntries.Count.ToString()} prototype entries, {worldMapEntries.Count.ToString()} world-map locations, {tileArtEntries.Count.ToString()} tile-art ids, and {staticObjectEntries.Count.ToString()} static object entries from the local workspace.",
                prototypeEntries,
                worldMapEntries,
                tileArtEntries,
                staticObjectEntries,
                notes
            );
        }
        catch (Exception ex)
        {
            return CreateUnavailableSnapshot(
                "Game-data catalog unavailable",
                $"Unable to load the local ArcNET workspace catalog ({ex.GetType().Name}: {ex.Message})."
            );
        }
    }

    private static GameDataCatalogSnapshot CreateUnavailableSnapshot(string status, string summary) =>
        new(DateTimeOffset.UtcNow, false, status, summary, [], [], [], [], []);
}
