using System.Buffers.Binary;

namespace ArcNET.Diagnostics;

public static class CharacterSarDiagnostics
{
    public static IReadOnlyList<CharacterSarEntrySnapshot> Parse(byte[] rawBytes, int startOffset = 12)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);

        List<CharacterSarEntrySnapshot> snapshots = [];
        foreach (var sar in Enumerate(rawBytes, startOffset))
        {
            snapshots.Add(
                new CharacterSarEntrySnapshot(
                    sar.Offset,
                    sar.TotalBytes,
                    sar.DataOffset,
                    sar.ElementSize,
                    sar.ElementCount,
                    sar.BitsetWordCount,
                    sar.BitsetId,
                    sar.BitSlots.Count,
                    sar.Fingerprint,
                    [.. sar.Values],
                    [.. sar.BitSlots],
                    sar.IsFiller
                )
            );
        }

        return snapshots;
    }

    public static IReadOnlyList<CharacterSarAuditSnapshot> CreateAuditSnapshots(byte[] rawBytes, int limit)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        List<CharacterSarAuditSnapshot> snapshots = [];
        foreach (var sar in Parse(rawBytes))
        {
            if (snapshots.Count >= limit)
                break;

            snapshots.Add(
                new CharacterSarAuditSnapshot(
                    sar.Offset,
                    sar.TotalBytes,
                    sar.ElementSize,
                    sar.ElementCount,
                    sar.BitsetWordCount,
                    sar.BitsetId,
                    sar.BitSlotCount,
                    sar.Fingerprint,
                    ResolveAnnotation(sar, fallback: "Unclassified SAR"),
                    [.. sar.Values.Take(8)],
                    [.. sar.BitSlots.Take(24)]
                )
            );
        }

        return snapshots;
    }

    public static IReadOnlyList<CharacterSarDumpEntrySnapshot> CreateDumpEntries(byte[] rawBytes)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);

        List<CharacterSarDumpEntrySnapshot> snapshots = [];
        foreach (var sar in Parse(rawBytes))
        {
            snapshots.Add(
                new CharacterSarDumpEntrySnapshot(
                    sar.BitsetId,
                    sar.ElementSize,
                    sar.ElementCount,
                    sar.BitsetWordCount,
                    ResolveAnnotation(sar, fallback: "UNKNOWN"),
                    FormatElements(rawBytes, sar.DataOffset, sar.ElementSize, sar.ElementCount),
                    sar.IsFiller
                )
            );
        }

        return snapshots;
    }

    public static IReadOnlyList<CharacterSarPairing> MatchGroups(
        IReadOnlyList<CharacterSarEntrySnapshot> entriesA,
        IReadOnlyList<CharacterSarEntrySnapshot> entriesB
    )
    {
        if (entriesA.Count == 0 || entriesB.Count == 0)
            return [];

        if (entriesA.Count == 1 && entriesB.Count == 1)
            return [new CharacterSarPairing(0, 0, ScoreSarSimilarity(entriesA[0], entriesB[0]))];

        List<CharacterSarPairing> candidates = [];
        for (var indexA = 0; indexA < entriesA.Count; indexA++)
        {
            for (var indexB = 0; indexB < entriesB.Count; indexB++)
                candidates.Add(
                    new CharacterSarPairing(indexA, indexB, ScoreSarSimilarity(entriesA[indexA], entriesB[indexB]))
                );
        }

        candidates.Sort(
            static (left, right) =>
            {
                var byScore = right.Score.CompareTo(left.Score);
                if (byScore != 0)
                    return byScore;

                var byDistance = Math.Abs(left.IndexA - left.IndexB).CompareTo(Math.Abs(right.IndexA - right.IndexB));
                if (byDistance != 0)
                    return byDistance;

                var byA = left.IndexA.CompareTo(right.IndexA);
                return byA != 0 ? byA : left.IndexB.CompareTo(right.IndexB);
            }
        );

        var minScore = entriesA[0].ElementSize == 4 ? 120 : 80;
        var usedA = new bool[entriesA.Count];
        var usedB = new bool[entriesB.Count];
        List<CharacterSarPairing> matches = [];

        foreach (var candidate in candidates)
        {
            if (candidate.Score < minScore)
                break;
            if (usedA[candidate.IndexA] || usedB[candidate.IndexB])
                continue;

            usedA[candidate.IndexA] = true;
            usedB[candidate.IndexB] = true;
            matches.Add(candidate);
        }

        return matches;
    }

    public static List<(int Idx, int VA, int VB)> CompareElements(
        CharacterSarEntrySnapshot entryA,
        CharacterSarEntrySnapshot entryB
    )
    {
        if (entryA.ElementSize != 4 || entryB.ElementSize != 4 || entryA.ElementCount != entryB.ElementCount)
            return [];

        var count = Math.Min(entryA.Values.Count, entryB.Values.Count);
        List<(int Idx, int VA, int VB)> result = [];
        for (var index = 0; index < count; index++)
        {
            if (entryA.Values[index] != entryB.Values[index])
                result.Add((index, entryA.Values[index], entryB.Values[index]));
        }

        return result;
    }

    public static (List<(int Idx, int VA, int VB)> Semantic, int PointerCount) PartitionElementDiffs(
        List<(int Idx, int VA, int VB)> diffs
    )
    {
        List<(int Idx, int VA, int VB)> semantic = [];
        var pointerCount = 0;
        foreach (var diff in diffs)
        {
            if (IsPointerLike(diff.VA) && IsPointerLike(diff.VB))
                pointerCount++;
            else
                semantic.Add(diff);
        }

        return (semantic, pointerCount);
    }

    public static List<(int Slot, int Value)> DecodeReputation(CharacterSarEntrySnapshot sar, int[] reputationRaw)
    {
        List<(int Slot, int Value)> result = [];
        if (sar.ElementSize != 4 || sar.ElementCount != reputationRaw.Length)
            return result;

        var canUseBitSlots = sar.BitSlots.Count == reputationRaw.Length;
        for (var index = 0; index < reputationRaw.Length; index++)
            result.Add((canUseBitSlots ? sar.BitSlots[index] : index, reputationRaw[index]));

        return result;
    }

    public static string AnnotateFingerprint(string fingerprint) =>
        fingerprint switch
        {
            "4:28:2" => "Stats INT32[28]",
            "4:12:2" => "BasicSkills INT32[12]",
            "4:4:2" => "TechSkills/HP/FatigueDmg/Schematics INT32[4]",
            "4:25:2" => "SpellTech INT32[25]",
            "4:7:2" => "Blessingsx7 or NPC-dispatch INT32[7]",
            "4:1:2" => "Gold/single-int32",
            "4:3:2" => "Portrait/MaxFollowers INT32[3]",
            "4:2:2" => "Conditions/Curse INT32[2]",
            "4:10:2" => "PcPartyStatus? INT32[10] (transient)",
            "4:11:2" => "GameStats INT32[11] (alt bsCnt)",
            "4:11:3" => "GameStats INT32[11]",
            "4:13:3" => "GameStats INT32[13] tech",
            "4:5:2" => "Effects/Blessing INT32[5]",
            "8:1:2" => "GoldHandle 8Bx1",
            "8:2:2" => "CurseTsData 8Bx2",
            "8:3:2" => "Handles 8Bx3",
            "8:7:2" => "BlessingTsData 8Bx7",
            "8:11:38" => "CritterHandles 8Bx11",
            "24:1:2" => "CritterFollower 24Bx1",
            "24:2:2" => "CritterFollowers 24Bx2",
            "24:3:2" => "CritterFollowers 24Bx3",
            "4:19:3" => "Reputation INT32[19]",
            _ when fingerprint.StartsWith("4:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":5", StringComparison.Ordinal) => "Conditions/PermanentMods INT32[N]",
            _ when fingerprint.StartsWith("4:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":4", StringComparison.Ordinal) =>
                "Conditions/PermanentMods INT32[N] (bsCnt4)",
            _ when fingerprint.StartsWith("4:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":3", StringComparison.Ordinal)
                    && !fingerprint.Equals("4:19:3", StringComparison.Ordinal)
                    && !fingerprint.Equals("4:11:3", StringComparison.Ordinal)
                    && !fingerprint.Equals("4:13:3", StringComparison.Ordinal) =>
                "Conditions/PermanentMods INT32[N] (bsCnt3)",
            _ when fingerprint.StartsWith("4:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":2", StringComparison.Ordinal) =>
                "Blessing/Curse/Schematics candidate INT32[n]",
            _ when fingerprint.StartsWith("8:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":2", StringComparison.Ordinal) => "BlessingTs/CurseTs 8BxN",
            _ when fingerprint.StartsWith("8:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":39", StringComparison.Ordinal) => "Rumors 8BxN",
            _ when fingerprint.StartsWith("16:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":37", StringComparison.Ordinal) => "Quest eSize=16",
            _ when fingerprint.StartsWith("16:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":38", StringComparison.Ordinal) => "Quest-alt eSize=16",
            _ when fingerprint.StartsWith("24:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":3", StringComparison.Ordinal) => "CritterHandles 24BxN",
            _ when fingerprint.StartsWith("24:", StringComparison.Ordinal)
                    && fingerprint.EndsWith(":2", StringComparison.Ordinal) => "CritterHandles 24BxN",
            _ => string.Empty,
        };

    public static string AnnotateBsId(int bitsetId) =>
        bitsetId switch
        {
            0x4046 => "HP-damage INT32[4]",
            0x4047 => "HP-adjacent INT32[7]",
            0x423D => "Fatigue-adjacent INT32[4]",
            0x423E => "Fatigue INT32[4]",
            0x4050 => "pre-stat INT32[7]",
            0x4299 => "Stats INT32[28]",
            0x43C3 => "BasicSkills INT32[12]",
            0x49FC => "Effects INT32[n]",
            0x49FD => "EffectCauses INT32[n]",
            0x49FE => "CritterInventory handles 24BxN",
            0x49FF => "CritterFollower handle 24Bx1",
            0x4A00 => "QuestState? sz16xn",
            0x4A07 => "TechSkills INT32[4]",
            0x4A08 => "SpellTech INT32[25]",
            0x4B13 => "Gold INT32[1]",
            0x4D68 => "GameStats INT32[11..13]",
            0x4D69 => "pre-stat INT32[4]",
            0x4D6A => "pre-stat INT32[4]",
            0x4D6F => "critter-handles 8Bx11",
            0x4D77 => "GoldHandle 24Bx1",
            0x4DA2 => "SpellTech-adjacent INT32[25]",
            0x4DA3 => "PositionAI INT32[3]",
            0x4DA4 => "Portrait/MaxFollowers INT32[3]",
            _ => string.Empty,
        };

    public static string AnnotateSarValue(CharacterSarEntrySnapshot sar)
    {
        if (sar.ElementSize == 4 && sar.BitsetWordCount == 2 && sar.Values.Count >= 2)
        {
            if (sar.Values.Any(static value => value != -1 && IsPointerLike(value)))
                return $"NPC-dispatch ptrs INT32[{sar.ElementCount}]";

            var nonMinusOne = sar.Values.Where(static value => value != -1).ToArray();
            if (nonMinusOne.Length > 0 && nonMinusOne.All(static value => value > 500))
                return $"ProtoIdArray INT32[{sar.ElementCount}] (bless/schematics)";

            if (sar.ElementCount == 2)
            {
                var first = sar.Values[0];
                var second = sar.Values[1];
                if (first is >= 30 and <= 500 && second is >= 30 and <= 500)
                    return "CondProto/CurseProto INT32[2]";
                if (first is >= 0 and <= 10 || second is >= 0 and <= 10)
                    return "CondFlag INT32[2]";
            }
        }

        return AnnotateFingerprint(sar.Fingerprint);
    }

    public static string GetElementLabel(string fingerprint, int index) =>
        fingerprint switch
        {
            "4:28:2" => index switch
            {
                0 => "STR",
                1 => "DEX",
                2 => "CON",
                3 => "BEA",
                4 => "INT",
                5 => "PER",
                6 => "WIL",
                7 => "CHA",
                8 => "CarryWt",
                9 => "DmgBonus",
                10 => "AcAdj",
                11 => "Speed",
                12 => "HealRate",
                13 => "PoisRec",
                14 => "ReactMod",
                15 => "MaxFoll",
                16 => "MTApt",
                17 => "lv",
                18 => "XP",
                19 => "align",
                20 => "fate",
                21 => "unspent",
                22 => "magicPts",
                23 => "techPts",
                24 => "poisonLvl",
                25 => "age",
                26 => "gender",
                27 => "race",
                _ => index.ToString(),
            },
            "4:12:2" => index switch
            {
                0 => "BOW",
                1 => "DODGE",
                2 => "MELEE",
                3 => "THROW",
                4 => "BKSTB",
                5 => "PPKT",
                6 => "PROWL",
                7 => "STRAP",
                8 => "GAMBL",
                9 => "HAGGL",
                10 => "HEAL",
                11 => "PERS",
                _ => index.ToString(),
            },
            "4:4:2" => index switch
            {
                0 => "REPR",
                1 => "FRMS",
                2 => "PKLCK",
                3 => "DTRAP",
                _ => index.ToString(),
            },
            "4:25:2" => index switch
            {
                0 => "Conv",
                1 => "Div",
                2 => "Air",
                3 => "Erth",
                4 => "Fire",
                5 => "Watr",
                6 => "Forc",
                7 => "Ment",
                8 => "Meta",
                9 => "Mrph",
                10 => "Natr",
                11 => "NBlk",
                12 => "NWht",
                13 => "Phan",
                14 => "Summ",
                15 => "Temp",
                16 => "MAST",
                17 => "Herb",
                18 => "Chem",
                19 => "Elec",
                20 => "Xpls",
                21 => "Gun",
                22 => "Mech",
                23 => "Smth",
                24 => "Thrp",
                _ => index.ToString(),
            },
            _ => index.ToString(),
        };

    public static bool IsPointerLike(int value) => Math.Abs((long)value) > 200_000_000L;

    public static string FormatSlotList(IReadOnlyList<int> slots, int maxShow = int.MaxValue)
    {
        if (slots.Count == 0)
            return "[]";

        var showCount = Math.Min(slots.Count, maxShow);
        var body = string.Join(",", slots.Take(showCount));
        return slots.Count > showCount ? $"[{body}, more]" : $"[{body}]";
    }

    public static string FormatInt32List(IReadOnlyList<int> values, int maxShow = int.MaxValue)
    {
        if (values.Count == 0)
            return "[]";

        var showCount = Math.Min(values.Count, maxShow);
        var body = string.Join(",", values.Take(showCount));
        return values.Count > showCount ? $"[{body},...]" : $"[{body}]";
    }

    public static string FormatInt32Preview(IReadOnlyList<int> values, int maxShow)
    {
        if (values.Count == 0)
            return "[]";

        var showCount = Math.Min(values.Count, maxShow);
        var body = string.Join(",", values.Take(showCount));
        return values.Count > showCount ? $"[{body},...]" : $"[{body}]";
    }

    public static string FormatElements(
        byte[] rawBytes,
        int dataOffset,
        int elementSize,
        int elementCount,
        int maxShow = 32
    )
    {
        var showCount = Math.Min(elementCount, maxShow);
        if (elementSize == 4)
        {
            List<int> values = [];
            for (var index = 0; index < showCount && dataOffset + index * 4 + 4 <= rawBytes.Length; index++)
                values.Add(BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(dataOffset + index * 4, 4)));

            var body = string.Join(",", values);
            return elementCount > values.Count ? $"[{body},...]" : $"[{body}]";
        }

        var availableBytes = Math.Max(0, Math.Min(rawBytes.Length - dataOffset, elementSize * showCount));
        var bytes = availableBytes == 0 ? ReadOnlySpan<byte>.Empty : rawBytes.AsSpan(dataOffset, availableBytes);
        var suffix = elementCount > showCount ? "..." : string.Empty;
        return $"0x{Convert.ToHexString(bytes)}{suffix}";
    }

    private static IEnumerable<ParsedSar> Enumerate(byte[] rawBytes, int startOffset)
    {
        var position = startOffset;
        while (position + 13 <= rawBytes.Length)
        {
            if (rawBytes[position] != 0x01)
            {
                position++;
                continue;
            }

            var elementSize = BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(position + 1, 4));
            if (elementSize is not (1 or 2 or 4 or 8 or 16 or 24))
            {
                position++;
                continue;
            }

            var elementCount = BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(position + 5, 4));
            if (elementCount is < 1 or > 8192)
            {
                position++;
                continue;
            }

            var dataLength = (long)elementSize * elementCount;
            if (dataLength > 131072 || position + 13 + dataLength + 4 > rawBytes.Length)
            {
                position++;
                continue;
            }

            var bitsetId = BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(position + 9, 4));
            var bitsetCountOffset = (int)(position + 13 + dataLength);
            var bitsetWordCount = BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(bitsetCountOffset, 4));
            if (bitsetWordCount is < 0 or > 4096)
            {
                position++;
                continue;
            }

            var sarEnd = bitsetCountOffset + 4 + bitsetWordCount * 4;
            if (sarEnd > rawBytes.Length)
                break;

            List<int> values = [];
            var sampleCount = elementSize == 4 ? Math.Min(elementCount, 512) : 2;
            if (elementSize == 4)
            {
                for (var index = 0; index < sampleCount; index++)
                    values.Add(BinaryPrimitives.ReadInt32LittleEndian(rawBytes.AsSpan(position + 13 + index * 4, 4)));
            }

            List<int> bitSlots = [];
            for (var wordIndex = 0; wordIndex < bitsetWordCount; wordIndex++)
            {
                var word = BinaryPrimitives.ReadUInt32LittleEndian(
                    rawBytes.AsSpan(bitsetCountOffset + 4 + wordIndex * 4, 4)
                );
                for (var bitIndex = 0; bitIndex < 32; bitIndex++)
                {
                    if ((word & (1u << bitIndex)) != 0)
                        bitSlots.Add(wordIndex * 32 + bitIndex);
                }
            }

            var isFiller =
                elementSize == 4 && elementCount == 4 && values.Count == 4 && values.All(static value => value == -1);
            yield return new ParsedSar(
                position,
                sarEnd - position,
                position + 13,
                elementSize,
                elementCount,
                bitsetWordCount,
                bitsetId,
                $"{elementSize}:{elementCount}:{bitsetWordCount}",
                values,
                bitSlots,
                isFiller
            );

            position = sarEnd;
        }
    }

    private static string ResolveAnnotation(CharacterSarEntrySnapshot sar, string fallback)
    {
        var byBitsetId = AnnotateBsId(sar.BitsetId);
        if (!string.IsNullOrEmpty(byBitsetId))
            return byBitsetId;

        var byValue = AnnotateSarValue(sar);
        return string.IsNullOrEmpty(byValue) ? fallback : byValue;
    }

    private static int ScoreSarSimilarity(CharacterSarEntrySnapshot entryA, CharacterSarEntrySnapshot entryB)
    {
        if (entryA.Fingerprint != entryB.Fingerprint)
            return int.MinValue;

        var score = 0;
        score += ScoreBitSlots(entryA.BitSlots, entryB.BitSlots);

        if (entryA.ElementSize == 4 && entryB.ElementSize == 4)
            score += ScoreInt32Values(entryA.Values, entryB.Values);
        else if (entryA.TotalBytes == entryB.TotalBytes)
            score += 20;

        return score;
    }

    private static int ScoreBitSlots(IReadOnlyList<int> slotsA, IReadOnlyList<int> slotsB)
    {
        if (slotsA.Count == 0 && slotsB.Count == 0)
            return 0;

        var indexA = 0;
        var indexB = 0;
        var overlap = 0;
        while (indexA < slotsA.Count && indexB < slotsB.Count)
        {
            if (slotsA[indexA] == slotsB[indexB])
            {
                overlap++;
                indexA++;
                indexB++;
            }
            else if (slotsA[indexA] < slotsB[indexB])
            {
                indexA++;
            }
            else
            {
                indexB++;
            }
        }

        var union = slotsA.Count + slotsB.Count - overlap;
        if (union == 0)
            return 0;
        if (overlap == union)
            return 200;

        return overlap * 60 - (union - overlap) * 25;
    }

    private static int ScoreInt32Values(IReadOnlyList<int> valuesA, IReadOnlyList<int> valuesB)
    {
        var count = Math.Min(valuesA.Count, valuesB.Count);
        if (count == 0)
            return 0;

        var exact = 0;
        var near = 0;
        long penalty = 0;
        for (var index = 0; index < count; index++)
        {
            var diff = Math.Abs((long)valuesA[index] - valuesB[index]);
            if (diff == 0)
                exact++;
            else if (diff <= 3)
                near++;

            penalty += Math.Min(diff, 2500);
        }

        var score = exact * 80 + near * 20;
        score -= (int)Math.Min(penalty / Math.Max(1, count * 4), 250);

        var aHighProto = LooksLikeHighProtoArray(valuesA);
        var bHighProto = LooksLikeHighProtoArray(valuesB);
        if (aHighProto == bHighProto)
            score += 60;
        else
            score -= 100;

        var aMostlyNegative = LooksMostlyNegative(valuesA);
        var bMostlyNegative = LooksMostlyNegative(valuesB);
        if (aMostlyNegative == bMostlyNegative)
            score += 40;
        else
            score -= 60;

        return score;
    }

    private static bool LooksLikeHighProtoArray(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
            return false;

        var inspected = Math.Min(values.Count, 8);
        var high = 0;
        for (var index = 0; index < inspected; index++)
        {
            if (values[index] > 1000)
                high++;
        }

        return high >= Math.Max(2, inspected / 2);
    }

    private static bool LooksMostlyNegative(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
            return false;

        var inspected = Math.Min(values.Count, 8);
        var negative = 0;
        for (var index = 0; index < inspected; index++)
        {
            if (values[index] < 0)
                negative++;
        }

        return negative >= Math.Max(2, inspected / 2);
    }

    private sealed record ParsedSar(
        int Offset,
        int TotalBytes,
        int DataOffset,
        int ElementSize,
        int ElementCount,
        int BitsetWordCount,
        int BitsetId,
        string Fingerprint,
        IReadOnlyList<int> Values,
        IReadOnlyList<int> BitSlots,
        bool IsFiller
    );
}
