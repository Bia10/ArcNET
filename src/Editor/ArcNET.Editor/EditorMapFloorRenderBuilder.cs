using System.Collections.Concurrent;
using System.Numerics;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Builds render-ready floor-tile projections from host-neutral scene previews.
/// Sectors are processed in parallel and tile/roof iteration is accelerated with precomputed dense-tile bitmasks.
/// </summary>
public static class EditorMapFloorRenderBuilder
{
    private const int WallTransparencyLeft = 0x0002;
    private const int WallTransparencyRight = 0x0004;
    private const int WallTransDisallow = 0x0001;

    private static readonly bool[,,] RoofCoverageMatrix =
    {
        {
            { false, false, false, false },
            { false, false, false, false },
            { true, true, true, false },
            { true, true, true, false },
        },
        {
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, false },
        },
        {
            { false, false, false, false },
            { false, false, false, false },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { true, true, true, false },
            { true, true, true, false },
            { true, true, true, false },
            { false, false, false, false },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, false },
        },
        {
            { false, false, true, true },
            { false, false, true, true },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { false, false, false, false },
            { false, false, false, false },
            { false, false, true, true },
            { false, false, true, true },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
        },
        {
            { false, false, true, true },
            { false, false, true, true },
            { false, false, true, true },
            { false, false, false, false },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { false, false, false, false },
        },
        {
            { false, false, true, true },
            { false, false, true, true },
            { false, false, true, true },
            { false, false, true, true },
        },
        {
            { true, true, true, true },
            { true, true, true, true },
            { true, true, true, true },
            { false, false, true, true },
        },
    };

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
        ObjectFlags Flags,
        int WallFlags,
        SceneryFlags SceneryFlags,
        int MapTileX,
        int MapTileY,
        Location Tile,
        long BaseTileDrawOrder,
        int SameTileOrder,
        double AnchorX,
        double AnchorY,
        EditorMapObjectSpriteBounds? SpriteBounds,
        bool IsTileGridSnapped,
        float Rotation,
        int RotationIndex,
        int BlitScale,
        int BlitFlags,
        uint BlitColor,
        int BlitAlpha,
        bool IsShrunk,
        float RotationPitch,
        bool IsRoofCovered = false,
        ArtId LightAid = default,
        Color? LightColor = null
    );

    private sealed record RawRoofRenderItem(
        string SectorAssetPath,
        Location RoofCell,
        int MapTileX,
        int MapTileY,
        ArtId ArtId,
        long BaseTileDrawOrder,
        double AnchorX,
        double AnchorY
    );

    private sealed record RawAuxiliaryRenderItem(
        string SectorAssetPath,
        GameObjectGuid ParentObjectId,
        ObjectType ParentObjectType,
        EditorMapCommittedRenderLayer CommittedRenderLayer,
        ArtId ArtId,
        EditorMapObjectAuxiliaryRenderLayer Layer,
        int SlotOrder,
        int MapTileX,
        int MapTileY,
        Location Tile,
        long ParentBaseTileDrawOrder,
        int ParentSameTileOrder,
        double AnchorX,
        double AnchorY,
        int RotationIndex,
        int ScalePercent,
        bool IsShrunk,
        bool IsParentDead,
        bool IsRoofCovered,
        uint? SuggestedTintColor = null,
        EditorMapSpriteBlendMode BlendMode = EditorMapSpriteBlendMode.SourceOver
    );

    private sealed class SectorAccumulator
    {
        public readonly List<RawTileRenderItem> RawTiles = [];
        public readonly List<RawTileOverlayRenderItem> RawTileOverlays = [];
        public readonly List<RawObjectRenderItem> RawObjects = [];
        public readonly List<RawRoofRenderItem> RawRoofs = [];
        public readonly List<RawAuxiliaryRenderItem> RawAuxiliaries = [];
        public readonly List<EditorMapLightRenderItem> RawLights = [];
        public double MinLeft = double.PositiveInfinity;
        public double MinTop = double.PositiveInfinity;
        public double MaxRight = double.NegativeInfinity;
        public double MaxBottom = double.NegativeInfinity;
    }

    private sealed class SceneSectorLookup(
        IReadOnlyList<EditorMapSectorScenePreview> sectors,
        int sectorTileWidth,
        int sectorTileHeight
    )
    {
        private readonly Dictionary<(int LocalX, int LocalY), EditorMapSectorScenePreview> _sectorsByLocalCoordinate =
            sectors.ToDictionary(static sector => (sector.LocalX, sector.LocalY));

        public bool TryGetSectorTile(
            int mapTileX,
            int mapTileY,
            out EditorMapSectorScenePreview sector,
            out int localTileX,
            out int localTileY
        )
        {
            var localX = FloorDivide(mapTileX, sectorTileWidth);
            var localY = FloorDivide(mapTileY, sectorTileHeight);
            localTileX = PositiveModulo(mapTileX, sectorTileWidth);
            localTileY = PositiveModulo(mapTileY, sectorTileHeight);

            if (_sectorsByLocalCoordinate.TryGetValue((localX, localY), out sector!))
                return true;

            sector = null!;
            return false;
        }
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
        var sceneSectorLookup = new SceneSectorLookup(sectors, sectorTileWidth, sectorTileHeight);

        // Phase 1: Collect raw render items in parallel across sectors using lock-free accumulators.
        var accumulators = new ConcurrentBag<SectorAccumulator>();

        var parallelResult = Parallel.ForEach(
            sectors,
            new ParallelOptions { CancellationToken = cancellationToken },
            () => new SectorAccumulator(),
            (sector, _, _, local) =>
            {
                ProcessSector(
                    sector,
                    request,
                    sectorTileWidth,
                    sectorTileHeight,
                    mapTileWidth,
                    sceneSectorLookup,
                    local
                );
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
        var totalAuxiliaryCount = 0;
        var totalLightCount = 0;
        foreach (var acc in accumulators)
        {
            totalTileCount += acc.RawTiles.Count;
            totalOverlayCount += acc.RawTileOverlays.Count;
            totalObjectCount += acc.RawObjects.Count;
            totalRoofCount += acc.RawRoofs.Count;
            totalAuxiliaryCount += acc.RawAuxiliaries.Count;
            totalLightCount += acc.RawLights.Count;
        }

        var rawTiles = new List<RawTileRenderItem>(totalTileCount);
        var rawTileOverlays = new List<RawTileOverlayRenderItem>(totalOverlayCount);
        var rawObjects = new List<RawObjectRenderItem>(totalObjectCount);
        var rawRoofs = new List<RawRoofRenderItem>(totalRoofCount);
        var rawAuxiliaries = new List<RawAuxiliaryRenderItem>(totalAuxiliaryCount);
        var rawLights = new List<EditorMapLightRenderItem>(totalLightCount);
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
            rawAuxiliaries.AddRange(local.RawAuxiliaries);
            rawLights.AddRange(local.RawLights);

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

        SortRawItems(rawTiles, rawTileOverlays, rawObjects, rawRoofs, rawAuxiliaries);
        cancellationToken.ThrowIfCancellationRequested();

        return BuildResult(
            scenePreview.MapName,
            request,
            rawTiles,
            rawTileOverlays,
            rawObjects,
            rawRoofs,
            rawAuxiliaries,
            rawLights,
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
        _ = changedSectorAssetPath;
        return Build(scenePreview, request, cancellationToken);
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
            EditorMapObjectAuxiliaryRenderItem a => string.Equals(
                a.SectorAssetPath,
                sectorAssetPath,
                StringComparison.OrdinalIgnoreCase
            ),
            _ => false,
        };

    private static EditorMapCommittedRenderLayer GetCommittedRenderLayer(ObjectType objectType, ObjectFlags flags) =>
        flags.HasFlag(ObjectFlags.Flat)
            ? EditorMapCommittedRenderLayer.GroundDecal
            : objectType switch
            {
                ObjectType.Wall => EditorMapCommittedRenderLayer.Wall,
                ObjectType.Portal => EditorMapCommittedRenderLayer.Wall,
                ObjectType.Scenery => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Container => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Pc => EditorMapCommittedRenderLayer.Mobile,
                ObjectType.Npc => EditorMapCommittedRenderLayer.Mobile,
                ObjectType.Projectile => EditorMapCommittedRenderLayer.Mobile,
                ObjectType.Weapon => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Ammo => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Armor => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Gold => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Food => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Scroll => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Key => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.KeyRing => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Written => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Generic => EditorMapCommittedRenderLayer.Scenery,
                ObjectType.Trap => EditorMapCommittedRenderLayer.Scenery,
                _ => EditorMapCommittedRenderLayer.Ground,
            };

    private static void GenerateAuxiliaryItems(
        string sectorAssetPath,
        EditorMapObjectPreview obj,
        EditorMapCommittedRenderLayer committedLayer,
        int mapTileX,
        int mapTileY,
        Location tile,
        double anchorX,
        double anchorY,
        long parentBaseTileDrawOrder,
        int parentSameTileOrder,
        SectorAccumulator local,
        bool isRoofCovered
    )
    {
        var rotationIndex = obj.RotationIndex;
        var scalePercent = obj.BlitScale;
        var isShrunk = obj.IsShrunk;

        for (var i = 0; i < obj.UnderlayArtIds.Count; i++)
        {
            var artId = new ArtId(unchecked((uint)obj.UnderlayArtIds[i]));
            if (artId.Value == 0)
                continue;

            var isReactionUnderlay = artId.Value == 433;
            uint? suggestedTintColor = isReactionUnderlay ? obj.ReactionColor : null;
            var blendMode = EditorMapSpriteBlendMode.SourceOver;

            if (artId.Type is ArtId.TypeCode.Light)
            {
                blendMode = EditorMapSpriteBlendMode.Add;
                suggestedTintColor = obj.LightColor is not null
                    ? 0xFF000000u
                        | ((uint)obj.LightColor.Value.R << 16)
                        | ((uint)obj.LightColor.Value.G << 8)
                        | (uint)obj.LightColor.Value.B
                    : 0xFFFFFFFFu;
            }

            local.RawAuxiliaries.Add(
                new RawAuxiliaryRenderItem(
                    SectorAssetPath: sectorAssetPath,
                    ParentObjectId: obj.ObjectId,
                    ParentObjectType: obj.ObjectType,
                    CommittedRenderLayer: committedLayer,
                    ArtId: artId,
                    Layer: EditorMapObjectAuxiliaryRenderLayer.Underlay,
                    SlotOrder: i,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: tile,
                    ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                    ParentSameTileOrder: parentSameTileOrder,
                    AnchorX: anchorX,
                    AnchorY: anchorY,
                    RotationIndex: rotationIndex,
                    ScalePercent: isReactionUnderlay ? 100 : scalePercent,
                    IsShrunk: !isReactionUnderlay && isShrunk,
                    IsParentDead: obj.IsDead,
                    IsRoofCovered: isRoofCovered,
                    SuggestedTintColor: suggestedTintColor,
                    BlendMode: blendMode
                )
            );
        }

        if (!obj.IsFlat && obj.ShadowArtId.Value != 0)
        {
            local.RawAuxiliaries.Add(
                new RawAuxiliaryRenderItem(
                    SectorAssetPath: sectorAssetPath,
                    ParentObjectId: obj.ObjectId,
                    ParentObjectType: obj.ObjectType,
                    CommittedRenderLayer: committedLayer,
                    ArtId: obj.ShadowArtId,
                    Layer: EditorMapObjectAuxiliaryRenderLayer.Shadow,
                    SlotOrder: 0,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: tile,
                    ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                    ParentSameTileOrder: parentSameTileOrder,
                    AnchorX: anchorX,
                    AnchorY: anchorY,
                    RotationIndex: rotationIndex,
                    ScalePercent: scalePercent,
                    IsShrunk: isShrunk,
                    IsParentDead: obj.IsDead,
                    IsRoofCovered: isRoofCovered,
                    SuggestedTintColor: obj.IsWading ? 0xFF5C5C5C : null,
                    BlendMode: EditorMapSpriteBlendMode.Subtract
                )
            );
        }

        var overlaySlotOrder = 0;
        var overlaySlotCount = Math.Max(obj.OverlayForeArtIds.Count, obj.OverlayBackArtIds.Count);
        for (var slotIndex = overlaySlotCount - 1; slotIndex >= 0; slotIndex--)
        {
            var foreArtId =
                slotIndex < obj.OverlayForeArtIds.Count
                    ? new ArtId(unchecked((uint)obj.OverlayForeArtIds[slotIndex]))
                    : default;
            if (foreArtId.Value != 0)
            {
                uint? suggestedTintColor = null;
                var blendMode = EditorMapSpriteBlendMode.SourceOver;

                if (foreArtId.Type is ArtId.TypeCode.Light)
                {
                    blendMode = EditorMapSpriteBlendMode.Add;
                    suggestedTintColor = obj.LightColor is not null
                        ? 0xFF000000u
                            | ((uint)obj.LightColor.Value.R << 16)
                            | ((uint)obj.LightColor.Value.G << 8)
                            | (uint)obj.LightColor.Value.B
                        : 0xFFFFFFFFu;
                }

                local.RawAuxiliaries.Add(
                    new RawAuxiliaryRenderItem(
                        SectorAssetPath: sectorAssetPath,
                        ParentObjectId: obj.ObjectId,
                        ParentObjectType: obj.ObjectType,
                        CommittedRenderLayer: committedLayer,
                        ArtId: foreArtId,
                        Layer: EditorMapObjectAuxiliaryRenderLayer.OverlayFore,
                        SlotOrder: overlaySlotOrder++,
                        MapTileX: mapTileX,
                        MapTileY: mapTileY,
                        Tile: tile,
                        ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                        ParentSameTileOrder: parentSameTileOrder,
                        AnchorX: anchorX,
                        AnchorY: anchorY,
                        RotationIndex: rotationIndex,
                        ScalePercent: scalePercent,
                        IsShrunk: isShrunk,
                        IsParentDead: obj.IsDead,
                        IsRoofCovered: isRoofCovered,
                        SuggestedTintColor: suggestedTintColor,
                        BlendMode: blendMode
                    )
                );
            }

            var backArtId =
                slotIndex < obj.OverlayBackArtIds.Count
                    ? new ArtId(unchecked((uint)obj.OverlayBackArtIds[slotIndex]))
                    : default;
            if (backArtId.Value == 0)
                continue;

            uint? backSuggestedTintColor = null;
            var backBlendMode = EditorMapSpriteBlendMode.SourceOver;

            if (backArtId.Type is ArtId.TypeCode.Light)
            {
                backBlendMode = EditorMapSpriteBlendMode.Add;
                backSuggestedTintColor = obj.LightColor is not null
                    ? 0xFF000000u
                        | ((uint)obj.LightColor.Value.R << 16)
                        | ((uint)obj.LightColor.Value.G << 8)
                        | (uint)obj.LightColor.Value.B
                    : 0xFFFFFFFFu;
            }

            local.RawAuxiliaries.Add(
                new RawAuxiliaryRenderItem(
                    SectorAssetPath: sectorAssetPath,
                    ParentObjectId: obj.ObjectId,
                    ParentObjectType: obj.ObjectType,
                    CommittedRenderLayer: committedLayer,
                    ArtId: backArtId,
                    Layer: EditorMapObjectAuxiliaryRenderLayer.OverlayBack,
                    SlotOrder: overlaySlotOrder++,
                    MapTileX: mapTileX,
                    MapTileY: mapTileY,
                    Tile: tile,
                    ParentBaseTileDrawOrder: parentBaseTileDrawOrder,
                    ParentSameTileOrder: parentSameTileOrder,
                    AnchorX: anchorX,
                    AnchorY: anchorY,
                    RotationIndex: rotationIndex,
                    ScalePercent: scalePercent,
                    IsShrunk: isShrunk,
                    IsParentDead: obj.IsDead,
                    IsRoofCovered: isRoofCovered,
                    SuggestedTintColor: backSuggestedTintColor,
                    BlendMode: backBlendMode
                )
            );
        }
    }

    private static void ProcessSector(
        EditorMapSectorScenePreview sector,
        EditorMapFloorRenderRequest request,
        int sectorTileWidth,
        int sectorTileHeight,
        int mapTileWidth,
        SceneSectorLookup sceneSectorLookup,
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
            var sameTileOrders = BuildCeSameTileOrders(sector.Objects);
            for (var objectIndex = 0; objectIndex < sector.Objects.Count; objectIndex++)
            {
                var obj = sector.Objects[objectIndex];
                if (obj.Location is not { } location)
                    continue;

                // CE editor mode shows ALL objects regardless of visibility flags.
                // Do not filter OF_INVISIBLE here — the flag is only meaningful in gameplay mode.

                var mapTileX = checked((sector.LocalX * sectorTileWidth) + location.X);
                var mapTileY = checked((sector.LocalY * sectorTileHeight) + location.Y);
                var isRoofCovered = request.IncludeRoofs && IsRoofCovered(sceneSectorLookup, mapTileX, mapTileY);

                if (ShouldHideTransparentWallUnderFadedRoof(obj, mapTileX, mapTileY, sceneSectorLookup))
                    continue;

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
                    request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d,
                    request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d,
                    obj,
                    anchorX,
                    anchorY,
                    ref local.MinLeft,
                    ref local.MinTop,
                    ref local.MaxRight,
                    ref local.MaxBottom
                );

                var committedLayer = GetCommittedRenderLayer(obj.ObjectType, obj.Flags);

                local.RawObjects.Add(
                    new RawObjectRenderItem(
                        SectorAssetPath: sector.AssetPath,
                        ObjectId: obj.ObjectId,
                        ProtoId: obj.ProtoId,
                        ObjectType: obj.ObjectType,
                        CurrentArtId: obj.CurrentArtId,
                        Flags: obj.Flags,
                        WallFlags: obj.WallFlags,
                        SceneryFlags: obj.SceneryFlags,
                        MapTileX: mapTileX,
                        MapTileY: mapTileY,
                        Tile: location,
                        BaseTileDrawOrder: baseTileDrawOrder,
                        SameTileOrder: sameTileOrders[objectIndex],
                        AnchorX: anchorX,
                        AnchorY: anchorY,
                        SpriteBounds: obj.SpriteBounds,
                        IsTileGridSnapped: obj.IsTileGridSnapped,
                        Rotation: obj.Rotation,
                        RotationIndex: obj.RotationIndex,
                        BlitScale: obj.BlitScale,
                        BlitFlags: obj.BlitFlags,
                        BlitColor: obj.BlitColor,
                        BlitAlpha: obj.BlitAlpha,
                        IsShrunk: obj.IsShrunk,
                        RotationPitch: obj.RotationPitch,
                        IsRoofCovered: isRoofCovered,
                        LightAid: obj.LightAid,
                        LightColor: obj.LightColor
                    )
                );

                // Generate auxiliary layer items (underlays, shadows, overlays).
                // CE sub_443620() applies rect->y += 15 for wading objects to ALL eye candy rects.
                // Scale the 15px CE offset to ArcNET's tile height.
                var auxiliaryAnchorY =
                    obj.IsWading && !obj.Flags.HasFlag(ObjectFlags.WaterWalking)
                        ? anchorY + (15d * request.TileHeightPixels / 40d)
                        : anchorY;
                GenerateAuxiliaryItems(
                    sector.AssetPath,
                    obj,
                    committedLayer,
                    mapTileX,
                    mapTileY,
                    location,
                    anchorX,
                    auxiliaryAnchorY,
                    baseTileDrawOrder,
                    sameTileOrders[objectIndex],
                    local,
                    isRoofCovered
                );

                if (request.IncludeLightOverlays)
                {
                    if (obj.LightAid.Value != 0)
                    {
                        var scaleX =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
                        var scaleY =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
                        double lightCenterX = tileCenterX + (obj.OffsetX * scaleX);
                        double lightCenterY = tileCenterY + (obj.OffsetY * scaleY);
                        var suggestedTintColor = obj.LightColor is not null
                            ? 0xFF000000u
                                | ((uint)obj.LightColor.Value.R << 16)
                                | ((uint)obj.LightColor.Value.G << 8)
                                | obj.LightColor.Value.B
                            : 0xFFFFFFFFu;

                        local.RawLights.Add(
                            new EditorMapLightRenderItem
                            {
                                SectorAssetPath = sector.AssetPath,
                                MapTileX = mapTileX,
                                MapTileY = mapTileY,
                                Tile = location,
                                ArtId = obj.LightAid,
                                DrawOrder = 0,
                                AnchorX = lightCenterX,
                                AnchorY = lightCenterY,
                                SuggestedTintColor = suggestedTintColor,
                                SuggestedOpacity = 0.4d,
                                Flags = (SectorLightFlags)obj.LightFlags,
                            }
                        );
                    }

                    for (var i = 0; i < obj.OverlayLights.Count; i++)
                    {
                        var overlayLight = obj.OverlayLights[i];
                        var scaleX =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
                        var scaleY =
                            request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
                        double lightCenterX = tileCenterX + (obj.OffsetX * scaleX);
                        double lightCenterY = tileCenterY + (obj.OffsetY * scaleY);
                        var suggestedTintColor = overlayLight.Color is not null
                            ? 0xFF000000u
                                | ((uint)overlayLight.Color.Value.R << 16)
                                | ((uint)overlayLight.Color.Value.G << 8)
                                | overlayLight.Color.Value.B
                            : 0xFFFFFFFFu;

                        local.RawLights.Add(
                            new EditorMapLightRenderItem
                            {
                                SectorAssetPath = sector.AssetPath,
                                MapTileX = mapTileX,
                                MapTileY = mapTileY,
                                Tile = location,
                                ArtId = overlayLight.ArtId,
                                DrawOrder = 0,
                                AnchorX = lightCenterX,
                                AnchorY = lightCenterY,
                                SuggestedTintColor = suggestedTintColor,
                                SuggestedOpacity = 0.4d,
                                Flags = (SectorLightFlags)overlayLight.Flags,
                            }
                        );
                    }
                }
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

        // Sector lights
        if (request.IncludeLightOverlays)
        {
            for (var i = 0; i < sector.Lights.Count; i++)
            {
                var lightPreview = sector.Lights[i];
                var localTileX = lightPreview.TileX % 64;
                var localTileY = lightPreview.TileY % 64;
                var mapTileX = checked((sector.LocalX * sectorTileWidth) + localTileX);
                var mapTileY = checked((sector.LocalY * sectorTileHeight) + localTileY);

                var (tileCenterX, tileCenterY) = ProjectTileCenter(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    mapTileX,
                    mapTileY
                );

                var scaleX = request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileWidthPixels / 80d : 1d;
                var scaleY = request.ViewMode is EditorMapSceneViewMode.Isometric ? request.TileHeightPixels / 40d : 1d;
                double lightCenterX = tileCenterX + (lightPreview.OffsetX * scaleX);
                double lightCenterY = tileCenterY + (lightPreview.OffsetY * scaleY);

                local.RawLights.Add(
                    new EditorMapLightRenderItem
                    {
                        SectorAssetPath = sector.AssetPath,
                        MapTileX = mapTileX,
                        MapTileY = mapTileY,
                        Tile = new Location(checked((short)localTileX), checked((short)localTileY)),
                        ArtId = lightPreview.ArtId,
                        DrawOrder = 0,
                        AnchorX = lightCenterX,
                        AnchorY = lightCenterY,
                        SuggestedTintColor = lightPreview.TintColor | 0xFF000000u,
                        SuggestedOpacity = 0.4d,
                        Flags = lightPreview.Flags,
                    }
                );
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

        var roof = new ArtId(roofArtId.Value);
        if (roof.IsRoofFill)
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
                ArtId: roof,
                BaseTileDrawOrder: baseDrawOrder,
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
            IncludeEditorObjectStateTint = request.IncludeEditorObjectStateTint,
            IncludeFloorLightTint = request.IncludeFloorLightTint,
        };

    private static int[] BuildCeSameTileOrders(IReadOnlyList<EditorMapObjectPreview> objects)
    {
        var sameTileOrders = new int[objects.Count];
        var orderedObjectIndicesByTile = new Dictionary<int, List<int>>();

        for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++)
        {
            var obj = objects[objectIndex];
            if (obj.Location is not { } location)
                continue;

            var tileIndex = GetTileIndex(location.X, location.Y);
            if (!orderedObjectIndicesByTile.TryGetValue(tileIndex, out var objectIndices))
            {
                objectIndices = [];
                orderedObjectIndicesByTile[tileIndex] = objectIndices;
            }

            InsertCeSameTileObject(objectIndices, objects, objectIndex);
        }

        foreach (var objectIndices in orderedObjectIndicesByTile.Values)
        {
            for (var sameTileOrder = 0; sameTileOrder < objectIndices.Count; sameTileOrder++)
                sameTileOrders[objectIndices[sameTileOrder]] = sameTileOrder;
        }

        return sameTileOrders;
    }

    private static void InsertCeSameTileObject(
        IList<int> orderedObjectIndices,
        IReadOnlyList<EditorMapObjectPreview> objects,
        int newObjectIndex
    )
    {
        var newObject = objects[newObjectIndex];
        var (newPrimary, newSecondary) = GetObjectTileOrderComponents(newObject);

        for (var index = 0; index < orderedObjectIndices.Count; index++)
        {
            var existingObject = objects[orderedObjectIndices[index]];

            if (newObject.IsFlat)
            {
                if (!existingObject.IsFlat || newObject.IsUnderAllScenery)
                {
                    orderedObjectIndices.Insert(index, newObjectIndex);
                    return;
                }
            }
            else
            {
                if (newObject.ObjectType is ObjectType.Wall && existingObject.ObjectType is ObjectType.Portal)
                {
                    orderedObjectIndices.Insert(index, newObjectIndex);
                    return;
                }

                if (newObject.ObjectType is ObjectType.Portal && existingObject.ObjectType is ObjectType.Wall)
                {
                    orderedObjectIndices.Insert(index + 1, newObjectIndex);
                    return;
                }
            }

            if (existingObject.IsFlat)
                continue;

            var (existingPrimary, existingSecondary) = GetObjectTileOrderComponents(existingObject);
            if (newPrimary < existingPrimary || (newPrimary == existingPrimary && newSecondary < existingSecondary))
            {
                orderedObjectIndices.Insert(index, newObjectIndex);
                return;
            }
        }

        orderedObjectIndices.Add(newObjectIndex);
    }

    private static bool IsRoofCovered(SceneSectorLookup sceneSectorLookup, int mapTileX, int mapTileY)
    {
        var coveredTileX = mapTileX + 3;
        var coveredTileY = mapTileY + 3;
        if (
            !sceneSectorLookup.TryGetSectorTile(
                coveredTileX,
                coveredTileY,
                out var roofSector,
                out var roofLocalTileX,
                out var roofLocalTileY
            )
        )
        {
            return false;
        }

        var roofArtId = roofSector.GetRoofArtId(roofLocalTileX / 4, roofLocalTileY / 4);
        if (roofArtId is null or 0u or uint.MaxValue)
            return false;

        var roof = new ArtId(roofArtId.Value);
        if (roof.IsRoofFill)
            return false;

        if (roof.FrameIndex is < 0 or >= 13)
            return false;

        var row = PositiveModulo(coveredTileY, 4);
        var col = PositiveModulo(coveredTileX, 4);
        if (roof.IsRoofMirrored)
            col = 3 - col;

        return RoofCoverageMatrix[roof.FrameIndex, row, col];
    }

    private static bool ShouldHideTransparentWallUnderFadedRoof(
        EditorMapObjectPreview objectPreview,
        int mapTileX,
        int mapTileY,
        SceneSectorLookup sceneSectorLookup
    ) =>
        objectPreview.ObjectType is ObjectType.Wall
        && (objectPreview.WallFlags & (WallTransparencyLeft | WallTransparencyRight)) != 0
        && (objectPreview.WallFlags & WallTransDisallow) == 0
        && !UsesNonCardinalWallRotation(objectPreview.CurrentArtId)
        && IsRoofFaded(sceneSectorLookup, mapTileX, mapTileY);

    private static bool IsRoofFaded(SceneSectorLookup sceneSectorLookup, int mapTileX, int mapTileY)
    {
        var coveredTileX = mapTileX + 3;
        var coveredTileY = mapTileY + 3;
        if (
            !sceneSectorLookup.TryGetSectorTile(
                coveredTileX,
                coveredTileY,
                out var sector,
                out var localTileX,
                out var localTileY
            )
        )
            return false;

        var roofArtId = sector.GetRoofArtId(localTileX / 4, localTileY / 4);
        if (roofArtId is null or 0u or uint.MaxValue)
            return false;

        var roof = new ArtId(roofArtId.Value);
        return !roof.IsRoofFill && roof.IsRoofFaded;
    }

    private static bool UsesNonCardinalWallRotation(ArtId artId)
    {
        var rotationIndex = (int)((artId.Value >> 11) & 0x7u);
        return rotationIndex is > 1 and < 6;
    }

    private static int GetAuxiliaryBand(EditorMapObjectAuxiliaryRenderLayer layer) =>
        layer switch
        {
            EditorMapObjectAuxiliaryRenderLayer.Underlay => 0,
            EditorMapObjectAuxiliaryRenderLayer.Shadow => 1,
            _ => 2,
        };

    private static bool IsFlatObject(RawObjectRenderItem item) => item.Flags.HasFlag(ObjectFlags.Flat);

    private static bool IsUnderAllScenery(RawObjectRenderItem item) =>
        item.ObjectType is ObjectType.Scenery && item.SceneryFlags.HasFlag(SceneryFlags.UnderAll);

    private static bool IsGhostOrArmorOverlay(RawAuxiliaryRenderItem item) =>
        (
            item.Layer
            is EditorMapObjectAuxiliaryRenderLayer.OverlayBack
                or EditorMapObjectAuxiliaryRenderLayer.OverlayFore
        )
        && (
            item.ParentObjectType is ObjectType.Armor
            || (item.ParentObjectType is ObjectType.Npc && item.IsParentDead && item.ArtId.ArtNum == 243)
        );

    private sealed record NonFlatSortItem(
        EditorMapRenderQueueItemKind Kind,
        int Index,
        long BaseTileDrawOrder,
        int SameTileOrder,
        int MapTileX,
        int MapTileY,
        int SubOrder,
        int SlotOrder
    );

    private static void SortRawItems(
        List<RawTileRenderItem> rawTiles,
        List<RawTileOverlayRenderItem> rawTileOverlays,
        List<RawObjectRenderItem> rawObjects,
        List<RawRoofRenderItem> rawRoofs,
        List<RawAuxiliaryRenderItem> rawAuxiliaries
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
                var cmp = a.BaseTileDrawOrder.CompareTo(b.BaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.SameTileOrder.CompareTo(b.SameTileOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );

        rawRoofs.Sort(
            (a, b) =>
            {
                var cmp = a.BaseTileDrawOrder.CompareTo(b.BaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );

        rawAuxiliaries.Sort(
            (a, b) =>
            {
                var cmp = GetAuxiliaryBand(a.Layer).CompareTo(GetAuxiliaryBand(b.Layer));
                if (cmp != 0)
                    return cmp;
                cmp = a.ParentBaseTileDrawOrder.CompareTo(b.ParentBaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.ParentSameTileOrder.CompareTo(b.ParentSameTileOrder);
                if (cmp != 0)
                    return cmp;
                cmp = a.SlotOrder.CompareTo(b.SlotOrder);
                if (cmp != 0)
                    return cmp;
                return a.MapTileX.CompareTo(b.MapTileX);
            }
        );
    }

    private static uint SampleLightColor(
        double px,
        double py,
        bool isIndoor,
        IReadOnlyList<EditorMapLightRenderItem> lights
    )
    {
        double accumR = isIndoor ? 128d : 255d;
        double accumG = isIndoor ? 128d : 255d;
        double accumB = isIndoor ? 128d : 255d;

        const double radius = 300.0d;

        foreach (var light in lights)
        {
            if (light.Flags.HasFlag(SectorLightFlags.Off))
                continue;

            if (light.Flags.HasFlag(SectorLightFlags.Indoor) && !isIndoor)
                continue;
            if (light.Flags.HasFlag(SectorLightFlags.Outdoor) && isIndoor)
                continue;

            var dx = px - light.AnchorX;
            var dy = py - light.AnchorY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance >= radius)
                continue;

            var intensity = 1.0d - (distance / radius);

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

    private static EditorMapFloorRenderPreview BuildResult(
        string mapName,
        EditorMapFloorRenderRequest request,
        List<RawTileRenderItem> rawTiles,
        List<RawTileOverlayRenderItem> rawTileOverlays,
        List<RawObjectRenderItem> rawObjects,
        List<RawRoofRenderItem> rawRoofs,
        List<RawAuxiliaryRenderItem> rawAuxiliaries,
        List<EditorMapLightRenderItem> rawLights,
        double offsetX,
        double offsetY,
        double minLeft,
        double maxRight,
        double minTop,
        double maxBottom
    )
    {
        var finalLights = new EditorMapLightRenderItem[rawLights.Count];
        for (var i = 0; i < rawLights.Count; i++)
        {
            var l = rawLights[i];
            finalLights[i] = new EditorMapLightRenderItem
            {
                SectorAssetPath = l.SectorAssetPath,
                MapTileX = l.MapTileX,
                MapTileY = l.MapTileY,
                Tile = l.Tile,
                ArtId = l.ArtId,
                DrawOrder = i,
                AnchorX = l.AnchorX + offsetX,
                AnchorY = l.AnchorY + offsetY,
                SuggestedTintColor = l.SuggestedTintColor,
                SuggestedOpacity = l.SuggestedOpacity,
                Flags = l.Flags,
            };
        }

        Array.Sort(
            finalLights,
            (a, b) =>
            {
                var cmp = a.MapTileX.CompareTo(b.MapTileX);
                return cmp != 0 ? cmp : a.MapTileY.CompareTo(b.MapTileY);
            }
        );

        for (var i = 0; i < finalLights.Length; i++)
        {
            finalLights[i] = new EditorMapLightRenderItem
            {
                SectorAssetPath = finalLights[i].SectorAssetPath,
                MapTileX = finalLights[i].MapTileX,
                MapTileY = finalLights[i].MapTileY,
                Tile = finalLights[i].Tile,
                ArtId = finalLights[i].ArtId,
                DrawOrder = i,
                AnchorX = finalLights[i].AnchorX,
                AnchorY = finalLights[i].AnchorY,
                SuggestedTintColor = finalLights[i].SuggestedTintColor,
                SuggestedOpacity = finalLights[i].SuggestedOpacity,
                Flags = finalLights[i].Flags,
            };
        }

        var tiles = new EditorMapFloorTileRenderItem[rawTiles.Count];
        for (var i = 0; i < rawTiles.Count; i++)
        {
            var t = rawTiles[i];
            uint? suggestedTintColor = null;
            EditorMapTileLightDiagnostics? lightDiagnostics = null;

            if (request.IncludeFloorLightTint)
            {
                var stepX = request.TileWidthPixels / 2d;
                var stepY = request.TileHeightPixels / 2d;
                var isIndoor = t.ArtId.Value == 0 || t.ArtId.TileType == 0;

                var tileCenterX = t.CenterX + offsetX;
                var tileCenterY = t.CenterY + offsetY;

                var topLeft = SampleLightColor(tileCenterX - stepX, tileCenterY - stepY, isIndoor, finalLights);
                var topCenter = SampleLightColor(tileCenterX, tileCenterY - stepY, isIndoor, finalLights);
                var topRight = SampleLightColor(tileCenterX + stepX, tileCenterY - stepY, isIndoor, finalLights);
                var middleLeft = SampleLightColor(tileCenterX - stepX, tileCenterY, isIndoor, finalLights);
                var middleCenter = SampleLightColor(tileCenterX, tileCenterY, isIndoor, finalLights);
                var middleRight = SampleLightColor(tileCenterX + stepX, tileCenterY, isIndoor, finalLights);
                var bottomLeft = SampleLightColor(tileCenterX - stepX, tileCenterY + stepY, isIndoor, finalLights);
                var bottomCenter = SampleLightColor(tileCenterX, tileCenterY + stepY, isIndoor, finalLights);
                var bottomRight = SampleLightColor(tileCenterX + stepX, tileCenterY + stepY, isIndoor, finalLights);

                suggestedTintColor = middleCenter;
                lightDiagnostics = new EditorMapTileLightDiagnostics(
                    TopLeft: topLeft,
                    TopCenter: topCenter,
                    TopRight: topRight,
                    MiddleLeft: middleLeft,
                    MiddleCenter: middleCenter,
                    MiddleRight: middleRight,
                    BottomLeft: bottomLeft,
                    BottomCenter: bottomCenter,
                    BottomRight: bottomRight
                );
            }

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
                SuggestedTintColor = suggestedTintColor,
                LightDiagnostics = lightDiagnostics,
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
                CommittedRenderLayer = GetCommittedRenderLayer(o.ObjectType, o.Flags),
                CurrentArtId = o.CurrentArtId,
                Flags = o.Flags,
                WallFlags = o.WallFlags,
                SceneryFlags = o.SceneryFlags,
                MapTileX = o.MapTileX,
                MapTileY = o.MapTileY,
                Tile = o.Tile,
                DrawOrder = i,
                AnchorX = o.AnchorX + offsetX,
                AnchorY = o.AnchorY + offsetY,
                SpriteBounds = o.SpriteBounds,
                IsTileGridSnapped = o.IsTileGridSnapped,
                Rotation = o.Rotation,
                RotationIndex = o.RotationIndex,
                BlitScale = o.BlitScale,
                BlitFlags = o.BlitFlags,
                BlitColor = o.BlitColor,
                BlitAlpha = o.BlitAlpha,
                IsShrunk = o.IsShrunk,
                RotationPitch = o.RotationPitch,
                IsRoofCovered = o.IsRoofCovered,
                LightAid = o.LightAid,
                LightColor = o.LightColor,
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

        var auxiliaries = new EditorMapObjectAuxiliaryRenderItem[rawAuxiliaries.Count];
        for (var i = 0; i < rawAuxiliaries.Count; i++)
        {
            var a = rawAuxiliaries[i];
            auxiliaries[i] = new EditorMapObjectAuxiliaryRenderItem
            {
                SectorAssetPath = a.SectorAssetPath,
                ParentObjectId = a.ParentObjectId,
                ParentObjectType = a.ParentObjectType,
                CommittedRenderLayer = a.CommittedRenderLayer,
                ArtId = a.ArtId,
                Layer = a.Layer,
                MapTileX = a.MapTileX,
                MapTileY = a.MapTileY,
                Tile = a.Tile,
                DrawOrder = i,
                AnchorX = a.AnchorX + offsetX,
                AnchorY = a.AnchorY + offsetY,
                RotationIndex = a.RotationIndex,
                ScalePercent = a.ScalePercent,
                IsShrunk = a.IsShrunk,
                IsRoofCovered = a.IsRoofCovered,
                SuggestedTintColor = a.SuggestedTintColor,
                BlendMode = a.BlendMode,
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
            roofs,
            rawAuxiliaries,
            auxiliaries,
            finalLights
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
            ObjectAuxiliaryItems = auxiliaries,
            Lights = finalLights,
            RenderQueue = renderQueue,
            IncludeEditorObjectStateTint = request.IncludeEditorObjectStateTint,
            IncludeFloorLightTint = request.IncludeFloorLightTint,
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
        return (tileCenterX + offsetX, tileCenterY + offsetY - offsetZ);
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

        // CE object anchor: base_x = loc_x + offset_x + 40; rect.x = base_x - hot_x
        // Our tileCenterX = 40*(Y-X) = loc_x + 40, so no additional offset is needed.
        // Previously a -40/-20 adjustment was applied for Scenery type which was incorrect.
        var baseOffsetX = (double)objectPreview.OffsetX;
        var baseOffsetY = (double)objectPreview.OffsetY;

        return (baseOffsetX * scaleX, baseOffsetY * scaleY, objectPreview.OffsetZ * scaleY);
    }

    internal static long GetDrawOrder(EditorMapSceneViewMode viewMode, int mapTileWidth, int mapTileX, int mapTileY) =>
        viewMode switch
        {
            EditorMapSceneViewMode.TopDown => checked((((long)mapTileY * 10_000_000L) + mapTileX)),
            EditorMapSceneViewMode.Isometric => checked((((long)mapTileY + mapTileX) * 10_000_000L) + mapTileY),
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

    private static int FloorDivide(int value, int divisor)
    {
        var quotient = value / divisor;
        var remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private static int PositiveModulo(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }

    internal static void ExpandObjectBounds(
        double scaleX,
        double scaleY,
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
        var left = anchorX - (centerX * scaleX);
        var top = anchorY - (centerY * scaleY);
        var right = left + (spriteBounds.MaxFrameWidth * scaleX);
        var bottom = top + (spriteBounds.MaxFrameHeight * scaleY);

        minLeft = Math.Min(minLeft, left);
        minTop = Math.Min(minTop, top);
        maxRight = Math.Max(maxRight, right);
        maxBottom = Math.Max(maxBottom, bottom);
    }

    internal static void ExpandObjectBounds(
        double scaleX,
        double scaleY,
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
        var left = anchorX - (centerX * scaleX);
        var top = anchorY - (centerY * scaleY);
        var right = left + (spriteBounds.MaxFrameWidth * scaleX);
        var bottom = top + (spriteBounds.MaxFrameHeight * scaleY);

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
        if (objectType is not ObjectType.Wall and not ObjectType.Portal)
            return (spriteBounds.MaxFrameCenterX, spriteBounds.MaxFrameCenterY);

        var rotationIndex = NormalizeWallPortalRotationIndex((int)((artId.Value >> 11) & 0x7u));
        var adjustedCenterX = spriteBounds.MaxFrameCenterX;
        var adjustedCenterY = spriteBounds.MaxFrameCenterY;

        // CE tig_art_frame_data applies an extra hotspot shift for the north/south-facing wall and portal
        // families before any mirror flip is evaluated.
        if (rotationIndex is < 2 or > 5)
        {
            adjustedCenterX -= 40;
            adjustedCenterY += 20;
        }

        if ((artId.Value & 0x1u) != 0)
            adjustedCenterX = spriteBounds.MaxFrameWidth - adjustedCenterX - 2;

        return (adjustedCenterX, adjustedCenterY);
    }

    private static int NormalizeWallPortalRotationIndex(int rotationIndex)
    {
        var normalizedRotationIndex = rotationIndex % 8;
        return normalizedRotationIndex < 0 ? normalizedRotationIndex + 8 : normalizedRotationIndex;
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
        IReadOnlyList<EditorMapRoofRenderItem> roofs,
        IReadOnlyList<RawAuxiliaryRenderItem> rawAuxiliaries,
        IReadOnlyList<EditorMapObjectAuxiliaryRenderItem> auxiliaries,
        IReadOnlyList<EditorMapLightRenderItem> lights
    )
    {
        List<(double SortKey, EditorMapRenderQueueItemKind Kind, int Index)> queue = [];

        var sortedTiles = rawTiles
            .Select(static (tile, idx) => (tile, idx))
            .OrderBy(static t => t.tile.DrawOrder)
            .ToArray();
        for (var i = 0; i < sortedTiles.Length; i++)
        {
            // Keep world-map floor phases compact so millions of tiles cannot spill into later object bands.
            var sortKey = -2_000_000_000d + i;
            queue.Add((sortKey, EditorMapRenderQueueItemKind.FloorTile, sortedTiles[i].idx));
        }

        var sortedOverlays = rawTileOverlays
            .Select(static (overlay, idx) => (overlay, idx))
            .OrderBy(static o => o.overlay.SortKey)
            .ToArray();
        for (var i = 0; i < sortedOverlays.Length; i++)
        {
            var sortKey = -1_000_000_000d + (i * 4d) + (int)sortedOverlays[i].overlay.Kind;
            queue.Add((sortKey, EditorMapRenderQueueItemKind.TileOverlay, sortedOverlays[i].idx));
        }

        var underlayCounter = 0d;
        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            if (rawAuxiliaries[index].Layer is not EditorMapObjectAuxiliaryRenderLayer.Underlay)
                continue;

            queue.Add((0d + underlayCounter++, EditorMapRenderQueueItemKind.ObjectAuxiliary, index));
        }

        var underAllCounter = 0d;
        for (var index = 0; index < rawObjects.Count; index++)
        {
            if (!IsUnderAllScenery(rawObjects[index]))
                continue;

            queue.Add((100_000_000d + underAllCounter++, EditorMapRenderQueueItemKind.Object, index));
        }

        var flatCounter = 0d;
        for (var index = 0; index < rawObjects.Count; index++)
        {
            if (!IsFlatObject(rawObjects[index]) || IsUnderAllScenery(rawObjects[index]))
                continue;

            queue.Add((200_000_000d + flatCounter++, EditorMapRenderQueueItemKind.Object, index));
        }

        var shadowCounter = 0d;
        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            if (rawAuxiliaries[index].Layer is not EditorMapObjectAuxiliaryRenderLayer.Shadow)
                continue;

            queue.Add((400_000_000d + shadowCounter++, EditorMapRenderQueueItemKind.ObjectAuxiliary, index));
        }

        var nonFlatList = new List<NonFlatSortItem>();

        for (var index = 0; index < rawObjects.Count; index++)
        {
            if (IsFlatObject(rawObjects[index]))
                continue;

            var obj = rawObjects[index];
            nonFlatList.Add(
                new NonFlatSortItem(
                    Kind: EditorMapRenderQueueItemKind.Object,
                    Index: index,
                    BaseTileDrawOrder: obj.BaseTileDrawOrder,
                    SameTileOrder: obj.SameTileOrder,
                    MapTileX: obj.MapTileX,
                    MapTileY: obj.MapTileY,
                    SubOrder: 0,
                    SlotOrder: 0
                )
            );
        }

        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            var aux = rawAuxiliaries[index];
            if (!IsGhostOrArmorOverlay(aux))
                continue;

            nonFlatList.Add(
                new NonFlatSortItem(
                    Kind: EditorMapRenderQueueItemKind.ObjectAuxiliary,
                    Index: index,
                    BaseTileDrawOrder: aux.ParentBaseTileDrawOrder,
                    SameTileOrder: aux.ParentSameTileOrder,
                    MapTileX: aux.MapTileX,
                    MapTileY: aux.MapTileY,
                    SubOrder: 1,
                    SlotOrder: aux.SlotOrder
                )
            );
        }

        nonFlatList.Sort(
            (a, b) =>
            {
                var cmp = a.BaseTileDrawOrder.CompareTo(b.BaseTileDrawOrder);
                if (cmp != 0)
                    return cmp;

                cmp = a.SameTileOrder.CompareTo(b.SameTileOrder);
                if (cmp != 0)
                    return cmp;

                cmp = a.MapTileX.CompareTo(b.MapTileX);
                if (cmp != 0)
                    return cmp;

                cmp = a.MapTileY.CompareTo(b.MapTileY);
                if (cmp != 0)
                    return cmp;

                cmp = a.SubOrder.CompareTo(b.SubOrder);
                if (cmp != 0)
                    return cmp;

                return a.SlotOrder.CompareTo(b.SlotOrder);
            }
        );

        var nonFlatCounter = 0d;
        foreach (var item in nonFlatList)
        {
            queue.Add((600_000_000d + nonFlatCounter++, item.Kind, item.Index));
        }

        var overlayCounter = 0d;
        for (var index = 0; index < rawAuxiliaries.Count; index++)
        {
            var aux = rawAuxiliaries[index];
            if (
                aux.Layer
                is not (
                    EditorMapObjectAuxiliaryRenderLayer.OverlayBack
                    or EditorMapObjectAuxiliaryRenderLayer.OverlayFore
                )
            )
            {
                continue;
            }

            if (IsGhostOrArmorOverlay(aux))
                continue;

            queue.Add((700_000_000d + overlayCounter++, EditorMapRenderQueueItemKind.ObjectAuxiliary, index));
        }

        var roofCounter = 0d;
        for (var index = 0; index < rawRoofs.Count; index++)
            queue.Add((800_000_000d + roofCounter++, EditorMapRenderQueueItemKind.Roof, index));

        var lightCounter = 0d;
        for (var index = 0; index < lights.Count; index++)
            queue.Add((900_000_000d + lightCounter++, EditorMapRenderQueueItemKind.Light, index));

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
                        EditorMapRenderQueueItemKind.ObjectAuxiliary => new EditorMapRenderQueueItem
                        {
                            Kind = item.Kind,
                            DrawOrder = drawOrder,
                            SortKey = item.SortKey,
                            ObjectAuxiliaryItem = auxiliaries[item.Index],
                            CommittedRenderLayer = auxiliaries[item.Index].CommittedRenderLayer,
                        },
                        EditorMapRenderQueueItemKind.Light => new EditorMapRenderQueueItem
                        {
                            Kind = item.Kind,
                            DrawOrder = drawOrder,
                            SortKey = item.SortKey,
                            Light = lights[item.Index],
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
