using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Tests;

public class GameDataStoreTests
{
    private static GameObjectHeader MakeHeader(uint guid) =>
        new()
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(-1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(2, 0, (int)guid, Guid.Empty),
            GameObjectType = ObjectType.Generic,
            Bitmap = new byte[2], // 2 bytes = 16 bits
        };

    private static ScrFile MakeScript(string description = "Test script") =>
        new()
        {
            HeaderFlags = 0,
            HeaderCounters = 0,
            Description = description,
            Flags = 0,
            Entries = [],
        };

    private static DlgFile MakeDialog(string text = "Hello") =>
        new()
        {
            Entries =
            [
                new DialogEntry
                {
                    Num = 1,
                    Text = text,
                    GenderField = string.Empty,
                    Iq = 0,
                    Conditions = string.Empty,
                    ResponseVal = 0,
                    Actions = string.Empty,
                },
            ],
        };

    [Test]
    public async Task AddObject_IncreasesCount()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(1));
        await Assert.That(store.Objects.Count).IsEqualTo(1);
    }

    [Test]
    public async Task FindByGuid_ExistingObject_ReturnsHeader()
    {
        var store = new GameDataStore();
        var header = MakeHeader(42);
        store.AddObject(header);

        var found = store.FindByGuid(header.ObjectId);
        await Assert.That(found).IsNotNull();
        await Assert.That(found!.ObjectId).IsEqualTo(header.ObjectId);
    }

    [Test]
    public async Task FindByGuid_MissingGuid_ReturnsNull()
    {
        var store = new GameDataStore();
        var missing = new GameObjectGuid(2, 0, 999, Guid.Empty);
        var found = store.FindByGuid(missing);
        await Assert.That(found).IsNull();
    }

    [Test]
    public async Task MarkDirty_AddsToDirtySet()
    {
        var store = new GameDataStore();
        var header = MakeHeader(7);
        store.AddObject(header);
        store.MarkDirty(header.ObjectId);

        await Assert.That(store.DirtyObjects.Contains(header.ObjectId)).IsTrue();
    }

    [Test]
    public async Task ClearDirty_EmptiesDirtySet()
    {
        var store = new GameDataStore();
        var header = MakeHeader(7);
        store.AddObject(header);
        store.MarkDirty(header.ObjectId);
        store.ClearDirty();

        await Assert.That(store.DirtyObjects.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ObjectChanged_FiredOnMarkDirty()
    {
        var store = new GameDataStore();
        var header = MakeHeader(3);
        store.AddObject(header);

        GameObjectGuid? received = null;
        store.ObjectChanged += (_, id) => received = id;
        store.MarkDirty(header.ObjectId);

        await Assert.That(received).IsNotNull();
        await Assert.That(received!.Value).IsEqualTo(header.ObjectId);
    }

    [Test]
    public async Task AddMessage_IncreasesMessageCount()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(0, "Hello world"));
        await Assert.That(store.Messages.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddMessage_PreservesIndexAndText()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1100, "snd_foo", "Skill text"));

        await Assert.That(store.Messages[0].Index).IsEqualTo(1100);
        await Assert.That(store.Messages[0].SoundId).IsEqualTo("snd_foo");
        await Assert.That(store.Messages[0].Text).IsEqualTo("Skill text");
    }

    [Test]
    public async Task Clear_ResetsAllCollections()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(1));
        store.AddMessage(new MessageEntry(1, "x"));
        store.AddScript(MakeScript());
        store.AddDialog(MakeDialog());
        store.Clear();

        await Assert.That(store.Objects.Count).IsEqualTo(0);
        await Assert.That(store.Messages.Count).IsEqualTo(0);
        await Assert.That(store.Scripts.Count).IsEqualTo(0);
        await Assert.That(store.Dialogs.Count).IsEqualTo(0);
        await Assert.That(store.DirtyObjects.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddDialogAndScript_IncreaseCounts()
    {
        var store = new GameDataStore();
        store.AddScript(MakeScript());
        store.AddDialog(MakeDialog());

        await Assert.That(store.Scripts.Count).IsEqualTo(1);
        await Assert.That(store.Dialogs.Count).IsEqualTo(1);
        await Assert.That(store.Dialogs[0].Entries[0].Text).IsEqualTo("Hello");
    }

    // ── G4: MessagesBySource origin tracking ────────────────────────────

    [Test]
    public async Task AddMessage_WithSourcePath_PopulatesMessagesBySource()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(10, "Alpha"), "game.mes");
        store.AddMessage(new MessageEntry(20, "Beta"), "items.mes");

        await Assert.That(store.MessagesBySource.ContainsKey("game.mes")).IsTrue();
        await Assert.That(store.MessagesBySource.ContainsKey("items.mes")).IsTrue();
        await Assert.That(store.MessagesBySource["game.mes"][0].Text).IsEqualTo("Alpha");
        await Assert.That(store.MessagesBySource["items.mes"][0].Text).IsEqualTo("Beta");
        // Flat Messages list must still contain all entries
        await Assert.That(store.Messages.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Clear_ResetsBySourceDictionaries()
    {
        var store = new GameDataStore();
        store.AddMessage(new MessageEntry(1, "x"), "game.mes");
        store.Clear();

        await Assert.That(store.MessagesBySource.Count).IsEqualTo(0);
        await Assert.That(store.Messages.Count).IsEqualTo(0);
    }
}
