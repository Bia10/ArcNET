namespace ArcNET.Editor;

/// <summary>
/// One unified normalized render-queue item that hosts can paint in order.
/// </summary>
public sealed class EditorMapRenderQueueItem
{
    /// <summary>
    /// Item discriminator for the queue payload.
    /// </summary>
    public required EditorMapRenderQueueItemKind Kind { get; init; }

    /// <summary>
    /// Stable host-facing draw order in the combined queue.
    /// </summary>
    public required int DrawOrder { get; init; }

    /// <summary>
    /// Stable internal sort key used to derive <see cref="DrawOrder"/>.
    /// Hosts typically consume <see cref="DrawOrder"/> only.
    /// </summary>
    public required double SortKey { get; init; }

    /// <summary>
    /// Projected floor tile payload when <see cref="Kind"/> is <see cref="EditorMapRenderQueueItemKind.FloorTile"/>.
    /// </summary>
    public EditorMapFloorTileRenderItem? Tile { get; init; }

    /// <summary>
    /// Projected placed-object payload when <see cref="Kind"/> is <see cref="EditorMapRenderQueueItemKind.Object"/>.
    /// </summary>
    public EditorMapObjectRenderItem? Object { get; init; }

    /// <summary>
    /// Projected roof payload when <see cref="Kind"/> is <see cref="EditorMapRenderQueueItemKind.Roof"/>.
    /// </summary>
    public EditorMapRoofRenderItem? Roof { get; init; }

    /// <summary>
    /// Projected live placement preview payload when <see cref="Kind"/> is <see cref="EditorMapRenderQueueItemKind.PlacementPreviewObject"/>.
    /// </summary>
    public EditorMapPlacementPreviewObject? PlacementPreviewObject { get; init; }

    /// <summary>
    /// Projected tile overlay payload when <see cref="Kind"/> is <see cref="EditorMapRenderQueueItemKind.TileOverlay"/>.
    /// </summary>
    public EditorMapTileOverlayRenderItem? TileOverlay { get; init; }
}
