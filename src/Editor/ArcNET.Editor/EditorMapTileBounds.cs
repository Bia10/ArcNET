namespace ArcNET.Editor;

/// <summary>
/// Visible tile-space bounds for one host viewport projected from an editor map camera.
/// </summary>
public sealed class EditorMapTileBounds
{
    /// <summary>
    /// Minimum visible tile X coordinate.
    /// </summary>
    public required double MinTileX { get; init; }

    /// <summary>
    /// Minimum visible tile Y coordinate.
    /// </summary>
    public required double MinTileY { get; init; }

    /// <summary>
    /// Maximum visible tile X coordinate.
    /// </summary>
    public required double MaxTileX { get; init; }

    /// <summary>
    /// Maximum visible tile Y coordinate.
    /// </summary>
    public required double MaxTileY { get; init; }

    /// <summary>
    /// Visible width in tile units.
    /// </summary>
    public double Width => MaxTileX - MinTileX;

    /// <summary>
    /// Visible height in tile units.
    /// </summary>
    public double Height => MaxTileY - MinTileY;
}
