namespace ArcNET.Editor;

/// <summary>
/// Host-neutral request for projecting render-ready floor tiles from one scene preview.
/// </summary>
public sealed class EditorMapFloorRenderRequest
{
    /// <summary>
    /// Optional ART resolver used when light-pass sampling needs access to CE light mask pixels.
    /// </summary>
    public EditorArtResolver? ArtResolver { get; init; }

    /// <summary>
    /// Requested scene view mode.
    /// </summary>
    public EditorMapSceneViewMode ViewMode { get; init; } = EditorMapSceneViewMode.Isometric;

    /// <summary>
    /// Width in pixels of one rendered floor tile.
    /// For isometric rendering this is the diamond width.
    /// </summary>
    public double TileWidthPixels { get; init; } = 80d;

    /// <summary>
    /// Height in pixels of one rendered floor tile.
    /// For isometric rendering this is the diamond height.
    /// </summary>
    public double TileHeightPixels { get; init; } = 40d;

    /// <summary>
    /// Indicates whether tiles whose art identifier is zero should still be emitted.
    /// </summary>
    public bool IncludeEmptyTiles { get; init; }

    /// <summary>
    /// Indicates whether placed objects should be projected into the same normalized render space.
    /// </summary>
    public bool IncludeObjects { get; init; } = true;

    /// <summary>
    /// Indicates whether roof cells should be projected into the same normalized render space.
    /// </summary>
    public bool IncludeRoofs { get; init; } = true;

    /// <summary>
    /// Indicates whether blocked-tile overlays should be emitted.
    /// </summary>
    public bool IncludeBlockedTileOverlays { get; init; } = true;

    /// <summary>
    /// Indicates whether light overlays should be emitted.
    /// </summary>
    public bool IncludeLightOverlays { get; init; } = true;

    /// <summary>
    /// Indicates whether tile-script overlays should be emitted.
    /// </summary>
    public bool IncludeScriptOverlays { get; init; } = true;

    /// <summary>
    /// Indicates whether committed object renders should expose editor-state tint diagnostics.
    /// </summary>
    public bool IncludeEditorObjectStateTint { get; init; }

    /// <summary>
    /// Indicates whether committed floor tiles should expose floor-light tint diagnostics.
    /// </summary>
    public bool IncludeFloorLightTint { get; init; }

    /// <summary>
    /// Optional CE ambient-lighting context used when resolving day/night and light-scheme tinting.
    /// </summary>
    public EditorMapAmbientLightingState? AmbientLighting { get; init; }

    /// <summary>
    /// Returns one cloned request with visibility flags composed from one persisted map-preview state.
    /// </summary>
    public EditorMapFloorRenderRequest WithPreviewState(EditorProjectMapPreviewState previewState)
    {
        ArgumentNullException.ThrowIfNull(previewState);

        return new EditorMapFloorRenderRequest
        {
            ArtResolver = ArtResolver,
            ViewMode = ViewMode,
            TileWidthPixels = TileWidthPixels,
            TileHeightPixels = TileHeightPixels,
            IncludeEmptyTiles = IncludeEmptyTiles,
            IncludeObjects = IncludeObjects && previewState.ShowObjects,
            IncludeRoofs = IncludeRoofs && previewState.ShowRoofs,
            IncludeBlockedTileOverlays = IncludeBlockedTileOverlays && previewState.ShowBlockedTiles,
            IncludeLightOverlays = IncludeLightOverlays && previewState.ShowLights,
            IncludeScriptOverlays = IncludeScriptOverlays && previewState.ShowScripts,
            IncludeEditorObjectStateTint = IncludeEditorObjectStateTint,
            IncludeFloorLightTint = IncludeFloorLightTint,
            AmbientLighting = AmbientLighting,
        };
    }

    /// <summary>
    /// Returns one cloned request with a different ART resolver.
    /// </summary>
    public EditorMapFloorRenderRequest WithArtResolver(EditorArtResolver? artResolver) =>
        new()
        {
            ArtResolver = artResolver,
            ViewMode = ViewMode,
            TileWidthPixels = TileWidthPixels,
            TileHeightPixels = TileHeightPixels,
            IncludeEmptyTiles = IncludeEmptyTiles,
            IncludeObjects = IncludeObjects,
            IncludeRoofs = IncludeRoofs,
            IncludeBlockedTileOverlays = IncludeBlockedTileOverlays,
            IncludeLightOverlays = IncludeLightOverlays,
            IncludeScriptOverlays = IncludeScriptOverlays,
            IncludeEditorObjectStateTint = IncludeEditorObjectStateTint,
            IncludeFloorLightTint = IncludeFloorLightTint,
            AmbientLighting = AmbientLighting,
        };

    /// <summary>
    /// Returns one cloned request with a different ambient-lighting context.
    /// </summary>
    public EditorMapFloorRenderRequest WithAmbientLighting(EditorMapAmbientLightingState? ambientLighting) =>
        new()
        {
            ArtResolver = ArtResolver,
            ViewMode = ViewMode,
            TileWidthPixels = TileWidthPixels,
            TileHeightPixels = TileHeightPixels,
            IncludeEmptyTiles = IncludeEmptyTiles,
            IncludeObjects = IncludeObjects,
            IncludeRoofs = IncludeRoofs,
            IncludeBlockedTileOverlays = IncludeBlockedTileOverlays,
            IncludeLightOverlays = IncludeLightOverlays,
            IncludeScriptOverlays = IncludeScriptOverlays,
            IncludeEditorObjectStateTint = IncludeEditorObjectStateTint,
            IncludeFloorLightTint = IncludeFloorLightTint,
            AmbientLighting = ambientLighting,
        };

    /// <summary>
    /// Creates one opinionated world-edit scene render request for the supplied view mode.
    /// </summary>
    public static EditorMapFloorRenderRequest CreateWorldEditPreset(
        EditorMapSceneViewMode viewMode = EditorMapSceneViewMode.Isometric
    ) =>
        viewMode switch
        {
            EditorMapSceneViewMode.Isometric => new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 80d,
                TileHeightPixels = 40d,
                IncludeEmptyTiles = true,
            },
            EditorMapSceneViewMode.TopDown => new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.TopDown,
                TileWidthPixels = 32d,
                TileHeightPixels = 32d,
                IncludeEmptyTiles = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
}
