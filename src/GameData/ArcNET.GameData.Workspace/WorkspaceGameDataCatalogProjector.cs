using System.Globalization;

namespace ArcNET.GameData.Workspace;

public static class WorkspaceGameDataCatalogProjector
{
    public static IReadOnlyList<PrototypePaletteEntry> ToPrototypePaletteEntries(WorkspaceGameDataCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return
        [
            .. catalog.PrototypeEntries.Select(static entry => new PrototypePaletteEntry(
                entry.ProtoNumber,
                entry.ObjectType.ToString(),
                entry.AssetPath,
                entry.DisplayName,
                entry.Description,
                entry.PaletteGroup,
                entry.ArtAssetPath
            )),
        ];
    }

    public static IReadOnlyList<WorldMapCatalogEntry> ToWorldMapEntries(WorkspaceGameDataCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return
        [
            .. catalog.WorldAreaCatalog.Areas.Select(static area => new WorldMapCatalogEntry(
                area.AreaId,
                area.DisplayName,
                area.WorldX,
                area.WorldY,
                area.IsWorldMapVisible,
                area.Description,
                area.HasWorldCoordinates
                    ? $"World ({area.WorldX.ToString(CultureInfo.InvariantCulture)}, {area.WorldY.ToString(CultureInfo.InvariantCulture)})"
                    : "World coordinates unavailable",
                FormatMapSummary(area.MapEntries),
                [.. area.MapEntries.Select(static entry => entry.MapName)]
            )),
        ];
    }

    public static IReadOnlyList<TileArtCatalogEntry> ToTileArtEntries(WorkspaceGameDataCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return
        [
            .. catalog.TileArtEntries.Select(static entry => new TileArtCatalogEntry(
                entry.ArtId.Value,
                entry.ArtId.ToString(),
                entry.DisplayName,
                entry.ArtId.Type.ToString(),
                entry.ArtId.ArtNum,
                entry.ArtId.FrameIndex,
                entry.ArtId.PaletteIndex,
                entry.AssetPath,
                FormatTileArtSummary(entry)
            )),
        ];
    }

    public static IReadOnlyList<StaticObjectCatalogEntry> ToStaticObjectEntries(WorkspaceGameDataCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return
        [
            .. catalog.StaticObjectEntries.Select(static entry => new StaticObjectCatalogEntry(
                entry.SourceKindText,
                entry.DisplayName,
                entry.ObjectType.ToString(),
                entry.ObjectIdText,
                entry.ObjectGuidText,
                entry.ProtoNumber,
                entry.PrototypeText,
                entry.SourceAssetPath,
                entry.LocationText,
                entry.SummaryText
            )),
        ];
    }

    private static string FormatMapSummary(IReadOnlyList<WorkspaceWorldAreaMapEntry> mapEntries)
    {
        if (mapEntries.Count == 0)
            return "No linked local map anchors.";

        var preview = string.Join(
            ", ",
            mapEntries
                .Take(3)
                .Select(static entry =>
                    $"{entry.MapName} @ ({entry.EntryTileX.ToString(CultureInfo.InvariantCulture)}, {entry.EntryTileY.ToString(CultureInfo.InvariantCulture)})"
                )
        );
        return mapEntries.Count > 3
            ? $"{preview}, +{(mapEntries.Count - 3).ToString(CultureInfo.InvariantCulture)} more"
            : preview;
    }

    private static string FormatTileArtSummary(WorkspaceTileArtCatalogEntry entry)
    {
        var artId = entry.ArtId;
        return $"{artId.Type} art #{artId.ArtNum.ToString(CultureInfo.InvariantCulture)} - frame {artId.FrameIndex.ToString(CultureInfo.InvariantCulture)} - palette {artId.PaletteIndex.ToString(CultureInfo.InvariantCulture)}";
    }
}
