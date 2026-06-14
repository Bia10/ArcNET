using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceTileArtCatalogBuilderTests
{
    [Test]
    public async Task Build_ParsesTileArtIdsFromTileNameCatalog(CancellationToken cancellationToken)
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "art", "tile"));

        MessageFormat.WriteToFile(
            new MesFile { Entries = [new MessageEntry(0, "grass")] },
            Path.Combine(gameDirectory, "data", "art", "tile", "tilename.mes")
        );
        var artFile = WorkspaceCatalogTestData.MakeArtFile();
        ArtFormat.WriteToFile(in artFile, Path.Combine(gameDirectory, "data", "art", "tile", "grassbse6a.art"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(
            gameDirectory,
            cancellationToken: cancellationToken
        );

        var entries = WorkspaceTileArtCatalogBuilder.Build(loadResult.GameData);

        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].DisplayName).IsEqualTo("grassbse6a");
        await Assert.That(entries[0].AssetPath).IsEqualTo("art/tile/grassbse6a.art");
        await Assert.That(entries[0].ArtId).IsEqualTo(new ArtId(0x000011C0u));
        await Assert.That(entries[0].ArtId.Type).IsEqualTo(ArtId.TypeCode.Tile);
    }
}
