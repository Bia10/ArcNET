using static ArcNET.Diagnostics.SaveGlobalInt32Reader;

namespace ArcNET.Diagnostics;

public static class SaveGlobalDiffService
{
    public static SaveGlobalFileDiffSnapshot Compare(
        in SaveGlobalFileSnapshot before,
        in SaveGlobalFileSnapshot after,
        int maxChangedSamples = 6,
        int maxWindowInts = 16,
        int minWindowSuffixInts = 16,
        int maxSaveIdPairPreview = 10
    )
    {
        var commonInts = Math.Min(before.TotalInts, after.TotalInts);
        var prefixInts = 0;
        while (prefixInts < commonInts && ReadInt32(before.Bytes, prefixInts) == ReadInt32(after.Bytes, prefixInts))
            prefixInts++;

        var samples = new List<SaveGlobalChangedIntSampleSnapshot>(Math.Min(maxChangedSamples, commonInts));
        var changedInts = 0;
        for (var index = 0; index < commonInts; index++)
        {
            var oldValue = ReadInt32(before.Bytes, index);
            var newValue = ReadInt32(after.Bytes, index);
            if (oldValue == newValue)
                continue;

            changedInts++;
            if (samples.Count >= maxChangedSamples)
                continue;

            samples.Add(new SaveGlobalChangedIntSampleSnapshot(index, oldValue, newValue));
        }

        var commonBytes = Math.Min(before.Bytes.Length, after.Bytes.Length);
        var changedTailBytes = 0;
        for (var index = commonInts * 4; index < commonBytes; index++)
        {
            if (before.Bytes[index] != after.Bytes[index])
                changedTailBytes++;
        }

        var isIdentical =
            before.Bytes.Length == after.Bytes.Length
            && changedInts == 0
            && changedTailBytes == 0
            && before.TrailingBytes == after.TrailingBytes;

        var saveIdPairs =
            before.SaveIdPairs is { } beforePairs && after.SaveIdPairs is { } afterPairs
                ? CompareSaveIdPairs(in before, in beforePairs, in after, in afterPairs, maxSaveIdPairPreview)
                : null;

        return new SaveGlobalFileDiffSnapshot(
            isIdentical,
            before.Bytes.Length,
            after.Bytes.Length,
            before.Header0,
            before.Header1,
            after.Header0,
            after.Header1,
            before.NonZeroCount,
            after.NonZeroCount,
            changedInts,
            prefixInts,
            Math.Max(0, after.TotalInts - before.TotalInts),
            Math.Max(0, before.TotalInts - after.TotalInts),
            changedTailBytes,
            before.TrailingBytes,
            after.TrailingBytes,
            samples,
            SaveGlobalRangeAnalysisService.TryDetectContiguousIntWindow(
                in before,
                in after,
                prefixInts,
                maxWindowInts,
                minWindowSuffixInts
            ),
            before.QuadSummary is { } beforeQuad && after.QuadSummary is { } afterQuad
                ? CompareAlignedQuad(in beforeQuad, in afterQuad)
                : null,
            saveIdPairs
        );
    }

    private static SaveGlobalSaveIdPairDiffSnapshot? CompareSaveIdPairs(
        in SaveGlobalFileSnapshot beforeFile,
        in SaveIdPairTableSnapshot before,
        in SaveGlobalFileSnapshot afterFile,
        in SaveIdPairTableSnapshot after,
        int maxPreview
    )
    {
        var changedIds = new List<int>();
        foreach (var (id, newValue) in after.Values)
        {
            if (!before.Values.TryGetValue(id, out var oldValue) || oldValue != newValue)
                changedIds.Add(id);
        }

        foreach (var id in before.Values.Keys)
        {
            if (!after.Values.ContainsKey(id))
                changedIds.Add(id);
        }

        var beforePrefixIntCount = SaveGlobalAnalysisService.GetData2PrefixIntCount(in before);
        var afterPrefixIntCount = SaveGlobalAnalysisService.GetData2PrefixIntCount(in after);
        var beforeSuffixIntCount = SaveGlobalAnalysisService.GetData2SuffixIntCount(beforeFile.TotalInts, in before);
        var afterSuffixIntCount = SaveGlobalAnalysisService.GetData2SuffixIntCount(afterFile.TotalInts, in after);
        var prefixChanged = SaveGlobalRangeAnalysisService.CountChangedIntRegion(
            beforeFile.Bytes,
            0,
            beforePrefixIntCount,
            afterFile.Bytes,
            0,
            afterPrefixIntCount
        );
        var suffixChanged = SaveGlobalRangeAnalysisService.CountChangedIntRegion(
            beforeFile.Bytes,
            before.EndInt + 1,
            beforeSuffixIntCount,
            afterFile.Bytes,
            after.EndInt + 1,
            afterSuffixIntCount
        );

        changedIds.Sort();
        if (
            changedIds.Count == 0
            && before.StartInt == after.StartInt
            && before.PairCount == after.PairCount
            && before.NonZeroPairs == after.NonZeroPairs
            && prefixChanged == 0
            && suffixChanged == 0
            && beforePrefixIntCount == afterPrefixIntCount
            && beforeSuffixIntCount == afterSuffixIntCount
        )
            return null;

        var preview = new List<SaveGlobalSaveIdPairValueDiffSnapshot>(Math.Min(changedIds.Count, maxPreview));
        for (var index = 0; index < changedIds.Count && index < maxPreview; index++)
        {
            var id = changedIds[index];
            preview.Add(
                new SaveGlobalSaveIdPairValueDiffSnapshot(
                    id,
                    before.Values.TryGetValue(id, out var oldValue) ? oldValue : null,
                    after.Values.TryGetValue(id, out var newValue) ? newValue : null
                )
            );
        }

        return new SaveGlobalSaveIdPairDiffSnapshot(
            before.StartInt,
            after.StartInt,
            before.PairCount,
            after.PairCount,
            before.NonZeroPairs,
            after.NonZeroPairs,
            beforePrefixIntCount,
            afterPrefixIntCount,
            prefixChanged,
            beforeSuffixIntCount,
            afterSuffixIntCount,
            suffixChanged,
            preview,
            changedIds.Count
        );
    }

    private static SaveGlobalAlignedQuadDiffSnapshot? CompareAlignedQuad(
        in AlignedQuadSummary before,
        in AlignedQuadSummary after
    )
    {
        var frontMatter = CompareFrontMatter(in before, in after);
        var tail = CompareTail(in before, in after);
        if (
            before.SectionCount == after.SectionCount
            && before.ZeroSectionCount == after.ZeroSectionCount
            && before.LongestZeroSectionStart == after.LongestZeroSectionStart
            && before.LongestZeroSectionLength == after.LongestZeroSectionLength
            && frontMatter is null
            && tail is null
        )
            return null;

        return new SaveGlobalAlignedQuadDiffSnapshot(
            before.SectionCount,
            after.SectionCount,
            before.ZeroSectionCount,
            after.ZeroSectionCount,
            before.LongestZeroSectionStart,
            before.LongestZeroSectionLength,
            after.LongestZeroSectionStart,
            after.LongestZeroSectionLength,
            frontMatter,
            tail
        );
    }

    private static SaveGlobalFrontMatterDiffSnapshot? CompareFrontMatter(
        in AlignedQuadSummary before,
        in AlignedQuadSummary after
    )
    {
        var beforeRuns = before.FrontMatterRuns;
        var afterRuns = after.FrontMatterRuns;
        var samePrefix = 0;
        var commonCount = Math.Min(beforeRuns.Count, afterRuns.Count);
        while (samePrefix < commonCount && HasSameRunShape(beforeRuns[samePrefix], afterRuns[samePrefix]))
            samePrefix++;

        if (
            before.FrontMatterRowCount == after.FrontMatterRowCount
            && before.FrontMatterSectionCount == after.FrontMatterSectionCount
            && samePrefix == beforeRuns.Count
            && samePrefix == afterRuns.Count
        )
            return null;

        return new SaveGlobalFrontMatterDiffSnapshot(
            before.FrontMatterRowCount,
            after.FrontMatterRowCount,
            before.FrontMatterSectionCount,
            after.FrontMatterSectionCount,
            samePrefix,
            samePrefix < beforeRuns.Count ? beforeRuns[samePrefix] : null,
            samePrefix < afterRuns.Count ? afterRuns[samePrefix] : null
        );
    }

    private static SaveGlobalTailDiffSnapshot? CompareTail(in AlignedQuadSummary before, in AlignedQuadSummary after)
    {
        var beforeRuns = before.TailRuns;
        var afterRuns = after.TailRuns;
        var samePrefix = 0;
        var commonCount = Math.Min(beforeRuns.Count, afterRuns.Count);
        while (samePrefix < commonCount && HasSameRunShape(beforeRuns[samePrefix], afterRuns[samePrefix]))
            samePrefix++;

        if (
            before.TailRowStart == after.TailRowStart
            && before.TailRowCount == after.TailRowCount
            && before.TailSectionCount == after.TailSectionCount
            && samePrefix == beforeRuns.Count
            && samePrefix == afterRuns.Count
        )
            return null;

        return new SaveGlobalTailDiffSnapshot(
            before.TailRowStart,
            after.TailRowStart,
            before.TailRowCount,
            after.TailRowCount,
            before.TailSectionCount,
            after.TailSectionCount,
            samePrefix,
            samePrefix < beforeRuns.Count ? beforeRuns[samePrefix] : null,
            samePrefix < afterRuns.Count ? afterRuns[samePrefix] : null
        );
    }

    private static bool HasSameRunShape(AlignedQuadRunSummary left, AlignedQuadRunSummary right)
    {
        var leftSignature = left.Signature;
        var rightSignature = right.Signature;
        return left.Length == right.Length
            && leftSignature.B == rightSignature.B
            && leftSignature.C == rightSignature.C
            && leftSignature.D == rightSignature.D;
    }
}
