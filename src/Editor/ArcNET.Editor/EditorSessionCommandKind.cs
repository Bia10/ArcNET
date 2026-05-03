namespace ArcNET.Editor;

/// <summary>
/// One default host-facing session command action.
/// </summary>
public enum EditorSessionCommandKind
{
    /// <summary>
    /// Undo the session's current preferred staged or applied change.
    /// </summary>
    Undo = 0,

    /// <summary>
    /// Redo the session's current preferred staged or applied change.
    /// </summary>
    Redo = 1,
}
