using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor;

internal sealed class EditorMapVirtualTerrainPaintableSceneSource(
    EditorMapFloorRenderPreview sceneRender,
    IEditorMapRenderSpriteSource? spriteSource,
    EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
)
{
    private const double FloorLayerSortBase = -2_000_000_000d;
    private const double OverlayLayerSortBase = -1_000_000_000d;
    private const double RoofLayerSortBase = 800_000_000d;
    private const double LightLayerSortBase = 900_000_000d;
    private const double DrawOrderSortScale = 1_000_000d;
    private const double LightSampleRadius = 300d;

    public bool HasItems => sceneRender.VirtualTerrainSectors.Count > 0;

    private sealed class LightSpatialIndex
    {
        private const double CellSize = LightSampleRadius;
        private static readonly IReadOnlyList<EditorMapLightRenderItem> EmptyLights = [];
        private readonly Dictionary<(int X, int Y), List<EditorMapLightRenderItem>> _cells = [];

        public LightSpatialIndex(
            IReadOnlyList<EditorMapLightRenderItem> sceneLights,
            IReadOnlyList<EditorMapLightRenderItem> virtualLights
        )
        {
            Add(sceneLights);
            Add(virtualLights);
        }

        public IReadOnlyList<EditorMapLightRenderItem> Query(double centerX, double centerY, double radius)
        {
            if (_cells.Count == 0)
                return EmptyLights;

            var minCellX = GetCellCoordinate(centerX - radius);
            var maxCellX = GetCellCoordinate(centerX + radius);
            var minCellY = GetCellCoordinate(centerY - radius);
            var maxCellY = GetCellCoordinate(centerY + radius);
            var radiusSquared = radius * radius;
            List<EditorMapLightRenderItem>? matches = null;
            for (var cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                for (var cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    if (!_cells.TryGetValue((cellX, cellY), out var bucket))
                        continue;

                    for (var index = 0; index < bucket.Count; index++)
                    {
                        var light = bucket[index];
                        var dx = centerX - light.AnchorX;
                        var dy = centerY - light.AnchorY;
                        if ((dx * dx) + (dy * dy) >= radiusSquared)
                            continue;

                        (matches ??= []).Add(light);
                    }
                }
            }

            return matches ?? EmptyLights;
        }

        private void Add(IReadOnlyList<EditorMapLightRenderItem> lights)
        {
            for (var index = 0; index < lights.Count; index++)
            {
                var light = lights[index];
                if (light.Flags.HasFlag(SectorLightFlags.Off))
                    continue;

                var cell = GetCell(light.AnchorX, light.AnchorY);
                if (!_cells.TryGetValue(cell, out var bucket))
                {
                    bucket = [];
                    _cells[cell] = bucket;
                }

                bucket.Add(light);
            }
        }

        private static (int X, int Y) GetCell(double x, double y) => (GetCellCoordinate(x), GetCellCoordinate(y));

        private static int GetCellCoordinate(double value) => (int)Math.Floor(value / CellSize);
    }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(EditorMapSceneViewportLayout viewport)
    {
        if (!HasItems)
            yield break;

        var queue = new List<EditorMapRenderQueueItem>();
        IReadOnlyList<EditorMapLightRenderItem> virtualLights = sceneRender.IncludeFloorLightTint
            ? CreateVisibleVirtualLights(viewport)
            : [];
        var lightSpatialIndex = sceneRender.IncludeFloorLightTint
            ? new LightSpatialIndex(sceneRender.Lights, virtualLights)
            : null;

        for (var sectorIndex = 0; sectorIndex < sceneRender.VirtualTerrainSectors.Count; sectorIndex++)
        {
            var sector = sceneRender.VirtualTerrainSectors[sectorIndex];
            if (
                sceneRender.IsTerrainSectorMaterialized(sector.AssetPath)
                || !sceneRender.GetVirtualTerrainSectorBounds(sector).Intersects(viewport)
            )
            {
                continue;
            }

            AddVisibleFloorTiles(sector, viewport, virtualLights, lightSpatialIndex, queue);
            AddVisibleRoofs(sector, viewport, queue);
            AddVisibleSectorLights(sector, viewport, queue);
        }

        if (queue.Count == 0)
            yield break;

        queue.Sort(CompareQueueItems);
        for (var index = 0; index < queue.Count; index++)
        {
            var item = EditorMapPaintableSceneBuilder.BuildItem(
                sceneRender,
                queue[index],
                spriteSource,
                spriteReferenceCache
            );
            if (EditorMapPaintableScene.IntersectsViewport(item, viewport))
                yield return item;
        }
    }

    private void AddVisibleFloorTiles(
        EditorMapSectorScenePreview sector,
        EditorMapSceneViewportLayout viewport,
        IReadOnlyList<EditorMapLightRenderItem> virtualLights,
        LightSpatialIndex? lightSpatialIndex,
        List<EditorMapRenderQueueItem> queue
    )
    {
        var lightTileIndices =
            sceneRender.IncludeTerrainLightOverlays || sceneRender.IncludeFloorLightTint ? sector.LightTileIndices : [];
        var scriptedTileIndices = sceneRender.IncludeTerrainScriptOverlays ? sector.ScriptedTileIndices : [];
        var jumpPointTileIndices = sceneRender.IncludeTerrainJumpPointOverlays ? sector.JumpPointTileIndices : [];
        var tileRowMasks = sector.TileRowMasks;

        for (var tileY = 0; tileY < sector.TileHeight; tileY++)
        {
            var rowMask = tileRowMasks[tileY];
            if (rowMask == 0 && !sceneRender.IncludeEmptyTerrainTiles)
                continue;

            if (sceneRender.IncludeEmptyTerrainTiles)
            {
                for (var tileX = 0; tileX < sector.TileWidth; tileX++)
                    AddVisibleFloorTile(
                        sector,
                        tileX,
                        tileY,
                        viewport,
                        lightTileIndices,
                        scriptedTileIndices,
                        jumpPointTileIndices,
                        virtualLights,
                        lightSpatialIndex,
                        queue
                    );
                continue;
            }

            var remaining = rowMask;
            while (remaining != 0)
            {
                var tileX = System.Numerics.BitOperations.TrailingZeroCount(remaining);
                AddVisibleFloorTile(
                    sector,
                    tileX,
                    tileY,
                    viewport,
                    lightTileIndices,
                    scriptedTileIndices,
                    jumpPointTileIndices,
                    virtualLights,
                    lightSpatialIndex,
                    queue
                );
                remaining &= remaining - 1;
            }
        }
    }

    private void AddVisibleFloorTile(
        EditorMapSectorScenePreview sector,
        int tileX,
        int tileY,
        EditorMapSceneViewportLayout viewport,
        HashSet<int> lightTileIndices,
        HashSet<int> scriptedTileIndices,
        HashSet<int> jumpPointTileIndices,
        IReadOnlyList<EditorMapLightRenderItem> virtualLights,
        LightSpatialIndex? lightSpatialIndex,
        List<EditorMapRenderQueueItem> queue
    )
    {
        var tileArtId = sector.GetTileArtId(tileX, tileY);
        if (!sceneRender.IncludeEmptyTerrainTiles && tileArtId == 0)
            return;

        var mapTileX = checked((sector.LocalX * sector.TileWidth) + tileX);
        var mapTileY = checked((sector.LocalY * sector.TileHeight) + tileY);
        var (centerX, centerY) = ProjectTileCenter(mapTileX, mapTileY);
        if (!IntersectsTile(centerX, centerY, viewport))
            return;

        var tileIndex = checked((tileY * sector.TileWidth) + tileX);
        var tileDrawOrder = EditorMapFloorRenderBuilder.GetDrawOrder(sceneRender.ViewMode, 0, mapTileX, mapTileY);
        var tileSortOrdinal = GetSortOrdinal(tileDrawOrder);
        var drawOrder = EditorMapFloorRenderPreview.CreateVirtualTerrainDrawOrder(tileDrawOrder);
        var artId = new ArtId(tileArtId);
        var (suggestedTintColor, diagnostics) = sceneRender.IncludeFloorLightTint
            ? CreateLightDiagnostics(centerX, centerY, artId, sector, virtualLights, lightSpatialIndex)
            : (null, null);
        var tile = new EditorMapFloorTileRenderItem
        {
            SectorAssetPath = sector.AssetPath,
            MapTileX = mapTileX,
            MapTileY = mapTileY,
            Tile = new Location(checked((short)tileX), checked((short)tileY)),
            ArtId = artId,
            IsBlocked = sector.IsTileBlocked(tileX, tileY),
            HasLight = lightTileIndices.Contains(tileIndex),
            HasScript = scriptedTileIndices.Contains(tileIndex),
            DrawOrder = drawOrder,
            CenterX = centerX,
            CenterY = centerY,
            SuggestedTintColor = suggestedTintColor,
            LightDiagnostics = diagnostics,
        };

        queue.Add(
            new EditorMapRenderQueueItem
            {
                Kind = EditorMapRenderQueueItemKind.FloorTile,
                DrawOrder = drawOrder,
                SortKey = FloorLayerSortBase + tileSortOrdinal,
                Tile = tile,
            }
        );

        AddVisibleTileOverlays(
            sector,
            tile,
            tileIndex,
            tileSortOrdinal,
            lightTileIndices,
            scriptedTileIndices,
            jumpPointTileIndices,
            queue
        );
    }

    private void AddVisibleTileOverlays(
        EditorMapSectorScenePreview sector,
        EditorMapFloorTileRenderItem tile,
        int tileIndex,
        double tileSortOrdinal,
        HashSet<int> lightTileIndices,
        HashSet<int> scriptedTileIndices,
        HashSet<int> jumpPointTileIndices,
        List<EditorMapRenderQueueItem> queue
    )
    {
        if (sceneRender.IncludeTerrainBlockedTileOverlays && tile.IsBlocked)
            AddOverlay(sector.AssetPath, tile, EditorMapTileOverlayKind.BlockedTile, tileSortOrdinal, queue);

        if (sceneRender.IncludeTerrainLightOverlays && lightTileIndices.Contains(tileIndex))
            AddOverlay(sector.AssetPath, tile, EditorMapTileOverlayKind.Light, tileSortOrdinal, queue);

        if (sceneRender.IncludeTerrainScriptOverlays && scriptedTileIndices.Contains(tileIndex))
            AddOverlay(sector.AssetPath, tile, EditorMapTileOverlayKind.Script, tileSortOrdinal, queue);

        if (sceneRender.IncludeTerrainJumpPointOverlays && jumpPointTileIndices.Contains(tileIndex))
            AddOverlay(sector.AssetPath, tile, EditorMapTileOverlayKind.JumpPoint, tileSortOrdinal, queue);
    }

    private static void AddOverlay(
        string sectorAssetPath,
        EditorMapFloorTileRenderItem tile,
        EditorMapTileOverlayKind kind,
        double tileSortOrdinal,
        List<EditorMapRenderQueueItem> queue
    )
    {
        var overlay = new EditorMapTileOverlayRenderItem
        {
            SectorAssetPath = sectorAssetPath,
            MapTileX = tile.MapTileX,
            MapTileY = tile.MapTileY,
            Tile = tile.Tile,
            Kind = kind,
            DrawOrder = tile.DrawOrder,
            CenterX = tile.CenterX,
            CenterY = tile.CenterY,
            SuggestedOpacity = EditorMapFloorRenderBuilder.GetTileOverlaySuggestedOpacity(kind),
            SuggestedTintColor = EditorMapFloorRenderBuilder.GetTileOverlaySuggestedTintColor(kind),
        };
        queue.Add(
            new EditorMapRenderQueueItem
            {
                Kind = EditorMapRenderQueueItemKind.TileOverlay,
                DrawOrder = tile.DrawOrder,
                SortKey = OverlayLayerSortBase + (tileSortOrdinal * 4d) + (int)kind,
                TileOverlay = overlay,
            }
        );
    }

    private void AddVisibleRoofs(
        EditorMapSectorScenePreview sector,
        EditorMapSceneViewportLayout viewport,
        List<EditorMapRenderQueueItem> queue
    )
    {
        if (!sceneRender.IncludeTerrainRoofs || sector.RoofArtIds is null)
            return;

        var roofRowMasks = sector.RoofRowMasks;
        for (var roofY = 0; roofY < sector.RoofHeight; roofY++)
        {
            var rowMask = roofRowMasks?[roofY] ?? 0UL;
            if (roofRowMasks is not null && rowMask == 0)
                continue;

            if (roofRowMasks is null)
            {
                for (var roofX = 0; roofX < sector.RoofWidth; roofX++)
                    AddVisibleRoof(sector, roofX, roofY, viewport, queue);
                continue;
            }

            var remaining = rowMask;
            while (remaining != 0)
            {
                var roofX = System.Numerics.BitOperations.TrailingZeroCount(remaining);
                AddVisibleRoof(sector, roofX, roofY, viewport, queue);
                remaining &= remaining - 1;
            }
        }
    }

    private void AddVisibleRoof(
        EditorMapSectorScenePreview sector,
        int roofX,
        int roofY,
        EditorMapSceneViewportLayout viewport,
        List<EditorMapRenderQueueItem> queue
    )
    {
        var roofArtId = sector.GetRoofArtId(roofX, roofY);
        if (roofArtId is null or 0u or uint.MaxValue)
            return;

        var artId = new ArtId(roofArtId.Value);
        if (artId.IsRoofFill || artId.IsRoofFaded)
            return;

        var mapTileX = checked((sector.LocalX * sector.TileWidth) + (roofX * 4));
        var mapTileY = checked((sector.LocalY * sector.TileHeight) + (roofY * 4));
        var (anchorX, anchorY) = EditorMapFloorRenderBuilder.ProjectRoofAnchor(
            sceneRender.ViewMode,
            sceneRender.TileWidthPixels,
            sceneRender.TileHeightPixels,
            mapTileX,
            mapTileY
        );
        anchorX += sceneRender.OffsetX;
        anchorY += sceneRender.OffsetY;

        if (
            !IntersectsAnchor(
                anchorX,
                anchorY,
                sceneRender.TileWidthPixels * 4d,
                sceneRender.TileHeightPixels * 8d,
                viewport
            )
        )
            return;

        var sortMapTileX = mapTileX + 3;
        var sortMapTileY = mapTileY + 3;
        var roofDrawOrder = EditorMapFloorRenderBuilder.GetDrawOrder(
            sceneRender.ViewMode,
            0,
            sortMapTileX,
            sortMapTileY
        );
        var drawOrder = EditorMapFloorRenderPreview.CreateVirtualTerrainDrawOrder(roofDrawOrder);
        var roof = new EditorMapRoofRenderItem
        {
            SectorAssetPath = sector.AssetPath,
            RoofCell = new Location(checked((short)roofX), checked((short)roofY)),
            MapTileX = mapTileX,
            MapTileY = mapTileY,
            ArtId = artId,
            DrawOrder = drawOrder,
            AnchorX = anchorX,
            AnchorY = anchorY,
        };
        queue.Add(
            new EditorMapRenderQueueItem
            {
                Kind = EditorMapRenderQueueItemKind.Roof,
                DrawOrder = drawOrder,
                SortKey = RoofLayerSortBase + GetSortOrdinal(roofDrawOrder),
                Roof = roof,
            }
        );
    }

    private void AddVisibleSectorLights(
        EditorMapSectorScenePreview sector,
        EditorMapSceneViewportLayout viewport,
        List<EditorMapRenderQueueItem> queue
    )
    {
        if (!sceneRender.IncludeTerrainLightOverlays)
            return;

        for (var index = 0; index < sector.Lights.Count; index++)
        {
            var light = CreateSectorLight(sector, sector.Lights[index]);
            if (
                !IntersectsAnchor(
                    light.AnchorX,
                    light.AnchorY,
                    sceneRender.TileWidthPixels * 6d,
                    sceneRender.TileHeightPixels * 6d,
                    viewport
                )
            )
                continue;

            var lightDrawOrder = EditorMapFloorRenderBuilder.GetDrawOrder(
                sceneRender.ViewMode,
                0,
                light.MapTileX,
                light.MapTileY
            );
            queue.Add(
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Light,
                    DrawOrder = EditorMapFloorRenderPreview.CreateVirtualTerrainDrawOrder(lightDrawOrder),
                    SortKey = LightLayerSortBase + GetSortOrdinal(lightDrawOrder),
                    Light = light,
                }
            );
        }
    }

    private IReadOnlyList<EditorMapLightRenderItem> CreateVisibleVirtualLights(EditorMapSceneViewportLayout viewport)
    {
        List<EditorMapLightRenderItem>? virtualLights = null;
        for (var sectorIndex = 0; sectorIndex < sceneRender.VirtualTerrainSectors.Count; sectorIndex++)
        {
            var sector = sceneRender.VirtualTerrainSectors[sectorIndex];
            if (
                sceneRender.IsTerrainSectorMaterialized(sector.AssetPath)
                || !sceneRender.GetVirtualTerrainSectorBounds(sector).Intersects(viewport)
            )
            {
                continue;
            }

            for (var lightIndex = 0; lightIndex < sector.Lights.Count; lightIndex++)
                (virtualLights ??= []).Add(CreateSectorLight(sector, sector.Lights[lightIndex]));
        }

        return virtualLights ?? [];
    }

    private EditorMapLightRenderItem CreateSectorLight(
        EditorMapSectorScenePreview sector,
        EditorMapLightPreview lightPreview
    )
    {
        var localTileX = lightPreview.TileX % sector.TileWidth;
        var localTileY = lightPreview.TileY % sector.TileHeight;
        var mapTileX = checked((sector.LocalX * sector.TileWidth) + localTileX);
        var mapTileY = checked((sector.LocalY * sector.TileHeight) + localTileY);
        var (tileCenterX, tileCenterY) = ProjectTileCenter(mapTileX, mapTileY);
        var scaleX = sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? sceneRender.TileWidthPixels / 80d : 1d;
        var scaleY = sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? sceneRender.TileHeightPixels / 40d : 1d;
        var sectorAmbientColors = ResolveSectorAmbientLightColors(sector.LightSchemeIdx, sceneRender.AmbientLighting);

        return new EditorMapLightRenderItem
        {
            SectorAssetPath = sector.AssetPath,
            MapTileX = mapTileX,
            MapTileY = mapTileY,
            Tile = new Location(checked((short)localTileX), checked((short)localTileY)),
            ArtId = lightPreview.ArtId,
            DrawOrder = EditorMapFloorRenderPreview.CreateVirtualTerrainDrawOrder(
                EditorMapFloorRenderBuilder.GetDrawOrder(sceneRender.ViewMode, 0, mapTileX, mapTileY)
            ),
            AnchorX = tileCenterX + (lightPreview.OffsetX * scaleX),
            AnchorY = tileCenterY + (lightPreview.OffsetY * scaleY),
            SuggestedTintColor = ResolveProjectedLightTint(
                lightPreview.TintColor | 0xFF000000u,
                lightPreview.Flags,
                sectorAmbientColors
            ),
            SuggestedOpacity = 0.4d,
            Flags = lightPreview.Flags,
        };
    }

    private (uint? SuggestedTintColor, EditorMapTileLightDiagnostics? Diagnostics) CreateLightDiagnostics(
        double tileCenterX,
        double tileCenterY,
        ArtId artId,
        EditorMapSectorScenePreview sector,
        IReadOnlyList<EditorMapLightRenderItem> virtualLights,
        LightSpatialIndex? lightSpatialIndex
    )
    {
        var stepX = sceneRender.TileWidthPixels / 2d;
        var stepY = sceneRender.TileHeightPixels / 2d;
        var isIndoor = IsIndoorTileArt(artId);
        var ambientColors = ResolveSectorAmbientLightColors(sector.LightSchemeIdx, sceneRender.AmbientLighting);
        var activeLights = CreateActiveLightList(
            tileCenterX,
            tileCenterY,
            stepX,
            stepY,
            virtualLights,
            lightSpatialIndex
        );
        var topLeft = SampleLightColor(tileCenterX - stepX, tileCenterY - stepY, ambientColors, isIndoor, activeLights);
        var topCenter = SampleLightColor(tileCenterX, tileCenterY - stepY, ambientColors, isIndoor, activeLights);
        var topRight = SampleLightColor(
            tileCenterX + stepX,
            tileCenterY - stepY,
            ambientColors,
            isIndoor,
            activeLights
        );
        var middleLeft = SampleLightColor(tileCenterX - stepX, tileCenterY, ambientColors, isIndoor, activeLights);
        var middleCenter = SampleLightColor(tileCenterX, tileCenterY, ambientColors, isIndoor, activeLights);
        var middleRight = SampleLightColor(tileCenterX + stepX, tileCenterY, ambientColors, isIndoor, activeLights);
        var bottomLeft = SampleLightColor(
            tileCenterX - stepX,
            tileCenterY + stepY,
            ambientColors,
            isIndoor,
            activeLights
        );
        var bottomCenter = SampleLightColor(tileCenterX, tileCenterY + stepY, ambientColors, isIndoor, activeLights);
        var bottomRight = SampleLightColor(
            tileCenterX + stepX,
            tileCenterY + stepY,
            ambientColors,
            isIndoor,
            activeLights
        );

        return (
            middleCenter,
            new EditorMapTileLightDiagnostics(
                TopLeft: topLeft,
                TopCenter: topCenter,
                TopRight: topRight,
                MiddleLeft: middleLeft,
                MiddleCenter: middleCenter,
                MiddleRight: middleRight,
                BottomLeft: bottomLeft,
                BottomCenter: bottomCenter,
                BottomRight: bottomRight
            )
        );
    }

    private IReadOnlyList<EditorMapLightRenderItem> CreateActiveLightList(
        double tileCenterX,
        double tileCenterY,
        double stepX,
        double stepY,
        IReadOnlyList<EditorMapLightRenderItem> virtualLights,
        LightSpatialIndex? lightSpatialIndex
    )
    {
        var activeRadius = LightSampleRadius + Math.Sqrt((stepX * stepX) + (stepY * stepY));
        if (lightSpatialIndex is not null)
            return lightSpatialIndex.Query(tileCenterX, tileCenterY, activeRadius);

        List<EditorMapLightRenderItem>? activeLights = null;
        AddActiveLights(sceneRender.Lights, tileCenterX, tileCenterY, activeRadius, ref activeLights);
        AddActiveLights(virtualLights, tileCenterX, tileCenterY, activeRadius, ref activeLights);
        return activeLights ?? [];
    }

    private static void AddActiveLights(
        IReadOnlyList<EditorMapLightRenderItem> lights,
        double tileCenterX,
        double tileCenterY,
        double activeRadius,
        ref List<EditorMapLightRenderItem>? activeLights
    )
    {
        for (var lightIndex = 0; lightIndex < lights.Count; lightIndex++)
        {
            var light = lights[lightIndex];
            if (light.Flags.HasFlag(SectorLightFlags.Off))
                continue;

            var dx = tileCenterX - light.AnchorX;
            var dy = tileCenterY - light.AnchorY;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance >= activeRadius)
                continue;

            (activeLights ??= []).Add(light);
        }
    }

    private (double CenterX, double CenterY) ProjectTileCenter(int mapTileX, int mapTileY)
    {
        var (centerX, centerY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
            sceneRender.ViewMode,
            sceneRender.TileWidthPixels,
            sceneRender.TileHeightPixels,
            mapTileX,
            mapTileY
        );
        return (centerX + sceneRender.OffsetX, centerY + sceneRender.OffsetY);
    }

    private bool IntersectsTile(double centerX, double centerY, EditorMapSceneViewportLayout viewport) =>
        IntersectsAnchor(
            centerX,
            centerY,
            sceneRender.TileWidthPixels * 2d,
            sceneRender.TileHeightPixels * 2d,
            viewport
        );

    private static bool IntersectsAnchor(
        double anchorX,
        double anchorY,
        double width,
        double height,
        EditorMapSceneViewportLayout viewport
    )
    {
        var halfWidth = Math.Max(1d, width) / 2d;
        var halfHeight = Math.Max(1d, height) / 2d;
        return anchorX + halfWidth >= viewport.VisibleLeft
            && anchorX - halfWidth <= viewport.VisibleRight
            && anchorY + halfHeight >= viewport.VisibleTop
            && anchorY - halfHeight <= viewport.VisibleBottom;
    }

    private static int CompareQueueItems(EditorMapRenderQueueItem left, EditorMapRenderQueueItem right)
    {
        var cmp = left.SortKey.CompareTo(right.SortKey);
        if (cmp != 0)
            return cmp;

        cmp = left.Kind.CompareTo(right.Kind);
        if (cmp != 0)
            return cmp;

        cmp = left.DrawOrder.CompareTo(right.DrawOrder);
        return cmp != 0 ? cmp : CompareCoordinates(left, right);
    }

    private static int CompareCoordinates(EditorMapRenderQueueItem left, EditorMapRenderQueueItem right)
    {
        var (leftX, leftY) = GetMapTileCoordinates(left);
        var (rightX, rightY) = GetMapTileCoordinates(right);
        var cmp = leftX.CompareTo(rightX);
        return cmp != 0 ? cmp : leftY.CompareTo(rightY);
    }

    private static (int MapTileX, int MapTileY) GetMapTileCoordinates(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => (item.Tile?.MapTileX ?? 0, item.Tile?.MapTileY ?? 0),
            EditorMapRenderQueueItemKind.TileOverlay => (
                item.TileOverlay?.MapTileX ?? 0,
                item.TileOverlay?.MapTileY ?? 0
            ),
            EditorMapRenderQueueItemKind.Roof => (item.Roof?.MapTileX ?? 0, item.Roof?.MapTileY ?? 0),
            EditorMapRenderQueueItemKind.Light => (item.Light?.MapTileX ?? 0, item.Light?.MapTileY ?? 0),
            _ => (0, 0),
        };

    private static double GetSortOrdinal(long drawOrder) => drawOrder / DrawOrderSortScale;

    private static EditorMapAmbientLightColors ResolveSectorAmbientLightColors(
        int lightSchemeIndex,
        EditorMapAmbientLightingState? ambientLighting
    ) =>
        ambientLighting?.ResolveForSector(lightSchemeIndex)
        ?? new EditorMapAmbientLightColors(new(128, 128, 128), new(255, 255, 255));

    private static uint ResolveProjectedLightTint(
        uint baseTintColor,
        SectorLightFlags flags,
        EditorMapAmbientLightColors ambientColors
    ) =>
        flags.HasFlag(SectorLightFlags.Indoor)
            ? PackOpaqueColor(ambientColors.Indoor)
            : (flags.HasFlag(SectorLightFlags.Outdoor) ? PackOpaqueColor(ambientColors.Outdoor) : baseTintColor);

    private static uint SampleLightColor(
        double px,
        double py,
        EditorMapAmbientLightColors ambientColors,
        bool isIndoor,
        IReadOnlyList<EditorMapLightRenderItem> lights
    )
    {
        var baseAmbientColor = isIndoor ? ambientColors.Indoor : ambientColors.Outdoor;
        double accumR = baseAmbientColor.R;
        double accumG = baseAmbientColor.G;
        double accumB = baseAmbientColor.B;

        for (var index = 0; index < lights.Count; index++)
        {
            var light = lights[index];
            if (light.Flags.HasFlag(SectorLightFlags.Off))
                continue;

            var dx = px - light.AnchorX;
            var dy = py - light.AnchorY;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance >= LightSampleRadius)
                continue;

            var intensity = 1.0d - (distance / LightSampleRadius);
            var lightColor = light.SuggestedTintColor;
            var r = (double)((lightColor >> 16) & 0xFFu);
            var g = (double)((lightColor >> 8) & 0xFFu);
            var b = (double)(lightColor & 0xFFu);

            if (light.Flags.HasFlag(SectorLightFlags.Dark))
            {
                accumR -= r * intensity;
                accumG -= g * intensity;
                accumB -= b * intensity;
            }
            else
            {
                accumR += r * intensity;
                accumG += g * intensity;
                accumB += b * intensity;
            }
        }

        var finalR = (byte)Math.Clamp(accumR, 0d, 255d);
        var finalG = (byte)Math.Clamp(accumG, 0d, 255d);
        var finalB = (byte)Math.Clamp(accumB, 0d, 255d);
        return 0xFF000000u | ((uint)finalR << 16) | ((uint)finalG << 8) | finalB;
    }

    private static uint PackOpaqueColor(Color color) =>
        0xFF000000u | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

    private static bool IsIndoorTileArt(ArtId artId) => artId.Value == 0 || artId.TileType == 0;
}
