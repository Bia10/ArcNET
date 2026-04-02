using System.Text;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.GameData.Tests;

public sealed class GameDataSaverTests
{
    // ── SaveMessagesToMemory ────────────────────────────────────────────────

    [Test]
    public async Task SaveMessagesToMemory_PreservesOriginalIndex()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1100, "First skill"));
        store.AddMessage(new MessageEntry(1200, "Second skill"));

        var bytes = GameDataSaver.SaveMessagesToMemory(store);
        var text = Encoding.ASCII.GetString(bytes);

        await Assert.That(text).Contains("{1100}{First skill}");
        await Assert.That(text).Contains("{1200}{Second skill}");
    }

    [Test]
    public async Task SaveMessagesToMemory_TwoField_NullSoundId()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(5, null, "Hello"));

        var bytes = GameDataSaver.SaveMessagesToMemory(store);
        var text = Encoding.ASCII.GetString(bytes);

        await Assert.That(text).Contains("{5}{Hello}");
        await Assert.That(text).DoesNotContain("{null}");
    }

    [Test]
    public async Task SaveMessagesToMemory_ThreeField_SoundIdPreserved()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(200, "snd_abc", "The text"));

        var bytes = GameDataSaver.SaveMessagesToMemory(store);
        var text = Encoding.ASCII.GetString(bytes);

        await Assert.That(text).Contains("{200}{snd_abc}{The text}");
    }

    [Test]
    public async Task SaveMessagesToMemory_EmptyStore_ReturnsEmptyBytes()
    {
        var store = new GameDataStore();
        var bytes = GameDataSaver.SaveMessagesToMemory(store);

        await Assert.That(bytes.Length).IsEqualTo(0);
    }

    // ── Round-trip (messages) ───────────────────────────────────────────────

    [Test]
    public async Task RoundTrip_Messages_IndexAndTextSurvive()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(10, "Alpha"));
        store.AddMessage(new MessageEntry(20, "snd_x", "Beta"));

        var bytes = GameDataSaver.SaveMessagesToMemory(store);
        var reloaded = MessageFormat.ParseMemory(bytes);

        await Assert.That(reloaded.Entries.Count).IsEqualTo(2);
        await Assert.That(reloaded.Entries[0].Index).IsEqualTo(10);
        await Assert.That(reloaded.Entries[0].Text).IsEqualTo("Alpha");
        await Assert.That(reloaded.Entries[1].Index).IsEqualTo(20);
        await Assert.That(reloaded.Entries[1].SoundId).IsEqualTo("snd_x");
        await Assert.That(reloaded.Entries[1].Text).IsEqualTo("Beta");
    }

    // ── SaveToMemory ────────────────────────────────────────────────────────

    [Test]
    public async Task SaveToMemory_WithMessages_ContainsGameMesKey()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1, "test"));

        var result = GameDataSaver.SaveToMemory(store);

        await Assert.That(result.ContainsKey("game.mes")).IsTrue();
    }

    [Test]
    public async Task SaveToMemory_EmptyStore_ReturnsEmptyDict()
    {
        var store = new GameDataStore();
        var result = GameDataSaver.SaveToMemory(store);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    // ── SaveMessagesToFile ──────────────────────────────────────────────────

    [Test]
    public async Task SaveMessagesToFile_WritesReadableContent()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(42, "File test"));

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".mes");
        try
        {
            GameDataSaver.SaveMessagesToFile(store, path);
            var content = File.ReadAllText(path, Encoding.ASCII);

            await Assert.That(content).Contains("{42}{File test}");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task SaveMessagesToFile_CreatesDirectoryIfMissing()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1, "dir test"));

        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "sub");
        var path = Path.Combine(dir, "out.mes");
        try
        {
            GameDataSaver.SaveMessagesToFile(store, path);
            await Assert.That(File.Exists(path)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    // ── Null checks ─────────────────────────────────────────────────────────

    [Test]
    public void SaveMessagesToMemory_NullStore_Throws() =>
        Assert.Throws<ArgumentNullException>(() => GameDataSaver.SaveMessagesToMemory(null!));

    [Test]
    public void SaveToMemory_NullStore_Throws() =>
        Assert.Throws<ArgumentNullException>(() => GameDataSaver.SaveToMemory(null!));

    // ── G4: per-source message save ─────────────────────────────────────────

    [Test]
    public async Task SaveToMemory_WithMessagesBySource_UsesSourceFilenames()
    {
        // Load from two named blobs so origin tracking is populated
        var blobs = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["game.mes"] = System.Text.Encoding.UTF8.GetBytes("{10}{Alpha}\n"),
            ["items.mes"] = System.Text.Encoding.UTF8.GetBytes("{20}{Beta}\n"),
        };
        var store = await GameDataLoader.LoadFromMemoryAsync(blobs);

        var result = GameDataSaver.SaveToMemory(store);

        // Both original filenames must be preserved as keys
        await Assert.That(result.ContainsKey("game.mes")).IsTrue();
        await Assert.That(result.ContainsKey("items.mes")).IsTrue();
        // Merged "game.mes" fallback key must NOT appear when origin is known
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SaveToMemory_WithoutOrigin_FallsBackToGameMes()
    {
        // Entries added programmatically (no source path) should fall back
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1, "test"));

        var result = GameDataSaver.SaveToMemory(store);

        await Assert.That(result.ContainsKey("game.mes")).IsTrue();
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SaveToDirectoryAsync_WithMessagesBySource_WritesSeparateFiles()
    {
        var blobs = new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["game.mes"] = System.Text.Encoding.UTF8.GetBytes("{10}{Alpha}\n"),
            ["items.mes"] = System.Text.Encoding.UTF8.GetBytes("{20}{Beta}\n"),
        };
        var store = await GameDataLoader.LoadFromMemoryAsync(blobs);

        var outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await GameDataSaver.SaveToDirectoryAsync(store, outDir);

            await Assert.That(File.Exists(Path.Combine(outDir, "game.mes"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(outDir, "items.mes"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(outDir))
                Directory.Delete(outDir, recursive: true);
        }
    }
}
