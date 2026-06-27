using ArcNET.Formats;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceJumpPointCatalogBuilderTests
{
    [Test]
    public async Task Build_ProjectsLoadedMapJumpPoints(CancellationToken cancellationToken)
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "maps", "map01"));

        var jumpFile = new JmpFile
        {
            Jumps =
            [
                new JumpEntry
                {
                    Flags = 3,
                    SourceLoc = PackLocation(12, 34),
                    DestinationMapId = 5022,
                    DestinationLoc = PackLocation(56, 78),
                },
            ],
        };
        JmpFormat.WriteToFile(in jumpFile, Path.Combine(gameDirectory, "data", "maps", "map01", "map.jmp"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(
            gameDirectory,
            cancellationToken: cancellationToken
        );

        var entries = WorkspaceJumpPointCatalogBuilder.Build(loadResult.GameData);

        await Assert.That(entries.Count).IsEqualTo(1);
        var entry = entries[0];
        await Assert.That(entry.SourceAssetPath).IsEqualTo("maps/map01/map.jmp");
        await Assert.That(entry.SourceMapName).IsEqualTo("map01");
        await Assert.That(entry.Flags).IsEqualTo(3u);
        await Assert.That(entry.SourcePackedLocation).IsEqualTo(PackLocation(12, 34));
        await Assert.That(entry.SourceTileX).IsEqualTo(12);
        await Assert.That(entry.SourceTileY).IsEqualTo(34);
        await Assert.That(entry.DestinationMapId).IsEqualTo(5022);
        await Assert.That(entry.DestinationPackedLocation).IsEqualTo(PackLocation(56, 78));
        await Assert.That(entry.DestinationTileX).IsEqualTo(56);
        await Assert.That(entry.DestinationTileY).IsEqualTo(78);
    }

    private static long PackLocation(int x, int y) => (long)(uint)x | ((long)(uint)y << 32);
}
