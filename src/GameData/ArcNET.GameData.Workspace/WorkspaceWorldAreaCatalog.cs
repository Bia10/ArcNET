namespace ArcNET.GameData.Workspace;

/// <summary>
/// World-area anchors derived from installation message metadata.
/// </summary>
public sealed class WorkspaceWorldAreaCatalog
{
    /// <summary>
    /// Logical map name of the global world-scene map, when present.
    /// </summary>
    public string? WorldSceneMapName { get; init; }

    /// <summary>
    /// Parsed world areas ordered for browsing and rendering.
    /// </summary>
    public IReadOnlyList<WorkspaceWorldAreaEntry> Areas { get; init; } = [];

    /// <summary>
    /// Finds one world area by area identifier.
    /// </summary>
    public WorkspaceWorldAreaEntry? FindArea(int areaId) => Areas.FirstOrDefault(area => area.AreaId == areaId);

    /// <summary>
    /// Finds the world area associated with one logical map name.
    /// </summary>
    public WorkspaceWorldAreaEntry? FindAreaForMap(string mapName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);

        return Areas.FirstOrDefault(area =>
            area.MapEntries.Any(map => string.Equals(map.MapName, mapName, StringComparison.OrdinalIgnoreCase))
        );
    }
}
