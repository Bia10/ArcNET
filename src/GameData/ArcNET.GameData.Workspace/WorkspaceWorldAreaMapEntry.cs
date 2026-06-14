namespace ArcNET.GameData.Workspace;

/// <summary>
/// One map-list entry linked to a world area.
/// </summary>
public sealed class WorkspaceWorldAreaMapEntry
{
    /// <summary>
    /// Logical map name from <c>Rules/MapList.mes</c>.
    /// </summary>
    public required string MapName { get; init; }

    /// <summary>
    /// Suggested local entry X coordinate from <c>Rules/MapList.mes</c>.
    /// </summary>
    public int EntryTileX { get; init; }

    /// <summary>
    /// Suggested local entry Y coordinate from <c>Rules/MapList.mes</c>.
    /// </summary>
    public int EntryTileY { get; init; }

    /// <summary>
    /// Optional world-map selector value carried by the source table.
    /// </summary>
    public int? WorldMapId { get; init; }

    /// <summary>
    /// Optional map classification carried by the source table.
    /// </summary>
    public string? Type { get; init; }
}
