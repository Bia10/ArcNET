using System.Buffers.Binary;
using ArcNET.Formats;
using static ArcNET.Diagnostics.SaveGlobalInt32Reader;

namespace ArcNET.Diagnostics;

public static class SaveGlobalDumpService
{
    public static SaveGlobalFileDumpSnapshot Create(
        in SaveGlobalFileSnapshot file,
        int maxQuadPreviewRows = 6,
        int maxHexRows = 16,
        int maxPreviewInts = 64,
        int firstNonZeroEntries = 20,
        int lastNonZeroEntries = 10,
        int maxAsciiPreviewStrings = 10,
        int maxSaveIdPairPreview = 16,
        int maxData2RegionPreviewInts = 8
    )
    {
        var bytes = file.Bytes;
        var totalInts = file.TotalInts;
        return new SaveGlobalFileDumpSnapshot(
            file.Header0,
            file.Header1,
            totalInts,
            file.TrailingBytes,
            file.BeefCafeCount,
            file.MinusOneCount,
            CreateQuadPreviewRows(bytes, totalInts, maxQuadPreviewRows),
            CreateHexRows(bytes, maxHexRows),
            Math.Max(0, bytes.Length - Math.Min(maxHexRows, (bytes.Length + 15) / 16) * 16),
            CreateIntRows(bytes, totalInts, maxPreviewInts),
            CreateNonZeroSummary(bytes, totalInts, firstNonZeroEntries, lastNonZeroEntries),
            CreateAsciiCandidates(bytes, maxAsciiPreviewStrings),
            file.SaveIdPairs is { } saveIdPairs
                ? CreateSaveIdPairDetails(file, saveIdPairs, maxSaveIdPairPreview, maxData2RegionPreviewInts)
                : null
        );
    }

    private static IReadOnlyList<SaveGlobalQuadPreviewRowSnapshot> CreateQuadPreviewRows(
        byte[] bytes,
        int totalInts,
        int maxQuadPreviewRows
    )
    {
        var quadCount = Math.Min(maxQuadPreviewRows, Math.Max(0, (totalInts - 2) / 4));
        List<SaveGlobalQuadPreviewRowSnapshot> rows = [];
        for (var row = 0; row < quadCount; row++)
        {
            var baseIndex = 2 + row * 4;
            rows.Add(
                new SaveGlobalQuadPreviewRowSnapshot(
                    row,
                    ReadInt32(bytes, baseIndex),
                    ReadInt32(bytes, baseIndex + 1),
                    ReadInt32(bytes, baseIndex + 2),
                    ReadInt32(bytes, baseIndex + 3)
                )
            );
        }

        return rows;
    }

    private static IReadOnlyList<SaveGlobalHexPreviewRowSnapshot> CreateHexRows(byte[] bytes, int maxHexRows)
    {
        var rowCount = Math.Min(maxHexRows, (bytes.Length + 15) / 16);
        List<SaveGlobalHexPreviewRowSnapshot> rows = [];
        for (var row = 0; row < rowCount; row++)
        {
            var offset = row * 16;
            var length = Math.Min(16, bytes.Length - offset);
            var slice = new byte[length];
            bytes.AsSpan(offset, length).CopyTo(slice);
            rows.Add(new SaveGlobalHexPreviewRowSnapshot(offset, slice));
        }

        return rows;
    }

    private static IReadOnlyList<SaveGlobalIntPreviewRowSnapshot> CreateIntRows(
        byte[] bytes,
        int totalInts,
        int maxPreviewInts
    )
    {
        var intCount = Math.Min(maxPreviewInts, totalInts);
        List<SaveGlobalIntPreviewRowSnapshot> rows = [];
        for (var start = 0; start < intCount; start += 8)
        {
            var end = Math.Min(start + 8, intCount);
            List<int> values = [];
            for (var index = start; index < end; index++)
                values.Add(ReadInt32(bytes, index));
            rows.Add(new SaveGlobalIntPreviewRowSnapshot(start, values));
        }

        return rows;
    }

    private static SaveGlobalNonZeroSummarySnapshot CreateNonZeroSummary(
        byte[] bytes,
        int totalInts,
        int firstNonZeroEntries,
        int lastNonZeroEntries
    )
    {
        List<SaveGlobalIndexedIntSnapshot> entries = [];
        List<SaveGlobalIndexedIntSnapshot> first = [];
        Queue<SaveGlobalIndexedIntSnapshot> last = new(lastNonZeroEntries);
        var count = 0;
        for (var index = 0; index < totalInts; index++)
        {
            var value = ReadInt32(bytes, index);
            if (value == 0)
                continue;

            var entry = new SaveGlobalIndexedIntSnapshot(index, value);
            if (count < 100)
                entries.Add(entry);
            if (first.Count < firstNonZeroEntries)
                first.Add(entry);

            if (last.Count == lastNonZeroEntries)
                _ = last.Dequeue();
            last.Enqueue(entry);
            count++;
        }

        var density = totalInts == 0 ? 0.0 : count * 100.0 / totalInts;
        return count <= 100
            ? new SaveGlobalNonZeroSummarySnapshot(count, totalInts, density, false, entries, [], [])
            : new SaveGlobalNonZeroSummarySnapshot(count, totalInts, density, true, [], first, [.. last]);
    }

    private static IReadOnlyList<SaveGlobalAsciiCandidateSnapshot> CreateAsciiCandidates(
        byte[] bytes,
        int maxAsciiPreviewStrings
    )
    {
        List<SaveGlobalAsciiCandidateSnapshot> results = [];
        for (var index = 0; index + 4 < bytes.Length; index++)
        {
            var length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(index, 4));
            if (length is < 4 or > 64 || index + 4 + length > bytes.Length)
                continue;

            var span = bytes.AsSpan(index + 4, length);
            var printable = true;
            foreach (var value in span)
            {
                if (value < 0x20 || value > 0x7E)
                {
                    printable = false;
                    break;
                }
            }

            if (!printable)
                continue;

            results.Add(
                new SaveGlobalAsciiCandidateSnapshot(
                    index,
                    length,
                    new string(span.ToArray().Select(static b => (char)b).ToArray())
                )
            );
            if (results.Count >= maxAsciiPreviewStrings)
                break;
        }

        return results;
    }

    private static SaveGlobalSaveIdPairDetailsSnapshot CreateSaveIdPairDetails(
        in SaveGlobalFileSnapshot file,
        in SaveIdPairTableSnapshot saveIdPairs,
        int maxSaveIdPairPreview,
        int maxData2RegionPreviewInts
    )
    {
        var bytes = file.Bytes;
        var prefixIntCount =
            file.Data2Sav?.PrefixIntCount ?? SaveGlobalAnalysisService.GetData2PrefixIntCount(in saveIdPairs);
        var suffixIntCount =
            file.Data2Sav?.SuffixIntCount
            ?? SaveGlobalAnalysisService.GetData2SuffixIntCount(file.TotalInts, in saveIdPairs);

        List<SaveGlobalSaveIdPairValueSnapshot> preview = [];
        for (var index = 0; index < saveIdPairs.PairCount; index++)
        {
            var value = ReadInt32(bytes, saveIdPairs.StartInt + index * 2);
            if (value == 0)
                continue;

            preview.Add(
                new SaveGlobalSaveIdPairValueSnapshot(ReadInt32(bytes, saveIdPairs.StartInt + index * 2 + 1), value)
            );

            if (preview.Count >= maxSaveIdPairPreview)
                break;
        }

        return new SaveGlobalSaveIdPairDetailsSnapshot(
            saveIdPairs.StartInt,
            saveIdPairs.EndInt,
            saveIdPairs.PairCount,
            saveIdPairs.FirstId,
            saveIdPairs.LastId,
            saveIdPairs.NonZeroPairs,
            saveIdPairs.MaxValue,
            prefixIntCount > 0
                ? CreateData2RegionPreview(
                    file.Data2Sav,
                    isPrefix: true,
                    bytes,
                    prefixIntCount,
                    0,
                    maxData2RegionPreviewInts
                )
                : null,
            suffixIntCount > 0
                ? CreateData2RegionPreview(
                    file.Data2Sav,
                    isPrefix: false,
                    bytes,
                    suffixIntCount,
                    saveIdPairs.EndInt + 1,
                    maxData2RegionPreviewInts
                )
                : null,
            preview,
            Math.Max(0, saveIdPairs.NonZeroPairs - preview.Count)
        );
    }

    private static SaveGlobalData2RegionPreviewSnapshot CreateData2RegionPreview(
        Data2SavFile? data2Sav,
        bool isPrefix,
        byte[] bytes,
        int count,
        int fallbackStartInt,
        int maxData2RegionPreviewInts
    )
    {
        var previewCount = Math.Min(count, maxData2RegionPreviewInts);
        if (data2Sav is null)
        {
            List<int> head = [];
            for (var index = 0; index < previewCount; index++)
                head.Add(ReadInt32(bytes, fallbackStartInt + index));

            List<int> tail = [];
            if (isPrefix && count > maxData2RegionPreviewInts)
            {
                var tailStart = Math.Max(fallbackStartInt, fallbackStartInt + count - maxData2RegionPreviewInts);
                for (var index = 0; index < previewCount; index++)
                    tail.Add(ReadInt32(bytes, tailStart + index));
            }

            return new SaveGlobalData2RegionPreviewSnapshot(count, head, tail);
        }

        var values = new int[previewCount];
        if (isPrefix)
        {
            data2Sav.CopyPrefixInts(0, values);
            if (count > maxData2RegionPreviewInts)
            {
                var tail = new int[previewCount];
                data2Sav.CopyPrefixInts(count - previewCount, tail);
                return new SaveGlobalData2RegionPreviewSnapshot(count, values, tail);
            }

            return new SaveGlobalData2RegionPreviewSnapshot(count, values, []);
        }

        data2Sav.CopySuffixInts(0, values);
        return new SaveGlobalData2RegionPreviewSnapshot(count, values, []);
    }
}
