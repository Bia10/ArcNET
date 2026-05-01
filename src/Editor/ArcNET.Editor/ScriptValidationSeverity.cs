namespace ArcNET.Editor;

/// <summary>
/// Severity levels for <see cref="ScriptValidationIssue"/>.
/// </summary>
public enum ScriptValidationSeverity
{
    /// <summary>
    /// Informational only.
    /// </summary>
    Info,

    /// <summary>
    /// Potential script-authoring problem; output may still work depending on engine behavior.
    /// </summary>
    Warning,

    /// <summary>
    /// Structurally invalid script content.
    /// </summary>
    Error,
}
