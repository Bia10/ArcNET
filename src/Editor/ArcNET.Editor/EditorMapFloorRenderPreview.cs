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
    /// Render-ready auxiliary object layers in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapObjectAuxiliaryRenderItem> ObjectAuxiliaryItems { get; init; } = [];

    /// <summary>
    /// Render-ready tile overlays in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapTileOverlayRenderItem> Overlays { get; init; }

    /// <summary>
    /// Render-ready CE light-system masks in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public IReadOnlyList<EditorMapLightRenderItem> Lights { get; init; } = [];

    /// <summary>
    /// Render-ready roof cells in the same normalized render space as <see cref="Tiles"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapRoofRenderItem> Roofs { get; init; }

    /// <summary>
    /// Unified render queue for <see cref="Tiles"/>, <see cref="Overlays"/>, <see cref="Objects"/>, and <see cref="Roofs"/>.
    /// </summary>
    public required IReadOnlyList<EditorMapRenderQueueItem> RenderQueue { get; init; }

    /// <summary>
    /// Indicates whether committed object renders should expose editor-state tint diagnostics.
    /// </summary>
    public bool IncludeEditorObjectStateTint { get; init; }

    /// <summary>
    /// Indicates whether committed floor tiles should expose floor-light tint diagnostics.
    /// </summary>
    public bool IncludeFloorLightTint { get; init; }

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
}
