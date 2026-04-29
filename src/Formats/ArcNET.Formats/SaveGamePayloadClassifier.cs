namespace ArcNET.Formats;

internal static class SaveGamePayloadClassifier
{
    public static SaveGamePayloadCatalog Classify(IReadOnlyDictionary<string, byte[]> payloads)
    {
        var builders = new Dictionary<string, SaveMapStateBuilder>(StringComparer.OrdinalIgnoreCase);
        var messageFiles = new List<(string VirtualPath, byte[] Data)>();
        var townMapFogs = new List<(string VirtualPath, TownMapFog Data)>();
        var dataSavFiles = new List<(string VirtualPath, DataSavFile Data)>();
        var data2SavFiles = new List<(string VirtualPath, Data2SavFile Data)>();
        var rawFiles = new List<(string VirtualPath, byte[] Data)>();

        foreach (var (virtualPath, data) in payloads)
        {
            if (TryGetMapPath(virtualPath, out var mapPath, out var relPath))
            {
                if (!builders.TryGetValue(mapPath, out var builder))
                {
                    builder = new SaveMapStateBuilder(mapPath);
                    builders[mapPath] = builder;
                }

                builder.Add(relPath, data);
                continue;
            }

            if (virtualPath.EndsWith(".mes", StringComparison.OrdinalIgnoreCase))
            {
                messageFiles.Add((virtualPath, data));
                continue;
            }

            if (virtualPath.EndsWith(".tmf", StringComparison.OrdinalIgnoreCase))
            {
                townMapFogs.Add((virtualPath, TownMapFogFormat.ParseMemory(data)));
                continue;
            }

            if (Path.GetFileName(virtualPath).Equals("data.sav", StringComparison.OrdinalIgnoreCase))
            {
                dataSavFiles.Add((virtualPath, DataSavFormat.ParseMemory(data)));
                continue;
            }

            if (Path.GetFileName(virtualPath).Equals("data2.sav", StringComparison.OrdinalIgnoreCase))
            {
                data2SavFiles.Add((virtualPath, Data2SavFormat.ParseMemory(data)));
                continue;
            }

            rawFiles.Add((virtualPath, data));
        }

        return new SaveGamePayloadCatalog
        {
            MapBuilders = builders.Values.ToList(),
            MessageFiles = messageFiles,
            TownMapFogs = townMapFogs,
            DataSavFiles = dataSavFiles,
            Data2SavFiles = data2SavFiles,
            RawFiles = rawFiles,
        };
    }

    private static bool TryGetMapPath(string virtualPath, out string mapPath, out string relPath)
    {
        var parts = virtualPath.Split('/');
        if (
            parts.Length >= 5
            && parts[0].Equals("modules", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("maps", StringComparison.OrdinalIgnoreCase)
        )
        {
            mapPath = $"{parts[0]}/{parts[1]}/{parts[2]}/{parts[3]}";
            relPath = string.Join("/", parts, 4, parts.Length - 4);
            return true;
        }

        mapPath = string.Empty;
        relPath = string.Empty;
        return false;
    }
}
