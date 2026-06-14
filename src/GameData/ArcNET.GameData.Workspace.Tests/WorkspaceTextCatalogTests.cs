using ArcNET.Formats;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceTextCatalogTests
{
    [Test]
    public async Task LoadFromGameDirectory_LoadsBackgroundQuestRumorAndNamedBanks()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "rules"));

        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1010, "Gifted background")] },
            Path.Combine(gameDirectory, "data", "mes", "gameback.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(20, "1010")] },
            Path.Combine(gameDirectory, "data", "rules", "backgrnd.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Find the ring. Then return it to Bates.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamequestlog.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Find shiny ring. Bring it back.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamequestlogdumb.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(20000, "Gnomes whisper by the bridge.")] },
            Path.Combine(gameDirectory, "data", "mes", "game_rd_npc_m2m.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(20000, "Bridge whispers.")] },
            Path.Combine(gameDirectory, "data", "mes", "game_rd_npc_m2m_dumb.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(7, "Hero of Tarant")] },
            Path.Combine(gameDirectory, "data", "mes", "gamereplog.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(10, "Blessed")] },
            Path.Combine(gameDirectory, "data", "mes", "gamebless.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(20, "Cursed")] },
            Path.Combine(gameDirectory, "data", "mes", "gamecurse.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(55, "Half ogre bruiser")] },
            Path.Combine(gameDirectory, "data", "mes", "description.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(4, "Skeleton Key")] },
            Path.Combine(gameDirectory, "data", "mes", "gamekey.mes")
        );

        var catalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(gameDirectory);
        var background = catalog.ResolveBackground(1010);
        var quest = catalog.ResolveQuest(1001);
        var rumor = catalog.EnumerateRumors().Single();

        await Assert.That(catalog.AvailabilityNote).IsNull();
        await Assert.That(background.BackgroundId).IsEqualTo(2);
        await Assert.That(background.Name).IsEqualTo("Gifted background");
        await Assert.That(background.Body).IsEqualTo("Gifted background");
        await Assert.That(quest.SummaryLabel).IsEqualTo("Find the ring");
        await Assert.That(quest.Description).IsEqualTo("Find the ring. Then return it to Bates.");
        await Assert.That(quest.DumbDescription).IsEqualTo("Find shiny ring. Bring it back.");
        await Assert.That(rumor.RumorId).IsEqualTo(1000);
        await Assert.That(rumor.NormalText).IsEqualTo("Gnomes whisper by the bridge.");
        await Assert.That(rumor.DumbText).IsEqualTo("Bridge whispers.");
        await Assert.That(catalog.ResolveReputationName(7)).IsEqualTo("Hero of Tarant");
        await Assert.That(catalog.ResolveBlessingName(1)).IsEqualTo("Blessed");
        await Assert.That(catalog.ResolveCurseName(2)).IsEqualTo("Cursed");
        await Assert.That(catalog.ResolveDescription(55)).IsEqualTo("Half ogre bruiser");
        await Assert.That(catalog.ResolveKeyName(4)).IsEqualTo("Skeleton Key");
    }

    [Test]
    public async Task LoadFromGameDirectory_ReturnsUnavailableCatalogWhenInstallCannotLoad()
    {
        using var sandbox = TemporaryDirectory.Create();
        var missingGameDirectory = Path.Combine(sandbox.RootPath, "Missing");

        var catalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(missingGameDirectory);

        await Assert.That(catalog.AvailabilityNote).Contains("Game text catalog unavailable");
        await Assert.That(catalog.EnumerateBackgrounds()).IsEmpty();
        await Assert.That(catalog.EnumerateQuests()).IsEmpty();
        await Assert.That(catalog.EnumerateRumors()).IsEmpty();
    }

    [Test]
    public async Task LoadFromModulePath_WhenModuleDirectoryIsPassed_PrefersModuleTextOverrides()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "Vendigroth"));
        Directory.CreateDirectory(Path.Combine(moduleDirectory, "mes"));

        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Base quest. Return it to Bates.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamequestlog.mes")
        );
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Module quest. Return it to Bates.")] },
            Path.Combine(moduleDirectory, "mes", "gamequestlog.mes")
        );

        var catalog = await WorkspaceTextCatalog.LoadFromModulePathAsync(moduleDirectory, forceReload: true);

        await Assert.That(catalog.ResolveQuest(1001).Description).IsEqualTo("Module quest. Return it to Bates.");
    }

    [Test]
    public async Task LoadFromGameDirectory_WhenForceReloadIsRequested_RebuildsTheCachedCatalog()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "rules"));

        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Base quest. Return it to Bates.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamequestlog.mes")
        );

        var initialCatalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(gameDirectory, forceReload: true);
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Reloaded quest. Return it to Bates.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamequestlog.mes")
        );

        var cachedCatalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(gameDirectory);
        var reloadedCatalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(gameDirectory, forceReload: true);

        await Assert.That(initialCatalog.ResolveQuest(1001).Description).IsEqualTo("Base quest. Return it to Bates.");
        await Assert.That(cachedCatalog.ResolveQuest(1001).Description).IsEqualTo("Base quest. Return it to Bates.");
        await Assert
            .That(reloadedCatalog.ResolveQuest(1001).Description)
            .IsEqualTo("Reloaded quest. Return it to Bates.");
    }

    [Test]
    public async Task LoadFromGameDirectory_WhenAnUnavailableCatalogWasReturned_RetriesOnTheNextCall()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = Path.Combine(sandbox.RootPath, "Arcanum");

        var unavailableCatalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(
            gameDirectory,
            forceReload: true
        );

        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "rules"));
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1001, "Recovered quest. Return it to Bates.")] },
            Path.Combine(gameDirectory, "data", "mes", "gamequestlog.mes")
        );

        var recoveredCatalog = await WorkspaceTextCatalog.LoadFromGameDirectoryAsync(gameDirectory);

        await Assert.That(unavailableCatalog.AvailabilityNote).Contains("Game text catalog unavailable");
        await Assert.That(recoveredCatalog.AvailabilityNote).IsNull();
        await Assert
            .That(recoveredCatalog.ResolveQuest(1001).Description)
            .IsEqualTo("Recovered quest. Return it to Bates.");
    }
}
