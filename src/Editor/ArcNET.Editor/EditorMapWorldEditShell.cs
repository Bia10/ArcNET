using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Request used when composing one opinionated tracked world-edit shell for a host map editor.
/// </summary>
public sealed class EditorMapWorldEditShellRequest
{
    /// <summary>
    /// Optional ART resolver used to auto-create one workspace-backed sprite source when <see cref="SpriteSource"/> is not supplied.
    /// </summary>
    public EditorArtResolver? ArtResolver { get; init; }

    /// <summary>
    /// Optional ART-backed sprite source used to enrich shell paintable scene items with packed frames.
    /// </summary>
    public IEditorMapRenderSpriteSource? SpriteSource { get; init; }

    /// <summary>
    /// Indicates whether committed scene sprites should be preloaded before the shell is returned.
    /// Hosts that render visible chunks on demand can disable this to reduce first-shell latency.
    /// </summary>
    public bool PreloadSceneSprites { get; init; } = true;

    /// <summary>
    /// Scene/view preset used for the committed render and tracked placement preview.
    /// </summary>
    public EditorMapSceneViewMode ViewMode { get; init; } = EditorMapSceneViewMode.Isometric;

    /// <summary>
    /// Optional radius, in sectors around the current camera, whose terrain should be materialized for this shell.
    /// Use <see langword="null"/> for the complete terrain payload.
    /// </summary>
    public int? FocusedTerrainSectorRadius { get; init; }

    /// <summary>
    /// Optional radius, in sectors around the current camera, whose objects should be projected for this shell.
    /// Use <see langword="null"/> for the complete object payload.
    /// </summary>
    public int? FocusedObjectSectorRadius { get; init; }

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
    /// Indicates whether the object palette should be fully browsed when no search text is supplied.
    /// Hosts should keep this false for automatic refreshes and set it only for an explicit browse action.
    /// </summary>
    public bool IncludeFullObjectPaletteBrowse { get; init; }

    /// <summary>
    /// Indicates whether the tracked object-placement tool should also be previewed as one live shell overlay when possible.
    /// </summary>
    public bool IncludeTrackedPlacementPreview { get; init; } = true;

    /// <summary>
    /// Indicates whether committed object renders should expose editor-state tint diagnostics.
    /// </summary>
    public bool IncludeEditorObjectStateTint { get; init; }

    /// <summary>
    /// Indicates whether committed floor tiles should expose floor-light tint diagnostics.
    /// </summary>
    public bool IncludeFloorLightTint { get; init; }

    /// <summary>
    /// Optional explicit ambient-lighting context used when composing the shell render.
    /// When omitted, the session resolves the current CE light scheme and hour from workspace data.
    /// </summary>
    public EditorMapAmbientLightingState? AmbientLighting { get; init; }
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
    /// Optional radius, in sectors around the current camera, whose terrain was materialized for this shell.
    /// </summary>
    public int? FocusedTerrainSectorRadius { get; init; }

    /// <summary>
    /// Optional radius, in sectors around the current camera, whose objects were projected for this shell.
    /// </summary>
    public int? FocusedObjectSectorRadius { get; init; }

    /// <summary>
    /// Optional tracked object-placement preview overlay when the tracked tool can currently preview or apply.
    /// </summary>
    public EditorMapPlacementPreview? TrackedPlacementPreview { get; init; }

    /// <summary>
    /// Optional host-ready paintable scene derived from <see cref="TrackedPlacementPreview"/>.
    /// Hosts can render this directly instead of rebuilding the preview queue during paint.
    /// </summary>
    public EditorMapPaintableScene? TrackedPlacementPaintableScene { get; init; }

    /// <summary>
    /// Optional host-ready terrain-facade overlay paintable scene for the tracked terrain tool.
    /// </summary>
    public EditorMapPaintableScene? TrackedTerrainFacadePaintableScene { get; init; }

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
    /// Persisted inspector workflow state for the tracked map view.
    /// </summary>
    public required EditorProjectMapObjectInspectorState ObjectInspectorState { get; init; }

    /// <summary>
    /// Tracked object/proto inspector summary for the shell.
    /// </summary>
    public required EditorObjectInspectorSummary ObjectInspector { get; init; }

    /// <summary>
    /// Typed flags-pane contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorFlagsSummary ObjectInspectorFlags { get; init; }

    /// <summary>
    /// Typed script-attachments contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorScriptAttachmentsSummary ObjectInspectorScriptAttachments { get; init; }

    /// <summary>
    /// Typed critter-progression contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorCritterProgressionSummary ObjectInspectorCritterProgression { get; init; }

    /// <summary>
    /// Typed light contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorLightSummary ObjectInspectorLight { get; init; }

    /// <summary>
    /// Typed generator contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorGeneratorSummary ObjectInspectorGenerator { get; init; }

    /// <summary>
    /// Typed blending contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorBlendingSummary ObjectInspectorBlending { get; init; }

    /// <summary>
    /// Typed container contract for the shell's current inspector target.
    /// </summary>
    public required EditorObjectInspectorContainerSummary ObjectInspectorContainer { get; init; }

    /// <summary>
    /// Optional loaded jump points (portals) for the current map.
    /// </summary>
    public JmpFile? JumpPoints { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the shell includes one live tracked placement preview overlay.
    /// </summary>
    public bool HasTrackedPlacementPreview => TrackedPlacementPreview is not null;
}
