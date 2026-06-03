using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// One paintable scene item projected for host renderers such as Avalonia or Skia.
/// </summary>
public sealed class EditorMapPaintableSceneItem
{
    public required EditorMapRenderQueueItemKind Kind { get; init; }
    public required int DrawOrder { get; init; }
    public required double SortKey { get; init; }
    public EditorMapCommittedRenderLayer? CommittedRenderLayer { get; init; }
    public required double Left { get; init; }
    public required double Top { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required double AnchorX { get; init; }
    public required double AnchorY { get; init; }
    public required double SuggestedOpacity { get; init; }
    public uint? SuggestedTintColor { get; init; }
    public bool TintIgnoresLightVisibility { get; init; }
    public bool UseGrayscalePaletteOverride { get; init; }
    public bool UseLightMaskTint { get; init; }
    public bool SuppressFallback { get; init; }
    public SectorLightFlags? LightFlags { get; init; }
    public EditorMapTileLightDiagnostics? TileLightDiagnostics { get; init; }
    public EditorMapTileOverlayKind? TileOverlayKind { get; init; }
    public EditorMapPaintableSceneSpriteSourceRect? SpriteSourceRect { get; init; }
    public EditorMapPaintableSceneSpriteDestinationRect? SpriteDestinationRect { get; init; }
    public bool IsRoofCovered { get; init; }
    public EditorMapObjectColorArray? ObjectColorArray { get; init; }
    public EditorMapObjectAlphaLerp? ObjectAlphaLerp { get; init; }
    public EditorMapRoofAlphaLerp? RoofAlphaLerp { get; init; }
    public EditorMapSpriteBlendMode BlendMode { get; init; } = EditorMapSpriteBlendMode.SourceOver;
    public bool UseSubtractiveShadowBlend { get; init; }
    public EditorMapRenderSprite? Sprite { get; init; }
    public EditorMapPaintableSceneSpriteReference? SpriteReference { get; init; }
    public IReadOnlyList<EditorMapRenderPoint>? GeometryPoints { get; init; }
    public EditorMapPaintableSceneGeometry? Geometry { get; init; }
}

public enum EditorMapPaintableSceneGeometryKind
{
    Rectangle,
    Diamond,
}

public readonly record struct EditorMapPaintableSceneGeometry(
    EditorMapPaintableSceneGeometryKind Kind,
    double CenterX,
    double CenterY,
    double Width,
    double Height
);

/// <summary>
/// Lightweight sprite lookup key and frame metrics retained by paintable scene items.
/// Hosts can resolve pixels lazily through the owning scene sprite source when an item becomes visible.
/// </summary>
public sealed class EditorMapPaintableSceneSpriteReference
{
    public required ArtId ArtId { get; init; }
    public string? AssetPath { get; init; }
    public EditorMapRenderQueueItemKind? RenderItemKind { get; init; }
    public required int RotationIndex { get; init; }
    public required int FrameIndex { get; init; }
    public int FramesPerRotation { get; init; } = 1;
    public uint FrameRate { get; init; }
    public int ScalePercent { get; init; } = 100;
    public bool IsShrunk { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int CenterX { get; init; }
    public required int CenterY { get; init; }
    public int DeltaX { get; init; }
    public int DeltaY { get; init; }
}

/// <summary>
/// Host-facing paintable projection of one committed scene render plus an optional live placement ghost.
/// </summary>
public sealed class EditorMapPaintableScene
{
    public required string MapName { get; init; }
    public required EditorMapSceneViewMode ViewMode { get; init; }
    public required double WidthPixels { get; init; }
    public required double HeightPixels { get; init; }
    public IEditorMapRenderSpriteSource? SpriteSource { get; init; }
    public required IReadOnlyList<EditorMapPaintableSceneItem> Items { get; init; }
    public required EditorMapRenderSpriteCoverage SpriteCoverage { get; init; }
    internal EditorMapPaintableSceneItemSource? ItemSource { get; init; }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(EditorMapSceneViewportLayout viewport)
    {
        if (ItemSource is not null)
        {
            foreach (var item in ItemSource.EnumerateVisibleItems(viewport))
                yield return item;
            yield break;
        }

        for (var itemIndex = 0; itemIndex < Items.Count; itemIndex++)
        {
            var item = Items[itemIndex];
            if (IntersectsViewport(item, viewport))
                yield return item;
        }
    }

    internal static bool IntersectsViewport(EditorMapPaintableSceneItem item, EditorMapSceneViewportLayout viewport)
    {
        var itemWidth = Math.Max(1d, item.Width);
        var itemHeight = Math.Max(1d, item.Height);
        var itemRight = item.Left + itemWidth;
        var itemBottom = item.Top + itemHeight;

        return itemRight >= viewport.VisibleLeft
            && item.Left <= viewport.VisibleRight
            && itemBottom >= viewport.VisibleTop
            && item.Top <= viewport.VisibleBottom;
    }

    internal static bool IntersectsViewport(
        EditorMapPaintableSceneItemBounds item,
        EditorMapSceneViewportLayout viewport
    )
    {
        var itemWidth = Math.Max(1d, item.Width);
        var itemHeight = Math.Max(1d, item.Height);
        var itemRight = item.Left + itemWidth;
        var itemBottom = item.Top + itemHeight;

        return itemRight >= viewport.VisibleLeft
            && item.Left <= viewport.VisibleRight
            && itemBottom >= viewport.VisibleTop
            && item.Top <= viewport.VisibleBottom;
    }
}

internal readonly record struct EditorMapPaintableSceneItemBounds(double Left, double Top, double Width, double Height);

internal readonly record struct EditorMapPaintableSceneSpriteReferenceKey(
    ArtId ArtId,
    string? AssetPath,
    EditorMapRenderQueueItemKind? RenderItemKind,
    int RotationIndex,
    int FrameIndex,
    int FramesPerRotation,
    uint FrameRate,
    int ScalePercent,
    bool IsShrunk,
    int Width,
    int Height,
    int CenterX,
    int CenterY,
    int DeltaX,
    int DeltaY
);

internal sealed class EditorMapPaintableSceneSpriteReferenceCache
{
    private readonly object _gate = new();
    private readonly Dictionary<
        EditorMapPaintableSceneSpriteReferenceKey,
        EditorMapPaintableSceneSpriteReference
    > _cache = [];

    public EditorMapPaintableSceneSpriteReference GetOrAdd(EditorMapPaintableSceneSpriteReferenceKey key)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out var reference))
                return reference;

            reference = new EditorMapPaintableSceneSpriteReference
            {
                ArtId = key.ArtId,
                AssetPath = key.AssetPath,
                RenderItemKind = key.RenderItemKind,
                RotationIndex = key.RotationIndex,
                FrameIndex = key.FrameIndex,
                FramesPerRotation = key.FramesPerRotation,
                FrameRate = key.FrameRate,
                ScalePercent = key.ScalePercent,
                IsShrunk = key.IsShrunk,
                Width = key.Width,
                Height = key.Height,
                CenterX = key.CenterX,
                CenterY = key.CenterY,
                DeltaX = key.DeltaX,
                DeltaY = key.DeltaY,
            };
            _cache[key] = reference;
            return reference;
        }
    }
}

internal readonly record struct EditorMapPaintableSceneItemPlan(
    int QueueIndex,
    int QuadrantIndex,
    EditorMapPaintableSceneItemBounds Bounds
);

internal interface IEditorMapPaintableSceneItemSegmentSource : IReadOnlyList<EditorMapPaintableSceneItem>
{
    IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(EditorMapSceneViewportLayout viewport);
}

internal sealed class EditorMapPaintableSceneItemSource : IReadOnlyList<EditorMapPaintableSceneItem>
{
    private readonly EditorMapFloorRenderPreview _sceneRender;
    private readonly IEditorMapPaintableSceneItemSegmentSource[] _segments;
    private readonly EditorMapPaintableSceneSliceSource[]? _sliceSources;
    private readonly int[] _segmentStartIndices;

    private EditorMapPaintableSceneItemSource(
        EditorMapFloorRenderPreview sceneRender,
        IEditorMapPaintableSceneItemSegmentSource[] segments,
        EditorMapPaintableSceneSliceSource[]? sliceSources
    )
    {
        _sceneRender = sceneRender;
        _segments = segments;
        _sliceSources = sliceSources;
        _segmentStartIndices = new int[segments.Length];

        var nextStartIndex = 0;
        for (var index = 0; index < segments.Length; index++)
        {
            _segmentStartIndices[index] = nextStartIndex;
            nextStartIndex += segments[index].Count;
        }

        Count = nextStartIndex;
    }

    public static EditorMapPaintableSceneItemSource CreateCommitted(
        EditorMapFloorRenderPreview sceneRender,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var spriteReferenceCache = new EditorMapPaintableSceneSpriteReferenceCache();
        var sliceSources = new EditorMapPaintableSceneSliceSource[sceneRender.Slices.Count];
        for (var index = 0; index < sceneRender.Slices.Count; index++)
        {
            sliceSources[index] = new EditorMapPaintableSceneSliceSource(
                sceneRender,
                sceneRender.Slices[index],
                spriteSource,
                spriteReferenceCache
            );
        }

        return new EditorMapPaintableSceneItemSource(sceneRender, sliceSources, sliceSources);
    }

    public static EditorMapPaintableSceneItemSource CreateFlat(
        EditorMapFloorRenderPreview sceneRender,
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var segment = new EditorMapPaintableSceneFlatSource(
            sceneRender,
            queue,
            spriteSource,
            new EditorMapPaintableSceneSpriteReferenceCache()
        );
        return new EditorMapPaintableSceneItemSource(sceneRender, [segment], null);
    }

    public int Count { get; }

    public EditorMapPaintableSceneItem this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            for (var segmentIndex = _segments.Length - 1; segmentIndex >= 0; segmentIndex--)
            {
                var segmentStartIndex = _segmentStartIndices[segmentIndex];
                if (index < segmentStartIndex)
                    continue;

                return _segments[segmentIndex][index - segmentStartIndex];
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(EditorMapSceneViewportLayout viewport)
    {
        if (_sliceSources is not null)
        {
            foreach (var (sliceIndex, queueIndex) in _sceneRender.EnumerateVisiblePackedQueueIndices(viewport))
            {
                foreach (var item in _sliceSources[sliceIndex].EnumerateVisibleItems(queueIndex, viewport))
                    yield return item;
            }

            yield break;
        }

        for (var index = 0; index < _segments.Length; index++)
        {
            foreach (var item in _segments[index].EnumerateVisibleItems(viewport))
                yield return item;
        }
    }

    public IEnumerator<EditorMapPaintableSceneItem> GetEnumerator()
    {
        for (var index = 0; index < Count; index++)
            yield return this[index];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

internal abstract class EditorMapPaintableSceneSegmentSourceBase : IEditorMapPaintableSceneItemSegmentSource
{
    private readonly EditorMapFloorRenderPreview _sceneRender;
    private readonly IEditorMapRenderSpriteSource? _spriteSource;
    private readonly EditorMapPaintableSceneSpriteReferenceCache _spriteReferenceCache;
    private readonly int _count;
    private readonly object _planGate = new();
    private readonly object _cacheGate = new();
    private EditorMapPaintableSceneItemPlan[]? _plans;
    private int[]? _planStartByQueueIndex;
    private byte[]? _planCountByQueueIndex;
    private EditorMapPaintableSceneItem?[]? _items;

    protected EditorMapPaintableSceneSegmentSourceBase(
        EditorMapFloorRenderPreview sceneRender,
        IEditorMapRenderSpriteSource? spriteSource,
        int count,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        _sceneRender = sceneRender;
        _spriteSource = spriteSource;
        _count = count;
        _spriteReferenceCache = spriteReferenceCache;
    }

    public int Count => _count;

    public EditorMapPaintableSceneItem this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsurePlanCache();
            var items = _items!;
            var item = items[index];
            if (item is not null)
                return item;

            lock (_cacheGate)
            {
                item = items[index];
                if (item is not null)
                    return item;

                var plan = _plans![index];
                var queueItem = GetQueueItem(plan.QueueIndex);
                item =
                    plan.QuadrantIndex >= 0
                        ? EditorMapPaintableSceneBuilder.BuildFloorTileQuadrant(
                            _sceneRender,
                            queueItem,
                            _spriteSource,
                            _spriteReferenceCache,
                            plan.QuadrantIndex
                        )
                        : EditorMapPaintableSceneBuilder.BuildItem(
                            _sceneRender,
                            queueItem,
                            _spriteSource,
                            _spriteReferenceCache
                        );
                items[index] = item;
                return item;
            }
        }
    }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(EditorMapSceneViewportLayout viewport)
    {
        if (!IntersectsViewport(viewport))
            yield break;

        EnsurePlanCache();
        var plans = _plans!;
        for (var index = 0; index < plans.Length; index++)
        {
            if (EditorMapPaintableScene.IntersectsViewport(plans[index].Bounds, viewport))
                yield return this[index];
        }
    }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(
        int queueIndex,
        EditorMapSceneViewportLayout viewport
    )
    {
        if (!IntersectsViewport(viewport))
            yield break;

        EnsurePlanCache();
        var planStartByQueueIndex = _planStartByQueueIndex!;
        var planCountByQueueIndex = _planCountByQueueIndex!;
        var startIndex = planStartByQueueIndex[queueIndex];
        if (startIndex < 0)
            yield break;

        var count = planCountByQueueIndex[queueIndex];
        for (var offset = 0; offset < count; offset++)
        {
            var planIndex = startIndex + offset;
            if (EditorMapPaintableScene.IntersectsViewport(_plans![planIndex].Bounds, viewport))
                yield return this[planIndex];
        }
    }

    public IEnumerator<EditorMapPaintableSceneItem> GetEnumerator()
    {
        for (var index = 0; index < _count; index++)
            yield return this[index];
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    protected virtual bool IntersectsViewport(EditorMapSceneViewportLayout viewport) => true;

    protected abstract int QueueCount { get; }

    protected abstract EditorMapRenderQueueItem GetQueueItem(int queueIndex);

    private void EnsurePlanCache()
    {
        if (_plans is not null)
            return;

        lock (_planGate)
        {
            if (_plans is not null)
                return;

            var plans = new EditorMapPaintableSceneItemPlan[_count];
            var planStartByQueueIndex = new int[QueueCount];
            Array.Fill(planStartByQueueIndex, -1);
            var planCountByQueueIndex = new byte[QueueCount];
            var items = new EditorMapPaintableSceneItem[_count];

            var destIndex = 0;
            for (var queueIndex = 0; queueIndex < QueueCount; queueIndex++)
            {
                var queueItem = GetQueueItem(queueIndex);
                planStartByQueueIndex[queueIndex] = destIndex;

                if (
                    queueItem.Kind is EditorMapRenderQueueItemKind.FloorTile
                    && queueItem.Tile is { } tile
                    && tile.LightDiagnostics?.HasInterpolationVariance == true
                )
                {
                    var quadrantBounds = EditorMapPaintableSceneBuilder.BuildFloorTileQuadrantBounds(
                        _sceneRender,
                        queueItem,
                        _spriteSource,
                        _spriteReferenceCache
                    );
                    planCountByQueueIndex[queueIndex] = checked((byte)quadrantBounds.Length);
                    for (var quadrantIndex = 0; quadrantIndex < quadrantBounds.Length; quadrantIndex++)
                    {
                        plans[destIndex++] = new EditorMapPaintableSceneItemPlan(
                            queueIndex,
                            quadrantIndex,
                            quadrantBounds[quadrantIndex]
                        );
                    }
                }
                else
                {
                    planCountByQueueIndex[queueIndex] = 1;
                    plans[destIndex++] = new EditorMapPaintableSceneItemPlan(
                        queueIndex,
                        QuadrantIndex: -1,
                        EditorMapPaintableSceneBuilder.BuildItemBounds(
                            _sceneRender,
                            queueItem,
                            _spriteSource,
                            _spriteReferenceCache
                        )
                    );
                }
            }

            _plans = plans;
            _planStartByQueueIndex = planStartByQueueIndex;
            _planCountByQueueIndex = planCountByQueueIndex;
            _items = items;
        }
    }
}

internal sealed class EditorMapPaintableSceneSliceSource : EditorMapPaintableSceneSegmentSourceBase
{
    private readonly EditorMapSectorRenderSlice _slice;

    public EditorMapPaintableSceneSliceSource(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapSectorRenderSlice slice,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
        : base(sceneRender, spriteSource, CountExpandedItems(slice), spriteReferenceCache)
    {
        _slice = slice;
    }

    protected override int QueueCount => _slice.Queue.Count;

    protected override bool IntersectsViewport(EditorMapSceneViewportLayout viewport) =>
        _slice.Bounds.Intersects(viewport);

    protected override EditorMapRenderQueueItem GetQueueItem(int queueIndex) =>
        _slice.CreateRenderQueueItem(queueIndex);

    private static int CountExpandedItems(EditorMapSectorRenderSlice slice)
    {
        var totalCount = 0;
        for (var queueIndex = 0; queueIndex < slice.Queue.Count; queueIndex++)
        {
            var entry = slice.Queue[queueIndex];
            if (
                entry.Kind is EditorMapRenderQueueItemKind.FloorTile
                && slice.Tiles[entry.PayloadIndex].LightDiagnostics?.HasInterpolationVariance == true
            )
            {
                totalCount += 4;
            }
            else
            {
                totalCount++;
            }
        }

        return totalCount;
    }
}

internal sealed class EditorMapPaintableSceneFlatSource : EditorMapPaintableSceneSegmentSourceBase
{
    private readonly IReadOnlyList<EditorMapRenderQueueItem> _queue;

    public EditorMapPaintableSceneFlatSource(
        EditorMapFloorRenderPreview sceneRender,
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
        : base(sceneRender, spriteSource, CountExpandedItems(queue), spriteReferenceCache)
    {
        _queue = queue;
    }

    protected override int QueueCount => _queue.Count;

    protected override EditorMapRenderQueueItem GetQueueItem(int queueIndex) => _queue[queueIndex];

    private static int CountExpandedItems(IReadOnlyList<EditorMapRenderQueueItem> queue)
    {
        var totalCount = 0;
        for (var index = 0; index < queue.Count; index++)
        {
            var queueItem = queue[index];
            if (
                queueItem.Kind is EditorMapRenderQueueItemKind.FloorTile
                && queueItem.Tile is { } tile
                && tile.LightDiagnostics?.HasInterpolationVariance == true
            )
            {
                totalCount += 4;
            }
            else
            {
                totalCount++;
            }
        }

        return totalCount;
    }
}

/// <summary>
/// Builds host-ready paintable scene items from normalized committed and live scene render queues.
/// </summary>
public static class EditorMapPaintableSceneBuilder
{
    private const double CeTerrainBlitWidth = 78d;
    private const double CeTerrainBlitHeight = 40d;
    private const double CeTerrainLayoutCenterX = 39d;
    private const double CeTerrainLayoutCenterY = 20d;

    private static readonly EditorMapRenderSpriteRequest DefaultFloorTileRequest = new()
    {
        RenderItemKind = EditorMapRenderQueueItemKind.FloorTile,
        RotationIndex = 0,
        ScalePercent = 100,
        IsShrunk = false,
    };

    private static readonly EditorMapRenderSpriteRequest DefaultRoofRequest = new()
    {
        RenderItemKind = EditorMapRenderQueueItemKind.Roof,
        RotationIndex = 0,
        ScalePercent = 100,
        IsShrunk = false,
    };

    private static readonly EditorMapRenderSpriteRequest DefaultLightRequest = new()
    {
        RenderItemKind = EditorMapRenderQueueItemKind.Light,
        RotationIndex = 0,
        ScalePercent = 100,
        IsShrunk = false,
    };

    private readonly record struct SpriteReference(
        ArtId ArtId,
        EditorMapRenderQueueItemKind RenderItemKind,
        int RotationIndex,
        int ScalePercent,
        bool IsShrunk
    );

    /// <summary>
    /// Builds one paintable scene from the committed render queue and one optional placement-preview queue.
    /// When <paramref name="spriteSource"/> is supplied, ART-backed queue items are enriched with cached preview frames.
    /// </summary>
    public static EditorMapPaintableScene Build(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapPlacementPreview? placementPreview = null,
        IEditorMapRenderSpriteSource? spriteSource = null,
        EditorMapRenderSpriteCoverage? existingSpriteCoverage = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        var queue = placementPreview?.RenderQueue ?? sceneRender.RenderQueue;
        var itemSource = CreateItemSource(sceneRender, queue, spriteSource, cancellationToken);

        var spriteCoverage = existingSpriteCoverage ?? BuildSpriteCoverage(queue, spriteSource, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return new EditorMapPaintableScene
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            WidthPixels = Math.Max(sceneRender.WidthPixels, placementPreview?.WidthPixels ?? 0d),
            HeightPixels = Math.Max(sceneRender.HeightPixels, placementPreview?.HeightPixels ?? 0d),
            SpriteSource = spriteSource,
            Items = itemSource,
            SpriteCoverage = spriteCoverage,
            ItemSource = itemSource,
        };
    }

    /// <summary>
    /// Builds one paintable scene containing only live placement-preview objects for hosts that draw it over an already rendered committed scene.
    /// </summary>
    public static EditorMapPaintableScene BuildPlacementOverlay(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapPlacementPreview? placementPreview,
        IEditorMapRenderSpriteSource? spriteSource = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        EditorMapRenderQueueItem[] queue;
        if (placementPreview is null || placementPreview.RenderQueue.Count == 0)
        {
            queue = [];
        }
        else
        {
            // Direct loop avoids LINQ iterator allocations for a typically tiny set of preview items.
            var previewCount = 0;
            var renderQueue = placementPreview.RenderQueue;
            for (var i = 0; i < renderQueue.Count; i++)
            {
                if (renderQueue[i].Kind is EditorMapRenderQueueItemKind.PlacementPreviewObject)
                    previewCount++;
            }

            queue = new EditorMapRenderQueueItem[previewCount];
            var dest = 0;
            for (var i = 0; i < renderQueue.Count; i++)
            {
                if (renderQueue[i].Kind is EditorMapRenderQueueItemKind.PlacementPreviewObject)
                    queue[dest++] = renderQueue[i];
            }
        }

        var itemSource = CreateItemSource(sceneRender, queue, spriteSource, cancellationToken);

        // Skip full sprite coverage analysis for placement overlays — they are transient
        // single-frame previews with typically 1-3 items. The coverage data is never consumed
        // by the host for overlay scenes, so building it is pure overhead.
        var spriteCoverage = EditorMapRenderSpriteCoverage.Empty;
        cancellationToken.ThrowIfCancellationRequested();

        return new EditorMapPaintableScene
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            WidthPixels = sceneRender.WidthPixels,
            HeightPixels = sceneRender.HeightPixels,
            SpriteSource = spriteSource,
            Items = itemSource,
            SpriteCoverage = spriteCoverage,
            ItemSource = itemSource,
        };
    }

    private static EditorMapPaintableSceneItemSource CreateItemSource(
        EditorMapFloorRenderPreview sceneRender,
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return queue == sceneRender.RenderQueue && sceneRender.Slices.Count > 0
            ? EditorMapPaintableSceneItemSource.CreateCommitted(sceneRender, spriteSource)
            : EditorMapPaintableSceneItemSource.CreateFlat(sceneRender, queue, spriteSource);
    }

    internal static EditorMapPaintableSceneItemBounds[] BuildFloorTileQuadrantBounds(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var layout = GetFloorTileQuadrantLayout(sceneRender, queueItem, spriteSource, spriteReferenceCache);
        var bounds = new EditorMapPaintableSceneItemBounds[4];
        for (var quadrantIndex = 0; quadrantIndex < 4; quadrantIndex++)
        {
            bounds[quadrantIndex] = new EditorMapPaintableSceneItemBounds(
                layout.Quadrants[quadrantIndex].Left,
                layout.Quadrants[quadrantIndex].Top,
                layout.Quadrants[quadrantIndex].Width,
                layout.Quadrants[quadrantIndex].Height
            );
        }

        return bounds;
    }

    internal static EditorMapPaintableSceneItem BuildFloorTileQuadrant(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache,
        int quadrantIndex
    )
    {
        var tile =
            queueItem.Tile
            ?? throw new InvalidOperationException("Floor tile queue items must carry one tile payload.");
        var layout = GetFloorTileQuadrantLayout(sceneRender, queueItem, spriteSource, spriteReferenceCache);
        var suppressFallback = ShouldSuppressFloorTileFallback(tile.ArtId, layout.SpriteReference);
        var g = tile.LightDiagnostics!.Value;
        var defaultColor = tile.SuggestedTintColor ?? 0xFFFFFFFF;
        var c0 = g.TopLeft ?? defaultColor;
        var c1 = g.TopCenter ?? defaultColor;
        var c2 = g.TopRight ?? defaultColor;
        var c3 = g.MiddleLeft ?? defaultColor;
        var c4 = g.MiddleCenter ?? defaultColor;
        var c5 = g.MiddleRight ?? defaultColor;
        var c6 = g.BottomLeft ?? defaultColor;
        var c7 = g.BottomCenter ?? defaultColor;
        var c8 = g.BottomRight ?? defaultColor;

        uint[][] quadrantColors =
        [
            [c0, c1, c4, c3], // Q0: Top-Left
            [c1, c2, c5, c4], // Q1: Top-Right
            [c3, c4, c7, c6], // Q2: Bottom-Left
            [c4, c5, c8, c7], // Q3: Bottom-Right
        ];

        var quadrant = layout.Quadrants[quadrantIndex];
        return new EditorMapPaintableSceneItem
        {
            Kind = queueItem.Kind,
            DrawOrder = queueItem.DrawOrder,
            SortKey = queueItem.SortKey,
            Left = quadrant.Left,
            Top = quadrant.Top,
            Width = quadrant.Width,
            Height = quadrant.Height,
            AnchorX = quadrant.AnchorX,
            AnchorY = quadrant.AnchorY,
            SuggestedOpacity = 1d,
            SuggestedTintColor = null,
            ObjectColorArray = new EditorMapObjectColorArray(quadrantColors[quadrantIndex]),
            TileLightDiagnostics = tile.LightDiagnostics,
            SpriteSourceRect = quadrant.SourceRect,
            SpriteDestinationRect = quadrant.DestinationRect,
            SuppressFallback = suppressFallback,
            SpriteReference = layout.SpriteReference,
            Geometry = quadrant.Geometry,
        };
    }

    private static EditorMapRenderSpriteCoverage BuildSpriteCoverage(
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var referencedSet = new HashSet<SpriteReference>();
        for (var i = 0; i < queue.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var refOpt = TryGetSpriteReference(queue[i]);
            if (refOpt is { ArtId.Value: not 0u } reference)
            {
                referencedSet.Add(reference);
            }
        }

        var referencedSpriteReferences = new SpriteReference[referencedSet.Count];
        referencedSet.CopyTo(referencedSpriteReferences);

        Array.Sort(
            referencedSpriteReferences,
            static (a, b) =>
            {
                var cmp = a.ArtId.Value.CompareTo(b.ArtId.Value);
                if (cmp != 0)
                    return cmp;
                return a.RenderItemKind.CompareTo(b.RenderItemKind);
            }
        );

        cancellationToken.ThrowIfCancellationRequested();
        var resolvedList = new List<SpriteReference>(referencedSpriteReferences.Length);
        var unresolvedList = new List<SpriteReference>(referencedSpriteReferences.Length);

        for (var i = 0; i < referencedSpriteReferences.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reference = referencedSpriteReferences[i];
            var req = CreateSpriteRequest(reference);
            if (spriteSource?.CanResolve(reference.ArtId, req) == true)
            {
                resolvedList.Add(reference);
            }
            else
            {
                unresolvedList.Add(reference);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var resolvedSpriteReferences = resolvedList.ToArray();
        var unresolvedSpriteReferences = unresolvedList.ToArray();

        var referencedArtIdsSet = new HashSet<ArtId>();
        for (var i = 0; i < referencedSpriteReferences.Length; i++)
        {
            referencedArtIdsSet.Add(referencedSpriteReferences[i].ArtId);
        }
        var referencedArtIds = new ArtId[referencedArtIdsSet.Count];
        referencedArtIdsSet.CopyTo(referencedArtIds);
        Array.Sort(referencedArtIds, static (a, b) => a.Value.CompareTo(b.Value));

        var resolvedArtIdsSet = new HashSet<ArtId>();
        for (var i = 0; i < resolvedSpriteReferences.Length; i++)
        {
            resolvedArtIdsSet.Add(resolvedSpriteReferences[i].ArtId);
        }
        var resolvedArtIds = new ArtId[resolvedArtIdsSet.Count];
        resolvedArtIdsSet.CopyTo(resolvedArtIds);
        Array.Sort(resolvedArtIds, static (a, b) => a.Value.CompareTo(b.Value));

        var unresolvedArtIdsSet = new HashSet<ArtId>();
        for (var i = 0; i < unresolvedSpriteReferences.Length; i++)
        {
            unresolvedArtIdsSet.Add(unresolvedSpriteReferences[i].ArtId);
        }
        var unresolvedArtIds = new ArtId[unresolvedArtIdsSet.Count];
        unresolvedArtIdsSet.CopyTo(unresolvedArtIds);
        Array.Sort(unresolvedArtIds, static (a, b) => a.Value.CompareTo(b.Value));

        return new EditorMapRenderSpriteCoverage
        {
            ReferencedSpriteReferenceCount = referencedSpriteReferences.Length,
            ResolvedSpriteReferenceCount = resolvedSpriteReferences.Length,
            UnresolvedSpriteReferenceCount = unresolvedSpriteReferences.Length,
            ReferencedArtIds = referencedArtIds,
            ResolvedArtIds = resolvedArtIds,
            UnresolvedArtIds = unresolvedArtIds,
        };
    }

    private static EditorMapRenderSpriteRequest CreateSpriteRequest(
        EditorMapRenderQueueItemKind renderItemKind,
        int rotationIndex = 0,
        int scalePercent = 100,
        bool isShrunk = false
    )
    {
        if (rotationIndex == 0 && scalePercent == 100 && !isShrunk)
        {
            if (renderItemKind == EditorMapRenderQueueItemKind.FloorTile)
                return DefaultFloorTileRequest;
            if (renderItemKind == EditorMapRenderQueueItemKind.Roof)
                return DefaultRoofRequest;
            if (renderItemKind == EditorMapRenderQueueItemKind.Light)
                return DefaultLightRequest;
        }

        return new EditorMapRenderSpriteRequest
        {
            RenderItemKind = renderItemKind,
            RotationIndex = rotationIndex,
            ScalePercent = scalePercent,
            IsShrunk = isShrunk,
        };
    }

    private static EditorMapRenderSpriteRequest CreateSpriteRequestFromRotation(
        EditorMapRenderQueueItemKind renderItemKind,
        float rotation,
        int scalePercent = 100,
        bool isShrunk = false
    ) => CreateSpriteRequest(renderItemKind, ResolveRotationIndex(rotation), scalePercent, isShrunk);

    internal static EditorMapPaintableSceneItem BuildItem(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        return queueItem.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => BuildFloorTile(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.TileOverlay => BuildTileOverlay(sceneRender, queueItem),
            EditorMapRenderQueueItemKind.Object => BuildObject(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.ObjectAuxiliary => BuildObjectAuxiliary(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.Roof => BuildRoof(sceneRender, queueItem, spriteSource, spriteReferenceCache),
            EditorMapRenderQueueItemKind.Light => BuildLight(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => BuildPlacementPreviewObject(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(queueItem.Kind),
                queueItem.Kind,
                "Unsupported render queue kind."
            ),
        };
    }

    private static EditorMapPaintableSceneItem BuildFloorTile(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var tile =
            queueItem.Tile
            ?? throw new InvalidOperationException("Floor tile queue items must carry one tile payload.");
        var spriteReference = TryCreateSpriteReference(
            tile.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.FloorTile),
            spriteSource,
            spriteReferenceCache
        );
        var geometry = CreateTileGeometry(
            sceneRender,
            tile.CenterX,
            tile.CenterY,
            footprintWidth: 1,
            footprintHeight: 1
        );
        return CreateItem(
            queueItem,
            tile.CenterX,
            tile.CenterY,
            sceneRender.TileWidthPixels,
            sceneRender.TileHeightPixels,
            spriteReference,
            geometry,
            suggestedOpacity: 1d,
            suggestedTintColor: tile.SuggestedTintColor,
            suppressFallback: ShouldSuppressFloorTileFallback(tile.ArtId, spriteReference),
            layoutCenterX: sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? CeTerrainLayoutCenterX : null,
            layoutCenterY: sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? CeTerrainLayoutCenterY : null,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static bool ShouldSuppressFloorTileFallback(
        ArtId artId,
        EditorMapPaintableSceneSpriteReference? spriteReference
    ) => artId.Value == 0u && spriteReference is null;

    private static EditorMapPaintableSceneItem BuildTileOverlay(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem
    )
    {
        var overlay =
            queueItem.TileOverlay
            ?? throw new InvalidOperationException("Tile overlay queue items must carry one tile-overlay payload.");
        var geometry = CreateTileGeometry(
            sceneRender,
            overlay.CenterX,
            overlay.CenterY,
            footprintWidth: 1,
            footprintHeight: 1
        );
        return CreateItem(
            queueItem,
            overlay.CenterX,
            overlay.CenterY,
            sceneRender.TileWidthPixels,
            sceneRender.TileHeightPixels,
            spriteReference: null,
            geometry,
            overlay.SuggestedOpacity,
            overlay.SuggestedTintColor
        );
    }

    private static EditorMapPaintableSceneItem BuildObject(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var obj =
            queueItem.Object
            ?? throw new InvalidOperationException("Object queue items must carry one object payload.");
        var request = CreateSpriteRequest(
            EditorMapRenderQueueItemKind.Object,
            obj.RotationIndex,
            obj.BlitScale,
            obj.IsShrunk
        );
        var spriteReference = TryCreateSpriteReference(
            obj.CurrentArtId,
            request,
            spriteSource,
            spriteReferenceCache,
            CreateFallbackSpriteMetrics(obj.ObjectType, obj.CurrentArtId, obj.SpriteBounds)
        );

        var blitFlags = (BlitFlags)unchecked((uint)obj.BlitFlags);
        var suggestedOpacity = 1.0d;
        if ((blitFlags & BlitFlags.BlendAlphaConst) != 0)
        {
            // CE's sub_442520 only injects a 128-alpha fallback for translucent or invisible
            // objects when the object is already on an alpha-blended render path.
            suggestedOpacity = obj.BlitAlpha / 255.0d;
        }

        var isStandaloneSceneryLightMask = IsStandaloneSceneryLightMaskObject(obj, spriteReference?.AssetPath);
        var suggestedTintColor = (blitFlags & BlitFlags.BlendColorConst) != 0 ? (uint?)obj.BlitColor : null;
        if (
            suggestedTintColor is null
            && (obj.CurrentArtId.Type is ArtId.TypeCode.Light || isStandaloneSceneryLightMask)
        )
            suggestedTintColor = PackPrimaryLightMaskTintColor(obj.LightColor);

        return CreateItem(
            queueItem,
            obj.AnchorX,
            obj.AnchorY,
            obj.SpriteBounds?.MaxFrameWidth ?? spriteReference?.Width ?? 0d,
            obj.SpriteBounds?.MaxFrameHeight ?? spriteReference?.Height ?? 0d,
            spriteReference,
            geometry: null,
            suggestedOpacity,
            suggestedTintColor,
            isStandaloneSceneryLightMask: isStandaloneSceneryLightMask,
            includeEditorObjectStateTint: sceneRender.IncludeEditorObjectStateTint,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildObjectAuxiliary(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var auxiliary =
            queueItem.ObjectAuxiliaryItem
            ?? throw new InvalidOperationException(
                "Object-auxiliary queue items must carry one object-auxiliary payload."
            );
        var request = CreateSpriteRequest(
            EditorMapRenderQueueItemKind.ObjectAuxiliary,
            auxiliary.RotationIndex,
            auxiliary.ScalePercent,
            auxiliary.IsShrunk
        );
        var spriteReference = TryCreateSpriteReference(auxiliary.ArtId, request, spriteSource, spriteReferenceCache);

        return CreateItem(
            queueItem,
            auxiliary.AnchorX,
            auxiliary.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            geometry: null,
            suggestedOpacity: 1d,
            auxiliary.SuggestedTintColor,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildRoof(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var roof =
            queueItem.Roof ?? throw new InvalidOperationException("Roof queue items must carry one roof payload.");
        var spriteReference = TryCreateSpriteReference(
            roof.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Roof),
            spriteSource,
            spriteReferenceCache
        );
        return CreateItem(
            queueItem,
            roof.AnchorX,
            roof.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            geometry: null,
            suggestedOpacity: 1d,
            suggestedTintColor: null,
            roofAlphaLerp: GetRoofAlphaLerp(roof.ArtId),
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildLight(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var light =
            queueItem.Light ?? throw new InvalidOperationException("Light queue items must carry one light payload.");
        var spriteReference = TryCreateSpriteReference(
            light.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Light),
            spriteSource,
            spriteReferenceCache
        );
        return CreateItem(
            queueItem,
            light.AnchorX,
            light.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            geometry: null,
            light.SuggestedOpacity,
            light.SuggestedTintColor,
            lightFlags: light.Flags,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildPlacementPreviewObject(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var previewObject =
            queueItem.PlacementPreviewObject
            ?? throw new InvalidOperationException(
                "Placement-preview queue items must carry one placement-preview payload."
            );
        var request = CreateSpriteRequest(
            EditorMapRenderQueueItemKind.PlacementPreviewObject,
            previewObject.RotationIndex,
            previewObject.BlitScale,
            previewObject.IsShrunk
        );
        var spriteReference = TryCreateSpriteReference(
            previewObject.CurrentArtId,
            request,
            spriteSource,
            spriteReferenceCache,
            CreateFallbackSpriteMetrics(
                previewObject.ObjectType,
                previewObject.CurrentArtId,
                previewObject.SpriteBounds
            )
        );

        return CreateItem(
            queueItem,
            previewObject.AnchorX,
            previewObject.AnchorY,
            previewObject.SpriteBounds?.MaxFrameWidth ?? spriteReference?.Width ?? 0d,
            previewObject.SpriteBounds?.MaxFrameHeight ?? spriteReference?.Height ?? 0d,
            spriteReference,
            geometry: null,
            previewObject.SuggestedOpacity,
            previewObject.SuggestedTintColor,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static uint PackPrimaryLightMaskTintColor(Color? lightColor) =>
        lightColor is null
            ? 0xFFFFFFFFu
            : 0xFF000000u | ((uint)lightColor.Value.R << 16) | ((uint)lightColor.Value.G << 8) | lightColor.Value.B;

    private static bool IsStandaloneSceneryLightMaskObject(EditorMapObjectRenderItem obj, string? assetPath) =>
        obj.CurrentArtId.Type is ArtId.TypeCode.Scenery
        && (
            IsVisibleStandaloneSceneryLightMaskObject(obj, assetPath) || IsSceneryLightMaskHelperObject(obj, assetPath)
        );

    private static bool IsVisibleStandaloneSceneryLightMaskObject(EditorMapObjectRenderItem obj, string? assetPath) =>
        !obj.Flags.HasFlag(ObjectFlags.Off)
        && obj.LightAid.Value == 0
        && IsStandaloneSceneryLightMaskAssetPath(assetPath);

    private static bool IsStandaloneSceneryLightMaskAssetPath(string? assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        var normalizedAssetPath = assetPath.Replace('\\', '/');
        if (!normalizedAssetPath.StartsWith("art/scenery/", StringComparison.OrdinalIgnoreCase))
            return false;

        var fileNameStart = normalizedAssetPath.LastIndexOf('/') + 1;
        var fileName = fileNameStart <= 0 ? normalizedAssetPath : normalizedAssetPath[fileNameStart..];
        return fileName.Contains("-light", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("_light", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSceneryLightMaskHelperObject(EditorMapObjectRenderItem obj, string? assetPath) =>
        HasSceneryLightMaskHelperBehavior(obj) && IsSceneryLightMaskHelperAssetPath(assetPath);

    private static bool HasSceneryLightMaskHelperBehavior(EditorMapObjectRenderItem obj)
    {
        var hasHiddenMaskFlags = obj.Flags.HasFlag(ObjectFlags.Off) || obj.Flags.HasFlag(ObjectFlags.DontLight);
        var hasProjectedGlowHelperFlags =
            obj.LightAid.Value != 0
            && (
                obj.Flags.HasFlag(ObjectFlags.NoBlock)
                || obj.Flags.HasFlag(ObjectFlags.SeeThrough)
                || obj.Flags.HasFlag(ObjectFlags.ShootThrough)
            );
        return hasHiddenMaskFlags || hasProjectedGlowHelperFlags;
    }

    private static bool IsSceneryLightMaskHelperAssetPath(string? assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        var normalizedAssetPath = assetPath.Replace('\\', '/');
        if (!normalizedAssetPath.StartsWith("art/scenery/", StringComparison.OrdinalIgnoreCase))
            return false;

        var fileNameStart = normalizedAssetPath.LastIndexOf('/') + 1;
        var fileName = fileNameStart <= 0 ? normalizedAssetPath : normalizedAssetPath[fileNameStart..];
        return fileName.Contains("light", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("glow", StringComparison.OrdinalIgnoreCase);
    }

    private static EditorMapPaintableSceneItem CreateItem(
        EditorMapRenderQueueItem queueItem,
        double anchorX,
        double anchorY,
        double fallbackWidth,
        double fallbackHeight,
        EditorMapPaintableSceneSpriteReference? spriteReference,
        EditorMapPaintableSceneGeometry? geometry,
        double suggestedOpacity,
        uint? suggestedTintColor,
        bool isStandaloneSceneryLightMask = false,
        bool includeEditorObjectStateTint = false,
        SectorLightFlags? lightFlags = null,
        double? layoutCenterX = null,
        double? layoutCenterY = null,
        EditorMapRoofAlphaLerp? roofAlphaLerp = null,
        bool suppressFallback = false,
        double sceneScaleX = 1d,
        double sceneScaleY = 1d
    )
    {
        var layout = CreateItemLayout(
            anchorX,
            anchorY,
            fallbackWidth,
            fallbackHeight,
            spriteReference,
            layoutCenterX,
            layoutCenterY,
            sceneScaleX,
            sceneScaleY
        );

        var isEyeCandyTranslucent =
            queueItem.Object?.CurrentArtId.IsEyeCandyTranslucent == true
            || queueItem.ObjectAuxiliaryItem?.ArtId.IsEyeCandyTranslucent == true
            || queueItem.PlacementPreviewObject?.CurrentArtId.IsEyeCandyTranslucent == true;
        var blendMode = EditorMapSpriteBlendMode.SourceOver;
        if (isEyeCandyTranslucent)
        {
            blendMode = EditorMapSpriteBlendMode.Add;
        }
        else if (queueItem.ObjectAuxiliaryItem is not null)
        {
            blendMode = queueItem.ObjectAuxiliaryItem.BlendMode;
        }
        else if (queueItem.Object is not null)
        {
            var blitFlags = (BlitFlags)unchecked((uint)queueItem.Object.BlitFlags);
            if ((blitFlags & BlitFlags.BlendAdd) != 0)
                blendMode = EditorMapSpriteBlendMode.Add;
            else if ((blitFlags & BlitFlags.BlendSub) != 0)
                blendMode = EditorMapSpriteBlendMode.Subtract;
            else if ((blitFlags & BlitFlags.BlendMul) != 0)
                blendMode = EditorMapSpriteBlendMode.Multiply;
        }

        var dontLight = queueItem.Object?.Flags.HasFlag(ObjectFlags.DontLight) == true;

        var isStoned = queueItem.Object?.Flags.HasFlag(ObjectFlags.Stoned) == true;

        var isAnimatedDead = queueItem.Object?.Flags.HasFlag(ObjectFlags.AnimatedDead) == true;
        var isFrozen = queueItem.Object?.Flags.HasFlag(ObjectFlags.Frozen) == true;
        var isDestroyed = queueItem.Object?.Flags.HasFlag(ObjectFlags.Destroyed) == true;
        var isOff = queueItem.Object?.Flags.HasFlag(ObjectFlags.Off) == true;

        // CE object_setup_blit() priority chain:
        // 1. Frozen → ADD + COLOR_CONST with blue tint (0, 128, 255)
        // 2. Eye candy translucency → ADD
        // 3. AnimatedDead → green tint (0, 255, 0)
        // 4. Editor destroyed/off → ADD + COLOR_CONST with red/green tint (highest priority override)
        var finalTintColor = suggestedTintColor;
        if (isFrozen)
        {
            blendMode = EditorMapSpriteBlendMode.Add;
            finalTintColor = 0xFF0080FF; // CE tig_color_make(0, 128, 255)
        }

        if (isAnimatedDead)
            finalTintColor = 0xFF00FF00;

        if (includeEditorObjectStateTint)
        {
            // Editor state tints override everything (CE object_setup_blit last check).
            if (isDestroyed)
            {
                blendMode = EditorMapSpriteBlendMode.Add;
                finalTintColor = 0xFFFF0000; // CE tig_color_make(255, 0, 0)
            }
            else if (isOff)
            {
                blendMode = EditorMapSpriteBlendMode.Add;
                finalTintColor = 0xFF00FF00; // CE tig_color_make(0, 255, 0)
            }
        }

        var isAuxiliaryLightMask =
            queueItem.ObjectAuxiliaryItem?.UseLightMaskTint == true
            || queueItem.ObjectAuxiliaryItem?.ArtId.Type is ArtId.TypeCode.Light;
        var isPrimaryLightMask =
            queueItem.Object?.CurrentArtId.Type is ArtId.TypeCode.Light
            || queueItem.PlacementPreviewObject?.CurrentArtId.Type is ArtId.TypeCode.Light
            || isStandaloneSceneryLightMask;

        return new EditorMapPaintableSceneItem
        {
            Kind = queueItem.Kind,
            DrawOrder = queueItem.DrawOrder,
            SortKey = queueItem.SortKey,
            CommittedRenderLayer =
                queueItem.Object?.CommittedRenderLayer ?? queueItem.ObjectAuxiliaryItem?.CommittedRenderLayer,
            Left = layout.Left,
            Top = layout.Top,
            Width = layout.Width,
            Height = layout.Height,
            AnchorX = anchorX,
            AnchorY = anchorY,
            SuggestedOpacity = suggestedOpacity,
            SuggestedTintColor = finalTintColor,
            TintIgnoresLightVisibility = dontLight,
            UseGrayscalePaletteOverride = isStoned,
            // Light-mask tinting is reserved for visible light art on the object queues.
            // CE object-created lights (LightAid / overlay lights) are projected separately
            // as Kind.Light items so they stay distinct from the parent object sprite.
            UseLightMaskTint = !isFrozen && !isDestroyed && (isAuxiliaryLightMask || isPrimaryLightMask),
            SuppressFallback = suppressFallback,
            LightFlags = lightFlags,
            TileLightDiagnostics = queueItem.Tile?.LightDiagnostics,
            TileOverlayKind = queueItem.TileOverlay?.Kind,
            SpriteSourceRect = layout.SourceRect,
            SpriteDestinationRect = layout.DestinationRect,
            IsRoofCovered = queueItem.Object?.IsRoofCovered ?? queueItem.ObjectAuxiliaryItem?.IsRoofCovered ?? false,
            RoofAlphaLerp = roofAlphaLerp,
            BlendMode = blendMode,
            UseSubtractiveShadowBlend = blendMode is EditorMapSpriteBlendMode.Subtract,
            SpriteReference = spriteReference,
            Geometry = geometry,
        };
    }

    private static EditorMapPaintableSceneSpriteReference? TryCreateSpriteReference(
        ArtId artId,
        EditorMapRenderSpriteRequest request,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache,
        EditorMapRenderSpriteMetrics? fallbackMetrics = null
    )
    {
        if (artId.Value == 0)
            return null;

        request = AdjustEyeCandyRequest(artId, request);
        var metrics = spriteSource?.GetSpriteMetrics(artId, request) ?? fallbackMetrics;
        if (metrics is null)
            return null;

        return spriteReferenceCache.GetOrAdd(
            new EditorMapPaintableSceneSpriteReferenceKey(
                artId,
                metrics.AssetPath,
                request.RenderItemKind,
                metrics.RotationIndex,
                metrics.FrameIndex,
                metrics.FramesPerRotation,
                metrics.FrameRate,
                request.ScalePercent,
                request.IsShrunk,
                metrics.Width,
                metrics.Height,
                metrics.CenterX,
                metrics.CenterY,
                metrics.DeltaX,
                metrics.DeltaY
            )
        );
    }

    private static EditorMapRenderSpriteRequest AdjustEyeCandyRequest(ArtId artId, EditorMapRenderSpriteRequest request)
    {
        if (artId.Type == ArtId.TypeCode.EyeCandy)
        {
            var scaleIndex = (int)((artId.Value >> 1) & 7);
            if (scaleIndex != 4)
            {
                int[] multipliers = [50, 63, 75, 87, 100, 130, 160, 200];
                return new EditorMapRenderSpriteRequest
                {
                    RenderItemKind = request.RenderItemKind,
                    RotationIndex = request.RotationIndex,
                    ScalePercent = (request.ScalePercent * multipliers[scaleIndex]) / 100,
                    IsShrunk = request.IsShrunk,
                };
            }
        }
        return request;
    }

    private static EditorMapRenderSpriteMetrics? CreateFallbackSpriteMetrics(
        ObjectType objectType,
        ArtId artId,
        EditorMapObjectSpriteBounds? spriteBounds
    )
    {
        if (spriteBounds is null)
            return null;

        var (centerX, centerY) = EditorMapFloorRenderBuilder.GetLayoutSpriteCenter(objectType, artId, spriteBounds);
        return new EditorMapRenderSpriteMetrics
        {
            Width = spriteBounds.MaxFrameWidth,
            Height = spriteBounds.MaxFrameHeight,
            CenterX = centerX,
            CenterY = centerY,
        };
    }

    private static ArtId? TryGetArtId(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.ArtId,
            EditorMapRenderQueueItemKind.Object => item.Object?.CurrentArtId,
            EditorMapRenderQueueItemKind.ObjectAuxiliary => item.ObjectAuxiliaryItem?.ArtId,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.ArtId,
            EditorMapRenderQueueItemKind.Light => item.Light?.ArtId,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.CurrentArtId,
            _ => null,
        };

    private static double GetSceneSpriteScaleX(EditorMapFloorRenderPreview sceneRender) =>
        sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? sceneRender.TileWidthPixels / 80d : 1d;

    private static double GetSceneSpriteScaleY(EditorMapFloorRenderPreview sceneRender) =>
        sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? sceneRender.TileHeightPixels / 40d : 1d;

    private static (
        double Left,
        double Top,
        double Width,
        double Height,
        int SourceWidth,
        int SourceHeight
    ) GetTerrainBlitLayout(
        EditorMapFloorRenderPreview sceneRender,
        double anchorX,
        double anchorY,
        EditorMapPaintableSceneSpriteReference? spriteReference
    )
    {
        if (sceneRender.ViewMode is not EditorMapSceneViewMode.Isometric)
        {
            var nonIsometricWidth =
                (spriteReference?.Width ?? sceneRender.TileWidthPixels) * GetSceneSpriteScaleX(sceneRender);
            var nonIsometricHeight =
                (spriteReference?.Height ?? sceneRender.TileHeightPixels) * GetSceneSpriteScaleY(sceneRender);
            var left = spriteReference is null
                ? anchorX - (nonIsometricWidth / 2d)
                : anchorX - (spriteReference.CenterX * GetSceneSpriteScaleX(sceneRender));
            var top = spriteReference is null
                ? anchorY - (nonIsometricHeight / 2d)
                : anchorY - (spriteReference.CenterY * GetSceneSpriteScaleY(sceneRender));
            return (
                left,
                top,
                nonIsometricWidth,
                nonIsometricHeight,
                Math.Max(1, spriteReference?.Width ?? (int)Math.Round(sceneRender.TileWidthPixels)),
                Math.Max(1, spriteReference?.Height ?? (int)Math.Round(sceneRender.TileHeightPixels))
            );
        }

        var scaleX = GetSceneSpriteScaleX(sceneRender);
        var scaleY = GetSceneSpriteScaleY(sceneRender);
        var isometricWidth = (spriteReference?.Width ?? CeTerrainBlitWidth) * scaleX;
        var isometricHeight = (spriteReference?.Height ?? CeTerrainBlitHeight) * scaleY;
        return (
            anchorX - (CeTerrainLayoutCenterX * scaleX),
            anchorY - (CeTerrainLayoutCenterY * scaleY),
            isometricWidth,
            isometricHeight,
            Math.Max(1, spriteReference?.Width ?? (int)CeTerrainBlitWidth),
            Math.Max(1, spriteReference?.Height ?? (int)CeTerrainBlitHeight)
        );
    }

    private static EditorMapRoofAlphaLerp? GetRoofAlphaLerp(ArtId artId)
    {
        if (!artId.IsRoofFaded)
            return null;

        const byte fullOpacity = 0;
        const byte partialOpacity = 128;
        const byte fullTransparency = 255;

        return artId.RoofPieceIndex switch
        {
            0 => new EditorMapRoofAlphaLerp(fullTransparency, partialOpacity, partialOpacity, fullTransparency),
            1 => new EditorMapRoofAlphaLerp(partialOpacity, fullOpacity, partialOpacity, fullTransparency),
            2 => new EditorMapRoofAlphaLerp(fullTransparency, partialOpacity, fullOpacity, partialOpacity),
            3 => new EditorMapRoofAlphaLerp(partialOpacity, fullOpacity, fullOpacity, partialOpacity),
            4 => new EditorMapRoofAlphaLerp(partialOpacity, partialOpacity, fullTransparency, fullTransparency),
            5 => new EditorMapRoofAlphaLerp(fullOpacity, fullOpacity, partialOpacity, partialOpacity),
            6 => new EditorMapRoofAlphaLerp(partialOpacity, partialOpacity, fullOpacity, fullOpacity),
            7 => new EditorMapRoofAlphaLerp(fullTransparency, fullTransparency, partialOpacity, partialOpacity),
            8 => new EditorMapRoofAlphaLerp(fullOpacity, fullOpacity, fullOpacity, fullOpacity),
            9 => new EditorMapRoofAlphaLerp(partialOpacity, fullTransparency, fullTransparency, partialOpacity),
            10 => new EditorMapRoofAlphaLerp(fullOpacity, partialOpacity, fullTransparency, partialOpacity),
            11 => new EditorMapRoofAlphaLerp(partialOpacity, fullTransparency, partialOpacity, fullOpacity),
            12 => new EditorMapRoofAlphaLerp(fullOpacity, fullOpacity, partialOpacity, partialOpacity),
            _ => null,
        };
    }

    private static int ResolveRotationIndex(float rotation)
    {
        if (!float.IsFinite(rotation))
            return 0;

        var normalizedTurns = MathF.Abs(rotation) > (MathF.Tau + 0.001f) ? rotation / 360f : rotation / MathF.Tau;
        normalizedTurns -= MathF.Floor(normalizedTurns);
        return checked((int)MathF.Round(normalizedTurns * 8f, MidpointRounding.AwayFromZero)) % 8;
    }

    private static SpriteReference? TryGetSpriteReference(EditorMapRenderQueueItem item)
    {
        var artId = TryGetArtId(item);
        if (artId is not { Value: not 0u } resolvedArtId)
            return null;

        var request = AdjustEyeCandyRequest(resolvedArtId, CreateSpriteRequest(item));
        return new SpriteReference(
            resolvedArtId,
            item.Kind,
            request.RotationIndex,
            request.ScalePercent,
            request.IsShrunk
        );
    }

    private static EditorMapRenderSpriteRequest CreateSpriteRequest(SpriteReference reference) =>
        CreateSpriteRequest(
            reference.RenderItemKind,
            reference.RotationIndex,
            reference.ScalePercent,
            reference.IsShrunk
        );

    private static EditorMapRenderSpriteRequest CreateSpriteRequest(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.Object when item.Object is { } obj => CreateSpriteRequest(
                item.Kind,
                obj.RotationIndex,
                obj.BlitScale,
                obj.IsShrunk
            ),
            EditorMapRenderQueueItemKind.ObjectAuxiliary when item.ObjectAuxiliaryItem is { } auxiliary =>
                CreateSpriteRequest(item.Kind, auxiliary.RotationIndex, auxiliary.ScalePercent, auxiliary.IsShrunk),
            EditorMapRenderQueueItemKind.PlacementPreviewObject when item.PlacementPreviewObject is { } previewObject =>
                CreateSpriteRequest(
                    item.Kind,
                    previewObject.RotationIndex,
                    previewObject.BlitScale,
                    previewObject.IsShrunk
                ),
            _ => CreateSpriteRequest(item.Kind),
        };

    private static EditorMapPaintableSceneGeometry CreateTileGeometry(
        EditorMapFloorRenderPreview sceneRender,
        double centerX,
        double centerY,
        int footprintWidth,
        int footprintHeight
    )
    {
        return new EditorMapPaintableSceneGeometry(
            sceneRender.ViewMode is EditorMapSceneViewMode.TopDown
                ? EditorMapPaintableSceneGeometryKind.Rectangle
                : EditorMapPaintableSceneGeometryKind.Diamond,
            centerX,
            centerY,
            sceneRender.TileWidthPixels * footprintWidth,
            sceneRender.TileHeightPixels * footprintHeight
        );
    }

    internal static EditorMapPaintableSceneItemBounds BuildItemBounds(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var layout = queueItem.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => GetFloorTileLayout(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.TileOverlay => GetTileOverlayLayout(sceneRender, queueItem),
            EditorMapRenderQueueItemKind.Object => GetObjectLayout(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.ObjectAuxiliary => GetObjectAuxiliaryLayout(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.Roof => GetRoofLayout(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.Light => GetLightLayout(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => GetPlacementPreviewObjectLayout(
                sceneRender,
                queueItem,
                spriteSource,
                spriteReferenceCache
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(queueItem.Kind),
                queueItem.Kind,
                "Unsupported render queue kind."
            ),
        };

        return new EditorMapPaintableSceneItemBounds(layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (
        double Left,
        double Top,
        double Width,
        double Height,
        EditorMapPaintableSceneSpriteSourceRect? SourceRect,
        EditorMapPaintableSceneSpriteDestinationRect? DestinationRect
    ) CreateItemLayout(
        double anchorX,
        double anchorY,
        double fallbackWidth,
        double fallbackHeight,
        EditorMapPaintableSceneSpriteReference? spriteReference,
        double? layoutCenterX = null,
        double? layoutCenterY = null,
        double sceneScaleX = 1d,
        double sceneScaleY = 1d
    )
    {
        var width = (spriteReference?.Width ?? fallbackWidth) * sceneScaleX;
        var height = (spriteReference?.Height ?? fallbackHeight) * sceneScaleY;
        var left = spriteReference is null
            ? anchorX - (width / 2d)
            : anchorX - ((layoutCenterX ?? spriteReference.CenterX) * sceneScaleX);
        var top = spriteReference is null
            ? anchorY - (height / 2d)
            : anchorY - ((layoutCenterY ?? spriteReference.CenterY) * sceneScaleY);
        EditorMapPaintableSceneSpriteSourceRect? spriteSourceRect = spriteReference is null
            ? null
            : new EditorMapPaintableSceneSpriteSourceRect(0, 0, spriteReference.Width, spriteReference.Height);
        EditorMapPaintableSceneSpriteDestinationRect? spriteDestinationRect = spriteReference is null
            ? null
            : new EditorMapPaintableSceneSpriteDestinationRect(left, top, width, height);
        return (left, top, width, height, spriteSourceRect, spriteDestinationRect);
    }

    private static (double Left, double Top, double Width, double Height) GetFloorTileLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var tile =
            queueItem.Tile
            ?? throw new InvalidOperationException("Floor tile queue items must carry one tile payload.");
        var spriteReference = TryCreateSpriteReference(
            tile.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.FloorTile),
            spriteSource,
            spriteReferenceCache
        );
        var layout = CreateItemLayout(
            tile.CenterX,
            tile.CenterY,
            sceneRender.TileWidthPixels,
            sceneRender.TileHeightPixels,
            spriteReference,
            sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? CeTerrainLayoutCenterX : null,
            sceneRender.ViewMode is EditorMapSceneViewMode.Isometric ? CeTerrainLayoutCenterY : null,
            GetSceneSpriteScaleX(sceneRender),
            GetSceneSpriteScaleY(sceneRender)
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (double Left, double Top, double Width, double Height) GetTileOverlayLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem
    )
    {
        var overlay =
            queueItem.TileOverlay
            ?? throw new InvalidOperationException("Tile overlay queue items must carry one tile-overlay payload.");
        var layout = CreateItemLayout(
            overlay.CenterX,
            overlay.CenterY,
            sceneRender.TileWidthPixels,
            sceneRender.TileHeightPixels,
            spriteReference: null
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (double Left, double Top, double Width, double Height) GetObjectLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var obj =
            queueItem.Object
            ?? throw new InvalidOperationException("Object queue items must carry one object payload.");
        var spriteReference = TryCreateSpriteReference(
            obj.CurrentArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Object, obj.RotationIndex, obj.BlitScale, obj.IsShrunk),
            spriteSource,
            spriteReferenceCache,
            CreateFallbackSpriteMetrics(obj.ObjectType, obj.CurrentArtId, obj.SpriteBounds)
        );
        var layout = CreateItemLayout(
            obj.AnchorX,
            obj.AnchorY,
            obj.SpriteBounds?.MaxFrameWidth ?? spriteReference?.Width ?? 0d,
            obj.SpriteBounds?.MaxFrameHeight ?? spriteReference?.Height ?? 0d,
            spriteReference,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (double Left, double Top, double Width, double Height) GetObjectAuxiliaryLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var auxiliary =
            queueItem.ObjectAuxiliaryItem
            ?? throw new InvalidOperationException(
                "Object-auxiliary queue items must carry one object-auxiliary payload."
            );
        var spriteReference = TryCreateSpriteReference(
            auxiliary.ArtId,
            CreateSpriteRequest(
                EditorMapRenderQueueItemKind.ObjectAuxiliary,
                auxiliary.RotationIndex,
                auxiliary.ScalePercent,
                auxiliary.IsShrunk
            ),
            spriteSource,
            spriteReferenceCache
        );
        var layout = CreateItemLayout(
            auxiliary.AnchorX,
            auxiliary.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (double Left, double Top, double Width, double Height) GetRoofLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var roof =
            queueItem.Roof ?? throw new InvalidOperationException("Roof queue items must carry one roof payload.");
        var spriteReference = TryCreateSpriteReference(
            roof.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Roof),
            spriteSource,
            spriteReferenceCache
        );
        var layout = CreateItemLayout(
            roof.AnchorX,
            roof.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (double Left, double Top, double Width, double Height) GetLightLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var light =
            queueItem.Light ?? throw new InvalidOperationException("Light queue items must carry one light payload.");
        var spriteReference = TryCreateSpriteReference(
            light.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Light),
            spriteSource,
            spriteReferenceCache
        );
        var layout = CreateItemLayout(
            light.AnchorX,
            light.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static (double Left, double Top, double Width, double Height) GetPlacementPreviewObjectLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var previewObject =
            queueItem.PlacementPreviewObject
            ?? throw new InvalidOperationException(
                "Placement-preview queue items must carry one placement-preview payload."
            );
        var spriteReference = TryCreateSpriteReference(
            previewObject.CurrentArtId,
            CreateSpriteRequest(
                EditorMapRenderQueueItemKind.PlacementPreviewObject,
                previewObject.RotationIndex,
                previewObject.BlitScale,
                previewObject.IsShrunk
            ),
            spriteSource,
            spriteReferenceCache,
            CreateFallbackSpriteMetrics(
                previewObject.ObjectType,
                previewObject.CurrentArtId,
                previewObject.SpriteBounds
            )
        );
        var layout = CreateItemLayout(
            previewObject.AnchorX,
            previewObject.AnchorY,
            previewObject.SpriteBounds?.MaxFrameWidth ?? spriteReference?.Width ?? 0d,
            previewObject.SpriteBounds?.MaxFrameHeight ?? spriteReference?.Height ?? 0d,
            spriteReference,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
        return (layout.Left, layout.Top, layout.Width, layout.Height);
    }

    private static FloorTileQuadrantLayout GetFloorTileQuadrantLayout(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapPaintableSceneSpriteReferenceCache spriteReferenceCache
    )
    {
        var tile =
            queueItem.Tile
            ?? throw new InvalidOperationException("Floor tile queue items must carry one tile payload.");
        var spriteReference = TryCreateSpriteReference(
            tile.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.FloorTile),
            spriteSource,
            spriteReferenceCache
        );
        var (left, top, fullWidth, fullHeight, sourceWidth, sourceHeight) = GetTerrainBlitLayout(
            sceneRender,
            tile.CenterX,
            tile.CenterY,
            spriteReference
        );
        var leftSourceWidth = Math.Max(1, sourceWidth / 2);
        var rightSourceWidth = Math.Max(1, sourceWidth - leftSourceWidth);
        var topSourceHeight = Math.Max(1, sourceHeight / 2);
        var bottomSourceHeight = Math.Max(1, sourceHeight - topSourceHeight);
        var leftDestinationWidth = fullWidth * leftSourceWidth / sourceWidth;
        var rightDestinationWidth = fullWidth - leftDestinationWidth;
        var topDestinationHeight = fullHeight * topSourceHeight / sourceHeight;
        var bottomDestinationHeight = fullHeight - topDestinationHeight;
        var footprintWidth = sceneRender.TileWidthPixels;
        var footprintHeight = sceneRender.TileHeightPixels;
        var halfFootprintWidth = footprintWidth / 2d;
        var halfFootprintHeight = footprintHeight / 2d;
        var footprintLeft = tile.CenterX - halfFootprintWidth;
        var footprintTop = tile.CenterY - halfFootprintHeight;
        var quadrants = new FloorTileQuadrantInfo[4];

        for (int i = 0; i < 4; i++)
        {
            var isRight = i % 2 == 1;
            var isBottom = i >= 2;
            var sourceX = isRight ? leftSourceWidth : 0;
            var sourceY = isBottom ? topSourceHeight : 0;
            var destinationWidth = isRight ? rightDestinationWidth : leftDestinationWidth;
            var destinationHeight = isBottom ? bottomDestinationHeight : topDestinationHeight;
            var qLeft = left + (isRight ? leftDestinationWidth : 0d);
            var qTop = top + (isBottom ? topDestinationHeight : 0d);
            var sourceRect = new EditorMapPaintableSceneSpriteSourceRect(
                sourceX,
                sourceY,
                isRight ? rightSourceWidth : leftSourceWidth,
                isBottom ? bottomSourceHeight : topSourceHeight
            );
            var destinationRect = new EditorMapPaintableSceneSpriteDestinationRect(
                qLeft,
                qTop,
                destinationWidth,
                destinationHeight
            );
            var footprintQuadrantLeft = footprintLeft + (isRight ? halfFootprintWidth : 0d);
            var footprintQuadrantTop = footprintTop + (isBottom ? halfFootprintHeight : 0d);
            var geometryCenterX =
                sceneRender.ViewMode is EditorMapSceneViewMode.TopDown
                    ? footprintQuadrantLeft + (halfFootprintWidth / 2d)
                    : footprintQuadrantLeft + (halfFootprintWidth / 2d);
            var geometryCenterY =
                sceneRender.ViewMode is EditorMapSceneViewMode.TopDown
                    ? footprintQuadrantTop + (halfFootprintHeight / 2d)
                    : footprintQuadrantTop + (halfFootprintHeight / 2d);

            quadrants[i] = new FloorTileQuadrantInfo(
                qLeft,
                qTop,
                destinationWidth,
                destinationHeight,
                qLeft + (destinationWidth / 2d),
                qTop + (destinationHeight / 2d),
                sourceRect,
                destinationRect,
                new EditorMapPaintableSceneGeometry(
                    sceneRender.ViewMode is EditorMapSceneViewMode.TopDown
                        ? EditorMapPaintableSceneGeometryKind.Rectangle
                        : EditorMapPaintableSceneGeometryKind.Diamond,
                    geometryCenterX,
                    geometryCenterY,
                    halfFootprintWidth,
                    halfFootprintHeight
                )
            );
        }

        return new FloorTileQuadrantLayout(spriteReference, quadrants);
    }

    private readonly record struct FloorTileQuadrantLayout(
        EditorMapPaintableSceneSpriteReference? SpriteReference,
        FloorTileQuadrantInfo[] Quadrants
    );

    private readonly record struct FloorTileQuadrantInfo(
        double Left,
        double Top,
        double Width,
        double Height,
        double AnchorX,
        double AnchorY,
        EditorMapPaintableSceneSpriteSourceRect SourceRect,
        EditorMapPaintableSceneSpriteDestinationRect DestinationRect,
        EditorMapPaintableSceneGeometry Geometry
    );
}
