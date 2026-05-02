namespace ArcNET.Editor;

/// <summary>
/// High-level staged command action exposed by <see cref="EditorWorkspaceSession"/>.
/// </summary>
public enum EditorSessionStagedCommandKind
{
    /// <summary>
    /// Undo one staged local change through the preferred routed transaction.
    /// </summary>
    Undo = 0,

    /// <summary>
    /// Redo one staged local change through the preferred routed transaction.
    /// </summary>
    Redo = 1,
}
