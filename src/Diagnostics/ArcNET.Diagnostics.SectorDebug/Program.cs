using System.Numerics;
using ArcNET.Editor;
using ArcNET.Formats;

const string DefaultGameRoot = @"C:\Games\Arcanum\ArcanumCleanUAPnohighres - Copy";
const int DefaultTop = 10;
const int SampleLimit = 3;

var gameRoot = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0].Trim() : DefaultGameRoot;
var mapFilter = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1].Trim() : null;
var top = args.Length > 2 && int.TryParse(args[2], out var parsedTop) ? Math.Max(parsedTop, 0) : DefaultTop;

Console.WriteLine($"Loading editor workspace from '{gameRoot}'...");
var workspace = await EditorWorkspaceLoader.LoadFromGameInstallAsync(gameRoot);

var candidateMapNames = workspace
    .Index.MapNames.Where(mapName =>
        mapFilter is null || mapName.Contains(mapFilter, StringComparison.OrdinalIgnoreCase)
    )
    .OrderBy(mapName => mapName, StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (candidateMapNames.Length == 0)
{
    Console.WriteLine($"No maps matched filter '{mapFilter ?? "*"}'.");
    return;
}

Console.WriteLine($"Analyzing {candidateMapNames.Length} map(s)...");

var mapReports = new List<MapSpecialTileReport>(candidateMapNames.Length);
var sectorReports = new List<SectorSpecialTileReport>();
var mapErrors = new List<string>();

for (var mapIndex = 0; mapIndex < candidateMapNames.Length; mapIndex++)
{
    if (mapIndex > 0 && mapIndex % 25 == 0)
        Console.WriteLine($"  Progress: {mapIndex}/{candidateMapNames.Length} maps");

    var mapName = candidateMapNames[mapIndex];
    try
    {
        var preview = workspace.CreateMapScenePreview(mapName);
        var mapSceneBlockedTiles = 0;
        var mapRawBlockedTiles = 0;
        var mapTerrainOnlyBlockedTiles = 0;
        var mapTileScriptCount = 0;
        var mapJumpCount = 0;
        var jumpSamples = new List<string>(SampleLimit);
        var terrainSectorSamples = new List<string>(SampleLimit);

        foreach (
            var sector in preview.Sectors.OrderBy(static sector => sector.AssetPath, StringComparer.OrdinalIgnoreCase)
        )
        {
            var rawSector = workspace.FindSector(sector.AssetPath);
            var sceneBlockedTiles = CountBlockedTiles(sector.BlockMask);
            var rawMapBlockedTiles = rawSector is null ? sceneBlockedTiles : CountBlockedTiles(rawSector.BlockMask);
            var terrainOnlyBlockedTiles = Math.Max(0, sceneBlockedTiles - rawMapBlockedTiles);
            var tileScriptCount = sector.TileScripts.Count;
            var jumpCount = sector.JumpPoints.Count;

            mapSceneBlockedTiles += sceneBlockedTiles;
            mapRawBlockedTiles += rawMapBlockedTiles;
            mapTerrainOnlyBlockedTiles += terrainOnlyBlockedTiles;
            mapTileScriptCount += tileScriptCount;
            mapJumpCount += jumpCount;

            if (jumpSamples.Count < SampleLimit)
            {
                foreach (var jumpSample in CollectJumpSamples(sector, SampleLimit - jumpSamples.Count))
                {
                    jumpSamples.Add(jumpSample);
                    if (jumpSamples.Count == SampleLimit)
                        break;
                }
            }

            if (terrainSectorSamples.Count < SampleLimit && terrainOnlyBlockedTiles > 0)
                terrainSectorSamples.Add($"{sector.AssetPath}:{terrainOnlyBlockedTiles}");

            if (sceneBlockedTiles == 0 && tileScriptCount == 0 && jumpCount == 0)
                continue;

            sectorReports.Add(
                new SectorSpecialTileReport(
                    mapName,
                    sector.AssetPath,
                    sceneBlockedTiles,
                    rawMapBlockedTiles,
                    terrainOnlyBlockedTiles,
                    tileScriptCount,
                    jumpCount,
                    [.. CollectBlockedSamples(sector, rawSector, SampleLimit)],
                    [.. CollectTileScriptSamples(sector, SampleLimit)],
                    [.. CollectJumpSamples(sector, SampleLimit)]
                )
            );
        }

        mapReports.Add(
            new MapSpecialTileReport(
                mapName,
                preview.Sectors.Count,
                mapSceneBlockedTiles,
                mapRawBlockedTiles,
                mapTerrainOnlyBlockedTiles,
                mapTileScriptCount,
                mapJumpCount,
                [.. jumpSamples],
                [.. terrainSectorSamples]
            )
        );
    }
    catch (Exception ex)
    {
        mapErrors.Add($"{mapName}: {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(
    $"SUMMARY|maps={candidateMapNames.Length}|sceneBlockedMaps={mapReports.Count(static report => report.SceneBlockedTiles > 0)}|scriptedMaps={mapReports.Count(static report => report.TileScriptCount > 0)}|jumpMaps={mapReports.Count(static report => report.JumpCount > 0)}|terrainAugmentedMaps={mapReports.Count(static report => report.TerrainOnlyBlockedTiles > 0)}|failures={mapErrors.Count}"
);

WriteMapSection(
    "MAP",
    mapReports.Where(static report =>
        report.SceneBlockedTiles > 0 || report.TileScriptCount > 0 || report.JumpCount > 0
    ),
    top,
    report =>
        $"MAP|{report.MapName}|sectors={report.SectorCount}|sceneBlocked={report.SceneBlockedTiles}|rawMapBlocked={report.RawMapBlockedTiles}|terrainOnlyBlocked={report.TerrainOnlyBlockedTiles}|tileScripts={report.TileScriptCount}|jumps={report.JumpCount}|jumpSamples={FormatSamples(report.JumpSamples)}"
);

WriteSectorSection(
    "BLOCKED-SECTOR",
    sectorReports.Where(static report => report.SceneBlockedTiles > 0),
    top,
    report =>
        $"BLOCKED-SECTOR|map={report.MapName}|asset={report.AssetPath}|sceneBlocked={report.SceneBlockedTiles}|rawMapBlocked={report.RawMapBlockedTiles}|terrainOnlyBlocked={report.TerrainOnlyBlockedTiles}|samples={FormatSamples(report.BlockedSamples)}",
    static report => report.SceneBlockedTiles
);

WriteSectorSection(
    "SCRIPT-SECTOR",
    sectorReports.Where(static report => report.TileScriptCount > 0),
    top,
    report =>
        $"SCRIPT-SECTOR|map={report.MapName}|asset={report.AssetPath}|tileScripts={report.TileScriptCount}|samples={FormatSamples(report.TileScriptSamples)}",
    static report => report.TileScriptCount
);

WriteMapSection(
    "JUMP-MAP",
    mapReports.Where(static report => report.JumpCount > 0),
    top,
    report => $"JUMP-MAP|map={report.MapName}|jumps={report.JumpCount}|samples={FormatSamples(report.JumpSamples)}",
    static report => report.JumpCount
);

WriteMapSection(
    "TERRAIN-BLOCKED-MAP",
    mapReports.Where(static report => report.TerrainOnlyBlockedTiles > 0),
    top,
    report =>
        $"TERRAIN-BLOCKED-MAP|map={report.MapName}|sceneBlocked={report.SceneBlockedTiles}|rawMapBlocked={report.RawMapBlockedTiles}|terrainOnlyBlocked={report.TerrainOnlyBlockedTiles}|sampleSectors={FormatSamples(report.TerrainSectorSamples)}",
    static report => report.TerrainOnlyBlockedTiles
);

if (mapErrors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("FAILURES");
    foreach (var mapError in mapErrors.Take(top))
        Console.WriteLine($"ERROR|{mapError}");
}

static int CountBlockedTiles(uint[] blockMask)
{
    var count = 0;
    for (var index = 0; index < blockMask.Length; index++)
        count += BitOperations.PopCount(blockMask[index]);

    return count;
}

static IEnumerable<string> CollectBlockedSamples(EditorMapSectorScenePreview sector, Sector? rawSector, int limit)
{
    var produced = 0;
    for (var tileY = 0; tileY < sector.TileHeight; tileY++)
    {
        for (var tileX = 0; tileX < sector.TileWidth; tileX++)
        {
            if (!sector.IsTileBlocked(tileX, tileY))
                continue;

            var source = rawSector is not null && rawSector.BlockMask.IsBlocked(tileX, tileY) ? "map" : "terrain";
            yield return $"({tileX},{tileY})[{source}]";

            produced++;
            if (produced == limit)
                yield break;
        }
    }
}

static IEnumerable<string> CollectTileScriptSamples(EditorMapSectorScenePreview sector, int limit)
{
    for (var index = 0; index < Math.Min(limit, sector.TileScripts.Count); index++)
    {
        var tileScript = sector.TileScripts[index];
        yield return $"({tileScript.TileX},{tileScript.TileY})->script:{tileScript.ScriptId}";
    }
}

static IEnumerable<string> CollectJumpSamples(EditorMapSectorScenePreview sector, int limit)
{
    for (var index = 0; index < Math.Min(limit, sector.JumpPoints.Count); index++)
    {
        var jumpPoint = sector.JumpPoints[index];
        yield return $"{jumpPoint.MapTileX},{jumpPoint.MapTileY}->{jumpPoint.DestinationMapId}:{jumpPoint.DestinationTileX},{jumpPoint.DestinationTileY}";
    }
}

static string FormatSamples(IReadOnlyList<string> samples) => samples.Count == 0 ? "-" : string.Join(',', samples);

static void WriteSectorSection(
    string title,
    IEnumerable<SectorSpecialTileReport> reports,
    int top,
    Func<SectorSpecialTileReport, string> formatter,
    Func<SectorSpecialTileReport, int> orderBy
)
{
    Console.WriteLine();
    Console.WriteLine(title);

    foreach (
        var report in reports
            .OrderByDescending(orderBy)
            .ThenBy(static report => report.MapName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static report => report.AssetPath, StringComparer.OrdinalIgnoreCase)
            .Take(top)
    )
    {
        Console.WriteLine(formatter(report));
    }
}

static void WriteMapSection(
    string title,
    IEnumerable<MapSpecialTileReport> reports,
    int top,
    Func<MapSpecialTileReport, string> formatter,
    Func<MapSpecialTileReport, int>? orderBy = null
)
{
    Console.WriteLine();
    Console.WriteLine(title);

    var orderedReports = orderBy is null
        ? reports.OrderBy(static report => report.MapName, StringComparer.OrdinalIgnoreCase)
        : reports.OrderByDescending(orderBy).ThenBy(static report => report.MapName, StringComparer.OrdinalIgnoreCase);

    foreach (var report in orderedReports.Take(top))
        Console.WriteLine(formatter(report));
}

sealed record MapSpecialTileReport(
    string MapName,
    int SectorCount,
    int SceneBlockedTiles,
    int RawMapBlockedTiles,
    int TerrainOnlyBlockedTiles,
    int TileScriptCount,
    int JumpCount,
    IReadOnlyList<string> JumpSamples,
    IReadOnlyList<string> TerrainSectorSamples
);

sealed record SectorSpecialTileReport(
    string MapName,
    string AssetPath,
    int SceneBlockedTiles,
    int RawMapBlockedTiles,
    int TerrainOnlyBlockedTiles,
    int TileScriptCount,
    int JumpCount,
    IReadOnlyList<string> BlockedSamples,
    IReadOnlyList<string> TileScriptSamples,
    IReadOnlyList<string> JumpSamples
);
