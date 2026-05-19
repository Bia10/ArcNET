using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Builds the CE editor-only facade preview pass from one selected terrain facade and one tracked map selection.
/// </summary>
public static class EditorMapFacadePaintableSceneBuilder
{
    public static EditorMapPaintableScene? Build(
        EditorMapFloorRenderPreview sceneRender,
        EditorProjectMapSelectionState? selection,
        EditorTerrainPaletteEntry? terrainEntry,
        FacadeWalk? facadeWalk,
        IEditorMapRenderSpriteSource? spriteSource = null
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        if (
            selection is not { Tile: { } selectedTile, Area: null }
            || terrainEntry is not { ArtId.Type: ArtId.TypeCode.Facade }
            || facadeWalk is null
            || facadeWalk.Header.Width == 0
            || facadeWalk.Header.Height == 0
            || facadeWalk.Entries.Length == 0
        )
        {
            return null;
        }

        var overlayTiles = BuildOverlayTiles(sceneRender, selectedTile, terrainEntry.ArtId, facadeWalk);
        if (overlayTiles.Length == 0)
            return null;

        var renderQueue = new EditorMapRenderQueueItem[overlayTiles.Length];
        for (var index = 0; index < overlayTiles.Length; index++)
        {
            renderQueue[index] = new EditorMapRenderQueueItem
            {
                Kind = EditorMapRenderQueueItemKind.FloorTile,
                DrawOrder = index,
                SortKey = index * 4096d,
                CommittedRenderLayer = EditorMapCommittedRenderLayer.Ground,
                Tile = overlayTiles[index],
            };
        }

        var overlayPreview = new EditorMapFloorRenderPreview
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            TileWidthPixels = sceneRender.TileWidthPixels,
            TileHeightPixels = sceneRender.TileHeightPixels,
            WidthPixels = sceneRender.WidthPixels,
            HeightPixels = sceneRender.HeightPixels,
            Tiles = overlayTiles,
            Objects = [],
            ObjectAuxiliaryItems = [],
            Overlays = [],
            Lights = [],
            Roofs = [],
            RenderQueue = renderQueue,
            OffsetX = sceneRender.OffsetX,
            OffsetY = sceneRender.OffsetY,
            RawMinLeft = sceneRender.RawMinLeft,
            RawMinTop = sceneRender.RawMinTop,
            RawMaxRight = sceneRender.RawMaxRight,
            RawMaxBottom = sceneRender.RawMaxBottom,
        };

        return EditorMapPaintableSceneBuilder.Build(overlayPreview, spriteSource: spriteSource);
    }

    internal static string GetFacadeWalkAssetPath(ArtId artId)
    {
        if (artId.Type is not ArtId.TypeCode.Facade || artId.FacadeNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(artId), artId, "Facade walk paths require facade art.");

        return $"art/facade/facwalk.{artId.FacadeNumber:X2}";
    }

    private static EditorMapFloorTileRenderItem[] BuildOverlayTiles(
        EditorMapFloorRenderPreview sceneRender,
        Location selectedTile,
        ArtId baseArtId,
        FacadeWalk facadeWalk
    )
    {
        var topLeftMapTileX = selectedTile.X - ((int)facadeWalk.Header.Width / 2);
        var topLeftMapTileY = selectedTile.Y - ((int)facadeWalk.Header.Height / 2);
        var sortedEntries = facadeWalk
            .Entries.Select(
                (entry, frameIndex) =>
                    new FacadeEntryProjection(
                        FrameIndex: frameIndex,
                        MapTileX: checked(topLeftMapTileX + (int)entry.X),
                        MapTileY: checked(topLeftMapTileY + (int)entry.Y),
                        Tile: new Location(checked((short)entry.X), checked((short)entry.Y)),
                        IsBlocked: !entry.Walkable
                    )
            )
            .OrderBy(static entry => entry.MapTileX + entry.MapTileY)
            .ThenBy(static entry => entry.MapTileX)
            .ThenBy(static entry => entry.MapTileY)
            .ToArray();

        var overlayTiles = new EditorMapFloorTileRenderItem[sortedEntries.Length];
        for (var index = 0; index < sortedEntries.Length; index++)
        {
            var entry = sortedEntries[index];
            var (centerX, centerY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
                sceneRender.ViewMode,
                sceneRender.TileWidthPixels,
                sceneRender.TileHeightPixels,
                entry.MapTileX,
                entry.MapTileY
            );
            if (sceneRender.ViewMode is EditorMapSceneViewMode.Isometric)
                centerX += 1d;

            overlayTiles[index] = new EditorMapFloorTileRenderItem
            {
                SectorAssetPath = GetFacadeWalkAssetPath(baseArtId),
                MapTileX = entry.MapTileX,
                MapTileY = entry.MapTileY,
                Tile = entry.Tile,
                ArtId = baseArtId.WithFrameIndex(entry.FrameIndex),
                IsBlocked = entry.IsBlocked,
                HasLight = false,
                HasScript = false,
                DrawOrder = index,
                CenterX = centerX + sceneRender.OffsetX,
                CenterY = centerY + sceneRender.OffsetY,
            };
        }

        return overlayTiles;
    }

    private readonly record struct FacadeEntryProjection(
        int FrameIndex,
        int MapTileX,
        int MapTileY,
        Location Tile,
        bool IsBlocked
    );
}
