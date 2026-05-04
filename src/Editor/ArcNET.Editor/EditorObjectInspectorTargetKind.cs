namespace ArcNET.Editor;

/// <summary>
/// Identifies the current inspectable target resolved by the object/proto inspector summary.
/// </summary>
public enum EditorObjectInspectorTargetKind
{
    /// <summary>
    /// No single inspectable object or shared proto target could be resolved.
    /// </summary>
    None = 0,

    /// <summary>
    /// The inspector targets one selected placed object from the tracked map selection.
    /// </summary>
    SelectedObject = 1,

    /// <summary>
    /// The inspector targets one proto definition, either directly or as the shared proto behind the current selection.
    /// </summary>
    ProtoDefinition = 2,
}
