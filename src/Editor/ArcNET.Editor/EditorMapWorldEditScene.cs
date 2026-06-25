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
    /// Optional override for blocked, script, and jump overlay preview visibility.
    /// When omitted, the owning map view preview state controls each overlay independently.
    /// </summary>
    public bool? IncludeSpecialTileOverlays { get; init; }

    /// <summary>
    /// Optional radius, in sectors around the current camera, whose terrain should be materialized.
    /// Objects remain projected for every sector so editor object catalogs stay complete.
    /// </summary>
    public int? FocusedTerrainSectorRadius { get; init; }

    /// <summary>
    /// Optional radius, in sectors around the current camera, whose objects should be projected.
    /// Use <see langword="null"/> to project objects for every sector.
    /// </summary>
    public int? FocusedObjectSectorRadius { get; init; }

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

    /// <summary>
    /// Indicates whether committed scene sprites should be preloaded before the scene is returned.
    /// Hosts that render through retained/on-demand chunk caches can disable this to reduce first-shell latency.
    /// </summary>
    public bool PreloadSceneSprites { get; init; } = true;

    /// <summary>
    /// Indicates whether paintable scene sprite coverage should be resolved while composing this scene.
    /// Hosts that defer sprite warm-up to retained/on-demand render caches can disable this for the first shell.
    /// </summary>
    public bool IncludeSpriteCoverage { get; init; } = true;

    /// <summary>
    /// Indicates whether partial terrain sprite coverage should include non-materialized virtual terrain sectors.
    /// Hosts that render virtual terrain through retained chunk sources can disable this to reduce first-shell latency.
    /// </summary>
    public bool IncludeVirtualTerrainSpriteCoverage { get; init; } = true;

    /// <summary>
    /// Optional existing render preview to use for delta building.
    /// </summary>
    public EditorMapFloorRenderPreview? ExistingPreview { get; init; }

    /// <summary>
    /// Optional path to the sector asset that changed, triggering the delta build.
    /// </summary>
    public string? ChangedSectorAssetPath { get; init; }

    /// <summary>
    /// Optional existing sprite coverage to reuse instead of re-scanning the entire scene render queue.
    /// </summary>
    public EditorMapRenderSpriteCoverage? ExistingSpriteCoverage { get; init; }
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
