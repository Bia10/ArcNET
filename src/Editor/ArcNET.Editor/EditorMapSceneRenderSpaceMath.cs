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

    /// <summary>
    /// Clockwise camera roll, in degrees, around the viewport center.
    /// </summary>
    public double RollDegrees { get; init; }

    /// <summary>
    /// Camera pitch, in degrees, applied as vertical render-space tilt.
    /// </summary>
    public double PitchDegrees { get; init; }

    /// <summary>
    /// Camera yaw, in degrees, applied as horizontal render-space tilt.
    /// </summary>
    public double YawDegrees { get; init; }
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
    public double RollDegrees { get; init; }
    public double PitchDegrees { get; init; }
    public double YawDegrees { get; init; }

    public double VisibleLeft => EditorMapSceneRenderSpaceMath.CreateVisibleRenderBounds(this).Left;
    public double VisibleTop => EditorMapSceneRenderSpaceMath.CreateVisibleRenderBounds(this).Top;
    public double VisibleRight => EditorMapSceneRenderSpaceMath.CreateVisibleRenderBounds(this).Right;
    public double VisibleBottom => EditorMapSceneRenderSpaceMath.CreateVisibleRenderBounds(this).Bottom;
}

/// <summary>
/// One point in normalized render space.
/// </summary>
public readonly record struct EditorMapRenderPoint(double X, double Y);

/// <summary>
/// One axis-aligned normalized render-space rectangle.
/// </summary>
public readonly record struct EditorMapRenderBounds(double Left, double Top, double Right, double Bottom);

/// <summary>
/// One affine transform from normalized render space to viewport pixels.
/// </summary>
public readonly record struct EditorMapSceneViewportTransform(
    double M11,
    double M12,
    double M21,
    double M22,
    double OffsetX,
    double OffsetY
);

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
    public const double MaxCameraTiltDegrees = 60d;
    private const double MinimumTiltScale = 0.35d;
    private const double TiltShearFactor = 0.25d;

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
            RollDegrees = NormalizeRollDegrees(camera.RollDegrees),
            PitchDegrees = ClampCameraTiltDegrees(camera.PitchDegrees),
            YawDegrees = ClampCameraTiltDegrees(camera.YawDegrees),
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
            RollDegrees = NormalizeRollDegrees(effectiveViewport.RollDegrees),
            PitchDegrees = ClampCameraTiltDegrees(effectiveViewport.PitchDegrees),
            YawDegrees = ClampCameraTiltDegrees(effectiveViewport.YawDegrees),
        };
    }

    /// <summary>
    /// Creates the affine transform that maps normalized render space into viewport pixel space.
    /// </summary>
    public static EditorMapSceneViewportTransform CreateViewportTransform(EditorMapSceneViewportLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidatePositiveFinite(nameof(layout.Zoom), layout.Zoom);

        var rollRadians = DegreesToRadians(NormalizeRollDegrees(layout.RollDegrees));
        var pitchRadians = DegreesToRadians(ClampCameraTiltDegrees(layout.PitchDegrees));
        var yawRadians = DegreesToRadians(ClampCameraTiltDegrees(layout.YawDegrees));
        var scaleX = Math.Max(MinimumTiltScale, Math.Cos(yawRadians));
        var scaleY = Math.Max(MinimumTiltScale, Math.Cos(pitchRadians));
        var shearX = Math.Sin(yawRadians) * TiltShearFactor;
        var shearY = -Math.Sin(pitchRadians) * TiltShearFactor;
        var cos = Math.Cos(rollRadians);
        var sin = Math.Sin(rollRadians);

        var m11 = layout.Zoom * ((cos * scaleX) - (sin * shearY));
        var m12 = layout.Zoom * ((sin * scaleX) + (cos * shearY));
        var m21 = layout.Zoom * ((cos * shearX) - (sin * scaleY));
        var m22 = layout.Zoom * ((sin * shearX) + (cos * scaleY));
        var offsetX = (layout.ViewportWidth / 2d) - (m11 * layout.CenterRenderX) - (m21 * layout.CenterRenderY);
        var offsetY = (layout.ViewportHeight / 2d) - (m12 * layout.CenterRenderX) - (m22 * layout.CenterRenderY);
        return new EditorMapSceneViewportTransform(m11, m12, m21, m22, offsetX, offsetY);
    }

    /// <summary>
    /// Creates the axis-aligned normalized render-space bounds currently visible in the viewport.
    /// </summary>
    public static EditorMapRenderBounds CreateVisibleRenderBounds(EditorMapSceneViewportLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var topLeft = ViewportToRenderPoint(layout, 0d, 0d);
        var topRight = ViewportToRenderPoint(layout, layout.ViewportWidth, 0d);
        var bottomLeft = ViewportToRenderPoint(layout, 0d, layout.ViewportHeight);
        var bottomRight = ViewportToRenderPoint(layout, layout.ViewportWidth, layout.ViewportHeight);
        return new EditorMapRenderBounds(
            Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)),
            Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y)),
            Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)),
            Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y))
        );
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
                sampleTile.CenterX - ((mapTileX - sampleTile.MapTileX) * sceneRender.TileWidthPixels),
                sampleTile.CenterY + ((mapTileY - sampleTile.MapTileY) * sceneRender.TileHeightPixels)
            ),
            EditorMapSceneViewMode.Isometric => new EditorMapRenderPoint(
                sampleTile.CenterX
                    + ((mapTileY - sampleTile.MapTileY) - (mapTileX - sampleTile.MapTileX))
                        * (sceneRender.TileWidthPixels / 2d),
                sampleTile.CenterY
                    + ((mapTileX - sampleTile.MapTileX) + (mapTileY - sampleTile.MapTileY))
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
                sampleTile.MapTileX - ((renderX - sampleTile.CenterX) / sceneRender.TileWidthPixels),
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
        return RenderToViewportPoint(layout, renderX, layout.CenterRenderY).X;
    }

    /// <summary>
    /// Converts one render-space Y coordinate into viewport pixel space.
    /// </summary>
    public static double RenderToViewportY(EditorMapSceneViewportLayout layout, double renderY)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(renderY), renderY);
        return RenderToViewportPoint(layout, layout.CenterRenderX, renderY).Y;
    }

    /// <summary>
    /// Converts one normalized render-space point into viewport pixel space.
    /// </summary>
    public static EditorMapRenderPoint RenderToViewportPoint(
        EditorMapSceneViewportLayout layout,
        double renderX,
        double renderY
    )
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(renderX), renderX);
        ValidateFinite(nameof(renderY), renderY);

        var transform = CreateViewportTransform(layout);
        return new EditorMapRenderPoint(
            (renderX * transform.M11) + (renderY * transform.M21) + transform.OffsetX,
            (renderX * transform.M12) + (renderY * transform.M22) + transform.OffsetY
        );
    }

    /// <summary>
    /// Converts one viewport pixel X coordinate back into normalized render space.
    /// </summary>
    public static double ViewportToRenderX(EditorMapSceneViewportLayout layout, double viewportX)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(viewportX), viewportX);
        return ViewportToRenderPoint(layout, viewportX, layout.ViewportHeight / 2d).X;
    }

    /// <summary>
    /// Converts one viewport pixel Y coordinate back into normalized render space.
    /// </summary>
    public static double ViewportToRenderY(EditorMapSceneViewportLayout layout, double viewportY)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(viewportY), viewportY);
        return ViewportToRenderPoint(layout, layout.ViewportWidth / 2d, viewportY).Y;
    }

    /// <summary>
    /// Converts one viewport pixel point back into normalized render space.
    /// </summary>
    public static EditorMapRenderPoint ViewportToRenderPoint(
        EditorMapSceneViewportLayout layout,
        double viewportX,
        double viewportY
    )
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinite(nameof(viewportX), viewportX);
        ValidateFinite(nameof(viewportY), viewportY);

        var transform = CreateViewportTransform(layout);
        var deltaX = viewportX - transform.OffsetX;
        var deltaY = viewportY - transform.OffsetY;
        var determinant = (transform.M11 * transform.M22) - (transform.M21 * transform.M12);
        if (Math.Abs(determinant) < 0.000001d)
            throw new InvalidOperationException("The map scene viewport transform is not invertible.");

        return new EditorMapRenderPoint(
            ((deltaX * transform.M22) - (deltaY * transform.M21)) / determinant,
            ((deltaY * transform.M11) - (deltaX * transform.M12)) / determinant
        );
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

        var renderPoint = ViewportToRenderPoint(layout, viewportX, viewportY);

        var tilePoint = UnprojectMapTile(sceneRender, renderPoint.X, renderPoint.Y);
        var mapTileX = (int)Math.Round(tilePoint.MapTileX, MidpointRounding.AwayFromZero);
        var mapTileY = (int)Math.Round(tilePoint.MapTileY, MidpointRounding.AwayFromZero);

        if (
            !sceneRender
                .GetOrCreateSpatialIndex()
                .TryHitTest(renderPoint.X, renderPoint.Y, mapTileX, mapTileY, out var hitTile, out var objectHits)
            || hitTile is null
        )
        {
            return null;
        }

        return new EditorMapRenderHit
        {
            MapTileX = hitTile.MapTileX,
            MapTileY = hitTile.MapTileY,
            SectorAssetPath = hitTile.SectorAssetPath,
            Tile = hitTile.Tile,
            RenderPoint = renderPoint,
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

        var selectedObject = hit.ObjectHits.LastOrDefault();
        return new EditorProjectMapSelectionState
        {
            SectorAssetPath = hit.SectorAssetPath,
            Tile = hit.Tile,
            ObjectId = selectedObject?.ObjectId,
            SourceAssetPath = selectedObject?.SectorAssetPath,
            SourceObjectIndex = selectedObject?.SourceObjectIndex,
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
        var deltaX = (normalizedY - normalizedX) / 2d;
        var deltaY = (normalizedY + normalizedX) / 2d;

        return new EditorMapTilePoint(sampleTile.MapTileX + deltaX, sampleTile.MapTileY + deltaY);
    }

    internal static bool ContainsRenderPoint(
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

    internal static bool ContainsRenderPoint(EditorMapObjectRenderItem obj, double renderX, double renderY)
    {
        var spriteBounds = obj.SpriteBounds;
        if (spriteBounds is null)
        {
            var left = obj.AnchorX - 8d;
            var top = obj.AnchorY - 8d;
            return renderX >= left && renderX <= left + 16d && renderY >= top && renderY <= top + 16d;
        }

        var (centerX, centerY) = EditorMapFloorRenderBuilder.GetLayoutSpriteCenter(
            obj.ObjectType,
            obj.CurrentArtId,
            spriteBounds
        );
        var spriteLeft = obj.AnchorX - centerX;
        var spriteTop = obj.AnchorY - centerY;
        return renderX >= spriteLeft
            && renderX <= spriteLeft + spriteBounds.MaxFrameWidth
            && renderY >= spriteTop
            && renderY <= spriteTop + spriteBounds.MaxFrameHeight;
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

    public static double NormalizeRollDegrees(double value)
    {
        ValidateFinite(nameof(value), value);

        var normalized = value % 360d;
        if (normalized > 180d)
            normalized -= 360d;
        else if (normalized <= -180d)
            normalized += 360d;

        return normalized;
    }

    public static double ClampCameraTiltDegrees(double value)
    {
        ValidateFinite(nameof(value), value);
        return Math.Clamp(value, -MaxCameraTiltDegrees, MaxCameraTiltDegrees);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
