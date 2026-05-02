using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Render-ready metadata for one projected roof cell.
/// </summary>
public sealed class EditorMapRoofRenderItem
{
    /// <summary>
    /// Normalized sector asset path that owns the roof cell.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Sector-local roof-cell coordinate.
    /// </summary>
    public required Location RoofCell { get; init; }

    /// <summary>
    /// Map-local tile X coordinate of the roof footprint origin.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local tile Y coordinate of the roof footprint origin.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Roof art identifier.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// Stable draw order among projected roof cells.
    /// </summary>
    public required int DrawOrder { get; init; }

    /// <summary>
    /// Projected screen-space anchor X coordinate in normalized preview bounds.
    /// </summary>
    public required double AnchorX { get; init; }

    /// <summary>
    /// Projected screen-space anchor Y coordinate in normalized preview bounds.
    /// </summary>
    public required double AnchorY { get; init; }

    /// <summary>
    /// Footprint width in map tiles. One roof cell spans a 4x4 floor-tile block.
    /// </summary>
    public int FootprintTileWidth => 4;

    /// <summary>
    /// Footprint height in map tiles. One roof cell spans a 4x4 floor-tile block.
    /// </summary>
    public int FootprintTileHeight => 4;
}
