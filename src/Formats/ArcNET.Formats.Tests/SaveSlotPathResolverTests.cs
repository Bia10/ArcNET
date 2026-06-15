namespace ArcNET.Formats.Tests;

public sealed class SaveSlotPathResolverTests
{
    [Test]
    public async Task ResolveFromFolder_WhenDecoratedGsiMatchesSlotStem_ReturnsDecoratedPath()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var decoratedPath = Path.Combine(directory.FullName, "Slot0001Hero.gsi");
            File.WriteAllBytes(decoratedPath, []);

            var paths = SaveSlotPathResolver.ResolveFromFolder(directory.FullName, "Slot0001");

            await Assert.That(paths.GsiPath).IsEqualTo(decoratedPath);
            await Assert.That(paths.TfaiPath).IsEqualTo(Path.Combine(directory.FullName, "Slot0001.tfai"));
            await Assert.That(paths.TfafPath).IsEqualTo(Path.Combine(directory.FullName, "Slot0001.tfaf"));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Test]
    public async Task ResolveFromFolder_WhenMultipleDecoratedGsiFilesMatch_Throws()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllBytes(Path.Combine(directory.FullName, "Slot0001Hero.gsi"), []);
            File.WriteAllBytes(Path.Combine(directory.FullName, "Slot0001Backup.gsi"), []);

            await Assert
                .That(() => SaveSlotPathResolver.ResolveFromFolder(directory.FullName, "Slot0001"))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
