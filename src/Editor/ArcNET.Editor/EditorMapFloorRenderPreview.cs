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
    /// Canonical slice-backed committed scene payloads grouped by sector.
    /// </summary>
    public IReadOnlyList<EditorMapSectorRenderSlice> Slices { get; init; } = [];

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
    /// Ambient-lighting context that was applied while projecting the render.
    /// </summary>
    public EditorMapAmbientLightingState? AmbientLighting { get; init; }

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

    public bool TryGetTile(string sectorAssetPath, Location tile, out EditorMapFloorTileRenderItem? item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectorAssetPath);

        if (Slices.Count > 0)
        {
            var slicesByAssetPath = _slicesByAssetPath ??= Slices.ToDictionary(
                static slice => slice.SectorAssetPath,
                StringComparer.OrdinalIgnoreCase
            );
            if (slicesByAssetPath.TryGetValue(sectorAssetPath, out var slice))
                return slice.TryGetTile(tile, out item);

            item = null;
            return false;
        }

        for (var index = 0; index < Tiles.Count; index++)
        {
            var candidate = Tiles[index];
            if (
                string.Equals(candidate.SectorAssetPath, sectorAssetPath, StringComparison.OrdinalIgnoreCase)
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
                static slice => slice.SectorAssetPath,
                StringComparer.OrdinalIgnoreCase
            );
            return slicesByAssetPath.TryGetValue(sectorAssetPath, out var slice) ? slice.GetObjectsAtTile(tile) : [];
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
                    && !string.Equals(candidate.SectorAssetPath, sectorAssetPath, StringComparison.OrdinalIgnoreCase)
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
            return _sectorBounds = [.. Slices.Select(static slice => slice.Bounds)];

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
