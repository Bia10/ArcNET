using ArcNET.Core.Primitives;

namespace ArcNET.Editor.Tests;

public sealed class EditorProjectMapSelectionStateTests
{
    [Test]
    public async Task AreaSelectionState_ReportsTileMembershipAndObjectSet()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 2, Guid.NewGuid());
        var area = new EditorProjectMapAreaSelectionState
        {
            MinMapTileX = 10,
            MinMapTileY = 20,
            MaxMapTileX = 12,
            MaxMapTileY = 21,
            ObjectIds = [firstObjectId, secondObjectId],
        };

        await Assert.That(area.Width).IsEqualTo(3);
        await Assert.That(area.Height).IsEqualTo(2);
        await Assert.That(area.TileCount).IsEqualTo(6);
        await Assert.That(area.HasObjectSelection).IsTrue();
        await Assert.That(area.HasMultipleObjectSelection).IsTrue();
        await Assert.That(area.ContainsMapTile(10, 20)).IsTrue();
        await Assert.That(area.ContainsMapTile(12, 21)).IsTrue();
        await Assert.That(area.ContainsMapTile(13, 21)).IsFalse();
    }

    [Test]
    public async Task AreaSelectionState_EnumeratesTilesInStableScreenOrder()
    {
        var area = new EditorProjectMapAreaSelectionState
        {
            MinMapTileX = 10,
            MinMapTileY = 20,
            MaxMapTileX = 12,
            MaxMapTileY = 21,
        };

        var tiles = area.EnumerateMapTiles().ToArray();

        await Assert.That(tiles.Length).IsEqualTo(6);
        await Assert.That(tiles[0].MapTileX).IsEqualTo(10);
        await Assert.That(tiles[0].MapTileY).IsEqualTo(21);
        await Assert.That(tiles[1].MapTileX).IsEqualTo(11);
        await Assert.That(tiles[1].MapTileY).IsEqualTo(21);
        await Assert.That(tiles[2].MapTileX).IsEqualTo(12);
        await Assert.That(tiles[2].MapTileY).IsEqualTo(21);
        await Assert.That(tiles[3].MapTileX).IsEqualTo(10);
        await Assert.That(tiles[3].MapTileY).IsEqualTo(20);
        await Assert.That(tiles[4].MapTileX).IsEqualTo(11);
        await Assert.That(tiles[4].MapTileY).IsEqualTo(20);
        await Assert.That(tiles[5].MapTileX).IsEqualTo(12);
        await Assert.That(tiles[5].MapTileY).IsEqualTo(20);
    }

    [Test]
    public async Task SelectionState_PointObjectSelection_ReportsSingleObject()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var selection = new EditorProjectMapSelectionState
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            Tile = new Location(7, 8),
            ObjectId = objectId,
        };

        await Assert.That(selection.HasTileSelection).IsTrue();
        await Assert.That(selection.HasAreaSelection).IsFalse();
        await Assert.That(selection.HasObjectSelection).IsTrue();
        await Assert.That(selection.HasMultipleObjectSelection).IsFalse();
        await Assert.That(selection.SelectedObjectCount).IsEqualTo(1);
        await Assert.That(selection.GetSelectedObjectIds().Count).IsEqualTo(1);
        await Assert.That(selection.GetSelectedObjectIds()[0]).IsEqualTo(objectId);
    }

    [Test]
    public async Task SelectionState_AreaSelection_UsesPersistedAreaObjectSet()
    {
        var primaryObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 11, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 22, Guid.NewGuid());
        var selection = new EditorProjectMapSelectionState
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            Tile = new Location(63, 63),
            ObjectId = primaryObjectId,
            Area = new EditorProjectMapAreaSelectionState
            {
                MinMapTileX = 63,
                MinMapTileY = 126,
                MaxMapTileX = 64,
                MaxMapTileY = 127,
                ObjectIds = [primaryObjectId, secondObjectId],
            },
        };

        await Assert.That(selection.HasTileSelection).IsTrue();
        await Assert.That(selection.HasAreaSelection).IsTrue();
        await Assert.That(selection.HasObjectSelection).IsTrue();
        await Assert.That(selection.HasMultipleObjectSelection).IsTrue();
        await Assert.That(selection.SelectedObjectCount).IsEqualTo(2);
        await Assert.That(selection.GetSelectedObjectIds().Count).IsEqualTo(2);
        await Assert.That(selection.GetSelectedObjectIds()[0]).IsEqualTo(primaryObjectId);
        await Assert.That(selection.GetSelectedObjectIds()[1]).IsEqualTo(secondObjectId);
    }

    [Test]
    public async Task SelectionState_WithoutObjects_ReturnsEmptyObjectSet()
    {
        var selection = new EditorProjectMapSelectionState
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            Tile = new Location(1, 2),
        };

        await Assert.That(selection.HasTileSelection).IsTrue();
        await Assert.That(selection.HasAreaSelection).IsFalse();
        await Assert.That(selection.HasObjectSelection).IsFalse();
        await Assert.That(selection.HasMultipleObjectSelection).IsFalse();
        await Assert.That(selection.SelectedObjectCount).IsEqualTo(0);
        await Assert.That(selection.GetSelectedObjectIds().Count).IsEqualTo(0);
    }
}
