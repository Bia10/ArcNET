namespace ArcNET.Editor;

/// <summary>
/// Host-neutral request for projecting render-ready floor tiles from one scene preview.
/// </summary>
public sealed class EditorMapFloorRenderRequest
{
    /// <summary>
    /// Requested scene view mode.
    /// </summary>
    public EditorMapSceneViewMode ViewMode { get; init; } = EditorMapSceneViewMode.Isometric;

    /// <summary>
    /// Width in pixels of one rendered floor tile.
    /// For isometric rendering this is the diamond width.
    /// </summary>
    public double TileWidthPixels { get; init; } = 64d;

    /// <summary>
    /// Height in pixels of one rendered floor tile.
    /// For isometric rendering this is the diamond height.
    /// </summary>
    public double TileHeightPixels { get; init; } = 32d;

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
    /// Returns one cloned request with visibility flags composed from one persisted map-preview state.
    /// </summary>
    public EditorMapFloorRenderRequest WithPreviewState(EditorProjectMapPreviewState previewState)
    {
        ArgumentNullException.ThrowIfNull(previewState);

        return new EditorMapFloorRenderRequest
        {
            ViewMode = ViewMode,
            TileWidthPixels = TileWidthPixels,
            TileHeightPixels = TileHeightPixels,
            IncludeEmptyTiles = IncludeEmptyTiles,
            IncludeObjects = IncludeObjects && previewState.ShowObjects,
            IncludeRoofs = IncludeRoofs && previewState.ShowRoofs,
            IncludeBlockedTileOverlays = IncludeBlockedTileOverlays && previewState.ShowBlockedTiles,
            IncludeLightOverlays = IncludeLightOverlays && previewState.ShowLights,
            IncludeScriptOverlays = IncludeScriptOverlays && previewState.ShowScripts,
        };
    }

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
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            },
            EditorMapSceneViewMode.TopDown => new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.TopDown,
                TileWidthPixels = 32d,
                TileHeightPixels = 32d,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(viewMode), viewMode, "Unsupported scene view mode."),
        };
}
