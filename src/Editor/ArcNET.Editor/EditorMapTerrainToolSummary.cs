namespace ArcNET.Editor;

/// <summary>
/// Host-facing snapshot of the tracked terrain-paint tool for one map view.
/// </summary>
public sealed class EditorMapTerrainToolSummary
{
    /// <summary>
    /// Stable identifier of the tracked map-view state that owns this tool snapshot.
    /// </summary>
    public required string MapViewStateId { get; init; }

    /// <summary>
    /// Map name targeted by the owning map view.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Normalized persisted terrain-tool state.
    /// </summary>
    public required EditorProjectMapTerrainToolState ToolState { get; init; }

    /// <summary>
    /// Resolved terrain palette entry referenced by <see cref="ToolState"/>, or <see langword="null"/> when unresolved.
    /// </summary>
    public EditorTerrainPaletteEntry? SelectedEntry { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when the tracked terrain tool currently resolves to one loadable palette entry.
    /// </summary>
    public bool CanApply => SelectedEntry is not null;
}
