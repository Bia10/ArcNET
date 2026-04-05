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
        foreach (var (path, bytes) in files)
        {
            if (path.EndsWith(".mob", StringComparison.OrdinalIgnoreCase))
                mobiles[path] = MobFormat.ParseMemory(bytes);
        }

        return new SaveGame
        {
            Info = info,
            Index = index,
            Files = files,
            Mobiles = mobiles,
        };
    }
}
