namespace ArcNET.Editor;

/// <summary>
/// Discriminator for one normalized scene render-queue item.
/// </summary>
public enum EditorMapRenderQueueItemKind
{
    /// <summary>
    /// One projected floor tile.
    /// </summary>
    FloorTile = 0,

    /// <summary>
    /// One projected placed object.
    /// </summary>
    Object = 1,

    /// <summary>
    /// One projected roof cell.
    /// </summary>
    Roof = 2,

    /// <summary>
    /// One projected live placement preview object.
    /// </summary>
    PlacementPreviewObject = 3,

    /// <summary>
    /// One projected tile-semantic overlay.
    /// </summary>
    TileOverlay = 4,
}
