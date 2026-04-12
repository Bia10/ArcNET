using System.Buffers.Binary;
using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Editor;
using ArcNET.Formats;
using Bia.ValueBuffers;

namespace Probe;

// Shared SAR (Sparse Array Record) utilities used across Probe analysis modes.

internal record SarEntry(
    int ESize,
    int ECnt,
    int BCnt,
    int BsId,
    int[] FirstVals,
    int[] BitSlots,
    bool IsFiller,
    int Offset,
    int TotalBytes
)
{
    public string Fingerprint => $"{ESize}:{ECnt}:{BCnt}";

    public string ValueSummary =>
        ESize == 4
            ? SarUtils.FormatInt32List(FirstVals.AsSpan(0, Math.Min(FirstVals.Length, 4)), ECnt > 4)
            : "(non-int32)";
}

internal record SlotSnapshot(
    int Slot,
    int Level,
    int RawBytesLen,
    List<SarEntry> Sars,
    string SaveName,
    CharacterMdyRecord? Character = null
);

internal readonly record struct SarPairing(int IndexA, int IndexB, int Score);

internal sealed record QuestTextLookup(IReadOnlyDictionary<int, string> Labels, string Source);

internal static class StringExtensions
{
    /// <summary>Shortens a long annotation to at most 12 characters for compact diff output.</summary>
    internal static string TruncateAnnotation(this string s) => ValueBufferText.TruncateText(s, 12);
}

internal readonly struct QuestRefFormatter : IValueStringBuilderFormatter<int>
{
    private readonly QuestTextLookup? _lookup;
    private readonly int _maxLabelLen;

    public QuestRefFormatter(QuestTextLookup? lookup, int maxLabelLen)
    {
        _lookup = lookup;
        _maxLabelLen = maxLabelLen;
    }

    public void Append(ref ValueStringBuilder builder, int value)
    {
        builder.Append('q');
        builder.Append(value);

        var label = SarUtils.ResolveQuestLabel(_lookup, value);
        if (label is null)
            return;

        builder.Append('[');
        builder.Append(ValueBufferText.TruncateText(label, _maxLabelLen));
        builder.Append(']');
    }
}

internal static class SarUtils
{
    /// <summary>
    /// Attempts to load quest labels from a local <c>quests.mes</c> source.
    /// Search order:
    /// 1. Loose overrides under the game directory's <c>data/</c> or <c>modules/</c> trees.
    /// 2. Packed DAT archives in the game root and <c>modules/</c>.
    /// </summary>
    public static QuestTextLookup? TryLoadQuestLookup(string saveDir)
    {
        var gameDir = Path.GetFullPath(Path.Combine(saveDir, "..", "..", ".."));

        foreach (var path in EnumerateLooseQuestFiles(gameDir))
        {
            try
            {
                var mes = MessageFormat.ParseFile(path);
                var lookup = BuildQuestLookup(mes, path);
                if (lookup is not null)
                    return lookup;
            }
            catch
            {
                // Keep probing other candidates.
            }
        }

        foreach (var datPath in EnumerateDatArchives(gameDir))
        {
            try
            {
                using var archive = DatArchive.Open(datPath);
                var entry = archive.Entries.FirstOrDefault(e => IsQuestLookupCandidate(Path.GetFileName(e.Path)));
                if (entry is null)
                    continue;

                var mes = MessageFormat.ParseMemory(archive.ReadEntry(entry));
                var lookup = BuildQuestLookup(mes, $"{Path.GetFileName(datPath)}:{entry.Path.Replace('\\', '/')}");
                if (lookup is not null)
                    return lookup;
            }
            catch
            {
                // Keep probing other archives.
            }
        }

        return null;
    }

    /// <summary>Parse all SAR entries from a v2 record's raw bytes.</summary>
    public static List<SarEntry> ParseSars(byte[] raw, int startOffset = 12)
    {
        var result = new List<SarEntry>();
        int pos = startOffset;
        while (pos + 13 <= raw.Length)
        {
            if (raw[pos] != 0x01)
            {
                pos++;
                continue;
            }
            int eSize = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(pos + 1, 4));
            if (eSize is not (1 or 2 or 4 or 8 or 16 or 24))
            {
                pos++;
                continue;
            }
            int eCnt = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(pos + 5, 4));
            if (eCnt is < 1 or > 8192)
            {
                pos++;
                continue;
            }
            long dataLen = (long)eSize * eCnt;
            if (pos + 13 + dataLen + 4 > raw.Length || dataLen > 131072)
            {
                pos++;
                continue;
            }
            int bsId = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(pos + 9, 4));
            int bcOff = (int)(pos + 13 + dataLen);
            int bcCnt = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(bcOff, 4));
            if (bcCnt is < 0 or > 4096)
            {
                pos++;
                continue;
            }
            int sarEnd = bcOff + 4 + bcCnt * 4;
            if (sarEnd > raw.Length)
                break;

            int sampleCount = eSize == 4 ? Math.Min(eCnt, 512) : 2;
            var firstVals = new int[sampleCount];
            if (eSize == 4)
                for (int i = 0; i < sampleCount; i++)
                    firstVals[i] = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(pos + 13 + i * 4, 4));

            var bitSlots = new List<int>();
            for (int bw = 0; bw < bcCnt; bw++)
            {
                uint word = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(bcOff + 4 + bw * 4, 4));
                for (int bb = 0; bb < 32; bb++)
                    if ((word & (1u << bb)) != 0)
                        bitSlots.Add(bw * 32 + bb);
            }

            bool isFiller = eSize == 4 && eCnt == 4 && firstVals.Length == 4 && firstVals.All(v => v == -1);
            result.Add(
                new SarEntry(eSize, eCnt, bcCnt, bsId, firstVals, bitSlots.ToArray(), isFiller, pos, sarEnd - pos)
            );
            pos = sarEnd;
        }
        return result;
    }

    /// <summary>Format a slot/bit index list for compact console output.</summary>
    public static string FormatSlotList(IReadOnlyList<int> slots, int maxShow = 12)
    {
        if (slots.Count == 0)
            return "[]";

        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.Append('[');
        if (slots.Count <= maxShow)
            sb.AppendJoin(',', slots);
        else
            sb.AppendJoin(',', slots, maxShow, " more".AsSpan());
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Formats an INT32 list for compact console output.</summary>
    public static string FormatInt32List(ReadOnlySpan<int> values, bool truncated = false)
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.Append('[');
        AppendInt32Values(ref sb, values);
        if (truncated)
        {
            if (!values.IsEmpty)
                sb.Append(",...");
            else
                sb.Append("...");
        }

        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Formats an INT32 list for compact console output.</summary>
    public static string FormatInt32List(IReadOnlyList<int> values, int maxShow = int.MaxValue)
    {
        if (values.Count == 0)
            return "[]";

        int showCount = Math.Min(values.Count, maxShow);
        if (values is int[] array)
            return FormatInt32List(array.AsSpan(0, showCount), values.Count > showCount);

        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.Append('[');
        if (showCount == values.Count)
            sb.AppendJoin(',', values);
        else
            AppendInt32Values(ref sb, values, showCount);
        if (values.Count > showCount)
        {
            if (showCount > 0)
                sb.Append(",...");
            else
                sb.Append("...");
        }

        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Formats an INT32 sequence for compact console output.</summary>
    public static string FormatInt32List(IEnumerable<int> values)
    {
        if (values is IReadOnlyList<int> list)
            return FormatInt32List(list);

        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.Append('[');
        sb.AppendJoin(',', values);
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Formats a slot/bit index sequence for compact console output.</summary>
    public static string FormatSlotList(IEnumerable<int> slots, int maxShow)
    {
        if (slots is IReadOnlyList<int> list)
            return FormatSlotList(list, maxShow);

        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.Append('[');
        sb.AppendJoin(',', slots, maxShow, " more".AsSpan());
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Returns the resolved quest label for <paramref name="protoId"/>, or <see langword="null"/> when unavailable.</summary>
    public static string? ResolveQuestLabel(QuestTextLookup? lookup, int protoId) =>
        lookup is not null && lookup.Labels.TryGetValue(protoId, out var label) ? label : null;

    /// <summary>Formats a quest proto ID with a truncated label when a quest lookup is available.</summary>
    public static string FormatQuestRef(int protoId, QuestTextLookup? lookup, int maxLabelLen = 24)
    {
        Span<char> buf = stackalloc char[64];
        var sb = new ValueStringBuilder(buf);
        sb.Append('q');
        sb.Append(protoId);

        var label = ResolveQuestLabel(lookup, protoId);
        if (label is not null)
        {
            sb.Append('[');
            sb.Append(ValueBufferText.TruncateText(label, maxLabelLen));
            sb.Append(']');
        }

        return sb.ToString();
    }

    /// <summary>Formats a sequence of quest proto IDs with optional labels.</summary>
    public static string FormatQuestRefs(IEnumerable<int> protoIds, QuestTextLookup? lookup, int maxLabelLen = 24)
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.AppendEnclosedJoin("[", ", ", "]", protoIds, new QuestRefFormatter(lookup, maxLabelLen));
        return sb.ToString();
    }

    /// <summary>
    /// Format the observed quest-state bitmask for display.
    /// Low bits are structurally understood; bit 8 appears in late-game saves but is still unlabeled.
    /// </summary>
    public static string FormatQuestState(int state)
    {
        if (state == 0)
            return "known";
        return state switch
        {
            1 => "active",
            2 => "completed(primary)",
            4 => "completed(secondary)",
            _ => FormatQuestStateBits(state),
        };
    }

    private static string FormatQuestStateBits(int state)
    {
        Span<char> buf = stackalloc char[128];
        var sb = new ValueStringBuilder(buf);
        if ((state & 0x001) != 0)
        {
            sb.Append("active");
            sb.Append('|');
        }
        if ((state & 0x002) != 0)
        {
            sb.Append("completed(primary)");
            sb.Append('|');
        }
        if ((state & 0x004) != 0)
        {
            sb.Append("completed(secondary)");
            sb.Append('|');
        }
        if ((state & 0x100) != 0)
        {
            sb.Append("bit8?");
            sb.Append('|');
        }

        int unknownMask = state & ~0x107;
        if (unknownMask != 0)
        {
            sb.AppendHex((uint)unknownMask, "0x".AsSpan(), 1);
            sb.Append('|');
        }

        if (sb.Length == 0)
        {
            sb.AppendHex((uint)state, "0x".AsSpan(), 3);
            return sb.ToString();
        }

        sb.TrimEnd('|');
        sb.Append(" [");
        sb.AppendHex((uint)state, "0x".AsSpan(), 3);
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>Format SAR element values for display.</summary>
    public static string FormatElements(byte[] raw, int dataOff, int eSize, int eCnt, int maxShow = 32)
    {
        int showCnt = Math.Min(eCnt, maxShow);
        if (eSize == 4)
        {
            int availableCount = 0;
            if (dataOff < raw.Length)
                availableCount = Math.Min(showCnt, (raw.Length - dataOff) / 4);

            Span<int> values = stackalloc int[availableCount];
            for (var index = 0; index < availableCount; index++)
                values[index] = BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan(dataOff + index * 4, 4));

            return FormatInt32List(values, eCnt > availableCount);
        }
        else if (eSize == 1)
        {
            return ValueBufferText.FormatHex(
                raw.AsSpan(dataOff, Math.Min(eSize * showCnt, raw.Length - dataOff)),
                includePrefix: true
            );
        }
        else
        {
            int showBytes = Math.Min(eSize * showCnt, raw.Length - dataOff);
            return ValueBufferText.FormatHex(
                raw.AsSpan(dataOff, showBytes),
                includePrefix: true,
                suffix: eCnt > showCnt ? "..." : null
            );
        }
    }

    /// <summary>
    /// Find the player's v2 character record in a save.
    /// Priority:
    /// 1. Record with quest data whose level matches <see cref="ArcNET.Editor.LoadedSave.Info"/> leader level.
    /// 2. Record with quest data (any level).
    /// 3. Record with reputation data whose level matches leader level.
    /// 4. Record with a non-empty Name whose level matches leader level.
    /// 5. Record with a non-empty Name.
    /// 6. Record whose level matches leader level (by raw bytes length, largest first).
    /// 7. Largest record overall.
    /// </summary>
    public static CharacterMdyRecord? FindPlayerRecord(ArcNET.Editor.LoadedSave save)
    {
        int leaderLevel = save.Info.LeaderLevel;
        var candidates = new List<CharacterMdyRecord>();
        foreach (var (_, mdyFile) in save.MobileMdys)
        foreach (var rec in mdyFile.Records.Where(r => r.IsCharacter))
            candidates.Add(rec.Character!);

        if (candidates.Count == 0)
            return null;

        // Helper: level matches SaveInfo
        bool LvMatch(CharacterMdyRecord ch) => ch.Stats.Length > 17 && ch.Stats[17] == leaderLevel;

        // 1. Quest data + level match
        var best = candidates.FirstOrDefault(c => c.QuestCount > 0 && LvMatch(c));
        if (best is not null)
            return best;

        // 2. Quest data (any level) — prefer level match as tiebreaker
        var questCandidates = candidates.Where(c => c.QuestCount > 0).ToList();
        if (questCandidates.Count > 0)
        {
            best = questCandidates.FirstOrDefault(LvMatch) ?? questCandidates.MaxBy(c => c.QuestCount);
            return best!;
        }

        // 3. Reputation + level match
        best = candidates.FirstOrDefault(c => c.ReputationRaw is not null && LvMatch(c));
        if (best is not null)
            return best;

        // 4. Named + level match
        best = candidates.FirstOrDefault(c => c.Name is { Length: > 0 } && LvMatch(c));
        if (best is not null)
            return best;

        // 5. Named (any level)
        best = candidates.FirstOrDefault(c => c.Name is { Length: > 0 });
        if (best is not null)
            return best;

        // 6. Level match, largest
        var levelMatched = candidates.Where(LvMatch).MaxBy(c => c.RawBytes.Length);
        if (levelMatched is not null)
            return levelMatched;

        // 7. Largest
        return candidates.MaxBy(c => c.RawBytes.Length);
    }

    /// <summary>Map a SAR structural fingerprint ("eSize:eCnt:bsCnt") to a known field name.</summary>
    public static string AnnotateFingerprint(string fp) =>
        fp switch
        {
            "4:28:2" => "Stats INT32[28]",
            "4:12:2" => "BasicSkills INT32[12]",
            "4:4:2" => "TechSkills/HP/FatigueDmg/Schematics INT32[4]",
            "4:25:2" => "SpellTech INT32[25]",
            // 4:7:2: value-dependent — Blessings (all vals >1000) vs NPC dispatch table (mostly -1)
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
            // Blessing/Curse pair detection (session 12 RE findings):
            //   4:N:2 followed immediately by 8:N:2 (same N) = PcBlessingIdx/PcCurseIdx + timestamps
            //   Standalone 4:N:2 in post-stat region with values > 1000 = PcSchematicsFoundIdx
            // Conditions / PermanentMods (bits 41/42 critter-shared):
            //   4:N:5 = Conditions or PermanentMods (bsCnt=5 → up to 160 bitset slots; eCnt grows with active effects)
            //   4:N:4 = same fields, mid-range eCnt (bsCnt=4 → up to 128 bitset slots)
            //   4:N:3 = same fields at lower eCnt (bsCnt=3 → up to 96 bitset slots)
            _ when fp.StartsWith("4:") && fp.EndsWith(":5") => "Conditions/PermanentMods INT32[N]",
            _ when fp.StartsWith("4:") && fp.EndsWith(":4") => "Conditions/PermanentMods INT32[N] (bsCnt4)",
            _ when fp.StartsWith("4:")
                    && fp.EndsWith(":3")
                    && !fp.Equals("4:19:3")
                    && !fp.Equals("4:11:3")
                    && !fp.Equals("4:13:3") => "Conditions/PermanentMods INT32[N] (bsCnt3)",
            _ when fp.StartsWith("4:") && fp.EndsWith(":2") => "Blessing/Curse/Schematics candidate INT32[n]",
            _ when fp.StartsWith("8:") && fp.EndsWith(":2") => "BlessingTs/CurseTs 8BxN",
            _ when fp.StartsWith("8:") && fp.EndsWith(":39") => "Rumors 8BxN",
            _ when fp.StartsWith("16:") && fp.EndsWith(":37") => "Quest eSize=16",
            _ when fp.StartsWith("16:") && fp.EndsWith(":38") => "Quest-alt eSize=16",
            _ when fp.StartsWith("24:") && fp.EndsWith(":3") => "CritterHandles 24BxN",
            _ when fp.StartsWith("24:") && fp.EndsWith(":2") => "CritterHandles 24BxN",
            _ => "",
        };

    /// <summary>
    /// Decode a reputation SAR's bitset-slot indices into faction IDs and their values.
    /// The bitset encodes which slots (faction proto-ID offsets) are populated.
    /// Returns a list of (slotIndex, value) for each non-zero reputation entry.
    /// </summary>
    public static List<(int Slot, int Value)> DecodeReputation(SarEntry sar, int[] repRaw)
    {
        var result = new List<(int, int)>();
        if (sar.ESize != 4 || sar.ECnt != repRaw.Length)
            return result;

        bool canUseBitSlots = sar.BitSlots.Length == repRaw.Length;
        for (int i = 0; i < repRaw.Length; i++)
            result.Add((canUseBitSlots ? sar.BitSlots[i] : i, repRaw[i]));
        return result;
    }

    /// <summary>Compare INT32 element values between two SARs with the same eCnt.
    /// Returns a list of (index, valueA, valueB) for each element that differs.</summary>
    public static List<(int Idx, int VA, int VB)> CompareElements(SarEntry a, SarEntry b)
    {
        if (a.ESize != 4 || b.ESize != 4 || a.ECnt != b.ECnt)
            return [];
        int n = Math.Min(a.FirstVals.Length, b.FirstVals.Length);
        var result = new List<(int, int, int)>();
        for (int k = 0; k < n; k++)
            if (a.FirstVals[k] != b.FirstVals[k])
                result.Add((k, a.FirstVals[k], b.FirstVals[k]));
        return result;
    }

    /// <summary>
    /// Pair duplicate SARs with the same fingerprint by bitset/value similarity instead of raw occurrence order.
    /// Low-similarity candidates are left unmatched so mode 9 can report NEW/GONE instead of a misleading CHG.
    /// </summary>
    public static List<SarPairing> MatchSarGroups(IReadOnlyList<SarEntry> a, IReadOnlyList<SarEntry> b)
    {
        if (a.Count == 0 || b.Count == 0)
            return [];

        if (a.Count == 1 && b.Count == 1)
            return [new SarPairing(0, 0, ScoreSarSimilarity(a[0], b[0]))];

        var candidates = new List<SarPairing>(a.Count * b.Count);
        for (int ia = 0; ia < a.Count; ia++)
        for (int ib = 0; ib < b.Count; ib++)
            candidates.Add(new SarPairing(ia, ib, ScoreSarSimilarity(a[ia], b[ib])));

        candidates.Sort(
            (x, y) =>
            {
                int byScore = y.Score.CompareTo(x.Score);
                if (byScore != 0)
                    return byScore;

                int byDistance = Math.Abs(x.IndexA - x.IndexB).CompareTo(Math.Abs(y.IndexA - y.IndexB));
                if (byDistance != 0)
                    return byDistance;

                int byA = x.IndexA.CompareTo(y.IndexA);
                return byA != 0 ? byA : x.IndexB.CompareTo(y.IndexB);
            }
        );

        int minScore = a[0].ESize == 4 ? 120 : 80;
        var usedA = new bool[a.Count];
        var usedB = new bool[b.Count];
        var matches = new List<SarPairing>();

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

    private static int ScoreSarSimilarity(SarEntry a, SarEntry b)
    {
        if (a.Fingerprint != b.Fingerprint)
            return int.MinValue;

        int score = 0;
        score += ScoreBitSlots(a.BitSlots, b.BitSlots);

        if (a.ESize == 4 && b.ESize == 4)
            score += ScoreInt32Values(a.FirstVals, b.FirstVals);
        else if (a.TotalBytes == b.TotalBytes)
            score += 20;

        return score;
    }

    private static int ScoreBitSlots(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        if (a.Count == 0 && b.Count == 0)
            return 0;

        int ia = 0;
        int ib = 0;
        int overlap = 0;
        while (ia < a.Count && ib < b.Count)
        {
            if (a[ia] == b[ib])
            {
                overlap++;
                ia++;
                ib++;
            }
            else if (a[ia] < b[ib])
            {
                ia++;
            }
            else
            {
                ib++;
            }
        }

        int union = a.Count + b.Count - overlap;
        if (union == 0)
            return 0;
        if (overlap == union)
            return 200;

        return overlap * 60 - (union - overlap) * 25;
    }

    private static int ScoreInt32Values(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        int n = Math.Min(a.Count, b.Count);
        if (n == 0)
            return 0;

        int exact = 0;
        int near = 0;
        long penalty = 0;
        for (int i = 0; i < n; i++)
        {
            long diff = Math.Abs((long)a[i] - b[i]);
            if (diff == 0)
                exact++;
            else if (diff <= 3)
                near++;

            penalty += Math.Min(diff, 2500);
        }

        int score = exact * 80 + near * 20;
        score -= (int)Math.Min(penalty / Math.Max(1, n * 4), 250);

        bool aHighProto = LooksLikeHighProtoArray(a);
        bool bHighProto = LooksLikeHighProtoArray(b);
        if (aHighProto == bHighProto)
            score += 60;
        else
            score -= 100;

        bool aMostlyNegative = LooksMostlyNegative(a);
        bool bMostlyNegative = LooksMostlyNegative(b);
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

        int inspected = Math.Min(values.Count, 8);
        int high = 0;
        for (int i = 0; i < inspected; i++)
            if (values[i] > 1000)
                high++;

        return high >= Math.Max(2, inspected / 2);
    }

    private static bool LooksMostlyNegative(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
            return false;

        int inspected = Math.Min(values.Count, 8);
        int negative = 0;
        for (int i = 0; i < inspected; i++)
            if (values[i] < 0)
                negative++;

        return negative >= Math.Max(2, inspected / 2);
    }

    /// <summary>
    /// Returns a short human-readable label for a known element index within a specific SAR fingerprint.
    /// Falls back to the numeric index when no label is registered.
    /// </summary>
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

    /// <summary>
    /// Returns true when <paramref name="v"/> is in the magnitude range typical of
    /// runtime pointer/dispatch-table values (>200 000 000 absolute).
    /// Legitimate Arcanum game values — proto IDs, XP, stats — are always smaller.
    /// </summary>
    public static bool IsPointerLike(int v) => Math.Abs((long)v) > 200_000_000L;

    /// <summary>
    /// Splits a set of element diffs into semantic changes (game-data values) and
    /// pointer-like noise (both old and new value look like runtime addresses).
    /// Returns the semantic list and the number of suppressed pointer diffs.
    /// </summary>
    public static (List<(int Idx, int VA, int VB)> Semantic, int PointerCount) PartitionElementDiffs(
        List<(int Idx, int VA, int VB)> diffs
    )
    {
        var semantic = new List<(int Idx, int VA, int VB)>(diffs.Count);
        int pointerCount = 0;
        foreach (var d in diffs)
        {
            // Suppress only when BOTH old and new look like pointers.
            // If one side changed from/to a valid game value that's a real event.
            if (IsPointerLike(d.VA) && IsPointerLike(d.VB))
                pointerCount++;
            else
                semantic.Add(d);
        }

        return (semantic, pointerCount);
    }

    /// <summary>
    /// Value-aware annotation for a SAR entry that refines the fingerprint-only label
    /// when the element values themselves carry structural meaning.
    /// For <c>4:N:2</c> SARs this distinguishes blessing/curse proto-ID arrays
    /// (values mostly in the 1000–5000 range) from NPC dispatch tables
    /// (values are pointer-like large negatives).
    /// </summary>
    public static string AnnotateSarValue(SarEntry sar)
    {
        if (sar.ESize == 4 && sar.BCnt == 2 && sar.FirstVals.Length >= 2)
        {
            // Dispatch-table detection: any value looks like a runtime pointer.
            if (sar.FirstVals.Any(v => v != -1 && IsPointerLike(v)))
                return $"NPC-dispatch ptrs INT32[{sar.ECnt}]";

            // Blessing/schematic proto-ID detection: all non-(-1) values are > 500.
            int nonMinus1 = sar.FirstVals.Count(v => v != -1);
            if (nonMinus1 > 0 && sar.FirstVals.Where(v => v != -1).All(v => v > 500))
                return $"ProtoIdArray INT32[{sar.ECnt}] (bless/schematics)";

            // For eCnt=2: distinguish small-valued condition flags from mid-range proto-ID pairs.
            if (sar.ECnt == 2 && sar.FirstVals.Length == 2)
            {
                int v0 = sar.FirstVals[0],
                    v1 = sar.FirstVals[1];
                // Both values in the proto-ID range typical for conditions/curses (30–500).
                if (v0 is >= 30 and <= 500 && v1 is >= 30 and <= 500)
                    return "CondProto/CurseProto INT32[2]";
                // Small integer pair: likely a condition flag (type, severity/count).
                if (v0 is >= 0 and <= 10 || v1 is >= 0 and <= 10)
                    return "CondFlag INT32[2]";
            }
        }

        return AnnotateFingerprint(sar.Fingerprint);
    }

    /// <summary>Map a known bsId to a human-readable field label (ArciMagus session-specific bsIds).</summary>
    public static string AnnotateBsId(int bsId) =>
        bsId switch
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
            0x4A00 => "QuestState? sz16\u00d7n",
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
            _ => "",
        };

    private static IEnumerable<string> EnumerateLooseQuestFiles(string gameDir)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] roots =
        [
            Path.Combine(gameDir, "data"),
            Path.Combine(gameDir, "modules"),
            Path.Combine(gameDir, "modules", "Arcanum"),
        ];

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var fileName in s_questLookupCandidateFileNames)
            foreach (var path in Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories))
                if (seen.Add(path))
                    yield return path;
        }
    }

    private static IEnumerable<string> EnumerateDatArchives(string gameDir)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] roots = [gameDir, Path.Combine(gameDir, "modules")];

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                continue;

            foreach (var path in Directory.EnumerateFiles(root, "*.dat", SearchOption.TopDirectoryOnly).Order())
                if (seen.Add(path))
                    yield return path;
        }
    }

    private static QuestTextLookup? BuildQuestLookup(MesFile mes, string source)
    {
        var labels = new Dictionary<int, string>();
        foreach (var entry in mes.Entries)
        {
            if (entry.Index < 1000 || string.IsNullOrWhiteSpace(entry.Text) || labels.ContainsKey(entry.Index))
                continue;

            var normalized = string.Join(
                ' ',
                entry
                    .Text.Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            );

            if (normalized.Length > 0)
                labels[entry.Index] = normalized;
        }

        if (source.Contains("Module template", StringComparison.OrdinalIgnoreCase) && labels.Count <= 1)
            return null;

        return labels.Count == 0 ? null : new QuestTextLookup(labels, source);
    }

    private static void AppendInt32Values(ref ValueStringBuilder sb, ReadOnlySpan<int> values)
    {
        if (!values.IsEmpty)
            sb.AppendJoin(',', values);
    }

    private static void AppendInt32Values(ref ValueStringBuilder sb, IReadOnlyList<int> values, int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
                sb.Append(',');

            sb.Append(values[index]);
        }
    }

    private static bool IsQuestLookupCandidate(string fileName) =>
        s_questLookupCandidateFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase);

    private static readonly string[] s_questLookupCandidateFileNames =
    [
        "quests.mes",
        "gamequestlog.mes",
        "gamequestlogdumb.mes",
        "gamequest.mes",
    ];
}
