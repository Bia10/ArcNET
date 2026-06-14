using System.Runtime.Versioning;
using ArcNET.Formats;

namespace ArcNET.Diagnostics.Windows.Tests;

[SupportedOSPlatform("windows")]
public sealed class RuntimeWorkspacePathHintResolverTests
{
    [Test]
    public async Task TryResolveModuleDirectoryByMapId_WhenExactlyOneModuleMatches_ReturnsThatModule()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            WriteMapList(
                Path.Combine(gameDirectory, "modules", "Arcanum", "Rules", "MapList.mes"),
                [new MessageEntry(5001, "BaseTown, 10, 20, Area: 1")]
            );
            WriteMapList(
                Path.Combine(gameDirectory, "modules", "co8", "Rules", "MapList.mes"),
                [new MessageEntry(6001, "Co8Town, 30, 40, Area: 2")]
            );
            WriteMapList(
                Path.Combine(gameDirectory, "modules", "Vendigroth", "Rules", "MapList.mes"),
                [new MessageEntry(7001, "VendigrothRuins, 50, 60, Area: 3")]
            );

            var resolved = RuntimeWorkspacePathHintResolver.TryResolveModuleDirectoryByMapId(
                gameDirectory,
                7001,
                out var moduleDirectory
            );

            await Assert.That(resolved).IsTrue();
            await Assert.That(moduleDirectory).IsEqualTo(Path.Combine(gameDirectory, "modules", "Vendigroth"));
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    [Test]
    public async Task TryResolveModuleDirectoryByMapId_WhenMultipleModulesMatch_ReturnsFalse()
    {
        var sandbox = Directory.CreateTempSubdirectory();
        try
        {
            var gameDirectory = Path.Combine(sandbox.FullName, "Arcanum");
            WriteMapList(
                Path.Combine(gameDirectory, "modules", "Arcanum", "Rules", "MapList.mes"),
                [new MessageEntry(5001, "BaseTown, 10, 20, Area: 1")]
            );
            WriteMapList(
                Path.Combine(gameDirectory, "modules", "co8", "Rules", "MapList.mes"),
                [new MessageEntry(6001, "SharedTown, 30, 40, Area: 2")]
            );
            WriteMapList(
                Path.Combine(gameDirectory, "modules", "Vendigroth", "Rules", "MapList.mes"),
                [new MessageEntry(6001, "SharedTownAgain, 50, 60, Area: 3")]
            );

            var resolved = RuntimeWorkspacePathHintResolver.TryResolveModuleDirectoryByMapId(
                gameDirectory,
                6001,
                out var moduleDirectory
            );

            await Assert.That(resolved).IsFalse();
            await Assert.That(moduleDirectory).IsEqualTo(string.Empty);
        }
        finally
        {
            sandbox.Delete(recursive: true);
        }
    }

    private static void WriteMapList(string path, IReadOnlyList<MessageEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        MessageFormat.WriteToFile(new MesFile { Entries = entries }, path);
    }
}
