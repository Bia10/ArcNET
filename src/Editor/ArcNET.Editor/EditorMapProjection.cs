namespace ArcNET.Editor;

/// <summary>
/// One map's sector layout projected into a 2D sector grid.
/// </summary>
public sealed class EditorMapProjection
{
    /// <summary>
    /// Map directory name that owns the projected sectors.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Minimum absolute sector-grid X coordinate across all positioned sectors.
    /// </summary>
    public required int MinSectorX { get; init; }

    /// <summary>
    /// Minimum absolute sector-grid Y coordinate across all positioned sectors.
    /// </summary>
    public required int MinSectorY { get; init; }

    /// <summary>
    /// Maximum absolute sector-grid X coordinate across all positioned sectors.
    /// </summary>
    public required int MaxSectorX { get; init; }

    /// <summary>
    /// Maximum absolute sector-grid Y coordinate across all positioned sectors.
    /// </summary>
    public required int MaxSectorY { get; init; }

    /// <summary>
    /// Positioned sectors that could be placed into the 2D grid.
    /// </summary>
    public required IReadOnlyList<EditorMapSectorProjection> Sectors { get; init; }

    /// <summary>
    /// Number of indexed sectors for this map whose asset paths did not encode a sector-grid location.
    /// </summary>
    public required int UnpositionedSectorCount { get; init; }

    /// <summary>
    /// Width of the dense map-local projection grid.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Height of the dense map-local projection grid.
    /// </summary>
    public required int Height { get; init; }
}
