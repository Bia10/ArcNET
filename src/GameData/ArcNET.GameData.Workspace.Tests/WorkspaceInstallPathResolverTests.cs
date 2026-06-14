namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceInstallPathResolverTests
{
    [Test]
    public async Task ResolveGameInstallDirectory_WhenPathIsExecutable_ReturnsContainingInstallDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "data"));
        var executablePath = sandbox.CreateFile(Path.Combine("Arcanum", "Arcanum.exe"));

        var resolved = WorkspaceInstallPathResolver.ResolveGameInstallDirectory(executablePath);

        await Assert.That(resolved).IsEqualTo(gameDirectory);
    }

    [Test]
    public async Task ResolveGameInstallDirectory_WhenPathIsModuleDirectory_ReturnsOwningInstallDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));

        var resolved = WorkspaceInstallPathResolver.ResolveGameInstallDirectory(
            Path.Combine(gameDirectory, "modules", "co8")
        );

        await Assert.That(resolved).IsEqualTo(gameDirectory);
    }

    [Test]
    public async Task ResolveGameInstallDirectory_WhenPathIsModulesDirectory_ReturnsOwningInstallDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        var modulesDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));

        var resolved = WorkspaceInstallPathResolver.ResolveGameInstallDirectory(modulesDirectory);

        await Assert.That(resolved).IsEqualTo(gameDirectory);
    }

    [Test]
    public async Task ResolveGameInstallDirectory_WhenWrapperContainsOneInstall_ReturnsNestedInstallDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var wrapperDirectory = sandbox.CreateDirectory("Wrapper");
        var gameDirectory = sandbox.CreateDirectory(Path.Combine("Wrapper", "Arcanum"));
        sandbox.CreateDirectory(Path.Combine("Wrapper", "Arcanum", "data"));

        var resolved = WorkspaceInstallPathResolver.ResolveGameInstallDirectory(wrapperDirectory);

        await Assert.That(resolved).IsEqualTo(gameDirectory);
    }

    [Test]
    public async Task ResolveGameInstallDirectory_WhenPathDoesNotExist_ReturnsNormalizedPath()
    {
        using var sandbox = TemporaryDirectory.Create();
        var missingPath = Path.Combine(sandbox.RootPath, "missing", "Arcanum");

        var resolved = WorkspaceInstallPathResolver.ResolveGameInstallDirectory(missingPath);

        await Assert.That(resolved).IsEqualTo(Path.GetFullPath(missingPath));
    }

    [Test]
    public async Task ResolveModuleDirectory_NormalizesInstallAnchorBeforeCombiningModuleName()
    {
        using var sandbox = TemporaryDirectory.Create();
        sandbox.CreateDirectory("wrapper");
        var gameDirectory = sandbox.CreateDirectory(Path.Combine("wrapper", "Arcanum"));
        sandbox.CreateDirectory(Path.Combine("wrapper", "Arcanum", "data"));

        var resolved = WorkspaceInstallPathResolver.ResolveModuleDirectory(
            Path.Combine(sandbox.RootPath, "wrapper"),
            "co8"
        );

        await Assert.That(resolved).IsEqualTo(Path.Combine(gameDirectory, "modules", "co8"));
    }

    [Test]
    public async Task ResolveWorkspaceDirectory_WhenPathIsModuleSaveDirectory_ReturnsModuleDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        sandbox.CreateDirectory(Path.Combine("Arcanum", "data"));
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));
        var saveDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8", "Save"));

        var resolved = WorkspaceInstallPathResolver.ResolveWorkspaceDirectory(saveDirectory);

        await Assert.That(resolved).IsEqualTo(moduleDirectory);
    }

    [Test]
    public async Task ResolveWorkspaceDirectory_WhenPathIsModuleArchive_ReturnsModuleDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));
        var moduleArchivePath = sandbox.CreateFile(Path.Combine("Arcanum", "modules", "co8.dat"));

        var resolved = WorkspaceInstallPathResolver.ResolveWorkspaceDirectory(moduleArchivePath);

        await Assert.That(resolved).IsEqualTo(Path.Combine(gameDirectory, "modules", "co8"));
    }

    [Test]
    public async Task ResolveWorkspaceDirectory_WhenPathIsFileUnderModuleDirectory_ReturnsModuleDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        sandbox.CreateDirectory(Path.Combine("Arcanum", "data"));
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));
        var moduleFilePath = sandbox.CreateFile(Path.Combine("Arcanum", "modules", "co8", "module.dll"));

        var resolved = WorkspaceInstallPathResolver.ResolveWorkspaceDirectory(moduleFilePath);

        await Assert.That(resolved).IsEqualTo(moduleDirectory);
    }

    [Test]
    public async Task TryResolveSingleModuleDirectory_WhenInstallHasOneArchiveBackedModule_ReturnsThatModuleDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));
        var expectedModuleDirectory = Path.Combine(gameDirectory, "modules", "Arcanum");
        sandbox.CreateFile(Path.Combine("Arcanum", "modules", "Arcanum.dat"));

        var resolved = WorkspaceInstallPathResolver.TryResolveSingleModuleDirectory(
            gameDirectory,
            out var moduleDirectory
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(moduleDirectory).IsEqualTo(expectedModuleDirectory);
    }

    [Test]
    public async Task TryResolveSingleModuleDirectory_WhenInstallHasMultipleModules_ReturnsFalse()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));
        sandbox.CreateFile(Path.Combine("Arcanum", "modules", "Arcanum.dat"));
        sandbox.CreateFile(Path.Combine("Arcanum", "modules", "co8.dat"));

        var resolved = WorkspaceInstallPathResolver.TryResolveSingleModuleDirectory(
            gameDirectory,
            out var moduleDirectory
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(moduleDirectory).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ResolveOwningGameDirectoryFromModuleDirectory_WhenPathIsModuleDirectory_ReturnsOwningInstallDirectory()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "modules", "co8"));

        var resolved = WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(moduleDirectory);

        await Assert.That(resolved).IsEqualTo(gameDirectory);
    }

    [Test]
    public async Task ResolveOwningGameDirectoryFromModuleDirectory_WhenPathIsNotUnderModulesDirectory_ThrowsArgumentException()
    {
        using var sandbox = TemporaryDirectory.Create();
        var moduleDirectory = sandbox.CreateDirectory(Path.Combine("Arcanum", "co8"));

        await Assert
            .That(() => WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(moduleDirectory))
            .Throws<ArgumentException>();
    }
}
