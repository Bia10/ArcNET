namespace ArcNET.Editor;

/// <summary>
/// Request used when composing one bundled host-facing world-edit scene.
/// </summary>
public sealed class EditorMapWorldEditSceneRequest
{
    /// <summary>
    /// Optional committed-scene render request override.
    /// Preview visibility flags from the owning map view are still applied on top.
    /// </summary>
    public EditorMapFloorRenderRequest? RenderRequest { get; init; }

    /// <summary>
    /// Optional single palette placement request to project as a live ghost preview.
    /// </summary>
    public EditorObjectPalettePlacementRequest? PlacementRequest { get; init; }

    /// <summary>
    /// Optional explicit viewport width used when building one render-space layout.
    /// Defaults to the committed scene width.
    /// </summary>
    public double? ViewportWidth { get; init; }

    /// <summary>
    /// Optional explicit viewport height used when building one render-space layout.
    /// Defaults to the committed scene height.
    /// </summary>
    public double? ViewportHeight { get; init; }

    /// <summary>
    /// Optional render-space viewport state override.
    /// When omitted, the owning map-view tile camera is projected into render space.
    /// </summary>
    public EditorMapRenderViewportState? Viewport { get; init; }

    /// <summary>
    /// Optional ART resolver used to auto-create one workspace-backed sprite source when <see cref="SpriteSource"/> is not supplied.
    /// </summary>
    public EditorArtResolver? ArtResolver { get; init; }

    /// <summary>
    /// Optional ART-backed sprite source used to enrich paintable scene items with packed frames.
    /// </summary>
    public IEditorMapRenderSpriteSource? SpriteSource { get; init; }
}

/// <summary>
/// Bundled host-facing world-edit scene for one map view.
/// </summary>
public sealed class EditorMapWorldEditScene
{
    /// <summary>
    /// Effective map-view state that owns the composed scene.
    /// </summary>
    public required EditorProjectMapViewState MapViewState { get; init; }

    /// <summary>
    /// Bundled committed scene render projected from the effective workspace state.
    /// </summary>
    public required EditorMapFloorRenderPreview SceneRender { get; init; }

    /// <summary>
    /// Optional live placement-preview ghost projected on top of the committed scene.
    /// </summary>
    public EditorMapPlacementPreview? PlacementPreview { get; init; }

    /// <summary>
    /// Render-space viewport layout derived from the owning map-view camera or one explicit override.
    /// </summary>
    public required EditorMapSceneViewportLayout ViewportLayout { get; init; }

    /// <summary>
    /// Host-ready paintable scene items derived from the committed queue and any optional placement-preview queue.
    /// </summary>
    public required EditorMapPaintableScene PaintableScene { get; init; }

    /// <summary>
    /// Sprite-resolution coverage for the paintable scene's referenced ART identifiers.
    /// </summary>
    public required EditorMapRenderSpriteCoverage SpriteCoverage { get; init; }

    /// <summary>
    /// Map name targeted by <see cref="MapViewState"/>.
    /// </summary>
    public string MapName => MapViewState.MapName;
}
