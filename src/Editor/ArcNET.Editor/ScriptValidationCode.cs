namespace ArcNET.Editor;

/// <summary>
/// Stable machine-readable rules reported by <see cref="ScriptValidator"/>.
/// </summary>
public enum ScriptValidationCode
{
    /// <summary>
    /// The script description exceeds the 40 ASCII characters preserved on disk.
    /// </summary>
    DescriptionTooLong = 0,

    /// <summary>
    /// The script description contains one or more non-ASCII characters that will not round-trip as written.
    /// </summary>
    DescriptionContainsNonAscii = 1,

    /// <summary>
    /// One or more active attachment slots fall outside ArcNET's currently named slot range.
    /// </summary>
    UnknownAttachmentSlot = 2,
}
