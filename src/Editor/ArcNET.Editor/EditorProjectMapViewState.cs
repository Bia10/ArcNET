namespace ArcNET.Editor;

/// <summary>
/// Typed map-view state persisted with an editor project.
/// </summary>
public sealed class EditorProjectMapViewState
{
    /// <summary>
    /// Stable host-defined identifier for this map view state.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Logical map name targeted by this view state.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Optional host-defined view identifier.
    /// </summary>
    public string? ViewId { get; init; }

    /// <summary>
    /// Typed camera state for the map view.
    /// </summary>
    public EditorProjectMapCameraState Camera { get; init; } = new();

    /// <summary>
    /// Typed selection state for the map view.
    /// </summary>
    public EditorProjectMapSelectionState Selection { get; init; } = new();

    /// <summary>
    /// Typed preview configuration for the map view.
    /// </summary>
    public EditorProjectMapPreviewState Preview { get; init; } = new();
}
