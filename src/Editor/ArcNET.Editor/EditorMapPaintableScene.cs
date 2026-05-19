using ArcNET.Core.Primitives;
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
}

/// <summary>
/// Lightweight sprite lookup key and frame metrics retained by paintable scene items.
/// Hosts can resolve pixels lazily through the owning scene sprite source when an item becomes visible.
/// </summary>
public sealed class EditorMapPaintableSceneSpriteReference
{
    public required ArtId ArtId { get; init; }
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
    internal EditorMapPaintableSceneViewportIndex? ViewportIndex { get; init; }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(EditorMapSceneViewportLayout viewport)
    {
        if (ViewportIndex is null)
        {
            for (var itemIndex = 0; itemIndex < Items.Count; itemIndex++)
            {
                var item = Items[itemIndex];
                if (IntersectsViewport(item, viewport))
                    yield return item;
            }

            yield break;
        }

        foreach (var item in ViewportIndex.EnumerateVisibleItems(Items, viewport))
            yield return item;
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
}

internal sealed class EditorMapPaintableSceneViewportIndex
{
    private const double CellSize = 512d;
    private readonly IReadOnlyDictionary<(int X, int Y), int[]> _itemIndicesByCell;

    private EditorMapPaintableSceneViewportIndex(IReadOnlyDictionary<(int X, int Y), int[]> itemIndicesByCell)
    {
        _itemIndicesByCell = itemIndicesByCell;
    }

    public static EditorMapPaintableSceneViewportIndex Create(IReadOnlyList<EditorMapPaintableSceneItem> items)
    {
        var mutableItemIndicesByCell = new Dictionary<(int X, int Y), List<int>>();
        for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            var item = items[itemIndex];
            var itemWidth = Math.Max(1d, item.Width);
            var itemHeight = Math.Max(1d, item.Height);
            var minCellX = GetCellIndex(item.Left);
            var maxCellX = GetCellIndex(item.Left + itemWidth);
            var minCellY = GetCellIndex(item.Top);
            var maxCellY = GetCellIndex(item.Top + itemHeight);

            for (var cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                for (var cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    var cellKey = (cellX, cellY);
                    if (!mutableItemIndicesByCell.TryGetValue(cellKey, out var cellItemIndices))
                    {
                        cellItemIndices = [];
                        mutableItemIndicesByCell[cellKey] = cellItemIndices;
                    }

                    cellItemIndices.Add(itemIndex);
                }
            }
        }

        return new EditorMapPaintableSceneViewportIndex(
            mutableItemIndicesByCell.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray())
        );
    }

    public IEnumerable<EditorMapPaintableSceneItem> EnumerateVisibleItems(
        IReadOnlyList<EditorMapPaintableSceneItem> items,
        EditorMapSceneViewportLayout viewport
    )
    {
        var minCellX = GetCellIndex(viewport.VisibleLeft);
        var maxCellX = GetCellIndex(viewport.VisibleRight);
        var minCellY = GetCellIndex(viewport.VisibleTop);
        var maxCellY = GetCellIndex(viewport.VisibleBottom);
        var visibleItemIndices = new List<int>();
        var seenItemIndices = new HashSet<int>();

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (!_itemIndicesByCell.TryGetValue((cellX, cellY), out var cellItemIndices))
                    continue;

                for (var indexInCell = 0; indexInCell < cellItemIndices.Length; indexInCell++)
                {
                    var itemIndex = cellItemIndices[indexInCell];
                    if (!seenItemIndices.Add(itemIndex))
                        continue;

                    visibleItemIndices.Add(itemIndex);
                }
            }
        }

        visibleItemIndices.Sort();
        for (var visibleIndex = 0; visibleIndex < visibleItemIndices.Count; visibleIndex++)
        {
            var item = items[visibleItemIndices[visibleIndex]];
            if (EditorMapPaintableScene.IntersectsViewport(item, viewport))
                yield return item;
        }
    }

    private static int GetCellIndex(double coordinate) => (int)Math.Floor(coordinate / CellSize);
}

/// <summary>
/// Builds host-ready paintable scene items from normalized committed and live scene render queues.
/// </summary>
public static class EditorMapPaintableSceneBuilder
{
    private const int ParallelItemBuildThreshold = 512;

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
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        var queue = placementPreview?.RenderQueue ?? sceneRender.RenderQueue;
        var items = BuildItems(sceneRender, queue, spriteSource, cancellationToken);

        var spriteCoverage = BuildSpriteCoverage(queue, spriteSource, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return new EditorMapPaintableScene
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            WidthPixels = Math.Max(sceneRender.WidthPixels, placementPreview?.WidthPixels ?? 0d),
            HeightPixels = Math.Max(sceneRender.HeightPixels, placementPreview?.HeightPixels ?? 0d),
            SpriteSource = spriteSource,
            Items = items,
            SpriteCoverage = spriteCoverage,
            ViewportIndex = EditorMapPaintableSceneViewportIndex.Create(items),
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

        EditorMapRenderQueueItem[] queue = placementPreview is null
            ? []
            : placementPreview
                .RenderQueue.Where(static item => item.Kind is EditorMapRenderQueueItemKind.PlacementPreviewObject)
                .ToArray();
        var items = BuildItems(sceneRender, queue, spriteSource, cancellationToken);

        var spriteCoverage = BuildSpriteCoverage(queue, spriteSource, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return new EditorMapPaintableScene
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            WidthPixels = sceneRender.WidthPixels,
            HeightPixels = sceneRender.HeightPixels,
            SpriteSource = spriteSource,
            Items = items,
            SpriteCoverage = spriteCoverage,
            ViewportIndex = EditorMapPaintableSceneViewportIndex.Create(items),
        };
    }

    private static EditorMapPaintableSceneItem[] BuildItems(
        EditorMapFloorRenderPreview sceneRender,
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource,
        CancellationToken cancellationToken
    )
    {
        var items = new EditorMapPaintableSceneItem[queue.Count];
        if (queue.Count < ParallelItemBuildThreshold)
        {
            for (var itemIndex = 0; itemIndex < queue.Count; itemIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                items[itemIndex] = BuildItem(sceneRender, queue[itemIndex], spriteSource);
            }

            return items;
        }

        Parallel.For(
            0,
            queue.Count,
            new ParallelOptions { CancellationToken = cancellationToken },
            itemIndex => items[itemIndex] = BuildItem(sceneRender, queue[itemIndex], spriteSource)
        );
        cancellationToken.ThrowIfCancellationRequested();
        return items;
    }

    private static EditorMapRenderSpriteCoverage BuildSpriteCoverage(
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var referencedSpriteReferences = queue
            .Select(TryGetSpriteReference)
            .Where(static reference => reference is { ArtId.Value: not 0u })
            .Select(static reference => reference!.Value)
            .Distinct()
            .OrderBy(static reference => reference.ArtId.Value)
            .ThenBy(static reference => reference.RenderItemKind)
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();
        var resolvedSpriteReferences = referencedSpriteReferences
            .Where(reference => spriteSource?.CanResolve(reference.ArtId, CreateSpriteRequest(reference)) == true)
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedLookup = resolvedSpriteReferences.ToHashSet();
        var unresolvedSpriteReferences = referencedSpriteReferences
            .Where(reference => !resolvedLookup.Contains(reference))
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        var referencedArtIds = referencedSpriteReferences
            .Select(static reference => reference.ArtId)
            .Distinct()
            .OrderBy(static artId => artId.Value)
            .ToArray();
        var resolvedArtIds = resolvedSpriteReferences
            .Select(static reference => reference.ArtId)
            .Distinct()
            .OrderBy(static artId => artId.Value)
            .ToArray();
        var unresolvedArtIds = unresolvedSpriteReferences
            .Select(static reference => reference.ArtId)
            .Distinct()
            .OrderBy(static artId => artId.Value)
            .ToArray();

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
    ) =>
        new()
        {
            RenderItemKind = renderItemKind,
            RotationIndex = rotationIndex,
            ScalePercent = scalePercent,
            IsShrunk = isShrunk,
        };

    private static EditorMapRenderSpriteRequest CreateSpriteRequestFromRotation(
        EditorMapRenderQueueItemKind renderItemKind,
        float rotation,
        int scalePercent = 100,
        bool isShrunk = false
    ) => CreateSpriteRequest(renderItemKind, ResolveRotationIndex(rotation), scalePercent, isShrunk);

    private static EditorMapPaintableSceneItem BuildItem(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        return queueItem.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => BuildFloorTile(sceneRender, queueItem, spriteSource),
            EditorMapRenderQueueItemKind.TileOverlay => BuildTileOverlay(sceneRender, queueItem),
            EditorMapRenderQueueItemKind.Object => BuildObject(sceneRender, queueItem, spriteSource),
            EditorMapRenderQueueItemKind.ObjectAuxiliary => BuildObjectAuxiliary(sceneRender, queueItem, spriteSource),
            EditorMapRenderQueueItemKind.Roof => BuildRoof(sceneRender, queueItem, spriteSource),
            EditorMapRenderQueueItemKind.Light => BuildLight(sceneRender, queueItem, spriteSource),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => BuildPlacementPreviewObject(
                sceneRender,
                queueItem,
                spriteSource
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
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var tile =
            queueItem.Tile
            ?? throw new InvalidOperationException("Floor tile queue items must carry one tile payload.");
        var spriteReference = TryCreateSpriteReference(
            tile.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.FloorTile),
            spriteSource
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
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

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
        IEditorMapRenderSpriteSource? spriteSource
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
            CreateFallbackSpriteMetrics(obj.ObjectType, obj.CurrentArtId, obj.SpriteBounds)
        );

        return CreateItem(
            queueItem,
            obj.AnchorX,
            obj.AnchorY,
            obj.SpriteBounds?.MaxFrameWidth ?? spriteReference?.Width ?? 0d,
            obj.SpriteBounds?.MaxFrameHeight ?? spriteReference?.Height ?? 0d,
            spriteReference,
            geometryPoints: null,
            suggestedOpacity: 1d,
            suggestedTintColor: null,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildObjectAuxiliary(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
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
        var spriteReference = TryCreateSpriteReference(auxiliary.ArtId, request, spriteSource);

        return CreateItem(
            queueItem,
            auxiliary.AnchorX,
            auxiliary.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            geometryPoints: null,
            suggestedOpacity: 1d,
            auxiliary.SuggestedTintColor,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildRoof(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var roof =
            queueItem.Roof ?? throw new InvalidOperationException("Roof queue items must carry one roof payload.");
        var spriteReference = TryCreateSpriteReference(
            roof.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Roof),
            spriteSource
        );
        return CreateItem(
            queueItem,
            roof.AnchorX,
            roof.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            geometryPoints: null,
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
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var light =
            queueItem.Light ?? throw new InvalidOperationException("Light queue items must carry one light payload.");
        var spriteReference = TryCreateSpriteReference(
            light.ArtId,
            CreateSpriteRequest(EditorMapRenderQueueItemKind.Light),
            spriteSource
        );
        return CreateItem(
            queueItem,
            light.AnchorX,
            light.AnchorY,
            spriteReference?.Width ?? 0d,
            spriteReference?.Height ?? 0d,
            spriteReference,
            geometryPoints: null,
            light.SuggestedOpacity,
            light.SuggestedTintColor,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem BuildPlacementPreviewObject(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
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
            geometryPoints: null,
            previewObject.SuggestedOpacity,
            previewObject.SuggestedTintColor,
            sceneScaleX: GetSceneSpriteScaleX(sceneRender),
            sceneScaleY: GetSceneSpriteScaleY(sceneRender)
        );
    }

    private static EditorMapPaintableSceneItem CreateItem(
        EditorMapRenderQueueItem queueItem,
        double anchorX,
        double anchorY,
        double fallbackWidth,
        double fallbackHeight,
        EditorMapPaintableSceneSpriteReference? spriteReference,
        IReadOnlyList<EditorMapRenderPoint>? geometryPoints,
        double suggestedOpacity,
        uint? suggestedTintColor,
        EditorMapRoofAlphaLerp? roofAlphaLerp = null,
        double sceneScaleX = 1d,
        double sceneScaleY = 1d
    )
    {
        var width = (spriteReference?.Width ?? fallbackWidth) * sceneScaleX;
        var height = (spriteReference?.Height ?? fallbackHeight) * sceneScaleY;
        var left = spriteReference is null ? anchorX - (width / 2d) : anchorX - (spriteReference.CenterX * sceneScaleX);
        var top = spriteReference is null ? anchorY - (height / 2d) : anchorY - (spriteReference.CenterY * sceneScaleY);
        EditorMapPaintableSceneSpriteSourceRect? spriteSourceRect = spriteReference is null
            ? null
            : new EditorMapPaintableSceneSpriteSourceRect(0, 0, spriteReference.Width, spriteReference.Height);
        EditorMapPaintableSceneSpriteDestinationRect? spriteDestinationRect = spriteReference is null
            ? null
            : new EditorMapPaintableSceneSpriteDestinationRect(left, top, width, height);

        return new EditorMapPaintableSceneItem
        {
            Kind = queueItem.Kind,
            DrawOrder = queueItem.DrawOrder,
            SortKey = queueItem.SortKey,
            CommittedRenderLayer =
                queueItem.Object?.CommittedRenderLayer ?? queueItem.ObjectAuxiliaryItem?.CommittedRenderLayer,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            AnchorX = anchorX,
            AnchorY = anchorY,
            SuggestedOpacity = suggestedOpacity,
            SuggestedTintColor = suggestedTintColor,
            TintIgnoresLightVisibility = false,
            UseLightMaskTint = queueItem.ObjectAuxiliaryItem?.ArtId.Type is ArtId.TypeCode.Light,
            TileLightDiagnostics = queueItem.Tile?.LightDiagnostics,
            TileOverlayKind = queueItem.TileOverlay?.Kind,
            SpriteSourceRect = spriteSourceRect,
            SpriteDestinationRect = spriteDestinationRect,
            IsRoofCovered = queueItem.ObjectAuxiliaryItem?.IsRoofCovered ?? false,
            RoofAlphaLerp = roofAlphaLerp,
            BlendMode = queueItem.ObjectAuxiliaryItem?.BlendMode ?? EditorMapSpriteBlendMode.SourceOver,
            UseSubtractiveShadowBlend = queueItem.ObjectAuxiliaryItem?.BlendMode is EditorMapSpriteBlendMode.Subtract,
            SpriteReference = spriteReference,
            GeometryPoints = geometryPoints,
        };
    }

    private static EditorMapPaintableSceneSpriteReference? TryCreateSpriteReference(
        ArtId artId,
        EditorMapRenderSpriteRequest request,
        IEditorMapRenderSpriteSource? spriteSource,
        EditorMapRenderSpriteMetrics? fallbackMetrics = null
    )
    {
        if (artId.Value == 0)
            return null;

        var metrics = spriteSource?.GetSpriteMetrics(artId, request) ?? fallbackMetrics;
        if (metrics is null)
            return null;

        return new EditorMapPaintableSceneSpriteReference
        {
            ArtId = artId,
            RenderItemKind = request.RenderItemKind,
            RotationIndex = metrics.RotationIndex,
            FrameIndex = metrics.FrameIndex,
            ScalePercent = request.ScalePercent,
            IsShrunk = request.IsShrunk,
            Width = metrics.Width,
            Height = metrics.Height,
            CenterX = metrics.CenterX,
            CenterY = metrics.CenterY,
        };
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

        var request = CreateSpriteRequest(item);
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

    private static IReadOnlyList<EditorMapRenderPoint> CreateTileGeometry(
        EditorMapFloorRenderPreview sceneRender,
        double centerX,
        double centerY,
        int footprintWidth,
        int footprintHeight
    )
    {
        if (sceneRender.ViewMode is EditorMapSceneViewMode.TopDown)
        {
            var width = sceneRender.TileWidthPixels * footprintWidth;
            var height = sceneRender.TileHeightPixels * footprintHeight;
            return
            [
                new EditorMapRenderPoint(
                    centerX - (sceneRender.TileWidthPixels / 2d),
                    centerY - (sceneRender.TileHeightPixels / 2d)
                ),
                new EditorMapRenderPoint(
                    centerX - (sceneRender.TileWidthPixels / 2d) + width,
                    centerY - (sceneRender.TileHeightPixels / 2d)
                ),
                new EditorMapRenderPoint(
                    centerX - (sceneRender.TileWidthPixels / 2d) + width,
                    centerY - (sceneRender.TileHeightPixels / 2d) + height
                ),
                new EditorMapRenderPoint(
                    centerX - (sceneRender.TileWidthPixels / 2d),
                    centerY - (sceneRender.TileHeightPixels / 2d) + height
                ),
            ];
        }

        return
        [
            new EditorMapRenderPoint(centerX, centerY - ((sceneRender.TileHeightPixels / 2d) * footprintHeight)),
            new EditorMapRenderPoint(centerX + ((sceneRender.TileWidthPixels / 2d) * footprintWidth), centerY),
            new EditorMapRenderPoint(centerX, centerY + ((sceneRender.TileHeightPixels / 2d) * footprintHeight)),
            new EditorMapRenderPoint(centerX - ((sceneRender.TileWidthPixels / 2d) * footprintWidth), centerY),
        ];
    }
}
