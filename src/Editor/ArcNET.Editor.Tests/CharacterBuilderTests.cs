using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public class CharacterBuilderTests
{
    private static readonly GameObjectGuid s_objectId = new(2, 0, 1, Guid.Empty);
    private static readonly GameObjectGuid s_protoId = new(1, 0, 0, Guid.Empty);

    // ── Construction ──────────────────────────────────────────────────────────

    [Test]
    public async Task NewPc_WithNoProperties_BuildsValidHeader()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).Build();
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Pc);
        await Assert.That(mob.Properties.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NewNpc_WithNoProperties_BuildsValidHeader()
    {
        var mob = new CharacterBuilder(ObjectType.Npc, s_objectId, s_protoId).Build();
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Npc);
    }

    // ── Common fields ─────────────────────────────────────────────────────────

    [Test]
    public async Task WithHitPoints_SetsHpPtsAndAdj()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithHitPoints(75, 5).Build();

        var pts = mob.Properties.First(p => p.Field == ObjectField.ObjFHpPts);
        var adj = mob.Properties.First(p => p.Field == ObjectField.ObjFHpAdj);
        await Assert.That(pts.GetInt32()).IsEqualTo(75);
        await Assert.That(adj.GetInt32()).IsEqualTo(5);
    }

    [Test]
    public async Task WithHitPoints_DefaultAdj_IsZero()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithHitPoints(50).Build();

        var adj = mob.Properties.First(p => p.Field == ObjectField.ObjFHpAdj);
        await Assert.That(adj.GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task WithFatigue_SetsFatiguePtsAndAdj()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithFatigue(100, 10).Build();

        var pts = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterFatiguePts);
        var adj = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterFatigueAdj);
        await Assert.That(pts.GetInt32()).IsEqualTo(100);
        await Assert.That(adj.GetInt32()).IsEqualTo(10);
    }

    [Test]
    public async Task WithPortrait_SetsField()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithPortrait(42).Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterPortrait);
        await Assert.That(prop.GetInt32()).IsEqualTo(42);
    }

    [Test]
    public async Task WithGold_SetsField()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithGold(999).Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterGold);
        await Assert.That(prop.GetInt32()).IsEqualTo(999);
    }

    [Test]
    public async Task WithBaseStats_SetsStatArray()
    {
        int[] stats = [10, 12, 8, 14, 10, 9];
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithBaseStats(stats).Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterStatBaseIdx);
        await Assert.That(prop.GetInt32Array().SequenceEqual(stats)).IsTrue();
    }

    [Test]
    public async Task WithBasicSkills_SetsSkillArray()
    {
        int[] skills = [1, 2, 3, 4, 5];
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithBasicSkills(skills).Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterBasicSkillIdx);
        await Assert.That(prop.GetInt32Array().SequenceEqual(skills)).IsTrue();
    }

    [Test]
    public async Task WithInventory_SetsListAndCount()
    {
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithInventory([item1, item2]).Build();

        var listProp = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterInventoryListIdx);
        var countProp = mob.Properties.First(p => p.Field == ObjectField.ObjFCritterInventoryNum);
        var ids = listProp.GetObjectIdArray();
        await Assert.That(ids.Length).IsEqualTo(2);
        await Assert.That(ids[0]).IsEqualTo(item1);
        await Assert.That(ids[1]).IsEqualTo(item2);
        await Assert.That(countProp.GetInt32()).IsEqualTo(2);
    }

    [Test]
    public async Task WithLocation_SetsLocationField()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithLocation(256, 512).Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFLocation);
        var (x, y) = prop.GetLocation();
        await Assert.That(x).IsEqualTo(256);
        await Assert.That(y).IsEqualTo(512);
    }

    // ── PC-only fields ────────────────────────────────────────────────────────

    [Test]
    public async Task WithPlayerName_SetsString()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithPlayerName("Roberta").Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
        await Assert.That(prop.GetString()).IsEqualTo("Roberta");
    }

    [Test]
    public async Task WithBankMoney_SetsField()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithBankMoney(5000).Build();

        var prop = mob.Properties.First(p => p.Field == ObjectField.ObjFPcBankMoney);
        await Assert.That(prop.GetInt32()).IsEqualTo(5000);
    }

    // ── Header bitmap ─────────────────────────────────────────────────────────

    [Test]
    public async Task Build_BitmapReflectsAllSetFields()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId)
            .WithHitPoints(100)
            .WithPlayerName("Test")
            .WithBaseStats([10, 10, 10, 10, 10, 10])
            .Build();

        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFHpPts)).IsTrue();
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFHpAdj)).IsTrue();
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFPcPlayerName)).IsTrue();
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFCritterStatBaseIdx)).IsTrue();
    }

    // ── Round-trip through MobFormat ──────────────────────────────────────────

    [Test]
    public async Task Build_RoundTripsThroughMobFormat()
    {
        var mob = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId)
            .WithHitPoints(80, 3)
            .WithPlayerName("Alice")
            .WithGold(250)
            .WithBaseStats([10, 12, 9, 14, 8, 11])
            .Build();

        var bytes = MobFormat.WriteToArray(mob);
        var parsed = MobFormat.ParseMemory(bytes);

        await Assert.That(parsed.Header.GameObjectType).IsEqualTo(ObjectType.Pc);
        var name = parsed.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
        await Assert.That(name.GetString()).IsEqualTo("Alice");
        var gold = parsed.Properties.First(p => p.Field == ObjectField.ObjFCritterGold);
        await Assert.That(gold.GetInt32()).IsEqualTo(250);
    }

    // ── Existing character editing ────────────────────────────────────────────

    [Test]
    public async Task FromExisting_PreservesUnmodifiedProperties()
    {
        var original = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId)
            .WithHitPoints(100)
            .WithGold(500)
            .Build();

        var edited = new CharacterBuilder(original).WithPlayerName("Bob").Build();

        var pts = edited.Properties.First(p => p.Field == ObjectField.ObjFHpPts);
        await Assert.That(pts.GetInt32()).IsEqualTo(100);
        var gold = edited.Properties.First(p => p.Field == ObjectField.ObjFCritterGold);
        await Assert.That(gold.GetInt32()).IsEqualTo(500);
        var name = edited.Properties.First(p => p.Field == ObjectField.ObjFPcPlayerName);
        await Assert.That(name.GetString()).IsEqualTo("Bob");
    }

    [Test]
    public async Task FromExisting_OriginalIsUnchanged()
    {
        var original = new CharacterBuilder(ObjectType.Pc, s_objectId, s_protoId).WithGold(100).Build();

        _ = new CharacterBuilder(original).WithGold(9999).Build();

        var goldProp = original.Properties.First(p => p.Field == ObjectField.ObjFCritterGold);
        await Assert.That(goldProp.GetInt32()).IsEqualTo(100);
    }
}
