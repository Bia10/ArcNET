namespace ArcNET.Editor;

/// <summary>
/// Stable machine-readable dialog validation rules produced by <see cref="DialogValidator"/>.
/// </summary>
public enum DialogValidationCode
{
    /// <summary>
    /// More than one dialog entry uses the same entry number.
    /// </summary>
    DuplicateEntryNumber = 0,

    /// <summary>
    /// A dialog entry uses a negative IQ requirement.
    /// </summary>
    NegativeIntelligenceRequirement = 1,

    /// <summary>
    /// A dialog entry points at a positive response target that does not exist.
    /// </summary>
    MissingResponseTarget = 2,
}
