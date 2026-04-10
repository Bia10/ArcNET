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
public sealed partial record CharacterMdyRecord
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

    // ── bsIds confirmed via live-save RE (Probe, session 6) ──────────────────
    // Primary arrays (found by signature scan; bsIds documented for reference).
    // Stats (28-elem INT32):       bsId=0x4299
    // BasicSkills (12-elem INT32): bsId=0x43C3
    // TechSkills (4-elem INT32):   bsId=0x4A07
    // SpellTech (25-elem INT32):   bsId=0x4A08

    // bsId for the single-INT32 gold-amount SAR that follows the known four arrays.
    private const int GoldAmountBsId = 0x4B13;

    // bsId for the OBJ_TYPE_Gold item handle (24-byte ObjectID) — the actual gold object
    // owned by the PC.  The gold quantity is on the object itself; bsId=0x4B13 caches it.
    // Read-only: not patched (patching the gold-object handle would require a separate probe).
    private const int GoldHandleBsId = 0x4D77;

    // bsId for the active-effects INT32 array (CritterEffectsIdx, ObjF bit 74).
    // Probe confirmed: variable element count per character (5 effects in the test save).
    // Element layout: each int32 is an effect prototype ID currently active on the critter.
    private const int EffectsBsId = 0x49FC;

    // bsId for the effect-cause INT32 array (CritterEffectCauseIdx, ObjF bit 75).
    // Parallel to EffectsBsId: element[n] is the cause ID for Effects[n].
    private const int EffectCausesBsId = 0x49FD;

    // bsId for the 11-element game-statistics SAR (PC-wide data embedded in the v2 record).
    // Confirmed element layout:
    //   [0]=TotalKills, [1..7]=misc counters, [8]=Arrows, [9..10]=critter flags copies.
    //   Inner bits 11/12 = Bullets/PowerCells — absent for magic-focused characters;
    //   present as elements [11]/[12] for tech characters (eCnt grows to 13).
    private const int GameStatsBsId = 0x4D68;
    private const int GameStatsElementCount = 11; // magic-char baseline; tech chars have 13
    private const int GameStatsTotalKillsIndex = 0;
    private const int GameStatsArrowsIndex = 8;
    private const int GameStatsBulletsIndex = 11; // tech chars only (eCnt >= 12)
    private const int GameStatsPowerCellsIndex = 12; // tech chars only (eCnt >= 13)

    // bsId for the 3-element portrait / followers SAR.
    // Confirmed element layout: [0]=MaxFollowersComputed, [1]=PortraitIndex, [2]=0.
    private const int PortraitBsId = 0x4DA4;
    private const int PortraitElementCount = 3;
    private const int PortraitMaxFollowersElement = 0;
    private const int PortraitIndexElement = 1;

    // Quest-log SAR structural fingerprint (session-independent — bsId varies per game session).
    // eSize=16: each quest-log entry is a 16-byte record (4 × INT32).
    // bsCnt=37: the bitset following the data is always 37 uint32 words (1184 bits), covering
    //   the full Arcanum quest-slot address space.  This value is stable across all tested saves:
    //   Slot0013 (bsId=0x4A00, eCnt=9), Slot0100 (bsId=0x45C7, eCnt=34), Slot0120 (bsId=0x6AFD, eCnt=46).
    // The Nth set bit in the 37-word bitset is the Arcanum quest-slot ID for the Nth 16-byte entry.
    private const int QuestSarElementSize = 16;
    private const int QuestSarBitsetWords = 37;

    // Reputation SAR (PC field bit 130 — PcReputationIdx): INT32[19] with bcCnt=3.
    // Absent in early saves (not triggered until PC interacts with factions).
    // Confirmed: Slot0100 (bsId=0x51E4), Slot0120 (bsId=0x4E2A), Slot0177 (bsId=0x5244) — all same session.
    private const int ReputationSarElementCount = 19;
    private const int ReputationSarBitsetWords = 3;

    // Blessing / Curse / Schematics SAR structural detection (session 12 RE findings, Slot0177):
    //
    // Blessing pair (PcBlessingIdx bit 135 + PcBlessingTsIdx bit 136):
    //   First occurrence of a consecutive 4:N:2 + 8:N:2 pair (same N) found in the post-stat
    //   extended scan region.  The INT32[N] contains blessing-effect prototype IDs, one per god.
    //   The 8B×N array contains timestamp data (8 bytes per blessing entry).
    //   Confirmed: Slot0177 SAR#13/14 — bsIds 0x48E9/0x48EA — N grew from 5→7 at Slot0174→0177.
    //
    // Curse pair (PcCurseIdx bit 137 + PcCurseTsIdx bit 138):
    //   Second occurrence of a consecutive 4:M:2 + 8:M:2 pair.
    //   Confirmed: Slot0177 SAR#16/17 — bsIds 0x2AA3/0x48E2 — M=2 (2 gods' curses), values [67,53].
    //
    // Schematics (PcSchematicsFoundIdx bit 142):
    //   Standalone 4:K:2 in extended scan whose first INT32 value exceeds 1000 (tech proto ID range)
    //   and is NOT immediately followed by a matching 8:K:2 SAR.
    //   Confirmed: Slot0177 SAR#19 — bsId=0x5228 — K=4 values [5090,4810,4010,5410].
    private const int BlessingTsElementSize = 8; // each blessing/curse timestamp entry is 8 bytes in v2 format

    // Rumors SAR (PC field bit 140 — PcRumorIdx): 8-byte elements × variable eCnt, bcCnt=39.
    // eCnt grows as the player learns new rumors (0 at start, ~60+ at end-game).
    // Absent in early saves; first appears around level 10 when the player enters populated areas.
    // Session-independent structural fingerprint: eSize=8, bcCnt=39.
    // Confirmed across sessions and slot range 0033–0120.
    private const int RumorsSarElementSize = 8;
    private const int RumorsSarBitsetWords = 39;

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
    /// Byte offset within <see cref="RawBytes"/> of element [0] in the active-effects SAR
    /// (bsId=0x49FC, INT32[n]).  −1 when absent.
    /// </summary>
    internal int EffectsDataOffset { get; init; } = -1;

    /// <summary>Number of elements in the active-effects SAR (bsId=0x49FC).  0 when absent.</summary>
    internal int EffectsElementCount { get; init; }

    /// <summary>
    /// Active effect prototype IDs (bsId=0x49FC).  Each element is a prototype ID of an
    /// effect currently applied to this character.  Returns <see langword="null"/> when
    /// the SAR is absent (no active effects).
    /// </summary>
    public int[]? Effects => EffectsDataOffset >= 0 ? ReadInts(RawBytes, EffectsDataOffset, EffectsElementCount) : null;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [0] in the effect-causes SAR
    /// (bsId=0x49FD, INT32[n]).  −1 when absent.  Parallel to <see cref="Effects"/>.
    /// </summary>
    internal int EffectCausesDataOffset { get; init; } = -1;

    /// <summary>Number of elements in the effect-causes SAR (bsId=0x49FD).  0 when absent.</summary>
    internal int EffectCausesElementCount { get; init; }

    /// <summary>
    /// Effect-cause IDs parallel to <see cref="Effects"/> (bsId=0x49FD).
    /// Returns <see langword="null"/> when the SAR is absent.
    /// </summary>
    public int[]? EffectCauses =>
        EffectCausesDataOffset >= 0 ? ReadInts(RawBytes, EffectCausesDataOffset, EffectCausesElementCount) : null;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the first byte of the rumors SAR element data.
    /// Identified by structural fingerprint: eSize=8, bcCnt=<see cref="RumorsSarBitsetWords"/> (39).
    /// −1 when absent (early saves before the PC has entered populated areas).
    /// </summary>
    internal int RumorsDataOffset { get; init; } = -1;

    /// <summary>Number of rumor entries (eCnt in the rumors SAR).  0 when absent.</summary>
    public int RumorsCount { get; init; }

    /// <summary>
    /// Raw rumor data (PC field bit 140, PcRumorIdx), or <see langword="null"/> when absent.
    /// Each entry is exactly <see cref="RumorsSarElementSize"/> (8) bytes.
    /// eCnt grows as the player learns new rumors.
    /// </summary>
    public byte[]? RumorsRaw =>
        RumorsDataOffset >= 0 ? RawBytes.AsSpan(RumorsDataOffset, RumorsCount * RumorsSarElementSize).ToArray() : null;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the first byte of quest-log entry data.
    /// Identified by structural fingerprint: eSize=16, bsCnt=<see cref="QuestSarBitsetWords"/> (37).
    /// −1 when absent.
    /// </summary>
    internal int QuestDataOffset { get; init; } = -1;

    /// <summary>Number of quest-log entries (eCnt in the quest SAR).  0 when absent.</summary>
    public int QuestCount { get; init; }

    /// <summary>
    /// Raw quest-log entry data, or <see langword="null"/> when absent.
    /// Each entry is exactly <see cref="QuestSarElementSize"/> (16) bytes.
    /// The quest-slot IDs are encoded in the bitset — see <see cref="QuestBitsetRaw"/>.
    /// </summary>
    public byte[]? QuestDataRaw =>
        QuestDataOffset >= 0 ? RawBytes.AsSpan(QuestDataOffset, QuestCount * QuestSarElementSize).ToArray() : null;

    /// <summary>
    /// The 37-word bitset that follows the quest-log data, or <see langword="null"/> when absent.
    /// Bit N (0-indexed across all 37 words, LSB-first within each word) is set when Arcanum
    /// quest-slot N has a live entry in the log.  The Nth set bit corresponds to the Nth
    /// 16-byte entry in <see cref="QuestDataRaw"/>.
    /// </summary>
    public int[]? QuestBitsetRaw
    {
        get
        {
            if (QuestDataOffset < 0)
                return null;
            // Quest SAR layout: ...data[eCnt*16] + bsCnt(4B) + bitset[bsCnt*4B]
            // +4 skips the bsCnt field to reach the first bitset word.
            var bitsetOff = QuestDataOffset + QuestCount * QuestSarElementSize + 4;
            if (bitsetOff + QuestSarBitsetWords * 4 > RawBytes.Length)
                return null;
            return ReadInts(RawBytes, bitsetOff, QuestSarBitsetWords);
        }
    }

    /// <summary>
    /// Decodes the quest bitset into the list of quest proto IDs (slot indices) for which
    /// log entries exist, in ascending order.  Returns <see langword="null"/> when no quest
    /// SAR is present.  Each returned ID is a bit index in <see cref="QuestBitsetRaw"/>.
    /// In Arcanum, quest proto IDs start at 1000 (quests.mes).
    /// </summary>
    public int[]? QuestActiveIds
    {
        get
        {
            var bits = QuestBitsetRaw;
            if (bits is null)
                return null;
            var ids = new List<int>(QuestCount);
            for (int wi = 0; wi < bits.Length; wi++)
            {
                uint word = (uint)bits[wi];
                for (int bi = 0; bi < 32; bi++)
                    if ((word & (1u << bi)) != 0)
                        ids.Add(wi * 32 + bi);
            }
            return ids.ToArray();
        }
    }

    /// <summary>
    /// Decoded quest entries.  Returns <see langword="null"/> when the quest SAR is absent.
    /// Each entry pairs a quest proto ID (from the bitset) with its 16-byte header:
    /// INT32[0] = donor/context proto ID, INT32[1] = game-tick timestamp, INT32[2] = state,
    /// INT32[3] = 0 (reserved).  Observed low bits: 0x01=active/triggered,
    /// 0x02=primary-complete, 0x04=secondary-complete. Late-game saves also use 0x100.
    /// </summary>
    public IReadOnlyList<(int ProtoId, int Context, int Timestamp, int State)>? QuestEntries
    {
        get
        {
            var ids = QuestActiveIds;
            var data = QuestDataRaw;
            if (ids is null || data is null || ids.Length != QuestCount)
                return null;
            var result = new (int, int, int, int)[QuestCount];
            for (int i = 0; i < QuestCount; i++)
            {
                int off = i * QuestSarElementSize;
                int context = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(off, 4));
                int timestamp = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(off + 4, 4));
                int state = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(off + 8, 4));
                result[i] = (ids[i], context, timestamp, state);
            }
            return result;
        }
    }

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of the first element of the reputation SAR.
    /// Identified by structural fingerprint: eSize=4, eCnt=19, bsCnt=3.
    /// −1 when absent (early saves before the PC has interacted with any faction).
    /// </summary>
    internal int ReputationDataOffset { get; init; } = -1;

    /// <summary>
    /// Raw 19-element faction-reputation array (PC field bit 130, PcReputationIdx),
    /// or <see langword="null"/> when absent.
    /// Identified by structural fingerprint: eSize=4, eCnt=19, bsCnt=3.
    /// The bsId varies per game session.
    /// </summary>
    public int[]? ReputationRaw =>
        ReputationDataOffset >= 0 ? ReadInts(RawBytes, ReputationDataOffset, ReputationSarElementCount) : null;

    /// <summary>
    /// Decoded faction slot indices for the reputation SAR (the bitset set-bit positions).
    /// Element i of <see cref="ReputationRaw"/> corresponds to faction-slot <c>ReputationFactionSlots[i]</c>.
    /// Slot layout (from Slot0177/0178 RE): slots 0–12 = 13 main factions;
    /// slots 64–69 = 6 additional factions (total 19).
    /// Returns <see langword="null"/> when no reputation SAR is present.
    /// </summary>
    public int[]? ReputationFactionSlots
    {
        get
        {
            if (ReputationDataOffset < 0)
                return null;
            // SAR layout from header offset (ReputationDataOffset - 13):
            // 1B presence + 4B eSize + 4B eCnt + 4B bsId + eCnt*4B data + 4B bsCnt + bsCnt*4B bitset
            // bsCnt = 3 for reputation SAR.
            int dataEnd = ReputationDataOffset + ReputationSarElementCount * 4;
            int bsCntOff = dataEnd; // bsCnt field
            if (bsCntOff + 4 > RawBytes.Length)
                return null;
            int bsCnt = BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(bsCntOff, 4));
            if (bsCnt <= 0 || bsCntOff + 4 + bsCnt * 4 > RawBytes.Length)
                return null;
            var slots = new List<int>(ReputationSarElementCount);
            for (int wi = 0; wi < bsCnt; wi++)
            {
                uint word = BinaryPrimitives.ReadUInt32LittleEndian(RawBytes.AsSpan(bsCntOff + 4 + wi * 4, 4));
                for (int bi = 0; bi < 32; bi++)
                    if ((word & (1u << bi)) != 0)
                        slots.Add(wi * 32 + bi);
            }
            return slots.Count == ReputationSarElementCount ? slots.ToArray() : null;
        }
    }

    // ── Blessing / Curse / Schematics offsets (session 12 findings) ──────────

    /// <summary>
    /// Byte offset of element [0] of the blessing-prototype-ID array (PcBlessingIdx, bit 135).
    /// Identified by: first 4:N:2 + 8:N:2 consecutive pair in the post-stat extended scan.
    /// −1 when absent (character has not received any divine blessing).
    /// </summary>
    internal int BlessingProtoDataOffset { get; init; } = -1;

    /// <summary>Number of blessing entries (N in 4:N:2 + 8:N:2 pair).  0 when absent.</summary>
    public int BlessingProtoElementCount { get; init; }

    /// <summary>
    /// Byte offset of element [0] of the blessing-timestamp array (PcBlessingTsIdx, bit 136).
    /// Each entry is <see cref="BlessingTsElementSize"/> (8) bytes.
    /// −1 when absent.
    /// </summary>
    internal int BlessingTsDataOffset { get; init; } = -1;

    /// <summary>
    /// Byte offset of element [0] of the curse-prototype-ID array (PcCurseIdx, bit 137).
    /// Identified by: second 4:M:2 + 8:M:2 consecutive pair in the post-stat extended scan.
    /// −1 when absent (character has not been cursed by any god).
    /// </summary>
    internal int CurseProtoDataOffset { get; init; } = -1;

    /// <summary>Number of curse entries (M in 4:M:2 + 8:M:2 pair).  0 when absent.</summary>
    public int CurseProtoElementCount { get; init; }

    /// <summary>
    /// Byte offset of element [0] of the curse-timestamp array (PcCurseTsIdx, bit 138).
    /// Each entry is <see cref="BlessingTsElementSize"/> (8) bytes.
    /// −1 when absent.
    /// </summary>
    internal int CurseTsDataOffset { get; init; } = -1;

    /// <summary>
    /// Byte offset of element [0] of the schematics-found prototype-ID array (PcSchematicsFoundIdx, bit 142).
    /// Identified by: standalone 4:K:2 in post-stat scan whose first value exceeds 1000 (tech proto ID range).
    /// −1 when absent (character found no tech schematics — typical for pure magic builds).
    /// </summary>
    internal int SchematicsDataOffset { get; init; } = -1;

    /// <summary>Number of schematics entries (K).  0 when absent.</summary>
    public int SchematicsElementCount { get; init; }

    /// <summary>
    /// Divine blessing prototype IDs (PcBlessingIdx, bit 135), one per god who blessed this character.
    /// Returns <see langword="null"/> when the character has received no blessings.
    /// </summary>
    public int[]? BlessingRaw =>
        BlessingProtoDataOffset >= 0 ? ReadInts(RawBytes, BlessingProtoDataOffset, BlessingProtoElementCount) : null;

    /// <summary>
    /// Raw blessing timestamp data (PcBlessingTsIdx, bit 136): 8 bytes per blessing entry,
    /// parallel to <see cref="BlessingRaw"/>.
    /// Returns <see langword="null"/> when absent.
    /// </summary>
    public byte[]? BlessingTsRaw =>
        BlessingTsDataOffset >= 0
            ? RawBytes.AsSpan(BlessingTsDataOffset, BlessingProtoElementCount * BlessingTsElementSize).ToArray()
            : null;

    /// <summary>
    /// Divine curse prototype IDs (PcCurseIdx, bit 137), one per god who cursed this character.
    /// Returns <see langword="null"/> when the character has no active curses.
    /// </summary>
    public int[]? CurseRaw =>
        CurseProtoDataOffset >= 0 ? ReadInts(RawBytes, CurseProtoDataOffset, CurseProtoElementCount) : null;

    /// <summary>
    /// Raw curse timestamp data (PcCurseTsIdx, bit 138): 8 bytes per curse entry,
    /// parallel to <see cref="CurseRaw"/>.
    /// Returns <see langword="null"/> when absent.
    /// </summary>
    public byte[]? CurseTsRaw =>
        CurseTsDataOffset >= 0
            ? RawBytes.AsSpan(CurseTsDataOffset, CurseProtoElementCount * BlessingTsElementSize).ToArray()
            : null;

    /// <summary>
    /// Tech schematic prototype IDs found by this character (PcSchematicsFoundIdx, bit 142).
    /// Returns <see langword="null"/> when the character has found no tech schematics
    /// (typical for pure magic builds).
    /// </summary>
    public int[]? SchematicsRaw =>
        SchematicsDataOffset >= 0 ? ReadInts(RawBytes, SchematicsDataOffset, SchematicsElementCount) : null;

    /// <summary>
    /// Raw four-element Fatigue SAR values (bsId=0x423E), or null when absent.
    /// /// Element [2] is the fatigue damage — set to reduce displayed Fatigue.
    /// </summary>
    public int[]? FatigueDamageRaw =>
        FatigueDamageDataOffset >= 0 ? ReadInts(RawBytes, FatigueDamageDataOffset, 4) : null;

    /// <summary>Fatigue damage taken from bsId=0x423E element [2].  0 when at full fatigue or SAR absent.</summary>
    public int FatigueDamage => FatigueDamageRaw is { } r ? r[2] : 0;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [11] (Bullets) in the
    /// 12-or-13-element game-statistics SAR (bsId=0x4D68).  −1 when absent (magic chars or SAR not found).
    /// </summary>
    internal int BulletsDataOffset { get; init; } = -1;

    /// <summary>
    /// The player's current bullet count (bsId=0x4D68[11]).
    /// Returns 0 when the element is absent (magic-focused characters have no Bullets slot).
    /// </summary>
    public int Bullets =>
        BulletsDataOffset >= 0 ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(BulletsDataOffset, 4)) : 0;

    /// <summary>
    /// Byte offset within <see cref="RawBytes"/> of element [12] (PowerCells) in the
    /// 13-element game-statistics SAR (bsId=0x4D68).  −1 when absent.
    /// </summary>
    internal int PowerCellsDataOffset { get; init; } = -1;

    /// <summary>
    /// The player's current power-cell count (bsId=0x4D68[12]).
    /// Returns 0 when the element is absent.
    /// </summary>
    public int PowerCells =>
        PowerCellsDataOffset >= 0
            ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(PowerCellsDataOffset, 4))
            : 0;

    // Parsing members are defined in the companion partial file.

    // Mutation members are defined in the companion partial file.

    // ── Private helpers ───────────────────────────────────────────────────────

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
