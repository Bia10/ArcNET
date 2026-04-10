using System.Buffers.Binary;

namespace ArcNET.Formats;

public sealed partial record CharacterMdyRecord
{
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
    /// Returns a new record with the bullet count set to <paramref name="bullets"/>.
    /// Returns this record unchanged when the tech-char Bullets slot is absent.
    /// </summary>
    public CharacterMdyRecord WithBullets(int bullets)
    {
        if (BulletsDataOffset < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(BulletsDataOffset, 4), bullets);
        return this with { RawBytes = raw };
    }

    /// <summary>
    /// Returns a new record with the power-cell count set to <paramref name="powerCells"/>.
    /// Returns this record unchanged when the tech-char PowerCells slot is absent.
    /// </summary>
    public CharacterMdyRecord WithPowerCells(int powerCells)
    {
        if (PowerCellsDataOffset < 0)
            return this;
        var raw = (byte[])RawBytes.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(PowerCellsDataOffset, 4), powerCells);
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
        var newDataLen = newData.Length;
        var newSarTotalLen = SarHeaderSize + newDataLen + 4 + QuestSarBitsetWords * 4;

        var newRaw = new byte[RawBytes.Length - oldSarTotalLen + newSarTotalLen];

        // 1. Prefix: bytes before the SAR presence byte.
        RawBytes.AsSpan(0, sarHeaderOff).CopyTo(newRaw);

        // 2. SAR header: presence(1B) + eSize(4B) + newECnt(4B) + bsId(4B, preserved).
        newRaw[sarHeaderOff] = 0x01;
        BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(sarHeaderOff + 1, 4), QuestSarElementSize);
        BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(sarHeaderOff + 5, 4), newECnt);
        RawBytes.AsSpan(sarHeaderOff + 9, 4).CopyTo(newRaw.AsSpan(sarHeaderOff + 9, 4));

        // 3. New data.
        newData.CopyTo(newRaw.AsSpan(sarHeaderOff + SarHeaderSize));

        // 4. bsCnt (always QuestSarBitsetWords=37) + new bitset.
        var newBsCntOff = sarHeaderOff + SarHeaderSize + newDataLen;
        BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(newBsCntOff, 4), QuestSarBitsetWords);
        for (var i = 0; i < QuestSarBitsetWords; i++)
            BinaryPrimitives.WriteInt32LittleEndian(newRaw.AsSpan(newBsCntOff + 4 + i * 4, 4), newBitset[i]);

        // 5. Suffix: bytes after the old SAR (Gold, Name, etc. — all shift by the size delta).
        var oldSarEnd = sarHeaderOff + oldSarTotalLen;
        RawBytes.AsSpan(oldSarEnd).CopyTo(newRaw.AsSpan(newBsCntOff + 4 + QuestSarBitsetWords * 4));

        // Re-parse to rebuild all derived offsets that may have shifted.
        return Parse(newRaw, out _);
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
        var raw = PatchInts(RawBytes, BlessingProtoDataOffset, blessingProtoIds.ToArray());
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
        var raw = PatchInts(RawBytes, CurseProtoDataOffset, curseProtoIds.ToArray());
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
        var raw = PatchInts(RawBytes, SchematicsDataOffset, schematicProtoIds.ToArray());
        return this with { RawBytes = raw };
    }
}
