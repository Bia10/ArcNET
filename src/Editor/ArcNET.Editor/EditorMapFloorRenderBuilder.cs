using System.Collections.Concurrent;
using System.Numerics;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Builds render-ready floor-tile projections from host-neutral scene previews.
/// Sectors are processed in parallel and tile/roof iteration is accelerated with precomputed dense-tile bitmasks.
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
        long DrawOrder,
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
        int TileOrderSecondary,
        int TypeSortPriority,
        double TieBreakerSortKey,
        int PreviewOrder,
        double AnchorX,
        double AnchorY,
        EditorMapObjectSpriteBounds? SpriteBounds,
        bool IsTileGridSnapped,
        float Rotation,
        float RotationPitch
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

    private sealed class SectorAccumulator
    {
        public readonly List<RawTileRenderItem> RawTiles = [];
        public readonly List<RawTileOverlayRenderItem> RawTileOverlays = [];
        public readonly List<RawObjectRenderItem> RawObjects = [];
        public readonly List<RawRoofRenderItem> RawRoofs = [];
        public double MinLeft = double.PositiveInfinity;
        public double MinTop = double.PositiveInfinity;
        public double MaxRight = double.NegativeInfinity;
        public double MaxBottom = double.NegativeInfinity;
    }

    /// <summary>
    /// Builds one render-ready floor preview from <paramref name="scenePreview"/>.
    /// Sectors are processed in parallel; tile and roof iteration is accelerated with precomputed dense-tile bitmasks.
    /// </summary>
    public static EditorMapFloorRenderPreview Build(
        EditorMapScenePreview scenePreview,
        EditorMapFloorRenderRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);

        request ??= new EditorMapFloorRenderRequest();
        ValidateRequest(request);

        if (scenePreview.Sectors.Count == 0)
            return CreateEmptyPreview(scenePreview.MapName, request);

        var sectorTileWidth = scenePreview.Sectors[0].TileWidth;
        var sectorTileHeight = scenePreview.Sectors[0].TileHeight;
        if (sectorTileWidth <= 0 || sectorTileHeight <= 0)
            throw new InvalidOperationException("Scene preview sectors must expose positive tile dimensions.");

        var mapTileWidth = checked(scenePreview.Width * sectorTileWidth);

        var sectors = scenePreview
            .Sectors.OrderBy(static sector => sector.LocalY)
            .ThenBy(static sector => sector.LocalX)
            .ToArray();

        // Phase 1: Collect raw render items in parallel across sectors using lock-free accumulators.
        var accumulators = new ConcurrentBag<SectorAccumulator>();

        var parallelResult = Parallel.ForEach(
            sectors,
            new ParallelOptions { CancellationToken = cancellationToken },
            () => new SectorAccumulator(),
            (sector, _, _, local) =>
            {
                ProcessSector(sector, request, sectorTileWidth, sectorTileHeight, mapTileWidth, local);
                return local;
            },
            local =>
            {
                accumulators.Add(local);
            }
        );

        if (!parallelResult.IsCompleted)
            cancellationToken.ThrowIfCancellationRequested();

        // Phase 1b: Merge accumulators — pre-count to avoid reallocations.
        var totalTileCount = 0;
        var totalOverlayCount = 0;
        var totalObjectCount = 0;
        var totalRoofCount = 0;
        foreach (var acc in accumulators)
        {
            totalTileCount += acc.RawTiles.Count;
            totalOverlayCount += acc.RawTileOverlays.Count;
            totalObjectCount += acc.RawObjects.Count;
            totalRoofCount += acc.RawRoofs.Count;
        }

        var rawTiles = new List<RawTileRenderItem>(totalTileCount);
        var rawTileOverlays = new List<RawTileOverlayRenderItem>(totalOverlayCount);
        var rawObjects = new List<RawObjectRenderItem>(totalObjectCount);
        var rawRoofs = new List<RawRoofRenderItem>(totalRoofCount);
        var minLeft = double.PositiveInfinity;
        var minTop = double.PositiveInfinity;
        var maxRight = double.NegativeInfinity;
        var maxBottom = double.NegativeInfinity;

        foreach (var local in accumulators)
        {
            rawTiles.AddRange(local.RawTiles);
            rawTileOverlays.AddRange(local.RawTileOverlays);
            rawObjects.AddRange(local.RawObjects);
            rawRoofs.AddRange(local.RawRoofs);

            if (local.MinLeft < minLeft)
                minLeft = local.MinLeft;
            if (local.MinTop < minTop)
                minTop = local.MinTop;
            if (local.MaxRight > maxRight)
                maxRight = local.MaxRight;
            if (local.MaxBottom > maxBottom)
                maxBottom = local.MaxBottom;
        }

        if (rawTiles.Count == 0)
            return CreateEmptyPreview(scenePreview.MapName, request);

        // Phase 2: Sort and build final output.
        cancellationToken.ThrowIfCancellationRequested();
        var offsetX = -minLeft;
        var offsetY = -minTop;

        SortRawItems(rawTiles, rawTileOverlays, rawObjects, rawRoofs);
        cancellationToken.ThrowIfCancellationRequested();

        return BuildResult(
            scenePreview.MapName,
            request,
            rawTiles,
            rawTileOverlays,
            rawObjects,
            rawRoofs,
            offsetX,
            offsetY,
            minLeft,
            maxRight,
            minTop,
            maxBottom
        );
    }

    /// <summary>
    /// Builds a delta floor render preview by replacing one sector's items in <paramref name="existingPreview"/>.
    /// Only the changed sector is re-processed; all other sector entries are preserved and re-sorted alongside the new items.
    /// This is significantly cheaper than a full rebuild when editing one sector at a time.
    /// </summary>
    public static EditorMapFloorRenderPreview BuildDelta(
        EditorMapFloorRenderPreview existingPreview,
        EditorMapScenePreview scenePreview,
        string changedSectorAssetPath,
        EditorMapFloorRenderRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(existingPreview);
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedSectorAssetPath);

        request ??= new EditorMapFloorRenderRequest();
        ValidateRequest(request);

        var changedSector = scenePreview.Sectors.FirstOrDefault(s =>
            string.Equals(s.AssetPath, changedSectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (changedSector is null)
            return existingPreview;

        var sectorTileWidth = changedSector.TileWidth;
        var sectorTileHeight = changedSector.TileHeight;
        var mapTileWidth = checked(scenePreview.Width * sectorTileWidth);

        // Process just the changed sector.
        cancellationToken.ThrowIfCancellationRequested();
        var local = new SectorAccumulator();
        ProcessSector(changedSector, request, sectorTileWidth, sectorTileHeight, mapTileWidth, local);

        // Remove old entries for this sector, then append new raw items.
        var rawTiles = RemoveSectorItems(existingPreview.Tiles, changedSectorAssetPath)
            .Select(t => new RawTileRenderItem(
                SectorAssetPath: t.SectorAssetPath,
                MapTileX: t.MapTileX,
                MapTileY: t.MapTileY,
                Tile: t.Tile,
                ArtId: t.ArtId,
                IsBlocked: t.IsBlocked,
                HasLight: t.HasLight,
                HasScript: t.HasScript,
                DrawOrder: GetDrawOrder(request.ViewMode, mapTileWidth, t.MapTileX, t.MapTileY),
                CenterX: t.CenterX - existingPreview.OffsetX,
                CenterY: t.CenterY - existingPreview.OffsetY
            ))
            .Concat(local.RawTiles)
            .ToList();

        var rawTileOverlays = RemoveSectorItems(existingPreview.Overlays, changedSectorAssetPath)
            .Select(o => new RawTileOverlayRenderItem(
                SectorAssetPath: o.SectorAssetPath,
                MapTileX: o.MapTileX,
                MapTileY: o.MapTileY,
                Tile: o.Tile,
                Kind: o.Kind,
                SortKey: GetTileOverlaySortKey(
                    GetDrawOrder(request.ViewMode, mapTileWidth, o.MapTileX, o.MapTileY),
                    o.Kind
                ),
                CenterX: o.CenterX - existingPreview.OffsetX,
                CenterY: o.CenterY - existingPreview.OffsetY,
                SuggestedOpacity: o.SuggestedOpacity,
                SuggestedTintColor: o.SuggestedTintColor
            ))
            .Concat(local.RawTileOverlays)
            .ToList();

        var rawObjects = RemoveSectorItems(existingPreview.Objects, changedSectorAssetPath)
            .Select(o => new RawObjectRenderItem(
                SectorAssetPath: o.SectorAssetPath,
                ObjectId: o.ObjectId,
                ProtoId: o.ProtoId,
                ObjectType: o.ObjectType,
                CurrentArtId: o.CurrentArtId,
                MapTileX: o.MapTileX,
                MapTileY: o.MapTileY,
                Tile: o.Tile,
                SortKey: GetObjectSortKey(
                    GetDrawOrder(request.ViewMode, mapTileWidth, o.MapTileX, o.MapTileY),
                    new EditorMapObjectPreview
                    {
                        ObjectId = o.ObjectId,
                        ProtoId = o.ProtoId,
                        ObjectType = o.ObjectType,
                        CurrentArtId = o.CurrentArtId,
                        RotationPitch = o.RotationPitch,
                        OffsetX = (int)(o.AnchorX - existingPreview.OffsetX),
                        OffsetY = (int)(o.AnchorY - existingPreview.OffsetY),
                    }
                ),
                TileOrderSecondary: 0,
                TypeSortPriority: GetObjectTypeSortPriority(o.ObjectType),
                TieBreakerSortKey: (o.SpriteBounds?.MaxFrameCenterY ?? 0)
                    + ((o.SpriteBounds?.MaxFrameHeight ?? 0) / 4096d),
                PreviewOrder: o.DrawOrder,
                AnchorX: o.AnchorX - existingPreview.OffsetX,
                AnchorY: o.AnchorY - existingPreview.OffsetY,
                SpriteBounds: o.SpriteBounds,
                IsTileGridSnapped: o.IsTileGridSnapped,
                Rotation: o.Rotation,
                RotationPitch: o.RotationPitch
            ))
            .Concat(local.RawObjects)
            .ToList();

        var rawRoofs = RemoveSectorItems(existingPreview.Roofs, changedSectorAssetPath)
            .Select(r => new RawRoofRenderItem(
                SectorAssetPath: r.SectorAssetPath,
                RoofCell: r.RoofCell,
                MapTileX: r.MapTileX,
                MapTileY: r.MapTileY,
                ArtId: r.ArtId,
                SortKey: GetRoofSortKey(GetDrawOrder(request.ViewMode, mapTileWidth, r.MapTileX, r.MapTileY)),
                AnchorX: r.AnchorX - existingPreview.OffsetX,
                AnchorY: r.AnchorY - existingPreview.OffsetY
            ))
            .Concat(local.RawRoofs)
            .ToList();

        // Merge bounds.
        var minLeft = existingPreview.RawMinLeft;
        var minTop = existingPreview.RawMinTop;
        var maxRight = existingPreview.RawMaxRight;
        var maxBottom = existingPreview.RawMaxBottom;
        if (local.RawTiles.Count > 0)
        {
            minLeft = Math.Min(minLeft, local.MinLeft);
            minTop = Math.Min(minTop, local.MinTop);
            maxRight = Math.Max(maxRight, local.MaxRight);
            maxBottom = Math.Max(maxBottom, local.MaxBottom);
        }

        var offsetX = -minLeft;
        var offsetY = -minTop;

        // Re-sort and build.
        cancellationToken.ThrowIfCancellationRequested();
        SortRawItems(rawTiles, rawTileOverlays, rawObjects, rawRoofs);

        cancellationToken.ThrowIfCancellationRequested();
        return BuildResult(
            scenePreview.MapName,
            request,
            rawTiles,
            rawTileOverlays,
            rawObjects,
            rawRoofs,
            offsetX,
            offsetY,
            minLeft,
            maxRight,
            minTop,
            maxBottom
        );
    }

    private static List<T> RemoveSectorItems<T>(IReadOnlyList<T> items, string sectorAssetPath)
    {
        var result = new List<T>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!ItemBelongsToSector(item, sectorAssetPath))
                result.Add(item);
        }

        return result;
    }

    private static bool ItemBelongsToSector<T>(T item, string sectorAssetPath) =>
        item switch
        {
            EditorMapFloorTileRenderItem t => string.Equals(
                t.SectorAssetPath,
                sectorAssetPath,
                StringComparison.OrdinalIgnoreCase
            ),
            EditorMapObjectRenderItem o => string.Equals(
                o.SectorAssetPath,
                sectorAssetPath,
                StringComparison.OrdinalIgnoreCase
            ),
            EditorMapTileOverlayRenderItem ol => string.Equals(
                ol.SectorAssetPath,
                sectorAssetPath,
                StringComparison.OrdinalIgnoreCase
            ),
            EditorMapRoofRenderItem r => string.Equals(
                r.SectorAssetPath,
                sectorAssetPath,
                StringComparison.OrdinalIgnoreCase
            ),
            _ => false,
        };

    private static void ProcessSector(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        SectorAccumulator local
    )
    {
        var halfTileWidth = request.TileWidthPixels / 2d;
        var halfTileHeight = request.TileHeightPixels / 2d;

        var lightTileIndices = sector.LightTileIndices;
        var scriptedTileIndices = sector.ScriptedTileIndices;

        // Tiles: use precomputed row bitmasks to skip empty rows/columns.
        var tileRowMasks = sector.TileRowMasks;
        for (var tileY = 0; tileY < sectorTileHeight; tileY++)
        {
            var rowMask = tileRowMasks[tileY];
            if (rowMask == 0 && !request.IncludeEmptyTiles)
                continue;

            if (request.IncludeEmptyTiles)
            {
                for (var tileX = 0; tileX < sectorTileWidth; tileX++)
                    ProcessTile(
                        sector,
                        request,
                        sectorTileWidth,
                        sectorTileHeight,
                        mapTileWidth,
                        halfTileWidth,
                        halfTileHeight,
                        tileX,
                        tileY,
                        lightTileIndices,
                        scriptedTileIndices,
                        local
                    );
                continue;
            }

            var remaining = rowMask;
            while (remaining != 0)
            {
                var tileX = BitOperations.TrailingZeroCount(remaining);
                ProcessTile(
                    sector,
                    request,
                    sectorTileWidth,
                    sectorTileHeight,
                    mapTileWidth,
                    halfTileWidth,
                    halfTileHeight,
                    tileX,
                    tileY,
                    lightTileIndices,
                    scriptedTileIndices,
                    local
                );
                remaining &= remaining - 1;
            }
        }

        // Objects.
        if (request.IncludeObjects)
        {
            for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
            {
                var obj = sector.Objects[objectIndex];
                if (obj.Location is not { } location)
                    continue;

                var mapTileX = checked((sector.LocalX * sectorTileWidth) + location.X);
                var mapTileY = checked((sector.LocalY * sectorTileHeight) + location.Y);
                var baseTileDrawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, mapTileX, mapTileY);
                var (tileCenterX, tileCenterY) = ProjectTileCenter(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    mapTileX,
                    mapTileY
                );
                var (anchorX, anchorY) = ProjectObjectAnchor(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    tileCenterX,
                    tileCenterY,
                    obj
                );

                ExpandObjectBounds(
                    obj,
                    anchorX,
                    anchorY,
                    ref local.MinLeft,
                    ref local.MinTop,
                    ref local.MaxRight,
                    ref local.MaxBottom
                );

                var (tileOrderPrimary, tileOrderSecondary) = GetObjectTileOrderComponents(obj);

                local.RawObjects.Add(
                    new RawObjectRenderItem(
                        SectorAssetPath: sector.AssetPath,
                        ObjectId: obj.ObjectId,
                        ProtoId: obj.ProtoId,
                        ObjectType: obj.ObjectType,
                        CurrentArtId: obj.CurrentArtId,
                        MapTileX: mapTileX,
                        MapTileY: mapTileY,
                        Tile: location,
                        SortKey: GetObjectSortKey(baseTileDrawOrder, tileOrderPrimary),
                        TileOrderSecondary: tileOrderSecondary,
                        TypeSortPriority: GetObjectTypeSortPriority(obj.ObjectType),
                        TieBreakerSortKey: GetObjectTieBreakerSortKey(obj),
                        PreviewOrder: objectIndex,
                        AnchorX: anchorX,
                        AnchorY: anchorY,
                        SpriteBounds: obj.SpriteBounds,
                        IsTileGridSnapped: obj.IsTileGridSnapped,
                        Rotation: obj.Rotation,
                        RotationPitch: obj.RotationPitch
                    )
                );
            }
        }

        // Roofs: use precomputed row bitmasks.
        if (request.IncludeRoofs && sector.RoofArtIds is not null)
        {
            var roofRowMasks = sector.RoofRowMasks;
            for (var roofY = 0; roofY < sector.RoofHeight; roofY++)
            {
                if (roofRowMasks is not null)
                {
                    var rowMask = roofRowMasks[roofY];
                    if (rowMask == 0)
                        continue;

                    var remaining = rowMask;
                    while (remaining != 0)
                    {
                        var roofX = BitOperations.TrailingZeroCount(remaining);
                        ProcessRoof(
                            sector,
                            request,
                            sectorTileWidth,
                            sectorTileHeight,
                            mapTileWidth,
                            roofX,
                            roofY,
                            local
                        );
                        remaining &= remaining - 1;
                    }
                }
                else
                {
                    for (var roofX = 0; roofX < sector.RoofWidth; roofX++)
                        ProcessRoof(
                            sector,
                            request,
                            sectorTileWidth,
                            sectorTileHeight,
                            mapTileWidth,
                            roofX,
                            roofY,
                            local
                        );
                }
            }
        }
    }

    private static void ProcessTile(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        double halfTileWidth,
        double halfTileHeight,
        int tileX,
        int tileY,
        HashSet<int> lightTileIndices,
        HashSet<int> scriptedTileIndices,
        SectorAccumulator local
    )
    {
        var tileArtId = sector.GetTileArtId(tileX, tileY);
        if (!request.IncludeEmptyTiles && tileArtId == 0)
            return;

        var mapTileX = checked((sector.LocalX * sectorTileWidth) + tileX);
        var mapTileY = checked((sector.LocalY * sectorTileHeight) + tileY);
        var tileIndex = GetTileIndex(tileX, tileY);
        var drawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, mapTileX, mapTileY);
        var (centerX, centerY) = ProjectTileCenter(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            mapTileX,
            mapTileY
        );

        local.MinLeft = Math.Min(local.MinLeft, centerX - halfTileWidth);
        local.MinTop = Math.Min(local.MinTop, centerY - halfTileHeight);
        local.MaxRight = Math.Max(local.MaxRight, centerX + halfTileWidth);
        local.MaxBottom = Math.Max(local.MaxBottom, centerY + halfTileHeight);

        local.RawTiles.Add(
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
            local.RawTileOverlays.Add(
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
                    SuggestedTintColor: GetTileOverlaySuggestedTintColor(EditorMapTileOverlayKind.BlockedTile)
                )
            );
        }

        if (lightTileIndices.Contains(tileIndex) && request.IncludeLightOverlays)
        {
            local.RawTileOverlays.Add(
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
            local.RawTileOverlays.Add(
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

    private static void ProcessRoof(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        int roofX,
        int roofY,
        SectorAccumulator local
    )
    {
        var roofArtId = sector.GetRoofArtId(roofX, roofY);
        if (roofArtId is null or 0u or uint.MaxValue)
            return;

        var mapTileX = checked((sector.LocalX * sectorTileWidth) + (roofX * 4));
        var mapTileY = checked((sector.LocalY * sectorTileHeight) + (roofY * 4));
        var sortMapTileX = mapTileX + 3;
        var sortMapTileY = mapTileY + 3;
        var baseDrawOrder = GetDrawOrder(request.ViewMode, mapTileWidth, sortMapTileX, sortMapTileY);
        var (anchorX, anchorY) = ProjectRoofAnchor(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            mapTileX,
            mapTileY
        );

        ExpandRoofBounds(
            request.ViewMode,
            request.TileWidthPixels,
            request.TileHeightPixels,
            anchorX,
            anchorY,
            ref local.MinLeft,
            ref local.MinTop,
            ref local.MaxRight,
            ref local.MaxBottom
        );

        local.RawRoofs.Add(
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

    private static EditorMapFloorRenderPreview CreateEmptyPreview(
        string mapName,
        EditorMapFloorRenderRequest request
    ) =>
        new()
        {
            MapName = mapName,
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

    private static void SortRawItems(
        List<RawTileRenderItem> rawTiles,
        List<RawTileOverlayRenderItem> rawTileOverlays,
        List<RawObjectRenderItem> rawObjects,
        List<RawRoofRenderItem> rawRoofs
    )
    {
        rawTiles.Sort(
            (a, b) =>
            {
                var cmp = a.DrawOrder.CompareTo(b.DrawOrder);
                return cmp != 0 ? cmp : a.MapTileX.CompareTo(b.MapTileX);
            }
        );

        rawTileOverlays.Sort(
            (a, b) =>
            {
                var cmp = a.SortKey.CompareTo(b.SortKey);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileY.CompareTo(b.MapTileY);
                return cmp != 0 ? cmp : a.Kind.CompareTo(b.Kind);
            }
        );

        rawObjects.Sort(
            (a, b) =>
            {
                var cmp = a.SortKey.CompareTo(b.SortKey);
                if (cmp != 0)
                    return cmp;
                cmp = a.TileOrderSecondary.CompareTo(b.TileOrderSecondary);
                if (cmp != 0)
                    return cmp;
                cmp = a.TypeSortPriority.CompareTo(b.TypeSortPriority);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.PreviewOrder.CompareTo(b.PreviewOrder);
            }
        );

        rawRoofs.Sort(
            (a, b) =>
            {
                var cmp = a.SortKey.CompareTo(b.SortKey);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );
    }

    private static EditorMapFloorRenderPreview BuildResult(
        string mapName,
        EditorMapFloorRenderRequest request,
        List<RawTileRenderItem> rawTiles,
        List<RawTileOverlayRenderItem> rawTileOverlays,
        List<RawObjectRenderItem> rawObjects,
        List<RawRoofRenderItem> rawRoofs,
        double offsetX,
        double offsetY,
        double minLeft,
        double maxRight,
        double minTop,
        double maxBottom
    )
    {
        var tiles = new EditorMapFloorTileRenderItem[rawTiles.Count];
        for (var i = 0; i < rawTiles.Count; i++)
        {
            var t = rawTiles[i];
            tiles[i] = new EditorMapFloorTileRenderItem
            {
                SectorAssetPath = t.SectorAssetPath,
                MapTileX = t.MapTileX,
                MapTileY = t.MapTileY,
                Tile = t.Tile,
                ArtId = t.ArtId,
                IsBlocked = t.IsBlocked,
                HasLight = t.HasLight,
                HasScript = t.HasScript,
                DrawOrder = i,
                CenterX = t.CenterX + offsetX,
                CenterY = t.CenterY + offsetY,
            };
        }

        var objects = new EditorMapObjectRenderItem[rawObjects.Count];
        for (var i = 0; i < rawObjects.Count; i++)
        {
            var o = rawObjects[i];
            objects[i] = new EditorMapObjectRenderItem
            {
                SectorAssetPath = o.SectorAssetPath,
                ObjectId = o.ObjectId,
                ProtoId = o.ProtoId,
                ObjectType = o.ObjectType,
                CurrentArtId = o.CurrentArtId,
                MapTileX = o.MapTileX,
                MapTileY = o.MapTileY,
                Tile = o.Tile,
                DrawOrder = i,
                AnchorX = o.AnchorX + offsetX,
                AnchorY = o.AnchorY + offsetY,
                SpriteBounds = o.SpriteBounds,
                IsTileGridSnapped = o.IsTileGridSnapped,
                Rotation = o.Rotation,
                RotationPitch = o.RotationPitch,
            };
        }

        var overlays = new EditorMapTileOverlayRenderItem[rawTileOverlays.Count];
        for (var i = 0; i < rawTileOverlays.Count; i++)
        {
            var o = rawTileOverlays[i];
            overlays[i] = new EditorMapTileOverlayRenderItem
            {
                SectorAssetPath = o.SectorAssetPath,
                MapTileX = o.MapTileX,
                MapTileY = o.MapTileY,
                Tile = o.Tile,
                Kind = o.Kind,
                DrawOrder = i,
                CenterX = o.CenterX + offsetX,
                CenterY = o.CenterY + offsetY,
                SuggestedOpacity = o.SuggestedOpacity,
                SuggestedTintColor = o.SuggestedTintColor,
            };
        }

        var roofs = new EditorMapRoofRenderItem[rawRoofs.Count];
        for (var i = 0; i < rawRoofs.Count; i++)
        {
            var r = rawRoofs[i];
            roofs[i] = new EditorMapRoofRenderItem
            {
                SectorAssetPath = r.SectorAssetPath,
                RoofCell = r.RoofCell,
                MapTileX = r.MapTileX,
                MapTileY = r.MapTileY,
                ArtId = r.ArtId,
                DrawOrder = i,
                AnchorX = r.AnchorX + offsetX,
                AnchorY = r.AnchorY + offsetY,
            };
        }

        var renderQueue = BuildRenderQueue(
            rawTiles,
            tiles,
            rawTileOverlays,
            overlays,
            rawObjects,
            objects,
            rawRoofs,
            roofs
        );

        return new EditorMapFloorRenderPreview
        {
            MapName = mapName,
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
            OffsetX = offsetX,
            OffsetY = offsetY,
            RawMinLeft = minLeft,
            RawMinTop = minTop,
            RawMaxRight = maxRight,
            RawMaxBottom = maxBottom,
        };
    }

    private static int GetTileIndex(int tileX, int tileY) => checked((tileY * 64) + tileX);

    internal static (double AnchorX, double AnchorY) ProjectObjectAnchor(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        double tileCenterX,
        double tileCenterY,
        EditorMapObjectPreview objectPreview
    )
    {
        var (offsetX, offsetY, offsetZ) = ScaleObjectOffsets(
            viewMode,
            tileWidthPixels,
            tileHeightPixels,
            objectPreview
        );
        _ = offsetZ;
        return (tileCenterX + offsetX, tileCenterY + offsetY);
    }

    private static (double OffsetX, double OffsetY, double OffsetZ) ScaleObjectOffsets(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        EditorMapObjectPreview objectPreview
    )
    {
        if (viewMode is not EditorMapSceneViewMode.Isometric)
            return (objectPreview.OffsetX, objectPreview.OffsetY, objectPreview.OffsetZ);

        var scaleX = tileWidthPixels / 80d;
        var scaleY = tileHeightPixels / 40d;
        return (
            (objectPreview.OffsetX + 40) * scaleX,
            (objectPreview.OffsetY + 20) * scaleY,
            objectPreview.OffsetZ * scaleY
        );
    }

    internal static long GetDrawOrder(EditorMapSceneViewMode viewMode, int mapTileWidth, int mapTileX, int mapTileY) =>
        viewMode switch
        {
            EditorMapSceneViewMode.TopDown => checked((((long)mapTileY * mapTileWidth) + mapTileX)),
            EditorMapSceneViewMode.Isometric => checked((((long)mapTileY + mapTileX) * mapTileWidth) + mapTileX),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };

    internal static (double CenterX, double CenterY) ProjectTileCenter(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int mapTileY
    )
    {
        return viewMode switch
        {
            EditorMapSceneViewMode.TopDown => (
                (-mapTileX * tileWidthPixels) + (tileWidthPixels / 2d),
                (mapTileY * tileHeightPixels) + (tileHeightPixels / 2d)
            ),
            EditorMapSceneViewMode.Isometric => (
                (mapTileY - mapTileX) * (tileWidthPixels / 2d),
                ((mapTileX + mapTileY) * (tileHeightPixels / 2d)) + (tileHeightPixels / 2d)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
    }

    internal static double GetObjectSortKey(long baseTileDrawOrder, EditorMapObjectPreview objectPreview)
    {
        var (tileOrderPrimary, _) = GetObjectTileOrderComponents(objectPreview);
        return GetObjectSortKey(baseTileDrawOrder, tileOrderPrimary);
    }

    internal static (int Primary, int Secondary) GetObjectTileOrderComponents(EditorMapObjectPreview objectPreview)
    {
        var (offsetX, offsetY) = GetObjectTileOrderOffsets(objectPreview);
        var horizontal = (offsetX - 40) / 2;
        var vertical = 2 * (offsetY / 2);
        return (Primary: horizontal + vertical, Secondary: vertical - horizontal);
    }

    internal static double GetObjectTieBreakerSortKey(EditorMapObjectPreview objectPreview) =>
        (objectPreview.SpriteBounds?.MaxFrameCenterY ?? 0)
        + ((objectPreview.SpriteBounds?.MaxFrameHeight ?? 0) / 4096d)
        + (objectPreview.CollisionHeight / 16777216d);

    private static double GetObjectSortKey(long baseTileDrawOrder, int tileOrderPrimary) =>
        (baseTileDrawOrder * 4096d) + 2048d + tileOrderPrimary;

    private static (int OffsetX, int OffsetY) GetObjectTileOrderOffsets(EditorMapObjectPreview objectPreview) =>
        UsesCeWallPortalOrdering(objectPreview.ObjectType)
            ? (0, GetCeWallPortalOrderingOffsetY(objectPreview.CurrentArtId))
            : (objectPreview.OffsetX, objectPreview.OffsetY);

    private static bool UsesCeWallPortalOrdering(ObjectType objectType) =>
        objectType is ObjectType.Wall or ObjectType.Portal;

    private static int GetCeWallPortalOrderingOffsetY(ArtId artId)
    {
        var rotationIndex = (int)((artId.Value >> 11) & 0x7u);
        return rotationIndex is > 1 and < 6 ? 19 : -20;
    }

    private static int GetObjectTypeSortPriority(ObjectType objectType) => objectType is ObjectType.Portal ? 1 : 0;

    internal static double GetTileOverlaySortKey(long tileDrawOrder, EditorMapTileOverlayKind kind) =>
        (tileDrawOrder * 4096d) + 1024d + (int)kind;

    internal static double GetRoofSortKey(long baseTileDrawOrder) => (baseTileDrawOrder * 4096d) + 3072d;

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
        EditorMapObjectPreview objectPreview,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        var spriteBounds = objectPreview.SpriteBounds;
        if (spriteBounds is null)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX);
            maxBottom = Math.Max(maxBottom, anchorY);
            return;
        }

        var (centerX, centerY) = GetLayoutSpriteCenter(objectPreview, spriteBounds);
        var left = anchorX - centerX;
        var top = anchorY - centerY;
        var right = left + spriteBounds.MaxFrameWidth;
        var bottom = top + spriteBounds.MaxFrameHeight;

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    internal static void ExpandObjectBounds(
        EditorMapPlacementPreviewObject previewObject,
        double anchorX,
        double anchorY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        ArgumentNullException.ThrowIfNull(previewObject);

        var spriteBounds = previewObject.SpriteBounds;
        if (spriteBounds is null)
        {
            minLeft = Math.Min(minLeft, anchorX);
            minTop = Math.Min(minTop, anchorY);
            maxRight = Math.Max(maxRight, anchorX);
            maxBottom = Math.Max(maxBottom, anchorY);
            return;
        }

        var (centerX, centerY) = GetLayoutSpriteCenter(
            previewObject.ObjectType,
            previewObject.CurrentArtId,
            spriteBounds
        );
        var left = anchorX - centerX;
        var top = anchorY - centerY;
        var right = left + spriteBounds.MaxFrameWidth;
        var bottom = top + spriteBounds.MaxFrameHeight;

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    public static (int CenterX, int CenterY) GetLayoutSpriteCenter(
        EditorMapObjectPreview objectPreview,
        EditorMapObjectSpriteBounds spriteBounds
    ) => GetLayoutSpriteCenter(objectPreview.ObjectType, objectPreview.CurrentArtId, spriteBounds);

    public static (int CenterX, int CenterY) GetLayoutSpriteCenter(
        ObjectType objectType,
        ArtId artId,
        EditorMapObjectSpriteBounds spriteBounds
    )
    {
        return (spriteBounds.MaxFrameCenterX, spriteBounds.MaxFrameCenterY);
    }

    internal static (double AnchorX, double AnchorY) ProjectRoofAnchor(
        EditorMapSceneViewMode viewMode,
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int topMapTileY
    )
    {
        return viewMode switch
        {
            EditorMapSceneViewMode.TopDown => (-mapTileX * tileWidthPixels, topMapTileY * tileHeightPixels),
            EditorMapSceneViewMode.Isometric => ProjectIsometricRoofAnchor(
                tileWidthPixels,
                tileHeightPixels,
                mapTileX,
                topMapTileY
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
    }

    private static (double AnchorX, double AnchorY) ProjectIsometricRoofAnchor(
        double tileWidthPixels,
        double tileHeightPixels,
        int mapTileX,
        int topMapTileY
    )
    {
        var normalizedMapTileX = checked(mapTileX + 2);
        var normalizedMapTileY = checked(topMapTileY + 2);
        var (centerX, centerY) = ProjectTileCenter(
            EditorMapSceneViewMode.Isometric,
            tileWidthPixels,
            tileHeightPixels,
            normalizedMapTileX,
            normalizedMapTileY
        );
        return (centerX - (tileWidthPixels * 2d), centerY - (tileHeightPixels * 5.5d));
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
