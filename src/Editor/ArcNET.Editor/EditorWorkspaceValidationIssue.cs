namespace ArcNET.Editor;

/// <summary>
/// One workspace-level validation finding derived from indexed asset relationships.
/// </summary>
public sealed record EditorWorkspaceValidationIssue
{
    /// <summary>
    /// Stable machine-readable rule for the finding when ArcNET can classify it.
    /// </summary>
    public EditorWorkspaceValidationCode? Code { get; init; }

    /// <summary>
    /// Severity of the finding.
    /// </summary>
    public required EditorWorkspaceValidationSeverity Severity { get; init; }

    /// <summary>
    /// Asset path associated with the finding, or <see langword="null"/> for workspace-wide issues.
    /// </summary>
    public string? AssetPath { get; init; }

    /// <summary>
    /// Human-readable description of the finding.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Referenced script identifier for script-reference findings when available.
    /// </summary>
    public int? ReferencedScriptId { get; init; }

    /// <summary>
    /// Referenced proto number for proto-reference findings when available.
    /// </summary>
    public int? ReferencedProtoNumber { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        AssetPath is null ? $"[{Severity}] {Message}" : $"[{Severity}] {AssetPath}: {Message}";

    internal static EditorWorkspaceValidationIssue Error(
        string? assetPath,
        string message,
        EditorWorkspaceValidationCode? code = null,
        int? referencedScriptId = null,
        int? referencedProtoNumber = null
    ) =>
        new()
        {
            Code = code,
            Severity = EditorWorkspaceValidationSeverity.Error,
            AssetPath = assetPath,
            Message = message,
            ReferencedScriptId = referencedScriptId,
            ReferencedProtoNumber = referencedProtoNumber,
        };

    internal static EditorWorkspaceValidationIssue Warning(
        string? assetPath,
        string message,
        EditorWorkspaceValidationCode? code = null,
        int? referencedScriptId = null,
        int? referencedProtoNumber = null
    ) =>
        new()
        {
            Code = code,
            Severity = EditorWorkspaceValidationSeverity.Warning,
            AssetPath = assetPath,
            Message = message,
            ReferencedScriptId = referencedScriptId,
            ReferencedProtoNumber = referencedProtoNumber,
        };

    internal static EditorWorkspaceValidationIssue Info(
        string? assetPath,
        string message,
        EditorWorkspaceValidationCode? code = null,
        int? referencedScriptId = null,
        int? referencedProtoNumber = null
    ) =>
        new()
        {
            Code = code,
            Severity = EditorWorkspaceValidationSeverity.Info,
            AssetPath = assetPath,
            Message = message,
            ReferencedScriptId = referencedScriptId,
            ReferencedProtoNumber = referencedProtoNumber,
        };
}
