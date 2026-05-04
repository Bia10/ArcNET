using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

public class MobDataExtensionsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObjectHeader MakeHeader(ObjectType type, bool isProto = false)
    {
        // Wall = 12-byte bitmap, Weapon = 16-byte, Pc = 20-byte
        var bitmapBytes = type switch
        {
            ObjectType.Pc or ObjectType.Npc => 20,
            ObjectType.Weapon
            or ObjectType.Ammo
            or ObjectType.Armor
            or ObjectType.Gold
            or ObjectType.Food
            or ObjectType.Scroll
            or ObjectType.Key
            or ObjectType.KeyRing
            or ObjectType.Written
            or ObjectType.Generic => 16,
            _ => 12,
        };

        return new GameObjectHeader
        {
            Version = 0x77,
            ProtoId = isProto ? new GameObjectGuid(-1, 0, 0, Guid.Empty) : new GameObjectGuid(1, 0, 0, Guid.Empty),
            ObjectId = new GameObjectGuid(2, 0, 1, Guid.Empty),
            GameObjectType = type,
            PropCollectionItems = isProto ? (short)0 : (short)0,
            Bitmap = new byte[bitmapBytes],
        };
    }

    private static ObjectProperty MakeProp(ObjectField field, int value = 42)
    {
        var bytes = BitConverter.GetBytes(value);
        return new ObjectProperty { Field = field, RawBytes = bytes };
    }

    private static MobData MakeMob(ObjectType type, params ObjectProperty[] props) =>
        new MobData { Header = MakeHeader(type), Properties = props }.RebuildHeader();

    // ── GetProperty ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetProperty_PresentField_ReturnsProperty()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName));
        var result = mob.GetProperty(ObjectField.ObjFName);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Field).IsEqualTo(ObjectField.ObjFName);
    }

    [Test]
    public async Task GetProperty_AbsentField_ReturnsNull()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName));
        await Assert.That(mob.GetProperty(ObjectField.ObjFHpPts)).IsNull();
    }

    [Test]
    public async Task GetProperty_EmptyProperties_ReturnsNull()
    {
        var mob = MakeMob(ObjectType.Wall);
        await Assert.That(mob.GetProperty(ObjectField.ObjFName)).IsNull();
    }

    // ── WithProperty ─────────────────────────────────────────────────────────

    [Test]
    public async Task WithProperty_NewField_AppendsAndUpdatesHeader()
    {
        var mob = MakeMob(ObjectType.Wall);
        var updated = mob.WithProperty(MakeProp(ObjectField.ObjFName));

        await Assert.That(updated.Properties.Count).IsEqualTo(1);
        await Assert.That(updated.Header.Bitmap.HasField(ObjectField.ObjFName)).IsTrue();
        await Assert.That(updated.Header.PropCollectionItems).IsEqualTo((short)1);
    }

    [Test]
    public async Task WithProperty_ExistingField_ReplacesValue()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName, 10));
        var updated = mob.WithProperty(MakeProp(ObjectField.ObjFName, 99));

        await Assert.That(updated.Properties.Count).IsEqualTo(1);
        await Assert.That(BitConverter.ToInt32(updated.Properties[0].RawBytes)).IsEqualTo(99);
    }

    [Test]
    public async Task WithProperty_DoesNotMutateOriginal()
    {
        var mob = MakeMob(ObjectType.Wall);
        _ = mob.WithProperty(MakeProp(ObjectField.ObjFName));
        await Assert.That(mob.Properties.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WithProperty_NewLowerField_PreservesFieldOrderForRoundTrip()
    {
        var mob = MakeMob(ObjectType.Npc, MakeProp(ObjectField.ObjFMaterial, 9));

        var updated = mob.WithProperty(ObjectPropertyFactory.ForInt32Array(ObjectField.ObjFOverlayLightAid, [7, 8]));
        var reparsed = MobFormat.ParseMemory(MobFormat.WriteToArray(updated));

        await Assert.That(updated.Properties[0].Field).IsEqualTo(ObjectField.ObjFOverlayLightAid);
        await Assert.That(updated.Properties[1].Field).IsEqualTo(ObjectField.ObjFMaterial);
        await Assert.That(reparsed.GetProperty(ObjectField.ObjFOverlayLightAid)).IsNotNull();
        await Assert
            .That(reparsed.GetProperty(ObjectField.ObjFOverlayLightAid)!.GetInt32Array())
            .IsEquivalentTo([7, 8]);
    }

    // ── WithoutProperty ───────────────────────────────────────────────────────

    [Test]
    public async Task WithoutProperty_ExistingField_RemovesAndUpdatesHeader()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName), MakeProp(ObjectField.ObjFHpPts));
        var updated = mob.WithoutProperty(ObjectField.ObjFName);

        await Assert.That(updated.Properties.Count).IsEqualTo(1);
        await Assert.That(updated.Header.Bitmap.HasField(ObjectField.ObjFName)).IsFalse();
        await Assert.That(updated.Header.Bitmap.HasField(ObjectField.ObjFHpPts)).IsTrue();
    }

    [Test]
    public async Task WithoutProperty_AbsentField_LeavesUnchanged()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName));
        var updated = mob.WithoutProperty(ObjectField.ObjFHpPts);
        await Assert.That(updated.Properties.Count).IsEqualTo(1);
    }

    // ── RebuildHeader ─────────────────────────────────────────────────────────

    [Test]
    public async Task RebuildHeader_BitmapMatchesProperties()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName), MakeProp(ObjectField.ObjFHpPts));
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFName)).IsTrue();
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFHpPts)).IsTrue();
        await Assert.That(mob.Header.Bitmap.HasField(ObjectField.ObjFMaterial)).IsFalse();
    }

    [Test]
    public async Task RebuildHeader_PropCollectionItems_MatchesCount()
    {
        var mob = MakeMob(ObjectType.Wall, MakeProp(ObjectField.ObjFName), MakeProp(ObjectField.ObjFHpPts));
        await Assert.That(mob.Header.PropCollectionItems).IsEqualTo((short)2);
    }

    // ── ProtoData parallel surface ────────────────────────────────────────────

    [Test]
    public async Task ProtoData_GetProperty_WorksLikeMobData()
    {
        var proto = new ProtoData
        {
            Header = MakeHeader(ObjectType.Wall, isProto: true),
            Properties = [MakeProp(ObjectField.ObjFName)],
        }.RebuildHeader();

        var result = proto.GetProperty(ObjectField.ObjFName);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ProtoData_WithProperty_AppendsAndUpdatesHeader()
    {
        var proto = new ProtoData
        {
            Header = MakeHeader(ObjectType.Wall, isProto: true),
            Properties = [],
        }.RebuildHeader();

        var updated = proto.WithProperty(MakeProp(ObjectField.ObjFName));
        await Assert.That(updated.Properties.Count).IsEqualTo(1);
        await Assert.That(updated.Header.Bitmap.HasField(ObjectField.ObjFName)).IsTrue();
    }

    [Test]
    public async Task ProtoData_WithoutProperty_RemovesField()
    {
        var proto = new ProtoData
        {
            Header = MakeHeader(ObjectType.Wall, isProto: true),
            Properties = [MakeProp(ObjectField.ObjFName)],
        }.RebuildHeader();

        var updated = proto.WithoutProperty(ObjectField.ObjFName);
        await Assert.That(updated.Properties.Count).IsEqualTo(0);
    }
}
