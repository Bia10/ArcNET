namespace ArcNET.Editor;

/// <summary>
/// Persisted target mode for one tracked map-view object inspector.
/// </summary>
public enum EditorProjectMapObjectInspectorTargetMode
{
    /// <summary>
    /// Resolve the inspector target from the tracked map-view selection.
    /// </summary>
    Selection = 0,

    /// <summary>
    /// Inspect one persisted proto definition regardless of the tracked selection.
    /// </summary>
    ProtoDefinition = 1,
}
