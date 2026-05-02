namespace ArcNET.Editor;

/// <summary>
/// Result of applying one layer-brush request to grouped scene-sector hits.
/// </summary>
public sealed class EditorMapLayerBrushResult
{
    /// <summary>
    /// Direct sector changes staged by the brush request.
    /// </summary>
    public IReadOnlyList<EditorSessionChange> Changes { get; init; } = [];

    /// <summary>
    /// Number of changes in <see cref="Changes"/>.
    /// </summary>
    public int ChangeCount => Changes.Count;

    /// <summary>
    /// Returns <see langword="true"/> when the request staged one or more layer changes.
    /// </summary>
    public bool HasChanges => ChangeCount > 0;
}
