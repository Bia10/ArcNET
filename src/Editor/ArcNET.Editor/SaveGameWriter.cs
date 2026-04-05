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
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null
    )
    {
        // 1. Build updated files dictionary starting from the original raw bytes.
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

        // 2. Rebuild TFAI index — walk the original tree and update any TfaiFileEntry
        //    whose payload size has changed.  This preserves directory structure and ordering.
        var index = RebuildIndex(original.Index, files);

        // 3. Serialize.
        var gsiBytes = SaveInfoFormat.WriteToArray(updatedInfo ?? original.Info);
        var tfaiBytes = SaveIndexFormat.WriteToArray(index);
        var tfafBytes = TfafFormat.Pack(index, files);

        // 4. Write all three files.
        File.WriteAllBytes(gsiPath, gsiBytes);
        File.WriteAllBytes(tfaiPath, tfaiBytes);
        File.WriteAllBytes(tfafPath, tfafBytes);
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
        IReadOnlyDictionary<string, ScrFile>? updatedScripts = null
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
            updatedScripts
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
