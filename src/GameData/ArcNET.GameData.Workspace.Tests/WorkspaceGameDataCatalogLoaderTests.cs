using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceGameDataCatalogLoaderTests
{
    [Test]
    public async Task LoadFromModulePath_ComposesPrototypeWorldTileAndStaticCatalogs(
        CancellationToken cancellationToken
    )
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        var executablePath = sandbox.CreateFile(Path.Combine("Arcanum", "Arcanum.exe"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "scenery"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "Rules"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "art", "tile"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "maps", "map01", "mobile"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "maps", "map01"));

        var prototype = WorkspaceCatalogTestData.MakePrototype(
            ObjectType.Scenery,
            2001,
            ObjectPropertyFactory.ForInt32(ObjectField.CurrentAid, unchecked((int)0x00000123u))
        );
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "scenery", "002001 - Lamp.pro")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Lamp Post")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );

        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(21, "62182,65662,0,0/Tarant/The biggest city in Arcanum.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamearea.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(21, "Tarant [w:1]")] },
            Path.Combine(gameDirectory, "data", "Rules", "TownMap.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(5015, "Tarant-City Hall, 96, 93, Area: 21")] },
            Path.Combine(gameDirectory, "data", "Rules", "MapList.mes")
        );

        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(0, "grass")] },
            Path.Combine(gameDirectory, "data", "art", "tile", "tilename.mes")
        );
        var artFile = WorkspaceCatalogTestData.MakeArtFile();
        ArtFormat.WriteToFile(in artFile, Path.Combine(gameDirectory, "data", "art", "tile", "grassbse6a.art"));

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

        await WorkspaceContentLoader.LoadGameInstallAsync(gameDirectory, cancellationToken: cancellationToken);

        var catalog = await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(executablePath);

        await Assert.That(catalog.PrototypeEntries.Count).IsEqualTo(1);
        await Assert.That(catalog.PrototypeEntries[0].DisplayName).IsEqualTo("Lamp Post");
        await Assert.That(catalog.PrototypeEntries[0].CurrentArtId).IsEqualTo(new ArtId(0x00000123u));

        await Assert.That(catalog.WorldAreaCatalog.Areas.Count).IsEqualTo(1);
        await Assert.That(catalog.WorldAreaCatalog.Areas[0].DisplayName).IsEqualTo("Tarant");
        await Assert.That(catalog.WorldAreaCatalog.Areas[0].MapEntries[0].MapName).IsEqualTo("Tarant-City Hall");

        await Assert.That(catalog.TileArtEntries.Count).IsEqualTo(1);
        await Assert.That(catalog.TileArtEntries[0].ArtId).IsEqualTo(new ArtId(0x000011C0u));

        await Assert.That(catalog.StaticObjectEntries.Count).IsEqualTo(2);
        await Assert.That(catalog.StaticObjectEntries.All(static entry => entry.DisplayName == "Lamp Post")).IsTrue();
    }

    [Test]
    public async Task LoadFromModulePath_WhenModuleDirectoryIsPassed_PrefersModuleOverlay(
        CancellationToken cancellationToken
    )
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "scenery"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));

        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "Vendigroth"));
        Directory.CreateDirectory(Path.Combine(moduleDirectory, "mes"));

        var prototype = WorkspaceCatalogTestData.MakePrototype(ObjectType.Scenery, 2001);
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "scenery", "002001 - Lamp.pro")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Base Lamp")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Module Lamp")] },
            Path.Combine(moduleDirectory, "mes", "description.mes")
        );

        await WorkspaceContentLoader.LoadModuleAsync(moduleDirectory, cancellationToken: cancellationToken);

        var catalog = await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(moduleDirectory);

        await Assert.That(catalog.PrototypeEntries.Count).IsEqualTo(1);
        await Assert.That(catalog.PrototypeEntries[0].DisplayName).IsEqualTo("Module Lamp");
    }

    [Test]
    public async Task LoadFromGameDirectory_WhenForceReloadIsRequested_RebuildsTheCachedCatalog(
        CancellationToken cancellationToken
    )
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "scenery"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));

        var prototype = WorkspaceCatalogTestData.MakePrototype(ObjectType.Scenery, 2001);
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "scenery", "002001 - Lamp.pro")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Base Lamp")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );

        await WorkspaceContentLoader.LoadGameInstallAsync(gameDirectory, cancellationToken: cancellationToken);

        var initialCatalog = await WorkspaceGameDataCatalogLoader.LoadFromGameDirectoryAsync(
            gameDirectory,
            forceReload: true
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Reloaded Lamp")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );

        var cachedCatalog = await WorkspaceGameDataCatalogLoader.LoadFromGameDirectoryAsync(gameDirectory);
        var reloadedCatalog = await WorkspaceGameDataCatalogLoader.LoadFromGameDirectoryAsync(
            gameDirectory,
            forceReload: true
        );

        await Assert.That(initialCatalog.PrototypeEntries[0].DisplayName).IsEqualTo("Base Lamp");
        await Assert.That(cachedCatalog.PrototypeEntries[0].DisplayName).IsEqualTo("Base Lamp");
        await Assert.That(reloadedCatalog.PrototypeEntries[0].DisplayName).IsEqualTo("Reloaded Lamp");
    }

    [Test]
    public async Task LoadFromGameDirectory_WhenTheInitialLoadFails_RetriesInsteadOfKeepingAFaultedCache()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = Path.Combine(sandbox.RootPath, "Arcanum");

        await Assert
            .That(async () =>
                await WorkspaceGameDataCatalogLoader.LoadFromGameDirectoryAsync(gameDirectory, forceReload: true)
            )
            .Throws<DirectoryNotFoundException>();

        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "proto", "scenery"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        var prototype = WorkspaceCatalogTestData.MakePrototype(ObjectType.Scenery, 2001);
        ProtoFormat.WriteToFile(
            in prototype,
            Path.Combine(gameDirectory, "data", "proto", "scenery", "002001 - Lamp.pro")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(2001, "Recovered Lamp")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );

        var recoveredCatalog = await WorkspaceGameDataCatalogLoader.LoadFromGameDirectoryAsync(gameDirectory);

        await Assert.That(recoveredCatalog.PrototypeEntries.Count).IsEqualTo(1);
        await Assert.That(recoveredCatalog.PrototypeEntries[0].DisplayName).IsEqualTo("Recovered Lamp");
    }
}
