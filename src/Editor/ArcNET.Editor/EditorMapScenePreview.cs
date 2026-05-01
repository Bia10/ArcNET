namespace ArcNET.Editor;

/// <summary>
/// Rich preview data for one projected map using the loaded sector payloads from a workspace.
/// </summary>
public sealed class EditorMapScenePreview
{
    /// <summary>
    /// Map name that owns the preview.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Dense map width in sector cells.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Dense map height in sector cells.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Number of indexed sectors whose source paths did not encode a sector-grid location.
    /// </summary>
    public required int UnpositionedSectorCount { get; init; }

    /// <summary>
    /// Per-sector preview data in dense local-grid coordinates.
    /// </summary>
    public required IReadOnlyList<EditorMapSectorScenePreview> Sectors { get; init; }
}
