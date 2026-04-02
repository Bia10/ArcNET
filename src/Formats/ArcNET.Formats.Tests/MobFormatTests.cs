using System.Buffers;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MobFormat"/>.</summary>
public sealed class MobFormatTests
{
    private static byte[] BuildBytes(Action<SpanWriter> fill)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        fill(w);
        return buf.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes a minimal valid MOB header with a Wall object type.
    /// Wall bitmap is 12 bytes. We set bit 21 (ObjFName → Int32).
    /// </summary>
    private static byte[] BuildMinimalWallMob()
    {
        return BuildBytes(w =>
        {
            w.WriteInt32(0x77); // version

            // ProtoId (16 bytes) — Type must NOT be 0xFFFFFFFF for a mob (instance).
            // 0xFFFFFFFF would set IsPrototype=true and suppress PropCollectionItems.
            // Use ObjectType.Wall (0) to reference a Wall prototype.
            w.WriteUInt32((uint)ObjectType.Wall);
            w.WriteUInt32(1);
            w.WriteUInt32(0);
            w.WriteUInt32(0);

            // ObjectId (16 bytes) — non-proto instance
            w.WriteUInt32((uint)ObjectType.Wall.GetHashCode()); // Type field
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(1); // Foo0, Foo2, Guid

            // GameObjectType
            w.WriteUInt32((uint)ObjectType.Wall);

            // PropCollectionItems (int16, present because not prototype)
            w.WriteInt16(1);

            // Bitmap — 12 bytes for Wall; set bit 21 (ObjFName)
            var bitmap = new byte[12];
            bitmap[2] = 0x20; // byte 2, bit 5 = bit index 21
            w.WriteBytes(bitmap);

            // Property: ObjFName = Int32 (value = 42)
            w.WriteInt32(42);
        });
    }

    [Test]
    public async Task Parse_MinimalWall_HeaderFieldsCorrect()
    {
        var bytes = BuildMinimalWallMob();
        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Header.Version).IsEqualTo(0x77);
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Wall);
        await Assert.That(mob.Header.IsPrototype).IsFalse();
    }

    [Test]
    public async Task Parse_MinimalWall_OnePropertyRead()
    {
        var bytes = BuildMinimalWallMob();
        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.ObjFName);
        await Assert.That(mob.Properties[0].RawBytes.Length).IsEqualTo(4);
    }

    [Test]
    public async Task Parse_BadVersion_Throws()
    {
        var bytes = BuildBytes(w => w.WriteInt32(0x01));
        Assert.Throws<InvalidDataException>(() => MobFormat.ParseMemory(bytes));
    }

    [Test]
    public async Task RoundTrip_MinimalWall_Identical()
    {
        var bytes = BuildMinimalWallMob();
        var original = MobFormat.ParseMemory(bytes);
        var rewritten = MobFormat.WriteToArray(in original);
        var back = MobFormat.ParseMemory(rewritten);

        await Assert.That(back.Header.GameObjectType).IsEqualTo(original.Header.GameObjectType);
        await Assert.That(back.Properties.Count).IsEqualTo(original.Properties.Count);
        await Assert.That(back.Properties[0].Field).IsEqualTo(original.Properties[0].Field);
        await Assert.That(back.Properties[0].RawBytes.SequenceEqual(original.Properties[0].RawBytes)).IsTrue();
    }

    [Test]
    public async Task Parse_EmptyBitmap_NoProperties()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            // ProtoId — not proto
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(1);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            // ObjectId
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(1);
            w.WriteUInt32((uint)ObjectType.Portal);
            w.WriteInt16(0); // PropCollectionItems
            // Portal bitmap — 12 bytes, all zero
            w.WriteBytes(new byte[12]);
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(0);
        await Assert.That(mob.Header.GameObjectType).IsEqualTo(ObjectType.Portal);
    }

    [Test]
    public async Task Parse_TwoProperties_BothPresent()
    {
        // Wall MOB with bit 21 (ObjFName, Int32) and bit 23 (ObjFAid, Int32) set.
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            // ProtoId
            w.WriteUInt32((uint)ObjectType.Wall);
            w.WriteUInt32(1);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            // ObjectId
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(2);
            w.WriteUInt32((uint)ObjectType.Wall);
            w.WriteInt16(2); // 2 properties
            // Bitmap 12 bytes: set bits 21 and 23
            var bitmap = new byte[12];
            bitmap[21 / 8] |= (byte)(1 << (21 % 8));
            bitmap[23 / 8] |= (byte)(1 << (23 % 8));
            w.WriteBytes(bitmap);
            w.WriteInt32(999); // ObjFName = 999
            w.WriteInt32(42); // ObjFAid  = 42
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(2);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.ObjFName);
        await Assert.That(mob.Properties[0].GetInt32()).IsEqualTo(999);
        await Assert.That(mob.Properties[1].Field).IsEqualTo(ObjectField.ObjFAid);
        await Assert.That(mob.Properties[1].GetInt32()).IsEqualTo(42);
    }
}
