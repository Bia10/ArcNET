using ArcNET.Formats;

namespace ArcNET.GameData.SaveGames;

/// <summary>
/// Writes a modified <see cref="LoadedSave"/> back to disk.
/// When any embedded file is updated its bytes are recomputed, the TFAI index is rebuilt
/// to reflect any size changes, and all three save-slot files are written.
/// </summary>
public static class SaveGameWriter
{
    /// <summary>
    /// Writes a save slot to three explicit file paths.
    /// </summary>
    public static void Save(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null
    )
    {
        var (gsiBytes, tfaiBytes, tfafBytes) = Serialize(original, updates);

        AtomicSaveSlotFileWriter.Write(gsiPath, gsiBytes, tfaiPath, tfaiBytes, tfafPath, tfafBytes);
    }

    /// <summary>
    /// Writes a save slot by folder and slot name.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// </summary>
    public static void Save(LoadedSave original, string saveFolder, string slotName, SaveGameUpdates? updates = null)
    {
        var paths = SaveSlotPathResolver.ResolveFromFolder(saveFolder, slotName);
        Save(original, paths.GsiPath, paths.TfaiPath, paths.TfafPath, updates);
    }

    /// <summary>
    /// Asynchronously writes a save slot to three explicit file paths.
    /// Serialization is performed synchronously on a thread-pool thread;
    /// all three file writes are then issued as true async I/O.
    /// </summary>
    public static async Task SaveAsync(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    )
    {
        var (gsiBytes, tfaiBytes, tfafBytes) = await Task.Run(() => Serialize(original, updates), cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        await AtomicSaveSlotFileWriter
            .WriteAsync(gsiPath, gsiBytes, tfaiPath, tfaiBytes, tfafPath, tfafBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a save slot by folder and slot name.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// </summary>
    public static Task SaveAsync(
        LoadedSave original,
        string saveFolder,
        string slotName,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    )
    {
        var paths = SaveSlotPathResolver.ResolveFromFolder(saveFolder, slotName);
        return SaveAsync(original, paths.GsiPath, paths.TfaiPath, paths.TfafPath, updates, cancellationToken);
    }

    private static (byte[] gsi, byte[] tfai, byte[] tfaf) Serialize(LoadedSave original, SaveGameUpdates? updates)
    {
        var files = SaveGamePayloadComposer.Compose(original, updates);
        var index = RebuildIndex(original.Index, files);

        return (
            SaveInfoFormat.WriteToArray(updates?.UpdatedInfo ?? original.Info),
            SaveIndexFormat.WriteToArray(index),
            TfafFormat.Pack(index, files)
        );
    }

    internal static SaveIndex RebuildIndex(SaveIndex original, IReadOnlyDictionary<string, byte[]> files) =>
        SaveGameIndexRebuilder.Rebuild(original, files);
}
