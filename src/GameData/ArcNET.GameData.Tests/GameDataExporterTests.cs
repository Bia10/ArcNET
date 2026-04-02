using System.Text.Json;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.GameData.Tests;

public sealed class GameDataExporterTests
{
    [Test]
    public async Task ExportToJson_EmptyStore_ProducesValidJson()
    {
        var store = new GameDataStore();
        var json = GameDataExporter.ExportToJson(store);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        await Assert.That(root.GetProperty("messages").GetArrayLength()).IsEqualTo(0);
        await Assert.That(root.GetProperty("objects").GetArrayLength()).IsEqualTo(0);
        await Assert.That(root.GetProperty("sectors").GetArrayLength()).IsEqualTo(0);
        await Assert.That(root.GetProperty("protos").GetArrayLength()).IsEqualTo(0);
        await Assert.That(root.GetProperty("mobs").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task ExportToJson_WithMessages_IndexAndSoundIdAndTextExported()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(100, "First"));
        store.AddMessage(new MessageEntry(200, "snd_x", "Second"));

        var json = GameDataExporter.ExportToJson(store);
        using var doc = JsonDocument.Parse(json);
        var messages = doc.RootElement.GetProperty("messages");

        await Assert.That(messages.GetArrayLength()).IsEqualTo(2);

        var first = messages[0];
        await Assert.That(first.GetProperty("index").GetInt32()).IsEqualTo(100);
        await Assert.That(first.GetProperty("text").GetString()).IsEqualTo("First");
        await Assert.That(first.GetProperty("soundId").ValueKind).IsEqualTo(JsonValueKind.Null);

        var second = messages[1];
        await Assert.That(second.GetProperty("index").GetInt32()).IsEqualTo(200);
        await Assert.That(second.GetProperty("soundId").GetString()).IsEqualTo("snd_x");
        await Assert.That(second.GetProperty("text").GetString()).IsEqualTo("Second");
    }

    [Test]
    public async Task ExportToJson_ProducesWellFormedJson()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1, "Hello"));

        var json = GameDataExporter.ExportToJson(store);

        // Will throw if the JSON is malformed
        using var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.ValueKind).IsEqualTo(JsonValueKind.Object);
    }

    [Test]
    public async Task ExportToJsonFileAsync_WritesJsonToFile()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(7, "File export"));

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        try
        {
            await GameDataExporter.ExportToJsonFileAsync(store, path);

            await Assert.That(File.Exists(path)).IsTrue();
            var content = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(content);
            await Assert.That(doc.RootElement.GetProperty("messages").GetArrayLength()).IsEqualTo(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
