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

    // ── MaxFollowers (bsId=0x4DA4[0]) ────────────────────────────────────────

    [Test]
    public async Task Parse_WithPortraitSar_ReadsMaxFollowersCorrectly()
    {
        // BuildRecordWithPortrait sets MaxFollowers=2. Verify element [0] is exposed.
        var bytes = BuildRecordWithPortrait(portraitIndex: 5, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.MaxFollowers).IsEqualTo(2);
    }

    [Test]
    public async Task WithMaxFollowers_PatchesCorrectly()
    {
        var bytes = BuildRecordWithPortrait(portraitIndex: 5, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithMaxFollowers(6);

        await Assert.That(patched.MaxFollowers).IsEqualTo(6);
        await Assert.That(rec.MaxFollowers).IsEqualTo(2); // original unchanged
    }

    [Test]
    public async Task WithMaxFollowers_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithPortrait(portraitIndex: 3, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithMaxFollowers(4);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.MaxFollowers).IsEqualTo(4);
        await Assert.That(reparsed.PortraitIndex).IsEqualTo(3); // portrait unaffected
    }

    [Test]
    public async Task Parse_WithoutPortraitSar_MaxFollowersIsMinusOne()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.MaxFollowers).IsEqualTo(-1);
    }

    // ── WithName ─────────────────────────────────────────────────────────────

    [Test]
    public async Task WithName_ChangesName()
    {
        var bytes = BuildRecordWithName("OldName", stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithName("NewName");

        await Assert.That(patched.Name).IsEqualTo("NewName");
        await Assert.That(rec.Name).IsEqualTo("OldName"); // original unchanged
    }

    [Test]
    public async Task WithName_ShorterName_ShrinksRawBytes()
    {
        var bytes = BuildRecordWithName("LongName123", stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithName("A");

        await Assert.That(patched.RawBytes.Length).IsLessThan(rec.RawBytes.Length);
    }

    [Test]
    public async Task WithName_LongerName_GrowsRawBytes()
    {
        var bytes = BuildRecordWithName("A", stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithName("LongerName");

        await Assert.That(patched.RawBytes.Length).IsGreaterThan(rec.RawBytes.Length);
    }

    [Test]
    public async Task WithName_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithName("Percival", stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithName("Torian");

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.Name).IsEqualTo("Torian");
    }

    [Test]
    public async Task WithName_NullName_ReturnsUnchanged()
    {
        var bytes = BuildRecordWithName("Unchanged", stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithName(null);

        await Assert.That(patched.Name).IsEqualTo("Unchanged");
        await Assert.That(ReferenceEquals(patched, rec)).IsTrue();
    }

    // ── HP Damage SAR (bsId=0x4046, INT32[4]) ────────────────────────────────
    // These SARs appear in the pre-stat region (between the v2 magic and the stats SAR).

    private static byte[] BuildRecordWithHpSar(int acBonus, int hpPtsBonus, int hpAdj, int hpDamage)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var hpData = new byte[4 * 4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(hpData.AsSpan(0, 4), acBonus);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(hpData.AsSpan(4, 4), hpPtsBonus);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(hpData.AsSpan(8, 4), hpAdj);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(hpData.AsSpan(12, 4), hpDamage);
        var hpSar = Sar(4, 4, 0x4046, hpData);
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));
        // HP SAR is in the pre-stat region (before the stats SAR).
        return [.. magic, .. hpSar, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar];
    }

    [Test]
    public async Task Parse_WithHpSar_ReadsHpDamageCorrectly()
    {
        var bytes = BuildRecordWithHpSar(acBonus: 0, hpPtsBonus: 0, hpAdj: 0, hpDamage: 25);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.HpDamage).IsEqualTo(25);
    }

    [Test]
    public async Task Parse_WithHpSar_ReadsAllElements()
    {
        var bytes = BuildRecordWithHpSar(acBonus: 1, hpPtsBonus: 2, hpAdj: 3, hpDamage: 4);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var raw = rec.HpDamageRaw;
        await Assert.That(raw).IsNotNull();
        await Assert.That(raw![0]).IsEqualTo(1);
        await Assert.That(raw[1]).IsEqualTo(2);
        await Assert.That(raw[2]).IsEqualTo(3);
        await Assert.That(raw[3]).IsEqualTo(4);
    }

    [Test]
    public async Task Parse_WithoutHpSar_HpDamageIsZero()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.HpDamage).IsEqualTo(0);
        await Assert.That(rec.HpDamageRaw).IsNull();
    }

    [Test]
    public async Task WithHpDamageValue_PatchesCorrectly()
    {
        var bytes = BuildRecordWithHpSar(0, 0, 0, hpDamage: 10);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithHpDamageValue(50);

        await Assert.That(patched.HpDamage).IsEqualTo(50);
        await Assert.That(rec.HpDamage).IsEqualTo(10); // original unchanged
    }

    [Test]
    public async Task WithHpDamageValue_PreservesOtherElements()
    {
        var bytes = BuildRecordWithHpSar(acBonus: 1, hpPtsBonus: 2, hpAdj: 3, hpDamage: 10);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithHpDamageValue(99);
        var raw = patched.HpDamageRaw!;

        await Assert.That(raw[0]).IsEqualTo(1);
        await Assert.That(raw[1]).IsEqualTo(2);
        await Assert.That(raw[2]).IsEqualTo(3);
        await Assert.That(raw[3]).IsEqualTo(99);
    }

    [Test]
    public async Task WithHpDamageValue_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithHpSar(0, 0, 0, hpDamage: 10);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithHpDamageValue(77);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.HpDamage).IsEqualTo(77);
    }

    [Test]
    public async Task WithHpDamage_PatchesAllElements()
    {
        var bytes = BuildRecordWithHpSar(0, 0, 0, 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithHpDamage([5, 6, 7, 8]);
        var raw = patched.HpDamageRaw!;

        await Assert.That(raw[0]).IsEqualTo(5);
        await Assert.That(raw[1]).IsEqualTo(6);
        await Assert.That(raw[2]).IsEqualTo(7);
        await Assert.That(raw[3]).IsEqualTo(8);
    }

    // ── Fatigue Damage SAR (bsId=0x423E, INT32[4]) ───────────────────────────

    private static byte[] BuildRecordWithFatigueSar(int ptsBonus, int adj, int fatigueDamage, int unknown)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var fatData = new byte[4 * 4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(fatData.AsSpan(0, 4), ptsBonus);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(fatData.AsSpan(4, 4), adj);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(fatData.AsSpan(8, 4), fatigueDamage);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(fatData.AsSpan(12, 4), unknown);
        var fatSar = Sar(4, 4, 0x423E, fatData);
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));
        // Fatigue SAR is in the pre-stat region (before the stats SAR).
        return [.. magic, .. fatSar, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar];
    }

    [Test]
    public async Task Parse_WithFatigueSar_ReadsFatigueDamageCorrectly()
    {
        var bytes = BuildRecordWithFatigueSar(ptsBonus: 0, adj: 0, fatigueDamage: 15, unknown: 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.FatigueDamage).IsEqualTo(15);
    }

    [Test]
    public async Task Parse_WithoutFatigueSar_FatigueDamageIsZero()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.FatigueDamage).IsEqualTo(0);
        await Assert.That(rec.FatigueDamageRaw).IsNull();
    }

    [Test]
    public async Task WithFatigueDamageValue_PatchesCorrectly()
    {
        var bytes = BuildRecordWithFatigueSar(0, 0, fatigueDamage: 5, 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithFatigueDamageValue(30);

        await Assert.That(patched.FatigueDamage).IsEqualTo(30);
        await Assert.That(rec.FatigueDamage).IsEqualTo(5); // original unchanged
    }

    [Test]
    public async Task WithFatigueDamageValue_PreservesOtherElements()
    {
        var bytes = BuildRecordWithFatigueSar(ptsBonus: 1, adj: 2, fatigueDamage: 3, unknown: 4);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithFatigueDamageValue(99);
        var raw = patched.FatigueDamageRaw!;

        await Assert.That(raw[0]).IsEqualTo(1);
        await Assert.That(raw[1]).IsEqualTo(2);
        await Assert.That(raw[2]).IsEqualTo(99);
        await Assert.That(raw[3]).IsEqualTo(4);
    }

    [Test]
    public async Task WithFatigueDamageValue_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithFatigueSar(0, 0, fatigueDamage: 5, 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithFatigueDamageValue(42);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.FatigueDamage).IsEqualTo(42);
    }

    [Test]
    public async Task WithFatigueDamage_PatchesAllElements()
    {
        var bytes = BuildRecordWithFatigueSar(0, 0, 0, 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithFatigueDamage([10, 20, 30, 40]);
        var raw = patched.FatigueDamageRaw!;

        await Assert.That(raw[0]).IsEqualTo(10);
        await Assert.That(raw[1]).IsEqualTo(20);
        await Assert.That(raw[2]).IsEqualTo(30);
        await Assert.That(raw[3]).IsEqualTo(40);
    }

    // ── Position / AI SAR (bsId=0x4DA3, INT32[3]) ────────────────────────────

    private static byte[] BuildRecordWithPositionAiSar(int currentAid, int location, int offsetX)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var posData = new byte[3 * 4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(posData.AsSpan(0, 4), currentAid);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(posData.AsSpan(4, 4), location);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(posData.AsSpan(8, 4), offsetX);
        var posSar = Sar(4, 3, 0x4DA3, posData);
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));
        // Position/AI SAR is in the pre-stat region (before the stats SAR).
        return [.. magic, .. posSar, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar];
    }

    [Test]
    public async Task Parse_WithPositionAiSar_ReadsValuesCorrectly()
    {
        var bytes = BuildRecordWithPositionAiSar(currentAid: 42, location: 1800, offsetX: 7);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var raw = rec.PositionAiRaw;
        await Assert.That(raw).IsNotNull();
        await Assert.That(raw![0]).IsEqualTo(42);
        await Assert.That(raw[1]).IsEqualTo(1800);
        await Assert.That(raw[2]).IsEqualTo(7);
    }

    [Test]
    public async Task Parse_WithoutPositionAiSar_ReturnsNull()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.PositionAiRaw).IsNull();
    }

    [Test]
    public async Task WithPositionAi_PatchesCorrectly()
    {
        var bytes = BuildRecordWithPositionAiSar(currentAid: 0, location: 100, offsetX: 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithPositionAi([1, 2000, 5]);

        var raw = patched.PositionAiRaw!;
        await Assert.That(raw[0]).IsEqualTo(1);
        await Assert.That(raw[1]).IsEqualTo(2000);
        await Assert.That(raw[2]).IsEqualTo(5);
        // original unchanged
        await Assert.That(rec.PositionAiRaw![0]).IsEqualTo(0);
        await Assert.That(rec.PositionAiRaw[1]).IsEqualTo(100);
    }

    [Test]
    public async Task WithPositionAi_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithPositionAiSar(currentAid: 0, location: 500, offsetX: 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var patched = rec.WithPositionAi([10, 1500, 3]);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.PositionAiRaw![0]).IsEqualTo(10);
        await Assert.That(reparsed.PositionAiRaw[1]).IsEqualTo(1500);
        await Assert.That(reparsed.PositionAiRaw[2]).IsEqualTo(3);
    }

    [Test]
    public async Task WithPositionAi_WrongLength_ThrowsArgumentException()
    {
        var bytes = BuildRecordWithPositionAiSar(currentAid: 1, location: 200, offsetX: 3);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var action = () => rec.WithPositionAi([10, 20]);

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task WithHpDamage_WrongLength_ThrowsArgumentException()
    {
        var bytes = BuildRecordWithHpSar(0, 0, 0, 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var action = () => rec.WithHpDamage([10, 20, 30]);

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task WithFatigueDamage_WrongLength_ThrowsArgumentException()
    {
        var bytes = BuildRecordWithFatigueSar(0, 0, 0, 0);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var action = () => rec.WithFatigueDamage([10, 20, 30]);

        await Assert.That(action).Throws<ArgumentException>();
    }

    // ── Effects / EffectCauses (bsId=0x49FC / 0x49FD) ────────────────────────

    private static byte[] BuildRecordWithEffectsSars(int[] effectIds, int[] causesIds)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsSar = Sar(4, 28, 0x4299, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));

        // effects SAR: bsId=0x49FC
        var efData = new byte[effectIds.Length * 4];
        for (var i = 0; i < effectIds.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(efData.AsSpan(i * 4, 4), effectIds[i]);
        var effectsSar = Sar(4, effectIds.Length, 0x49FC, efData);

        // causes SAR: bsId=0x49FD
        var causeData = new byte[causesIds.Length * 4];
        for (var i = 0; i < causesIds.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(causeData.AsSpan(i * 4, 4), causesIds[i]);
        var causesSar = Sar(4, causesIds.Length, 0x49FD, causeData);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. effectsSar, .. causesSar];
    }

    [Test]
    public async Task Parse_WithEffectsSar_ReadsEffectsCorrectly()
    {
        var bytes = BuildRecordWithEffectsSars([72, 105, 50], [0, 1, 7]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Effects).IsNotNull();
        await Assert.That(rec.Effects![0]).IsEqualTo(72);
        await Assert.That(rec.Effects[1]).IsEqualTo(105);
        await Assert.That(rec.Effects[2]).IsEqualTo(50);
    }

    [Test]
    public async Task Parse_WithEffectsCausesSar_ReadsEffectCausesCorrectly()
    {
        var bytes = BuildRecordWithEffectsSars([72, 105, 50], [0, 1, 7]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.EffectCauses).IsNotNull();
        await Assert.That(rec.EffectCauses![0]).IsEqualTo(0);
        await Assert.That(rec.EffectCauses[1]).IsEqualTo(1);
        await Assert.That(rec.EffectCauses[2]).IsEqualTo(7);
    }

    [Test]
    public async Task Parse_WithoutEffectsSar_EffectsIsNull()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Effects).IsNull();
        await Assert.That(rec.EffectCauses).IsNull();
    }

    [Test]
    public async Task Parse_EffectsAndCauses_RoundTrip()
    {
        int[] effects = [72, 105, 50, 327, 158];
        int[] causes = [0, 1, 7, 3, 5];
        var bytes = BuildRecordWithEffectsSars(effects, causes);

        var rec = CharacterMdyRecord.Parse(bytes, out var consumed);

        // RawBytes must include both SARs
        await Assert.That(consumed).IsEqualTo(bytes.Length);
        await Assert.That(rec.Effects!.Length).IsEqualTo(effects.Length);
        for (var i = 0; i < effects.Length; i++)
            await Assert.That(rec.Effects[i]).IsEqualTo(effects[i]);
        for (var i = 0; i < causes.Length; i++)
            await Assert.That(rec.EffectCauses![i]).IsEqualTo(causes[i]);

        // Round trip: reparsing the raw bytes must give identical results
        var reparsed = CharacterMdyRecord.Parse(rec.RawBytes, out _);
        await Assert.That(reparsed.Effects!.Length).IsEqualTo(effects.Length);
        for (var i = 0; i < effects.Length; i++)
            await Assert.That(reparsed.Effects[i]).IsEqualTo(effects[i]);
    }

    [Test]
    public async Task Parse_EffectsCount_MatchesSarElementCount()
    {
        // Variable number of active effects
        int[] effects = [10, 20, 30, 40, 50, 60, 70];
        var bytes = BuildRecordWithEffectsSars(effects, [0, 0, 0, 0, 0, 0, 0]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.Effects!.Length).IsEqualTo(7);
    }

    // ── Quest-log SAR (eSize=16, bsCnt=37 structural fingerprint) ────────────

    /// <summary>
    /// Builds a SAR packet with a non-zero bsCnt and bitset words.
    /// </summary>
    private static byte[] SarWithBitset(int elemSz, int elemCnt, int bsId, byte[] data, int bsCnt, int[] bitset)
    {
        var buf = new byte[13 + data.Length + 4 + bsCnt * 4];
        buf[0] = 0x01;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), elemSz);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), elemCnt);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), bsId);
        data.CopyTo(buf, 13);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(13 + data.Length, 4), bsCnt);
        for (var i = 0; i < Math.Min(bsCnt, bitset.Length); i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                buf.AsSpan(13 + data.Length + 4 + i * 4, 4),
                bitset[i]
            );
        return buf;
    }

    /// <summary>
    /// Builds a minimal record that includes a quest-log SAR with the given entry count
    /// and active slot IDs encoded in the 37-word bitset.
    /// The record also contains a gold SAR so Gold=500 can be used to verify re-parse correctness.
    /// </summary>
    private static byte[] BuildRecordWithQuestSar(int questCnt, int[] activeSlotIds)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(500));

        var questData = new byte[questCnt * 16];
        var bitset = new int[37];
        foreach (var slotId in activeSlotIds)
        {
            var wordIdx = slotId / 32;
            var bitIdx = slotId % 32;
            if (wordIdx < 37)
                bitset[wordIdx] |= 1 << bitIdx;
        }
        var questSar = SarWithBitset(16, questCnt, 0x4A00, questData, 37, bitset);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. questSar];
    }

    [Test]
    public async Task Parse_WithQuestSar_ReadsQuestCountCorrectly()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 5, activeSlotIds: [0, 1, 2, 3, 4]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.QuestCount).IsEqualTo(5);
    }

    [Test]
    public async Task Parse_WithQuestSar_QuestDataRawHasCorrectLength()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 3, activeSlotIds: []);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.QuestDataRaw).IsNotNull();
        await Assert.That(rec.QuestDataRaw!.Length).IsEqualTo(3 * 16);
    }

    [Test]
    public async Task Parse_WithQuestSar_QuestBitsetRawHas37Elements()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 2, activeSlotIds: [10, 50, 100]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.QuestBitsetRaw).IsNotNull();
        await Assert.That(rec.QuestBitsetRaw!.Length).IsEqualTo(37);
    }

    [Test]
    public async Task Parse_WithQuestSar_BitsetReflectsActiveSlotIds()
    {
        // Slot IDs 10, 50, 100 → word 0 bit 10, word 1 bit 18, word 3 bit 4
        var bytes = BuildRecordWithQuestSar(questCnt: 3, activeSlotIds: [10, 50, 100]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        var bs = rec.QuestBitsetRaw!;

        await Assert.That((bs[0] >> 10) & 1).IsEqualTo(1); // slot 10
        await Assert.That((bs[1] >> 18) & 1).IsEqualTo(1); // slot 50
        await Assert.That((bs[3] >> 4) & 1).IsEqualTo(1); // slot 100
    }

    [Test]
    public async Task Parse_WithQuestSar_QueryCachesArePerInstance()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 3, activeSlotIds: [10, 50, 100]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var ids = rec.QuestActiveIds;
        var entries = rec.QuestEntries;
        var clone = rec with { RawBytes = [.. rec.RawBytes] };

        await Assert.That(object.ReferenceEquals(ids, rec.QuestActiveIds)).IsTrue();
        await Assert.That(object.ReferenceEquals(entries, rec.QuestEntries)).IsTrue();
        await Assert.That(object.ReferenceEquals(ids, clone.QuestActiveIds)).IsFalse();
        await Assert.That(object.ReferenceEquals(entries, clone.QuestEntries)).IsFalse();
    }

    [Test]
    public async Task Parse_WithoutQuestSar_QuestDataIsNull()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.QuestCount).IsEqualTo(0);
        await Assert.That(rec.QuestDataRaw).IsNull();
        await Assert.That(rec.QuestBitsetRaw).IsNull();
    }

    [Test]
    public async Task WithQuestDataRaw_PatchesBytesInPlace()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 2, activeSlotIds: []);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var newData = new byte[2 * 16];
        newData[0] = 0xAB;
        newData[16] = 0xCD;
        var patched = rec.WithQuestDataRaw(newData);

        await Assert.That(patched.QuestDataRaw![0]).IsEqualTo((byte)0xAB);
        await Assert.That(patched.QuestDataRaw[16]).IsEqualTo((byte)0xCD);
        await Assert.That(patched.QuestCount).IsEqualTo(2); // entry count unchanged
        await Assert.That(rec.QuestDataRaw![0]).IsEqualTo((byte)0); // original unchanged
    }

    [Test]
    public async Task WithQuestDataRaw_WrongSize_ReturnsUnchanged()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 2, activeSlotIds: []);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var wrongSize = new byte[3 * 16]; // 3 entries ≠ 2
        var result = rec.WithQuestDataRaw(wrongSize);

        await Assert.That(object.ReferenceEquals(result, rec)).IsTrue();
    }

    [Test]
    public async Task WithQuestStateRaw_ResizesEntryCountAndPreservesGold()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 2, activeSlotIds: [5]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);
        await Assert.That(rec.Gold).IsEqualTo(500);

        var newData = new byte[4 * 16]; // grow from 2 → 4 entries
        var newBitset = new int[37];
        newBitset[0] = 0b111; // slots 0, 1, 2

        var patched = rec.WithQuestStateRaw(newData, newBitset);

        await Assert.That(patched.QuestCount).IsEqualTo(4);
        await Assert.That(patched.QuestBitsetRaw![0]).IsEqualTo(0b111);
        await Assert.That(patched.Gold).IsEqualTo(500); // Gold offset survives resize
    }

    [Test]
    public async Task WithQuestStateRaw_RoundTrips_ViaReparse()
    {
        var bytes = BuildRecordWithQuestSar(questCnt: 1, activeSlotIds: [7]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var newData = new byte[2 * 16];
        var newBitset = new int[37];
        newBitset[0] = 0b11;
        var patched = rec.WithQuestStateRaw(newData, newBitset);

        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);
        await Assert.That(reparsed.QuestCount).IsEqualTo(2);
        await Assert.That(reparsed.QuestBitsetRaw![0]).IsEqualTo(0b11);
    }

    // ── Reputation SAR (eSize=4, eCnt=19, bsCnt=3 structural fingerprint) ────

    private static byte[] BuildRecordWithReputationSar(int[] reputationValues)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(200));

        var repData = new byte[19 * 4];
        for (var i = 0; i < Math.Min(19, reputationValues.Length); i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                repData.AsSpan(i * 4, 4),
                reputationValues[i]
            );
        var repSar = SarWithBitset(4, 19, 0x4E2A, repData, 3, new int[3]);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. repSar];
    }

    [Test]
    public async Task Parse_WithReputationSar_ReputationRawHas19Elements()
    {
        var repValues = Enumerable.Range(0, 19).ToArray();
        var bytes = BuildRecordWithReputationSar(repValues);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.ReputationRaw).IsNotNull();
        await Assert.That(rec.ReputationRaw!.Length).IsEqualTo(19);
    }

    [Test]
    public async Task Parse_WithReputationSar_ReadsValuesCorrectly()
    {
        var repValues = new int[19];
        repValues[0] = 1031;
        repValues[6] = 150;
        repValues[8] = -750;
        var bytes = BuildRecordWithReputationSar(repValues);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.ReputationRaw![0]).IsEqualTo(1031);
        await Assert.That(rec.ReputationRaw[6]).IsEqualTo(150);
        await Assert.That(rec.ReputationRaw[8]).IsEqualTo(-750);
    }

    [Test]
    public async Task Parse_WithoutReputationSar_ReputationRawIsNull()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.ReputationRaw).IsNull();
    }

    // ── Rumors SAR (eSize=8, bcCnt=39 structural fingerprint) ────────────────

    private static byte[] BuildRecordWithRumorsSar(int rumorCount, byte[]? rumorData = null)
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(100));

        var data = rumorData ?? new byte[rumorCount * 8];
        // Use a recognizable bsId that is otherwise unused in these tests
        var rumorsSar = SarWithBitset(8, rumorCount, 0x40E3, data, 39, new int[39]);

        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar, .. rumorsSar];
    }

    [Test]
    public async Task Parse_WithRumorsSar_RumorsCountIsCorrect()
    {
        var bytes = BuildRecordWithRumorsSar(rumorCount: 34);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.RumorsCount).IsEqualTo(34);
    }

    [Test]
    public async Task Parse_WithRumorsSar_RumorsRawHasCorrectByteLength()
    {
        var bytes = BuildRecordWithRumorsSar(rumorCount: 5);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.RumorsRaw).IsNotNull();
        await Assert.That(rec.RumorsRaw!.Length).IsEqualTo(5 * 8); // 5 entries × 8 bytes each
    }

    [Test]
    public async Task Parse_WithRumorsSar_RumorsRawContainsCorrectBytes()
    {
        var data = new byte[3 * 8];
        data[0] = 0xAA;
        data[7] = 0xBB; // first element sentinel
        data[16] = 0xCC; // third element sentinel
        var bytes = BuildRecordWithRumorsSar(rumorCount: 3, rumorData: data);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.RumorsRaw![0]).IsEqualTo((byte)0xAA);
        await Assert.That(rec.RumorsRaw[7]).IsEqualTo((byte)0xBB);
        await Assert.That(rec.RumorsRaw[16]).IsEqualTo((byte)0xCC);
    }

    [Test]
    public async Task Parse_WithoutRumorsSar_RumorsRawIsNull()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.RumorsRaw).IsNull();
        await Assert.That(rec.RumorsCount).IsEqualTo(0);
    }

    [Test]
    public async Task WithRumorsRaw_PatchesElementDataInPlace()
    {
        var original = new byte[2 * 8];
        var bytes = BuildRecordWithRumorsSar(rumorCount: 2, rumorData: original);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var newData = new byte[2 * 8];
        newData[0] = 0x11;
        newData[8] = 0x22;
        var patched = rec.WithRumorsRaw(newData);

        await Assert.That(patched.RumorsCount).IsEqualTo(2);
        await Assert.That(patched.RumorsRaw![0]).IsEqualTo((byte)0x11);
        await Assert.That(patched.RumorsRaw[8]).IsEqualTo((byte)0x22);
        // unpatched parts of original record (gold) must be preserved
        await Assert.That(patched.Gold).IsEqualTo(rec.Gold);
    }

    [Test]
    public async Task WithRumorsRaw_ReturnsSelfWhenDataLengthMismatch()
    {
        var bytes = BuildRecordWithRumorsSar(rumorCount: 3);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        // Wrong size: 2 entries instead of 3
        var wrongSize = new byte[2 * 8];
        var result = rec.WithRumorsRaw(wrongSize);

        await Assert.That(ReferenceEquals(result, rec)).IsTrue();
    }

    // ── Blessing / Curse / Schematics detection ───────────────────────────────
    //
    // Structural fingerprints (session 12 RE, Slot0177 v2 player record):
    //   First  4:N:2 + 8:N:2 consecutive pair → PcBlessingIdx + PcBlessingTsIdx
    //   Second 4:M:2 + 8:M:2 consecutive pair → PcCurseIdx   + PcCurseTsIdx
    //   Standalone 4:K:2 with firstVal > 1000  → PcSchematicsFoundIdx
    //
    // bsCnt=2 is required by the pair-detection code; SARs with other bsCnt values
    // are excluded from the detection.
    //
    // The false-positive V2Magic test covers the bug discovered in session 13:
    // the byte sequence 02 00 00 00 0F 00 00 00 00 00 00 00 (= V2Magic) can appear
    // at the bsCnt field of a SAR whose bsCnt=2 and whose first bitset word = 0x0F.
    // The fix in CharacterMdyRecord.Parse advances nextMagicPos past the current
    // SAR boundary and continues scanning.

    // Builds a SAR with bsCnt bitset words (eSize=4 elements).
    private static byte[] Sar32WithBitset(int[] data, int bsId, int[] bitset)
    {
        var buf = new byte[13 + data.Length * 4 + 4 + bitset.Length * 4];
        buf[0] = 0x01;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), 4);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), data.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), bsId);
        for (var i = 0; i < data.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(13 + i * 4, 4), data[i]);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            buf.AsSpan(13 + data.Length * 4, 4),
            bitset.Length
        );
        for (var i = 0; i < bitset.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                buf.AsSpan(13 + data.Length * 4 + 4 + i * 4, 4),
                bitset[i]
            );
        return buf;
    }

    // Builds a SAR with bsCnt bitset words (eSize=8 elements, each 8 bytes of zeros).
    private static byte[] Sar64WithBitset(int elemCnt, int bsId, int[] bitset)
    {
        var dataLen = elemCnt * 8;
        var buf = new byte[13 + dataLen + 4 + bitset.Length * 4];
        buf[0] = 0x01;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), 8);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), elemCnt);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), bsId);
        // data is all zeros (already 0-initialized)
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(13 + dataLen, 4), bitset.Length);
        for (var i = 0; i < bitset.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
                buf.AsSpan(13 + dataLen + 4 + i * 4, 4),
                bitset[i]
            );
        return buf;
    }

    // Builds a base v2 record (V2Magic + Stats + Basic + Tech + Spell + Gold).
    private static byte[] BaseRecord()
    {
        byte[] magic = [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var statsSar = Sar(4, 28, 0x4DA5, IntArray(28, 8));
        var basicSar = Sar(4, 12, 0x43C3, IntArray(12));
        var techSar = Sar(4, 4, 0x4A07, IntArray(4));
        var spellSar = Sar(4, 25, 0x4A08, IntArray(25));
        var goldSar = Sar(4, 1, 0x4B13, IntBytes(0));
        return [.. magic, .. statsSar, .. basicSar, .. techSar, .. spellSar, .. goldSar];
    }

    // Builds a standalone reputation SAR (4:19:3) to act as a non-matching terminator
    // that finalizes any pending pair candidate (schematicsElementCount = pairCandidateECnt).
    private static byte[] ReputationSar()
    {
        var buf = new byte[13 + 19 * 4 + 4 + 3 * 4];
        buf[0] = 0x01;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, 4), 4);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(5, 4), 19);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(9, 4), 0x5244);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(13 + 19 * 4, 4), 3);
        return buf;
    }

    [Test]
    public async Task Parse_DetectsBlessing_WhenPresentAfterSpellTech()
    {
        // Blessing pair: 4:3:2 + 8:3:2
        var blessingIds = new[] { 1049, 1051, 1004 };
        var blessingSar = Sar32WithBitset(blessingIds, 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(3, 0x48EA, [1, 0]);

        var bytes = (byte[])[.. BaseRecord(), .. blessingSar, .. blessingTsSar];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.BlessingRaw).IsNotNull();
        await Assert.That(rec.BlessingProtoElementCount).IsEqualTo(3);
        await Assert.That(rec.BlessingRaw![0]).IsEqualTo(1049);
        await Assert.That(rec.BlessingRaw[1]).IsEqualTo(1051);
        await Assert.That(rec.BlessingRaw[2]).IsEqualTo(1004);
    }

    [Test]
    public async Task Parse_DetectsCurse_WhenPresentAfterBlessingPair()
    {
        // Blessing pair then curse pair
        var blessingSar = Sar32WithBitset([1049, 1051], 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(2, 0x48EA, [1, 0]);
        var curseIds = new[] { 67, 53 };
        var curseSar = Sar32WithBitset(curseIds, 0x2AA3, [1, 0]);
        var curseTsSar = Sar64WithBitset(2, 0x48E2, [1, 0]);

        var bytes = (byte[])[.. BaseRecord(), .. blessingSar, .. blessingTsSar, .. curseSar, .. curseTsSar];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.CurseRaw).IsNotNull();
        await Assert.That(rec.CurseProtoElementCount).IsEqualTo(2);
        await Assert.That(rec.CurseRaw![0]).IsEqualTo(67);
        await Assert.That(rec.CurseRaw[1]).IsEqualTo(53);
    }

    [Test]
    public async Task Parse_DetectsSchematics_AfterCursePair_WhenFirstValueAbove1000()
    {
        var blessingSar = Sar32WithBitset([1049], 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(1, 0x48EA, [1, 0]);
        var curseSar = Sar32WithBitset([67], 0x2AA3, [1, 0]);
        var curseTsSar = Sar64WithBitset(1, 0x48E2, [1, 0]);
        // Standalone schematic SAR — firstVal 5090 > 1000
        var schematicIds = new[] { 5090, 4810 };
        var schematicSar = Sar32WithBitset(schematicIds, 0x5228, [1, 0]);

        var bytes = (byte[])
            [
                .. BaseRecord(),
                .. blessingSar,
                .. blessingTsSar,
                .. curseSar,
                .. curseTsSar,
                .. schematicSar,
                .. ReputationSar(),
            ];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.SchematicsRaw).IsNotNull();
        await Assert.That(rec.SchematicsElementCount).IsEqualTo(2);
        await Assert.That(rec.SchematicsRaw![0]).IsEqualTo(5090);
        await Assert.That(rec.SchematicsRaw[1]).IsEqualTo(4810);
    }

    [Test]
    public async Task Parse_NullSchematics_WhenFirstValueBelow1000()
    {
        // Standalone 4:2:2 with small prototype IDs (not tech schematics)
        var smallIdSar = Sar32WithBitset([50, 100], 0x5555, [1, 0]);

        var bytes = (byte[])[.. BaseRecord(), .. smallIdSar, .. ReputationSar()];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.SchematicsRaw).IsNull();
    }

    /// <summary>
    /// Verifies the false-positive V2Magic recovery fix:
    /// a SAR whose bsCnt=2 and first bitset word=0x0F produces the byte sequence
    /// <c>02 00 00 00 0F 00 00 00 00 00 00 00</c> = V2Magic at the bsCnt offset.
    /// Without the fix the scan would terminate before the SAR, leaving schematics
    /// undetected.  With the fix the scan detects the false positive and continues.
    /// </summary>
    [Test]
    public async Task Parse_RecoversSchematics_WhenV2MagicAppearsInSchematicsBitset()
    {
        var blessingSar = Sar32WithBitset([1049], 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(1, 0x48EA, [1, 0]);
        var curseSar = Sar32WithBitset([67], 0x2AA3, [1, 0]);
        var curseTsSar = Sar64WithBitset(1, 0x48E2, [1, 0]);
        // Schematic SAR with bitset [0x0F, 0x00] → bsCnt=2 + bitset creates V2Magic!
        // bytes at bsCnt position: 02 00 00 00 | 0F 00 00 00 | 00 00 00 00 = V2Magic.
        var schematicSar = Sar32WithBitset([5090, 4810], 0x5228, [0x0F, 0x00]);

        var bytes = (byte[])
            [
                .. BaseRecord(),
                .. blessingSar,
                .. blessingTsSar,
                .. curseSar,
                .. curseTsSar,
                .. schematicSar,
                .. ReputationSar(),
            ];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        // Detection must succeed despite the false-positive V2Magic in the bitset.
        await Assert.That(rec.SchematicsRaw).IsNotNull();
        await Assert.That(rec.SchematicsElementCount).IsEqualTo(2);
        await Assert.That(rec.SchematicsRaw![0]).IsEqualTo(5090);
    }

    [Test]
    public async Task WithBlessingRaw_PatchesAndRoundTrips()
    {
        var blessingSar = Sar32WithBitset([1049, 1051, 1004], 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(3, 0x48EA, [1, 0]);
        var bytes = (byte[])[.. BaseRecord(), .. blessingSar, .. blessingTsSar];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithBlessingRaw([2001, 2002, 2003]);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.BlessingRaw).IsNotNull();
        await Assert.That(reparsed.BlessingRaw![0]).IsEqualTo(2001);
        await Assert.That(reparsed.BlessingRaw[1]).IsEqualTo(2002);
        await Assert.That(reparsed.BlessingRaw[2]).IsEqualTo(2003);
    }

    [Test]
    public async Task WithCurseRaw_PatchesAndRoundTrips()
    {
        var blessingSar = Sar32WithBitset([1049], 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(1, 0x48EA, [1, 0]);
        var curseSar = Sar32WithBitset([67, 53], 0x2AA3, [1, 0]);
        var curseTsSar = Sar64WithBitset(2, 0x48E2, [1, 0]);
        var bytes = (byte[])[.. BaseRecord(), .. blessingSar, .. blessingTsSar, .. curseSar, .. curseTsSar];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithCurseRaw([999, 888]);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.CurseRaw).IsNotNull();
        await Assert.That(reparsed.CurseRaw![0]).IsEqualTo(999);
        await Assert.That(reparsed.CurseRaw[1]).IsEqualTo(888);
    }

    [Test]
    public async Task WithSchematicsRaw_PatchesAndRoundTrips()
    {
        var blessingSar = Sar32WithBitset([1049], 0x48E9, [1, 0]);
        var blessingTsSar = Sar64WithBitset(1, 0x48EA, [1, 0]);
        var curseSar = Sar32WithBitset([67], 0x2AA3, [1, 0]);
        var curseTsSar = Sar64WithBitset(1, 0x48E2, [1, 0]);
        var schematicSar = Sar32WithBitset([5090, 4810], 0x5228, [1, 0]);
        var bytes = (byte[])
            [
                .. BaseRecord(),
                .. blessingSar,
                .. blessingTsSar,
                .. curseSar,
                .. curseTsSar,
                .. schematicSar,
                .. ReputationSar(),
            ];
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        var patched = rec.WithSchematicsRaw([6001, 7002]);
        var reparsed = CharacterMdyRecord.Parse(patched.RawBytes, out _);

        await Assert.That(reparsed.SchematicsRaw).IsNotNull();
        await Assert.That(reparsed.SchematicsRaw![0]).IsEqualTo(6001);
        await Assert.That(reparsed.SchematicsRaw[1]).IsEqualTo(7002);
    }

    [Test]
    public async Task Parse_NullBlessing_WhenAbsent()
    {
        var bytes = BuildRecord(gold: 0, stats: new int[28]);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        await Assert.That(rec.BlessingRaw).IsNull();
        await Assert.That(rec.CurseRaw).IsNull();
        await Assert.That(rec.SchematicsRaw).IsNull();
    }
}
