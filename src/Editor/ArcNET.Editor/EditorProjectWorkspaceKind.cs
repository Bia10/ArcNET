namespace ArcNET.Editor;

/// <summary>
/// Identifies how an editor project should reopen its workspace content.
/// </summary>
public enum EditorProjectWorkspaceKind
{
    /// <summary>
    /// Reopen the workspace from a loose or extracted content directory.
    /// </summary>
    ContentDirectory = 0,

    /// <summary>
    /// Reopen the workspace from a native Arcanum installation root.
    /// </summary>
    GameInstall = 1,
}
