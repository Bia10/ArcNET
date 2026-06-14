using ArcNET.Formats;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceContentLoaderTests
{
    [Test]
    public async Task HasModuleContent_WithLooseModuleDirectory_ReturnsTrue()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));

        var hasContent = WorkspaceContentLoader.HasModuleContent(gameDirectory, "co8");

        await Assert.That(hasContent).IsTrue();
    }

    [Test]
    public async Task HasModuleContent_WithPackedModuleArchiveOnly_ReturnsTrue()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        var archivePath = sandbox.CreateFile(Path.Combine("Arcanum", "modules", "co8.dat"));

        var hasContent = WorkspaceContentLoader.HasModuleContent(gameDirectory, "co8");

        await Assert.That(File.Exists(archivePath)).IsTrue();
        await Assert.That(hasContent).IsTrue();
    }

    [Test]
    public async Task HasModuleContent_WhenLooseDirectoryAndArchivesAreMissing_ReturnsFalse()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));

        var hasContent = WorkspaceContentLoader.HasModuleContent(gameDirectory, "co8");

        await Assert.That(hasContent).IsFalse();
    }

    [Test]
    public async Task LoadGameInstallAsync_ReturnsNullModuleContext()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "data", "mes"));
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1, "Base install")] },
            Path.Combine(gameDirectory, "data", "mes", "game.mes")
        );

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(gameDirectory);

        await Assert.That(loadResult.ModuleContext).IsNull();
    }

    [Test]
    public async Task LoadModuleAsync_ReturnsSharedModuleContext()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));
        var saveDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8", "Save"));
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8", "mes"));
        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(1, "Module override")] },
            Path.Combine(moduleDirectory, "mes", "game.mes")
        );

        var loadResult = await WorkspaceContentLoader.LoadModuleAsync(moduleDirectory);

        await Assert.That(loadResult.ModuleContext).IsNotNull();
        await Assert.That(loadResult.ModuleContext!.ModuleName).IsEqualTo("co8");
        await Assert.That(loadResult.ModuleContext.ModuleDirectory).IsEqualTo(moduleDirectory);
        await Assert.That(loadResult.ModuleContext.SaveDirectory).IsEqualTo(saveDirectory);
        await Assert.That(loadResult.ModuleContext.ArchivePaths).IsEquivalentTo(loadResult.ArchivePaths);
    }
}
