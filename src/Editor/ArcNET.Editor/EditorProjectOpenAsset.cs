namespace ArcNET.Editor;

/// <summary>
/// Persisted open-document state for one workspace asset.
/// </summary>
public sealed class EditorProjectOpenAsset
{
    /// <summary>
    /// Normalized workspace asset path.
    /// </summary>
    public required string AssetPath { get; init; }

    /// <summary>
    /// Optional host-defined view identifier for the open asset.
    /// </summary>
    public string? ViewId { get; init; }

    /// <summary>
    /// Indicates whether the host marked the document as pinned.
    /// </summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Additional host-defined document state.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
}
