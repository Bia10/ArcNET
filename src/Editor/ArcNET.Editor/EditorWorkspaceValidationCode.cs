namespace ArcNET.Editor;

/// <summary>
/// Stable machine-readable workspace validation rules.
/// </summary>
public enum EditorWorkspaceValidationCode
{
    /// <summary>
    /// One asset references a proto number that is not defined in the effective workspace.
    /// </summary>
    MissingProtoDefinition = 0,

    /// <summary>
    /// One proto asset has no matching display-name entry in the expected message sources.
    /// </summary>
    MissingProtoDisplayName = 1,

    /// <summary>
    /// One asset references a script identifier that is not defined in the effective workspace.
    /// </summary>
    MissingScriptDefinition = 2,
}
