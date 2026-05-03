namespace ArcNET.Editor;

/// <summary>
/// Thrown when a staged session apply/save would introduce one or more blocking workspace validation errors.
/// </summary>
public sealed class EditorSessionValidationException : InvalidOperationException
{
    /// <summary>
    /// Initializes the exception with the blocking validation findings.
    /// </summary>
    public EditorSessionValidationException(EditorWorkspaceValidationReport validation)
        : this(validation, CreateEmptyImpactSummary(), []) { }

    /// <summary>
    /// Initializes the exception with the blocking validation findings plus staged impact context.
    /// </summary>
    public EditorSessionValidationException(
        EditorWorkspaceValidationReport validation,
        EditorSessionImpactSummary impactSummary
    )
        : this(validation, impactSummary, []) { }

    /// <summary>
    /// Initializes the exception with the blocking validation findings, staged impact context, and available repairs.
    /// </summary>
    public EditorSessionValidationException(
        EditorWorkspaceValidationReport validation,
        EditorSessionImpactSummary impactSummary,
        IReadOnlyList<EditorSessionValidationRepairCandidate> repairCandidates
    )
        : base(CreateMessage(validation))
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(impactSummary);
        ArgumentNullException.ThrowIfNull(repairCandidates);
        Validation = validation;
        ImpactSummary = impactSummary;
        RepairCandidates = repairCandidates;
    }

    /// <summary>
    /// Blocking validation findings that prevented the staged session changes from being applied or saved.
    /// </summary>
    public EditorWorkspaceValidationReport Validation { get; }

    /// <summary>
    /// Staged impact context for the apply/save attempt that was blocked by validation.
    /// </summary>
    public EditorSessionImpactSummary ImpactSummary { get; }

    /// <summary>
    /// Repair candidates currently available for the blocked staged apply/save attempt.
    /// </summary>
    public IReadOnlyList<EditorSessionValidationRepairCandidate> RepairCandidates { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this blocked staged apply/save attempt has one or more repair candidates.
    /// </summary>
    public bool CanRepairFromSession => RepairCandidates.Count > 0;

    /// <summary>
    /// Number of available repair candidates currently exposed for this blocked staged apply/save attempt.
    /// </summary>
    public int RepairCandidateCount => RepairCandidates.Count;

    private static string CreateMessage(EditorWorkspaceValidationReport validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        var errorCount = validation.Issues.Count;
        return errorCount == 1
            ? "Cannot apply staged session changes because they introduce 1 blocking workspace validation error."
            : $"Cannot apply staged session changes because they introduce {errorCount} blocking workspace validation errors.";
    }

    private static EditorSessionImpactSummary CreateEmptyImpactSummary() =>
        new()
        {
            DirectKinds = [],
            DirectTargets = [],
            RelatedKinds = [],
            RelatedAssetPaths = [],
            MapNames = [],
            DefinedProtoNumbers = [],
            DefinedScriptIds = [],
            DefinedDialogIds = [],
            ReferencedProtoNumbers = [],
            ReferencedScriptIds = [],
            ReferencedArtIds = [],
        };
}
