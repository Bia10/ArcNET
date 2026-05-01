using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;
using static ArcNET.Formats.Tests.SpanWriterTestHelpers;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="ProtoFormat"/>.</summary>
public sealed class ProtoFormatTests
{
    // ── 24-byte ObjectID wire helpers ──
    // struct ObjectID { int16_t type; int16_t pad2; int pad4; TigGuid g; }
    private static void WriteOidBlocked(SpanWriter w)
    {
        w.WriteInt16(-1); // OID_TYPE_BLOCKED — marks this as a prototype definition
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(new byte[16]);
    }

    private static void WriteOidGuid(SpanWriter w, byte lastByte = 1)
    {
        w.WriteInt16(2); // OID_TYPE_GUID
        w.WriteInt16(0);
        w.WriteInt32(0);
        var g = new byte[16];
        g[15] = lastByte;
        w.WriteBytes(g);
    }

    /// <summary>
    /// Builds a minimal valid PRO binary (prototype — OID_TYPE_BLOCKED ProtoId).
    /// Scenery type, bitmap 12 bytes, bit 21 (ObjFName) set.
    /// </summary>
    private static byte[] BuildMinimalSceneryProto()
    {
        return BuildBytes(w =>
        {
            w.WriteInt32(0x77); // version

            // ProtoId — OID_TYPE_BLOCKED (-1) marks this as a prototype definition
            WriteOidBlocked(w);

            // ObjectId — 24-byte GUID-type OID
            WriteOidGuid(w, 1);

            // GameObjectType
            w.WriteUInt32((uint)ObjectType.Scenery);

            // NO PropCollectionItems field — prototype omits it

            // Bitmap — 12 bytes; bit 21 = ObjFName
            var bitmap = new byte[12];
            bitmap[2] = 0x20;
            w.WriteBytes(bitmap);

            // ObjFName property — Int32
            w.WriteInt32(99);
        });
    }

    [Test]
    public async Task Parse_MinimalSceneryProto_IsPrototype()
    {
        var bytes = BuildMinimalSceneryProto();
        var proto = ProtoFormat.ParseMemory(bytes);

        await Assert.That(proto.Header.Version).IsEqualTo(0x77);
        await Assert.That(proto.Header.IsPrototype).IsTrue();
        await Assert.That(proto.Header.GameObjectType).IsEqualTo(ObjectType.Scenery);
    }

    [Test]
    public async Task Parse_MinimalSceneryProto_OnePropertyRead()
    {
        var bytes = BuildMinimalSceneryProto();
        var proto = ProtoFormat.ParseMemory(bytes);

        await Assert.That(proto.Properties.Count).IsEqualTo(1);
        await Assert.That(proto.Properties[0].Field).IsEqualTo(ObjectField.ObjFName);
    }

    [Test]
    public async Task Parse_BadVersion_Throws()
    {
        var bytes = BuildBytes(w => w.WriteInt32(0x42));
        Assert.Throws<InvalidDataException>(() => ProtoFormat.ParseMemory(bytes));
    }

    [Test]
    public async Task Parse_TruncatedSarField_SetsParseNoteInsteadOfThrowing()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidBlocked(w);
            WriteOidGuid(w, 3);
            w.WriteUInt32((uint)ObjectType.Scenery);

            var bitmap = new byte[12];
            bitmap[31 / 8] |= (byte)(1 << (31 % 8));
            w.WriteBytes(bitmap);

            w.WriteByte(1);
            w.WriteUInt32(4);
            w.WriteUInt32(1);
            w.WriteUInt32(0);
            w.WriteInt32(99);
        });

        var proto = ProtoFormat.ParseMemory(bytes);

        await Assert.That(proto.Properties.Count).IsEqualTo(1);
        await Assert.That(proto.Properties[0].Field).IsEqualTo((ObjectField)31);
        await Assert.That(proto.Properties[0].RawBytes.Length).IsEqualTo(0);
        await Assert.That(proto.Properties[0].ParseNote).IsNotNull();
        await Assert.That(proto.Properties[0].ParseNote!).Contains("SAR element data plus bitset count");
    }

    [Test]
    public async Task RoundTrip_MinimalProto_Identical()
    {
        var bytes = BuildMinimalSceneryProto();
        var original = ProtoFormat.ParseMemory(bytes);
        var rewritten = ProtoFormat.WriteToArray(in original);
        var back = ProtoFormat.ParseMemory(rewritten);

        await Assert.That(back.Header.IsPrototype).IsEqualTo(original.Header.IsPrototype);
        await Assert.That(back.Header.GameObjectType).IsEqualTo(original.Header.GameObjectType);
        await Assert.That(back.Properties.Count).IsEqualTo(original.Properties.Count);
        await Assert.That(back.Properties[0].RawBytes.SequenceEqual(original.Properties[0].RawBytes)).IsTrue();
    }

    [Test]
    public async Task Parse_AllZeroBitmap_ZeroProperties()
    {
        // A valid prototype with a completely empty bitmap has no properties.
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidBlocked(w); // ProtoId — OID_TYPE_BLOCKED = prototype
            WriteOidGuid(w, 1); // ObjectId
            w.WriteUInt32((uint)ObjectType.Scenery);
            // Bitmap — 12 bytes all zero
            w.WriteBytes(new byte[12]);
        });

        var proto = ProtoFormat.ParseMemory(bytes);
        await Assert.That(proto.Properties.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_TrapPrototype_CorrectObjectType()
    {
        // Trap uses 12-byte bitmap; verify type dispatches correctly.
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0x77);
            WriteOidBlocked(w); // ProtoId — prototype marker
            WriteOidGuid(w, 2); // ObjectId
            w.WriteUInt32((uint)ObjectType.Trap);
            // Bitmap 12 bytes, bit 21 set (ObjFName)
            var bitmap = new byte[12];
            bitmap[2] = 0x20;
            w.WriteBytes(bitmap);
            w.WriteInt32(55); // ObjFName
        });

        var proto = ProtoFormat.ParseMemory(bytes);
        await Assert.That(proto.Header.GameObjectType).IsEqualTo(ObjectType.Trap);
        await Assert.That(proto.Properties.Count).IsEqualTo(1);
        await Assert.That(proto.Properties[0].GetInt32()).IsEqualTo(55);
    }
}
