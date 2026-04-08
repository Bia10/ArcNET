namespace ArcNET.Formats.Tests;

public class CharacterMdyRecordTests
{
    // ── Wire-format helpers ───────────────────────────────────────────────────

    // Builds a minimal SAR packet: presence(1) + elemSz(4) + elemCnt(4) + bsId(4) + data + bsCnt(4)
    private static byte[] Sar(int elemSz, int elemCnt, int bsId, byte[] data)
    {
        var buf = new byte[13 + data.Length + 4];
        buf[0] = 0x01;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), elemSz);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), elemCnt);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), bsId);
        data.CopyTo(buf, 13);
        // bsCnt = 0 at buf[13 + data.Length]
        return buf;
    }

    private static byte[] IntBytes(int v)
    {
        var b = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(b, v);
        return b;
    }

    private static byte[] IntArray(int count, int fill = 0)
    {
        var b = new byte[count * 4];
        if (fill != 0)
            for (var i = 0; i < count; i++)
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(i * 4, 4), fill);
        return b;
    }

    /// <summary>
    /// Builds a minimal full v2 record: magic + stats SAR + basicSkills + techSkills
    /// + spellTech + gold-amount SAR.
    /// </summary>
    private static byte[] BuildRecord(int gold, int[] stats)
    {
        // v2 magic
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        // stats SAR: elemSz=4, elemCnt=28, arbitrary bsId
        var statsData = new byte[28 * 4];
        for (var i = 0; i < Math.Min(stats.Length, 28); i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(i * 4, 4), stats[i]);
        var statsSar = Sar(4, 28, 0x4DA5, statsData);

        // basicSkills SAR: elemSz=4, elemCnt=12
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));

        // techSkills SAR: elemSz=4, elemCnt=4
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));

        // spellTech SAR: elemSz=4, elemCnt=25
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));

        // gold SAR: elemSz=4, elemCnt=1, bsId=0x4B13
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(gold));

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar];
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Parse_WithGoldSar_ReadsGoldCorrectly()
    {
        var bytes = BuildRecord(gold: 999, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Gold).IsEqualTo(999);
    }

    [Test]
    public async Task Parse_RecordWithGold_RawBytesContainsGoldSar()
    {
        const int gold = 1234;
        var bytes = BuildRecord(gold, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out var consumed);

        // consumed must include the gold SAR (whole record)
        await Assert.That(consumed).IsEqualTo(bytes.Length);
        await Assert.That(rec.RawBytes.Length).IsEqualTo(bytes.Length);
        await Assert.That(rec.Gold).IsEqualTo(gold);
    }

    [Test]
    public async Task WithGold_PatchesCorrectly()
    {
        var bytes = BuildRecord(gold: 50, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithGold(7777);

        await Assert.That(patched.Gold).IsEqualTo(7777);
        // Original unchanged
        await Assert.That(rec.Gold).IsEqualTo(50);
    }

    [Test]
    public async Task WithGold_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecord(gold: 100, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithGold(5000);

        // Re-parse the patched RawBytes
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.Gold).IsEqualTo(5000);
    }

    [Test]
    public async Task WithGold_RawBytesLengthUnchanged()
    {
        var bytes = BuildRecord(gold: 42, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithGold(99999);

        await Assert.That(patched.RawBytes.Length).IsEqualTo(rec.RawBytes.Length);
    }

    [Test]
    public async Task Parse_StatsPreservedWhenGoldPresent()
    {
        var stats = new int[28];
        stats[0] = 12; // STR
        stats[17] = 8; // LEVEL
        var bytes = BuildRecord(gold: 77, stats);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Stats[0]).IsEqualTo(12);
        await Assert.That(rec.Stats[17]).IsEqualTo(8);
    }

    /// <summary>
    /// Builds a minimal full v2 record that also includes the game-statistics SAR
    /// (bsId=0x4D68, 11 INT32 elements) so Silver and TotalKills can be tested.
    /// </summary>
    private static byte[] BuildRecordWithStats(int gold, int arrows, int totalKills, int[] charStats)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var statsData = new byte[28 * 4];
        for (var i = 0; i < Math.Min(charStats.Length, 28); i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(i * 4, 4), charStats[i]);
        var statsSar = Sar(4, 28, 0x4DA5, statsData);
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(gold));

        // game-statistics SAR: bsId=0x4D68, 11 INT32 elements.
        // element[0]=TotalKills, element[8]=Arrows, rest=0.
        var gameStatsData = new byte[11 * 4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(gameStatsData.AsSpan(0 * 4, 4), totalKills);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(gameStatsData.AsSpan(8 * 4, 4), arrows);
        var gameStatsSar = Sar(4, 11, 0x4D68, gameStatsData);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. gameStatsSar];
    }

    [Test]
    public async Task Parse_WithGameStatsSar_ReadsArrowsCorrectly()
    {
        var bytes = BuildRecordWithStats(gold: 50, arrows: 123, totalKills: 42, charStats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Arrows).IsEqualTo(123);
    }

    [Test]
    public async Task Parse_WithGameStatsSar_ReadsTotalKillsCorrectly()
    {
        var bytes = BuildRecordWithStats(gold: 50, arrows: 10, totalKills: 77, charStats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.TotalKills).IsEqualTo(77);
    }

    [Test]
    public async Task WithArrows_PatchesCorrectly()
    {
        var bytes = BuildRecordWithStats(gold: 50, arrows: 10, totalKills: 5, charStats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithArrows(500);

        await Assert.That(patched.Arrows).IsEqualTo(500);
        await Assert.That(rec.Arrows).IsEqualTo(10); // original unchanged
    }

    [Test]
    public async Task WithTotalKills_PatchesCorrectly()
    {
        var bytes = BuildRecordWithStats(gold: 50, arrows: 10, totalKills: 5, charStats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithTotalKills(999);

        await Assert.That(patched.TotalKills).IsEqualTo(999);
        await Assert.That(rec.TotalKills).IsEqualTo(5); // original unchanged
    }

    [Test]
    public async Task WithArrows_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithStats(gold: 50, arrows: 10, totalKills: 5, charStats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithArrows(888);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.Arrows).IsEqualTo(888);
        await Assert.That(reparsed.Gold).IsEqualTo(50); // gold unaffected
    }

    [Test]
    public async Task WithArrows_RawBytesLengthUnchanged()
    {
        var bytes = BuildRecordWithStats(gold: 50, arrows: 10, totalKills: 5, charStats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithArrows(99999);

        await Assert.That(patched.RawBytes.Length).IsEqualTo(rec.RawBytes.Length);
    }

    [Test]
    public async Task Parse_WithoutGameStatsSar_ArrowsAndKillsAreZero()
    {
        // Record with the four primary SARs only — no game-stats SAR
        var bytes = BuildRecord(gold: 50, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Arrows).IsEqualTo(0);
        await Assert.That(rec.TotalKills).IsEqualTo(0);
    }

    // ── Portrait ──────────────────────────────────────────────────────────────

    private static byte[] BuildRecordWithPortrait(int portraitIndex, int[] stats)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsData = new byte[28 * 4];
        for (var i = 0; i < Math.Min(stats.Length, 28); i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(i * 4, 4), stats[i]);
        var statsSar = Sar(4, 28, 0x4DA5, statsData);
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));

        // portrait SAR: bsId=0x4DA4, 3 INT32 elements: [MaxFollowers, PortraitIndex, 0]
        var portraitData = new byte[3 * 4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(portraitData.AsSpan(0, 4), 2);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(portraitData.AsSpan(4, 4), portraitIndex);
        var portraitSar = Sar(4, 3, 0x4DA4, portraitData);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. portraitSar];
    }

    [Test]
    public async Task Parse_WithPortraitSar_ReadsPortraitIndexCorrectly()
    {
        var bytes = BuildRecordWithPortrait(portraitIndex: 9, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.PortraitIndex).IsEqualTo(9);
    }

    [Test]
    public async Task WithPortraitIndex_PatchesCorrectly()
    {
        var bytes = BuildRecordWithPortrait(portraitIndex: 9, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithPortraitIndex(12);

        await Assert.That(patched.PortraitIndex).IsEqualTo(12);
        await Assert.That(rec.PortraitIndex).IsEqualTo(9); // original unchanged
    }

    [Test]
    public async Task WithPortraitIndex_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithPortrait(portraitIndex: 5, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithPortraitIndex(7);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.PortraitIndex).IsEqualTo(7);
    }

    [Test]
    public async Task Parse_WithoutPortraitSar_PortraitIndexIsMinusOne()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.PortraitIndex).IsEqualTo(-1);
    }

    // ── PC Name ───────────────────────────────────────────────────────────────

    private static byte[] BuildRecordWithName(string name, int[] stats)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsData = new byte[28 * 4];
        for (var i = 0; i < Math.Min(stats.Length, 28); i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(statsData.AsSpan(i * 4, 4), stats[i]);
        var statsSar = Sar(4, 28, 0x4DA5, statsData);
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));

        // Name field: 01 [uint32 len] [ascii bytes]
        var nameBytes = System.Text.Encoding.ASCII.GetBytes(name);
        var nameField = new byte[1 + 4 + nameBytes.Length];
        nameField[0] = 0x01;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(nameField.AsSpan(1, 4), nameBytes.Length);
        nameBytes.CopyTo(nameField, 5);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. nameField];
    }

    [Test]
    public async Task Parse_WithNameField_ReadsNameCorrectly()
    {
        var bytes = BuildRecordWithName("ArciMagus", stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Name).IsEqualTo("ArciMagus");
    }

    [Test]
    public async Task Parse_WithoutNameField_NameIsNull()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Name).IsNull();
    }

    [Test]
    public async Task Parse_WithoutGoldSar_GoldIsZero()
    {
        // Record with all four SARs but NO gold SAR
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var bytes = (byte[])[.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar];

        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Gold).IsEqualTo(0);
        await Assert.That(rec.HasCompleteData).IsTrue();
    }
}
