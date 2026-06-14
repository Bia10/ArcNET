namespace ArcNET.GameData.Workspace;

public sealed record class WorldMapCatalogEntry(
    int AreaId,
    string DisplayName,
    int WorldX,
    int WorldY,
    bool IsWorldMapVisible,
    string? Description,
    string CoordinateText,
    string MapSummaryText,
    IReadOnlyList<string> MapNames
)
{
    public bool HasWorldCoordinates => WorldX != 0 || WorldY != 0;
}
