using System.Globalization;
using System.Text;
using static ArcNET.Diagnostics.SaveGlobalInt32Reader;

namespace ArcNET.Diagnostics;

public static class SaveGlobalRangeAnalysisService
{
    private const int DefaultMaxWindowInts = 16;
    private const int DefaultMinWindowSuffixInts = 16;
    private const int MaxData2RegionPreviewInts = 8;

    public static SaveGlobalRangeAnalysisSnapshot Analyze(
        IReadOnlyList<SaveGlobalSlotSnapshot> snapshots,
        IReadOnlyList<string>? fileNames = null
    )
    {
        fileNames ??= SaveGlobalAnalysisService.KnownFileNames;
        var hotIndices = new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);
        var windowPatterns = new Dictionary<string, Dictionary<SaveGlobalWindowPatternKey, int>>(
            StringComparer.OrdinalIgnoreCase
        );
        var windowTraces = new Dictionary<string, Dictionary<SaveGlobalWindowTraceKey, int>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var fileName in fileNames)
        {
            hotIndices[fileName] = [];
            windowPatterns[fileName] = [];
            windowTraces[fileName] = [];
        }

        for (var index = 1; index < snapshots.Count; index++)
        {
            var previous = snapshots[index - 1];
            var current = snapshots[index];
            foreach (var fileName in fileNames)
            {
                if (
                    previous.Files.TryGetValue(fileName, out var before)
                    && current.Files.TryGetValue(fileName, out var after)
                )
                    RecordFileSignals(
                        in before,
                        in after,
                        hotIndices[fileName],
                        windowPatterns[fileName],
                        windowTraces[fileName]
                    );
            }
        }

        return new SaveGlobalRangeAnalysisSnapshot(
            ConvertHotIndices(hotIndices),
            ConvertWindowPatterns(windowPatterns),
            ConvertWindowTraces(windowTraces),
            BuildFrontMatterFamilies(snapshots),
            BuildTailFamilies(snapshots),
            BuildData2RegionFamilies(snapshots, isPrefix: true),
            BuildData2RegionFamilies(snapshots, isPrefix: false)
        );
    }

    public static int CountChangedIntRegion(
        byte[] beforeBytes,
        int beforeStartInt,
        int beforeCount,
        byte[] afterBytes,
        int afterStartInt,
        int afterCount
    )
    {
        var commonCount = Math.Min(beforeCount, afterCount);
        var changed = Math.Abs(beforeCount - afterCount);
        for (var index = 0; index < commonCount; index++)
        {
            if (ReadInt32(beforeBytes, beforeStartInt + index) != ReadInt32(afterBytes, afterStartInt + index))
                changed++;
        }

        return changed;
    }

    public static SaveGlobalContiguousIntWindow? TryDetectContiguousIntWindow(
        in SaveGlobalFileSnapshot before,
        in SaveGlobalFileSnapshot after,
        int prefixInts,
        int maxWindowInts = DefaultMaxWindowInts,
        int minWindowSuffixInts = DefaultMinWindowSuffixInts
    )
    {
        var beforeIndex = before.TotalInts - 1;
        var afterIndex = after.TotalInts - 1;
        var commonSuffixInts = 0;

        while (
            beforeIndex >= prefixInts
            && afterIndex >= prefixInts
            && ReadInt32(before.Bytes, beforeIndex) == ReadInt32(after.Bytes, afterIndex)
        )
        {
            commonSuffixInts++;
            beforeIndex--;
            afterIndex--;
        }

        var removedInts = before.TotalInts - prefixInts - commonSuffixInts;
        var addedInts = after.TotalInts - prefixInts - commonSuffixInts;
        if (removedInts < 0 || addedInts < 0)
            return null;

        if (removedInts == 0 && addedInts == 0)
            return null;

        if (removedInts > maxWindowInts || addedInts > maxWindowInts || commonSuffixInts < minWindowSuffixInts)
            return null;

        return new SaveGlobalContiguousIntWindow(prefixInts, removedInts, addedInts, commonSuffixInts);
    }

    private static void RecordFileSignals(
        in SaveGlobalFileSnapshot before,
        in SaveGlobalFileSnapshot after,
        Dictionary<int, int> hotIndices,
        Dictionary<SaveGlobalWindowPatternKey, int> windowPatterns,
        Dictionary<SaveGlobalWindowTraceKey, int> windowTraces
    )
    {
        var commonInts = Math.Min(before.TotalInts, after.TotalInts);
        var prefixInts = 0;
        while (prefixInts < commonInts && ReadInt32(before.Bytes, prefixInts) == ReadInt32(after.Bytes, prefixInts))
            prefixInts++;

        for (var index = 0; index < commonInts; index++)
        {
            if (ReadInt32(before.Bytes, index) == ReadInt32(after.Bytes, index))
                continue;

            hotIndices[index] = hotIndices.GetValueOrDefault(index) + 1;
        }

        var window = TryDetectContiguousIntWindow(in before, in after, prefixInts);
        if (window is not { } detectedWindow)
            return;

        var pattern = new SaveGlobalWindowPatternKey(
            detectedWindow.StartInt,
            detectedWindow.RemovedInts,
            detectedWindow.AddedInts
        );
        windowPatterns[pattern] = windowPatterns.GetValueOrDefault(pattern) + 1;

        if (detectedWindow.RemovedInts == detectedWindow.AddedInts && detectedWindow.RemovedInts > 0)
        {
            var trace = new SaveGlobalWindowTraceKey(detectedWindow.StartInt, detectedWindow.RemovedInts);
            windowTraces[trace] = windowTraces.GetValueOrDefault(trace) + 1;
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<SaveGlobalHotIndexHitSnapshot>> ConvertHotIndices(
        Dictionary<string, Dictionary<int, int>> hotIndices
    )
    {
        var results = new Dictionary<string, IReadOnlyList<SaveGlobalHotIndexHitSnapshot>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var (fileName, counts) in hotIndices)
        {
            results[fileName] =
            [
                .. counts
                    .OrderByDescending(static pair => pair.Value)
                    .ThenBy(static pair => pair.Key)
                    .Select(static pair => new SaveGlobalHotIndexHitSnapshot(pair.Key, pair.Value)),
            ];
        }

        return results;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<SaveGlobalWindowPatternHitSnapshot>> ConvertWindowPatterns(
        Dictionary<string, Dictionary<SaveGlobalWindowPatternKey, int>> windowPatterns
    )
    {
        var results = new Dictionary<string, IReadOnlyList<SaveGlobalWindowPatternHitSnapshot>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var (fileName, patterns) in windowPatterns)
        {
            results[fileName] =
            [
                .. patterns
                    .OrderByDescending(static pair => pair.Value)
                    .ThenBy(static pair => pair.Key.StartInt)
                    .ThenBy(static pair => pair.Key.RemovedInts)
                    .ThenBy(static pair => pair.Key.AddedInts)
                    .Select(static pair => new SaveGlobalWindowPatternHitSnapshot(
                        pair.Key.StartInt,
                        pair.Key.RemovedInts,
                        pair.Key.AddedInts,
                        pair.Value
                    )),
            ];
        }

        return results;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<SaveGlobalWindowTraceHitSnapshot>> ConvertWindowTraces(
        Dictionary<string, Dictionary<SaveGlobalWindowTraceKey, int>> windowTraces
    )
    {
        var results = new Dictionary<string, IReadOnlyList<SaveGlobalWindowTraceHitSnapshot>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var (fileName, traces) in windowTraces)
        {
            results[fileName] =
            [
                .. traces
                    .OrderByDescending(static pair => pair.Value)
                    .ThenBy(static pair => pair.Key.StartInt)
                    .ThenBy(static pair => pair.Key.Width)
                    .Select(static pair => new SaveGlobalWindowTraceHitSnapshot(
                        pair.Key.StartInt,
                        pair.Key.Width,
                        pair.Value
                    )),
            ];
        }

        return results;
    }

    private static IReadOnlyList<SaveGlobalFrontMatterFamilySnapshot> BuildFrontMatterFamilies(
        IReadOnlyList<SaveGlobalSlotSnapshot> snapshots
    )
    {
        var families = new Dictionary<SaveGlobalFrontMatterFamilyKey, List<int>>();
        foreach (var snapshot in snapshots)
        {
            if (!TryBuildFrontMatterFamilyKey(in snapshot, out var key))
                continue;

            if (!families.TryGetValue(key, out var slots))
            {
                slots = [];
                families.Add(key, slots);
            }

            slots.Add(snapshot.Slot);
        }

        return
        [
            .. families
                .OrderByDescending(static pair => pair.Value.Count)
                .ThenBy(static pair => pair.Value[0])
                .Select(static pair => new SaveGlobalFrontMatterFamilySnapshot(
                    [.. pair.Value],
                    pair.Key.RowCount,
                    pair.Key.SectionCount,
                    pair.Key.Sequence
                )),
        ];
    }

    private static IReadOnlyList<SaveGlobalTailFamilySnapshot> BuildTailFamilies(
        IReadOnlyList<SaveGlobalSlotSnapshot> snapshots
    )
    {
        var families = new Dictionary<SaveGlobalTailFamilyKey, List<int>>();
        foreach (var snapshot in snapshots)
        {
            if (!TryBuildTailFamilyKey(in snapshot, out var key))
                continue;

            if (!families.TryGetValue(key, out var slots))
            {
                slots = [];
                families.Add(key, slots);
            }

            slots.Add(snapshot.Slot);
        }

        return
        [
            .. families
                .OrderByDescending(static pair => pair.Value.Count)
                .ThenBy(static pair => pair.Value[0])
                .Select(static pair => new SaveGlobalTailFamilySnapshot(
                    [.. pair.Value],
                    pair.Key.RowCount,
                    pair.Key.SectionCount,
                    pair.Key.Sequence
                )),
        ];
    }

    private static IReadOnlyList<SaveGlobalData2RegionFamilySnapshot> BuildData2RegionFamilies(
        IReadOnlyList<SaveGlobalSlotSnapshot> snapshots,
        bool isPrefix
    )
    {
        var families = new Dictionary<SaveGlobalData2RegionFamilyKey, List<int>>();
        foreach (var snapshot in snapshots)
        {
            if (!TryBuildData2RegionFamilyKey(in snapshot, isPrefix, out var key))
                continue;

            if (!families.TryGetValue(key, out var slots))
            {
                slots = [];
                families.Add(key, slots);
            }

            slots.Add(snapshot.Slot);
        }

        return
        [
            .. families
                .OrderByDescending(static pair => pair.Value.Count)
                .ThenBy(static pair => pair.Value[0])
                .Select(static pair => new SaveGlobalData2RegionFamilySnapshot(
                    [.. pair.Value],
                    pair.Key.IntCount,
                    pair.Key.Sequence,
                    pair.Key.Preview
                )),
        ];
    }

    private static bool TryBuildFrontMatterFamilyKey(
        in SaveGlobalSlotSnapshot snapshot,
        out SaveGlobalFrontMatterFamilyKey key
    )
    {
        if (!snapshot.Files.TryGetValue("data.sav", out var file) || file.QuadSummary is not { } quadSummary)
        {
            key = default;
            return false;
        }

        if (quadSummary.FrontMatterSectionCount == 0)
        {
            key = default;
            return false;
        }

        key = new SaveGlobalFrontMatterFamilyKey(
            quadSummary.FrontMatterRowCount,
            quadSummary.FrontMatterSectionCount,
            BuildFrontMatterSequenceKey(quadSummary.FrontMatterRuns)
        );
        return true;
    }

    private static bool TryBuildTailFamilyKey(in SaveGlobalSlotSnapshot snapshot, out SaveGlobalTailFamilyKey key)
    {
        if (!snapshot.Files.TryGetValue("data.sav", out var file) || file.QuadSummary is not { } quadSummary)
        {
            key = default;
            return false;
        }

        if (quadSummary.TailSectionCount == 0)
        {
            key = default;
            return false;
        }

        key = new SaveGlobalTailFamilyKey(
            quadSummary.TailRowCount,
            quadSummary.TailSectionCount,
            BuildFrontMatterSequenceKey(quadSummary.TailRuns)
        );
        return true;
    }

    private static bool TryBuildData2RegionFamilyKey(
        in SaveGlobalSlotSnapshot snapshot,
        bool isPrefix,
        out SaveGlobalData2RegionFamilyKey key
    )
    {
        if (!snapshot.Files.TryGetValue("data2.sav", out var file) || file.Data2Sav is not { } data2Sav)
        {
            key = default;
            return false;
        }

        var count = isPrefix ? data2Sav.PrefixIntCount : data2Sav.SuffixIntCount;
        if (count == 0)
        {
            key = default;
            return false;
        }

        Span<int> values = count <= 128 ? stackalloc int[count] : new int[count];
        if (isPrefix)
            data2Sav.CopyPrefixInts(0, values);
        else
            data2Sav.CopySuffixInts(0, values);

        key = new SaveGlobalData2RegionFamilyKey(count, BuildIntSequenceKey(values), BuildIntPreview(values));
        return true;
    }

    private static string BuildFrontMatterSequenceKey(IReadOnlyList<AlignedQuadRunSummary> runs)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < runs.Count; index++)
        {
            if (index > 0)
                builder.Append(" | ");

            var run = runs[index];
            var signature = run.Signature;
            builder.Append(signature.B);
            builder.Append('/');
            builder.Append(signature.C);
            builder.Append('/');
            builder.Append(signature.D.ToString("X8", CultureInfo.InvariantCulture));
            builder.Append('x');
            builder.Append(run.Length);
        }

        return builder.ToString();
    }

    private static string BuildIntSequenceKey(ReadOnlySpan<int> values)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
                builder.Append(',');

            builder.Append(values[index]);
        }

        return builder.ToString();
    }

    private static string BuildIntPreview(ReadOnlySpan<int> values)
    {
        var builder = new StringBuilder();
        var headCount = Math.Min(values.Length, MaxData2RegionPreviewInts);
        builder.Append("head=");
        builder.Append(FormatIntSpanPreview(values[..headCount]));
        if (values.Length > MaxData2RegionPreviewInts)
        {
            builder.Append(" tail=");
            builder.Append(FormatIntSpanPreview(values[^MaxData2RegionPreviewInts..]));
        }

        return builder.ToString();
    }

    private static string FormatIntSpanPreview(ReadOnlySpan<int> values)
    {
        var builder = new StringBuilder();
        builder.Append('[');
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
                builder.Append(',');

            builder.Append(values[index]);
        }

        builder.Append(']');
        return builder.ToString();
    }
}
