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
    public async Task WithPositionAi_WrongLength_ReturnsUnchanged()
    {
        var bytes = BuildRecordWithPositionAiSar(currentAid: 1, location: 200, offsetX: 3);
        var rec = CharacterMdyRecord.Parse(bytes, out _);

        // WithPositionAi requires exactly 3 elements; passing 2 should be a no-op.
        var patched = rec.WithPositionAi([10, 20]);

        await Assert.That(patched.PositionAiRaw![0]).IsEqualTo(1);
    }
}
