namespace ArcNET.Editor;

/// <summary>
/// Read-only summary of the current staged session changes plus the validation state that would apply to them.
/// </summary>
public sealed class EditorSessionPendingChangeSummary
{
    /// <summary>
    /// Individual staged targets paired with any dependency context that can be resolved from the effective pending workspace state.
    /// </summary>
    public required IReadOnlyList<EditorSessionPendingChangeTargetSummary> TargetSummaries { get; init; }

    /// <summary>
    /// Individual staged changes in stable apply/save order.
    /// </summary>
    public required IReadOnlyList<EditorSessionChange> Changes { get; init; }

    /// <summary>
    /// Staged changes grouped by high-level change kind.
    /// </summary>
    public required IReadOnlyList<EditorSessionChangeKindSummary> Groups { get; init; }

    /// <summary>
    /// Full workspace validation report for the staged session state.
    /// </summary>
    public required EditorWorkspaceValidationReport Validation { get; init; }

    /// <summary>
    /// New blocking error-level validation findings introduced by the staged session state relative to the current workspace baseline.
    /// </summary>
    public required EditorWorkspaceValidationReport BlockingValidation { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when one or more session changes are currently staged.
    /// </summary>
    public bool HasChanges => Changes.Count > 0;

    /// <summary>
    /// Total number of staged session changes.
    /// </summary>
    public int TotalChangeCount => Changes.Count;
}
