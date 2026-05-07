namespace ArcNET.Editor;

/// <summary>
/// One world-area anchor plus the local maps associated with it.
/// </summary>
public sealed class EditorWorldAreaEntry
{
    /// <summary>
    /// Stable area identifier from <c>mes/gamearea.mes</c> and related tables.
    /// </summary>
    public required int AreaId { get; init; }

    /// <summary>
    /// User-facing anchor name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Global world-scene X coordinate.
    /// </summary>
    public required int WorldX { get; init; }

    /// <summary>
    /// Global world-scene Y coordinate.
    /// </summary>
    public required int WorldY { get; init; }

    /// <summary>
    /// Suggested label offset from the metadata table.
    /// </summary>
    public int LabelOffsetX { get; init; }

    /// <summary>
    /// Suggested label offset from the metadata table.
    /// </summary>
    public int LabelOffsetY { get; init; }

    /// <summary>
    /// World-area description from <c>mes/gamearea.mes</c>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional radius metadata carried by the source table.
    /// </summary>
    public int? Radius { get; init; }

    /// <summary>
    /// Whether the entry is explicitly marked as visible on the world-town table.
    /// </summary>
    public bool IsWorldMapVisible { get; init; }

    /// <summary>
    /// Local map entries linked back to this area id by <c>Rules/MapList.mes</c>.
    /// </summary>
    public IReadOnlyList<EditorWorldAreaMapEntry> MapEntries { get; init; } = [];

    /// <summary>
    /// Whether this entry has usable world-scene coordinates.
    /// </summary>
    public bool HasWorldCoordinates => WorldX != 0 || WorldY != 0;
}
