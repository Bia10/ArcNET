using System.Buffers;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.GameObjects.Tests;

/// <summary>
/// Verifies that <see cref="GameObjectHeader"/> round-trips correctly through
/// <c>Read → Write → Read</c> using hand-crafted binary fixtures.
/// </summary>
public class GameObjectHeaderRoundTripTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static byte[] BuildPrototypeHeaderBytes(ObjectType type, uint protoGuid, uint objectGuid)
    {
        var buf = new ArrayBufferWriter<byte>(64);
        var writer = new SpanWriter(buf);

        // version
        writer.WriteInt32(0x77);

        // ProtoId — marked as prototype: Type = 0xFFFFFFFF
        writer.WriteUInt32(0xFFFFFFFF);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(protoGuid);

        // ObjectId
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(objectGuid);

        // Type
        writer.WriteUInt32((uint)type);

        // No PropCollectionItems — IsPrototype == true (ProtoId.Type = 0xFFFFFFFF)

        // Bitmap — all zeros (length depends on type)
        var bitmapLen = type switch
        {
            ObjectType.Generic => 16,
            ObjectType.Npc => 20,
            ObjectType.Wall => 12,
            _ => 16,
        };
        var bitmapBytes = new byte[bitmapLen];
        writer.WriteBytes(bitmapBytes);

        return buf.WrittenMemory.ToArray();
    }

    private static byte[] RoundTrip(byte[] original)
    {
        // Read
        var readBuf = new SpanReader(original);
        var header = GameObjectHeader.Read(ref readBuf);

        // Write
        var writeBuf = new ArrayBufferWriter<byte>(original.Length);
        var writer = new SpanWriter(writeBuf);
        header.Write(ref writer);

        return writeBuf.WrittenMemory.ToArray();
    }

    // ── tests ───────────────────────────────────────────────────────────────

    [Test]
    public async Task PrototypeHeader_Generic_RoundTrips()
    {
        var original = BuildPrototypeHeaderBytes(ObjectType.Generic, 0xABCD, 0xEF01);
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task PrototypeHeader_Npc_RoundTrips()
    {
        var original = BuildPrototypeHeaderBytes(ObjectType.Npc, 0x0101, 0x0202);
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task PrototypeHeader_Wall_RoundTrips()
    {
        var original = BuildPrototypeHeaderBytes(ObjectType.Wall, 0x00FF, 0xFF00);
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task NonPrototypeHeader_RoundTrips()
    {
        // For a non-prototype header: ProtoId.Type != 0xFFFFFFFF → PropCollectionItems present
        var buf = new ArrayBufferWriter<byte>(64);
        var writer = new SpanWriter(buf);

        writer.WriteInt32(0x77);

        // ProtoId — not a prototype: Type = 0x00000005 (e.g. Container)
        writer.WriteUInt32(0x00000005);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x0001);

        // ObjectId
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x00);
        writer.WriteUInt32(0x0042);

        // Type = Container
        writer.WriteUInt32((uint)ObjectType.Container);

        // PropCollectionItems (present because non-prototype)
        writer.WriteInt16(3);

        // Bitmap (12 bytes for Container)
        writer.WriteBytes(new byte[12]);

        var original = buf.WrittenMemory.ToArray();
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task Read_InvalidVersion_Throws()
    {
        var bytes = new byte[48];
        bytes[0] = 0x42; // bad version — not 0x77

        var threw = false;
        try
        {
            var r = new SpanReader(bytes);
            GameObjectHeader.Read(ref r);
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Bitmap_BitsPreservedAfterRoundTrip()
    {
        var original = BuildPrototypeHeaderBytes(ObjectType.Generic, 1, 2);

        // Flip some bitmap bits (bitmap starts at byte offset: 4+16+16+4 = 40)
        original[40] = 0b10101010;
        original[41] = 0b11001100;

        var actual = RoundTrip(original);
        await Assert.That(actual[40]).IsEqualTo(original[40]);
        await Assert.That(actual[41]).IsEqualTo(original[41]);
    }
}
