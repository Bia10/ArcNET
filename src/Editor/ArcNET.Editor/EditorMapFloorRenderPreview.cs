using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Render-ready floor-tile projection for one map scene preview.
/// Canonical payload ownership lives in <see cref="Slices"/>; legacy lists are exposed through lazy adapters.
/// </summary>
public sealed class EditorMapFloorRenderPreview
{
    private IReadOnlyList<EditorMapFloorTileRenderItem>? _tiles;
    private IReadOnlyList<EditorMapObjectRenderItem>? _objects;
    private IReadOnlyList<EditorMapObjectAuxiliaryRenderItem>? _objectAuxiliaryItems;
    private IReadOnlyList<EditorMapTileOverlayRenderItem>? _overlays;
    private IReadOnlyList<EditorMapLightRenderItem>? _lights;
    private IReadOnlyList<EditorMapRoofRenderItem>? _roofs;
    private IReadOnlyList<EditorMapRenderQueueItem>? _renderQueue;
    private IReadOnlyDictionary<string, EditorMapSectorRenderSlice>? _slicesByAssetPath;
    private IReadOnlyDictionary<GameObjectGuid, EditorMapObjectRenderItem>? _objectsById;
    private IReadOnlyDictionary<GameObjectGuid, int>? _objectDrawOrderById;
    private IReadOnlyList<EditorMapSectorRenderSliceBounds>? _sectorBounds;
    private IReadOnlyDictionary<string, EditorMapSectorRenderSliceBounds>? _virtualTerrainBoundsByAssetPath;
    private EditorMapRenderSpatialIndex? _spatialIndex;

    /// <summary>
    /// Map name that owns the rendered floor preview.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// View mode used to project the floor tiles.
    /// </summary>
    public required EditorMapSceneViewMode ViewMode { get; init; }

    /// <summary>
    /// Width in pixels of one rendered floor tile.
    /// </summary>
    public required double TileWidthPixels { get; init; }

    /// <summary>
    /// Height in pixels of one rendered floor tile.
    /// </summary>
    public required double TileHeightPixels { get; init; }

    /// <summary>
    /// Total normalized preview width in pixels.
    /// </summary>
    public required double WidthPixels { get; init; }

    /// <summary>
    /// Total normalized preview height in pixels.
    /// </summary>
    public required double HeightPixels { get; init; }

    /// <summary>
    /// Stable content revision for the committed render scene. Hosts can key retained rendering by this value;
    /// camera-only changes must not affect it.
    /// </summary>
    public long SceneRevision { get; init; }

    /// <summary>
    /// Canonical slice-backed committed scene payloads grouped by sector.
    /// </summary>
    public IReadOnlyList<EditorMapSectorRenderSlice> Slices { get; init; } = [];

    /// <summary>
    /// Source-sector terrain retained for focused terrain previews.
    /// Hosts can project visible terrain from these dense sector arrays without materializing every tile up front.
    /// </summary>
    public IReadOnlyList<EditorMapSectorScenePreview> VirtualTerrainSectors { get; init; } = [];

    /// <summary>
    /// Normalized sector asset paths whose terrain was already materialized into <see cref="Slices"/>.
    /// </summary>
    public IReadOnlySet<string> MaterializedTerrainSectorAssetPaths { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Render-ready floor tiles in stable draw order.
    /// </summary>
    public IReadOnlyList<EditorMapFloorTileRenderItem> Tiles
    {
        get =>
            _tiles ??= new PackedSliceItemList<EditorMapFloorTileRenderItem>(
                Slices,
                TileOrderMap,
                static slice => slice.Tiles
            );
        init => _tiles = value;
    }

    /// <summary>
    /// Render-ready placed-object anchors in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapObjectRenderItem> Objects
    {
        get =>
            _objects ??= new PackedSliceItemList<EditorMapObjectRenderItem>(
                Slices,
                ObjectOrderMap,
                static slice => slice.Objects
            );
        init => _objects = value;
    }

    /// <summary>
    /// Render-ready auxiliary object layers in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapObjectAuxiliaryRenderItem> ObjectAuxiliaryItems
    {
        get =>
            _objectAuxiliaryItems ??= new PackedSliceItemList<EditorMapObjectAuxiliaryRenderItem>(
                Slices,
                ObjectAuxiliaryOrderMap,
                static slice => slice.ObjectAuxiliaryItems
            );
        init => _objectAuxiliaryItems = value;
    }

    /// <summary>
    /// Render-ready tile overlays in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapTileOverlayRenderItem> Overlays
    {
        get =>
            _overlays ??= new PackedSliceItemList<EditorMapTileOverlayRenderItem>(
                Slices,
                OverlayOrderMap,
                static slice => slice.Overlays
            );
        init => _overlays = value;
    }

    /// <summary>
    /// Render-ready CE light-system masks in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapLightRenderItem> Lights
    {
        get =>
            _lights ??= new PackedSliceItemList<EditorMapLightRenderItem>(
                Slices,
                LightOrderMap,
                static slice => slice.Lights
            );
        init => _lights = value;
    }

    /// <summary>
    /// Render-ready roof cells in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapRoofRenderItem> Roofs
    {
        get =>
            _roofs ??= new PackedSliceItemList<EditorMapRoofRenderItem>(
                Slices,
                RoofOrderMap,
                static slice => slice.Roofs
            );
        init => _roofs = value;
    }

    /// <summary>
    /// Unified render queue for <see cref="Tiles"/>, <see cref="Overlays"/>, <see cref="Objects"/>, and <see cref="Roofs"/>.
    /// </summary>
    public IReadOnlyList<EditorMapRenderQueueItem> RenderQueue
    {
        get => _renderQueue ??= new PackedRenderQueueItemList(Slices, RenderQueueOrderMap);
        init => _renderQueue = value;
    }

    /// <summary>
    /// Compact order map for <see cref="Tiles"/>. The high 16 bits encode the slice index, the low 16 bits the item index.
    /// </summary>
    internal IReadOnlyList<uint> TileOrderMap { get; init; } = [];

    /// <summary>
    /// Compact order map for <see cref="Objects"/>. The high 16 bits encode the slice index, the low 16 bits the item index.
    /// </summary>
    internal IReadOnlyList<uint> ObjectOrderMap { get; init; } = [];

    /// <summary>
    /// Compact order map for <see cref="ObjectAuxiliaryItems"/>. The high 16 bits encode the slice index, the low 16 bits the item index.
    /// </summary>
    internal IReadOnlyList<uint> ObjectAuxiliaryOrderMap { get; init; } = [];

    /// <summary>
    /// Compact order map for <see cref="Overlays"/>. The high 16 bits encode the slice index, the low 16 bits the item index.
    /// </summary>
    internal IReadOnlyList<uint> OverlayOrderMap { get; init; } = [];

    /// <summary>
    /// Compact order map for <see cref="Lights"/>. The high 16 bits encode the slice index, the low 16 bits the item index.
    /// </summary>
    internal IReadOnlyList<uint> LightOrderMap { get; init; } = [];

    /// <summary>
    /// Compact order map for <see cref="Roofs"/>. The high 16 bits encode the slice index, the low 16 bits the item index.
    /// </summary>
    internal IReadOnlyList<uint> RoofOrderMap { get; init; } = [];

    /// <summary>
    /// Compact order map for <see cref="RenderQueue"/>. The high 16 bits encode the slice index, the low 16 bits the queue index.
    /// </summary>
    internal IReadOnlyList<uint> RenderQueueOrderMap { get; init; } = [];

    /// <summary>
    /// Indicates whether committed object renders should expose editor-state tint diagnostics.
    /// </summary>
    public bool IncludeEditorObjectStateTint { get; init; }

    /// <summary>
    /// Indicates whether committed floor tiles should expose floor-light tint diagnostics.
    /// </summary>
    public bool IncludeFloorLightTint { get; init; }

    /// <summary>
    /// Indicates whether virtual terrain should emit zero-art floor tiles.
    /// </summary>
    public bool IncludeEmptyTerrainTiles { get; init; }

    /// <summary>
    /// Indicates whether virtual terrain should emit roof cells.
    /// </summary>
    public bool IncludeTerrainRoofs { get; init; }

    /// <summary>
    /// Indicates whether virtual terrain should emit blocked-tile overlays.
    /// </summary>
    public bool IncludeTerrainBlockedTileOverlays { get; init; }

    /// <summary>
    /// Indicates whether virtual terrain should emit light markers and light tile overlays.
    /// </summary>
    public bool IncludeTerrainLightOverlays { get; init; }

    /// <summary>
    /// Indicates whether virtual terrain should emit tile-script overlays.
    /// </summary>
    public bool IncludeTerrainScriptOverlays { get; init; }

    /// <summary>
    /// Indicates whether virtual terrain should emit jump-point overlays.
    /// </summary>
    public bool IncludeTerrainJumpPointOverlays { get; init; }

    /// <summary>
    /// Ambient-lighting context that was applied while projecting the render.
    /// </summary>
    public EditorMapAmbientLightingState? AmbientLighting { get; init; }

    /// <summary>
    /// Indicates that terrain was intentionally materialized for only a subset of sectors.
    /// Objects may still cover the full map.
    /// </summary>
    public bool IsTerrainMaterializationPartial { get; init; }

    /// <summary>
    /// Number of sectors whose terrain payload was materialized into this render preview.
    /// </summary>
    public int MaterializedTerrainSectorCount { get; init; }

    /// <summary>
    /// Number of sectors available in the source scene preview when this render preview was built.
    /// </summary>
    public int TotalTerrainSectorCount { get; init; }

    /// <summary>
    /// X offset applied to center/anchor coordinates when normalizing into the preview space.
    /// Used by delta builders to reconstruct raw coordinates.
    /// </summary>
    internal double OffsetX { get; init; }

    /// <summary>
    /// Y offset applied to center/anchor coordinates when normalizing into the preview space.
    /// </summary>
    internal double OffsetY { get; init; }

    /// <summary>
    /// Pre-offset minimum left coordinate used for bounds calculation.
    /// </summary>
    internal double RawMinLeft { get; init; }

    /// <summary>
    /// Pre-offset minimum top coordinate used for bounds calculation.
    /// </summary>
    internal double RawMinTop { get; init; }

    /// <summary>
    /// Pre-offset maximum right coordinate used for bounds calculation.
    /// </summary>
    internal double RawMaxRight { get; init; }

    /// <summary>
    /// Pre-offset maximum bottom coordinate used for bounds calculation.
    /// </summary>
    internal double RawMaxBottom { get; init; }

    public int TileCount => TileOrderMap.Count > 0 ? TileOrderMap.Count : Tiles.Count;

    public int ObjectCount => ObjectOrderMap.Count > 0 ? ObjectOrderMap.Count : Objects.Count;

    public int OverlayCount => OverlayOrderMap.Count > 0 ? OverlayOrderMap.Count : Overlays.Count;

    public int RoofCount => RoofOrderMap.Count > 0 ? RoofOrderMap.Count : Roofs.Count;

    public int LightCount => LightOrderMap.Count > 0 ? LightOrderMap.Count : Lights.Count;

    public int ObjectAuxiliaryCount =>
        ObjectAuxiliaryOrderMap.Count > 0 ? ObjectAuxiliaryOrderMap.Count : ObjectAuxiliaryItems.Count;

    public int RenderQueueCount => RenderQueueOrderMap.Count > 0 ? RenderQueueOrderMap.Count : RenderQueue.Count;

    internal EditorMapRenderSpatialIndex GetOrCreateSpatialIndex() =>
        _spatialIndex ??= EditorMapRenderSpatialIndex.Build(this);

    public bool TryGetTile(string sectorAssetPath, Location tile, out EditorMapFloorTileRenderItem? item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorAssetPath);

        if (Slices.Count > 0)
        {
            var slicesByAssetPath = _slicesByAssetPath ??= Slices.ToDictionary(
                static slice => ArcNET.Core.VirtualPath.Normalize(slice.SectorAssetPath),
                StringComparer.OrdinalIgnoreCase
            );
            if (slicesByAssetPath.TryGetValue(ArcNET.Core.VirtualPath.Normalize(sectorAssetPath), out var slice))
                return slice.TryGetTile(tile, out item);

            return TryCreateVirtualTerrainTile(sectorAssetPath, tile, out item);
        }

        for (var index = 0; index < Tiles.Count; index++)
        {
            var candidate = Tiles[index];
            if (
                string.Equals(
                    ArcNET.Core.VirtualPath.Normalize(candidate.SectorAssetPath),
                    ArcNET.Core.VirtualPath.Normalize(sectorAssetPath),
                    StringComparison.OrdinalIgnoreCase
                )
                && candidate.Tile == tile
            )
            {
                item = candidate;
                return true;
            }
        }

        item = null;
        return false;
    }

    public IReadOnlyList<EditorMapObjectRenderItem> GetObjectsAtTile(string? sectorAssetPath, Location tile)
    {
        if (Slices.Count > 0 && !string.IsNullOrWhiteSpace(sectorAssetPath))
        {
            var slicesByAssetPath = _slicesByAssetPath ??= Slices.ToDictionary(
                static slice => ArcNET.Core.VirtualPath.Normalize(slice.SectorAssetPath),
                StringComparer.OrdinalIgnoreCase
            );
            return slicesByAssetPath.TryGetValue(ArcNET.Core.VirtualPath.Normalize(sectorAssetPath), out var slice)
                ? slice.GetObjectsAtTile(tile)
                : [];
        }

        List<EditorMapObjectRenderItem>? objects = null;
        if (Slices.Count > 0)
        {
            for (var index = 0; index < Slices.Count; index++)
            {
                var sliceObjects = Slices[index].GetObjectsAtTile(tile);
                if (sliceObjects.Count == 0)
                    continue;

                objects ??= [];
                objects.AddRange(sliceObjects);
            }

            return objects?.ToArray() ?? [];
        }

        for (var index = 0; index < Objects.Count; index++)
        {
            var candidate = Objects[index];
            if (
                candidate.Tile != tile
                || (
                    !string.IsNullOrWhiteSpace(sectorAssetPath)
                    && !string.Equals(
                        ArcNET.Core.VirtualPath.Normalize(candidate.SectorAssetPath),
                        ArcNET.Core.VirtualPath.Normalize(sectorAssetPath),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                continue;
            }

            objects ??= [];
            objects.Add(candidate);
        }

        return objects?.ToArray() ?? [];
    }

    public bool TryGetObject(GameObjectGuid objectId, out EditorMapObjectRenderItem? item)
    {
        var objectsById = _objectsById ??= BuildObjectLookup();
        return objectsById.TryGetValue(objectId, out item);
    }

    public bool TryGetObjectDrawOrder(GameObjectGuid objectId, out int drawOrder)
    {
        var drawOrdersById = _objectDrawOrderById ??= BuildObjectDrawOrderLookup();
        return drawOrdersById.TryGetValue(objectId, out drawOrder);
    }

    public IReadOnlyList<EditorMapSectorRenderSliceBounds> GetSectorBounds()
    {
        if (_sectorBounds is not null)
            return _sectorBounds;

        if (Slices.Count > 0)
        {
            if (VirtualTerrainSectors.Count == 0)
                return _sectorBounds = [.. Slices.Select(static slice => slice.Bounds)];

            var bounds = new List<EditorMapSectorRenderSliceBounds>(Slices.Count + VirtualTerrainSectors.Count);
            for (var sliceIndex = 0; sliceIndex < Slices.Count; sliceIndex++)
                bounds.Add(Slices[sliceIndex].Bounds);

            for (var sectorIndex = 0; sectorIndex < VirtualTerrainSectors.Count; sectorIndex++)
            {
                var sector = VirtualTerrainSectors[sectorIndex];
                if (!IsTerrainSectorMaterialized(sector.AssetPath))
                    bounds.Add(GetVirtualTerrainSectorBounds(sector));
            }

            return _sectorBounds = bounds;
        }

        return _sectorBounds = [
            .. Tiles
                .GroupBy(static tile => tile.SectorAssetPath, StringComparer.OrdinalIgnoreCase)
                .Select(group => new EditorMapSectorRenderSliceBounds(
                    Left: group.Min(static tile => tile.CenterX - 0.5d),
                    Top: group.Min(static tile => tile.CenterY - 0.5d),
                    Width: Math.Max(
                        0d,
                        group.Max(static tile => tile.CenterX + 0.5d) - group.Min(static tile => tile.CenterX - 0.5d)
                    ),
                    Height: Math.Max(
                        0d,
                        group.Max(static tile => tile.CenterY + 0.5d) - group.Min(static tile => tile.CenterY - 0.5d)
                    ),
                    MinMapTileX: group.Min(static tile => tile.MapTileX),
                    MinMapTileY: group.Min(static tile => tile.MapTileY),
                    MaxMapTileX: group.Max(static tile => tile.MapTileX),
                    MaxMapTileY: group.Max(static tile => tile.MapTileY)
                )),
        ];
    }

    public bool IsTerrainSectorMaterialized(string sectorAssetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorAssetPath);
        return MaterializedTerrainSectorAssetPaths.Contains(ArcNET.Core.VirtualPath.Normalize(sectorAssetPath));
    }

    public bool TryGetVirtualTerrainSector(string sectorAssetPath, out EditorMapSectorScenePreview? sector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorAssetPath);
        var normalizedSectorAssetPath = ArcNET.Core.VirtualPath.Normalize(sectorAssetPath);
        for (var index = 0; index < VirtualTerrainSectors.Count; index++)
        {
            var candidate = VirtualTerrainSectors[index];
            if (
                string.Equals(
                    ArcNET.Core.VirtualPath.Normalize(candidate.AssetPath),
                    normalizedSectorAssetPath,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                sector = candidate;
                return true;
            }
        }

        sector = null;
        return false;
    }

    public EditorMapSectorRenderSliceBounds GetVirtualTerrainSectorBounds(EditorMapSectorScenePreview sector)
    {
        ArgumentNullException.ThrowIfNull(sector);

        var boundsByAssetPath = _virtualTerrainBoundsByAssetPath ??= BuildVirtualTerrainBoundsLookup();
        return boundsByAssetPath.TryGetValue(ArcNET.Core.VirtualPath.Normalize(sector.AssetPath), out var bounds)
            ? bounds
            : CreateVirtualTerrainSectorBounds(sector);
    }

    private IReadOnlyDictionary<string, EditorMapSectorRenderSliceBounds> BuildVirtualTerrainBoundsLookup()
    {
        var boundsByAssetPath = new Dictionary<string, EditorMapSectorRenderSliceBounds>(
            StringComparer.OrdinalIgnoreCase
        );
        for (var index = 0; index < VirtualTerrainSectors.Count; index++)
        {
            var sector = VirtualTerrainSectors[index];
            boundsByAssetPath[ArcNET.Core.VirtualPath.Normalize(sector.AssetPath)] = CreateVirtualTerrainSectorBounds(
                sector
            );
        }

        return boundsByAssetPath;
    }

    private EditorMapSectorRenderSliceBounds CreateVirtualTerrainSectorBounds(EditorMapSectorScenePreview sector)
    {
        var minMapTileX = sector.LocalX * sector.TileWidth;
        var minMapTileY = sector.LocalY * sector.TileHeight;
        var maxMapTileX = minMapTileX + sector.TileWidth - 1;
        var maxMapTileY = minMapTileY + sector.TileHeight - 1;
        var minLeft = double.PositiveInfinity;
        var minTop = double.PositiveInfinity;
        var maxRight = double.NegativeInfinity;
        var maxBottom = double.NegativeInfinity;

        ExpandVirtualTerrainTileBounds(minMapTileX, minMapTileY, ref minLeft, ref minTop, ref maxRight, ref maxBottom);
        ExpandVirtualTerrainTileBounds(minMapTileX, maxMapTileY, ref minLeft, ref minTop, ref maxRight, ref maxBottom);
        ExpandVirtualTerrainTileBounds(maxMapTileX, minMapTileY, ref minLeft, ref minTop, ref maxRight, ref maxBottom);
        ExpandVirtualTerrainTileBounds(maxMapTileX, maxMapTileY, ref minLeft, ref minTop, ref maxRight, ref maxBottom);

        var paddingX = TileWidthPixels * (IncludeTerrainRoofs ? 4d : 1d);
        var paddingY = TileHeightPixels * (IncludeTerrainRoofs ? 6d : 1d);
        minLeft -= paddingX;
        minTop -= paddingY;
        maxRight += paddingX;
        maxBottom += paddingY;

        if (double.IsInfinity(minLeft))
            return new EditorMapSectorRenderSliceBounds(0d, 0d, 0d, 0d, 0, 0, 0, 0);

        return new EditorMapSectorRenderSliceBounds(
            Left: minLeft,
            Top: minTop,
            Width: Math.Max(0d, maxRight - minLeft),
            Height: Math.Max(0d, maxBottom - minTop),
            MinMapTileX: minMapTileX,
            MinMapTileY: minMapTileY,
            MaxMapTileX: maxMapTileX,
            MaxMapTileY: maxMapTileY
        );
    }

    private void ExpandVirtualTerrainTileBounds(
        int mapTileX,
        int mapTileY,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    )
    {
        var (centerX, centerY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
            ViewMode,
            TileWidthPixels,
            TileHeightPixels,
            mapTileX,
            mapTileY
        );
        centerX += OffsetX;
        centerY += OffsetY;

        var halfTileWidth = TileWidthPixels / 2d;
        var halfTileHeight = TileHeightPixels / 2d;
        minLeft = Math.Min(minLeft, centerX - halfTileWidth);
        minTop = Math.Min(minTop, centerY - halfTileHeight);
        maxRight = Math.Max(maxRight, centerX + halfTileWidth);
        maxBottom = Math.Max(maxBottom, centerY + halfTileHeight);
    }

    private bool TryCreateVirtualTerrainTile(
        string sectorAssetPath,
        Location tile,
        out EditorMapFloorTileRenderItem? item
    )
    {
        if (
            IsTerrainSectorMaterialized(sectorAssetPath)
            || !TryGetVirtualTerrainSector(sectorAssetPath, out var sector)
            || sector is null
            || tile.X < 0
            || tile.Y < 0
            || tile.X >= sector.TileWidth
            || tile.Y >= sector.TileHeight
        )
        {
            item = null;
            return false;
        }

        var tileArtId = sector.GetTileArtId(tile.X, tile.Y);
        if (!IncludeEmptyTerrainTiles && tileArtId == 0)
        {
            item = null;
            return false;
        }

        var mapTileX = checked((sector.LocalX * sector.TileWidth) + tile.X);
        var mapTileY = checked((sector.LocalY * sector.TileHeight) + tile.Y);
        var tileIndex = checked((tile.Y * sector.TileWidth) + tile.X);
        var (centerX, centerY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
            ViewMode,
            TileWidthPixels,
            TileHeightPixels,
            mapTileX,
            mapTileY
        );
        var tileDrawOrder = EditorMapFloorRenderBuilder.GetDrawOrder(ViewMode, 0, mapTileX, mapTileY);

        item = new EditorMapFloorTileRenderItem
        {
            SectorAssetPath = sector.AssetPath,
            MapTileX = mapTileX,
            MapTileY = mapTileY,
            Tile = tile,
            ArtId = new ArtId(tileArtId),
            IsBlocked = sector.IsTileBlocked(tile.X, tile.Y),
            HasLight = sector.LightTileIndices.Contains(tileIndex),
            HasScript = sector.ScriptedTileIndices.Contains(tileIndex),
            DrawOrder = CreateVirtualTerrainDrawOrder(tileDrawOrder),
            CenterX = centerX + OffsetX,
            CenterY = centerY + OffsetY,
        };
        return true;
    }

    internal static int CreateVirtualTerrainDrawOrder(long tileDrawOrder) =>
        (int)Math.Clamp(tileDrawOrder / 1_000_000L, 0L, int.MaxValue);

    public IEnumerable<EditorMapRenderQueueItem> EnumerateVisibleRenderItems(EditorMapSceneViewportLayout viewport)
    {
        if (Slices.Count == 0 || RenderQueueOrderMap.Count == 0)
        {
            for (var index = 0; index < RenderQueue.Count; index++)
                yield return RenderQueue[index];

            yield break;
        }

        foreach (var (sliceIndex, queueIndex) in EnumerateVisiblePackedQueueIndices(viewport))
            yield return Slices[sliceIndex].CreateRenderQueueItem(queueIndex);
    }

    internal IEnumerable<(int SliceIndex, int QueueIndex)> EnumerateVisiblePackedQueueIndices(
        EditorMapSceneViewportLayout viewport
    )
    {
        if (Slices.Count == 0 || RenderQueueOrderMap.Count == 0)
            yield break;

        var visibleSliceIndices = new List<int>();
        for (var sliceIndex = 0; sliceIndex < Slices.Count; sliceIndex++)
        {
            if (Slices[sliceIndex].Bounds.Intersects(viewport) && Slices[sliceIndex].Queue.Count > 0)
                visibleSliceIndices.Add(sliceIndex);
        }

        if (visibleSliceIndices.Count == 0)
            yield break;

        var queue = new PriorityQueue<(int SliceIndex, int QueueIndex), int>();
        for (var index = 0; index < visibleSliceIndices.Count; index++)
        {
            var sliceIndex = visibleSliceIndices[index];
            queue.Enqueue((sliceIndex, 0), Slices[sliceIndex].Queue[0].DrawOrder);
        }

        while (queue.TryDequeue(out var current, out _))
        {
            yield return current;

            var slice = Slices[current.SliceIndex];
            var nextQueueIndex = current.QueueIndex + 1;
            if (nextQueueIndex < slice.Queue.Count)
                queue.Enqueue((current.SliceIndex, nextQueueIndex), slice.Queue[nextQueueIndex].DrawOrder);
        }
    }

    internal static uint PackSliceItemIndex(int sliceIndex, int itemIndex)
    {
        if ((uint)sliceIndex > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sliceIndex), sliceIndex, "Slice index exceeds packed range.");

        if ((uint)itemIndex > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(itemIndex), itemIndex, "Item index exceeds packed range.");

        return ((uint)sliceIndex << 16) | (ushort)itemIndex;
    }

    internal static (int SliceIndex, int ItemIndex) UnpackSliceItemIndex(uint packedIndex) =>
        ((int)(packedIndex >> 16), (int)(packedIndex & 0xFFFF));

    private IReadOnlyDictionary<GameObjectGuid, EditorMapObjectRenderItem> BuildObjectLookup()
    {
        if (Slices.Count == 0)
            return Objects.ToDictionary(static obj => obj.ObjectId);

        var objectsById = new Dictionary<GameObjectGuid, EditorMapObjectRenderItem>();
        for (var sliceIndex = 0; sliceIndex < Slices.Count; sliceIndex++)
        {
            var sliceObjects = Slices[sliceIndex].Objects;
            for (var objectIndex = 0; objectIndex < sliceObjects.Count; objectIndex++)
                objectsById[sliceObjects[objectIndex].ObjectId] = sliceObjects[objectIndex];
        }

        return objectsById;
    }

    private IReadOnlyDictionary<GameObjectGuid, int> BuildObjectDrawOrderLookup()
    {
        if (Slices.Count == 0)
        {
            if (RenderQueue.Count > 0)
            {
                return RenderQueue
                    .Where(static item => item.Kind is EditorMapRenderQueueItemKind.Object && item.Object is not null)
                    .ToDictionary(static item => item.Object!.ObjectId, static item => item.DrawOrder);
            }

            return Objects.ToDictionary(static obj => obj.ObjectId, static obj => obj.DrawOrder);
        }

        var drawOrdersById = new Dictionary<GameObjectGuid, int>();
        for (var sliceIndex = 0; sliceIndex < Slices.Count; sliceIndex++)
        {
            var slice = Slices[sliceIndex];
            for (var queueIndex = 0; queueIndex < slice.Queue.Count; queueIndex++)
            {
                var entry = slice.Queue[queueIndex];
                if (entry.Kind is not EditorMapRenderQueueItemKind.Object)
                    continue;

                drawOrdersById[slice.Objects[entry.PayloadIndex].ObjectId] = entry.DrawOrder;
            }
        }

        return drawOrdersById;
    }

    private sealed class PackedSliceItemList<T>(
        IReadOnlyList<EditorMapSectorRenderSlice> slices,
        IReadOnlyList<uint> orderMap,
        Func<EditorMapSectorRenderSlice, IReadOnlyList<T>> itemsSelector
    ) : IReadOnlyList<T>
    {
        public int Count => orderMap.Count;

        public T this[int index]
        {
            get
            {
                var (sliceIndex, itemIndex) = UnpackSliceItemIndex(orderMap[index]);
                return itemsSelector(slices[sliceIndex])[itemIndex];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var index = 0; index < orderMap.Count; index++)
                yield return this[index];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class PackedRenderQueueItemList(
        IReadOnlyList<EditorMapSectorRenderSlice> slices,
        IReadOnlyList<uint> orderMap
    ) : IReadOnlyList<EditorMapRenderQueueItem>
    {
        public int Count => orderMap.Count;

        public EditorMapRenderQueueItem this[int index]
        {
            get
            {
                var (sliceIndex, queueIndex) = UnpackSliceItemIndex(orderMap[index]);
                return slices[sliceIndex].CreateRenderQueueItem(queueIndex);
            }
        }

        public IEnumerator<EditorMapRenderQueueItem> GetEnumerator()
        {
            for (var index = 0; index < orderMap.Count; index++)
                yield return this[index];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
