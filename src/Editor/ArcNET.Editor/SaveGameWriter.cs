using ArcNET.Formats;

namespace ArcNET.Editor;

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
    /// <param name="original">The save game loaded by <see cref="SaveGameLoader"/>.</param>
    /// <param name="gsiPath">Destination path for the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Destination path for the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Destination path for the <c>.tfaf</c> data blob.</param>
    /// <param name="updates">
    /// Optional bundle of per-type replacements. Pass <see langword="null"/> (or omit) to
    /// write the save back unmodified; populate only the properties that changed.
    /// </param>
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
    /// <param name="original">The save game loaded by <see cref="SaveGameLoader"/>.</param>
    /// <param name="saveFolder">Directory containing the three save slot files.</param>
    /// <param name="slotName">Base file name without extension (e.g. <c>"slot1"</c>).</param>
    /// <param name="updates">
    /// Optional bundle of per-type replacements. Pass <see langword="null"/> (or omit) to
    /// write the save back unmodified.
    /// </param>
    public static void Save(LoadedSave original, string saveFolder, string slotName, SaveGameUpdates? updates = null)
    {
        var paths = SaveSlotPathResolver.ResolveFromFolder(saveFolder, slotName);
        Save(original, paths.GsiPath, paths.TfaiPath, paths.TfafPath, updates);
    }

    // ── Asynchronous save ─────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously writes a save slot to three explicit file paths.
    /// Serialization is performed synchronously on a thread-pool thread;
    /// all three file writes are then issued as true async I/O.
    /// </summary>
    /// <param name="original">The save game loaded by <see cref="SaveGameLoader"/>.</param>
    /// <param name="gsiPath">Destination path for the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Destination path for the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Destination path for the <c>.tfaf</c> data blob.</param>
    /// <param name="updates">
    /// Optional bundle of per-type replacements. Pass <see langword="null"/> (or omit) to
    /// write the save back unmodified.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task SaveAsync(
        LoadedSave original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveGameUpdates? updates = null,
        CancellationToken cancellationToken = default
    )
    {
        // Serialization is CPU-bound; offload to the thread pool so the caller
        // (typically an Avalonia UI thread) is not blocked.
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
    /// <param name="original">The save game loaded by <see cref="SaveGameLoader"/>.</param>
    /// <param name="saveFolder">Directory containing the three save slot files.</param>
    /// <param name="slotName">Base file name without extension (e.g. <c>"slot1"</c>).</param>
    /// <param name="updates">
    /// Optional bundle of per-type replacements. Pass <see langword="null"/> (or omit) to
    /// write the save back unmodified.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
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

    // ── Shared serialization ──────────────────────────────────────────────────

    /// <summary>
    /// Builds the three byte payloads from the save game and all pending updates.
    /// Pure CPU work — no I/O. Used by both <c>Save</c> and <c>SaveAsync</c>.
    /// </summary>
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

    // ── Index rebuild ─────────────────────────────────────────────────────────

    /// <summary>
    /// Walks the original <see cref="SaveIndex"/> tree and produces a new one where
    /// every <see cref="TfaiFileEntry.Size"/> reflects the current payload length from
    /// <paramref name="files"/>.  Directory structure and entry order are preserved.
    /// </summary>
    internal static SaveIndex RebuildIndex(SaveIndex original, IReadOnlyDictionary<string, byte[]> files) =>
        SaveGameIndexRebuilder.Rebuild(original, files);
}
