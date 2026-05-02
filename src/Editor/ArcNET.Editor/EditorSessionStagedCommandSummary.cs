namespace ArcNET.Editor;

/// <summary>
/// One host-facing staged command routed through the current preferred transaction selection logic.
/// This gives hosts a simple default undo/redo binding surface without requiring them to inspect
/// editor-specific types or manually reproduce preferred transaction routing.
/// </summary>
public sealed class EditorSessionStagedCommandSummary
{
    /// <summary>
    /// Command action represented by this entry.
    /// </summary>
    public required EditorSessionStagedCommandKind Kind { get; init; }

    /// <summary>
    /// Stable host-facing label for this command.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Transaction that would be routed when this command executes.
    /// </summary>
    public required EditorSessionStagedTransactionSummary Transaction { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this command can currently execute.
    /// </summary>
    public required bool CanExecute { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this command is the session's current preferred default
    /// action for its command kind.
    /// </summary>
    public required bool IsDefault { get; init; }
}
