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
            ProtoId = new GameObjectGuid(-1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(2, 0, 0, new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)),
            GameObjectType = ObjectType.Npc,
            Bitmap = new byte[20], // 20 bytes = 160 bits, covers all NPC fields
        };
        };
        store.Add(header);
        await Assert.That(store.Headers.Count).IsEqualTo(1);
    }
}
