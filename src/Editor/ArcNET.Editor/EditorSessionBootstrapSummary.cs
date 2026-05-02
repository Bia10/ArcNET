namespace ArcNET.Editor;

/// <summary>
/// Unified host-facing snapshot of the current session shell state.
/// This combines normalized project/session state with the command and transaction summaries
/// a host typically needs immediately after project load or during normal session refresh.
/// </summary>
public sealed class EditorSessionBootstrapSummary
{
    /// <summary>
    /// Current normalized project/session state tracked by the live session.
    /// </summary>
    public required EditorSessionProjectStateSummary ProjectState { get; init; }

    /// <summary>
    /// Optional restore summary associated with this bootstrap snapshot.
    /// This is present when the host created the snapshot from a project restore operation.
    /// </summary>
    public EditorProjectRestoreResult? Restore { get; init; }

    /// <summary>
    /// Current staged transaction summaries exposed by the session.
    /// </summary>
    public required IReadOnlyList<EditorSessionStagedTransactionSummary> StagedTransactions { get; init; }

    /// <summary>
    /// Current staged command summaries exposed by the session.
    /// </summary>
    public required IReadOnlyList<EditorSessionStagedCommandSummary> StagedCommands { get; init; }

    /// <summary>
    /// Current applied-history command summaries exposed by the session.
    /// </summary>
    public required IReadOnlyList<EditorSessionHistoryCommandSummary> HistoryCommands { get; init; }
}
