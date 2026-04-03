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
    // ── ObjectID wire helpers (24 bytes each) ─────────────────────────────────
    // struct ObjectID { int16_t type; int16_t pad2; int pad4; TigGuid g; }
    // OID_TYPE_BLOCKED = -1: marks a prototype definition (IsProto == true)

    private static void WriteOidBlocked(SpanWriter w)
    {
        w.WriteInt16(-1); // OID_TYPE_BLOCKED
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(new byte[16]);
    }

    private static void WriteOidRef(SpanWriter w, int protoIndex = 1)
    {
        w.WriteInt16(1); // OID_TYPE_A
        w.WriteInt16(0);
        w.WriteInt32(protoIndex);
        w.WriteBytes(new byte[16]);
    }

    private static void WriteOidGuid(SpanWriter w, Guid g)
    {
        w.WriteInt16(2); // OID_TYPE_GUID
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(g.ToByteArray());
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static byte[] BuildPrototypeHeaderBytes(ObjectType type, Guid protoGuid, Guid objectGuid)
    {
        var buf = new ArrayBufferWriter<byte>(96);
        var writer = new SpanWriter(buf);

        // version
        writer.WriteInt32(0x77);

        // ProtoId — marked as prototype: OidType = OID_TYPE_BLOCKED (-1)
        WriteOidBlocked(writer);

        // ObjectId
        WriteOidGuid(writer, objectGuid);

        // Type
        writer.WriteUInt32((uint)type);

        // No PropCollectionItems — IsPrototype == true

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
        var original = BuildPrototypeHeaderBytes(
            ObjectType.Generic,
            Guid.Parse("0000ABCD-0000-0000-0000-000000000000"),
            Guid.Parse("0000EF01-0000-0000-0000-000000000000")
        );
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task PrototypeHeader_Npc_RoundTrips()
    {
        var original = BuildPrototypeHeaderBytes(
            ObjectType.Npc,
            Guid.Parse("00000101-0000-0000-0000-000000000000"),
            Guid.Parse("00000202-0000-0000-0000-000000000000")
        );
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task PrototypeHeader_Wall_RoundTrips()
    {
        var original = BuildPrototypeHeaderBytes(
            ObjectType.Wall,
            Guid.Parse("000000FF-0000-0000-0000-000000000000"),
            Guid.Parse("0000FF00-0000-0000-0000-000000000000")
        );
        var actual = RoundTrip(original);
        await Assert.That(actual.SequenceEqual(original)).IsTrue();
    }

    [Test]
    public async Task NonPrototypeHeader_RoundTrips()
    {
        // For a non-prototype header: OidType != -1 → PropCollectionItems present
        var buf = new ArrayBufferWriter<byte>(96);
        var writer = new SpanWriter(buf);

        writer.WriteInt32(0x77);

        // ProtoId — non-prototype: references a Container prototype
        WriteOidRef(writer, protoIndex: 5);

        // ObjectId
        WriteOidGuid(writer, Guid.Parse("00000042-0000-0000-0000-000000000000"));

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
        var bytes = new byte[96];
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
        var original = BuildPrototypeHeaderBytes(
            ObjectType.Generic,
            Guid.Parse("00000001-0000-0000-0000-000000000000"),
            Guid.Parse("00000002-0000-0000-0000-000000000000")
        );

        // Bitmap offset for a prototype Generic header:
        // version(4) + ProtoId(24) + ObjectId(24) + ObjectType(4) = 56
        // No PropCollectionItems because IsPrototype=true.
        const int bitmapOffset = 56;
        original[bitmapOffset] = 0b10101010;
        original[bitmapOffset + 1] = 0b11001100;

        var actual = RoundTrip(original);
        await Assert.That(actual[bitmapOffset]).IsEqualTo(original[bitmapOffset]);
        await Assert.That(actual[bitmapOffset + 1]).IsEqualTo(original[bitmapOffset + 1]);
    }
}
