namespace ArcNET.Editor;

/// <summary>
/// Discriminator for one tile-semantic overlay projected into the normalized scene render space.
/// </summary>
public enum EditorMapTileOverlayKind
{
    /// <summary>
    /// Highlights one blocked floor tile.
    /// </summary>
    BlockedTile = 0,

    /// <summary>
    /// Highlights one floor tile targeted by a light.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Highlights one floor tile targeted by a tile script.
    /// </summary>
    Script = 2,
}
