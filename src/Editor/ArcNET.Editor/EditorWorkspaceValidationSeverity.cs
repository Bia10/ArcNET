namespace ArcNET.Editor;

/// <summary>
/// Severity levels for <see cref="EditorWorkspaceValidationIssue"/>.
/// </summary>
public enum EditorWorkspaceValidationSeverity
{
    /// <summary>
    /// Informational finding that may indicate an unsupported or unknown authoring detail.
    /// </summary>
    Info,

    /// <summary>
    /// Non-fatal integrity issue that should usually be fixed before shipping content.
    /// </summary>
    Warning,

    /// <summary>
    /// Broken cross-file reference that likely prevents correct asset resolution.
    /// </summary>
    Error,
}
