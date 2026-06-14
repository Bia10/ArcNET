using ArcNET.Formats;

namespace ArcNET.GameData.Workspace.Tests;

public sealed class WorkspaceMessageLookupTests
{
    [Test]
    public async Task FindMessageFile_WhenGameInstallLoadsMessages_ReturnsNormalizedLookupResult()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));

        var messageFile = new MesFile { Entries = [new MessageEntry(42, "The bridge is out.")] };
        MessageFormat.WriteToFile(in messageFile, Path.Combine(gameDirectory, "data", "mes", "gamequestlog.mes"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(gameDirectory);

        var loadedFile = WorkspaceMessageLookup.FindMessageFile(loadResult.GameData, @"mes\gamequestlog.mes");

        await Assert.That(loadedFile).IsNotNull();
        await Assert.That(loadedFile!.Entries.Count).IsEqualTo(1);
        await Assert.That(loadedFile.Entries[0].Index).IsEqualTo(42);
        await Assert.That(loadedFile.Entries[0].Text).IsEqualTo("The bridge is out.");
    }

    [Test]
    public async Task FindMessageFile_WhenAssetIsMissing_ReturnsNull()
    {
        using var sandbox = TemporaryDirectory.Create();
        var gameDirectory = sandbox.CreateDirectory("Arcanum");
        Directory.CreateDirectory(Path.Combine(gameDirectory, "data", "mes"));

        var loadResult = await WorkspaceContentLoader.LoadGameInstallAsync(gameDirectory);

        var loadedFile = WorkspaceMessageLookup.FindMessageFile(loadResult.GameData, "mes/missing.mes");

        await Assert.That(loadedFile).IsNull();
    }
}
