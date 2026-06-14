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

        var files = ArcNET.Editor.SaveGamePayloadComposer.Compose(original, SaveGameUpdates.ToLegacy(updates));
        var index = ArcNET.Editor.SaveGameIndexRebuilder.Rebuild(original.Index, files);

        return LoadedSave.FromLegacy(
            ArcNET.Editor.SaveGameLoader.LoadFromFiles(
                updates?.UpdatedInfo ?? original.Info,
                index,
                files,
                progress,
                cancellationToken
            )
        );
    }
}
