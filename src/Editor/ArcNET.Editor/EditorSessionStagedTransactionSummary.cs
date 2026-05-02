namespace ArcNET.Editor;

/// <summary>
/// One host-facing staged transaction currently tracked by <see cref="EditorWorkspaceSession"/>.
/// This summarizes the current staged state for one dialog, script, save, or direct-asset history scope
/// without requiring hosts to inspect editor-specific implementations directly.
/// </summary>
public sealed class EditorSessionStagedTransactionSummary
{
    /// <summary>
    /// High-level staged transaction kind represented by this entry.
    /// </summary>
    public required EditorSessionStagedHistoryScopeKind Kind { get; init; }

    /// <summary>
    /// Primary transaction target identifier when the transaction maps to one stable target.
    /// Dialog and script transactions use normalized asset paths.
    /// Save transactions use the current save-slot target.
    /// Direct-asset transactions use <see langword="null"/> because they can span multiple assets.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Stable host-facing label for this transaction.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Stable list of currently affected targets for this transaction.
    /// Dialog, script, and save transactions keep their primary target here even when the current edit was undone.
    /// </summary>
    public required IReadOnlyList<string> AffectedTargets { get; init; }

    /// <summary>
    /// Current pending session changes represented by this transaction in stable display order.
    /// This list is empty when the transaction only has redo state and no current staged change.
    /// </summary>
    public required IReadOnlyList<EditorSessionChange> PendingChanges { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction currently has staged changes.
    /// </summary>
    public required bool HasPendingChanges { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction can undo one staged change.
    /// </summary>
    public required bool CanUndo { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction can redo one staged change.
    /// </summary>
    public required bool CanRedo { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction currently participates in a session state
    /// that can be applied through the existing whole-session apply/save pathway.
    /// This is a session-level actionability flag, not a per-transaction selective apply operation.
    /// </summary>
    public required bool CanApplyFromSession { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction currently participates in staged session work
    /// that can be discarded through the existing whole-session discard pathway.
    /// This is a session-level actionability flag, not a per-transaction selective discard operation.
    /// </summary>
    public required bool CanDiscardFromSession { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction can currently be applied on its own
    /// through the selected-transaction apply pathway.
    /// </summary>
    public required bool CanApplyIndividually { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction can currently be saved on its own
    /// through the selected-transaction save pathway.
    /// </summary>
    public required bool CanSaveIndividually { get; init; }

    /// <summary>
    /// Blocking validation findings introduced when applying just this transaction.
    /// This is empty when the transaction has no currently staged changes or when its staged changes
    /// do not introduce new blocking validation errors.
    /// </summary>
    public required EditorWorkspaceValidationReport BlockingValidation { get; init; }

    /// <summary>
    /// Repair candidates currently available for this transaction.
    /// This is empty when no staged repairs are available for the transaction.
    /// </summary>
    public required IReadOnlyList<EditorSessionValidationRepairCandidate> RepairCandidates { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this transaction currently has one or more available repair candidates.
    /// </summary>
    public bool CanRepairFromSession => RepairCandidates.Count > 0;

    /// <summary>
    /// Number of current pending changes grouped under this transaction.
    /// </summary>
    public int PendingChangeCount => PendingChanges.Count;

    /// <summary>
    /// Number of available repair candidates currently exposed for this transaction.
    /// </summary>
    public int RepairCandidateCount => RepairCandidates.Count;
}
