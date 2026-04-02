using System.Buffers;
using ArcNET.Core;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="ProtoFormat"/>.</summary>
public sealed class ProtoFormatTests
{
    private static byte[] BuildBytes(Action<SpanWriter> fill)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        fill(w);
        return buf.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Builds a minimal valid PRO binary (prototype — all uint32 fields in Type = 0xFFFFFFFF).
    /// Scenery type, bitmap 12 bytes, bit 21 (ObjFName) set.
    /// </summary>
    private static byte[] BuildMinimalSceneryProto()
    {
        return BuildBytes(w =>
        {
            w.WriteInt32(0x77); // version

            // ProtoId — all 0xFF marks this buffer itself as a prototype definition
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);

            // ObjectId (acts as prototype identity)
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(1);

            // GameObjectType
            w.WriteUInt32((uint)ObjectType.Scenery);

            // NO PropCollectionItems field — prototype omits it
            // (GameObjectHeader.Read detects IsPrototype via ProtoId.IsProto)

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
            // ProtoId — all 0xFF = IsPrototype
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            // ObjectId
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(1);
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
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0xFFFFFFFF);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteUInt32(2);
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
