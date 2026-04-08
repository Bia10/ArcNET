using System.Buffers.Binary;

namespace ArcNET.Formats;

/// <summary>
/// Represents a v2 PC/NPC character record as it appears inside a <c>mobile.mdy</c> file.
/// <para>
/// A v2 record starts with a 12-byte magic header
/// <c>[02 00 00 00 0F 00 00 00 00 00 00 00]</c> and is followed by one to four
/// SAR (Sparse Array Record) packets that encode the character's stats, basic skills,
/// tech skills, and spell / tech discipline ranks.  Additional SAR fields (gold amount,
/// inventory handles, quests, etc.) may follow the four primary arrays.
/// </para>
/// <para>
/// <see cref="RawBytes"/> always contains the exact bytes as they appear on disk —
/// including all trailing SAR fields — so the writer can preserve them verbatim.
/// Use the <c>With*</c> methods to obtain a new record with patched bytes.
/// </para>
/// </summary>
public sealed record CharacterMdyRecord
{
    // ── Wire signatures ───────────────────────────────────────────────────────

    /// <summary>12-byte magic that identifies a v2 character record.</summary>
    internal static ReadOnlySpan<byte> V2Magic =>
        [0x02, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    // presence(1B)=0x01 + elemSz(4B) + elemCnt(4B) — SAR array signatures.
    private static ReadOnlySpan<byte> StatSig => [0x04, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00]; // elemCnt=28
    private static ReadOnlySpan<byte> BasicSkillSig => [0x04, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00]; // elemCnt=12
    private static ReadOnlySpan<byte> TechSkillSig => [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00]; // elemCnt=4
    private static ReadOnlySpan<byte> SpellTechSig => [0x04, 0x00, 0x00, 0x00, 0x19, 0x00, 0x00, 0x00]; // elemCnt=25

    // presence(1B) + elemSz(4B) + elemCnt(4B) + bitsetId(4B)
    private const int SarHeaderSize = 13;
    private const int MaxScanDistance = 4096;

    // bsId for the single-INT32 gold-amount SAR that follows the known four arrays.
    private const int GoldAmountBsId = 0x4B13;

    // bsId for the 11-element game-statistics SAR (PC-wide data embedded in the v2 record).
    // Confirmed element layout (inner-bitset bits 0..10 + 64..65):
    //   [0]=TotalKills (inner bit 0), [8]=Arrows (inner bit 10).
    //   [9]=CritterFlags (inner bit 64), [10]=CritterFlags2 (inner bit 65).
    //   Inner bits 11/12 = Bullets/PowerCells — absent (0) for magic-focused characters.
    private const int GameStatsBsId = 0x4D68;
    private const int GameStatsElementCount = 11;
    private const int GameStatsTotalKillsIndex = 0;
    private const int GameStatsArrowsIndex = 8;

    // bsId for the 3-element portrait / followers SAR.
    // Confirmed element layout: [0]=MaxFollowersComputed, [1]=PortraitIndex, [2]=0.
    private const int PortraitBsId = 0x4DA4;
    private const int PortraitElementCount = 3;
    private const int PortraitMaxFollowersElement = 0;
    private const int PortraitIndexElement = 1;

    // ── Public state ──────────────────────────────────────────────────────────

    /// <summary>
    /// The exact bytes of this record as read from disk, from the first byte of
    /// <see cref="V2Magic"/> through the end of the last SAR packet found.
    /// Written back verbatim when no changes are applied.
    /// </summary>
    public required byte[] RawBytes { get; init; }

    /// <summary>Stats array — 28 elements (strength … race).</summary>
    public required int[] Stats { get; init; }

    /// <summary>Basic skills array — 12 elements (bow … persuasion).</summary>
    public required int[] BasicSkills { get; init; }

    /// <summary>Tech skills array — 4 elements (repair … disarm traps).</summary>
    public required int[] TechSkills { get; init; }

    /// <summary>Spell / tech disciplines array — 25 elements (conveyance … therapeutics).</summary>
    public required int[] SpellTech { get; init; }

    /// <summary>
    /// <see langword="true"/> when all four SAR arrays were found in the record.
    /// PC records always have all four; NPCs may only have the stat array.
    /// </summary>
    public required bool HasCompleteData { get; init; }

    // ── Byte offsets for in-place patching ────────────────────────────────────
    // Offsets point to the first int data byte within RawBytes (-1 = absent).
    // Not `required` — only CharacterMdyRecord.Parse constructs instances.

    internal int StatsDataOffset { get; init; }
    internal int BasicSkillsDataOffset { get; init; }
    internal int TechSkillsDataOffset { get; init; }
    internal int SpellTechDataOffset { get; init; }

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the single INT32 that holds
    /// the player's gold amount (bsId=0x4B13).  −1 when absent (NPCs, incomplete records).
    /// </summary>
    internal int GoldDataOffset { get; init; } = -1;

    /// <summary>
    /// The player's current gold amount as stored in the record.
    /// Returns 0 when the gold SAR is absent from this record.
    /// </summary>
    public int Gold =>
        GoldDataOffset >= 0 ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(GoldDataOffset, 4)) : 0;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [8] (Arrows) in the
    /// 11-element game-statistics SAR (bsId=0x4D68).  −1 when absent.
    /// </summary>
    internal int ArrowsDataOffset { get; init; } = -1;

    /// <summary>
    /// The player's current arrow count (bsId=0x4D68[8]).
    /// Returns 0 when the game-statistics SAR is absent from this record.
    /// </summary>
    public int Arrows =>
        ArrowsDataOffset >= 0 ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(ArrowsDataOffset, 4)) : 0;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [0] (TotalKills) in the
    /// 11-element game-statistics SAR (bsId=0x4D68).  −1 when absent.
    /// </summary>
    internal int TotalKillsDataOffset { get; init; } = -1;

    /// <summary>
    /// The player's total kill count (bsId=0x4D68[0]).
    /// Returns 0 when the game-statistics SAR is absent from this record.
    /// </summary>
    public int TotalKills =>
        TotalKillsDataOffset >= 0
            ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(TotalKillsDataOffset, 4))
            : 0;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [1] (PortraitIndex) in the
    /// 3-element portrait SAR (bsId=0x4DA4).  −1 when absent.
    /// </summary>
    internal int PortraitDataOffset { get; init; } = -1;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [0] (MaxFollowers) in the
    /// 3-element portrait SAR (bsId=0x4DA4).  −1 when absent.
    /// </summary>
    internal int MaxFollowersDataOffset => PortraitDataOffset >= 0 ? PortraitDataOffset - PortraitIndexElement * 4 : -1;

    /// <summary>
    /// The character's portrait index (bsId=0x4DA4[1]).
    /// Returns −1 when the portrait SAR is absent from this record.
    /// </summary>
    public int PortraitIndex =>
        PortraitDataOffset >= 0 ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(PortraitDataOffset, 4)) : -1;

    /// <summary>
    /// The computed max-followers value (bsId=0x4DA4[0]).
    /// Returns −1 when the portrait SAR is absent from this record.
    /// </summary>
    public int MaxFollowers =>
        MaxFollowersDataOffset >= 0
            ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(MaxFollowersDataOffset, 4))
            : -1;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the 4-byte length prefix of the
    /// PC name string field (non-SAR encoding: <c>01 [uint32_len] [ascii_chars]</c>).
    /// −1 when absent or record is not a PC.
    /// </summary>
    internal int NameLengthOffset { get; init; } = -1;

    /// <summary>
    /// The PC's name as stored in the record (non-SAR length-prefixed ASCII field).
    /// Returns <see langword="null"/> when absent (NPC records or incomplete parses).
    /// </summary>
    public string? Name
    {
        get
        {
            if (NameLengthOffset < 0 || NameLengthOffset + 4 > RawBytes.Length)
                return null;
            var len = BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(NameLengthOffset, 4));
            if (len <= 0 || NameLengthOffset + 4 + len > RawBytes.Length)
                return null;
            return System.Text.Encoding.ASCII.GetString(RawBytes, NameLengthOffset + 4, len);
        }
    }

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the first INT32 in the pre-stat
    /// three-element SAR (bsId=0x4DA3).  −1 when absent.
    /// Probe-confirmed inner-bitset names: element[0]=CurrentAid, [1]=Location, [2]=OffsetX.
    /// These are critter position / AI-controller fields, not HP or fatigue.
    /// Preserved verbatim during round-trip so the game can read the PC's position back.
    /// </summary>
    internal int PositionAiDataOffset { get; init; } = -1;

    /// <summary>
    /// Raw three-element position / AI SAR values (bsId=0x4DA3), or null when absent.
    /// Indices: [0]=CurrentAid (AI obj ref), [1]=Location (tile), [2]=OffsetX.
    /// </summary>
    public int[]? PositionAiRaw => PositionAiDataOffset >= 0 ? ReadInts(RawBytes, PositionAiDataOffset, 3) : null;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the first INT32 in the HP SAR
    /// (bsId=0x4046, INT32[4]).  −1 when absent.
    /// Probe-confirmed: all four elements are 0 at full health.
    /// Layout hypothesis (ObjF fields 25-28): [AcBonus(0), HpPtsBonus(1), HpAdj(2), HpDamage(3)].
    /// Setting element [3] to a positive value reduces current HP below max
    /// (e.g. HpDamage=20 → CurrentHP = MaxHP−20).
    /// </summary>
    internal int HpDamageDataOffset { get; init; } = -1;

    /// <summary>
    /// Raw four-element HP SAR values (bsId=0x4046), or null when absent.
    /// Layout: [AcBonus, HpPtsBonus, HpAdj, HpDamage].
    /// Element [3] is the damage taken — set to reduce displayed HP.
    /// </summary>
    public int[]? HpDamageRaw => HpDamageDataOffset >= 0 ? ReadInts(RawBytes, HpDamageDataOffset, 4) : null;

    /// <summary>HP damage taken from bsId=0x4046 element [3].  0 when at full health or SAR absent.</summary>
    public int HpDamage => HpDamageRaw is { } r ? r[3] : 0;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the first INT32 in the Fatigue SAR
    /// (bsId=0x423E, INT32[4]).  −1 when absent.
    /// Layout hypothesis: [FatiguePtsBonus(0), FatigueAdj(1), FatigueDamage(2), ?(3)].
    /// </summary>
    internal int FatigueDamageDataOffset { get; init; } = -1;

    /// <summary>
    /// Raw four-element Fatigue SAR values (bsId=0x423E), or null when absent.
    /// Element [2] is the fatigue damage — set to reduce displayed Fatigue.
    /// </summary>
    public int[]? FatigueDamageRaw =>
        FatigueDamageDataOffset >= 0 ? ReadInts(RawBytes, FatigueDamageDataOffset, 4) : null;

    /// <summary>Fatigue damage taken from bsId=0x423E element [2].  0 when at full fatigue or SAR absent.</summary>
    public int FatigueDamage => FatigueDamageRaw is { } r ? r[2] : 0;

    // ── Parsing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a v2 character record starting at the beginning of <paramref name="span"/>.
    /// <paramref name="span"/> must start at the first byte of <see cref="V2Magic"/>.
    /// </summary>
    /// <param name="span">Bytes starting at the v2 magic.</param>
    /// <param name="consumed">Number of bytes consumed from <paramref name="span"/>.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the mandatory stats SAR cannot be located within
    /// <see cref="MaxScanDistance"/> bytes of the magic header.
    /// </exception>
    public static CharacterMdyRecord Parse(ReadOnlySpan<byte> span, out int consumed)
    {
        // ── Pre-stat scan: locate specific SARs that sit before the stat SAR.
        // bsId=0x4DA3 (INT32[3]): position / AI-controller data (CurrentAid, Location, OffsetX).
        // bsId=0x4046 (INT32[4]): HP-damage SAR — all zeros at full health.
        var positionAiDataOffset = -1;
        var hpDamageDataOffset = -1;

        // Mandatory: stats SAR (28 × int32), searched within the first 12 + MaxScanDistance bytes.
        var statOff = FindSar(span, 12, span.Length, StatSig);
        if (statOff < 0)
            throw new InvalidDataException("v2 character record: stats SAR not found within scan range");

        var statsDataOff = statOff + SarHeaderSize;
        if (statsDataOff + 28 * 4 > span.Length)
            throw new InvalidDataException("v2 character record: stats SAR data extends beyond available bytes");

        var stats = ReadInts(span, statsDataOff, 28);
        var end = SarEnd(span, statOff, 28);

        // Scan pre-stat region for bsId=0x4DA3 (INT32[3], position/AI data).
        ReadOnlySpan<byte> posAiSig = [0x04, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0xA3, 0x4D, 0x00, 0x00];
        var posAiOff = FindSar(span, 12, statOff, posAiSig);
        if (posAiOff >= 0 && posAiOff + SarHeaderSize + 3 * 4 <= span.Length)
            positionAiDataOffset = posAiOff + SarHeaderSize;

        // Scan pre-stat region for bsId=0x4046 (INT32[4], HP SAR: [AcBonus,HpPtsBonus,HpAdj,HpDamage]).
        ReadOnlySpan<byte> hpDmgSig = [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x46, 0x40, 0x00, 0x00];
        var hpDmgOff = FindSar(span, 12, statOff, hpDmgSig);
        if (hpDmgOff >= 0 && hpDmgOff + SarHeaderSize + 4 * 4 <= span.Length)
            hpDamageDataOffset = hpDmgOff + SarHeaderSize;

        // Scan pre-stat region for bsId=0x423E (INT32[4], Fatigue SAR: [FatiguePtsBonus,FatigueAdj,FatigueDamage,?]).
        ReadOnlySpan<byte> fatSig = [0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x3E, 0x42, 0x00, 0x00];
        var fatOff = FindSar(span, 12, statOff, fatSig);
        var fatigueDamageDataOffset =
            fatOff >= 0 && fatOff + SarHeaderSize + 4 * 4 <= span.Length ? fatOff + SarHeaderSize : -1;

        // Optional: basic skills, tech skills, spell/tech — each must follow the previous SAR.
        int[] basicSkills = new int[12];
        int[] techSkills = new int[4];
        int[] spellTech = new int[25];
        bool hasAll = false;

        int basicDataOff = -1,
            techDataOff = -1,
            spellDataOff = -1;

        var basicOff = FindSar(span, end, span.Length, BasicSkillSig);
        if (basicOff >= 0)
        {
            basicDataOff = basicOff + SarHeaderSize;
            basicSkills = ReadInts(span, basicDataOff, 12);
            end = SarEnd(span, basicOff, 12);

            var techOff = FindSar(span, end, span.Length, TechSkillSig);
            if (techOff >= 0)
            {
                techDataOff = techOff + SarHeaderSize;
                techSkills = ReadInts(span, techDataOff, 4);
                end = SarEnd(span, techOff, 4);

                var spellOff = FindSar(span, end, span.Length, SpellTechSig);
                if (spellOff >= 0)
                {
                    spellDataOff = spellOff + SarHeaderSize;
                    spellTech = ReadInts(span, spellDataOff, 25);
                    end = SarEnd(span, spellOff, 25);
                    hasAll = true;
                }
            }
        }

        // ── Extended scan: capture ALL remaining SAR fields in RawBytes ──────
        // The four arrays above cover only the first part of a PC v2 record.
        // Gold amount, inventory handles, quests, and other fields follow as
        // generic SARs up to ~32 KB further.  Capturing them all ensures that
        // RawBytes is complete and the writer never silently discards data when
        // saving changes.
        var goldDataOffset = -1;
        var arrowsDataOffset = -1;
        var totalKillsDataOffset = -1;
        var portraitDataOffset = -1;
        var nameLengthOffset = -1;
        var scanPos = end;

        // Cap the extended scan at the next v2 character record boundary.
        // Without this, the extended scan of an NPC v2 record (e.g. LVL=10)
        // would greedily consume all subsequent bytes — including the player's
        // v2 record that follows it in the same mobile.mdy file — because the
        // SAR scanner can match genuine SAR packets inside another record's data.
        var nextMagicPos = -1;
        for (var mp = end; mp + V2Magic.Length <= span.Length; mp++)
        {
            if (span.Slice(mp, V2Magic.Length).SequenceEqual(V2Magic))
            {
                nextMagicPos = mp;
                break;
            }
        }

        var extLimit = nextMagicPos >= 0 ? nextMagicPos : Math.Min(span.Length, end + 32768);
        while (scanPos + SarHeaderSize <= extLimit)
        {
            var nextSar = FindAnySar(span, scanPos, extLimit);
            if (nextSar < 0)
                break;

            var eSize = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(nextSar + 1, 4));
            var eCnt = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(nextSar + 5, 4));
            var bsId = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(nextSar + 9, 4));
            var dataLen = eSize * eCnt;
            var bcOff = nextSar + SarHeaderSize + dataLen;
            if (bcOff + 4 > span.Length)
                break;
            var bsCnt = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bcOff, 4));
            if (bsCnt is < 0 or > 256)
            {
                // False positive — advance one byte past this candidate and retry.
                scanPos = nextSar + 1;
                continue;
            }

            var sarEnd2 = bcOff + 4 + bsCnt * 4;
            if (sarEnd2 > extLimit)
                break;

            if (bsId == GoldAmountBsId && eSize == 4 && eCnt == 1)
                goldDataOffset = nextSar + SarHeaderSize;

            if (bsId == GameStatsBsId && eSize == 4 && eCnt == GameStatsElementCount)
            {
                totalKillsDataOffset = nextSar + SarHeaderSize + GameStatsTotalKillsIndex * 4;
                arrowsDataOffset = nextSar + SarHeaderSize + GameStatsArrowsIndex * 4;
            }

            if (bsId == PortraitBsId && eSize == 4 && eCnt == PortraitElementCount)
                portraitDataOffset = nextSar + SarHeaderSize + PortraitIndexElement * 4;

            end = sarEnd2;
            scanPos = sarEnd2;
        }

        // ── Post-SAR: scan for the non-SAR PC name field ──────────────────────
        // Encoding: presence(1B)=0x01 + length(4B LE) + ascii_chars(length bytes).
        // Presence byte 0x01 does NOT start a valid SAR here (elemSz would be the
        // length field, which is typically 1-32 and not in {1,2,4,8,16,24} with a
        // plausible SAR cnt).  We scan from `end` forward for the pattern.
        if (nameLengthOffset < 0)
        {
            var nameSearchEnd = Math.Min(span.Length, end + 512);
            for (var np = end; np + 5 <= nameSearchEnd; np++)
            {
                if (span[np] != 0x01)
                    continue;
                var nameLen = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(np + 1, 4));
                if (nameLen is < 1 or > 64)
                    continue;
                if (np + 5 + nameLen > span.Length)
                    continue;
                // Verify all chars are printable ASCII
                bool ok = true;
                for (var nc = 0; nc < nameLen && ok; nc++)
                {
                    var c = span[np + 5 + nc];
                    if (c < 0x20 || c > 0x7E)
                        ok = false;
                }
                if (!ok)
                    continue;
                // Extend consumed to cover the name field so RawBytes includes it.
                nameLengthOffset = np + 1;
                end = np + 1 + 4 + nameLen;
                break;
            }
        }

        consumed = end;
        return new CharacterMdyRecord
        {
            RawBytes = span[..consumed].ToArray(),
            Stats = stats,
            BasicSkills = basicSkills,
            TechSkills = techSkills,
            SpellTech = spellTech,
            HasCompleteData = hasAll,
            StatsDataOffset = statsDataOff,
            BasicSkillsDataOffset = basicDataOff,
            TechSkillsDataOffset = techDataOff,
            SpellTechDataOffset = spellDataOff,
            GoldDataOffset = goldDataOffset,
            ArrowsDataOffset = arrowsDataOffset,
            TotalKillsDataOffset = totalKillsDataOffset,
            PortraitDataOffset = portraitDataOffset,
            NameLengthOffset = nameLengthOffset,
            PositionAiDataOffset = positionAiDataOffset,
            HpDamageDataOffset = hpDamageDataOffset,
            FatigueDamageDataOffset = fatigueDamageDataOffset,
        };
    }

    // ── Patch methods ─────────────────────────────────────────────────────────

    /// <summary>Returns a new record with the stats array replaced by <paramref name="stats"/>.</summary>
    public CharacterMdyRecord WithStats(int[] stats)
    {
        var raw = PatchInts(RawBytes, StatsDataOffset, stats);
        return this with { RawBytes = raw, Stats = stats };
    }

    /// <summary>Returns a new record with the basic skills array replaced by <paramref name="basicSkills"/>.</summary>
    public CharacterMdyRecord WithBasicSkills(int[] basicSkills)
    {
        if (BasicSkillsDataOffset < 0)
            return this;
        var raw = PatchInts(RawBytes, BasicSkillsDataOffset, basicSkills);
        return this with { RawBytes = raw, BasicSkills = basicSkills };
    }

    /// <summary>Returns a new record with the tech skills array replaced by <paramref name="techSkills"/>.</summary>
    public CharacterMdyRecord WithTechSkills(int[] techSkills)
    {
        if (TechSkillsDataOffset < 0)
            return this;
        var raw = PatchInts(RawBytes, TechSkillsDataOffset, techSkills);
        return this with { RawBytes = raw, TechSkills = techSkills };
    }

    /// <summary>Returns a new record with the spell / tech array replaced by <paramref name="spellTech"/>.</summary>
    public CharacterMdyRecord WithSpellTech(int[] spellTech)
    {
        if (SpellTechDataOffset < 0)
            return this;
        var raw = PatchInts(RawBytes, SpellTechDataOffset, spellTech);
        return this with { RawBytes = raw, SpellTech = spellTech };
    }

    /// <summary>
    /// Returns a new record with the gold amount set to <paramref name="gold"/>.
    /// Returns this record unchanged when the gold SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithGold(int gold)
    {
        if (GoldDataOffset < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(GoldDataOffset, 4), gold);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the arrow count set to <paramref name="arrows"/>.
    /// Returns this record unchanged when the game-statistics SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithArrows(int arrows)
    {
        if (ArrowsDataOffset < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(ArrowsDataOffset, 4), arrows);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the portrait index set to <paramref name="portraitIndex"/>.
    /// Returns this record unchanged when the portrait SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithPortraitIndex(int portraitIndex)
    {
        if (PortraitDataOffset < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(PortraitDataOffset, 4), portraitIndex);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the max-followers computed value set to <paramref name="maxFollowers"/>
    /// (bsId=0x4DA4[0]).
    /// Returns this record unchanged when the portrait SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithMaxFollowers(int maxFollowers)
    {
        var off = MaxFollowersDataOffset;
        if (off < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(off, 4), maxFollowers);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the total kill count set to <paramref name="totalKills"/>.
    /// Returns this record unchanged when the game-statistics SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithTotalKills(int totalKills)
    {
        if (TotalKillsDataOffset < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(TotalKillsDataOffset, 4), totalKills);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the three position / AI SAR values replaced (bsId=0x4DA3).
    /// <paramref name="values"/> must have exactly 3 elements.
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithPositionAi(int[] values)
    {
        if (PositionAiDataOffset < 0 || values.Length != 3)
            return this;
        var raw = PatchInts(RawBytes, PositionAiDataOffset, values);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the HP SAR values replaced (bsId=0x4046).
    /// <paramref name="values"/> must have exactly 4 elements: [AcBonus, HpPtsBonus, HpAdj, HpDamage].
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithHpDamage(int[] values)
    {
        if (HpDamageDataOffset < 0 || values.Length != 4)
            return this;
        var raw = PatchInts(RawBytes, HpDamageDataOffset, values);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with only element [3] (HpDamage) changed in the HP SAR.
    /// <paramref name="damage"/> is the HP damage taken; set to 0 to fully heal.
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithHpDamageValue(int damage)
    {
        var cur = HpDamageRaw;
        if (cur is null)
            return this;
        return WithHpDamage([cur[0], cur[1], cur[2], damage]);
    }

    /// <summary>
    /// Returns a new record with the Fatigue SAR values replaced (bsId=0x423E).
    /// <paramref name="values"/> must have exactly 4 elements: [FatiguePtsBonus, FatigueAdj, FatigueDamage, ?].
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithFatigueDamage(int[] values)
    {
        if (FatigueDamageDataOffset < 0 || values.Length != 4)
            return this;
        var raw = PatchInts(RawBytes, FatigueDamageDataOffset, values);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with only element [2] (FatigueDamage) changed in the Fatigue SAR.
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithFatigueDamageValue(int damage)
    {
        var cur = FatigueDamageRaw;
        if (cur is null)
            return this;
        return WithFatigueDamage([cur[0], cur[1], damage, cur[3]]);
    }

    /// <summary>
    /// Returns a new record with the PC name replaced by <paramref name="newName"/>.
    /// The name field uses a variable-length encoding (<c>0x01 [uint32_len] [ascii_chars]</c>),
    /// so <see cref="RawBytes"/> is resized accordingly.
    /// Returns this record unchanged when the name field is absent or
    /// <paramref name="newName"/> is <see langword="null"/>.
    /// </summary>
    public CharacterMdyRecord WithName(string? newName)
    {
        if (newName is null || NameLengthOffset < 0)
            return this;

        var oldLen = BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(NameLengthOffset, 4));
        var newEncoded = System.Text.Encoding.ASCII.GetBytes(newName);
        var oldEnd = NameLengthOffset + 4 + oldLen;

        var newRaw = new byte[RawBytes.Length - oldLen + newEncoded.Length];
        // Copy everything up to and including the presence byte (NameLengthOffset - 1).
        RawBytes.AsSpan(0, NameLengthOffset).CopyTo(newRaw);
        // Write the new length and the new chars.
        BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(NameLengthOffset, 4), newEncoded.Length);
        newEncoded.CopyTo(newRaw.AsSpan(NameLengthOffset + 4));
        // Copy any bytes that follow the old name field.
        RawBytes.AsSpan(oldEnd).CopyTo(newRaw.AsSpan(NameLengthOffset + 4 + newEncoded.Length));

        // All SAR data offsets precede the name field and remain valid.
        return this with
        {
            RawBytes = newRaw,
            NameLengthOffset = NameLengthOffset,
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Scans forward from <paramref name="from"/> looking for a byte with value 0x01
    /// (presence flag) immediately followed by <paramref name="sig"/>
    /// (elemSz + elemCnt as 8 LE bytes).
    /// </summary>
    private static int FindSar(ReadOnlySpan<byte> data, int from, int limit, ReadOnlySpan<byte> sig)
    {
        var end = Math.Min(limit, from + MaxScanDistance);
        for (var i = from; i + SarHeaderSize <= end; i++)
        {
            if (data[i] != 0x01)
                continue;
            if (i + 1 + sig.Length > data.Length)
                break;
            if (data.Slice(i + 1, sig.Length).SequenceEqual(sig))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Scans forward from <paramref name="from"/> up to <paramref name="limit"/> looking
    /// for any plausible generic SAR header: presence=0x01, elemSz in {1,2,4,8,16},
    /// elemCnt in [1,512].  Returns the offset of the first match, or −1.
    /// </summary>
    private static int FindAnySar(ReadOnlySpan<byte> data, int from, int limit)
    {
        for (var i = from; i + SarHeaderSize <= limit; i++)
        {
            if (data[i] != 0x01)
                continue;
            if (i + SarHeaderSize > data.Length)
                break;
            var eSize = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(i + 1, 4));
            if (eSize is not (1 or 2 or 4 or 8 or 16))
                continue;
            var eCnt = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(i + 5, 4));
            if (eCnt is < 1 or > 512)
                continue;
            var dataLen = eSize * eCnt;
            if (i + SarHeaderSize + dataLen + 4 > data.Length)
                continue;
            return i;
        }
        return -1;
    }

    /// <summary>Returns the byte offset immediately after the end of a SAR packet.</summary>
    private static int SarEnd(ReadOnlySpan<byte> data, int sarOff, int elemCount)
    {
        var bcOff = sarOff + SarHeaderSize + elemCount * 4;
        if (bcOff + 4 > data.Length)
            return bcOff;
        var bc = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(bcOff, 4));
        if (bc is < 0 or > 256)
            bc = 0;
        return bcOff + 4 + bc * 4;
    }

    private static int[] ReadInts(ReadOnlySpan<byte> data, int off, int count)
    {
        var arr = new int[count];
        for (var i = 0; i < count && off + i * 4 + 4 <= data.Length; i++)
            arr[i] = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(off + i * 4, 4));
        return arr;
    }

    private static byte[] PatchInts(byte[] source, int off, int[] values)
    {
        var raw = (byte[])source.Clone();
        for (var i = 0; i < values.Length && off + i * 4 + 4 <= raw.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(off + i * 4, 4), values[i]);
        return raw;
    }
}
