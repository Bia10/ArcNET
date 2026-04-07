using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Writes a modified <see cref="SaveGame"/> back to disk.
/// When any embedded file is updated its bytes are recomputed, the TFAI index is rebuilt
/// to reflect any size changes, and all three save-slot files are written.
/// </summary>
public static class SaveGameWriter
{
    /// <summary>
    /// Writes a save slot to three explicit file paths.
    /// Pass only the dictionaries whose contents have changed; <see langword="null"/> keeps originals.
    /// </summary>
    /// <param name="original">The save game loaded by <see cref="SaveGameLoader"/>.</param>
    /// <param name="gsiPath">Destination path for the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Destination path for the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Destination path for the <c>.tfaf</c> data blob.</param>
    /// <param name="updatedInfo">
    /// Replacement <see cref="SaveInfo"/>; pass <see langword="null"/> to keep the original.
    /// </param>
    /// <param name="updatedMobiles">
    /// Map of virtual path → updated <see cref="MobData"/> to replace in the save.
    /// Only paths already present in <see cref="SaveGame.Files"/> are replaced.
    /// </param>
    /// <param name="updatedSectors">
    /// Map of virtual path → updated <see cref="Sector"/> to replace in the save.
    /// </param>
    /// <param name="updatedJumpFiles">
    /// Map of virtual path → updated <see cref="JmpFile"/> to replace in the save.
    /// </param>
    /// <param name="updatedMapProperties">
    /// Map of virtual path → updated <see cref="MapProperties"/> to replace in the save.
    /// </param>
    /// <param name="updatedScripts">
    /// Map of virtual path → updated <see cref="ScrFile"/> to replace in the save.
    /// </param>
    /// <param name="updatedDialogs">
    /// Map of virtual path → updated <see cref="DlgFile"/> to replace in the save.
    /// </param>
    /// <param name="updatedMobileMds">
    /// Map of virtual path → updated <see cref="MobileMdFile"/> to replace in the save.
    /// Keys must be paths to <c>mobile.md</c> files already present in <see cref="SaveGame.Files"/>.
    /// </param>
    /// <param name="updatedMobileMdys">
    /// Map of virtual path → updated <see cref="MobileMdyFile"/> to replace in the save.
    /// Keys must be paths to <c>mobile.mdy</c> files already present in <see cref="SaveGame.Files"/>.
    /// </param>
    /// <param name="rawFileUpdates">
    /// Map of virtual path → raw byte replacement applied after all typed updates.
    /// Use this for files that cannot be round-tripped through the typed parsers, such as
    /// <c>mobile.mdy</c> files containing v2 PC records edited via <see cref="SaveGameEditor"/>.
    /// </param>
    public static void Save(
        SaveGame original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveInfo? updatedInfo = null,
        IReadOnlyDictionary<string, MobData>? updatedMobiles = null,
        IReadOnlyDictionary<string, Sector>? updatedSectors = null,
        IReadOnlyDictionary<string, JmpFile>? updatedJumpFiles = null,
        IReadOnlyDictionary<string, MapProperties>? updatedMapProperties = null,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs = null,
        IReadOnlyDictionary<string, MobileMdFile>? updatedMobileMds = null,
        IReadOnlyDictionary<string, MobileMdyFile>? updatedMobileMdys = null,
        IReadOnlyDictionary<string, byte[]>? rawFileUpdates = null
    )
    {
        var (gsiBytes, tfaiBytes, tfafBytes) = Serialize(
            original,
            updatedInfo,
            updatedMobiles,
            updatedSectors,
            updatedJumpFiles,
            updatedMapProperties,
            updatedScripts,
            updatedDialogs,
            updatedMobileMds,
            updatedMobileMdys,
            rawFileUpdates
        );

        File.WriteAllBytes(gsiPath, gsiBytes);
        File.WriteAllBytes(tfaiPath, tfaiBytes);
        File.WriteAllBytes(tfafPath, tfafBytes);
    }

    // ── Shared serialization ──────────────────────────────────────────────────

    /// <summary>
    /// Builds the three byte payloads from the save game and all pending updates.
    /// Pure CPU work — no I/O. Used by both <c>Save</c> and <c>SaveAsync</c>.
    /// </summary>
    private static (byte[] gsi, byte[] tfai, byte[] tfaf) Serialize(
        SaveGame original,
        SaveInfo? updatedInfo,
        IReadOnlyDictionary<string, MobData>? updatedMobiles,
        IReadOnlyDictionary<string, Sector>? updatedSectors,
        IReadOnlyDictionary<string, JmpFile>? updatedJumpFiles,
        IReadOnlyDictionary<string, MapProperties>? updatedMapProperties,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs,
        IReadOnlyDictionary<string, MobileMdFile>? updatedMobileMds,
        IReadOnlyDictionary<string, MobileMdyFile>? updatedMobileMdys,
        IReadOnlyDictionary<string, byte[]>? rawFileUpdates
    )
    {
        var files = new Dictionary<string, byte[]>(original.Files, StringComparer.OrdinalIgnoreCase);

        if (updatedMobiles is not null)
            foreach (var (path, mob) in updatedMobiles)
                files[path] = MobFormat.WriteToArray(mob);

        if (updatedSectors is not null)
            foreach (var (path, sector) in updatedSectors)
                files[path] = SectorFormat.WriteToArray(sector);

        if (updatedJumpFiles is not null)
            foreach (var (path, jmp) in updatedJumpFiles)
                files[path] = JmpFormat.WriteToArray(jmp);

        if (updatedMapProperties is not null)
            foreach (var (path, props) in updatedMapProperties)
                files[path] = MapPropertiesFormat.WriteToArray(props);

        if (updatedScripts is not null)
            foreach (var (path, scr) in updatedScripts)
                files[path] = ScriptFormat.WriteToArray(scr);

        if (updatedDialogs is not null)
            foreach (var (path, dlg) in updatedDialogs)
                files[path] = DialogFormat.WriteToArray(dlg);

        if (updatedMobileMds is not null)
            foreach (var (path, md) in updatedMobileMds)
                files[path] = MobileMdFormat.WriteToArray(md);

        if (updatedMobileMdys is not null)
            foreach (var (path, mdy) in updatedMobileMdys)
                files[path] = MobileMdyFormat.WriteToArray(mdy);

        if (rawFileUpdates is not null)
            foreach (var (path, raw) in rawFileUpdates)
                files[path] = raw;

        var index = RebuildIndex(original.Index, files);

        return (
            SaveInfoFormat.WriteToArray(updatedInfo ?? original.Info),
            SaveIndexFormat.WriteToArray(index),
            TfafFormat.Pack(index, files)
        );
    }

    /// <summary>
    /// Writes a save slot by folder and slot name.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// </summary>
    public static void Save(
        SaveGame original,
        string saveFolder,
        string slotName,
        SaveInfo? updatedInfo = null,
        IReadOnlyDictionary<string, MobData>? updatedMobiles = null,
        IReadOnlyDictionary<string, Sector>? updatedSectors = null,
        IReadOnlyDictionary<string, JmpFile>? updatedJumpFiles = null,
        IReadOnlyDictionary<string, MapProperties>? updatedMapProperties = null,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs = null,
        IReadOnlyDictionary<string, MobileMdFile>? updatedMobileMds = null,
        IReadOnlyDictionary<string, MobileMdyFile>? updatedMobileMdys = null,
        IReadOnlyDictionary<string, byte[]>? rawFileUpdates = null
    ) =>
        Save(
            original,
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf"),
            updatedInfo,
            updatedMobiles,
            updatedSectors,
            updatedJumpFiles,
            updatedMapProperties,
            updatedScripts,
            updatedDialogs,
            updatedMobileMds,
            updatedMobileMdys,
            rawFileUpdates
        );

    // ── Asynchronous save ─────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously writes a save slot to three explicit file paths.
    /// Serialization is performed synchronously on a thread-pool thread;
    /// all three file writes are then issued as true async I/O.
    /// Pass only the dictionaries whose contents have changed; <see langword="null"/> keeps originals.
    /// </summary>
    public static async Task SaveAsync(
        SaveGame original,
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        SaveInfo? updatedInfo = null,
        IReadOnlyDictionary<string, MobData>? updatedMobiles = null,
        IReadOnlyDictionary<string, Sector>? updatedSectors = null,
        IReadOnlyDictionary<string, JmpFile>? updatedJumpFiles = null,
        IReadOnlyDictionary<string, MapProperties>? updatedMapProperties = null,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs = null,
        IReadOnlyDictionary<string, MobileMdFile>? updatedMobileMds = null,
        IReadOnlyDictionary<string, MobileMdyFile>? updatedMobileMdys = null,
        IReadOnlyDictionary<string, byte[]>? rawFileUpdates = null,
        CancellationToken cancellationToken = default
    )
    {
        // Serialization is CPU-bound; offload to the thread pool so the caller
        // (typically an Avalonia UI thread) is not blocked.
        var (gsiBytes, tfaiBytes, tfafBytes) = await Task.Run(
                () =>
                    Serialize(
                        original,
                        updatedInfo,
                        updatedMobiles,
                        updatedSectors,
                        updatedJumpFiles,
                        updatedMapProperties,
                        updatedScripts,
                        updatedDialogs,
                        updatedMobileMds,
                        updatedMobileMdys,
                        rawFileUpdates
                    ),
                cancellationToken
            )
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        await File.WriteAllBytesAsync(gsiPath, gsiBytes, cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(tfaiPath, tfaiBytes, cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(tfafPath, tfafBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a save slot by folder and slot name.
    /// Resolves paths as <c>{saveFolder}/{slotName}.gsi</c>, <c>.tfai</c>, and <c>.tfaf</c>.
    /// </summary>
    public static Task SaveAsync(
        SaveGame original,
        string saveFolder,
        string slotName,
        SaveInfo? updatedInfo = null,
        IReadOnlyDictionary<string, MobData>? updatedMobiles = null,
        IReadOnlyDictionary<string, Sector>? updatedSectors = null,
        IReadOnlyDictionary<string, JmpFile>? updatedJumpFiles = null,
        IReadOnlyDictionary<string, MapProperties>? updatedMapProperties = null,
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null,
        IReadOnlyDictionary<string, DlgFile>? updatedDialogs = null,
        IReadOnlyDictionary<string, MobileMdFile>? updatedMobileMds = null,
        IReadOnlyDictionary<string, MobileMdyFile>? updatedMobileMdys = null,
        IReadOnlyDictionary<string, byte[]>? rawFileUpdates = null,
        CancellationToken cancellationToken = default
    ) =>
        SaveAsync(
            original,
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf"),
            updatedInfo,
            updatedMobiles,
            updatedSectors,
            updatedJumpFiles,
            updatedMapProperties,
            updatedScripts,
            updatedDialogs,
            updatedMobileMds,
            updatedMobileMdys,
            rawFileUpdates,
            cancellationToken
        );

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
