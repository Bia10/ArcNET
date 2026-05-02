using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Render-ready metadata for one projected floor tile.
/// </summary>
public sealed class EditorMapFloorTileRenderItem
{
    private const int TilesPerRoofCellAxis = 4;

    /// <summary>
    /// Normalized sector asset path that owns the tile.
    /// </summary>
    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Map-local tile X coordinate.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local tile Y coordinate.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Sector-local tile coordinate.
    /// </summary>
    public required Location Tile { get; init; }

    /// <summary>
    /// Floor-tile art identifier.
    /// </summary>
    public required ArtId ArtId { get; init; }

    /// <summary>
    /// Indicates whether this tile is marked as blocked in the source sector.
    /// </summary>
    public required bool IsBlocked { get; init; }

    /// <summary>
    /// Indicates whether one or more sector lights target this tile.
    /// </summary>
    public required bool HasLight { get; init; }

    /// <summary>
    /// Indicates whether one or more tile scripts target this tile.
    /// </summary>
    public required bool HasScript { get; init; }

    /// <summary>
    /// Stable back-to-front draw order for hosts that render the returned tiles sequentially.
    /// </summary>
    public required int DrawOrder { get; init; }

    /// <summary>
    /// Projected screen-space tile center X coordinate in the normalized preview bounds.
    /// </summary>
    public required double CenterX { get; init; }

    /// <summary>
    /// Projected screen-space tile center Y coordinate in the normalized preview bounds.
    /// </summary>
    public required double CenterY { get; init; }

    /// <summary>
    /// Sector-local roof-cell coordinate derived from <see cref="Tile"/>.
    /// </summary>
    public Location RoofCell =>
        new(checked((short)(Tile.X / TilesPerRoofCellAxis)), checked((short)(Tile.Y / TilesPerRoofCellAxis)));
}
