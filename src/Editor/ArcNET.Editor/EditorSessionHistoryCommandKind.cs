namespace ArcNET.Editor;

/// <summary>
/// High-level applied-history command exposed by <see cref="EditorWorkspaceSession"/>.
/// </summary>
public enum EditorSessionHistoryCommandKind
{
    /// <summary>
    /// Undo the most recent applied session change group.
    /// </summary>
    Undo = 0,

    /// <summary>
    /// Redo the most recent undone session change group.
    /// </summary>
    Redo = 1,
}
