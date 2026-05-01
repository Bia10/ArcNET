namespace ArcNET.Editor;

/// <summary>
/// Persisted host-defined tool state.
/// </summary>
public sealed class EditorProjectToolState
{
    /// <summary>
    /// Stable tool identifier.
    /// </summary>
    public required string ToolId { get; init; }

    /// <summary>
    /// Optional host-defined scope or pane identifier for multi-instance tools.
    /// </summary>
    public string? ScopeId { get; init; }

    /// <summary>
    /// Host-defined tool-state properties.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
}
