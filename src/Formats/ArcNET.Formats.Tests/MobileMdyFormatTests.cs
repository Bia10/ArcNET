using System.Buffers;
using System.Buffers.Binary;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MobileMdyFormat"/>.</summary>
public sealed class MobileMdyFormatTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WriteOidGuid(SpanWriter w, Guid g)
    {
        w.WriteInt16(2); // OID_TYPE_GUID
        w.WriteInt16(0);
        w.WriteInt32(0);
        w.WriteBytes(g.ToByteArray());
    }

    private static void WriteOidRef(SpanWriter w, int protoIndex = 1)
    {
        w.WriteInt16(1); // OID_TYPE_A
        w.WriteInt16(0);
        w.WriteInt32(protoIndex);
        w.WriteBytes(new byte[16]);
    }

    /// <summary>
    /// Builds a minimal standard mob record binary (version + full mob header + props).
    /// Wall type, bit 21 (ObjFName = Int32) set, value = 7.
    /// This is the complete binary that MobFormat.Parse / MobileMdyFormat expects.
    /// </summary>
    private static byte[] BuildMinimalWallMobRecord(int version = 0x77)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        w.WriteInt32(version);

        WriteOidRef(w, protoIndex: 1);
        WriteOidGuid(w, Guid.Parse("10000001-0000-0000-0000-000000000001"));

        w.WriteUInt32((uint)ObjectType.Wall);
        w.WriteInt16(1); // PropCollectionItems

        // Bitmap 12 bytes; set bit 21 (ObjFName)
        var bitmap = new byte[12];
        bitmap[2] = 0x20;
        w.WriteBytes(bitmap);

        w.WriteInt32(7); // ObjFName = 7

        return buf.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Builds a minimal v2 character record: magic + stats SAR.
    /// </summary>
    private static byte[] BuildMinimalV2Record()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        // v2 magic
        w.WriteBytes([0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

        // Stats SAR: presence(1) + elemSz(4) + elemCnt(4) + bsId(4) + 28*int32 data + bsCnt(4)
        w.WriteBytes([0x01]); // presence
        w.WriteInt32(4); // elemSz = 4
        w.WriteInt32(28); // elemCnt = 28
        w.WriteInt32(0x4DA5); // bsId (arbitrary value for stats)
        w.WriteBytes(new byte[28 * 4]); // data: 28 int32 all zeros
        w.WriteInt32(0); // bsCnt = 0

        return buf.WrittenSpan.ToArray();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Parse_EmptyData_EmptyFile()
    {
        var file = MobileMdyFormat.ParseMemory(Array.Empty<byte>());

        await Assert.That(file.Records.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_SingleMobRecord_IsMob()
    {
        var bytes = BuildMinimalWallMobRecord();

        var file = MobileMdyFormat.ParseMemory(bytes);

        await Assert.That(file.Records.Count).IsEqualTo(1);
        await Assert.That(file.Records[0].IsMob).IsTrue();
    }

    [Test]
    public async Task Parse_SingleMobRecord_GameObjectTypeCorrect()
    {
        var bytes = BuildMinimalWallMobRecord();

        var file = MobileMdyFormat.ParseMemory(bytes);

        await Assert.That(file.Records[0].Mob!.Header.GameObjectType).IsEqualTo(ObjectType.Wall);
    }

    [Test]
    public async Task Parse_MultipleMobRecords_AllRead()
    {
        // Two separate wall mob records concatenated.
        var record1 = BuildMinimalWallMobRecord(0x77);
        var record2 = BuildMinimalWallMobRecord(0x08);

        var combined = new byte[record1.Length + record2.Length];
        record1.CopyTo(combined, 0);
        record2.CopyTo(combined, record1.Length);

        var file = MobileMdyFormat.ParseMemory(combined);

        await Assert.That(file.Records.Count).IsEqualTo(2);
        await Assert.That(file.Records[0].IsMob).IsTrue();
        await Assert.That(file.Records[1].IsMob).IsTrue();
    }

    [Test]
    public async Task Parse_SentinelDwordSkipped()
    {
        // A 0xFFFFFFFF sentinel dword followed by a valid mob record.
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        w.WriteUInt32(0xFFFFFFFF); // sentinel
        w.WriteBytes(BuildMinimalWallMobRecord());

        var file = MobileMdyFormat.ParseMemory(buf.WrittenSpan.ToArray());

        await Assert.That(file.Records.Count).IsEqualTo(1);
        await Assert.That(file.Records[0].IsMob).IsTrue();
    }

    [Test]
    public async Task Parse_MultipleSentinelsSkipped()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        // Three sentinels, then a mob record, then a sentinel, then another mob.
        w.WriteUInt32(0xFFFFFFFF);
        w.WriteUInt32(0xFFFFFFFF);
        w.WriteUInt32(0xFFFFFFFF);
        w.WriteBytes(BuildMinimalWallMobRecord());
        w.WriteUInt32(0xFFFFFFFF);
        w.WriteBytes(BuildMinimalWallMobRecord(0x08));

        var file = MobileMdyFormat.ParseMemory(buf.WrittenSpan.ToArray());

        await Assert.That(file.Records.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Parse_V2CharacterRecord_IsCharacter()
    {
        var bytes = BuildMinimalV2Record();

        var file = MobileMdyFormat.ParseMemory(bytes);

        await Assert.That(file.Records.Count).IsEqualTo(1);
        await Assert.That(file.Records[0].IsCharacter).IsTrue();
    }

    [Test]
    public async Task Parse_MixedMobAndCharacter_BothRead()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        w.WriteBytes(BuildMinimalWallMobRecord());
        w.WriteBytes(BuildMinimalV2Record());

        var file = MobileMdyFormat.ParseMemory(buf.WrittenSpan.ToArray());

        await Assert.That(file.Records.Count).IsEqualTo(2);
        await Assert.That(file.Records[0].IsMob).IsTrue();
        await Assert.That(file.Records[1].IsCharacter).IsTrue();
    }

    [Test]
    public async Task Mobs_ConvenienceView_OnlyMobs()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        w.WriteBytes(BuildMinimalWallMobRecord());
        w.WriteBytes(BuildMinimalV2Record());
        w.WriteBytes(BuildMinimalWallMobRecord(0x08));

        var file = MobileMdyFormat.ParseMemory(buf.WrittenSpan.ToArray());

        var mobs = file.Mobs.ToList();
        var chars = file.Characters.ToList();

        await Assert.That(mobs.Count).IsEqualTo(2);
        await Assert.That(chars.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RoundTrip_SingleMob_Identical()
    {
        var bytes = BuildMinimalWallMobRecord();

        var file = MobileMdyFormat.ParseMemory(bytes);
        var rewritten = MobileMdyFormat.WriteToArray(in file);

        await Assert.That(rewritten.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task RoundTrip_V2Character_RawBytesPreserved()
    {
        var v2Bytes = BuildMinimalV2Record();

        var file = MobileMdyFormat.ParseMemory(v2Bytes);
        var rewritten = MobileMdyFormat.WriteToArray(in file);

        await Assert.That(rewritten.SequenceEqual(v2Bytes)).IsTrue();
    }

    [Test]
    public async Task RoundTrip_MultipleMobs_Identical()
    {
        var record1 = BuildMinimalWallMobRecord(0x77);
        var record2 = BuildMinimalWallMobRecord(0x08);

        var combined = new byte[record1.Length + record2.Length];
        record1.CopyTo(combined, 0);
        record2.CopyTo(combined, record1.Length);

        var file = MobileMdyFormat.ParseMemory(combined);
        var rewritten = MobileMdyFormat.WriteToArray(in file);

        await Assert.That(rewritten.SequenceEqual(combined)).IsTrue();
    }

    [Test]
    public async Task Parse_VersionZero_Version77_BothRecognised()
    {
        // Verify both supported mob version values decode without error.
        var mobV08 = BuildMinimalWallMobRecord(0x08);
        var mobV77 = BuildMinimalWallMobRecord(0x77);

        var fileV08 = MobileMdyFormat.ParseMemory(mobV08);
        var fileV77 = MobileMdyFormat.ParseMemory(mobV77);

        await Assert.That(fileV08.Records.Count).IsEqualTo(1);
        await Assert.That(fileV77.Records.Count).IsEqualTo(1);
    }
}
