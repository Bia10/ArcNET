using ArcNET.GameData.Workspace;

namespace ArcNET.Editor;

internal static class EditorWorldAreaCatalogBuilder
{
    public static EditorWorldAreaCatalog Build(EditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var catalog = WorkspaceWorldAreaCatalogBuilder.Build(workspace.GameData);
        return new EditorWorldAreaCatalog
        {
            WorldSceneMapName = catalog.WorldSceneMapName,
            Areas = [.. catalog.Areas.Select(CreateArea)],
        };
    }

    private static EditorWorldAreaEntry CreateArea(WorkspaceWorldAreaEntry area) =>
        new()
        {
            AreaId = area.AreaId,
            DisplayName = area.DisplayName,
            WorldX = area.WorldX,
            WorldY = area.WorldY,
            LabelOffsetX = area.LabelOffsetX,
            LabelOffsetY = area.LabelOffsetY,
            Description = area.Description,
            Radius = area.Radius,
            IsWorldMapVisible = area.IsWorldMapVisible,
            MapEntries = [.. area.MapEntries.Select(CreateMapEntry)],
        };

    private static EditorWorldAreaMapEntry CreateMapEntry(WorkspaceWorldAreaMapEntry entry) =>
        new()
        {
            MapName = entry.MapName,
            EntryTileX = entry.EntryTileX,
            EntryTileY = entry.EntryTileY,
            WorldMapId = entry.WorldMapId,
            Type = entry.Type,
        };
}
