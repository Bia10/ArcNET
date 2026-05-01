namespace ArcNET.Editor;

/// <summary>
/// Summary of one pending change currently tracked by an <see cref="EditorWorkspaceSession"/>.
/// </summary>
public sealed class EditorSessionChange
{
    /// <summary>
    /// High-level kind of pending change.
    /// </summary>
    public required EditorSessionChangeKind Kind { get; init; }

    /// <summary>
    /// Changed asset path, or the save-slot name for save edits when available.
    /// </summary>
    public required string Target { get; init; }
}
