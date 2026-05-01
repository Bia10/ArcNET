namespace ArcNET.Editor;

/// <summary>
/// One host-facing staged-history scope that can be inspected and driven through <see cref="EditorWorkspaceSession"/>.
/// </summary>
public sealed class EditorSessionStagedHistoryScope
{
    /// <summary>
    /// High-level scope kind represented by this entry.
    /// </summary>
    public required EditorSessionStagedHistoryScopeKind Kind { get; init; }

    /// <summary>
    /// Scope target identifier.
    /// For dialog and script scopes this is the normalized asset path.
    /// For save scopes this is the current save-slot target.
    /// For direct-asset scopes this is <see langword="null"/>.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this scope currently has staged changes.
    /// </summary>
    public required bool HasPendingChanges { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this scope can undo one staged change.
    /// </summary>
    public required bool CanUndo { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this scope can redo one previously undone staged change.
    /// </summary>
    public required bool CanRedo { get; init; }
}
