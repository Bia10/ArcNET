using ArcNET.Formats;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceWorldAreaCatalogBuilderTests
{
    [Test]
    public async Task Build_JoinsGameAreaTownMapAndMapListMetadata()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "Rules"));

        var gameAreaFile = new MesFile
        {
            Entries =
            [
                new MessageEntry(0, "0, 0, 0, 0 /A Place Unimportant/This is the unknown area."),
                new MessageEntry(21, "62182,65662, 0, 0 /Tarant/The biggest, most industrial city in Arcanum."),
                new MessageEntry(22, "19704,37452, 0, 0 /Vendigroth Ruins/The remnants of an ancient city./Radius:1"),
            ],
        };
        MessageFormat.WriteToFile(in gameAreaFile, Path.Combine(gameDirectory, "data", "mes", "gamearea.mes"));

        var townMapFile = new MesFile
        {
            Entries =
            [
                new MessageEntry(21, "Tarant [w:1]"),
                new MessageEntry(22, "Vendigroth Ruins"),
                new MessageEntry(30, "Tarant Sewers lev 01"),
            ],
        };
        MessageFormat.WriteToFile(in townMapFile, Path.Combine(gameDirectory, "data", "Rules", "TownMap.mes"));

        var mapListFile = new MesFile
        {
            Entries =
            [
                new MessageEntry(5000, "Arcanum1-024-fixed, 92958, 82592, Type: START_MAP"),
                new MessageEntry(5015, "Tarant Sewers-01, 91, 98, WorldMap: 0, Area: 21"),
                new MessageEntry(5022, "Tarant-City Hall Downstairs, 96, 93, Area: 21"),
            ],
        };
        MessageFormat.WriteToFile(in mapListFile, Path.Combine(gameDirectory, "data", "Rules", "MapList.mes"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(gameDirectory);

        var catalog = WorkspaceWorldAreaCatalogBuilder.Build(loadResult.GameData);

        await Assert.That(catalog.WorldSceneMapName).IsEqualTo("Arcanum1-024-fixed");
        await Assert.That(catalog.Areas.Count).IsEqualTo(2);

        var tarant = catalog.FindArea(21);
        await Assert.That(tarant).IsNotNull();
        await Assert.That(tarant!.DisplayName).IsEqualTo("Tarant");
        await Assert.That(tarant.WorldX).IsEqualTo(62182);
        await Assert.That(tarant.WorldY).IsEqualTo(65662);
        await Assert.That(tarant.IsWorldMapVisible).IsTrue();
        await Assert.That(tarant.MapEntries.Count).IsEqualTo(2);
        await Assert.That(tarant.MapEntries[0].MapName).IsEqualTo("Tarant Sewers-01");
        await Assert.That(tarant.MapEntries[1].MapName).IsEqualTo("Tarant-City Hall Downstairs");

        var vendigroth = catalog.FindArea(22);
        await Assert.That(vendigroth).IsNotNull();
        await Assert.That(vendigroth!.Radius).IsEqualTo(1);
        await Assert.That(vendigroth.IsWorldMapVisible).IsFalse();

        var tarantAreaFromMap = catalog.FindAreaForMap("Tarant Sewers-01");
        await Assert.That(tarantAreaFromMap).IsNotNull();
        await Assert.That(tarantAreaFromMap!.AreaId).IsEqualTo(21);
    }
}
