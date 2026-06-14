using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using static ArcNET.Diagnostics.SaveGlobalInt32Reader;

namespace ArcNET.Diagnostics;

public static class SaveGlobalAnalysisService
{
    private const int QuadHeaderInts = 2;
    private const int QuadWidthInts = 4;
    private const int MaxQuadSignaturePreview = 6;
    private const int MaxQuadRunPreview = 6;
    private const int MaxQuadOrderedRunPreview = 6;

    public static IReadOnlyList<string> KnownFileNames { get; } = ["data.sav", "data2.sav"];

    public static SaveGlobalSlotSnapshot CreateSlotSnapshot(int slot, string slotStem, LoadedSave save)
    {
        var files = new Dictionary<string, SaveGlobalFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in KnownFileNames)
        {
            if (!save.Files.TryGetValue(fileName, out var bytes))
                continue;

            save.DataSavFiles.TryGetValue(fileName, out var dataSav);
            save.Data2SavFiles.TryGetValue(fileName, out var data2Sav);
            files[fileName] = Analyze(fileName, bytes, dataSav, data2Sav);
        }

        var typedContext = SaveTypedContextService.Create(save);
        return new SaveGlobalSlotSnapshot(
            slot,
            slotStem,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            files,
            typedContext.Player,
            typedContext.TownMapFogs
        );
    }

    public static SaveGlobalFileSnapshot Analyze(
        string fileName,
        byte[] bytes,
        DataSavFile? dataSav,
        Data2SavFile? data2Sav
    )
    {
        var rawBytes = dataSav?.RawBytes ?? bytes;
        var totalInts = rawBytes.Length / 4;
        var nonZeroCount = 0;
        for (var index = 0; index < totalInts; index++)
        {
            if (ReadInt32(rawBytes, index) != 0)
                nonZeroCount++;
        }

        return new SaveGlobalFileSnapshot(
            rawBytes,
            dataSav?.Header0 ?? ReadInt32(rawBytes, 0),
            dataSav?.Header1 ?? ReadInt32(rawBytes, 1),
            totalInts,
            rawBytes.Length % 4,
            nonZeroCount,
            CountValue(rawBytes, totalInts, unchecked((int)0xBEEFCAFE)),
            CountValue(rawBytes, totalInts, -1),
            CreateSaveIdPairSnapshot(data2Sav),
            fileName.Equals("data.sav", StringComparison.OrdinalIgnoreCase)
                ? CreateAlignedQuadSummary(dataSav?.RawBytes ?? rawBytes)
                : null,
            data2Sav
        );
    }

    public static int GetData2PrefixIntCount(in SaveIdPairTableSnapshot saveIdPairs) =>
        Math.Max(0, saveIdPairs.StartInt);

    public static int GetData2SuffixIntCount(int totalInts, in SaveIdPairTableSnapshot saveIdPairs) =>
        Math.Max(0, totalInts - saveIdPairs.EndInt - 1);

    private static SaveIdPairTableSnapshot? CreateSaveIdPairSnapshot(Data2SavFile? data2Sav)
    {
        if (data2Sav is null)
            return null;

        var values = new Dictionary<int, int>(data2Sav.IdPairs.Count);
        var nonZeroPairs = 0;
        var maxValue = int.MinValue;
        var firstId = 0;
        var lastId = 0;
        for (var index = 0; index < data2Sav.IdPairs.Count; index++)
        {
            var entry = data2Sav.IdPairs[index];
            values[entry.Id] = entry.Value;
            var value = entry.Value;
            if (value != 0)
                nonZeroPairs++;

            if (value > maxValue)
                maxValue = value;

            if (index == 0)
                firstId = entry.Id;

            lastId = entry.Id;
        }

        return new SaveIdPairTableSnapshot(
            data2Sav.IdPairTableStartInt,
            data2Sav.IdPairs.Count,
            data2Sav.IdPairTableEndInt,
            firstId,
            lastId,
            nonZeroPairs,
            maxValue,
            values
        );
    }

    private static AlignedQuadSummary? CreateAlignedQuadSummary(byte[] bytes)
    {
        var totalInts = bytes.Length / 4;
        var intsAfterHeader = Math.Max(0, totalInts - QuadHeaderInts);
        var quadCount = intsAfterHeader / QuadWidthInts;
        var remainderInts = intsAfterHeader % QuadWidthInts;
        if (quadCount == 0)
            return null;

        var signatures = new Dictionary<AlignedQuadSignature, AlignedQuadSignatureAccumulator>();
        var runs = new List<AlignedQuadRunSummary>();

        var runStart = 0;
        var runFirstA = 0;
        var runLastA = 0;
        var currentSignature = default(AlignedQuadSignature);
        var hasRun = false;

        for (var row = 0; row < quadCount; row++)
        {
            var baseInt = QuadHeaderInts + row * QuadWidthInts;
            var a = ReadInt32(bytes, baseInt);
            var signature = new AlignedQuadSignature(
                ReadInt32(bytes, baseInt + 1),
                ReadInt32(bytes, baseInt + 2),
                ReadInt32(bytes, baseInt + 3)
            );

            if (!signatures.TryGetValue(signature, out var accumulator))
            {
                accumulator = new AlignedQuadSignatureAccumulator(signature, row, a);
                signatures.Add(signature, accumulator);
            }
            else
            {
                accumulator.AddRow(row, a);
            }

            if (!hasRun)
            {
                currentSignature = signature;
                runStart = row;
                runFirstA = a;
                runLastA = a;
                hasRun = true;
                continue;
            }

            if (currentSignature == signature)
            {
                runLastA = a;
                continue;
            }

            FinalizeAlignedQuadRun(
                signatures[currentSignature],
                runs,
                currentSignature,
                runStart,
                row,
                runFirstA,
                runLastA
            );
            currentSignature = signature;
            runStart = row;
            runFirstA = a;
            runLastA = a;
        }

        if (hasRun)
            FinalizeAlignedQuadRun(
                signatures[currentSignature],
                runs,
                currentSignature,
                runStart,
                quadCount,
                runFirstA,
                runLastA
            );

        var zeroSectionCount = 0;
        var longestZeroSectionStart = -1;
        var longestZeroSectionLength = 0;
        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];
            var signature = run.Signature;
            if (!IsZeroAlignedQuadSignature(in signature))
                continue;

            zeroSectionCount++;
            if (run.Length <= longestZeroSectionLength)
                continue;

            longestZeroSectionLength = run.Length;
            longestZeroSectionStart = run.StartRow;
        }

        var frontMatterRowCount = Math.Max(0, longestZeroSectionStart);
        var frontMatterRuns =
            frontMatterRowCount > 0 ? runs.TakeWhile(run => run.StartRow < frontMatterRowCount).ToArray() : [];

        var tailRowStart = longestZeroSectionStart >= 0 ? longestZeroSectionStart + longestZeroSectionLength : -1;
        var tailRowCount = tailRowStart >= 0 ? Math.Max(0, quadCount - tailRowStart) : 0;
        var tailRuns = tailRowCount > 0 ? runs.SkipWhile(run => run.StartRow < tailRowStart).ToArray() : [];

        var leadingRuns = runs.Take(MaxQuadOrderedRunPreview).ToArray();
        var trailingRuns =
            runs.Count <= MaxQuadOrderedRunPreview
                ? leadingRuns
                : runs.Skip(Math.Max(0, runs.Count - MaxQuadOrderedRunPreview)).ToArray();

        var topSignatures = signatures
            .Values.Select(static value => value.ToSummary())
            .OrderByDescending(static summary => summary.Count)
            .ThenByDescending(static summary => summary.LongestRunLength)
            .ThenBy(static summary => summary.FirstRow)
            .Take(MaxQuadSignaturePreview)
            .ToArray();

        var topRuns = runs.OrderByDescending(static run => run.Length)
            .ThenBy(static run => run.StartRow)
            .Take(MaxQuadRunPreview)
            .ToArray();

        return new AlignedQuadSummary(
            QuadHeaderInts,
            quadCount,
            remainderInts,
            signatures.Count,
            runs.Count,
            zeroSectionCount,
            longestZeroSectionStart,
            longestZeroSectionLength,
            frontMatterRowCount,
            frontMatterRuns.Length,
            frontMatterRuns,
            tailRowStart,
            tailRowCount,
            tailRuns.Length,
            tailRuns,
            leadingRuns,
            trailingRuns,
            topSignatures,
            topRuns
        );
    }

    private static void FinalizeAlignedQuadRun(
        AlignedQuadSignatureAccumulator accumulator,
        List<AlignedQuadRunSummary> runs,
        AlignedQuadSignature signature,
        int startRow,
        int endRowExclusive,
        int firstA,
        int lastA
    )
    {
        var length = endRowExclusive - startRow;
        if (length <= 0)
            return;

        accumulator.RecordRun(startRow, length, firstA, lastA);
        runs.Add(new AlignedQuadRunSummary(startRow, length, signature, firstA, lastA));
    }

    private static bool IsZeroAlignedQuadSignature(in AlignedQuadSignature signature) =>
        signature.B == 0 && signature.C == 0 && signature.D == 0;
}
