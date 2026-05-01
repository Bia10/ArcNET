namespace ArcNET.Editor;

/// <summary>
/// High-level staged history scope exposed by <see cref="EditorWorkspaceSession"/>.
/// </summary>
public enum EditorSessionStagedHistoryScopeKind
{
    /// <summary>
    /// One tracked dialog editor.
    /// </summary>
    Dialog,

    /// <summary>
    /// One tracked script editor.
    /// </summary>
    Script,

    /// <summary>
    /// The session's tracked save editor.
    /// </summary>
    Save,

    /// <summary>
    /// The session's direct proto, mob, and sector staged asset surface.
    /// </summary>
    DirectAssets,
}
