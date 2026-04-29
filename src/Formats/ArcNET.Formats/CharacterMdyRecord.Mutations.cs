using System.Buffers.Binary;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

public sealed partial record CharacterMdyRecord
{
    // ── Patch methods ─────────────────────────────────────────────────────────

    /// <summary>Returns a new record with the stats array replaced by <paramref name="stats"/>.</summary>
    public CharacterMdyRecord WithStats(int[] stats)
    {
        var raw = CharacterMdyRecordBinary.PatchInts(RawBytes, StatsDataOffset, stats);
        return this with { RawBytes = raw, Stats = stats };
    }

    /// <summary>Returns a new record with the basic skills array replaced by <paramref name="basicSkills"/>.</summary>
    public CharacterMdyRecord WithBasicSkills(int[] basicSkills) =>
        PatchIntsIfPresent(
            BasicSkillsDataOffset,
            basicSkills,
            raw => this with { RawBytes = raw, BasicSkills = basicSkills }
        );

    /// <summary>Returns a new record with the tech skills array replaced by <paramref name="techSkills"/>.</summary>
    public CharacterMdyRecord WithTechSkills(int[] techSkills) =>
        PatchIntsIfPresent(
            TechSkillsDataOffset,
            techSkills,
            raw => this with { RawBytes = raw, TechSkills = techSkills }
        );

    /// <summary>Returns a new record with the spell / tech array replaced by <paramref name="spellTech"/>.</summary>
    public CharacterMdyRecord WithSpellTech(int[] spellTech) =>
        PatchIntsIfPresent(SpellTechDataOffset, spellTech, raw => this with { RawBytes = raw, SpellTech = spellTech });

    /// <summary>
    /// Returns a new record with the gold amount set to <paramref name="gold"/>.
    /// Returns this record unchanged when the gold SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithGold(int gold) => PatchInt32IfPresent(GoldDataOffset, gold);

    /// <summary>
    /// Returns a new record with the arrow count set to <paramref name="arrows"/>.
    /// Returns this record unchanged when the game-statistics SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithArrows(int arrows) => PatchInt32IfPresent(ArrowsDataOffset, arrows);

    /// <summary>
    /// Returns a new record with the portrait index set to <paramref name="portraitIndex"/>.
    /// Returns this record unchanged when the portrait SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithPortraitIndex(int portraitIndex) =>
        PatchInt32IfPresent(PortraitDataOffset, portraitIndex);

    /// <summary>
    /// Returns a new record with the max-followers computed value set to <paramref name="maxFollowers"/>
    /// (bsId=0x4DA4[0]).
    /// Returns this record unchanged when the portrait SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithMaxFollowers(int maxFollowers) =>
        PatchInt32IfPresent(MaxFollowersDataOffset, maxFollowers);

    /// <summary>
    /// Returns a new record with the total kill count set to <paramref name="totalKills"/>.
    /// Returns this record unchanged when the game-statistics SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithTotalKills(int totalKills) => PatchInt32IfPresent(TotalKillsDataOffset, totalKills);

    /// <summary>
    /// Returns a new record with the bullet count set to <paramref name="bullets"/>.
    /// Returns this record unchanged when the tech-char Bullets slot is absent.
    /// </summary>
    public CharacterMdyRecord WithBullets(int bullets) => PatchInt32IfPresent(BulletsDataOffset, bullets);

    /// <summary>
    /// Returns a new record with the power-cell count set to <paramref name="powerCells"/>.
    /// Returns this record unchanged when the tech-char PowerCells slot is absent.
    /// </summary>
    public CharacterMdyRecord WithPowerCells(int powerCells) => PatchInt32IfPresent(PowerCellsDataOffset, powerCells);

    /// <summary>
    /// Returns a new record with the three position / AI SAR values replaced (bsId=0x4DA3).
    /// <paramref name="values"/> must have exactly 3 elements.
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithPositionAi(int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (PositionAiDataOffset < 0)
            return this;
        if (values.Length != 3)
            throw new ArgumentException("Must have exactly 3 elements.", nameof(values));

        return PatchIntsIfPresent(PositionAiDataOffset, values);
    }

    /// <summary>
    /// Returns a new record with the HP SAR values replaced (bsId=0x4046).
    /// <paramref name="values"/> must have exactly 4 elements: [AcBonus, HpPtsBonus, HpAdj, HpDamage].
    /// Returns this record unchanged when the SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithHpDamage(int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (HpDamageDataOffset < 0)
            return this;
        if (values.Length != 4)
            throw new ArgumentException("Must have exactly 4 elements.", nameof(values));

        return PatchIntsIfPresent(HpDamageDataOffset, values);
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
        ArgumentNullException.ThrowIfNull(values);
        if (FatigueDamageDataOffset < 0)
            return this;
        if (values.Length != 4)
            throw new ArgumentException("Must have exactly 4 elements.", nameof(values));

        return PatchIntsIfPresent(FatigueDamageDataOffset, values);
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
    /// Returns a new record with the quest-log data bytes replaced in-place.
    /// <paramref name="newData"/> must be exactly <c>QuestCount × 16</c> bytes
    /// (same number of entries — no resize).  Use <see cref="WithQuestStateRaw"/>
    /// when the entry count must change.
    /// Returns this record unchanged when the quest SAR is absent or the length is wrong.
    /// </summary>
    public CharacterMdyRecord WithQuestDataRaw(ReadOnlySpan<byte> newData)
    {
        if (QuestDataOffset < 0 || newData.Length != QuestCount * QuestSarElementSize)
            return this;
        var raw = (byte[])RawBytes.Clone();
        newData.CopyTo(raw.AsSpan(QuestDataOffset, newData.Length));
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the quest-log data and bitset fully replaced.
    /// This overload supports changing the entry count (eCnt); all derived offsets
    /// are recomputed by re-parsing the resulting bytes.
    /// <para>
    /// <paramref name="newData"/> must be a multiple of <see cref="QuestSarElementSize"/> (16) bytes.
    /// <paramref name="newBitset"/> must have exactly <see cref="QuestSarBitsetWords"/> (37) elements.
    /// </para>
    /// Returns this record unchanged when the quest SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithQuestStateRaw(ReadOnlySpan<byte> newData, ReadOnlySpan<int> newBitset)
    {
        if (QuestDataOffset < 0)
            return this;
        if (newData.Length % QuestSarElementSize != 0)
            throw new ArgumentException($"newData must be a multiple of {QuestSarElementSize} bytes.", nameof(newData));
        if (newBitset.Length != QuestSarBitsetWords)
            throw new ArgumentException(
                $"newBitset must have exactly {QuestSarBitsetWords} elements.",
                nameof(newBitset)
            );

        var newECnt = newData.Length / QuestSarElementSize;
        var sarHeaderOff = QuestDataOffset - SarHeaderSize;
        var oldDataLen = QuestCount * QuestSarElementSize;
        var oldSarTotalLen = SarHeaderSize + oldDataLen + 4 + QuestSarBitsetWords * 4;

        Span<byte> initial = stackalloc byte[512];
        using var buf = new ValueByteBuffer(initial);

        // 1. Prefix: bytes before the SAR presence byte.
        buf.Write(RawBytes.AsSpan(0, sarHeaderOff));

        // 2. SAR header: presence(1B) + eSize(4B) + newECnt(4B) + bsId(4B, preserved).
        buf.Write(0x01);
        buf.WriteInt32LittleEndian(QuestSarElementSize);
        buf.WriteInt32LittleEndian(newECnt);
        buf.Write(RawBytes.AsSpan(sarHeaderOff + 9, 4)); // bsId preserved

        // 3. New data.
        buf.Write(newData);

        // 4. bsCnt (always QuestSarBitsetWords=37) + new bitset.
        buf.WriteInt32LittleEndian(QuestSarBitsetWords);
        buf.WriteInt32LittleEndianAll(newBitset);

        // 5. Suffix: bytes after the old SAR (Gold, Name, etc. — all shift by the size delta).
        var oldSarEnd = sarHeaderOff + oldSarTotalLen;
        buf.Write(RawBytes.AsSpan(oldSarEnd));

        // Re-parse to rebuild all derived offsets that may have shifted.
        return Parse(buf.ToArray(), out _);
    }

    /// <summary>
    /// Returns a new record with the rumors element data replaced in place.
    /// Only replaces the element bytes (same eCnt); does not resize the SAR.
    /// Returns this record unchanged when the rumors SAR is absent or
    /// <paramref name="newData"/> does not match the current element byte count.
    /// </summary>
    public CharacterMdyRecord WithRumorsRaw(ReadOnlySpan<byte> newData)
    {
        if (RumorsDataOffset < 0 || newData.Length != RumorsCount * RumorsSarElementSize)
            return this;
        var raw = (byte[])RawBytes.Clone();
        newData.CopyTo(raw.AsSpan(RumorsDataOffset, newData.Length));
        return this with { RawBytes = raw };
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
        var oldEnd = NameLengthOffset + 4 + oldLen;
        var newNameLen = System.Text.Encoding.ASCII.GetByteCount(newName); // ASCII: 1 byte per char

        Span<byte> initial = stackalloc byte[512];
        using var buf = new ValueByteBuffer(initial);
        // Prefix: everything up to and including the presence byte (NameLengthOffset - 1).
        buf.Write(RawBytes.AsSpan(0, NameLengthOffset));
        // New length + ASCII-encoded name.
        buf.WriteInt32LittleEndian(newNameLen);
        buf.WriteAsciiEncoded(newName.AsSpan());
        // Suffix: any bytes after the old name field.
        buf.Write(RawBytes.AsSpan(oldEnd));

        // All SAR data offsets precede the name field and remain valid.
        return this with
        {
            RawBytes = buf.ToArray(),
            NameLengthOffset = NameLengthOffset,
        };
    }

    /// <summary>
    /// Returns a new record with the faction-reputation values replaced in-place.
    /// <paramref name="values"/> must have exactly <see cref="ReputationSarElementCount"/> (19) elements.
    /// Returns this record unchanged when the reputation SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithReputationRaw(ReadOnlySpan<int> values)
    {
        if (ReputationDataOffset < 0 || values.Length != ReputationSarElementCount)
            return this;
        var raw = PatchInts(RawBytes, ReputationDataOffset, values);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the blessing prototype IDs replaced in-place.
    /// The count must match the existing <see cref="BlessingProtoElementCount"/>;
    /// use the OFF format / MobData bridge to add or remove blessings.
    /// Returns this record unchanged when the blessing SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithBlessingRaw(ReadOnlySpan<int> blessingProtoIds)
    {
        if (BlessingProtoDataOffset < 0 || blessingProtoIds.Length != BlessingProtoElementCount)
            return this;
        var raw = PatchInts(RawBytes, BlessingProtoDataOffset, blessingProtoIds);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the curse prototype IDs replaced in-place.
    /// The count must match the existing <see cref="CurseProtoElementCount"/>.
    /// Returns this record unchanged when the curse SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithCurseRaw(ReadOnlySpan<int> curseProtoIds)
    {
        if (CurseProtoDataOffset < 0 || curseProtoIds.Length != CurseProtoElementCount)
            return this;
        var raw = PatchInts(RawBytes, CurseProtoDataOffset, curseProtoIds);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the tech-schematic prototype IDs replaced in-place.
    /// The count must match the existing <see cref="SchematicsElementCount"/>.
    /// Returns this record unchanged when the schematics SAR is absent.
    /// </summary>
    public CharacterMdyRecord WithSchematicsRaw(ReadOnlySpan<int> schematicProtoIds)
    {
        if (SchematicsDataOffset < 0 || schematicProtoIds.Length != SchematicsElementCount)
            return this;
        var raw = PatchInts(RawBytes, SchematicsDataOffset, schematicProtoIds);
        return this with { RawBytes = raw };
    }

    private CharacterMdyRecord PatchInt32IfPresent(int offset, int value)
    {
        if (offset < 0)
            return this;

        return this with
        {
            RawBytes = CharacterMdyRecordBinary.CloneAndWriteInt32(RawBytes, offset, value),
        };
    }

    private CharacterMdyRecord PatchIntsIfPresent(int offset, ReadOnlySpan<int> values)
    {
        if (offset < 0)
            return this;

        return this with
        {
            RawBytes = CharacterMdyRecordBinary.PatchInts(RawBytes, offset, values),
        };
    }

    private CharacterMdyRecord PatchIntsIfPresent(
        int offset,
        ReadOnlySpan<int> values,
        Func<byte[], CharacterMdyRecord> apply
    )
    {
        if (offset < 0)
            return this;

        return apply(CharacterMdyRecordBinary.PatchInts(RawBytes, offset, values));
    }
}
