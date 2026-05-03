namespace ArcNET.Editor;

/// <summary>
/// Request used when composing one opinionated tracked world-edit shell for a host map editor.
/// </summary>
public sealed class EditorMapWorldEditShellRequest
{
    /// <summary>
    /// Scene/view preset used for the committed render and tracked placement preview.
    /// </summary>
    public EditorMapSceneViewMode ViewMode { get; init; } = EditorMapSceneViewMode.Isometric;

    /// <summary>
    /// Optional explicit viewport width for the composed shell scene.
    /// When omitted, the shell uses the committed scene width.
    /// </summary>
    public double? ViewportWidth { get; init; }

    /// <summary>
    /// Optional explicit viewport height for the composed shell scene.
    /// When omitted, the shell uses the committed scene height.
    /// </summary>
    public double? ViewportHeight { get; init; }

    /// <summary>
    /// Optional free-text object palette search to apply before returning browser entries.
    /// </summary>
    public string? ObjectPaletteSearchText { get; init; }

    /// <summary>
    /// Optional object palette category filter to apply after search.
    /// </summary>
    public string? ObjectPaletteCategory { get; init; }

    /// <summary>
    /// Indicates whether the tracked object-placement tool should also be previewed as one live shell overlay when possible.
    /// </summary>
    public bool IncludeTrackedPlacementPreview { get; init; } = true;
}

/// <summary>
/// Opinionated host-facing world-edit shell contract for one tracked map view.
/// Bundles the current scene/view preset, tool/browser summaries, and selection state into one bindable model.
/// </summary>
public sealed class EditorMapWorldEditShell
{
    /// <summary>
    /// Stable identifier of the tracked map-view state that owns this shell.
    /// </summary>
    public required string MapViewStateId { get; init; }

    /// <summary>
    /// Effective map name targeted by the shell.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Current persisted active world-edit tool for the tracked map view.
    /// </summary>
    public required EditorProjectMapWorldEditActiveTool ActiveTool { get; init; }

    /// <summary>
    /// Effective scene/view preset used by the shell.
    /// </summary>
    public required EditorMapSceneViewMode ViewMode { get; init; }

    /// <summary>
    /// Effective committed-scene render request used by the shell.
    /// </summary>
    public required EditorMapFloorRenderRequest RenderRequest { get; init; }

    /// <summary>
    /// Bundled committed world-edit scene for the tracked map view.
    /// </summary>
    public required EditorMapWorldEditScene Scene { get; init; }

    /// <summary>
    /// Optional tracked object-placement preview overlay when the tracked tool can currently preview or apply.
    /// </summary>
    public EditorMapPlacementPreview? TrackedPlacementPreview { get; init; }

    /// <summary>
    /// Tracked terrain tool summary for the shell.
    /// </summary>
    public required EditorMapTerrainToolSummary TerrainTool { get; init; }

    /// <summary>
    /// Tracked terrain palette browser summary for the shell.
    /// </summary>
    public required EditorMapTerrainPaletteSummary TerrainPalette { get; init; }

    /// <summary>
    /// Tracked object-placement tool summary for the shell.
    /// </summary>
    public required EditorMapObjectPlacementToolSummary ObjectPlacementTool { get; init; }

    /// <summary>
    /// Tracked object palette browser summary for the shell.
    /// </summary>
    public required EditorMapObjectPaletteSummary ObjectPalette { get; init; }

    /// <summary>
    /// Tracked selected-object summary for the shell.
    /// </summary>
    public required EditorMapObjectSelectionSummary ObjectSelection { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the shell includes one live tracked placement preview overlay.
    /// </summary>
    public bool HasTrackedPlacementPreview => TrackedPlacementPreview is not null;
}
