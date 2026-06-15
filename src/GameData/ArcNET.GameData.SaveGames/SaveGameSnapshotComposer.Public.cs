namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Shared in-memory save recomposition surface that applies update payloads and
/// materializes a fresh loaded-save snapshot without touching disk.
/// </summary>
public static class SaveGameSnapshotComposer
{
    public static LoadedSave Compose(
        LoadedSave original,
        SaveGameUpdates? updates = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(original);

        var files = SaveGamePayloadComposer.Compose(original, updates);
        var index = SaveGameIndexRebuilder.Rebuild(original.Index, files);

        return SaveGameLoader.LoadFromFiles(
            updates?.UpdatedInfo ?? original.Info,
            index,
            files,
            progress,
            cancellationToken
        );
    }
}
