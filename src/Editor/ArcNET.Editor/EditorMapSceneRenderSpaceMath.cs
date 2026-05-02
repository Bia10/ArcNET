using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Render-space camera state for one normalized map scene.
/// Hosts can persist this independently from tile-space camera state when they render the projected scene directly.
/// </summary>
public sealed class EditorMapRenderViewportState
{
    /// <summary>
    /// Render-space X coordinate that should appear at the viewport center.
    /// </summary>
    public double CenterRenderX { get; init; }

    /// <summary>
    /// Render-space Y coordinate that should appear at the viewport center.
    /// </summary>
    public double CenterRenderY { get; init; }

    /// <summary>
    /// Render-space zoom factor.
    /// </summary>
    public double Zoom { get; init; } = 1d;
}

/// <summary>
/// One viewport-to-render-space transform for a normalized map scene.
/// </summary>
public sealed class EditorMapSceneViewportLayout
{
    public required double ViewportWidth { get; init; }
    public required double ViewportHeight { get; init; }
    public required double SceneWidth { get; init; }
    public required double SceneHeight { get; init; }
    public required double CenterRenderX { get; init; }
    public required double CenterRenderY { get; init; }
    public required double Zoom { get; init; }

    public double VisibleLeft => CenterRenderX - ((ViewportWidth / Zoom) / 2d);
    public double VisibleTop => CenterRenderY - ((ViewportHeight / Zoom) / 2d);
    public double VisibleRight => CenterRenderX + ((ViewportWidth / Zoom) / 2d);
    public double VisibleBottom => CenterRenderY + ((ViewportHeight / Zoom) / 2d);
}

/// <summary>
/// One point in normalized render space.
/// </summary>
public readonly record struct EditorMapRenderPoint(double X, double Y);

/// <summary>
/// One point in fractional map-tile space.
/// </summary>
public readonly record struct EditorMapTilePoint(double MapTileX, double MapTileY);

/// <summary>
/// One rendered-scene hit resolved from viewport or render-space coordinates.
/// </summary>
public sealed class EditorMapRenderHit
{
    private const int TilesPerRoofCellAxis = 4;

    public required int MapTileX { get; init; }
    public required int MapTileY { get; init; }
    public required string SectorAssetPath { get; init; }
    public required Location Tile { get; init; }
    public required EditorMapRenderPoint RenderPoint { get; init; }
    public required IReadOnlyList<EditorMapObjectRenderItem> ObjectHits { get; init; }

    public Location RoofCell =>
        new(checked((short)(Tile.X / TilesPerRoofCellAxis)), checked((short)(Tile.Y / TilesPerRoofCellAxis)));

    public bool HasObjectHits => ObjectHits.Count > 0;
}

/// <summary>
/// Shared viewport, render-space, and map-tile math for normalized scene rendering.
/// Unlike <see cref="EditorMapCameraMath"/>, this contract works against the projected scene preview itself.
/// </summary>
public static class EditorMapSceneRenderSpaceMath
{
    /// <summary>
    /// Creates one render-space viewport state centered on the supplied scene, or projects the supplied tile-space camera
    /// into the scene's normalized render space when <paramref name="camera"/> is provided.
    /// </summary>
    public static EditorMapRenderViewportState CreateViewportState(
        EditorMapFloorRenderPreview sceneRender,
        EditorProjectMapCameraState? camera = null
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);

        if (camera is null)
        {
            return new EditorMapRenderViewportState
            {
                CenterRenderX = sceneRender.WidthPixels / 2d,
                CenterRenderY = sceneRender.HeightPixels / 2d,
                Zoom = 1d,
            };
        }

        var center = ProjectMapTileCenter(sceneRender, camera.CenterTileX, camera.CenterTileY);
        return new EditorMapRenderViewportState
        {
            CenterRenderX = center.X,
            CenterRenderY = center.Y,
            Zoom = camera.Zoom,
        };
    }

    /// <summary>
    /// Creates one viewport layout that maps viewport pixels to the scene's normalized render space.
    /// </summary>
    public static EditorMapSceneViewportLayout CreateViewportLayout(
        EditorMapFloorRenderPreview sceneRender,
        double viewportWidth,
        double viewportHeight,
        EditorMapRenderViewportState? viewportState = null
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);
        ValidatePositiveFinite(nameof(viewportWidth), viewportWidth);
        ValidatePositiveFinite(nameof(viewportHeight), viewportHeight);

        var effectiveViewport = viewportState ?? CreateViewportState(sceneRender);
        ValidatePositiveFinite(nameof(effectiveViewport.Zoom), effectiveViewport.Zoom);

        return new EditorMapSceneViewportLayout
        {
            ViewportWidth = viewportWidth,
            ViewportHeight = viewportHeight,
            SceneWidth = sceneRender.WidthPixels,
            SceneHeight = sceneRender.HeightPixels,
            CenterRenderX = effectiveViewport.CenterRenderX,
            CenterRenderY = effectiveViewport.CenterRenderY,
            Zoom = effectiveViewport.Zoom,
        };
    }

    /// <summary>
    /// Projects one fractional map-tile coordinate into the scene's normalized render space.
    /// </summary>
    public static EditorMapRenderPoint ProjectMapTileCenter(
        EditorMapFloorRenderPreview sceneRender,
        double mapTileX,
        double mapTileY
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);
        ValidateFinite(nameof(mapTileX), mapTileX);
        ValidateFinite(nameof(mapTileY), mapTileY);

        var sampleTile = GetReferenceTile(sceneRender);
        return sceneRender.ViewMode switch
        {
            EditorMapSceneViewMode.TopDown => new EditorMapRenderPoint(
                sampleTile.CenterX + ((mapTileX - sampleTile.MapTileX) * sceneRender.TileWidthPixels),
                sampleTile.CenterY + ((mapTileY - sampleTile.MapTileY) * sceneRender.TileHeightPixels)
            ),
            EditorMapSceneViewMode.Isometric => new EditorMapRenderPoint(
                sampleTile.CenterX
                    + ((mapTileX - sampleTile.MapTileX) + (mapTileY - sampleTile.MapTileY))
                        * (sceneRender.TileWidthPixels / 2d),
                sampleTile.CenterY
                    + ((mapTileX - sampleTile.MapTileX) - (mapTileY - sampleTile.MapTileY))
                        * (sceneRender.TileHeightPixels / 2d)
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sceneRender.ViewMode),
                sceneRender.ViewMode,
                "Unsupported scene view mode."
            ),
        };
    }

    /// <summary>
    /// Converts one normalized render-space point back into fractional map-tile space.
    /// </summary>
    public static EditorMapTilePoint UnprojectMapTile(
        EditorMapFloorRenderPreview sceneRender,
        double renderX,
        double renderY
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);
        ValidateFinite(nameof(renderX), renderX);
        ValidateFinite(nameof(renderY), renderY);

        var sampleTile = GetReferenceTile(sceneRender);
        return sceneRender.ViewMode switch
        {
            EditorMapSceneViewMode.TopDown => new EditorMapTilePoint(
                sampleTile.MapTileX + ((renderX - sampleTile.CenterX) / sceneRender.TileWidthPixels),
                sampleTile.MapTileY + ((renderY - sampleTile.CenterY) / sceneRender.TileHeightPixels)
            ),
            EditorMapSceneViewMode.Isometric => UnprojectIsometricMapTile(sceneRender, sampleTile, renderX, renderY),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sceneRender.ViewMode),
                sceneRender.ViewMode,
                "Unsupported scene view mode."
            ),
        };
    }

    /// <summary>
    /// Converts one render-space X coordinate into viewport pixel space.
    /// </summary>
    public static double RenderToViewportX(EditorMapSceneViewportLayout layout, double renderX)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(renderX), renderX);
        return (layout.ViewportWidth / 2d) + ((renderX - layout.CenterRenderX) * layout.Zoom);
    }

    /// <summary>
    /// Converts one render-space Y coordinate into viewport pixel space.
    /// </summary>
    public static double RenderToViewportY(EditorMapSceneViewportLayout layout, double renderY)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(renderY), renderY);
        return (layout.ViewportHeight / 2d) + ((renderY - layout.CenterRenderY) * layout.Zoom);
    }

    /// <summary>
    /// Converts one viewport pixel X coordinate back into normalized render space.
    /// </summary>
    public static double ViewportToRenderX(EditorMapSceneViewportLayout layout, double viewportX)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(viewportX), viewportX);
        return layout.CenterRenderX + ((viewportX - (layout.ViewportWidth / 2d)) / layout.Zoom);
    }

    /// <summary>
    /// Converts one viewport pixel Y coordinate back into normalized render space.
    /// </summary>
    public static double ViewportToRenderY(EditorMapSceneViewportLayout layout, double viewportY)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(viewportY), viewportY);
        return layout.CenterRenderY + ((viewportY - (layout.ViewportHeight / 2d)) / layout.Zoom);
    }

    /// <summary>
    /// Hit-tests one viewport point against the projected scene tiles and returns the committed tile plus stacked objects.
    /// </summary>
    public static EditorMapRenderHit? HitTestScene(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapSceneViewportLayout layout,
        double viewportX,
        double viewportY
    )
    {
        ArgumentNullException.ThrowIfNull(sceneRender);
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(viewportX), viewportX);
        ValidateFinite(nameof(viewportY), viewportY);

        if (viewportX < 0d || viewportX >= layout.ViewportWidth || viewportY < 0d || viewportY >= layout.ViewportHeight)
            return null;

        var renderX = ViewportToRenderX(layout, viewportX);
        var renderY = ViewportToRenderY(layout, viewportY);

        var hitTile = sceneRender
            .Tiles.Where(tile => ContainsRenderPoint(sceneRender, tile, renderX, renderY))
            .OrderByDescending(static tile => tile.DrawOrder)
            .FirstOrDefault();
        if (hitTile is null)
            return null;

        var objectHits = sceneRender
            .Objects.Where(obj =>
                obj.SectorAssetPath.Equals(hitTile.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
                && obj.Tile == hitTile.Tile
            )
            .OrderBy(static obj => obj.DrawOrder)
            .ToArray();

        return new EditorMapRenderHit
        {
            MapTileX = hitTile.MapTileX,
            MapTileY = hitTile.MapTileY,
            SectorAssetPath = hitTile.SectorAssetPath,
            Tile = hitTile.Tile,
            RenderPoint = new EditorMapRenderPoint(renderX, renderY),
            ObjectHits = objectHits,
        };
    }

    /// <summary>
    /// Hit-tests one viewport point and returns the equivalent persisted selection state when a projected tile was hit.
    /// </summary>
    public static EditorProjectMapSelectionState? HitTestSceneSelection(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapSceneViewportLayout layout,
        double viewportX,
        double viewportY
    )
    {
        var hit = HitTestScene(sceneRender, layout, viewportX, viewportY);
        if (hit is null)
            return null;

        return new EditorProjectMapSelectionState
        {
            SectorAssetPath = hit.SectorAssetPath,
            Tile = hit.Tile,
            ObjectId = hit.ObjectHits.FirstOrDefault()?.ObjectId,
        };
    }

    private static EditorMapFloorTileRenderItem GetReferenceTile(EditorMapFloorRenderPreview sceneRender) =>
        sceneRender.Tiles.FirstOrDefault()
        ?? throw new InvalidOperationException(
            "Rendered scene hit testing requires at least one committed floor tile."
        );

    private static EditorMapTilePoint UnprojectIsometricMapTile(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapFloorTileRenderItem sampleTile,
        double renderX,
        double renderY
    )
    {
        var normalizedX = (renderX - sampleTile.CenterX) / (sceneRender.TileWidthPixels / 2d);
        var normalizedY = (renderY - sampleTile.CenterY) / (sceneRender.TileHeightPixels / 2d);
        var deltaX = (normalizedX + normalizedY) / 2d;
        var deltaY = (normalizedX - normalizedY) / 2d;

        return new EditorMapTilePoint(sampleTile.MapTileX + deltaX, sampleTile.MapTileY + deltaY);
    }

    private static bool ContainsRenderPoint(
        EditorMapFloorRenderPreview sceneRender,
        EditorMapFloorTileRenderItem tile,
        double renderX,
        double renderY
    )
    {
        var deltaX = renderX - tile.CenterX;
        var deltaY = renderY - tile.CenterY;

        return sceneRender.ViewMode switch
        {
            EditorMapSceneViewMode.TopDown => Math.Abs(deltaX) <= (sceneRender.TileWidthPixels / 2d)
                && Math.Abs(deltaY) <= (sceneRender.TileHeightPixels / 2d),
            EditorMapSceneViewMode.Isometric => (Math.Abs(deltaX) / (sceneRender.TileWidthPixels / 2d))
                + (Math.Abs(deltaY) / (sceneRender.TileHeightPixels / 2d))
                <= 1d,
            _ => throw new ArgumentOutOfRangeException(
                nameof(sceneRender.ViewMode),
                sceneRender.ViewMode,
                "Unsupported scene view mode."
            ),
        };
    }

    private static void ValidateFinite(string paramName, double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite.");
    }

    private static void ValidatePositiveFinite(string paramName, double value)
    {
        if (!double.IsFinite(value) || value <= 0d)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite and positive.");
    }
}
