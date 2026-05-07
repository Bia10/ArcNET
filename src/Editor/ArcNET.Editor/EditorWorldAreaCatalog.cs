namespace ArcNET.Editor;

/// <summary>
/// World-area anchors derived from installation message metadata.
/// </summary>
public sealed class EditorWorldAreaCatalog
{
    /// <summary>
    /// Logical map name of the global world-scene map, when present.
    /// </summary>
    public string? WorldSceneMapName { get; init; }

    /// <summary>
    /// Parsed world areas ordered for browsing and rendering.
    /// </summary>
    public IReadOnlyList<EditorWorldAreaEntry> Areas { get; init; } = [];

    /// <summary>
    /// Finds one world area by area identifier.
    /// </summary>
    public EditorWorldAreaEntry? FindArea(int areaId) => Areas.FirstOrDefault(area => area.AreaId == areaId);

    /// <summary>
    /// Finds the world area associated with one logical map name.
    /// </summary>
    public EditorWorldAreaEntry? FindAreaForMap(string mapName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);

        return Areas.FirstOrDefault(area =>
            area.MapEntries.Any(map => string.Equals(map.MapName, mapName, StringComparison.OrdinalIgnoreCase))
        );
    }
}
