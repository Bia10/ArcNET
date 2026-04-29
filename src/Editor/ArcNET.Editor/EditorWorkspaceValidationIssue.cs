namespace ArcNET.Editor;

/// <summary>
/// One workspace-level validation finding derived from indexed asset relationships.
/// </summary>
public sealed record EditorWorkspaceValidationIssue
{
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

    /// <inheritdoc/>
    public override string ToString() =>
        AssetPath is null ? $"[{Severity}] {Message}" : $"[{Severity}] {AssetPath}: {Message}";

    internal static EditorWorkspaceValidationIssue Error(string? assetPath, string message) =>
        new()
        {
            Severity = EditorWorkspaceValidationSeverity.Error,
            AssetPath = assetPath,
            Message = message,
        };

    internal static EditorWorkspaceValidationIssue Warning(string? assetPath, string message) =>
        new()
        {
            Severity = EditorWorkspaceValidationSeverity.Warning,
            AssetPath = assetPath,
            Message = message,
        };

    internal static EditorWorkspaceValidationIssue Info(string? assetPath, string message) =>
        new()
        {
            Severity = EditorWorkspaceValidationSeverity.Info,
            AssetPath = assetPath,
            Message = message,
        };
}
