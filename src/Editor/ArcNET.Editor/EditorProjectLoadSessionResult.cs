namespace ArcNET.Editor;

/// <summary>
/// Result of reopening an <see cref="EditorProject"/> into a live session together with the
/// normalized restore summary reported by <see cref="EditorWorkspaceSession.RestoreProject(EditorProject)"/>.
/// </summary>
public sealed class EditorProjectLoadSessionResult
{
    /// <summary>
    /// Live session loaded from the project's workspace reference.
    /// </summary>
    public required EditorWorkspaceSession Session { get; init; }

    /// <summary>
    /// Restore summary returned after applying the project's persisted session metadata.
    /// </summary>
    public required EditorProjectRestoreResult Restore { get; init; }

    /// <summary>
    /// Unified host-facing bootstrap snapshot built from the live session and restore result.
    /// </summary>
    public EditorSessionBootstrapSummary BootstrapSummary => Session.GetBootstrapSummary(Restore);
}
