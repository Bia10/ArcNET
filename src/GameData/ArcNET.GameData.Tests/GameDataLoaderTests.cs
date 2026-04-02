using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.GameData.Tests;

public class GameDataLoaderTests
{
    [Test]
    public void DiscoverFiles_NonExistentDir_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() => GameDataLoader.DiscoverFiles("/nonexistent/path/xyz"));
    }

    [Test]
    public async Task DiscoverFiles_EmptyDir_ReturnsEmptyLists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = GameDataLoader.DiscoverFiles(tempDir);
            await Assert.That(result[FileFormat.Sector].Count).IsEqualTo(0);
            await Assert.That(result[FileFormat.Message].Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
