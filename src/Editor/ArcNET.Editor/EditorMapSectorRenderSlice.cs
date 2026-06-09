using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

public readonly record struct EditorMapRenderIndexEntry(
    EditorMapRenderQueueItemKind Kind,
    int PayloadIndex,
    double SortKey,
    int DrawOrder
);

public readonly record struct EditorMapSectorRenderSliceBounds(
    double Left,
    double Top,
    double Width,
    double Height,
    int MinMapTileX,
    int MinMapTileY,
    int MaxMapTileX,
    int MaxMapTileY
)
{
    public double Right => Left + Width;

    public double Bottom => Top + Height;

    public bool Intersects(EditorMapSceneViewportLayout viewport) =>
        Right >= viewport.VisibleLeft
        && Left <= viewport.VisibleRight
        && Bottom >= viewport.VisibleTop
        && Top <= viewport.VisibleBottom;
}

public sealed class EditorMapSectorRenderSlice
{
    private Dictionary<Location, EditorMapFloorTileRenderItem>? _tilesByLocation;
    private Dictionary<Location, IReadOnlyList<EditorMapObjectRenderItem>>? _objectsByLocation;

    public required string SectorAssetPath { get; init; }

    /// <summary>
    /// Stable content revision for this slice. Hosts can key retained rendering by this value;
    /// camera changes must not affect it.
    /// </summary>
    public long Revision { get; init; }

    public required EditorMapSectorRenderSliceBounds Bounds { get; init; }

    public required IReadOnlyList<EditorMapRenderIndexEntry> Queue { get; init; }

    public IReadOnlyList<EditorMapFloorTileRenderItem> Tiles { get; init; } = [];

    public IReadOnlyList<EditorMapTileOverlayRenderItem> Overlays { get; init; } = [];

    public IReadOnlyList<EditorMapObjectRenderItem> Objects { get; init; } = [];

    public IReadOnlyList<EditorMapObjectAuxiliaryRenderItem> ObjectAuxiliaryItems { get; init; } = [];

    public IReadOnlyList<EditorMapRoofRenderItem> Roofs { get; init; } = [];

    public IReadOnlyList<EditorMapLightRenderItem> Lights { get; init; } = [];

    public bool TryGetTile(Location tile, out EditorMapFloorTileRenderItem? item)
    {
        var tilesByLocation = _tilesByLocation ??= Tiles.ToDictionary(static tile => tile.Tile);
        return tilesByLocation.TryGetValue(tile, out item);
    }

    public IReadOnlyList<EditorMapObjectRenderItem> GetObjectsAtTile(Location tile)
    {
        if (_objectsByLocation is null)
        {
            var grouped = new Dictionary<Location, List<EditorMapObjectRenderItem>>();
            for (var index = 0; index < Objects.Count; index++)
            {
                var obj = Objects[index];
                if (!grouped.TryGetValue(obj.Tile, out var tileObjects))
                {
                    tileObjects = [];
                    grouped[obj.Tile] = tileObjects;
                }

                tileObjects.Add(obj);
            }

            _objectsByLocation = grouped.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyList<EditorMapObjectRenderItem>)pair.Value.ToArray()
            );
        }

        return _objectsByLocation.TryGetValue(tile, out var objects) ? objects : [];
    }

    internal EditorMapRenderQueueItem CreateRenderQueueItem(int queueIndex)
    {
        var entry = Queue[queueIndex];
        return entry.Kind switch
        {
            EditorMapRenderQueueItemKind.FloorTile => new EditorMapRenderQueueItem
            {
                Kind = entry.Kind,
                DrawOrder = entry.DrawOrder,
                SortKey = entry.SortKey,
                Tile = Tiles[entry.PayloadIndex],
            },
            EditorMapRenderQueueItemKind.Object => new EditorMapRenderQueueItem
            {
                Kind = entry.Kind,
                DrawOrder = entry.DrawOrder,
                SortKey = entry.SortKey,
                Object = Objects[entry.PayloadIndex],
            },
            EditorMapRenderQueueItemKind.TileOverlay => new EditorMapRenderQueueItem
            {
                Kind = entry.Kind,
                DrawOrder = entry.DrawOrder,
                SortKey = entry.SortKey,
                TileOverlay = Overlays[entry.PayloadIndex],
            },
            EditorMapRenderQueueItemKind.Roof => new EditorMapRenderQueueItem
            {
                Kind = entry.Kind,
                DrawOrder = entry.DrawOrder,
                SortKey = entry.SortKey,
                Roof = Roofs[entry.PayloadIndex],
            },
            EditorMapRenderQueueItemKind.ObjectAuxiliary => new EditorMapRenderQueueItem
            {
                Kind = entry.Kind,
                DrawOrder = entry.DrawOrder,
                SortKey = entry.SortKey,
                ObjectAuxiliaryItem = ObjectAuxiliaryItems[entry.PayloadIndex],
                CommittedRenderLayer = ObjectAuxiliaryItems[entry.PayloadIndex].CommittedRenderLayer,
            },
            EditorMapRenderQueueItemKind.Light => new EditorMapRenderQueueItem
            {
                Kind = entry.Kind,
                DrawOrder = entry.DrawOrder,
                SortKey = entry.SortKey,
                Light = Lights[entry.PayloadIndex],
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(entry.Kind),
                entry.Kind,
                "Unsupported render queue kind."
            ),
        };
    }
}
