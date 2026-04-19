namespace ArcNET.Formats;

/// <summary>
/// Loads a complete Arcanum save slot from disk or from raw bytes.
/// <para>
/// A save slot consists of three files sharing a stem name:
/// <c>&lt;slot&gt;.tfai</c>, <c>&lt;slot&gt;.tfaf</c>, and <c>&lt;slot&gt;.gsi</c>.
/// When only a TFAI path is supplied the companion paths are derived by replacing the extension.
/// </para>
/// </summary>
public static class SaveGameReader
{
    /// <summary>
    /// Loads the save slot whose TFAI index is at <paramref name="tfaiPath"/>.
    /// The companion <c>.tfaf</c> and <c>.gsi</c> paths are derived automatically.
    /// </summary>
    /// <param name="tfaiPath">Full filesystem path to the <c>.tfai</c> index file.</param>
    /// <returns>A fully-parsed <see cref="SaveGame"/> containing all map states.</returns>
    public static SaveGame Load(string tfaiPath)
    {
        var tfafPath = Path.ChangeExtension(tfaiPath, ".tfaf");
        var gsiPath = Path.ChangeExtension(tfaiPath, ".gsi");
        return Load(tfaiPath, tfafPath, gsiPath);
    }

    /// <summary>
    /// Loads the save slot whose TFAI index is at <paramref name="tfaiPath"/> and whose
    /// TFAF data blob is at <paramref name="tfafPath"/>.
    /// The companion <c>.gsi</c> path is derived from <paramref name="tfaiPath"/>.
    /// </summary>
    public static SaveGame Load(string tfaiPath, string tfafPath)
    {
        var gsiPath = Path.ChangeExtension(tfaiPath, ".gsi");
        return Load(tfaiPath, tfafPath, gsiPath);
    }

    /// <summary>
    /// Loads the save slot from explicitly specified paths for all three companion files.
    /// </summary>
    public static SaveGame Load(string tfaiPath, string tfafPath, string gsiPath)
    {
        var index = SaveIndexFormat.ParseFile(tfaiPath);
        var tfafData = (ReadOnlyMemory<byte>)File.ReadAllBytes(tfafPath);
        var gsiData = (ReadOnlyMemory<byte>)File.ReadAllBytes(gsiPath);

        var payloads = TfafFormat.ExtractAll(index, tfafData);
        var info = SaveInfoFormat.ParseMemory(gsiData);

        return ParseSaveGame(info, payloads);
    }

    /// <summary>
    /// Parses a <see cref="SaveGame"/> from pre-loaded byte blobs without filesystem access.
    /// </summary>
    /// <param name="tfaiData">Raw bytes of the <c>.tfai</c> index file.</param>
    /// <param name="tfafData">Raw bytes of the <c>.tfaf</c> data blob.</param>
    /// <param name="gsiData">Raw bytes of the <c>.gsi</c> metadata file.</param>
    public static SaveGame ParseMemory(
        ReadOnlyMemory<byte> tfaiData,
        ReadOnlyMemory<byte> tfafData,
        ReadOnlyMemory<byte> gsiData
    )
    {
        var index = SaveIndexFormat.ParseMemory(tfaiData);
        var payloads = TfafFormat.ExtractAll(index, tfafData);
        var info = SaveInfoFormat.ParseMemory(gsiData);
        return ParseSaveGame(info, payloads);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static SaveGame ParseSaveGame(SaveInfo info, IReadOnlyDictionary<string, byte[]> payloads)
    {
        var builders = new Dictionary<string, MapStateBuilder>(StringComparer.OrdinalIgnoreCase);
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
                    builder = new MapStateBuilder(mapPath);
                    builders[mapPath] = builder;
                }

                builder.Add(relPath, data);
            }
            else if (virtualPath.EndsWith(".mes", StringComparison.OrdinalIgnoreCase))
            {
                messageFiles.Add((virtualPath, data));
            }
            else if (virtualPath.EndsWith(".tmf", StringComparison.OrdinalIgnoreCase))
            {
                townMapFogs.Add((virtualPath, TownMapFogFormat.ParseMemory(data)));
            }
            else if (Path.GetFileName(virtualPath).Equals("data.sav", StringComparison.OrdinalIgnoreCase))
            {
                dataSavFiles.Add((virtualPath, DataSavFormat.ParseMemory(data)));
            }
            else if (Path.GetFileName(virtualPath).Equals("data2.sav", StringComparison.OrdinalIgnoreCase))
            {
                data2SavFiles.Add((virtualPath, Data2SavFormat.ParseMemory(data)));
            }
            else
            {
                rawFiles.Add((virtualPath, data));
            }
        }

        var maps = builders
            .Values.OrderBy(static b => b.MapPath, StringComparer.OrdinalIgnoreCase)
            .Select(static b => b.Build())
            .ToList();

        return new SaveGame
        {
            Info = info,
            EngineVersion = DetectEngineVersion(maps),
            Maps = maps,
            MessageFiles = messageFiles,
            TownMapFogs = townMapFogs,
            DataSavFiles = dataSavFiles,
            Data2SavFiles = data2SavFiles,
            RawFiles = rawFiles,
        };
    }

    /// <summary>
    /// Infers the engine version from object-file version fields found in the parsed maps.
    /// Returns <see cref="SaveEngineVersion.ArcanumCE"/> when any object in any map carries
    /// version <c>0x77</c>; otherwise returns <see cref="SaveEngineVersion.Vanilla"/>.
    /// </summary>
    private static SaveEngineVersion DetectEngineVersion(IReadOnlyList<SaveMapState> maps)
    {
        foreach (var map in maps)
        {
            // Check static object (.mob) headers.
            foreach (var (_, mob) in map.StaticObjects)
                if (mob.Header.Version == 0x77)
                    return SaveEngineVersion.ArcanumCE;

            // Check mobile.md diff records.
            if (map.StaticDiffs is { } md)
                foreach (var record in md.Records)
                    if (record.Version == 0x77)
                        return SaveEngineVersion.ArcanumCE;

            // Check mobile.mdy dynamic records.
            if (map.DynamicObjects is { } mdy)
                foreach (var record in mdy.Records)
                    if (record.IsMob && record.Mob!.Header.Version == 0x77)
                        return SaveEngineVersion.ArcanumCE;
        }

        return SaveEngineVersion.Vanilla;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="virtualPath"/> matches the pattern
    /// <c>modules/&lt;module&gt;/maps/&lt;mapname&gt;/…</c>. Sets <paramref name="mapPath"/>
    /// to the four-segment prefix and <paramref name="relPath"/> to the remainder.
    /// </summary>
    private static bool TryGetMapPath(string virtualPath, out string mapPath, out string relPath)
    {
        // Minimum: "modules/X/maps/Y/file" → 5 segments.
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

    // ── Builder ───────────────────────────────────────────────────────────────

    private sealed class MapStateBuilder
    {
        public string MapPath { get; }

        private readonly List<(string FileName, Sector Data)> _sectors = [];
        private readonly List<(string FileName, MobData Data)> _staticObjects = [];
        private MapProperties? _properties;
        private JmpFile? _jumpPoints;
        private MobileMdFile? _staticDiffs;
        private MobileMdyFile? _dynamicObjects;

        public MapStateBuilder(string mapPath) => MapPath = mapPath;

        public void Add(string relPath, byte[] data)
        {
            if (relPath.Equals("map.prp", StringComparison.OrdinalIgnoreCase))
            {
                _properties = MapPropertiesFormat.ParseMemory(data);
            }
            else if (relPath.Equals("map.jmp", StringComparison.OrdinalIgnoreCase))
            {
                _jumpPoints = JmpFormat.ParseMemory(data);
            }
            else if (relPath.Equals("mobile.md", StringComparison.OrdinalIgnoreCase))
            {
                _staticDiffs = MobileMdFormat.ParseMemory(data);
            }
            else if (relPath.Equals("mobile.mdy", StringComparison.OrdinalIgnoreCase))
            {
                _dynamicObjects = MobileMdyFormat.ParseMemory(data);
            }
            else if (relPath.EndsWith(".sec", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = relPath; // no sub-dir; already just the name
                _sectors.Add((fileName, SectorFormat.ParseMemory(data)));
            }
            else if (
                relPath.StartsWith("mobile/", StringComparison.OrdinalIgnoreCase)
                && relPath.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
            )
            {
                var fileName = relPath.Substring("mobile/".Length);
                _staticObjects.Add((fileName, MobFormat.ParseMemory(data)));
            }
            // Unknown sub-paths are silently ignored; they are not round-tripped.
        }

        public SaveMapState Build() =>
            new()
            {
                MapPath = MapPath,
                Properties = _properties,
                JumpPoints = _jumpPoints,
                Sectors = _sectors,
                StaticObjects = _staticObjects,
                StaticDiffs = _staticDiffs,
                DynamicObjects = _dynamicObjects,
            };
    }
}
