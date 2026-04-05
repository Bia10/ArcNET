using ArcNET.Core.Primitives;
using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public class MobDataBuilderTests
{
    private static readonly GameObjectGuid ProtoId = new(1, 0, 1, Guid.Empty);
    private static readonly GameObjectGuid ObjectId = new(2, 0, 42, Guid.Empty);

    private static ObjectProperty MakeProp(ObjectField field, int value = 10)
    {
        var bytes = BitConverter.GetBytes(value);
        return new ObjectProperty { Field = field, RawBytes = bytes };
    }

    // ── New from scratch ──────────────────────────────────────────────────────

    [Test]
    public async Task Build_NewObject_HasCorrectTypeAndIds()
    {
        var mob = new MobDataBuilder(ObjectType.Weapon, ObjectId, ProtoId).Build();

        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Weapon);
        await Assert.That(mob.Header.ObjectId).IsEqualTo(ObjectId);
        await Assert.That(mob.Header.ProtoId).IsEqualTo(ProtoId);
    }

    [Test]
    public async Task Build_NewObject_NoProperties_EmptyAndCleanBitmap()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId).Build();

        await Assert.That(mob.Properties.Count).IsEqualTo(0);
        await Assert.That(mob.Header.PropCollectionItems).IsEqualTo((short)0);
    }

    [Test]
    public async Task Build_Version_IsAlways0x77()
    {
        var mob = new MobDataBuilder(ObjectType.Generic, ObjectId, ProtoId).Build();
        await Assert.That(mob.Header.Version).IsEqualTo(0x77);
    }

    // ── WithProperty ─────────────────────────────────────────────────────────

    [Test]
    public async Task WithProperty_AddNewField_AppearsInPropertiesAndBitmap()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName))
            .Build();

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFName)).IsTrue();
        await Assert.That(mob.Header.PropCollectionItems).IsEqualTo((short)1);
    }

    [Test]
    public async Task WithProperty_DuplicateField_ReplacesExisting()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName, 1))
            .WithProperty(MakeProp(ObjectField.ObjFName, 99))
            .Build();

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(BitConverter.ToInt32(mob.Properties[0].RawBytes)).IsEqualTo(99);
    }

    // ── WithoutProperty ───────────────────────────────────────────────────────

    [Test]
    public async Task WithoutProperty_ExistingField_Removed()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName))
            .WithoutProperty(ObjectField.ObjFName)
            .Build();

        await Assert.That(mob.Properties.Count).IsEqualTo(0);
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFName)).IsFalse();
    }

    [Test]
    public async Task WithoutProperty_AbsentField_NoChange()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName))
            .WithoutProperty(ObjectField.ObjFHpPts)
            .Build();

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
    }

    // ── WithLocation ──────────────────────────────────────────────────────────

    [Test]
    public async Task WithLocation_SetsLocationProperty()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId).WithLocation(tileX: 10, tileY: 20).Build();

        var prop = mob.GetProperty(ObjectField.ObjFLocation);
        await Assert.That(prop).IsNotNull();

        // Wire format: 1 presence byte + 8-byte int64
        await Assert.That(prop!.RawBytes.Length).IsEqualTo(9);
        await Assert.That(prop.RawBytes[0]).IsEqualTo((byte)1); // present

        var packed = BitConverter.ToInt64(prop.RawBytes, 1);
        var x = (int)(packed & 0xFFFFFFFF);
        var y = (int)((packed >> 32) & 0xFFFFFFFF);
        await Assert.That(x).IsEqualTo(10);
        await Assert.That(y).IsEqualTo(20);
    }

    // ── From existing MobData ─────────────────────────────────────────────────

    [Test]
    public async Task ConstructFromExisting_PreservesProperties()
    {
        var original = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName))
            .Build();

        var modified = new MobDataBuilder(original).WithProperty(MakeProp(ObjectField.ObjFHpPts, 50)).Build();

        await Assert.That(modified.Properties.Count).IsEqualTo(2);
        await Assert.That(modified.Header.Bitmap.HasField(ObjectField.ObjFName)).IsTrue();
        await Assert.That(modified.Header.Bitmap.HasField(ObjectField.ObjFHpPts)).IsTrue();
    }

    [Test]
    public async Task ConstructFromExisting_DoesNotMutateOriginal()
    {
        var original = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName))
            .Build();

        _ = new MobDataBuilder(original).WithProperty(MakeProp(ObjectField.ObjFHpPts)).Build();

        await Assert.That(original.Properties.Count).IsEqualTo(1);
    }

    // ── Round-trip through MobFormat ─────────────────────────────────────────

    [Test]
    public async Task Build_WallMob_RoundTripsThroughMobFormat()
    {
        var mob = new MobDataBuilder(ObjectType.Wall, ObjectId, ProtoId)
            .WithProperty(MakeProp(ObjectField.ObjFName, 100))
            .Build();

        var bytes = MobFormat.WriteToArray(in mob);
        var reparsed = MobFormat.ParseMemory(bytes);

        await Assert.That(reparsed.Header.GameObjectType).IsEqualTo(ObjectType.Wall);
        var nameProp = reparsed.GetProperty(ObjectField.ObjFName);
        await Assert.That(nameProp).IsNotNull();
        await Assert.That(BitConverter.ToInt32(nameProp!.RawBytes)).IsEqualTo(100);
    }
}
