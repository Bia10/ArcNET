namespace ArcNET.Editor;

/// <summary>
/// Preview-ready placement metadata for one per-tile script entry.
/// </summary>
public sealed class EditorMapTileScriptPreview
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
    /// Script identifier attached to the tile.
    /// </summary>
    public required int ScriptId { get; init; }

    /// <summary>
    /// Raw node flags.
    /// </summary>
    public required uint NodeFlags { get; init; }

    /// <summary>
    /// Raw script header flags.
    /// </summary>
    public required uint ScriptFlags { get; init; }

    /// <summary>
    /// Raw script counter bitmask.
    /// </summary>
    public required uint ScriptCounters { get; init; }
}
