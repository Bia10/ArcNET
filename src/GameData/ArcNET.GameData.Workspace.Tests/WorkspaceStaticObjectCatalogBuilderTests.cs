using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceStaticObjectCatalogBuilderTests
{
    [Test]
    public async Task Build_ProjectsMobAndSectorObjectsAgainstPrototypeCatalog(CancellationToken cancellationToken)
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "scenery"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "maps", "map01", "mobile"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "maps", "map01"));

        var prototype = WorkspaceCatalogTestData.MakePrototype(ObjectType.Scenery, 2001);
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "scenery", "002001 - Lamp.pro")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Lamp Post")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );

        var mobileObject = WorkspaceCatalogTestData.MakeMob(
            ObjectType.Scenery,
            2001,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            tileX: 5,
            tileY: 6
        );
        MobFormat.WriteToFile(
            in mobileObject,
            Path.Combine(gameDirectory, "data", "maps", "map01", "mobile", "lamp.mob")
        );

        var sectorObject = WorkspaceCatalogTestData.MakeMob(
            ObjectType.Scenery,
            2001,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            tileX: 7,
            tileY: 8
        );
        var sector = WorkspaceCatalogTestData.MakeSector(sectorObject);
        SectorFormat.WriteToFile(in sector, Path.Combine(gameDirectory, "data", "maps", "map01", "0.sec"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(
            gameDirectory,
            cancellationToken: cancellationToken
        );
        var prototypeEntries = WorkspacePrototypeCatalogBuilder.Build(loadResult.GameData);

        var entries = WorkspaceStaticObjectCatalogBuilder.Build(loadResult.GameData, prototypeEntries);

        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert
            .That(entries.Select(static entry => entry.SourceKindText))
            .IsEquivalentTo(["Mob asset", "Sector object"]);
        await Assert.That(entries.All(static entry => entry.DisplayName == "Lamp Post")).IsTrue();
        await Assert.That(entries.All(static entry => entry.PrototypeText == "Lamp Post [2001]")).IsTrue();
        await Assert
            .That(entries.Select(static entry => entry.LocationText))
            .IsEquivalentTo(["Tile (5, 6)", "Tile (7, 8)"]);
        await Assert.That(entries.Any(static entry => entry.SourceAssetPath == "maps/map01/mobile/lamp.mob")).IsTrue();
        await Assert.That(entries.Any(static entry => entry.SourceAssetPath == "maps/map01/0.sec")).IsTrue();
    }
}
