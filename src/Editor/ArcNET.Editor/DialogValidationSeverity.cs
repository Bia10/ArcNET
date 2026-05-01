namespace ArcNET.Editor;

/// <summary>
/// Severity levels for <see cref="DialogValidationIssue"/>.
/// </summary>
public enum DialogValidationSeverity
{
    /// <summary>
    /// Informational only.
    /// </summary>
    Info,

    /// <summary>
    /// Potential dialog-authoring problem; output may still work depending on engine behavior.
    /// </summary>
    Warning,

    /// <summary>
    /// Structurally invalid dialog content.
    /// </summary>
    Error,
}
