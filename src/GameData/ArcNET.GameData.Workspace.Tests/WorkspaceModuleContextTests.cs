namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceModuleContextTests
{
    [Test]
    public async Task Create_WhenModuleSaveDirectoryExists_ReturnsNormalizedContextAndCopiesArchivePaths()
    {
        using var sandbox = TemporaryDirectory.Create();
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));
        var saveDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8", "Save"));
        IReadOnlyList<string> archivePaths =
        [
            Path.Combine(sandbox.RootPath, "Arcanum", "modules", "co8.dat"),
            Path.Combine(sandbox.RootPath, "Arcanum", "modules", "co8.patch1"),
        ];

        var context = WorkspaceModuleContext.Create(Path.Combine(moduleDirectory, "."), archivePaths);

        await Assert.That(context.ModuleName).IsEqualTo("co8");
        await Assert.That(context.ModuleDirectory).IsEqualTo(moduleDirectory);
        await Assert.That(context.SaveDirectory).IsEqualTo(saveDirectory);
        await Assert.That(context.ArchivePaths).IsEquivalentTo(archivePaths);
        await Assert.That(ReferenceEquals(context.ArchivePaths, archivePaths)).IsFalse();
    }

    [Test]
    public async Task Create_WhenModuleSaveDirectoryIsMissing_ReturnsNullSaveDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));

        var context = WorkspaceModuleContext.Create(moduleDirectory);

        await Assert.That(context.ModuleName).IsEqualTo("co8");
        await Assert.That(context.ModuleDirectory).IsEqualTo(moduleDirectory);
        await Assert.That(context.SaveDirectory).IsNull();
        await Assert.That(context.ArchivePaths).IsEmpty();
    }
}
