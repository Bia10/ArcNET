namespace ArcNET.Editor;

/// <summary>
/// Persisted host-defined view state associated with a workspace asset or layout view.
/// </summary>
public sealed class EditorProjectViewState
{
    /// <summary>
    /// Stable host-defined identifier for this view-state entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Optional workspace asset path that owns this view state.
    /// </summary>
    public string? AssetPath { get; init; }

    /// <summary>
    /// Optional host-defined view identifier.
    /// </summary>
    public string? ViewId { get; init; }

    /// <summary>
    /// Host-defined view-state properties.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
}
