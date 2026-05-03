namespace ArcNET.Editor;

/// <summary>
/// Persisted host-facing shell preferences for one project map view.
/// These values provide default inputs for tracked world-edit shell composition.
/// </summary>
public sealed class EditorProjectMapWorldEditShellState
{
    /// <summary>
    /// Preferred scene/view mode for the tracked world-edit shell.
    /// </summary>
    public EditorMapSceneViewMode ViewMode { get; init; } = EditorMapSceneViewMode.Isometric;

    /// <summary>
    /// Optional preferred viewport width for shell scene composition.
    /// </summary>
    public double? ViewportWidth { get; init; }

    /// <summary>
    /// Optional preferred viewport height for shell scene composition.
    /// </summary>
    public double? ViewportHeight { get; init; }

    /// <summary>
    /// Optional default object-palette search text for shell composition.
    /// </summary>
    public string? ObjectPaletteSearchText { get; init; }

    /// <summary>
    /// Optional default object-palette category filter for shell composition.
    /// </summary>
    public string? ObjectPaletteCategory { get; init; }

    /// <summary>
    /// Indicates whether tracked object-placement preview should be included by default.
    /// </summary>
    public bool IncludeTrackedPlacementPreview { get; init; } = true;
}
