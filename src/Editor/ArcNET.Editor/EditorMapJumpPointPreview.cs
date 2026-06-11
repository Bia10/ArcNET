namespace ArcNET.Editor;

/// <summary>
/// Preview-ready placement metadata for one map jump point.
/// </summary>
public sealed class EditorMapJumpPointPreview
{
    /// <summary>
    /// Zero-based tile index in the 64x64 sector tile grid.
    /// </summary>
    public required int TileIndex { get; init; }

    /// <summary>
    /// Tile X coordinate within the sector.
    /// </summary>
    public required int TileX { get; init; }

    /// <summary>
    /// Tile Y coordinate within the sector.
    /// </summary>
    public required int TileY { get; init; }

    /// <summary>
    /// Map-local source tile X coordinate.
    /// </summary>
    public required int MapTileX { get; init; }

    /// <summary>
    /// Map-local source tile Y coordinate.
    /// </summary>
    public required int MapTileY { get; init; }

    /// <summary>
    /// Destination map identifier.
    /// </summary>
    public required int DestinationMapId { get; init; }

    /// <summary>
    /// Destination tile X coordinate.
    /// </summary>
    public required int DestinationTileX { get; init; }

    /// <summary>
    /// Destination tile Y coordinate.
    /// </summary>
    public required int DestinationTileY { get; init; }

    /// <summary>
    /// Raw jump-point flags.
    /// </summary>
    public required uint Flags { get; init; }
}
