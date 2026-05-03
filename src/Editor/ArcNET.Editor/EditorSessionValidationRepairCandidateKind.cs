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

    /// <summary>
    /// Replace one script description with an explicit disk-safe value.
    /// </summary>
    SetScriptDescription = 2,

    /// <summary>
    /// Clear one broken script reference from a direct asset by retargeting it to script ID 0.
    /// </summary>
    ClearAssetScriptReference = 3,

    /// <summary>
    /// Clear one broken proto reference from a direct asset by rewriting matching object headers to a null proto ID.
    /// </summary>
    ClearAssetProtoReference = 4,

    /// <summary>
    /// Add one missing proto display-name entry through the message-asset authoring path.
    /// </summary>
    SetProtoDisplayName = 5,

    /// <summary>
    /// Preserve the first dialog entry number and renumber later duplicates to unused entry numbers.
    /// </summary>
    RenumberDuplicateDialogEntryNumber = 6,

    /// <summary>
    /// Clear non-empty script attachment entries that fall outside ArcNET's currently named attachment range.
    /// </summary>
    ClearUnknownScriptAttachmentSlots = 7,
}
