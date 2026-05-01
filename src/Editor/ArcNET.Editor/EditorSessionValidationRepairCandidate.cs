namespace ArcNET.Editor;

/// <summary>
/// One staged repair suggestion that a host can inspect and apply through <see cref="EditorWorkspaceSession"/>.
/// </summary>
public sealed class EditorSessionValidationRepairCandidate
{
    /// <summary>
    /// Repair operation this candidate would apply.
    /// </summary>
    public required EditorSessionValidationRepairCandidateKind Kind { get; init; }

    /// <summary>
    /// Asset path that owns the repair target.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Dialog entry number the repair targets.
    /// </summary>
    public required int DialogEntryNumber { get; init; }

    /// <summary>
    /// Short host-facing label for the repair.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Longer host-facing explanation of what the repair will change.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Suggested IQ requirement when <see cref="Kind"/> is <see cref="EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement"/>.
    /// </summary>
    public int? SuggestedIntelligenceRequirement { get; init; }

    /// <summary>
    /// Suggested response target when <see cref="Kind"/> is <see cref="EditorSessionValidationRepairCandidateKind.SetDialogResponseTarget"/>.
    /// </summary>
    public int? SuggestedResponseTargetNumber { get; init; }
}
