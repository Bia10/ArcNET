using System.Text;
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

    [Test]
    public async Task DiscoverFiles_MesFile_DetectedAsMessage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "game.mes"), "{1}{Hello}");
            var result = GameDataLoader.DiscoverFiles(tempDir);
            await Assert.That(result[FileFormat.Message].Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task DiscoverFiles_FacWalkFile_DetectedAsFacadeWalk()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tempDir, "facwalk.wall_001"), []);
            var result = GameDataLoader.DiscoverFiles(tempDir);
            await Assert.That(result[FileFormat.FacadeWalk].Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadMessages_ReturnsCorrectIndexToTextMap()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "game.mes"), "{100}{Hello}\n{200}{World}\n");
            var result = GameDataLoader.LoadMessages(tempDir);

            await Assert.That(result.ContainsKey(100)).IsTrue();
            await Assert.That(result[100]).IsEqualTo("Hello");
            await Assert.That(result.ContainsKey(200)).IsTrue();
            await Assert.That(result[200]).IsEqualTo("World");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task LoadMessages_NonExistentDir_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() => GameDataLoader.LoadMessages("/nonexistent/xyz"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task LoadFromMemoryAsync_MesBytes_PopulatesMessages()
    {
        var bytes = Encoding.UTF8.GetBytes("{10}{Alpha}\n{20}{Beta}\n");
        var blobs = new Dictionary<string, ReadOnlyMemory<byte>> { ["game.mes"] = bytes };

        var store = await GameDataLoader.LoadFromMemoryAsync(blobs);

        await Assert.That(store.Messages.Count).IsEqualTo(2);
        await Assert.That(store.Messages[0].Index).IsEqualTo(10);
        await Assert.That(store.Messages[0].Text).IsEqualTo("Alpha");
        await Assert.That(store.Messages[1].Index).IsEqualTo(20);
        await Assert.That(store.Messages[1].Text).IsEqualTo("Beta");
    }

    [Test]
    public async Task LoadFromMemoryAsync_EmptyDict_ReturnsEmptyStore()
    {
        var store = await GameDataLoader.LoadFromMemoryAsync(new Dictionary<string, ReadOnlyMemory<byte>>());

        await Assert.That(store.Messages.Count).IsEqualTo(0);
        await Assert.That(store.Objects.Count).IsEqualTo(0);
    }

    [Test]
    public async Task LoadFromMemoryAsync_NullDict_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => GameDataLoader.LoadFromMemoryAsync(null!));
    }

    [Test]
    public async Task LoadFromMemoryAsync_CancelledToken_Throws()
    {
        var bytes = Encoding.UTF8.GetBytes("{1}{x}\n");
        var blobs = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["a.mes"] = bytes,
            ["b.mes"] = bytes,
            ["c.mes"] = bytes,
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            GameDataLoader.LoadFromMemoryAsync(blobs, ct: cts.Token)
        );
    }

    [Test]
    public async Task LoadFromMemoryAsync_Progress_ReachesOne()
    {
        var bytes = Encoding.UTF8.GetBytes("{1}{x}\n");
        var blobs = new Dictionary<string, ReadOnlyMemory<byte>> { ["game.mes"] = bytes };

        float lastProgress = 0f;
        var reached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var progress = new Progress<float>(p =>
        {
            lastProgress = p;
            if (p >= 1f)
                reached.TrySetResult(true);
        });

        await GameDataLoader.LoadFromMemoryAsync(blobs, progress: progress);
        await reached.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.That(lastProgress).IsGreaterThanOrEqualTo(1f);
    }

    [Test]
    public async Task LoadFromDirectoryAsync_NonExistentDir_Throws()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            GameDataLoader.LoadFromDirectoryAsync("/nonexistent/xyz")
        );
    }

    [Test]
    public async Task LoadFromDirectoryAsync_MesFile_PopulatesMessages()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "items.mes"), "{500}{Sword}\n");
            var store = await GameDataLoader.LoadFromDirectoryAsync(tempDir);

            await Assert.That(store.Messages.Count).IsEqualTo(1);
            await Assert.That(store.Messages[0].Index).IsEqualTo(500);
            await Assert.That(store.Messages[0].Text).IsEqualTo("Sword");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── G5: duplicate index detection ─────────────────────────────────────

    [Test]
    public async Task LoadMessages_DuplicateIndex_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Two .mes files, both defining index 100
            File.WriteAllText(Path.Combine(tempDir, "game.mes"), "{100}{Hello}\n");
            File.WriteAllText(Path.Combine(tempDir, "items.mes"), "{100}{World}\n");

            Assert.Throws<InvalidOperationException>(() => GameDataLoader.LoadMessages(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }

        await Task.CompletedTask;
    }

    // ── G4: origin tracking via MessagesBySource ────────────────────────────

    [Test]
    public async Task LoadFromMemoryAsync_MessagesBySource_ContainsBothKeys()
    {
        var blobs = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["game.mes"] = Encoding.UTF8.GetBytes("{10}{Alpha}\n"),
            ["items.mes"] = Encoding.UTF8.GetBytes("{20}{Beta}\n"),
        };

        var store = await GameDataLoader.LoadFromMemoryAsync(blobs);

        await Assert.That(store.MessagesBySource.ContainsKey("game.mes")).IsTrue();
        await Assert.That(store.MessagesBySource.ContainsKey("items.mes")).IsTrue();
        await Assert.That(store.MessagesBySource["game.mes"][0].Text).IsEqualTo("Alpha");
        await Assert.That(store.MessagesBySource["items.mes"][0].Text).IsEqualTo("Beta");
    }
}
