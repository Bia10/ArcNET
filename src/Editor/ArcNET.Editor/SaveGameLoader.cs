using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Loads an Arcanum save slot from disk into a <see cref="LoadedSave"/> instance.
/// A save slot is identified by three files that share the same base name:
/// <list type="bullet">
///   <item><c>{slotName}.gsi</c> — save metadata</item>
///   <item><c>{slotName}.tfai</c> — archive index</item>
///   <item><c>{slotName}.tfaf</c> — archive data blob</item>
/// </list>
/// </summary>
public static class SaveGameLoader
{
    // ── Synchronous load ──────────────────────────────────────────────────────

    /// <summary>
    /// Loads a save slot from three explicit file paths.
    /// </summary>
    /// <param name="gsiPath">Path to the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Path to the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Path to the <c>.tfaf</c> data blob.</param>
    public static LoadedSave Load(string gsiPath, string tfaiPath, string tfafPath)
    {
        var info = SaveInfoFormat.ParseFile(gsiPath);
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        var tfafData = File.ReadAllBytes(tfafPath);
        return LoadFromParsed(info, index, tfafData);
    }

    /// <summary>
    /// Loads a save slot by folder and slot name.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// </summary>
    /// <param name="saveFolder">Directory containing the three save slot files.</param>
    /// <param name="slotName">Base file name without extension (e.g. <c>"slot1"</c>).</param>
    public static LoadedSave Load(string saveFolder, string slotName)
    {
        var paths = SaveSlotPathResolver.ResolveFromFolder(saveFolder, slotName);
        return Load(paths.GsiPath, paths.TfaiPath, paths.TfafPath);
    }

    // ── Asynchronous load ─────────────────────────────────────────────────────

    /// <summary>
    /// Loads a save slot from three explicit file paths asynchronously.
    /// <paramref name="progress"/> is reported per inner file parsed, in the range [0, 1].
    /// </summary>
    /// <param name="gsiPath">Path to the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Path to the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Path to the <c>.tfaf</c> data blob.</param>
    /// <param name="progress">
    /// Optional progress reporter. Receives values in [0, 1] as embedded files are parsed.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task<LoadedSave> LoadAsync(
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var info = await Task.Run(() => SaveInfoFormat.ParseFile(gsiPath), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var index = await Task.Run(() => SaveIndexFormat.ParseFile(tfaiPath), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var tfafData = await File.ReadAllBytesAsync(tfafPath, cancellationToken).ConfigureAwait(false);
        return await Task.Run(
                () => LoadFromParsed(info, index, tfafData, progress, cancellationToken),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a save slot by folder and slot name asynchronously.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// <paramref name="progress"/> is reported per inner file parsed, in the range [0, 1].
    /// </summary>
    /// <param name="saveFolder">Directory containing the three save slot files.</param>
    /// <param name="slotName">Base file name without extension (e.g. <c>"slot1"</c>).</param>
    /// <param name="progress">
    /// Optional progress reporter. Receives values in [0, 1] as embedded files are parsed.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static Task<LoadedSave> LoadAsync(
        string saveFolder,
        string slotName,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var paths = SaveSlotPathResolver.ResolveFromFolder(saveFolder, slotName);
        return LoadAsync(paths.GsiPath, paths.TfaiPath, paths.TfafPath, progress, cancellationToken);
    }

    // ── Core parsing ──────────────────────────────────────────────────────────

    internal static LoadedSave LoadFromParsed(
        SaveInfo info,
        SaveIndex index,
        byte[] tfafData,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var files = TfafFormat.ExtractAll(index, tfafData);
        return LoadFromFiles(info, index, files, progress, cancellationToken);
    }

    internal static LoadedSave LoadFromFiles(
        SaveInfo info,
        SaveIndex index,
        IReadOnlyDictionary<string, byte[]> files,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var builder = new LoadedSaveBuilder();

        var entries = files.ToList();
        var total = entries.Count;

        for (var i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (path, bytes) = (entries[i].Key, entries[i].Value);
            var mem = (ReadOnlyMemory<byte>)bytes;
            var format = FileFormatExtensions.FromPath(path);
            var fileName = Path.GetFileName(path);
            var hasTypedSurface = false;

            // Each format is parsed independently; a corrupt/stub file of one type
            // does not prevent the rest of the save from loading.
            try
            {
                hasTypedSurface = SaveEmbeddedFileHandlers.TryParse(path, fileName, format, mem, builder);
            }
            catch (Exception ex)
            {
                // Record the error but continue — raw bytes are still accessible via Files.
                builder.ParseErrors[path] = $"{ex.GetType().Name}: {ex.Message}";
            }

            if (!hasTypedSurface)
                builder.RawFiles[path] = bytes;

            progress?.Report((i + 1f) / total);
        }

        return builder.Build(info, index, files);
    }
}
