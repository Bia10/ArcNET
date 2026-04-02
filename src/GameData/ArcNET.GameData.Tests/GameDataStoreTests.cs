using System.Collections;
using ArcNET.Core.Primitives;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Tests;

public class GameDataStoreTests
{
    private static GameObjectHeader MakeHeader(uint guid) =>
        new()
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(0xFFFFFFFF, 0, 0, 0),
            ObjectId = new GameObjectGuid(0, 0, 0, guid),
            GameObjectType = ObjectType.Generic,
            Bitmap = new BitArray(16),
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
        var missing = new GameObjectGuid(0, 0, 0, 999);
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
        store.AddMessage("Hello world");
        await Assert.That(store.Messages.Count).IsEqualTo(1);
    }
}
