using System.Buffers.Binary;

namespace ArcNET.Formats;

public sealed partial record CharacterMdyRecord
{
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
        var bulletsDataOffset = -1;
        var powerCellsDataOffset = -1;
        var portraitDataOffset = -1;
        var nameLengthOffset = -1;
        var effectsDataOffset = -1;
        var effectsElementCount = 0;
        var effectCausesDataOffset = -1;
        var effectCausesElementCount = 0;
        var questDataOffset = -1;
        var questElementCount = 0;
        var reputationDataOffset = -1;
        var rumorsDataOffset = -1;
        var rumorsElementCount = 0;

        // Blessing/Curse/Schematics detection state
        var blessingProtoDataOffset = -1;
        var blessingProtoElementCount = 0;
        var blessingTsDataOffset = -1;
        var curseProtoDataOffset = -1;
        var curseProtoElementCount = 0;
        var curseTsDataOffset = -1;
        var schematicsDataOffset = -1;
        var schematicsElementCount = 0;
        // Tracks a "pending" 4:N:2 SAR that MAY be the first half of a blessing/curse pair
        var pairCandidateDataOffset = -1;
        var pairCandidateECnt = 0;

        var scanPos = end;

        // Cap the extended scan at the next v2 character record boundary.
        // Without this, the extended scan of an NPC v2 record (e.g. LVL=10)
        // would greedily consume all subsequent bytes — including the player's
        // v2 record that follows it in the same mobile.mdy file — because the
        // SAR scanner can match genuine SAR packets inside another record's data.
        //
        // NOTE: V2Magic can appear inside a SAR's own bitset data (e.g. when
        // a SAR's bsCnt field equals 0x02000000 and its first bitset word equals
        // 0x0F000000, which together form the V2Magic prefix).  When the search
        // below finds such a false-positive, the scan loop detects this and
        // advances the search past the SAR boundary.
        var nextMagicSearch = end; // starting position for the V2Magic search

        int FindNextV2Magic(ReadOnlySpan<byte> s, int from)
        {
            for (var mp = from; mp + V2Magic.Length <= s.Length; mp++)
            {
                if (s.Slice(mp, V2Magic.Length).SequenceEqual(V2Magic))
                    return mp;
            }
            return -1;
        }

        var nextMagicPos = FindNextV2Magic(span, nextMagicSearch);

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
            {
                // If the V2Magic that set extLimit is literally inside the current SAR's
                // byte range (false positive in SAR data / bitset bytes), advance past it
                // and keep scanning.
                if (nextMagicPos >= nextSar && nextMagicPos < sarEnd2)
                {
                    nextMagicPos = FindNextV2Magic(span, sarEnd2);
                    extLimit = nextMagicPos >= 0 ? nextMagicPos : Math.Min(span.Length, end + 32768);
                    // Re-check with updated extLimit — if still over, then break.
                    if (sarEnd2 > extLimit)
                        break;
                }
                else
                {
                    break;
                }
            }

            if (bsId == GoldAmountBsId && eSize == 4 && eCnt == 1)
                goldDataOffset = nextSar + SarHeaderSize;

            if (bsId == GameStatsBsId && eSize == 4 && eCnt >= GameStatsElementCount && eCnt <= 13)
            {
                totalKillsDataOffset = nextSar + SarHeaderSize + GameStatsTotalKillsIndex * 4;
                arrowsDataOffset = nextSar + SarHeaderSize + GameStatsArrowsIndex * 4;
                if (eCnt > GameStatsBulletsIndex)
                    bulletsDataOffset = nextSar + SarHeaderSize + GameStatsBulletsIndex * 4;
                if (eCnt > GameStatsPowerCellsIndex)
                    powerCellsDataOffset = nextSar + SarHeaderSize + GameStatsPowerCellsIndex * 4;
            }

            if (bsId == PortraitBsId && eSize == 4 && eCnt == PortraitElementCount)
                portraitDataOffset = nextSar + SarHeaderSize + PortraitIndexElement * 4;

            if (bsId == EffectsBsId && eSize == 4 && eCnt >= 1)
            {
                effectsDataOffset = nextSar + SarHeaderSize;
                effectsElementCount = eCnt;
            }

            if (bsId == EffectCausesBsId && eSize == 4 && eCnt >= 1)
            {
                effectCausesDataOffset = nextSar + SarHeaderSize;
                effectCausesElementCount = eCnt;
            }

            // Quest-log SAR: detected by structural fingerprint (eSize=16, bsCnt=37).
            // The bsId varies per game session and cannot be used for cross-session identification.
            if (eSize == QuestSarElementSize && bsCnt == QuestSarBitsetWords && eCnt is >= 1 and <= 512)
            {
                questDataOffset = nextSar + SarHeaderSize;
                questElementCount = eCnt;
            }

            // Reputation SAR (PcReputationIdx, bit 130): detected by structural fingerprint (eSize=4, eCnt=19, bsCnt=3).
            // Absent in early saves; present once the PC has interacted with at least one faction.
            if (eSize == 4 && eCnt == ReputationSarElementCount && bsCnt == ReputationSarBitsetWords)
                reputationDataOffset = nextSar + SarHeaderSize;

            // Rumors SAR (PcRumorIdx, bit 140): detected by structural fingerprint (eSize=8, bcCnt=39).
            // eCnt varies (grows as rumors are learned); absent in very early saves.
            if (eSize == RumorsSarElementSize && bsCnt == RumorsSarBitsetWords && eCnt is >= 1 and <= 2048)
            {
                rumorsDataOffset = nextSar + SarHeaderSize;
                rumorsElementCount = eCnt;
            }

            // ── Blessing / Curse / Schematics pair detection ──────────────────────
            // Detect consecutive 4:N:2 + 8:N:2 pairs in the post-stat region:
            //   First  pair → PcBlessingIdx (prototype IDs) + PcBlessingTsIdx (8B timestamps)
            //   Second pair → PcCurseIdx   (prototype IDs) + PcCurseTsIdx    (8B timestamps)
            // Standalone 4:K:2 whose first value > 1000 (proto ID range) → PcSchematicsFoundIdx.
            // The eSize and bsCnt checks exclude: conditions (bsCnt=5), Quest (eSize=16),
            // Reputation (bsCnt=3), Rumors (eSize=8, bsCnt=39), CritterHandles (eSize=24).
            if (eSize == 8 && bsCnt == 2 && eCnt == pairCandidateECnt && pairCandidateDataOffset >= 0)
            {
                // 8:N:2 completes the pending 4:N:2 candidate → blessing or curse pair
                if (blessingProtoDataOffset < 0)
                {
                    blessingProtoDataOffset = pairCandidateDataOffset;
                    blessingProtoElementCount = pairCandidateECnt;
                    blessingTsDataOffset = nextSar + SarHeaderSize;
                }
                else if (curseProtoDataOffset < 0)
                {
                    curseProtoDataOffset = pairCandidateDataOffset;
                    curseProtoElementCount = pairCandidateECnt;
                    curseTsDataOffset = nextSar + SarHeaderSize;
                }
                pairCandidateDataOffset = -1;
                pairCandidateECnt = 0;
            }
            else if (eSize == 4 && bsCnt == 2 && eCnt is >= 1 and <= 49)
            {
                // Finalize the previous 4:N:2 candidate (if any) as standalone before recording the new one
                if (pairCandidateDataOffset >= 0 && schematicsDataOffset < 0)
                {
                    var firstVal = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pairCandidateDataOffset, 4));
                    if (firstVal > 1000)
                    {
                        schematicsDataOffset = pairCandidateDataOffset;
                        schematicsElementCount = pairCandidateECnt;
                    }
                }
                pairCandidateDataOffset = nextSar + SarHeaderSize;
                pairCandidateECnt = eCnt;
            }
            else if (pairCandidateDataOffset >= 0)
            {
                // Non-matching SAR — finalize the pending 4:N:2 candidate as standalone
                if (schematicsDataOffset < 0)
                {
                    var firstVal = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pairCandidateDataOffset, 4));
                    if (firstVal > 1000)
                    {
                        schematicsDataOffset = pairCandidateDataOffset;
                        schematicsElementCount = pairCandidateECnt;
                    }
                }
                pairCandidateDataOffset = -1;
                pairCandidateECnt = 0;
            }

            end = sarEnd2;
            scanPos = sarEnd2;
        }

        // Finalize any trailing pair candidate left after the scan loop
        if (pairCandidateDataOffset >= 0 && schematicsDataOffset < 0)
        {
            var firstVal = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pairCandidateDataOffset, 4));
            if (firstVal > 1000)
            {
                schematicsDataOffset = pairCandidateDataOffset;
                schematicsElementCount = pairCandidateECnt;
            }
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
            BulletsDataOffset = bulletsDataOffset,
            PowerCellsDataOffset = powerCellsDataOffset,
            PortraitDataOffset = portraitDataOffset,
            NameLengthOffset = nameLengthOffset,
            PositionAiDataOffset = positionAiDataOffset,
            HpDamageDataOffset = hpDamageDataOffset,
            FatigueDamageDataOffset = fatigueDamageDataOffset,
            EffectsDataOffset = effectsDataOffset,
            EffectsElementCount = effectsElementCount,
            EffectCausesDataOffset = effectCausesDataOffset,
            EffectCausesElementCount = effectCausesElementCount,
            QuestDataOffset = questDataOffset,
            QuestCount = questElementCount,
            ReputationDataOffset = reputationDataOffset,
            RumorsDataOffset = rumorsDataOffset,
            RumorsCount = rumorsElementCount,
            BlessingProtoDataOffset = blessingProtoDataOffset,
            BlessingProtoElementCount = blessingProtoElementCount,
            BlessingTsDataOffset = blessingTsDataOffset,
            CurseProtoDataOffset = curseProtoDataOffset,
            CurseProtoElementCount = curseProtoElementCount,
            CurseTsDataOffset = curseTsDataOffset,
            SchematicsDataOffset = schematicsDataOffset,
            SchematicsElementCount = schematicsElementCount,
        };
    }

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
}
