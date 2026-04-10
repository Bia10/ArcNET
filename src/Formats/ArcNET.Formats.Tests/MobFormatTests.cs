using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;
using static ArcNET.Formats.Tests.SpanWriterTestHelpers;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MobFormat"/>.</summary>
public sealed class MobFormatTests
{
    // ── ObjectID wire helpers (24 bytes each) ────────────────────────────────────
    // struct ObjectID { int16_t type; int16_t pad2; int pad4; TigGuid g; }  sizeof=0x18
    // OID_TYPE_BLOCKED = -1  (marks a prototype definition)
    // OID_TYPE_GUID    =  2  (GUID-based instance OID)

    private static void WriteOidBlocked(SpanWriter w)
    {
        w.WriteInt16(-1); // OID_TYPE_BLOCKED
        w.WriteInt16(0); // padding_2
        w.WriteInt32(0); // padding_4
        w.WriteBytes(new byte[16]); // TigGuid = zeros
    }

    private static void WriteOidGuid(SpanWriter w, Guid g)
    {
        w.WriteInt16(2); // OID_TYPE_GUID
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(g.ToByteArray());
    }

    private static void WriteOidRef(SpanWriter w, int protoIndex = 1)
    {
        // OID_TYPE_A = 1 — references a prototype by art-based ID
        w.WriteInt16(1);
        w.WriteInt16(0);
        w.WriteInt32(protoIndex);
        w.WriteBytes(new byte[16]);
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

            // ProtoId (24 bytes) — non-prototype: OidType != -1.
            WriteOidRef(w, protoIndex: 1);

            // ObjectId (24 bytes) — unique instance GUID
            WriteOidGuid(w, Guid.Parse("00000001-0000-0000-0000-000000000000"));

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
            // ProtoId — non-prototype instance (ref to prototype)
            WriteOidRef(w);
            // ObjectId
            WriteOidGuid(w, Guid.Parse("00000001-0000-0000-0000-000000000000"));
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
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("00000002-0000-0000-0000-000000000000"));
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

    [Test]
    public async Task Container_InventoryListIdx_ParsedAsHandleArray()
    {
        // Container MOB with bit 68 (ContainerInventoryListIdx) holding a 2-item SAR block.
        // Verifies the wire type is HandleArray (SAR of 24-byte ObjectIDs), not Int32.
        var itemGuid1 = Guid.Parse("AAAAAAAA-0000-0000-0000-000000000001");
        var itemGuid2 = Guid.Parse("BBBBBBBB-0000-0000-0000-000000000002");

        // Build the SAR block for 2 ObjectIDs.
        // SAR layout: sarCount(1) + elementSize(4) + elementCount(4) + sarcIndex(4) = 13 bytes header
        //             + 2×24 bytes data + postSize(4) + post(4×postSize) bytes
        var elementSize = (uint)ObjectPropertyExtensions.ObjectIdWireSize; // 24
        const int elementCount = 2;
        const short oidTypeGuid = 2;
        var sarData = new byte[elementCount * (int)elementSize];
        for (var i = 0; i < elementCount; i++)
        {
            var o = i * (int)elementSize;
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(sarData.AsSpan(o), oidTypeGuid);
            // padding_2, padding_4 = 0
            (i == 0 ? itemGuid1 : itemGuid2)
                .ToByteArray()
                .CopyTo(sarData, o + 8);
        }

        var sarBytes = new byte[13 + sarData.Length + 4 + 4];
        sarBytes[0] = 1; // sarCount
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(sarBytes.AsSpan(1), elementSize);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(sarBytes.AsSpan(5), elementCount);
        // sarcIndex = 0
        sarData.CopyTo(sarBytes, 13);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(sarBytes.AsSpan(13 + sarData.Length), 1); // postSize = 1
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            sarBytes.AsSpan(13 + sarData.Length + 4),
            0xFFFFFFFF
        ); // all bits

        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidRef(w);
            WriteOidGuid(w, Guid.Parse("CCCCCCCC-0000-0000-0000-000000000003"));
            w.WriteUInt32((uint)ObjectType.Container);
            w.WriteInt16(1); // 1 property

            // Container bitmap (12 bytes): set bit 68 (ContainerInventoryListIdx)
            var bitmap = new byte[12];
            bitmap[68 / 8] |= (byte)(1 << (68 % 8));
            w.WriteBytes(bitmap);

            w.WriteBytes(sarBytes);
        });

        var mob = MobFormat.ParseMemory(bytes);

        await Assert.That(mob.Properties.Count).IsEqualTo(1);
        await Assert.That(mob.Properties[0].Field).IsEqualTo(ObjectField.ObjFContainerInventoryListIdx);

        var ids = mob.Properties[0].GetObjectIdArray();
        await Assert.That(ids.Length).IsEqualTo(2);
        await Assert.That(ids[0]).IsEqualTo(itemGuid1);
        await Assert.That(ids[1]).IsEqualTo(itemGuid2);
    }
}
