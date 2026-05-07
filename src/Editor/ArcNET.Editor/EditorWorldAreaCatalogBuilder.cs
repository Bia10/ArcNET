using System.Globalization;
using ArcNET.Formats;

namespace ArcNET.Editor;

internal static class EditorWorldAreaCatalogBuilder
{
    private const string GameAreaAssetPath = "mes/gamearea.mes";
    private const string TownMapAssetPath = "Rules/TownMap.mes";
    private const string MapListAssetPath = "Rules/MapList.mes";

    public static EditorWorldAreaCatalog Build(EditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var areasById = ParseGameAreas(workspace.FindMessageFile(GameAreaAssetPath));
        var townMapsById = ParseTownMap(workspace.FindMessageFile(TownMapAssetPath));
        var mapEntries = ParseMapList(workspace.FindMessageFile(MapListAssetPath));

        var mapEntriesByAreaId = mapEntries
            .Where(static entry => entry.AreaId is int)
            .GroupBy(static entry => entry.AreaId!.Value)
            .ToDictionary(
                static group => group.Key,
                static group =>
                    (IReadOnlyList<EditorWorldAreaMapEntry>)
                        [
                            .. group
                                .Select(static entry => entry.MapEntry)
                                .OrderBy(static entry => entry.MapName, StringComparer.OrdinalIgnoreCase),
                        ]
            );

        var worldSceneMapName = mapEntries
            .FirstOrDefault(static entry => string.Equals(entry.Type, "START_MAP", StringComparison.OrdinalIgnoreCase))
            ?.MapEntry.MapName;

        var areas = areasById
            .Keys.Union(townMapsById.Keys)
            .Union(mapEntriesByAreaId.Keys)
            .OrderBy(static areaId => areaId)
            .Select(areaId => CreateArea(areaId, areasById, townMapsById, mapEntriesByAreaId))
            .Where(static area => area is not null)
            .Cast<EditorWorldAreaEntry>()
            .OrderByDescending(static area => area.IsWorldMapVisible)
            .ThenBy(static area => area.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new EditorWorldAreaCatalog { WorldSceneMapName = worldSceneMapName, Areas = areas };
    }

    private static EditorWorldAreaEntry? CreateArea(
        int areaId,
        IReadOnlyDictionary<int, GameAreaEntry> areasById,
        IReadOnlyDictionary<int, TownMapEntry> townMapsById,
        IReadOnlyDictionary<int, IReadOnlyList<EditorWorldAreaMapEntry>> mapEntriesByAreaId
    )
    {
        areasById.TryGetValue(areaId, out var gameArea);
        townMapsById.TryGetValue(areaId, out var townMap);
        mapEntriesByAreaId.TryGetValue(areaId, out var mapEntries);

        if (gameArea is null || (gameArea.WorldX == 0 && gameArea.WorldY == 0))
            return null;

        var displayName = ResolveDisplayName(gameArea, townMap);
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        return new EditorWorldAreaEntry
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

        if (townMap is null || string.IsNullOrWhiteSpace(townMap.DisplayName))
            return gameArea.DisplayName;

        return townMap.IsWorldMapVisible || string.IsNullOrWhiteSpace(gameArea.DisplayName)
            ? townMap.DisplayName
            : gameArea.DisplayName;
    }

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
            if (!TryParseMapListEntry(entry, out var parsed))
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

    private static bool TryParseMapListEntry(MessageEntry entry, out MapListEntry parsed)
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
            new EditorWorldAreaMapEntry
            {
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

    private sealed record MapListEntry(int? AreaId, string? Type, EditorWorldAreaMapEntry MapEntry);
}
