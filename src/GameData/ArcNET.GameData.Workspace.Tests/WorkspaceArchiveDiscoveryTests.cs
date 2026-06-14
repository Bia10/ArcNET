using ArcNET.Archive;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceArchiveDiscoveryTests
{
    [Test]
    public async Task DiscoverGameInstallArchives_ReturnsArchivesInOverlayOrder_AndCapturesSkippedCandidates()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));
        var invalidArchivePath = sandbox.CreateFile(Path.Combine("Arcanum", "broken.dat"), [0x00, 0x01, 0x02]);
        var rootArchivePath = Path.Combine(gameDirectory, "arcanum1.dat");
        var moduleArchivePath = Path.Combine(gameDirectory, "modules", "Arcanum.dat");
        var patchArchivePath = Path.Combine(gameDirectory, "modules", "Arcanum.PATCH0");
        await WriteDatAsync(rootArchivePath, new Dictionary<string, byte[]> { ["mes\\root.mes"] = [0x01] });
        await WriteDatAsync(moduleArchivePath, new Dictionary<string, byte[]> { ["mes\\module.mes"] = [0x02] });
        await WriteDatAsync(patchArchivePath, new Dictionary<string, byte[]> { ["mes\\patch.mes"] = [0x03] });

        var discovery = WorkspaceArchiveDiscovery.DiscoverGameInstallArchives(gameDirectory);

        await Assert.That(discovery.ArchivePaths.Count).IsEqualTo(3);
        await Assert.That(discovery.ArchivePaths[0]).IsEqualTo(rootArchivePath);
        await Assert.That(discovery.ArchivePaths[1]).IsEqualTo(moduleArchivePath);
        await Assert.That(discovery.ArchivePaths[2]).IsEqualTo(patchArchivePath);
        await Assert.That(discovery.SkippedArchiveCandidates.Count).IsEqualTo(1);
        await Assert.That(discovery.SkippedArchiveCandidates[0].Path).IsEqualTo(invalidArchivePath);
    }

    [Test]
    public async Task DiscoverBaseInstallArchives_ReturnsOnlyRootDatArchives()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));
        var rootArchivePath = Path.Combine(gameDirectory, "arcanum1.dat");
        var moduleArchivePath = Path.Combine(gameDirectory, "modules", "Arcanum.dat");
        await WriteDatAsync(rootArchivePath, new Dictionary<string, byte[]> { ["mes\\root.mes"] = [0x01] });
        await WriteDatAsync(moduleArchivePath, new Dictionary<string, byte[]> { ["mes\\module.mes"] = [0x02] });

        var discovery = WorkspaceArchiveDiscovery.DiscoverBaseInstallArchives(gameDirectory);

        await Assert.That(discovery.ArchivePaths.Count).IsEqualTo(1);
        await Assert.That(discovery.ArchivePaths[0]).IsEqualTo(rootArchivePath);
        await Assert.That(discovery.SkippedArchiveCandidates.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverModuleArchives_ReturnsSiblingModuleArchives_WhenModuleDirectoryIsMissing()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        sandbox.CreateDirectory(Path.Combine("Arcanum", "modules"));
        var moduleDirectory = Path.Combine(gameDirectory, "modules", "co8");
        var moduleArchivePath = Path.Combine(gameDirectory, "modules", "co8.dat");
        var patchArchivePath = Path.Combine(gameDirectory, "modules", "co8.PATCH1");
        var unrelatedArchivePath = Path.Combine(gameDirectory, "modules", "Arcanum.dat");
        await WriteDatAsync(moduleArchivePath, new Dictionary<string, byte[]> { ["mes\\module.mes"] = [0x01] });
        await WriteDatAsync(patchArchivePath, new Dictionary<string, byte[]> { ["mes\\patch.mes"] = [0x02] });
        await WriteDatAsync(unrelatedArchivePath, new Dictionary<string, byte[]> { ["mes\\base.mes"] = [0x03] });

        var discovery = WorkspaceArchiveDiscovery.DiscoverModuleArchives(moduleDirectory);

        await Assert.That(discovery.ArchivePaths.Count).IsEqualTo(2);
        await Assert.That(discovery.ArchivePaths[0]).IsEqualTo(moduleArchivePath);
        await Assert.That(discovery.ArchivePaths[1]).IsEqualTo(patchArchivePath);
    }

    private static async Task WriteDatAsync(string archivePath, IReadOnlyDictionary<string, byte[]> entries)
    {
        using var sandbox = TemporaryDirectory.Create();
        foreach (var (virtualPath, bytes) in entries)
            sandbox.CreateFile(virtualPath, bytes);

        await DatPacker.PackAsync(sandbox.RootPath, archivePath);
    }
}
