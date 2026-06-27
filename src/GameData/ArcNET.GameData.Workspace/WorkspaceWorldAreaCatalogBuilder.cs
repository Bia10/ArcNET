using System.Globalization;
using ArcNET.Formats;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Builds world-area anchors from loaded workspace message tables.
/// </summary>
public static class WorkspaceWorldAreaCatalogBuilder
{
    private const string GameAreaAssetPath = "mes/gamearea.mes";
    private const string TownMapAssetPath = "Rules/TownMap.mes";
    private const string MapListAssetPath = "Rules/MapList.mes";

    public static WorkspaceWorldAreaCatalog Build(GameDataStore gameData)
    {
        ArgumentNullException.ThrowIfNull(gameData);

        var areasById = ParseGameAreas(WorkspaceMessageLookup.FindMessageFile(gameData, GameAreaAssetPath));
        var townMapsById = ParseTownMap(WorkspaceMessageLookup.FindMessageFile(gameData, TownMapAssetPath));
        var mapEntries = ParseMapList(WorkspaceMessageLookup.FindMessageFile(gameData, MapListAssetPath));

        var mapEntriesByAreaId = mapEntries
            .Where(static entry => entry.AreaId is int)
            .GroupBy(static entry => entry.AreaId!.Value)
            .ToDictionary(
                static group => group.Key,
                static group =>
                    (IReadOnlyList<WorkspaceWorldAreaMapEntry>)
                        [
                            .. group
                                .Select(static entry => entry.MapEntry)
                                .OrderBy(static entry => entry.MapName, StringComparer.OrdinalIgnoreCase),
                        ]
            );

        var worldSceneMapName = mapEntries
            .FirstOrDefault(static entry => string.Equals(entry.Type, "START_MAP", StringComparison.OrdinalIgnoreCase))
            ?.MapEntry.MapName;

        var townMaps = townMapsById.Values.ToArray();

        var areas = areasById
            .Keys.Union(mapEntriesByAreaId.Keys)
            .OrderBy(static areaId => areaId)
            .Select(areaId => CreateArea(areaId, areasById, townMaps, mapEntriesByAreaId))
            .Where(static area => area is not null)
            .Cast<WorkspaceWorldAreaEntry>()
            .OrderByDescending(static area => area.IsWorldMapVisible)
            .ThenBy(static area => area.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new WorkspaceWorldAreaCatalog { WorldSceneMapName = worldSceneMapName, Areas = areas };
    }

    private static WorkspaceWorldAreaEntry? CreateArea(
        int areaId,
        IReadOnlyDictionary<int, GameAreaEntry> areasById,
        IReadOnlyList<TownMapEntry> townMaps,
        IReadOnlyDictionary<int, IReadOnlyList<WorkspaceWorldAreaMapEntry>> mapEntriesByAreaId
    )
    {
        areasById.TryGetValue(areaId, out var gameArea);
        mapEntriesByAreaId.TryGetValue(areaId, out var mapEntries);

        if (gameArea is null || (gameArea.WorldX == 0 && gameArea.WorldY == 0))
            return null;

        var townMap = townMaps.FirstOrDefault(tm => NamesMatch(gameArea.DisplayName, tm.DisplayName));

        var displayName = ResolveDisplayName(gameArea, townMap);
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        return new WorkspaceWorldAreaEntry
        {
            AreaId = areaId,
            DisplayName = displayName,
            WorldX = gameArea.WorldX,
            WorldY = gameArea.WorldY,
            LabelOffsetX = gameArea.LabelOffsetX,
            LabelOffsetY = gameArea.LabelOffsetY,
            Description = gameArea.Description,
            Radius = gameArea.Radius,
            IsWorldMapVisible = townMap?.IsWorldMapVisible ?? false,
            MapEntries = mapEntries ?? [],
        };
    }

    private static string ResolveDisplayName(GameAreaEntry gameArea, TownMapEntry? townMap)
    {
        ArgumentNullException.ThrowIfNull(gameArea);

        if (!string.IsNullOrWhiteSpace(gameArea.DisplayName))
            return gameArea.DisplayName;

        return townMap?.DisplayName ?? string.Empty;
    }

    private static bool NamesMatch(string name1, string name2) =>
        CleanName(name1).Contains(CleanName(name2), StringComparison.Ordinal)
        || CleanName(name2).Contains(CleanName(name1), StringComparison.Ordinal);

    private static string CleanName(string name) => new([.. name.ToLowerInvariant().Where(char.IsLetterOrDigit)]);

    private static Dictionary<int, GameAreaEntry> ParseGameAreas(MesFile? file)
    {
        Dictionary<int, GameAreaEntry> areas = [];
        if (file is null)
            return areas;

        foreach (var entry in file.Entries)
        {
            if (!TryParseGameAreaEntry(entry, out var parsed))
                continue;

            areas[entry.Index] = parsed;
        }

        return areas;
    }

    private static Dictionary<int, TownMapEntry> ParseTownMap(MesFile? file)
    {
        Dictionary<int, TownMapEntry> areas = [];
        if (file is null)
            return areas;

        foreach (var entry in file.Entries)
        {
            if (!TryParseTownMapEntry(entry, out var parsed))
                continue;

            areas[entry.Index] = parsed;
        }

        return areas;
    }

    private static IReadOnlyList<MapListEntry> ParseMapList(MesFile? file)
    {
        if (file is null)
            return [];

        List<MapListEntry> entries = [];
        foreach (var entry in file.Entries)
        {
            var mapId = entries.Count + 1;
            if (!TryParseMapListEntry(entry, mapId, out var parsed))
                continue;

            entries.Add(parsed);
        }

        return entries;
    }

    private static bool TryParseGameAreaEntry(MessageEntry entry, out GameAreaEntry parsed)
    {
        parsed = null!;

        var segments = entry.Text.Split('/', StringSplitOptions.None);
        if (segments.Length < 3)
            return false;

        var coordinates = segments[0]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (coordinates.Length < 4)
            return false;

        if (
            !int.TryParse(coordinates[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var worldX)
            || !int.TryParse(coordinates[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var worldY)
            || !int.TryParse(coordinates[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var labelOffsetX)
            || !int.TryParse(coordinates[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var labelOffsetY)
        )
            return false;

        int? radius = null;
        for (var segmentIndex = 3; segmentIndex < segments.Length; segmentIndex++)
        {
            var segment = segments[segmentIndex].Trim();
            if (
                segment.StartsWith("Radius:", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(
                    segment["Radius:".Length..],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedRadius
                )
            )
            {
                radius = parsedRadius;
                break;
            }
        }

        parsed = new GameAreaEntry(
            worldX,
            worldY,
            labelOffsetX,
            labelOffsetY,
            segments[1].Trim(),
            segments[2].Trim(),
            radius
        );
        return true;
    }

    private static bool TryParseTownMapEntry(MessageEntry entry, out TownMapEntry parsed)
    {
        parsed = null!;

        var text = entry.Text.Trim();
        if (text.Length == 0)
            return false;

        var isWorldMapVisible = text.Contains("[w:1]", StringComparison.OrdinalIgnoreCase);
        parsed = new TownMapEntry(
            text.Replace("[w:1]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(),
            isWorldMapVisible
        );
        return parsed.DisplayName.Length > 0;
    }

    private static bool TryParseMapListEntry(MessageEntry entry, int mapId, out MapListEntry parsed)
    {
        parsed = null!;

        var segments = entry.Text.Split(',', StringSplitOptions.TrimEntries);
        if (segments.Length < 3)
            return false;

        var mapName = segments[0].Trim();
        if (mapName.Length == 0)
            return false;

        if (
            !int.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryTileX)
            || !int.TryParse(segments[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryTileY)
        )
            return false;

        int? areaId = null;
        int? worldMapId = null;
        string? type = null;

        for (var segmentIndex = 3; segmentIndex < segments.Length; segmentIndex++)
        {
            var segment = segments[segmentIndex].Trim();
            var separatorIndex = segment.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
                continue;

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            switch (key)
            {
                case "Area"
                    when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAreaId):
                    areaId = parsedAreaId;
                    break;
                case "WorldMap"
                    when int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var parsedWorldMapId
                    ):
                    worldMapId = parsedWorldMapId;
                    break;
                case "Type":
                    type = value;
                    break;
            }
        }

        parsed = new MapListEntry(
            areaId,
            type,
            new WorkspaceWorldAreaMapEntry
            {
                MapId = mapId,
                MapName = mapName,
                EntryTileX = entryTileX,
                EntryTileY = entryTileY,
                WorldMapId = worldMapId,
                Type = type,
            }
        );
        return true;
    }

    private sealed record GameAreaEntry(
        int WorldX,
        int WorldY,
        int LabelOffsetX,
        int LabelOffsetY,
        string DisplayName,
        string Description,
        int? Radius
    );

    private sealed record TownMapEntry(string DisplayName, bool IsWorldMapVisible);

    private sealed record MapListEntry(int? AreaId, string? Type, WorkspaceWorldAreaMapEntry MapEntry);
}
