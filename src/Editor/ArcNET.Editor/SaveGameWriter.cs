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

        // Write each file to a temp path first, then atomically rename into place.
        // This prevents a crash or I/O error between writes from leaving the three
        // save-slot files in an inconsistent state.
        var gsiTemp = gsiPath + ".tmp";
        var tfaiTemp = tfaiPath + ".tmp";
        var tfafTemp = tfafPath + ".tmp";
        try
        {
            File.WriteAllBytes(gsiTemp, gsiBytes);
            File.WriteAllBytes(tfaiTemp, tfaiBytes);
            File.WriteAllBytes(tfafTemp, tfafBytes);
            File.Move(gsiTemp, gsiPath, overwrite: true);
            File.Move(tfaiTemp, tfaiPath, overwrite: true);
            File.Move(tfafTemp, tfafPath, overwrite: true);
        }
        catch
        {
            TryDeleteSilently(gsiTemp);
            TryDeleteSilently(tfaiTemp);
            TryDeleteSilently(tfafTemp);
            throw;
        }
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
    public static void Save(LoadedSave original, string saveFolder, string slotName, SaveGameUpdates? updates = null) =>
        Save(
            original,
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf"),
            updates
        );

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

        // Write each file to a temp path first, then atomically rename into place.
        var gsiTemp = gsiPath + ".tmp";
        var tfaiTemp = tfaiPath + ".tmp";
        var tfafTemp = tfafPath + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(gsiTemp, gsiBytes, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(tfaiTemp, tfaiBytes, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(tfafTemp, tfafBytes, cancellationToken).ConfigureAwait(false);
            File.Move(gsiTemp, gsiPath, overwrite: true);
            File.Move(tfaiTemp, tfaiPath, overwrite: true);
            File.Move(tfafTemp, tfafPath, overwrite: true);
        }
        catch
        {
            TryDeleteSilently(gsiTemp);
            TryDeleteSilently(tfaiTemp);
            TryDeleteSilently(tfafTemp);
            throw;
        }
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
    ) =>
        SaveAsync(
            original,
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf"),
            updates,
            cancellationToken
        );

    // ── Shared serialization ──────────────────────────────────────────────────

    private static void TryDeleteSilently(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        { /* best effort */
        }
    }

    /// <summary>
    /// Builds the three byte payloads from the save game and all pending updates.
    /// Pure CPU work — no I/O. Used by both <c>Save</c> and <c>SaveAsync</c>.
    /// </summary>
    private static (byte[] gsi, byte[] tfai, byte[] tfaf) Serialize(LoadedSave original, SaveGameUpdates? updates)
    {
        var files = new Dictionary<string, byte[]>(original.Files, StringComparer.OrdinalIgnoreCase);

        // Explicit lambdas are required because WriteToArray methods use `in` parameters,
        // which are incompatible with the Func<T, byte[]> delegate (by-value parameter).
        ApplyUpdates(files, updates?.UpdatedMobiles, static m => MobFormat.WriteToArray(m));
        ApplyUpdates(files, updates?.UpdatedSectors, static s => SectorFormat.WriteToArray(s));
        ApplyUpdates(files, updates?.UpdatedJumpFiles, static j => JmpFormat.WriteToArray(j));
        ApplyUpdates(files, updates?.UpdatedMapProperties, static p => MapPropertiesFormat.WriteToArray(p));
        ApplyUpdates(files, updates?.UpdatedScripts, static s => ScriptFormat.WriteToArray(s));
        ApplyUpdates(files, updates?.UpdatedDialogs, static d => DialogFormat.WriteToArray(d));
        ApplyUpdates(files, updates?.UpdatedMobileMds, static f => MobileMdFormat.WriteToArray(f));
        ApplyUpdates(files, updates?.UpdatedMobileMdys, static f => MobileMdyFormat.WriteToArray(f));
        ApplyUpdates(files, updates?.RawFileUpdates, static bytes => bytes);

        var index = RebuildIndex(original.Index, files);

        return (
            SaveInfoFormat.WriteToArray(updates?.UpdatedInfo ?? original.Info),
            SaveIndexFormat.WriteToArray(index),
            TfafFormat.Pack(index, files)
        );
    }

    /// <summary>
    /// Serializes every entry in <paramref name="updates"/> via <paramref name="serialize"/>
    /// and merges the results into <paramref name="files"/>, overwriting existing entries.
    /// No-op when <paramref name="updates"/> is <see langword="null"/>.
    /// </summary>
    private static void ApplyUpdates<T>(
        Dictionary<string, byte[]> files,
        IReadOnlyDictionary<string, T>? updates,
        Func<T, byte[]> serialize
    )
    {
        if (updates is null)
            return;
        foreach (var (path, item) in updates)
            files[path] = serialize(item);
    }

    // ── Index rebuild ─────────────────────────────────────────────────────────

    /// <summary>
    /// Walks the original <see cref="SaveIndex"/> tree and produces a new one where
    /// every <see cref="TfaiFileEntry.Size"/> reflects the current payload length from
    /// <paramref name="files"/>.  Directory structure and entry order are preserved.
    /// </summary>
    internal static SaveIndex RebuildIndex(SaveIndex original, IReadOnlyDictionary<string, byte[]> files)
    {
        return new SaveIndex { Root = RebuildEntries(original.Root, string.Empty, files) };
    }

    private static IReadOnlyList<TfaiEntry> RebuildEntries(
        IReadOnlyList<TfaiEntry> entries,
        string pathPrefix,
        IReadOnlyDictionary<string, byte[]> files
    )
    {
        var result = new List<TfaiEntry>(entries.Count);
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                    {
                        var key = pathPrefix.Length == 0 ? file.Name : $"{pathPrefix}/{file.Name}";
                        var newSize = files.TryGetValue(key, out var payload) ? payload.Length : file.Size;
                        result.Add(new TfaiFileEntry { Name = file.Name, Size = newSize });
                        break;
                    }

                case TfaiDirectoryEntry dir:
                    {
                        var childPrefix = pathPrefix.Length == 0 ? dir.Name : $"{pathPrefix}/{dir.Name}";
                        result.Add(
                            new TfaiDirectoryEntry
                            {
                                Name = dir.Name,
                                Children = RebuildEntries(dir.Children, childPrefix, files),
                            }
                        );
                        break;
                    }
            }
        }

        return result.AsReadOnly();
    }
}
