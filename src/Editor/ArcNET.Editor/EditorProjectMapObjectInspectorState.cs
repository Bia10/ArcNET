namespace ArcNET.Editor;

/// <summary>
/// Persisted object-inspector workflow state for one project map view.
/// </summary>
public sealed class EditorProjectMapObjectInspectorState
{
    /// <summary>
    /// Current inspector target mode.
    /// </summary>
    public EditorProjectMapObjectInspectorTargetMode TargetMode { get; init; } =
        EditorProjectMapObjectInspectorTargetMode.Selection;

    /// <summary>
    /// Optional pinned proto number used when <see cref="TargetMode"/> is
    /// <see cref="EditorProjectMapObjectInspectorTargetMode.ProtoDefinition"/>.
    /// </summary>
    public int? PinnedProtoNumber { get; init; }

    /// <summary>
    /// Currently active inspector pane.
    /// </summary>
    public EditorObjectInspectorPane ActivePane { get; init; } = EditorObjectInspectorPane.Overview;
}
