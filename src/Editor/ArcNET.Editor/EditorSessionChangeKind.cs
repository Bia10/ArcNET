namespace ArcNET.Editor;

/// <summary>
/// High-level change categories surfaced by <see cref="EditorWorkspaceSession"/>.
/// </summary>
public enum EditorSessionChangeKind
{
    /// <summary>
    /// Pending dialog-file edits.
    /// </summary>
    Dialog = 0,

    /// <summary>
    /// Pending compiled-script edits.
    /// </summary>
    Script = 1,

    /// <summary>
    /// Pending prototype-file edits.
    /// </summary>
    Proto = 2,

    /// <summary>
    /// Pending mobile-object file edits.
    /// </summary>
    Mob = 3,

    /// <summary>
    /// Pending sector-file edits.
    /// </summary>
    Sector = 4,

    /// <summary>
    /// Pending save-slot edits.
    /// </summary>
    Save = 5,
}
