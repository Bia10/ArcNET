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
        var tfafPath = Path.ChangeExtension(tfaiPath, ".tfaf");
        var gsiPath = Path.ChangeExtension(tfaiPath, ".gsi");
        Save(save, tfaiPath, tfafPath, gsiPath);
    }

    /// <summary>
    /// Serialises <paramref name="save"/> and writes the three companion files to disk.
    /// The <c>.gsi</c> path is derived from <paramref name="tfaiPath"/> by replacing the extension.
    /// </summary>
    public static void Save(SaveGame save, string tfaiPath, string tfafPath)
    {
        var gsiPath = Path.ChangeExtension(tfaiPath, ".gsi");
        Save(save, tfaiPath, tfafPath, gsiPath);
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

    // ── Payload serialization ─────────────────────────────────────────────────

    private static Dictionary<string, byte[]> SerializePayloads(SaveGame save)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        // Module-level message file overrides.
        foreach (var (virtualPath, data) in save.MessageFiles)
            result[virtualPath] = data;

        // Top-level typed save-global files.
        foreach (var (virtualPath, fog) in save.TownMapFogs)
            result[virtualPath] = TownMapFogFormat.WriteToArray(fog);

        foreach (var (virtualPath, dataSav) in save.DataSavFiles)
            result[virtualPath] = DataSavFormat.WriteToArray(dataSav);

        foreach (var (virtualPath, data2) in save.Data2SavFiles)
            result[virtualPath] = Data2SavFormat.WriteToArray(data2);

        // Top-level raw save-global files that are still under reverse-engineering.
        foreach (var (virtualPath, data) in save.RawFiles)
            result[virtualPath] = data;

        // Per-map files.
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
        }

        return result;
    }

    // ── TFAI index reconstruction ─────────────────────────────────────────────

    private static SaveIndex BuildIndex(SaveGame save, IReadOnlyDictionary<string, byte[]> payloads)
    {
        // Group maps and message files by module name so they share a module directory node.
        var moduleNames = new List<string>();
        var mapsByModule = new Dictionary<string, List<SaveMapState>>(StringComparer.OrdinalIgnoreCase);
        var messagesByModule = new Dictionary<string, List<(string FileName, byte[] Data)>>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var map in save.Maps)
        {
            var module = ExtractModule(map.MapPath);
            if (!mapsByModule.TryGetValue(module, out var list))
            {
                list = [];
                mapsByModule[module] = list;
                moduleNames.Add(module);
            }

            list.Add(map);
        }

        foreach (var (virtualPath, data) in save.MessageFiles)
        {
            var module = ExtractModuleFromMesPath(virtualPath);
            if (!messagesByModule.TryGetValue(module, out var list))
            {
                list = [];
                messagesByModule[module] = list;
                if (!mapsByModule.ContainsKey(module))
                    moduleNames.Add(module);
            }

            var fileName = virtualPath.Substring(virtualPath.LastIndexOf('/') + 1);
            list.Add((fileName, data));
        }

        // Build module directory entries.
        var moduleEntries = new List<TfaiEntry>(moduleNames.Count);
        foreach (var moduleName in moduleNames)
        {
            var children = new List<TfaiEntry>();

            // maps/ sub-directory.
            if (mapsByModule.TryGetValue(moduleName, out var maps) && maps.Count > 0)
            {
                var mapEntries = new List<TfaiEntry>(maps.Count);
                foreach (var map in maps)
                {
                    var mapName = ExtractMapName(map.MapPath);
                    var mapFileEntries = BuildMapEntries(map, payloads);
                    mapEntries.Add(new TfaiDirectoryEntry { Name = mapName, Children = mapFileEntries });
                }

                children.Add(new TfaiDirectoryEntry { Name = "maps", Children = mapEntries });
            }

            // Module-level .mes files (siblings of maps/).
            if (messagesByModule.TryGetValue(moduleName, out var messages))
            {
                foreach (var (fileName, data) in messages)
                    children.Add(new TfaiFileEntry { Name = fileName, Size = data.Length });
            }

            moduleEntries.Add(new TfaiDirectoryEntry { Name = moduleName, Children = children });
        }

        var root = new List<TfaiEntry>();
        if (moduleEntries.Count > 0)
            root.Add(new TfaiDirectoryEntry { Name = "modules", Children = moduleEntries });

        foreach (var (virtualPath, _) in save.TownMapFogs)
            AddPath(root, virtualPath, payloads[virtualPath].Length);

        foreach (var (virtualPath, _) in save.DataSavFiles)
            AddPath(root, virtualPath, payloads[virtualPath].Length);

        foreach (var (virtualPath, _) in save.Data2SavFiles)
            AddPath(root, virtualPath, payloads[virtualPath].Length);

        foreach (var (virtualPath, _) in save.RawFiles)
            AddPath(root, virtualPath, payloads[virtualPath].Length);

        return new SaveIndex { Root = root };
    }

    /// <summary>Builds the ordered list of TFAI child entries for one map directory.</summary>
    /// <remarks>
    /// Canonical order: <c>map.prp</c>, <c>map.jmp</c>, <c>mobile/</c>, <c>mobile.md</c>,
    /// <c>mobile.mdy</c>, then <c>sector_*.sec</c> files in their original order.
    /// </remarks>
    private static List<TfaiEntry> BuildMapEntries(SaveMapState map, IReadOnlyDictionary<string, byte[]> payloads)
    {
        var prefix = map.MapPath;
        var entries = new List<TfaiEntry>();

        if (map.Properties is not null)
        {
            var size = payloads[$"{prefix}/map.prp"].Length;
            entries.Add(new TfaiFileEntry { Name = "map.prp", Size = size });
        }

        if (map.JumpPoints is not null)
        {
            var size = payloads[$"{prefix}/map.jmp"].Length;
            entries.Add(new TfaiFileEntry { Name = "map.jmp", Size = size });
        }

        if (map.StaticObjects.Count > 0)
        {
            var mobEntries = new List<TfaiEntry>(map.StaticObjects.Count);
            foreach (var (fileName, _) in map.StaticObjects)
            {
                var size = payloads[$"{prefix}/mobile/{fileName}"].Length;
                mobEntries.Add(new TfaiFileEntry { Name = fileName, Size = size });
            }

            entries.Add(new TfaiDirectoryEntry { Name = "mobile", Children = mobEntries });
        }

        if (map.StaticDiffs is not null)
        {
            var size = payloads[$"{prefix}/mobile.md"].Length;
            entries.Add(new TfaiFileEntry { Name = "mobile.md", Size = size });
        }

        if (map.DynamicObjects is not null)
        {
            var size = payloads[$"{prefix}/mobile.mdy"].Length;
            entries.Add(new TfaiFileEntry { Name = "mobile.mdy", Size = size });
        }

        foreach (var (fileName, _) in map.Sectors)
        {
            var size = payloads[$"{prefix}/{fileName}"].Length;
            entries.Add(new TfaiFileEntry { Name = fileName, Size = size });
        }

        return entries;
    }

    // ── Path helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the module segment from a map path of the form
    /// <c>modules/&lt;module&gt;/maps/&lt;mapname&gt;</c>.
    /// </summary>
    private static string ExtractModule(string mapPath)
    {
        // modules/<module>/maps/<mapname>
        //         ^------^
        var firstSlash = mapPath.IndexOf('/');
        var secondSlash = mapPath.IndexOf('/', firstSlash + 1);
        return mapPath.Substring(firstSlash + 1, secondSlash - firstSlash - 1);
    }

    /// <summary>
    /// Extracts the map-name segment from a path of the form
    /// <c>modules/&lt;module&gt;/maps/&lt;mapname&gt;</c>.
    /// </summary>
    private static string ExtractMapName(string mapPath)
    {
        var lastSlash = mapPath.LastIndexOf('/');
        return mapPath.Substring(lastSlash + 1);
    }

    /// <summary>
    /// Extracts the module segment from a module-level .mes virtual path of the form
    /// <c>modules/&lt;module&gt;/&lt;file&gt;.mes</c>.
    /// Falls back to an empty string for unexpected path formats.
    /// </summary>
    private static string ExtractModuleFromMesPath(string virtualPath)
    {
        var parts = virtualPath.Split('/');
        return parts.Length >= 2 ? parts[1] : string.Empty;
    }

    private static void AddPath(List<TfaiEntry> entries, string virtualPath, int size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(virtualPath);

        var parts = virtualPath.Split('/');
        var current = entries;
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (index == parts.Length - 1)
            {
                current.Add(new TfaiFileEntry { Name = part, Size = size });
                return;
            }

            var dir = FindOrCreateDirectory(current, part);
            current = GetMutableChildren(dir);
        }
    }

    private static TfaiDirectoryEntry FindOrCreateDirectory(List<TfaiEntry> entries, string name)
    {
        foreach (var entry in entries)
        {
            if (entry is TfaiDirectoryEntry dir && dir.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return dir;
        }

        var children = new List<TfaiEntry>();
        var created = new TfaiDirectoryEntry { Name = name, Children = children };
        entries.Add(created);
        return created;
    }

    private static List<TfaiEntry> GetMutableChildren(TfaiDirectoryEntry dir) =>
        dir.Children as List<TfaiEntry>
        ?? throw new InvalidOperationException("Save index builder requires mutable TfaiDirectoryEntry child lists.");
}
