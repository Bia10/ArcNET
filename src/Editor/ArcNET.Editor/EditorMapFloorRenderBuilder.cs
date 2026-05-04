using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Builds render-ready floor-tile projections from host-neutral scene previews.
/// </summary>
public static class EditorMapFloorRenderBuilder
{
    private sealed record RawTileRenderItem(
        string SectorAssetPath,
        int MapTileX,
        int MapTileY,
        Location Tile,
        ArtId ArtId,
        bool IsBlocked,
        bool HasLight,
        bool HasScript,
        int DrawOrder,
        double CenterX,
        double CenterY
    );

    private sealed record RawTileOverlayRenderItem(
        string SectorAssetPath,
        int MapTileX,
        int MapTileY,
        Location Tile,
        EditorMapTileOverlayKind Kind,
        double SortKey,
        double CenterX,
        double CenterY,
        double SuggestedOpacity,
        uint SuggestedTintColor
    );

    private sealed record RawObjectRenderItem(
        string SectorAssetPath,
        GameObjectGuid ObjectId,
        GameObjectGuid ProtoId,
        ObjectType ObjectType,
        ArtId CurrentArtId,
        int MapTileX,
        int MapTileY,
        Location Tile,
        double SortKey,
        int PreviewOrder,
        double AnchorX,
        double AnchorY,
        EditorMapObjectSpriteBounds? SpriteBounds,
        bool IsTileGridSnapped
    );

    private sealed record RawRoofRenderItem(
        string SectorAssetPath,
        Location RoofCell,
        int MapTileX,
        int MapTileY,
        ArtId ArtId,
        double SortKey,
        double AnchorX,
        double AnchorY
    );

    /// <summary>
    /// Builds one render-ready floor preview from <paramref name="scenePreview"/>.
    /// </summary>
    public static EditorMapFloorRenderPreview Build(
        EditorMapScenePreview scenePreview,
        EditorMapFloorRenderRequest? request = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);

        request ??= new EditorMapFloorRenderRequest();
        ValidateRequest(request);

        if (scenePreview.Sectors.Count == 0)
        {
            return new EditorMapFloorRenderPreview
            {
                MapName = scenePreview.MapName,
                ViewMode = request.ViewMode,
                TileWidthPixels = request.TileWidthPixels,
                TileHeightPixels = request.TileHeightPixels,
                WidthPixels = 0d,
                HeightPixels = 0d,
                Tiles = [],
                Objects = [],
                Overlays = [],
                Roofs = [],
                RenderQueue = [],
            };
        }

        var sectorTileWidth = scenePreview.Sectors[0].TileWidth;
        var sectorTileHeight = scenePreview.Sectors[0].TileHeight;
        if (sectorTileWidth <= 0 || sectorTileHeight <= 0)
            throw new InvalidOperationException("Scene preview sectors must expose positive tile dimensions.");

        var mapTileWidth = checked(scenePreview.Width * sectorTileWidth);
        var mapTileHeight = checked(scenePreview.Height * sectorTileHeight);
        var maxMapTileY = mapTileHeight - 1;
        var halfTileWidth = request.TileWidthPixels / 2d;
        var halfTileHeight = request.TileHeightPixels / 2d;

        List<RawTileRenderItem> rawTiles = [];
        List<RawTileOverlayRenderItem> rawTileOverlays = [];
        List<RawObjectRenderItem> rawObjects = [];
        List<RawRoofRenderItem> rawRoofs = [];
        var minLeft = double.PositiveInfinity;
        var minTop = double.PositiveInfinity;
        var maxRight = double.NegativeInfinity;
        var maxBottom = double.NegativeInfinity;

        foreach (
            var sector in scenePreview
                .Sectors.OrderBy(static sector => sector.LocalY)
                .ThenBy(static sector => sector.LocalX)
        )
        {
            var lightTileIndices = sector
                .Lights.Select(static light => GetTileIndex(light.TileX, light.TileY))
                .ToHashSet();
            var scriptedTileIndices = sector.TileScripts.Select(static tileScript => tileScript.TileIndex).ToHashSet();

            for (var tileY = 0; tileY < sectorTileHeight; tileY++)
            {
                for (var tileX = 0; tileX < sectorTileWidth; tileX++)
                {
                    var tileArtId = sector.GetTileArtId(tileX, tileY);
                    if (!request.IncludeEmptyTiles && tileArtId == 0)
                        continue;

                    var mapTileX = checked((sector.LocalX * sectorTileWidth) + tileX);
                    var mapTileY = checked((sector.LocalY * sectorTileHeight) + tileY);
                    var tileIndex = GetTileIndex(tileX, tileY);
                    var adjustedMapTileY = maxMapTileY - mapTileY;
                    var drawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, mapTileX, adjustedMapTileY);
                    var (centerX, centerY) = ProjectTileCenter(
                        request.ViewMode,
                        request.TileWidthPixels,
                        request.TileHeightPixels,
                        mapTileX,
                        adjustedMapTileY
                    );

                    minLeft = Math.Min(minLeft, centerX - halfTileWidth);
                    minTop = Math.Min(minTop, centerY - halfTileHeight);
                    maxRight = Math.Max(maxRight, centerX + halfTileWidth);
                    maxBottom = Math.Max(maxBottom, centerY + halfTileHeight);

                    rawTiles.Add(
                        new RawTileRenderItem(
                            SectorAssetPath: sector.AssetPath,
                            MapTileX: mapTileX,
                            MapTileY: mapTileY,
                            Tile: new Location(checked((short)tileX), checked((short)tileY)),
                            ArtId: new ArtId(tileArtId),
                            IsBlocked: sector.IsTileBlocked(tileX, tileY),
                            HasLight: lightTileIndices.Contains(tileIndex),
                            HasScript: scriptedTileIndices.Contains(tileIndex),
                            DrawOrder: drawOrder,
                            CenterX: centerX,
                            CenterY: centerY
                        )
                    );

                    if (sector.IsTileBlocked(tileX, tileY) && request.IncludeBlockedTileOverlays)
                    {
                        rawTileOverlays.Add(
                            new RawTileOverlayRenderItem(
                                SectorAssetPath: sector.AssetPath,
                                MapTileX: mapTileX,
                                MapTileY: mapTileY,
                                Tile: new Location(checked((short)tileX), checked((short)tileY)),
                                Kind: EditorMapTileOverlayKind.BlockedTile,
                                SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.BlockedTile),
                                CenterX: centerX,
                                CenterY: centerY,
                                SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.BlockedTile),
                                SuggestedTintColor: GetTileOverlaySuggestedTintColor(
                                    EditorMapTileOverlayKind.BlockedTile
                                )
                            )
                        );
                    }

                    if (lightTileIndices.Contains(tileIndex) && request.IncludeLightOverlays)
                    {
                        rawTileOverlays.Add(
                            new RawTileOverlayRenderItem(
                                SectorAssetPath: sector.AssetPath,
                                MapTileX: mapTileX,
                                MapTileY: mapTileY,
                                Tile: new Location(checked((short)tileX), checked((short)tileY)),
                                Kind: EditorMapTileOverlayKind.Light,
                                SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.Light),
                                CenterX: centerX,
                                CenterY: centerY,
                                SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.Light),
                                SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.Light)
                            )
                        );
                    }

                    if (scriptedTileIndices.Contains(tileIndex) && request.IncludeScriptOverlays)
                    {
                        rawTileOverlays.Add(
                            new RawTileOverlayRenderItem(
                                SectorAssetPath: sector.AssetPath,
                                MapTileX: mapTileX,
                                MapTileY: mapTileY,
                                Tile: new Location(checked((short)tileX), checked((short)tileY)),
                                Kind: EditorMapTileOverlayKind.Script,
                                SortKey: GetTileOverlaySortKey(drawOrder, EditorMapTileOverlayKind.Script),
                                CenterX: centerX,
                                CenterY: centerY,
                                SuggestedOpacity: GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind.Script),
                                SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.Script)
                            )
                        );
                    }
                }
            }

            if (!request.IncludeObjects)
                continue;

            for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
            {
                var obj = sector.Objects[objectIndex];
                if (obj.Location is not { } location)
                    continue;

                var mapTileX = checked((sector.LocalX * sectorTileWidth) + location.X);
                var mapTileY = checked((sector.LocalY * sectorTileHeight) + location.Y);
                var adjustedMapTileY = maxMapTileY - mapTileY;
                var baseTileDrawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, mapTileX, adjustedMapTileY);
                var (tileCenterX, tileCenterY) = ProjectTileCenter(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    mapTileX,
                    adjustedMapTileY
                );
                var (anchorX, anchorY) = ProjectObjectAnchor(tileCenterX, tileCenterY, obj);

                ExpandObjectBounds(
                    obj.SpriteBounds,
                    anchorX,
                    anchorY,
                    ref minLeft,
                    ref minTop,
                    ref maxRight,
                    ref maxBottom
                );

                rawObjects.Add(
                    new RawObjectRenderItem(
                        SectorAssetPath: sector.AssetPath,
                        ObjectId: obj.ObjectId,
                        ProtoId: obj.ProtoId,
                        ObjectType: obj.ObjectType,
                        CurrentArtId: obj.CurrentArtId,
                        MapTileX: mapTileX,
                        MapTileY: mapTileY,
                        Tile: location,
                        SortKey: GetObjectSortKey(baseTileDrawOrder, obj),
                        PreviewOrder: objectIndex,
                        AnchorX: anchorX,
                        AnchorY: anchorY,
                        SpriteBounds: obj.SpriteBounds,
                        IsTileGridSnapped: obj.IsTileGridSnapped
                    )
                );
            }

            if (!request.IncludeRoofs || sector.RoofArtIds is null)
                continue;

            for (var roofY = 0; roofY < sector.RoofHeight; roofY++)
            {
                for (var roofX = 0; roofX < sector.RoofWidth; roofX++)
                {
                    var roofArtId = sector.GetRoofArtId(roofX, roofY);
                    if (roofArtId is null or 0u)
                        continue;

                    var mapTileX = checked((sector.LocalX * sectorTileWidth) + (roofX * 4));
                    var mapTileY = checked((sector.LocalY * sectorTileHeight) + (roofY * 4));
                    var adjustedRoofTopMapTileY = maxMapTileY - (mapTileY + 3);
                    var sortMapTileX = mapTileX + 3;
                    var sortMapTileY = mapTileY + 3;
                    var adjustedSortMapTileY = maxMapTileY - sortMapTileY;
                    var baseDrawOrder = GetDrawOrder(
                        request.ViewMode,
                        mapTileWidth,
                        sortMapTileX,
                        adjustedSortMapTileY
                    );
                    var (anchorX, anchorY) = ProjectRoofAnchor(
                        request.ViewMode,
                        request.TileWidthPixels,
                        request.TileHeightPixels,
                        mapTileX,
                        adjustedRoofTopMapTileY
                    );

                    ExpandRoofBounds(
                        request.ViewMode,
                        request.TileWidthPixels,
                        request.TileHeightPixels,
                        anchorX,
                        anchorY,
                        ref minLeft,
                        ref minTop,
                        ref maxRight,
                        ref maxBottom
                    );

                    rawRoofs.Add(
                        new RawRoofRenderItem(
                            SectorAssetPath: sector.AssetPath,
                            RoofCell: new Location(checked((short)roofX), checked((short)roofY)),
                            MapTileX: mapTileX,
                            MapTileY: mapTileY,
                            ArtId: new ArtId(roofArtId.Value),
                            SortKey: GetRoofSortKey(baseDrawOrder),
                            AnchorX: anchorX,
                            AnchorY: anchorY
                        )
                    );
                }
            }
        }

        if (rawTiles.Count == 0)
        {
            return new EditorMapFloorRenderPreview
            {
                MapName = scenePreview.MapName,
                ViewMode = request.ViewMode,
                TileWidthPixels = request.TileWidthPixels,
                TileHeightPixels = request.TileHeightPixels,
                WidthPixels = 0d,
                HeightPixels = 0d,
                Tiles = [],
                Objects = [],
                Overlays = [],
                Roofs = [],
                RenderQueue = [],
            };
        }

        var offsetX = -minLeft;
        var offsetY = -minTop;
        var orderedTiles = rawTiles
            .OrderBy(static tile => tile.DrawOrder)
            .ThenBy(static tile => tile.MapTileX)
            .ToArray();
        var tiles = orderedTiles
            .Select(tile => new EditorMapFloorTileRenderItem
            {
                SectorAssetPath = tile.SectorAssetPath,
                MapTileX = tile.MapTileX,
                MapTileY = tile.MapTileY,
                Tile = tile.Tile,
                ArtId = tile.ArtId,
                IsBlocked = tile.IsBlocked,
                HasLight = tile.HasLight,
                HasScript = tile.HasScript,
                DrawOrder = tile.DrawOrder,
                CenterX = tile.CenterX + offsetX,
                CenterY = tile.CenterY + offsetY,
            })
            .ToArray();
        var orderedObjects = rawObjects
            .OrderBy(static obj => obj.SortKey)
            .ThenBy(static obj => obj.MapTileX)
            .ThenBy(static obj => obj.PreviewOrder)
            .ToArray();
        var objects = orderedObjects
            .Select(
                (obj, index) =>
                    new EditorMapObjectRenderItem
                    {
                        SectorAssetPath = obj.SectorAssetPath,
                        ObjectId = obj.ObjectId,
                        ProtoId = obj.ProtoId,
                        ObjectType = obj.ObjectType,
                        CurrentArtId = obj.CurrentArtId,
                        MapTileX = obj.MapTileX,
                        MapTileY = obj.MapTileY,
                        Tile = obj.Tile,
                        DrawOrder = index,
                        AnchorX = obj.AnchorX + offsetX,
                        AnchorY = obj.AnchorY + offsetY,
                        SpriteBounds = obj.SpriteBounds,
                        IsTileGridSnapped = obj.IsTileGridSnapped,
                    }
            )
            .ToArray();
        var orderedOverlays = rawTileOverlays
            .OrderBy(static overlay => overlay.SortKey)
            .ThenBy(static overlay => overlay.MapTileX)
            .ThenBy(static overlay => overlay.MapTileY)
            .ThenBy(static overlay => overlay.Kind)
            .ToArray();
        var overlays = orderedOverlays
            .Select(
                (overlay, index) =>
                    new EditorMapTileOverlayRenderItem
                    {
                        SectorAssetPath = overlay.SectorAssetPath,
                        MapTileX = overlay.MapTileX,
                        MapTileY = overlay.MapTileY,
                        Tile = overlay.Tile,
                        Kind = overlay.Kind,
                        DrawOrder = index,
                        CenterX = overlay.CenterX + offsetX,
                        CenterY = overlay.CenterY + offsetY,
                        SuggestedOpacity = overlay.SuggestedOpacity,
                        SuggestedTintColor = overlay.SuggestedTintColor,
                    }
            )
            .ToArray();
        var orderedRoofs = rawRoofs.OrderBy(static roof => roof.SortKey).ThenBy(static roof => roof.MapTileY).ToArray();
        var roofs = orderedRoofs
            .Select(
                (roof, index) =>
                    new EditorMapRoofRenderItem
                    {
                        SectorAssetPath = roof.SectorAssetPath,
                        RoofCell = roof.RoofCell,
                        MapTileX = roof.MapTileX,
                        MapTileY = roof.MapTileY,
                        ArtId = roof.ArtId,
                        DrawOrder = index,
                        AnchorX = roof.AnchorX + offsetX,
                        AnchorY = roof.AnchorY + offsetY,
                    }
            )
            .ToArray();
        var renderQueue = BuildRenderQueue(
            orderedTiles,
            tiles,
            orderedOverlays,
            overlays,
            orderedObjects,
            objects,
            orderedRoofs,
            roofs
        );

        return new EditorMapFloorRenderPreview
        {
            MapName = scenePreview.MapName,
            ViewMode = request.ViewMode,
            TileWidthPixels = request.TileWidthPixels,
            TileHeightPixels = request.TileHeightPixels,
            WidthPixels = maxRight - minLeft,
            HeightPixels = maxBottom - minTop,
            Tiles = tiles,
            Objects = objects,
            Overlays = overlays,
            Roofs = roofs,
            RenderQueue = renderQueue,
        };
    }

    private static int GetTileIndex(int tileX, int tileY) => checked((tileY * 64) + tileX);

    internal static (double AnchorX, double AnchorY) ProjectObjectAnchor(
        double tileCenterX,
        double tileCenterY,
        EditorMapObjectPreview objectPreview
    ) => (tileCenterX + objectPreview.OffsetX, tileCenterY + objectPreview.OffsetY - objectPreview.OffsetZ);

    internal static int GetDrawOrder(
        EditorMapSceneViewMode viewMode,
        int mapTileWidth,
        int mapTileX,
        int adjustedMapTileY
    ) =>
        viewMode switch
        {
            EditorMapSceneViewMode.TopDown => checked((adjustedMapTileY * mapTileWidth) + mapTileX),
            EditorMapSceneViewMode.Isometric => checked((((adjustedMapTileY + mapTileX) * mapTileWidth) + mapTileX)),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };

    internal static (double CenterX, double CenterY) ProjectTileCenter(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int adjustedMapTileY
    )
    {
        return viewMode switch
        {
            EditorMapSceneViewMode.TopDown => (
                (mapTileX * tileWidthPixels) + (tileWidthPixels / 2d),
                (adjustedMapTileY * tileHeightPixels) + (tileHeightPixels / 2d)
            ),
            EditorMapSceneViewMode.Isometric => (
                (mapTileX - adjustedMapTileY) * (tileWidthPixels / 2d),
                (mapTileX + adjustedMapTileY) * (tileHeightPixels / 2d)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
    }

    internal static double GetObjectSortKey(int baseTileDrawOrder, EditorMapObjectPreview objectPreview) =>
        (baseTileDrawOrder * 4096d)
        + objectPreview.OffsetY
        - objectPreview.OffsetZ
        + (objectPreview.SpriteBounds?.MaxFrameCenterY ?? 0)
        + ((objectPreview.SpriteBounds?.MaxFrameHeight ?? 0) / 4096d)
        + (objectPreview.CollisionHeight / 16777216d);

    internal static double GetTileOverlaySortKey(int tileDrawOrder, EditorMapTileOverlayKind kind) =>
        (tileDrawOrder * 4096d) + 1024d + (int)kind;

    internal static double GetRoofSortKey(int baseTileDrawOrder) => (baseTileDrawOrder * 4096d) + 3072d;

    internal static double GetTileOverlaySuggestedOpacity(EditorMapTileOverlayKind kind) =>
        kind switch
        {
            EditorMapTileOverlayKind.BlockedTile => 0.45d,
            EditorMapTileOverlayKind.Light => 0.4d,
            EditorMapTileOverlayKind.Script => 0.45d,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported tile overlay kind."),
        };

    internal static uint GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind kind) =>
        kind switch
        {
            EditorMapTileOverlayKind.BlockedTile => 0x88CC6666u,
            EditorMapTileOverlayKind.Light => 0x88E0C85Au,
            EditorMapTileOverlayKind.Script => 0x88996CCCu,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported tile overlay kind."),
        };

    internal static void ExpandObjectBounds(
        EditorMapObjectSpriteBounds? spriteBounds,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        if (spriteBounds is null)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX);
            maxBottom = Math.Max(maxBottom, anchorY);
            return;
        }

        var left = anchorX - spriteBounds.MaxFrameCenterX;
        var top = anchorY - spriteBounds.MaxFrameCenterY;
        var right = left + spriteBounds.MaxFrameWidth;
        var bottom = top + spriteBounds.MaxFrameHeight;

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    internal static (double AnchorX, double AnchorY) ProjectRoofAnchor(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int adjustedTopMapTileY
    )
    {
        return viewMode switch
        {
            EditorMapSceneViewMode.TopDown => (mapTileX * tileWidthPixels, adjustedTopMapTileY * tileHeightPixels),
            EditorMapSceneViewMode.Isometric => (
                (mapTileX - adjustedTopMapTileY) * (tileWidthPixels / 2d),
                (mapTileX + adjustedTopMapTileY + 3d) * (tileHeightPixels / 2d)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
    }

    internal static void ExpandRoofBounds(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        if (viewMode is EditorMapSceneViewMode.TopDown)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX + (tileWidthPixels * 4d));
            maxBottom = Math.Max(maxBottom, anchorY + (tileHeightPixels * 4d));
            return;
        }

        minLeft = Math.Min(minLeft, anchorX - (tileWidthPixels * 2d));
        minTop = Math.Min(minTop, anchorY - tileHeightPixels);
        maxRight = Math.Max(maxRight, anchorX + (tileWidthPixels * 2d));
        maxBottom = Math.Max(maxBottom, anchorY + (tileHeightPixels * 2d));
    }

    private static IReadOnlyList<EditorMapRenderQueueItem> BuildRenderQueue(
        IReadOnlyList<RawTileRenderItem> rawTiles,
        IReadOnlyList<EditorMapFloorTileRenderItem> tiles,
        IReadOnlyList<RawTileOverlayRenderItem> rawTileOverlays,
        IReadOnlyList<EditorMapTileOverlayRenderItem> tileOverlays,
        IReadOnlyList<RawObjectRenderItem> rawObjects,
        IReadOnlyList<EditorMapObjectRenderItem> objects,
        IReadOnlyList<RawRoofRenderItem> rawRoofs,
        IReadOnlyList<EditorMapRoofRenderItem> roofs
    )
    {
        List<(double SortKey, EditorMapRenderQueueItemKind Kind, int Index)> queue = [];
        for (var index = 0; index < rawTiles.Count; index++)
            queue.Add((rawTiles[index].DrawOrder * 4096d, EditorMapRenderQueueItemKind.FloorTile, index));

        for (var index = 0; index < rawTileOverlays.Count; index++)
            queue.Add((rawTileOverlays[index].SortKey, EditorMapRenderQueueItemKind.TileOverlay, index));

        for (var index = 0; index < rawObjects.Count; index++)
            queue.Add((rawObjects[index].SortKey, EditorMapRenderQueueItemKind.Object, index));

        for (var index = 0; index < rawRoofs.Count; index++)
            queue.Add((rawRoofs[index].SortKey, EditorMapRenderQueueItemKind.Roof, index));

        return queue
            .OrderBy(static item => item.SortKey)
            .ThenBy(static item => item.Kind)
            .ThenBy(static item => item.Index)
            .Select(
                (item, drawOrder) =>
                    item.Kind switch
                    {
                        EditorMapRenderQueueItemKind.FloorTile => new EditorMapRenderQueueItem
                        {
                            Kind = item.Kind,
                            DrawOrder = drawOrder,
                            SortKey = item.SortKey,
                            Tile = tiles[item.Index],
                        },
                        EditorMapRenderQueueItemKind.Object => new EditorMapRenderQueueItem
                        {
                            Kind = item.Kind,
                            DrawOrder = drawOrder,
                            SortKey = item.SortKey,
                            Object = objects[item.Index],
                        },
                        EditorMapRenderQueueItemKind.TileOverlay => new EditorMapRenderQueueItem
                        {
                            Kind = item.Kind,
                            DrawOrder = drawOrder,
                            SortKey = item.SortKey,
                            TileOverlay = tileOverlays[item.Index],
                        },
                        EditorMapRenderQueueItemKind.Roof => new EditorMapRenderQueueItem
                        {
                            Kind = item.Kind,
                            DrawOrder = drawOrder,
                            SortKey = item.SortKey,
                            Roof = roofs[item.Index],
                        },
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(item.Kind),
                            item.Kind,
                            "Unsupported render queue kind."
                        ),
                    }
            )
            .ToArray();
    }

    private static void ValidateRequest(EditorMapFloorRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!double.IsFinite(request.TileWidthPixels) || request.TileWidthPixels <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.TileWidthPixels,
                "Tile width must be a finite positive value."
            );
        }

        if (!double.IsFinite(request.TileHeightPixels) || request.TileHeightPixels <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.TileHeightPixels,
                "Tile height must be a finite positive value."
            );
        }
    }
}
