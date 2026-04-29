namespace ArcNET.Editor;

/// <summary>
/// Cross-file validation findings derived from an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorWorkspaceValidationReport
{
    /// <summary>
    /// Empty validation report used when no findings were discovered.
    /// </summary>
    public static EditorWorkspaceValidationReport Empty { get; } = new() { Issues = [] };

    /// <summary>
    /// All workspace validation findings in stable display order.
    /// </summary>
    public required IReadOnlyList<EditorWorkspaceValidationIssue> Issues { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when any validation findings were discovered.
    /// </summary>
    public bool HasIssues => Issues.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the report contains at least one error.
    /// </summary>
    public bool HasErrors => Issues.Any(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Error);
}
