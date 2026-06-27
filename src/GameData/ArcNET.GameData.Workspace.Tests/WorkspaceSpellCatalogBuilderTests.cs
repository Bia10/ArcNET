namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceSpellCatalogBuilderTests
{
    [Test]
    public async Task Build_UsesOriginalSpellEnumOrder(CancellationToken cancellationToken)
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(
            gameDirectory,
            cancellationToken: cancellationToken
        );

        var entries = WorkspaceSpellCatalogBuilder.Build(loadResult.GameData);

        await Assert.That(entries.Count).IsEqualTo(80);
        await Assert.That(entries[4].Name).IsEqualTo("Teleportation");
        await Assert.That(entries[16].Name).IsEqualTo("Stone Throw");
        await Assert.That(entries[21].Name).IsEqualTo("Wall Of Fire");
        await Assert.That(entries[30].Name).IsEqualTo("Shield Of Protection");
        await Assert.That(entries[55].Name).IsEqualTo("Harm");
        await Assert.That(entries[72].Name).IsEqualTo("Guardian Ogre");
        await Assert.That(entries[72].CollegeName).IsEqualTo("Summoning");
        await Assert.That(entries[72].Level).IsEqualTo(3);
    }
}
