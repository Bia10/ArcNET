namespace ArcNET.Formats;

/// <summary>
/// Serialises a <see cref="SaveGame"/> back to disk or to raw bytes.
/// <para>
/// All three companion files (<c>.tfai</c>, <c>.tfaf</c>, <c>.gsi</c>) are written
/// atomically in the sense that <see cref="SaveToMemory"/> produces all three byte arrays
/// before any file is written.
/// </para>
/// <para>
/// <b>File ordering note:</b> the canonical file order written by this class within each
/// map directory may differ from the order in the original save. The resulting TFAI+TFAF
/// pair is functionally identical and is correctly parsed by both the game engine and by
/// <see cref="SaveGameReader"/>.
/// </para>
/// </summary>
public static class SaveGameWriter
{
    /// <summary>
    /// Serialises <paramref name="save"/> and writes the three companion files to disk.
    /// The <c>.tfaf</c> and <c>.gsi</c> paths are derived from <paramref name="tfaiPath"/>
    /// by replacing the extension.
    /// </summary>
    public static void Save(SaveGame save, string tfaiPath)
    {
        var paths = SaveSlotPathResolver.ResolveFromTfaiPath(tfaiPath);
        Save(save, paths.TfaiPath, paths.TfafPath, paths.GsiPath);
    }

    /// <summary>
    /// Serialises <paramref name="save"/> and writes the three companion files to disk.
    /// The <c>.gsi</c> path is derived from <paramref name="tfaiPath"/> by replacing the extension.
    /// </summary>
    public static void Save(SaveGame save, string tfaiPath, string tfafPath)
    {
        var paths = SaveSlotPathResolver.ResolveFromTfaiAndTfafPaths(tfaiPath, tfafPath);
        Save(save, paths.TfaiPath, paths.TfafPath, paths.GsiPath);
    }

    /// <summary>
    /// Serialises <paramref name="save"/> and writes all three companion files to the
    /// explicitly specified paths.
    /// </summary>
    public static void Save(SaveGame save, string tfaiPath, string tfafPath, string gsiPath)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentException.ThrowIfNullOrWhiteSpace(tfaiPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tfafPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(gsiPath);

        var (tfai, tfaf, gsi) = SaveToMemory(save);
        File.WriteAllBytes(tfaiPath, tfai);
        File.WriteAllBytes(tfafPath, tfaf);
        File.WriteAllBytes(gsiPath, gsi);
    }

    /// <summary>
    /// Serialises <paramref name="save"/> to three in-memory byte arrays without touching
    /// the filesystem.
    /// </summary>
    /// <returns>
    /// A tuple of (<c>Tfai</c>, <c>Tfaf</c>, <c>Gsi</c>) byte arrays ready to be written
    /// to their respective files.
    /// </returns>
    public static (byte[] Tfai, byte[] Tfaf, byte[] Gsi) SaveToMemory(SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var payloads = SerializePayloads(save);
        var index = BuildIndex(save, payloads);

        var tfai = SaveIndexFormat.WriteToArray(index);
        var tfaf = TfafFormat.Pack(index, payloads);
        var gsi = SaveInfoFormat.WriteToArray(save.Info);

        return (tfai, tfaf, gsi);
    }

    private static Dictionary<string, byte[]> SerializePayloads(SaveGame save)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var (virtualPath, data) in save.MessageFiles)
            result[virtualPath] = data;

        foreach (var (virtualPath, fog) in save.TownMapFogs)
            result[virtualPath] = TownMapFogFormat.WriteToArray(fog);

        foreach (var (virtualPath, dataSav) in save.DataSavFiles)
            result[virtualPath] = DataSavFormat.WriteToArray(dataSav);

        foreach (var (virtualPath, data2) in save.Data2SavFiles)
            result[virtualPath] = Data2SavFormat.WriteToArray(data2);

        foreach (var (virtualPath, data) in save.RawFiles)
            result[virtualPath] = data;

        foreach (var map in save.Maps)
        {
            var prefix = map.MapPath;

            if (map.Properties is not null)
                result[$"{prefix}/map.prp"] = MapPropertiesFormat.WriteToArray(map.Properties);

            if (map.JumpPoints is not null)
                result[$"{prefix}/map.jmp"] = JmpFormat.WriteToArray(map.JumpPoints);

            foreach (var (fileName, mob) in map.StaticObjects)
                result[$"{prefix}/mobile/{fileName}"] = MobFormat.WriteToArray(mob);

            if (map.StaticDiffs is not null)
                result[$"{prefix}/mobile.md"] = MobileMdFormat.WriteToArray(map.StaticDiffs);

            if (map.DynamicObjects is not null)
                result[$"{prefix}/mobile.mdy"] = MobileMdyFormat.WriteToArray(map.DynamicObjects);

            foreach (var (fileName, sector) in map.Sectors)
                result[$"{prefix}/{fileName}"] = SectorFormat.WriteToArray(sector);

            foreach (var (relativePath, data) in map.UnknownFiles)
                result[$"{prefix}/{relativePath}"] = data;
        }

        return result;
    }

    private static SaveIndex BuildIndex(SaveGame save, IReadOnlyDictionary<string, byte[]> payloads) =>
        SaveIndexBuilder.Build(save, payloads);
}
