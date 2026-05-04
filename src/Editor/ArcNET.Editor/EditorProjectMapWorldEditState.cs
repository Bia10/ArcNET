namespace ArcNET.Editor;

/// <summary>
/// Persisted world-edit workflow state for one project map view.
/// </summary>
public sealed class EditorProjectMapWorldEditState
{
    /// <summary>
    /// Currently active world-edit tool.
    /// </summary>
    public EditorProjectMapWorldEditActiveTool ActiveTool { get; init; } = EditorProjectMapWorldEditActiveTool.None;

    /// <summary>
    /// Persisted terrain-paint tool state.
    /// </summary>
    public EditorProjectMapTerrainToolState Terrain { get; init; } = new();

    /// <summary>
    /// Persisted object-placement tool state.
    /// </summary>
    public EditorProjectMapObjectPlacementToolState ObjectPlacement { get; init; } = new();

    /// <summary>
    /// Persisted host-facing shell preferences for parity-style world-edit composition.
    /// </summary>
    public EditorProjectMapWorldEditShellState Shell { get; init; } = new();

    /// <summary>
    /// Persisted object-inspector workflow state.
    /// </summary>
    public EditorProjectMapObjectInspectorState Inspector { get; init; } = new();
}
