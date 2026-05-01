namespace ArcNET.Editor;

/// <summary>
/// Persisted bookmark targeting one workspace asset.
/// </summary>
public sealed class EditorProjectBookmark
{
    /// <summary>
    /// Stable host-defined bookmark identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Normalized workspace asset path targeted by the bookmark.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Optional host-visible bookmark label.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional host-defined view identifier for the bookmark target.
    /// </summary>
    public string? ViewId { get; init; }

    /// <summary>
    /// Optional host-defined location token inside the asset.
    /// </summary>
    public string? LocationKey { get; init; }

    /// <summary>
    /// Additional host-defined bookmark state.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
}
