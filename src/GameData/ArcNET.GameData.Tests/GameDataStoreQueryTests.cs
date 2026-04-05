using System.Collections;
using ArcNET.Core.Primitives;
using ArcNET.GameData;
using ArcNET.GameObjects;

namespace ArcNET.GameData.Tests;

public class GameDataStoreQueryTests
{
    private static GameObjectHeader MakeHeader(ObjectType type, uint objectSeed, uint protoSeed = 1) =>
        new()
        {
            Version = 0x77,
            ProtoId = new GameObjectGuid(1, 0, (int)protoSeed, Guid.Empty),
            ObjectId = new GameObjectGuid(2, 0, (int)objectSeed, Guid.Empty),
            GameObjectType = type,
            Bitmap = new BitArray(16),
        };

    // ── FindByType ────────────────────────────────────────────────────────────

    [Test]
    public async Task FindByType_EmptyStore_ReturnsEmptyList()
    {
        var store = new GameDataStore();
        var result = store.FindByType(ObjectType.Npc);
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindByType_MatchingObjects_ReturnsAll()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(ObjectType.Npc, 1));
        store.AddObject(MakeHeader(ObjectType.Npc, 2));
        store.AddObject(MakeHeader(ObjectType.Weapon, 3));

        var npcs = store.FindByType(ObjectType.Npc);
        await Assert.That(npcs.Count).IsEqualTo(2);
    }

    [Test]
    public async Task FindByType_NoMatch_ReturnsEmptyList()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(ObjectType.Weapon, 1));

        var portals = store.FindByType(ObjectType.Portal);
        await Assert.That(portals.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindByType_ReturnedHeaders_HaveCorrectType()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(ObjectType.Pc, 10));
        store.AddObject(MakeHeader(ObjectType.Npc, 11));
        store.AddObject(MakeHeader(ObjectType.Pc, 12));

        var pcs = store.FindByType(ObjectType.Pc);
        await Assert.That(pcs.Count).IsEqualTo(2);
        foreach (var h in pcs)
            await Assert.That(h.GameObjectType).IsEqualTo(ObjectType.Pc);
    }

    // ── FindByProtoId ─────────────────────────────────────────────────────────

    [Test]
    public async Task FindByProtoId_EmptyStore_ReturnsEmptyList()
    {
        var store = new GameDataStore();
        var protoId = new GameObjectGuid(1, 0, 5, Guid.Empty);
        var result = store.FindByProtoId(protoId);
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindByProtoId_MatchingObjects_ReturnsAll()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(ObjectType.Npc, 1, protoSeed: 7));
        store.AddObject(MakeHeader(ObjectType.Npc, 2, protoSeed: 7));
        store.AddObject(MakeHeader(ObjectType.Npc, 3, protoSeed: 8));

        var protoId = new GameObjectGuid(1, 0, 7, Guid.Empty);
        var result = store.FindByProtoId(protoId);
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task FindByProtoId_NoMatch_ReturnsEmptyList()
    {
        var store = new GameDataStore();
        store.AddObject(MakeHeader(ObjectType.Npc, 1, protoSeed: 5));

        var protoId = new GameObjectGuid(1, 0, 99, Guid.Empty);
        await Assert.That(store.FindByProtoId(protoId).Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindByProtoId_ReturnedHeaders_HaveCorrectProtoId()
    {
        var store = new GameDataStore();
        var targetProto = new GameObjectGuid(1, 0, 42, Guid.Empty);
        store.AddObject(MakeHeader(ObjectType.Weapon, 1, 42));
        store.AddObject(MakeHeader(ObjectType.Weapon, 2, 42));
        store.AddObject(MakeHeader(ObjectType.Armor, 3, 99));

        var result = store.FindByProtoId(targetProto);
        await Assert.That(result.Count).IsEqualTo(2);
        foreach (var h in result)
            await Assert.That(h.ProtoId).IsEqualTo(targetProto);
    }
}
