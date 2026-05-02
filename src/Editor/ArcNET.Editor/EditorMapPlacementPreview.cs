namespace ArcNET.Editor;

/// <summary>
/// Live snapped placement preview projected into the same normalized render space as scene floor previews.
/// </summary>
public sealed class EditorMapPlacementPreview
{
    /// <summary>
    /// Map name that owns the preview.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// View mode used to project the preview.
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
    /// Projected live placement ghost objects.
    /// </summary>
    public required IReadOnlyList<EditorMapPlacementPreviewObject> Objects { get; init; }

    /// <summary>
    /// Unified render queue for the committed scene plus the placement preview objects.
    /// </summary>
    public required IReadOnlyList<EditorMapRenderQueueItem> RenderQueue { get; init; }
}
