using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// One paintable scene item projected for host renderers such as Avalonia or Skia.
/// </summary>
public sealed class EditorMapPaintableSceneItem
{
    public required EditorMapRenderQueueItemKind Kind { get; init; }
    public required int DrawOrder { get; init; }
    public required double SortKey { get; init; }
    public required double Left { get; init; }
    public required double Top { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required double AnchorX { get; init; }
    public required double AnchorY { get; init; }
    public required double SuggestedOpacity { get; init; }
    public uint? SuggestedTintColor { get; init; }
    public EditorMapRenderSprite? Sprite { get; init; }
    public IReadOnlyList<EditorMapRenderPoint>? GeometryPoints { get; init; }
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
    public required IReadOnlyList<EditorMapPaintableSceneItem> Items { get; init; }
    public required EditorMapRenderSpriteCoverage SpriteCoverage { get; init; }
}

/// <summary>
/// Builds host-ready paintable scene items from normalized committed and live scene render queues.
/// </summary>
public static class EditorMapPaintableSceneBuilder
{
    /// <summary>
    /// Builds one paintable scene from the committed render queue and one optional placement-preview queue.
    /// When <paramref name="spriteSource"/> is supplied, ART-backed queue items are enriched with cached preview frames.
    /// </summary>
    public static EditorMapPaintableScene Build(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapPlacementPreview? placementPreview = null,
        IEditorMapRenderSpriteSource? spriteSource = null
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        var queue = placementPreview?.RenderQueue ?? sceneRender.RenderQueue;
        var items = queue.Select(item => BuildItem(sceneRender, item, spriteSource)).ToArray();
        var spriteCoverage = BuildSpriteCoverage(queue, spriteSource);

        return new EditorMapPaintableScene
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            WidthPixels = Math.Max(sceneRender.WidthPixels, placementPreview?.WidthPixels ?? 0d),
            HeightPixels = Math.Max(sceneRender.HeightPixels, placementPreview?.HeightPixels ?? 0d),
            Items = items,
            SpriteCoverage = spriteCoverage,
        };
    }

    private static EditorMapRenderSpriteCoverage BuildSpriteCoverage(
        IReadOnlyList<EditorMapRenderQueueItem> queue,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var referencedArtIds = queue
            .Select(TryGetArtId)
            .Where(static artId => artId is { Value: not 0u })
            .Select(static artId => artId!.Value)
            .Distinct()
            .OrderBy(static artId => artId.Value)
            .ToArray();

        var resolvedArtIds = referencedArtIds
            .Where(artId => spriteSource?.Resolve(artId) is not null)
            .OrderBy(static artId => artId.Value)
            .ToArray();
        var resolvedLookup = resolvedArtIds.ToHashSet();
        var unresolvedArtIds = referencedArtIds
            .Where(artId => !resolvedLookup.Contains(artId))
            .OrderBy(static artId => artId.Value)
            .ToArray();

        return new EditorMapRenderSpriteCoverage
        {
            ReferencedArtIds = referencedArtIds,
            ResolvedArtIds = resolvedArtIds,
            UnresolvedArtIds = unresolvedArtIds,
        };
    }

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
            EditorMapRenderQueueItemKind.Object => BuildObject(queueItem, spriteSource),
            EditorMapRenderQueueItemKind.Roof => BuildRoof(queueItem, spriteSource),
            EditorMapRenderQueueItemKind.PlacementPreviewObject => BuildPlacementPreviewObject(queueItem, spriteSource),
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
        var sprite = spriteSource?.Resolve(tile.ArtId);
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
            sprite,
            geometry,
            suggestedOpacity: 1d,
            suggestedTintColor: null
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
            sprite: null,
            geometry,
            overlay.SuggestedOpacity,
            overlay.SuggestedTintColor
        );
    }

    private static EditorMapPaintableSceneItem BuildObject(
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var obj =
            queueItem.Object
            ?? throw new InvalidOperationException("Object queue items must carry one object payload.");
        var sprite = spriteSource?.Resolve(obj.CurrentArtId);
        return CreateItem(
            queueItem,
            obj.AnchorX,
            obj.AnchorY,
            obj.SpriteBounds?.MaxFrameWidth ?? sprite?.Width ?? 0d,
            obj.SpriteBounds?.MaxFrameHeight ?? sprite?.Height ?? 0d,
            sprite,
            geometryPoints: null,
            suggestedOpacity: 1d,
            suggestedTintColor: null
        );
    }

    private static EditorMapPaintableSceneItem BuildRoof(
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var roof =
            queueItem.Roof ?? throw new InvalidOperationException("Roof queue items must carry one roof payload.");
        var sprite = spriteSource?.Resolve(roof.ArtId);
        return CreateItem(
            queueItem,
            roof.AnchorX,
            roof.AnchorY,
            sprite?.Width ?? 0d,
            sprite?.Height ?? 0d,
            sprite,
            geometryPoints: null,
            suggestedOpacity: 1d,
            suggestedTintColor: null
        );
    }

    private static EditorMapPaintableSceneItem BuildPlacementPreviewObject(
        EditorMapRenderQueueItem queueItem,
        IEditorMapRenderSpriteSource? spriteSource
    )
    {
        var previewObject =
            queueItem.PlacementPreviewObject
            ?? throw new InvalidOperationException(
                "Placement-preview queue items must carry one placement-preview payload."
            );
        var sprite = spriteSource?.Resolve(previewObject.CurrentArtId);
        return CreateItem(
            queueItem,
            previewObject.AnchorX,
            previewObject.AnchorY,
            previewObject.SpriteBounds?.MaxFrameWidth ?? sprite?.Width ?? 0d,
            previewObject.SpriteBounds?.MaxFrameHeight ?? sprite?.Height ?? 0d,
            sprite,
            geometryPoints: null,
            previewObject.SuggestedOpacity,
            previewObject.SuggestedTintColor
        );
    }

    private static EditorMapPaintableSceneItem CreateItem(
        EditorMapRenderQueueItem queueItem,
        double anchorX,
        double anchorY,
        double fallbackWidth,
        double fallbackHeight,
        EditorMapRenderSprite? sprite,
        IReadOnlyList<EditorMapRenderPoint>? geometryPoints,
        double suggestedOpacity,
        uint? suggestedTintColor
    )
    {
        var width = sprite?.Width ?? fallbackWidth;
        var height = sprite?.Height ?? fallbackHeight;
        var left = sprite is null ? anchorX - (width / 2d) : anchorX - sprite.CenterX;
        var top = sprite is null ? anchorY - (height / 2d) : anchorY - sprite.CenterY;

        return new EditorMapPaintableSceneItem
        {
            Kind = queueItem.Kind,
            DrawOrder = queueItem.DrawOrder,
            SortKey = queueItem.SortKey,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            AnchorX = anchorX,
            AnchorY = anchorY,
            SuggestedOpacity = suggestedOpacity,
            SuggestedTintColor = suggestedTintColor,
            Sprite = sprite,
            GeometryPoints = geometryPoints,
        };
    }

    private static ArtId? TryGetArtId(EditorMapRenderQueueItem item) =>
        item.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => item.Tile?.ArtId,
            EditorMapRenderQueueItemKind.Object => item.Object?.CurrentArtId,
            EditorMapRenderQueueItemKind.Roof => item.Roof?.ArtId,
            EditorMapRenderQueueItemKind.PlacementPreviewObject => item.PlacementPreviewObject?.CurrentArtId,
            _ => null,
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
