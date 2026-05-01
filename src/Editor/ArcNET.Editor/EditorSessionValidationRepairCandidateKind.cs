namespace ArcNET.Editor;

/// <summary>
/// Session-scoped staged repair operations that can be applied through <see cref="EditorWorkspaceSession"/>.
/// </summary>
public enum EditorSessionValidationRepairCandidateKind
{
    /// <summary>
    /// Set one dialog entry's IQ requirement to a specific non-negative value.
    /// </summary>
    SetDialogEntryIntelligenceRequirement = 0,

    /// <summary>
    /// Set one dialog entry's response target to a specific dialog entry number or 0.
    /// </summary>
    SetDialogResponseTarget = 1,
}
