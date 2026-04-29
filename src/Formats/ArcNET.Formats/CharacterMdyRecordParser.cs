using System.Buffers.Binary;

namespace ArcNET.Formats;

internal static class CharacterMdyRecordParser
{
    public static CharacterMdyRecord Parse(ReadOnlySpan<byte> span, out int consumed)
    {
        var layout = new CharacterMdyRecordLayout();

        var statOff = FindSar(
            span,
            CharacterMdyRecordSchema.V2Magic.Length,
            span.Length,
            CharacterMdyRecordSchema.StatSig
        );
        if (statOff < 0)
            throw new InvalidDataException("v2 character record: stats SAR not found within scan range");

        layout.StatsDataOffset = statOff + CharacterMdyRecordSchema.SarHeaderSize;
        if (layout.StatsDataOffset + 28 * 4 > span.Length)
            throw new InvalidDataException("v2 character record: stats SAR data extends beyond available bytes");

        var stats = CharacterMdyRecordBinary.ReadInts(span, layout.StatsDataOffset, 28);
        var end = SarEnd(span, statOff, 28);

        var posAiOff = FindSar(
            span,
            CharacterMdyRecordSchema.V2Magic.Length,
            statOff,
            CharacterMdyRecordSchema.PositionAiSig
        );
        if (posAiOff >= 0 && posAiOff + CharacterMdyRecordSchema.SarHeaderSize + 3 * 4 <= span.Length)
            layout.PositionAiDataOffset = posAiOff + CharacterMdyRecordSchema.SarHeaderSize;

        var hpDmgOff = FindSar(
            span,
            CharacterMdyRecordSchema.V2Magic.Length,
            statOff,
            CharacterMdyRecordSchema.HpDamageSig
        );
        if (hpDmgOff >= 0 && hpDmgOff + CharacterMdyRecordSchema.SarHeaderSize + 4 * 4 <= span.Length)
            layout.HpDamageDataOffset = hpDmgOff + CharacterMdyRecordSchema.SarHeaderSize;

        var fatOff = FindSar(
            span,
            CharacterMdyRecordSchema.V2Magic.Length,
            statOff,
            CharacterMdyRecordSchema.FatigueSig
        );
        if (fatOff >= 0 && fatOff + CharacterMdyRecordSchema.SarHeaderSize + 4 * 4 <= span.Length)
            layout.FatigueDamageDataOffset = fatOff + CharacterMdyRecordSchema.SarHeaderSize;

        int[] basicSkills = new int[12];
        int[] techSkills = new int[4];
        int[] spellTech = new int[25];
        var hasCompleteData = false;

        var basicOff = FindSar(span, end, span.Length, CharacterMdyRecordSchema.BasicSkillSig);
        if (basicOff >= 0)
        {
            layout.BasicSkillsDataOffset = basicOff + CharacterMdyRecordSchema.SarHeaderSize;
            basicSkills = CharacterMdyRecordBinary.ReadInts(span, layout.BasicSkillsDataOffset, 12);
            end = SarEnd(span, basicOff, 12);

            var techOff = FindSar(span, end, span.Length, CharacterMdyRecordSchema.TechSkillSig);
            if (techOff >= 0)
            {
                layout.TechSkillsDataOffset = techOff + CharacterMdyRecordSchema.SarHeaderSize;
                techSkills = CharacterMdyRecordBinary.ReadInts(span, layout.TechSkillsDataOffset, 4);
                end = SarEnd(span, techOff, 4);

                var spellOff = FindSar(span, end, span.Length, CharacterMdyRecordSchema.SpellTechSig);
                if (spellOff >= 0)
                {
                    layout.SpellTechDataOffset = spellOff + CharacterMdyRecordSchema.SarHeaderSize;
                    spellTech = CharacterMdyRecordBinary.ReadInts(span, layout.SpellTechDataOffset, 25);
                    end = SarEnd(span, spellOff, 25);
                    hasCompleteData = true;
                }
            }
        }

        var pairCandidateDataOffset = -1;
        var pairCandidateECnt = 0;
        var scanPos = end;
        var nextMagicPos = FindNextV2Magic(span, end);
        var extLimit =
            nextMagicPos >= 0 ? nextMagicPos : Math.Min(span.Length, end + CharacterMdyRecordSchema.ExtendedScanLimit);

        while (scanPos + CharacterMdyRecordSchema.SarHeaderSize <= extLimit)
        {
            var nextSar = FindAnySar(span, scanPos, extLimit);
            if (nextSar < 0)
                break;

            var elementSize = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(nextSar + 1, 4));
            var elementCount = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(nextSar + 5, 4));
            var bitsetId = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(nextSar + 9, 4));
            var dataLen = elementSize * elementCount;
            var bitsetCountOffset = nextSar + CharacterMdyRecordSchema.SarHeaderSize + dataLen;
            if (bitsetCountOffset + 4 > span.Length)
                break;

            var bitsetWordCount = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(bitsetCountOffset, 4));
            if (bitsetWordCount is < 0 or > CharacterMdyRecordSchema.MaxSarBitsetWords)
            {
                scanPos = nextSar + 1;
                continue;
            }

            var sarEnd = bitsetCountOffset + 4 + bitsetWordCount * 4;
            if (sarEnd > extLimit)
            {
                if (nextMagicPos >= nextSar && nextMagicPos < sarEnd)
                {
                    nextMagicPos = FindNextV2Magic(span, sarEnd);
                    extLimit =
                        nextMagicPos >= 0
                            ? nextMagicPos
                            : Math.Min(span.Length, end + CharacterMdyRecordSchema.ExtendedScanLimit);
                    if (sarEnd > extLimit)
                        break;
                }
                else
                {
                    break;
                }
            }

            TrackScalarOffsets(layout, nextSar, bitsetId, elementSize, elementCount);
            TrackArrayOffsets(
                layout,
                nextSar,
                elementSize,
                elementCount,
                bitsetWordCount,
                pairCandidateDataOffset,
                pairCandidateECnt,
                span
            );

            if (
                elementSize == CharacterMdyRecordSchema.BlessingTsElementSize
                && bitsetWordCount == 2
                && elementCount == pairCandidateECnt
                && pairCandidateDataOffset >= 0
            )
            {
                if (layout.BlessingProtoDataOffset < 0)
                {
                    layout.BlessingProtoDataOffset = pairCandidateDataOffset;
                    layout.BlessingProtoElementCount = pairCandidateECnt;
                    layout.BlessingTsDataOffset = nextSar + CharacterMdyRecordSchema.SarHeaderSize;
                }
                else if (layout.CurseProtoDataOffset < 0)
                {
                    layout.CurseProtoDataOffset = pairCandidateDataOffset;
                    layout.CurseProtoElementCount = pairCandidateECnt;
                    layout.CurseTsDataOffset = nextSar + CharacterMdyRecordSchema.SarHeaderSize;
                }

                pairCandidateDataOffset = -1;
                pairCandidateECnt = 0;
            }
            else if (
                elementSize == 4
                && bitsetWordCount == 2
                && elementCount is >= 1 and <= CharacterMdyRecordSchema.MaxBlessingPairEntries
            )
            {
                FinalizeStandaloneSchematicsCandidate(span, layout, pairCandidateDataOffset, pairCandidateECnt);
                pairCandidateDataOffset = nextSar + CharacterMdyRecordSchema.SarHeaderSize;
                pairCandidateECnt = elementCount;
            }
            else if (pairCandidateDataOffset >= 0)
            {
                FinalizeStandaloneSchematicsCandidate(span, layout, pairCandidateDataOffset, pairCandidateECnt);
                pairCandidateDataOffset = -1;
                pairCandidateECnt = 0;
            }

            end = sarEnd;
            scanPos = sarEnd;
        }

        FinalizeStandaloneSchematicsCandidate(span, layout, pairCandidateDataOffset, pairCandidateECnt);

        if (layout.NameLengthOffset < 0)
        {
            var nameSearchEnd = Math.Min(span.Length, end + CharacterMdyRecordSchema.NameSearchLimit);
            for (var namePos = end; namePos + 5 <= nameSearchEnd; namePos++)
            {
                if (span[namePos] != 0x01)
                    continue;

                var nameLength = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(namePos + 1, 4));
                if (nameLength is < 1 or > CharacterMdyRecordSchema.MaxNameLength)
                    continue;
                if (namePos + 5 + nameLength > span.Length)
                    continue;

                var isAscii = true;
                for (var nameChar = 0; nameChar < nameLength && isAscii; nameChar++)
                    isAscii = CharacterMdyRecordSchema.IsPrintableAscii(span[namePos + 5 + nameChar]);

                if (!isAscii)
                    continue;

                layout.NameLengthOffset = namePos + 1;
                end = namePos + 1 + 4 + nameLength;
                break;
            }
        }

        consumed = end;
        return CharacterMdyRecordFactory.Create(
            span[..consumed].ToArray(),
            stats,
            basicSkills,
            techSkills,
            spellTech,
            hasCompleteData,
            layout
        );
    }

    private static void TrackScalarOffsets(
        CharacterMdyRecordLayout layout,
        int sarOffset,
        int bitsetId,
        int elementSize,
        int elementCount
    )
    {
        if (bitsetId == CharacterMdyRecordSchema.GoldAmountBsId && elementSize == 4 && elementCount == 1)
            layout.GoldDataOffset = sarOffset + CharacterMdyRecordSchema.SarHeaderSize;

        if (
            bitsetId == CharacterMdyRecordSchema.GameStatsBsId
            && elementSize == 4
            && elementCount >= CharacterMdyRecordSchema.GameStatsElementCount
            && elementCount <= CharacterMdyRecordSchema.GameStatsPowerCellsIndex + 1
        )
        {
            layout.TotalKillsDataOffset =
                sarOffset
                + CharacterMdyRecordSchema.SarHeaderSize
                + CharacterMdyRecordSchema.GameStatsTotalKillsIndex * 4;
            layout.ArrowsDataOffset =
                sarOffset + CharacterMdyRecordSchema.SarHeaderSize + CharacterMdyRecordSchema.GameStatsArrowsIndex * 4;
            if (elementCount > CharacterMdyRecordSchema.GameStatsBulletsIndex)
                layout.BulletsDataOffset =
                    sarOffset
                    + CharacterMdyRecordSchema.SarHeaderSize
                    + CharacterMdyRecordSchema.GameStatsBulletsIndex * 4;
            if (elementCount > CharacterMdyRecordSchema.GameStatsPowerCellsIndex)
                layout.PowerCellsDataOffset =
                    sarOffset
                    + CharacterMdyRecordSchema.SarHeaderSize
                    + CharacterMdyRecordSchema.GameStatsPowerCellsIndex * 4;
        }

        if (
            bitsetId == CharacterMdyRecordSchema.PortraitBsId
            && elementSize == 4
            && elementCount == CharacterMdyRecordSchema.PortraitElementCount
        )
        {
            layout.PortraitDataOffset =
                sarOffset + CharacterMdyRecordSchema.SarHeaderSize + CharacterMdyRecordSchema.PortraitIndexElement * 4;
        }

        if (bitsetId == CharacterMdyRecordSchema.EffectsBsId && elementSize == 4 && elementCount >= 1)
        {
            layout.EffectsDataOffset = sarOffset + CharacterMdyRecordSchema.SarHeaderSize;
            layout.EffectsElementCount = elementCount;
        }

        if (bitsetId == CharacterMdyRecordSchema.EffectCausesBsId && elementSize == 4 && elementCount >= 1)
        {
            layout.EffectCausesDataOffset = sarOffset + CharacterMdyRecordSchema.SarHeaderSize;
            layout.EffectCausesElementCount = elementCount;
        }
    }

    private static void TrackArrayOffsets(
        CharacterMdyRecordLayout layout,
        int sarOffset,
        int elementSize,
        int elementCount,
        int bitsetWordCount,
        int pairCandidateDataOffset,
        int pairCandidateElementCount,
        ReadOnlySpan<byte> span
    )
    {
        if (
            elementSize == CharacterMdyRecordSchema.QuestSarElementSize
            && bitsetWordCount == CharacterMdyRecordSchema.QuestSarBitsetWords
            && elementCount is >= 1 and <= CharacterMdyRecordSchema.MaxSarElementCount
        )
        {
            layout.QuestDataOffset = sarOffset + CharacterMdyRecordSchema.SarHeaderSize;
            layout.QuestCount = elementCount;
        }

        if (
            elementSize == 4
            && elementCount == CharacterMdyRecordSchema.ReputationSarElementCount
            && bitsetWordCount == CharacterMdyRecordSchema.ReputationSarBitsetWords
        )
        {
            layout.ReputationDataOffset = sarOffset + CharacterMdyRecordSchema.SarHeaderSize;
        }

        if (
            elementSize == CharacterMdyRecordSchema.RumorsSarElementSize
            && bitsetWordCount == CharacterMdyRecordSchema.RumorsSarBitsetWords
            && elementCount is >= 1 and <= CharacterMdyRecordSchema.MaxRumorEntries
        )
        {
            layout.RumorsDataOffset = sarOffset + CharacterMdyRecordSchema.SarHeaderSize;
            layout.RumorsCount = elementCount;
        }
    }

    private static void FinalizeStandaloneSchematicsCandidate(
        ReadOnlySpan<byte> span,
        CharacterMdyRecordLayout layout,
        int pairCandidateDataOffset,
        int pairCandidateElementCount
    )
    {
        if (pairCandidateDataOffset < 0 || layout.SchematicsDataOffset >= 0)
            return;

        var firstValue = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pairCandidateDataOffset, 4));
        if (firstValue > 1000)
        {
            layout.SchematicsDataOffset = pairCandidateDataOffset;
            layout.SchematicsElementCount = pairCandidateElementCount;
        }
    }

    private static int FindNextV2Magic(ReadOnlySpan<byte> data, int from)
    {
        for (var position = from; position + CharacterMdyRecordSchema.V2Magic.Length <= data.Length; position++)
        {
            if (
                data.Slice(position, CharacterMdyRecordSchema.V2Magic.Length)
                    .SequenceEqual(CharacterMdyRecordSchema.V2Magic)
            )
                return position;
        }

        return -1;
    }

    private static int FindSar(ReadOnlySpan<byte> data, int from, int limit, ReadOnlySpan<byte> signature)
    {
        var end = Math.Min(limit, from + CharacterMdyRecordSchema.MaxScanDistance);
        for (var position = from; position + CharacterMdyRecordSchema.SarHeaderSize <= end; position++)
        {
            if (data[position] != 0x01)
                continue;
            if (position + 1 + signature.Length > data.Length)
                break;
            if (data.Slice(position + 1, signature.Length).SequenceEqual(signature))
                return position;
        }

        return -1;
    }

    private static int FindAnySar(ReadOnlySpan<byte> data, int from, int limit)
    {
        for (var position = from; position + CharacterMdyRecordSchema.SarHeaderSize <= limit; position++)
        {
            if (data[position] != 0x01)
                continue;
            if (position + CharacterMdyRecordSchema.SarHeaderSize > data.Length)
                break;

            var elementSize = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(position + 1, 4));
            var elementCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(position + 5, 4));
            if (!CharacterMdyRecordSchema.IsLikelyGenericSar(elementSize, elementCount))
                continue;

            var dataLen = elementSize * elementCount;
            if (position + CharacterMdyRecordSchema.SarHeaderSize + dataLen + 4 > data.Length)
                continue;

            return position;
        }

        return -1;
    }

    private static int SarEnd(ReadOnlySpan<byte> data, int sarOff, int elementCount)
    {
        var bitsetCountOffset = sarOff + CharacterMdyRecordSchema.SarHeaderSize + elementCount * 4;
        if (bitsetCountOffset + 4 > data.Length)
            return bitsetCountOffset;

        var bitsetWordCount = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(bitsetCountOffset, 4));
        if (bitsetWordCount is < 0 or > CharacterMdyRecordSchema.MaxSarBitsetWords)
            bitsetWordCount = 0;

        return bitsetCountOffset + 4 + bitsetWordCount * 4;
    }
}
