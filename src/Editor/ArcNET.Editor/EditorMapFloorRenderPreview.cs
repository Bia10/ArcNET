namespace ArcNET.Editor;

/// <summary>
/// Render-ready floor-tile projection for one map scene preview.
/// </summary>
public sealed class EditorMapFloorRenderPreview
{
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
    /// Render-ready floor tiles in stable draw order.
    /// </summary>
    public required IReadOnlyList<EditorMapFloorTileRenderItem> Tiles { get; init; }

    /// <summary>
    /// Render-ready placed-object anchors in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapObjectRenderItem> Objects { get; init; }

    /// <summary>
    /// Render-ready tile overlays in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapTileOverlayRenderItem> Overlays { get; init; }

    /// <summary>
    /// Render-ready roof cells in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapRoofRenderItem> Roofs { get; init; }

    /// <summary>
    /// Unified render queue for <see cref="Tiles"/>, <see cref="Overlays"/>, <see cref="Objects"/>, and <see cref="Roofs"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapRenderQueueItem> RenderQueue { get; init; }
}
