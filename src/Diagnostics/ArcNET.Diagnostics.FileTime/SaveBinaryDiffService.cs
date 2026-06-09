using System.Text;

namespace ArcNET.Diagnostics;

public static class SaveBinaryDiffService
{
    public static IReadOnlyList<SaveBinaryDiffRegionSnapshot> FindDiffRegions(
        byte[] beforeBytes,
        byte[] afterBytes,
        int contextBytes = 8,
        int mergeGap = 16
    )
    {
        var minLength = Math.Min(beforeBytes.Length, afterBytes.Length);
        var maxLength = Math.Max(beforeBytes.Length, afterBytes.Length);

        List<(int Start, int End)> spans = [];
        var spanStart = -1;
        for (var index = 0; index < maxLength; index++)
        {
            var differs = index >= minLength || beforeBytes[index] != afterBytes[index];
            if (differs && spanStart < 0)
                spanStart = index;
            else if (!differs && spanStart >= 0)
            {
                spans.Add((spanStart, index));
                spanStart = -1;
            }
        }

        if (spanStart >= 0)
            spans.Add((spanStart, maxLength));

        if (spans.Count == 0)
            return [];

        List<(int Start, int End)> merged = [spans[0]];
        for (var index = 1; index < spans.Count; index++)
        {
            var previous = merged[^1];
            var current = spans[index];
            if (current.Start - previous.End <= mergeGap)
                merged[^1] = (previous.Start, current.End);
            else
                merged.Add(current);
        }

        List<SaveBinaryDiffRegionSnapshot> regions = [];
        foreach (var (startRaw, endRaw) in merged)
        {
            var start = Math.Max(0, startRaw - contextBytes);
            var end = Math.Min(maxLength, endRaw + contextBytes);
            var length = end - start;
            var before = new byte[length];
            var after = new byte[length];
            var changed = 0;
            for (var index = 0; index < length; index++)
            {
                var absolute = start + index;
                before[index] = absolute < beforeBytes.Length ? beforeBytes[absolute] : (byte)0;
                after[index] = absolute < afterBytes.Length ? afterBytes[absolute] : (byte)0;
                if (before[index] != after[index])
                    changed++;
            }

            regions.Add(new SaveBinaryDiffRegionSnapshot(start, before, after, changed));
        }

        return regions;
    }

    public static SaveBinaryDiffPreviewSnapshot CreatePreview(
        IReadOnlyList<SaveBinaryDiffRegionSnapshot> regions,
        int maxRegions = 20,
        int bytesPerRow = 8
    )
    {
        var showCount = Math.Min(regions.Count, maxRegions);
        List<SaveBinaryDiffRegionPreviewSnapshot> previews = [];
        for (var regionIndex = 0; regionIndex < showCount; regionIndex++)
        {
            var region = regions[regionIndex];
            List<SaveBinaryHexRowSnapshot> rows = [];
            for (var rowOffset = 0; rowOffset < region.BeforeBytes.Length; rowOffset += bytesPerRow)
            {
                var rowLength = Math.Min(bytesPerRow, region.BeforeBytes.Length - rowOffset);
                rows.Add(
                    new SaveBinaryHexRowSnapshot(
                        region.Offset + rowOffset,
                        BuildHexColumn(
                            region.BeforeBytes,
                            region.AfterBytes,
                            rowOffset,
                            rowLength,
                            bytesPerRow,
                            beforeSide: true
                        ),
                        BuildAsciiColumn(region.BeforeBytes, rowOffset, rowLength),
                        BuildHexColumn(
                            region.BeforeBytes,
                            region.AfterBytes,
                            rowOffset,
                            rowLength,
                            bytesPerRow,
                            beforeSide: false
                        ),
                        BuildAsciiColumn(region.AfterBytes, rowOffset, rowLength)
                    )
                );
            }

            previews.Add(
                new SaveBinaryDiffRegionPreviewSnapshot(
                    region.Offset,
                    region.BeforeBytes.Length,
                    region.ChangedByteCount,
                    rows
                )
            );
        }

        return new SaveBinaryDiffPreviewSnapshot(previews, Math.Max(0, regions.Count - showCount));
    }

    public static SaveBinaryDiffSetSnapshot CompareInnerFiles(
        IReadOnlyDictionary<string, byte[]> filesA,
        IReadOnlyDictionary<string, byte[]> filesB
    )
    {
        var allKeys = new HashSet<string>(filesA.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(filesB.Keys);

        List<SaveInnerFileDiffSnapshot> diffs = [];
        foreach (var key in allKeys.OrderBy(static key => key))
        {
            var inA = filesA.TryGetValue(key, out var bytesA);
            var inB = filesB.TryGetValue(key, out var bytesB);
            bytesA ??= [];
            bytesB ??= [];

            if (!inA)
            {
                diffs.Add(new SaveInnerFileDiffSnapshot(key, false, true, 0, bytesB.Length, bytesA, bytesB, [], 0));
                continue;
            }

            if (!inB)
            {
                diffs.Add(new SaveInnerFileDiffSnapshot(key, true, false, bytesA.Length, 0, bytesA, bytesB, [], 0));
                continue;
            }

            if (bytesA.AsSpan().SequenceEqual(bytesB))
                continue;

            var regions = FindDiffRegions(bytesA, bytesB);
            diffs.Add(
                new SaveInnerFileDiffSnapshot(
                    key,
                    false,
                    false,
                    bytesA.Length,
                    bytesB.Length,
                    bytesA,
                    bytesB,
                    regions,
                    regions.Sum(static region => region.ChangedByteCount)
                )
            );
        }

        return new SaveBinaryDiffSetSnapshot(allKeys.Count, diffs.Count, allKeys.Count - diffs.Count, diffs);
    }

    private static string BuildHexColumn(
        byte[] beforeBytes,
        byte[] afterBytes,
        int rowOffset,
        int rowLength,
        int bytesPerRow,
        bool beforeSide
    )
    {
        var builder = new StringBuilder(bytesPerRow * 4 + 1);
        for (var column = 0; column < rowLength; column++)
        {
            var before = beforeBytes[rowOffset + column];
            var after = afterBytes[rowOffset + column];
            var value = beforeSide ? before : after;
            if (before != after)
                builder.Append('[').Append(value.ToString("X2")).Append(']');
            else
                builder.Append(' ').Append(value.ToString("X2")).Append(' ');

            if (column == 3)
                builder.Append(' ');
        }

        return builder.ToString().PadRight(bytesPerRow * 4 + 1);
    }

    private static string BuildAsciiColumn(byte[] bytes, int rowOffset, int rowLength)
    {
        var builder = new StringBuilder(rowLength);
        for (var column = 0; column < rowLength; column++)
        {
            var value = bytes[rowOffset + column];
            builder.Append(value is >= 32 and < 127 ? (char)value : '.');
        }

        return builder.ToString();
    }
}
