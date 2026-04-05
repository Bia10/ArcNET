using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Loads an Arcanum save slot from disk into a <see cref="SaveGame"/> instance.
/// A save slot is identified by three files that share the same base name:
/// <list type="bullet">
///   <item><c>{slotName}.gsi</c> — save metadata</item>
///   <item><c>{slotName}.tfai</c> — archive index</item>
///   <item><c>{slotName}.tfaf</c> — archive data blob</item>
/// </list>
/// </summary>
public static class SaveGameLoader
{
    /// <summary>
    /// Loads a save slot from three explicit file paths.
    /// </summary>
    /// <param name="gsiPath">Path to the <c>.gsi</c> metadata file.</param>
    /// <param name="tfaiPath">Path to the <c>.tfai</c> index file.</param>
    /// <param name="tfafPath">Path to the <c>.tfaf</c> data blob.</param>
    public static SaveGame Load(string gsiPath, string tfaiPath, string tfafPath)
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
    public static SaveGame Load(string saveFolder, string slotName) =>
        Load(
            Path.Combine(saveFolder, slotName + ".gsi"),
            Path.Combine(saveFolder, slotName + ".tfai"),
            Path.Combine(saveFolder, slotName + ".tfaf")
        );

    internal static SaveGame LoadFromParsed(SaveInfo info, SaveIndex index, byte[] tfafData)
    {
        var files = TfafFormat.ExtractAll(index, tfafData);

        var mobiles = new Dictionary<string, MobData>(StringComparer.OrdinalIgnoreCase);
        var sectors = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase);
        var jumpFiles = new Dictionary<string, JmpFile>(StringComparer.OrdinalIgnoreCase);
        var mapProperties = new Dictionary<string, MapProperties>(StringComparer.OrdinalIgnoreCase);
        var scripts = new Dictionary<string, ScrFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, bytes) in files)
        {
            var mem = (ReadOnlyMemory<byte>)bytes;
            var ext = Path.GetExtension(path);

            // Each format is parsed independently; a corrupt/stub file of one type
            // does not prevent the rest of the save from loading.
            try
            {
                if (ext.Equals(".mob", StringComparison.OrdinalIgnoreCase))
                    mobiles[path] = MobFormat.ParseMemory(mem);
                else if (ext.Equals(".sec", StringComparison.OrdinalIgnoreCase))
                    sectors[path] = SectorFormat.ParseMemory(mem);
                else if (ext.Equals(".jmp", StringComparison.OrdinalIgnoreCase))
                    jumpFiles[path] = JmpFormat.ParseMemory(mem);
                else if (ext.Equals(".prp", StringComparison.OrdinalIgnoreCase))
                    mapProperties[path] = MapPropertiesFormat.ParseMemory(mem);
                else if (ext.Equals(".scr", StringComparison.OrdinalIgnoreCase))
                    scripts[path] = ScriptFormat.ParseMemory(mem);
            }
            catch (Exception)
            {
                // Silently skip files that fail to parse.
                // The raw bytes remain accessible via SaveGame.Files for manual inspection.
            }
        }

        return new SaveGame
        {
            Info = info,
            Index = index,
            Files = files,
            Mobiles = mobiles,
            Sectors = sectors,
            JumpFiles = jumpFiles,
            MapPropertiesList = mapProperties,
            Scripts = scripts,
        };
    }
}
