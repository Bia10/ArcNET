namespace ArcNET.Formats;

internal static class SaveIndexBuilder
{
    private const string ModulesDirectory = "modules";
    private const string MapsDirectory = "maps";
    private const string MobileDirectory = "mobile";
    private const string MapPropertiesFileName = "map.prp";
    private const string JumpPointsFileName = "map.jmp";
    private const string StaticDiffFileName = "mobile.md";
    private const string DynamicObjectsFileName = "mobile.mdy";

    public static SaveIndex Build(SaveGame save, IReadOnlyDictionary<string, byte[]> payloads)
    {
        var (moduleNames, mapsByModule) = GroupByModule(save.Maps, static map => ExtractModule(map.MapPath));
        var (_, messagesByModule) = GroupByModule(
            save.MessageFiles,
            static message => ExtractModuleFromMesPath(message.VirtualPath)
        );

        foreach (var moduleName in messagesByModule.Keys)
            EnsureModuleNameTracked(moduleNames, mapsByModule, moduleName);

        var moduleEntries = new List<TfaiEntry>(moduleNames.Count);
        foreach (var moduleName in moduleNames)
        {
            var children = new List<TfaiEntry>();

            if (mapsByModule.TryGetValue(moduleName, out var maps) && maps.Count > 0)
            {
                var mapEntries = new List<TfaiEntry>(maps.Count);
                foreach (var map in maps)
                {
                    var mapName = ExtractMapName(map.MapPath);
                    mapEntries.Add(
                        new TfaiDirectoryEntry { Name = mapName, Children = BuildMapEntries(map, payloads) }
                    );
                }

                children.Add(new TfaiDirectoryEntry { Name = MapsDirectory, Children = mapEntries });
            }

            if (messagesByModule.TryGetValue(moduleName, out var messages))
            {
                foreach (var (virtualPath, data) in messages)
                    children.Add(new TfaiFileEntry { Name = Path.GetFileName(virtualPath), Size = data.Length });
            }

            moduleEntries.Add(new TfaiDirectoryEntry { Name = moduleName, Children = children });
        }

        var root = new List<TfaiEntry>();
        if (moduleEntries.Count > 0)
            root.Add(new TfaiDirectoryEntry { Name = ModulesDirectory, Children = moduleEntries });

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

    private static List<TfaiEntry> BuildMapEntries(SaveMapState map, IReadOnlyDictionary<string, byte[]> payloads)
    {
        var prefix = map.MapPath;
        var entries = new List<TfaiEntry>();

        if (map.Properties is not null)
            entries.Add(
                new TfaiFileEntry
                {
                    Name = MapPropertiesFileName,
                    Size = payloads[$"{prefix}/{MapPropertiesFileName}"].Length,
                }
            );

        if (map.JumpPoints is not null)
            entries.Add(
                new TfaiFileEntry
                {
                    Name = JumpPointsFileName,
                    Size = payloads[$"{prefix}/{JumpPointsFileName}"].Length,
                }
            );

        if (map.StaticObjects.Count > 0)
        {
            var mobEntries = new List<TfaiEntry>(map.StaticObjects.Count);
            foreach (var (fileName, _) in map.StaticObjects)
                mobEntries.Add(
                    new TfaiFileEntry
                    {
                        Name = fileName,
                        Size = payloads[$"{prefix}/{MobileDirectory}/{fileName}"].Length,
                    }
                );

            entries.Add(new TfaiDirectoryEntry { Name = MobileDirectory, Children = mobEntries });
        }

        if (map.StaticDiffs is not null)
            entries.Add(
                new TfaiFileEntry
                {
                    Name = StaticDiffFileName,
                    Size = payloads[$"{prefix}/{StaticDiffFileName}"].Length,
                }
            );

        if (map.DynamicObjects is not null)
            entries.Add(
                new TfaiFileEntry
                {
                    Name = DynamicObjectsFileName,
                    Size = payloads[$"{prefix}/{DynamicObjectsFileName}"].Length,
                }
            );

        foreach (var (fileName, _) in map.Sectors)
            entries.Add(new TfaiFileEntry { Name = fileName, Size = payloads[$"{prefix}/{fileName}"].Length });

        foreach (var (relativePath, _) in map.UnknownFiles)
            AddPath(entries, relativePath, payloads[$"{prefix}/{relativePath}"].Length);

        return entries;
    }

    private static (List<string> ModuleNames, Dictionary<string, List<T>> Groups) GroupByModule<T>(
        IEnumerable<T> items,
        Func<T, string> getModule
    )
    {
        var moduleNames = new List<string>();
        var groups = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var moduleName = getModule(item);
            if (!groups.TryGetValue(moduleName, out var list))
            {
                list = [];
                groups[moduleName] = list;
                moduleNames.Add(moduleName);
            }

            list.Add(item);
        }

        return (moduleNames, groups);
    }

    private static void EnsureModuleNameTracked(
        List<string> moduleNames,
        Dictionary<string, List<SaveMapState>> mapsByModule,
        string moduleName
    )
    {
        if (!mapsByModule.ContainsKey(moduleName))
            moduleNames.Add(moduleName);
    }

    private static string ExtractModule(string mapPath)
    {
        var firstSlash = mapPath.IndexOf('/');
        var secondSlash = mapPath.IndexOf('/', firstSlash + 1);
        return mapPath.Substring(firstSlash + 1, secondSlash - firstSlash - 1);
    }

    private static string ExtractMapName(string mapPath)
    {
        var lastSlash = mapPath.LastIndexOf('/');
        return mapPath.Substring(lastSlash + 1);
    }

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

            var directory = FindOrCreateDirectory(current, part);
            current = GetMutableChildren(directory);
        }
    }

    private static TfaiDirectoryEntry FindOrCreateDirectory(List<TfaiEntry> entries, string name)
    {
        foreach (var entry in entries)
        {
            if (
                entry is TfaiDirectoryEntry directory
                && directory.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            )
                return directory;
        }

        var children = new List<TfaiEntry>();
        var created = new TfaiDirectoryEntry { Name = name, Children = children };
        entries.Add(created);
        return created;
    }

    private static List<TfaiEntry> GetMutableChildren(TfaiDirectoryEntry directory) =>
        directory.Children as List<TfaiEntry>
        ?? throw new InvalidOperationException("Save index builder requires mutable TfaiDirectoryEntry child lists.");
}
