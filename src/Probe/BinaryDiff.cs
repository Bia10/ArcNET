using Bia.ValueBuffers;

namespace Probe;

// Binary diff utilities used by Probe Mode 11.

internal record DiffRegion(int Offset, byte[] A, byte[] B);

internal record FileDiff(
    string Path,
    bool OnlyInA,
    bool OnlyInB,
    int SizeA,
    int SizeB,
    byte[] BytesA,
    byte[] BytesB,
    List<DiffRegion> Regions
)
{
    /// <summary>Count of genuinely changed bytes (context bytes excluded).</summary>
    public int ChangedByteCount =>
        Regions.Sum(r =>
        {
            int n = 0;
            for (int i = 0; i < r.A.Length; i++)
                if (r.A[i] != r.B[i])
                    n++;
            return n;
        });
}

internal static class BinaryDiff
{
    /// <summary>
    /// Find all byte-differing regions between two buffers.
    /// Adjacent diff spans within <paramref name="mergeGap"/> bytes are merged.
    /// Each region is padded by <paramref name="contextBytes"/> on each side.
    /// Bytes beyond the shorter buffer are treated as 0x00 on that side.
    /// </summary>
    public static List<DiffRegion> FindDiffRegions(byte[] a, byte[] b, int contextBytes = 8, int mergeGap = 16)
    {
        int minLen = Math.Min(a.Length, b.Length);
        int maxLen = Math.Max(a.Length, b.Length);

        // Collect raw diff spans [start, end) (exclusive end)
        var spans = new List<(int Start, int End)>();
        int spanStart = -1;
        for (int i = 0; i < maxLen; i++)
        {
            bool differs = i >= minLen || a[i] != b[i];
            if (differs && spanStart < 0)
                spanStart = i;
            else if (!differs && spanStart >= 0)
            {
                spans.Add((spanStart, i));
                spanStart = -1;
            }
        }
        if (spanStart >= 0)
            spans.Add((spanStart, maxLen));

        if (spans.Count == 0)
            return [];

        // Merge spans with gap ≤ mergeGap
        var merged = new List<(int Start, int End)> { spans[0] };
        for (int i = 1; i < spans.Count; i++)
        {
            var prev = merged[^1];
            var cur = spans[i];
            if (cur.Start - prev.End <= mergeGap)
                merged[^1] = (prev.Start, cur.End);
            else
                merged.Add(cur);
        }

        // Expand by contextBytes and build DiffRegion list
        var result = new List<DiffRegion>(merged.Count);
        foreach (var (s, e) in merged)
        {
            int start = Math.Max(0, s - contextBytes);
            int end = Math.Min(maxLen, e + contextBytes);
            int len = end - start;
            var ra = new byte[len];
            var rb = new byte[len];
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                ra[i] = idx < a.Length ? a[idx] : (byte)0;
                rb[i] = idx < b.Length ? b[idx] : (byte)0;
            }
            result.Add(new DiffRegion(start, ra, rb));
        }
        return result;
    }

    /// <summary>
    /// Print a side-by-side hex diff to stdout.
    /// Each row shows 8 bytes from A and 8 bytes from B on the same line.
    /// Changed bytes are wrapped in [XX] brackets; an ASCII column follows each side.
    /// </summary>
    public static void PrintHexDiff(List<DiffRegion> regions, int maxRegions = 20)
    {
        int shown = 0;
        const int perRow = 8;
        Span<char> hexABuf = stackalloc char[64];
        Span<char> hexBBuf = stackalloc char[64];
        Span<char> ascABuf = stackalloc char[32];
        Span<char> ascBBuf = stackalloc char[32];
        var sbHexA = new ValueStringBuilder(hexABuf);
        var sbHexB = new ValueStringBuilder(hexBBuf);
        var sbAscA = new ValueStringBuilder(ascABuf);
        var sbAscB = new ValueStringBuilder(ascBBuf);
        foreach (var region in regions)
        {
            if (shown >= maxRegions)
            {
                Console.WriteLine($"  ... ({regions.Count - shown} more regions)");
                break;
            }
            shown++;

            int changed = 0;
            for (int i = 0; i < region.A.Length; i++)
                if (region.A[i] != region.B[i])
                    changed++;
            Console.WriteLine($"  @0x{region.Offset:X5}  ({region.A.Length}B, {changed} changed)");

            for (int row = 0; row < region.A.Length; row += perRow)
            {
                int rowLen = Math.Min(perRow, region.A.Length - row);
                int absOff = region.Offset + row;

                sbHexA.Clear();
                sbHexB.Clear();
                sbAscA.Clear();
                sbAscB.Clear();

                for (int col = 0; col < rowLen; col++)
                {
                    byte ba = region.A[row + col];
                    byte bb = region.B[row + col];
                    bool differs = ba != bb;

                    if (differs)
                    {
                        sbHexA.Append('[');
                        sbHexA.AppendHex(ba);
                        sbHexA.Append(']');
                        sbHexB.Append('[');
                        sbHexB.AppendHex(bb);
                        sbHexB.Append(']');
                    }
                    else
                    {
                        sbHexA.Append(' ');
                        sbHexA.AppendHex(ba);
                        sbHexA.Append(' ');
                        sbHexB.Append(' ');
                        sbHexB.AppendHex(bb);
                        sbHexB.Append(' ');
                    }
                    if (col == 3)
                    {
                        sbHexA.Append(' ');
                        sbHexB.Append(' ');
                    }
                    sbAscA.Append(ba is >= 32 and < 127 ? (char)ba : '.');
                    sbAscB.Append(bb is >= 32 and < 127 ? (char)bb : '.');
                }

                // Pad short last row to align ASCII columns
                int hexWidth = perRow * 4 + 1; // 4 chars/byte + 1 mid-gap space
                if (sbHexA.Length < hexWidth)
                    sbHexA.Append(' ', hexWidth - sbHexA.Length);
                if (sbHexB.Length < hexWidth)
                    sbHexB.Append(' ', hexWidth - sbHexB.Length);

                Console.WriteLine(
                    $"  {absOff:X5}:  {sbHexA.WrittenSpan} {sbAscA.WrittenSpan}  │  {sbHexB.WrittenSpan} {sbAscB.WrittenSpan}"
                );
            }
        }
    }

    /// <summary>
    /// Compare all inner files from two saves.
    /// Returns one FileDiff per file that is not byte-identical.
    /// </summary>
    public static List<FileDiff> CompareInnerFiles(
        IReadOnlyDictionary<string, byte[]> filesA,
        IReadOnlyDictionary<string, byte[]> filesB
    )
    {
        var result = new List<FileDiff>();
        var allKeys = new HashSet<string>(filesA.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(filesB.Keys);

        foreach (var key in allKeys.OrderBy(k => k))
        {
            bool inA = filesA.TryGetValue(key, out var bytesA);
            bool inB = filesB.TryGetValue(key, out var bytesB);
            bytesA ??= [];
            bytesB ??= [];

            if (!inA)
            {
                result.Add(new FileDiff(key, false, true, 0, bytesB.Length, bytesA, bytesB, []));
                continue;
            }
            if (!inB)
            {
                result.Add(new FileDiff(key, true, false, bytesA.Length, 0, bytesA, bytesB, []));
                continue;
            }
            if (bytesA.AsSpan().SequenceEqual(bytesB))
                continue; // identical — skip

            var regions = FindDiffRegions(bytesA, bytesB);
            result.Add(new FileDiff(key, false, false, bytesA.Length, bytesB.Length, bytesA, bytesB, regions));
        }
        return result;
    }
}
