using System.Collections;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.GameObjects.Tests;

public class GameObjectStoreTests
{
    [Test]
    public async Task Store_IsInitiallyEmpty()
    {
        var store = new GameObjectStore();
        await Assert.That(store.Headers.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Store_Add_IncreasesCount()
    {
        var store = new GameObjectStore();
        var header = new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(0xFFFFFFFF, 0, 0, 0),
            ObjectId = new GameObjectGuid(0, 0, 0, 1),
            GameObjectType = ObjectType.Npc,
            Bitmap = new BitArray(20),
        };
        store.Add(header);
        await Assert.That(store.Headers.Count).IsEqualTo(1);
    }
}
