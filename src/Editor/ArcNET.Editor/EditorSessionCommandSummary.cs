namespace ArcNET.Editor;

/// <summary>
/// One default host-facing session command that deterministically routes through staged local edits first
/// and then falls back to applied session history.
/// </summary>
public sealed class EditorSessionCommandSummary
{
    /// <summary>
    /// Command action represented by this entry.
    /// </summary>
    public required EditorSessionCommandKind Kind { get; init; }

    /// <summary>
    /// Stable host-facing label for this command.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Chosen routing source for this command.
    /// </summary>
    public required EditorSessionCommandSourceKind SourceKind { get; init; }

    /// <summary>
    /// Underlying staged command when <see cref="SourceKind"/> is <see cref="EditorSessionCommandSourceKind.Staged"/>.
    /// </summary>
    public EditorSessionStagedCommandSummary? StagedCommand { get; init; }

    /// <summary>
    /// Underlying applied-history command when <see cref="SourceKind"/> is <see cref="EditorSessionCommandSourceKind.History"/>.
    /// </summary>
    public EditorSessionHistoryCommandSummary? HistoryCommand { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this command can currently execute.
    /// </summary>
    public required bool CanExecute { get; init; }
}
