using System.Buffers;
using System.Buffers.Binary;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MobileMdFormat"/>.</summary>
public sealed class MobileMdFormatTests
{
    // ── Wire-format constants ─────────────────────────────────────────────────

    private const uint StartMarker = 0x12344321u;
    private const uint EndMarker = 0x23455432u;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes an ObjectID in OID_TYPE_GUID format (type=2, pad=0, 16-byte Guid).
    /// </summary>
    private static void WriteOidGuid(SpanWriter w, Guid g)
    {
        w.WriteInt16(2); // OID_TYPE_GUID
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(g.ToByteArray());
    }

    /// <summary>
    /// Writes an ObjectID in OID_TYPE_A format (type=1, pad=0, protoIndex, zeros).
    /// </summary>
    private static void WriteOidRef(SpanWriter w, int protoIndex = 1)
    {
        w.WriteInt16(1); // OID_TYPE_A
        w.WriteInt16(0);
        w.WriteInt32(protoIndex);
        w.WriteBytes(new byte[16]);
    }

    /// <summary>
    /// Builds a minimal standard-format Wall mob body (without version prefix).
    /// The version is passed separately in the mobile.md envelope.
    /// Wire: ProtoId(24) + ObjectId(24) + type(4) + propCollItems(2) + bitmap(12) + props.
    /// Bit 21 (ObjFName) is set; its payload is a single Int32.
    /// </summary>
    private static byte[] BuildWallMobBody(int version = 0x77)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        // version — needed as first byte for MobFormat.Parse when prepended.
        w.WriteInt32(version);

        // ProtoId (24 bytes) — OID_TYPE_A reference to proto 1
        WriteOidRef(w, protoIndex: 1);

        // ObjectId (24 bytes)
        WriteOidGuid(w, Guid.Parse("00000001-0000-0000-0000-000000000000"));

        // GameObjectType = Wall = 0
        w.WriteUInt32((uint)ObjectType.Wall);

        // PropCollectionItems
        w.WriteInt16(1);

        // Bitmap (12 bytes for Wall); set bit 21 (ObjFName)
        var bitmap = new byte[12];
        bitmap[2] = 0x20; // byte index 2 = bits 16-23; bit 5 of byte = bit index 21
        w.WriteBytes(bitmap);

        // Property payload: ObjFName (Int32) = 99
        w.WriteInt32(99);

        return buf.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Returns the rawMobBytes portion of <see cref="BuildWallMobBody"/> — the bytes
    /// AFTER the 4-byte version prefix (those go outside the START/END envelope).
    /// </summary>
    private static byte[] WallRawMobBytes() => BuildWallMobBody()[4..];

    /// <summary>
    /// Builds a complete mobile.md binary stream containing one record.
    /// Layout: OID(24) + version(4) + START(4) + rawMobBytes + END(4).
    /// </summary>
    private static byte[] BuildSingleRecord(GameObjectGuid oid, int version, byte[] rawMobBytes)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        oid.Write(ref w);
        w.WriteInt32(version);
        w.WriteUInt32(StartMarker);
        w.WriteBytes(rawMobBytes);
        w.WriteUInt32(EndMarker);

        return buf.WrittenSpan.ToArray();
    }

    private static GameObjectGuid MakeOid(Guid g) => new(2, 0, 0, g);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Parse_EmptyData_EmptyFile()
    {
        var file = MobileMdFormat.ParseMemory(Array.Empty<byte>());

        await Assert.That(file.Records.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_SingleRecord_MapObjectIdCorrect()
    {
        var g = Guid.Parse("AABBCCDD-0000-0000-0000-000000000001");
        var oid = MakeOid(g);
        var bytes = BuildSingleRecord(oid, 0x77, WallRawMobBytes());

        var file = MobileMdFormat.ParseMemory(bytes);

        await Assert.That(file.Records.Count).IsEqualTo(1);
        await Assert.That(file.Records[0].MapObjectId.Id).IsEqualTo(g);
    }

    [Test]
    public async Task Parse_SingleRecord_VersionPreserved()
    {
        var oid = MakeOid(Guid.NewGuid());
        var bytes = BuildSingleRecord(oid, 0x08, WallRawMobBytes());

        var file = MobileMdFormat.ParseMemory(bytes);

        await Assert.That(file.Records[0].Version).IsEqualTo(0x08);
    }

    [Test]
    public async Task Parse_SingleRecord_RawMobBytesMatchInput()
    {
        var oid = MakeOid(Guid.NewGuid());
        var rawMob = WallRawMobBytes();
        var bytes = BuildSingleRecord(oid, 0x77, rawMob);

        var file = MobileMdFormat.ParseMemory(bytes);

        await Assert.That(file.Records[0].RawMobBytes.SequenceEqual(rawMob)).IsTrue();
    }

    [Test]
    public async Task Parse_BadVersion_Throws()
    {
        var oid = MakeOid(Guid.NewGuid());

        // Write a record with version 0x01 (invalid).
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        oid.Write(ref w);
        w.WriteInt32(0x01); // invalid version
        w.WriteUInt32(StartMarker);
        w.WriteBytes(WallRawMobBytes());
        w.WriteUInt32(EndMarker);

        Assert.Throws<InvalidDataException>(() => MobileMdFormat.ParseMemory(buf.WrittenSpan.ToArray()));
    }

    [Test]
    public async Task Parse_MissingStartMarker_Throws()
    {
        var oid = MakeOid(Guid.NewGuid());

        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        oid.Write(ref w);
        w.WriteInt32(0x77);
        w.WriteUInt32(0xDEADBEEF); // wrong sentinel
        w.WriteBytes(WallRawMobBytes());
        w.WriteUInt32(EndMarker);

        Assert.Throws<InvalidDataException>(() => MobileMdFormat.ParseMemory(buf.WrittenSpan.ToArray()));
    }

    [Test]
    public async Task Parse_MultipleRecords_AllRead()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        var rawMob = WallRawMobBytes();

        var oid1 = MakeOid(Guid.Parse("00000001-0000-0000-0000-000000000001"));
        var oid2 = MakeOid(Guid.Parse("00000002-0000-0000-0000-000000000002"));
        var oid3 = MakeOid(Guid.Parse("00000003-0000-0000-0000-000000000003"));

        // Three adjacent records.
        foreach (var oid in new[] { oid1, oid2, oid3 })
        {
            oid.Write(ref w);
            w.WriteInt32(0x77);
            w.WriteUInt32(StartMarker);
            w.WriteBytes(rawMob);
            w.WriteUInt32(EndMarker);
        }

        var file = MobileMdFormat.ParseMemory(buf.WrittenSpan.ToArray());

        await Assert.That(file.Records.Count).IsEqualTo(3);
        await Assert.That(file.Records[0].MapObjectId.Id).IsEqualTo(oid1.Id);
        await Assert.That(file.Records[1].MapObjectId.Id).IsEqualTo(oid2.Id);
        await Assert.That(file.Records[2].MapObjectId.Id).IsEqualTo(oid3.Id);
    }

    [Test]
    public async Task RoundTrip_SingleRecord_Identical()
    {
        var oid = MakeOid(Guid.Parse("CAFEBABE-0000-0000-0000-000000000001"));
        var bytes = BuildSingleRecord(oid, 0x77, WallRawMobBytes());

        var file = MobileMdFormat.ParseMemory(bytes);
        var rewritten = MobileMdFormat.WriteToArray(in file);

        await Assert.That(rewritten.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task RoundTrip_MultipleRecords_Identical()
    {
        var rawMob = WallRawMobBytes();
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        for (var i = 1; i <= 4; i++)
        {
            var oid = MakeOid(new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            oid.Write(ref w);
            w.WriteInt32(0x77);
            w.WriteUInt32(StartMarker);
            w.WriteBytes(rawMob);
            w.WriteUInt32(EndMarker);
        }

        var bytes = buf.WrittenSpan.ToArray();
        var file = MobileMdFormat.ParseMemory(bytes);
        var rewritten = MobileMdFormat.WriteToArray(in file);

        await Assert.That(rewritten.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task Parse_SingleRecord_DataDecodedAsWallObject()
    {
        // The wall mob body we supply should decode successfully → Data is non-null.
        var oid = MakeOid(Guid.NewGuid());
        var bytes = BuildSingleRecord(oid, 0x77, WallRawMobBytes());

        var file = MobileMdFormat.ParseMemory(bytes);
        var record = file.Records[0];

        await Assert.That(record.Data).IsNotNull();
        await Assert.That(record.Data!.Header.GameObjectType).IsEqualTo(ObjectType.Wall);
    }

    [Test]
    public async Task Parse_GarbageMobBody_DataIsNull_ParseNoteSet()
    {
        // Mob body that will fail MobFormat.Parse (all zeros → invalid object type 0 may or
        // may not succeed; use a version-0 body that is clearly truncated instead).
        var oid = MakeOid(Guid.NewGuid());

        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        oid.Write(ref w);
        w.WriteInt32(0x77);
        w.WriteUInt32(StartMarker);

        // 4 bytes of garbage — too short to be a valid mob body.
        w.WriteBytes([0xFF, 0xFF, 0xFF, 0xFF]);

        w.WriteUInt32(EndMarker);

        var file = MobileMdFormat.ParseMemory(buf.WrittenSpan.ToArray());

        await Assert.That(file.Records.Count).IsEqualTo(1);
        await Assert.That(file.Records[0].Data).IsNull();
        await Assert.That(file.Records[0].ParseNote).IsNotNull();
    }
}
