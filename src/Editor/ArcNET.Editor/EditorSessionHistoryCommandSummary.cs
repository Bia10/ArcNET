namespace ArcNET.Editor;

/// <summary>
/// One host-facing applied-history command routed through the current top undo/redo history entry.
/// This gives hosts a command-style inspection surface for applied history without requiring them to
/// manually inspect the undo/redo stacks before binding standard actions.
/// </summary>
public sealed class EditorSessionHistoryCommandSummary
{
    /// <summary>
    /// Command action represented by this entry.
    /// </summary>
    public required EditorSessionHistoryCommandKind Kind { get; init; }

    /// <summary>
    /// Stable host-facing label for this command.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Applied-history entry that would be undone or redone when this command executes.
    /// </summary>
    public required EditorSessionHistoryEntry Entry { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this command can currently execute.
    /// </summary>
    public required bool CanExecute { get; init; }
}
