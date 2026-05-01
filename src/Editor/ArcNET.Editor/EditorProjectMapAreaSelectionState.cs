using ArcNET.Core.Primitives;

namespace ArcNET.Editor;

/// <summary>
/// Optional map-local tile area persisted with one map selection.
/// </summary>
public sealed class EditorProjectMapAreaSelectionState
{
    /// <summary>
    /// Inclusive minimum map-local tile X coordinate covered by the selection.
    /// </summary>
    public required int MinMapTileX { get; init; }

    /// <summary>
    /// Inclusive minimum map-local tile Y coordinate covered by the selection.
    /// </summary>
    public required int MinMapTileY { get; init; }

    /// <summary>
    /// Inclusive maximum map-local tile X coordinate covered by the selection.
    /// </summary>
    public required int MaxMapTileX { get; init; }

    /// <summary>
    /// Inclusive maximum map-local tile Y coordinate covered by the selection.
    /// </summary>
    public required int MaxMapTileY { get; init; }

    /// <summary>
    /// Selected object identifiers captured inside the area, in stable area-hit order.
    /// </summary>
    public IReadOnlyList<GameObjectGuid> ObjectIds { get; init; } = [];

    /// <summary>
    /// Returns <see langword="true"/> when the area captured one or more object identifiers.
    /// </summary>
    public bool HasObjectSelection => ObjectIds.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the area captured more than one object identifier.
    /// </summary>
    public bool HasMultipleObjectSelection => ObjectIds.Count > 1;

    /// <summary>
    /// Selected map-local tile width.
    /// </summary>
    public int Width => (MaxMapTileX - MinMapTileX) + 1;

    /// <summary>
    /// Selected map-local tile height.
    /// </summary>
    public int Height => (MaxMapTileY - MinMapTileY) + 1;

    /// <summary>
    /// Total selected tile count covered by the area bounds.
    /// </summary>
    public int TileCount => checked(Width * Height);

    /// <summary>
    /// Returns <see langword="true"/> when the supplied map-local tile lies inside the selection bounds.
    /// </summary>
    public bool ContainsMapTile(int mapTileX, int mapTileY) =>
        mapTileX >= MinMapTileX && mapTileX <= MaxMapTileX && mapTileY >= MinMapTileY && mapTileY <= MaxMapTileY;

    /// <summary>
    /// Enumerates selected map-local tiles in stable screen order.
    /// </summary>
    public IEnumerable<(int MapTileX, int MapTileY)> EnumerateMapTiles()
    {
        for (var mapTileY = MaxMapTileY; mapTileY >= MinMapTileY; mapTileY--)
        {
            for (var mapTileX = MinMapTileX; mapTileX <= MaxMapTileX; mapTileX++)
                yield return (mapTileX, mapTileY);
        }
    }
}
