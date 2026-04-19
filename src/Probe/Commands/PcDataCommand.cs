using System.Buffers.Binary;
using System.Globalization;
using ArcNET.Core;
using ArcNET.Editor;
using ArcNET.Formats;
using Bia.ValueBuffers;
using Probe;

namespace Probe.Commands;

/// <summary>
/// Mode 17: data.sav / data2.sav dump for reverse-engineering the per-slot global state format.
/// Current targets: GlobalFlags, GlobalVariables, BankMoney, and other save-global state that is
/// not present in mobile.mdy. Town-map fog is tracked separately via .tmf files.
/// Dumps a structured hex/INT32 preview plus decoded player/fog context to aid format RE
/// without reintroducing obvious heap churn.
/// </summary>
internal sealed class PcDataCommand : IProbeCommand
{
    private const string DiffSeparator =
        "----------------------------------------------------------------------------------------------------";
    private const int MaxHexRows = 16;
    private const int MaxPreviewInts = 64;
    private const int MaxQuadPreviewRows = 6;
    private const int FirstNonZeroEntries = 20;
    private const int LastNonZeroEntries = 10;
    private const int MaxAsciiPreviewStrings = 10;
    private const int MaxChangedIntSamples = 6;
    private const int MaxHotIndices = 12;
    private const int MaxWindowValuePreview = 8;
    private const int MaxWindowPatterns = 8;
    private const int MaxWindowTraceSpecs = 8;
    private const int MaxWindowInts = 16;
    private const int MinWindowSuffixInts = 16;
    private const int MaxSaveIdPairPreview = 16;
    private const int MaxSaveIdPairDeltaPreview = 10;
    private const int MaxData2RegionPreviewInts = 8;
    private const int QuadHeaderInts = 2;
    private const int QuadWidthInts = 4;
    private const int MaxFrontMatterRunPreview = 8;
    private const int MaxQuadSignaturePreview = 6;
    private const int MaxQuadRunPreview = 6;
    private const int MaxQuadOrderedRunPreview = 6;
    private const int MaxFrontMatterFamilyPreview = 12;
    private const int MaxData2RegionFamilyPreview = 12;

    private static void PrintTailCompactSummary(in AlignedQuadSummary summary)
    {
        if (summary.TailSectionCount == 0)
            return;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      tail16: start=");
        sb.Append(summary.TailRowStart);
        sb.Append(" rows=");
        sb.Append(summary.TailRowCount);
        sb.Append(" sects=");
        sb.Append(summary.TailSectionCount);
        sb.Append(" seq=");
        AppendFrontMatterSequence(ref sb, summary.TailRuns, MaxFrontMatterRunPreview);
        Console.WriteLine(sb.ToString());
    }

    private static readonly string[] s_savFileNames = ["data.sav", "data2.sav"];

    private readonly record struct SavFileSnapshot(
        byte[] Bytes,
        int Header0,
        int Header1,
        int TotalInts,
        int TrailingBytes,
        int NonZeroCount,
        int BeefCafeCount,
        int MinusOneCount,
        SaveIdPairTableSnapshot? SaveIdPairs,
        AlignedQuadSummary? QuadSummary,
        Data2SavFile? Data2Sav
    );

    private readonly record struct AlignedQuadSignature(int B, int C, int D);

    private readonly record struct AlignedQuadSummary(
        int StartInt,
        int QuadCount,
        int RemainderInts,
        int DistinctSignatures,
        int SectionCount,
        int ZeroSectionCount,
        int LongestZeroSectionStart,
        int LongestZeroSectionLength,
        int FrontMatterRowCount,
        int FrontMatterSectionCount,
        IReadOnlyList<AlignedQuadRunSummary> FrontMatterRuns,
        int TailRowStart,
        int TailRowCount,
        int TailSectionCount,
        IReadOnlyList<AlignedQuadRunSummary> TailRuns,
        IReadOnlyList<AlignedQuadRunSummary> LeadingRuns,
        IReadOnlyList<AlignedQuadRunSummary> TrailingRuns,
        IReadOnlyList<AlignedQuadSignatureSummary> TopSignatures,
        IReadOnlyList<AlignedQuadRunSummary> TopRuns
    );

    private readonly record struct AlignedQuadSignatureSummary(
        AlignedQuadSignature Signature,
        int Count,
        int FirstRow,
        int LastRow,
        int FirstA,
        int LastA,
        int LongestRunLength,
        int LongestRunStart,
        int LongestRunFirstA,
        int LongestRunLastA
    );

    private readonly record struct AlignedQuadRunSummary(
        int StartRow,
        int Length,
        AlignedQuadSignature Signature,
        int FirstA,
        int LastA
    );

    private readonly record struct PlayerStateSnapshot(
        int QuestCount,
        int RumorsCount,
        int Blessings,
        int Curses,
        int Schematics,
        IReadOnlyDictionary<int, int>? Reputation
    );

    private readonly record struct ContiguousIntWindow(
        int StartInt,
        int RemovedInts,
        int AddedInts,
        int CommonSuffixInts
    );

    private readonly record struct WindowPattern(int StartInt, int RemovedInts, int AddedInts);

    private readonly record struct WindowTraceSpec(int StartInt, int Width);

    private readonly record struct FrontMatterFamilyKey(int RowCount, int SectionCount, string Sequence);

    private readonly record struct TailFamilyKey(int RowCount, int SectionCount, string Sequence);

    private readonly record struct Data2RegionFamilyKey(int IntCount, string Sequence, string Preview);

    private readonly record struct SaveIdPairTableSnapshot(
        int StartInt,
        int PairCount,
        int EndInt,
        int FirstId,
        int LastId,
        int NonZeroPairs,
        int MaxValue,
        IReadOnlyDictionary<int, int> Values
    );

    private readonly record struct TownMapFogFileSnapshot(byte[] Bytes, int RevealedTiles);

    private readonly record struct TownMapFogSnapshot(
        int FileCount,
        int RevealedTiles,
        IReadOnlyDictionary<string, TownMapFogFileSnapshot> Files
    );

    private readonly record struct SlotSnapshot(
        int Slot,
        string SlotStem,
        string LeaderName,
        int LeaderLevel,
        IReadOnlyDictionary<string, SavFileSnapshot> Files,
        PlayerStateSnapshot? Player,
        TownMapFogSnapshot TownMapFogs
    );

    private sealed class AlignedQuadSignatureAccumulator
    {
        public AlignedQuadSignatureAccumulator(AlignedQuadSignature signature, int row, int a)
        {
            Signature = signature;
            Count = 1;
            FirstRow = row;
            LastRow = row;
            FirstA = a;
            LastA = a;
            LongestRunLength = 0;
            LongestRunStart = row;
            LongestRunFirstA = a;
            LongestRunLastA = a;
        }

        public AlignedQuadSignature Signature { get; }

        public int Count { get; private set; }

        public int FirstRow { get; }

        public int LastRow { get; private set; }

        public int FirstA { get; }

        public int LastA { get; private set; }

        public int LongestRunLength { get; private set; }

        public int LongestRunStart { get; private set; }

        public int LongestRunFirstA { get; private set; }

        public int LongestRunLastA { get; private set; }

        public void AddRow(int row, int a)
        {
            Count++;
            LastRow = row;
            LastA = a;
        }

        public void RecordRun(int startRow, int length, int firstA, int lastA)
        {
            if (length <= LongestRunLength)
                return;

            LongestRunLength = length;
            LongestRunStart = startRow;
            LongestRunFirstA = firstA;
            LongestRunLastA = lastA;
        }

        public AlignedQuadSignatureSummary ToSummary() =>
            new(
                Signature,
                Count,
                FirstRow,
                LastRow,
                FirstA,
                LastA,
                LongestRunLength,
                LongestRunStart,
                LongestRunFirstA,
                LongestRunLastA
            );
    }

    public Task RunAsync(string saveDir, string[] args)
    {
        var firstSlot = 13;
        var lastSlot = 13;
        if (args.Length >= 1)
        {
            _ = int.TryParse(args[0], out firstSlot);
            lastSlot = firstSlot;
        }
        if (args.Length >= 2)
            _ = int.TryParse(args[1], out lastSlot);
        if (firstSlot > lastSlot)
            (firstSlot, lastSlot) = (lastSlot, firstSlot);

        if (firstSlot == lastSlot)
            return RunSingleAsync(saveDir, firstSlot);

        return RunRangeAsync(saveDir, firstSlot, lastSlot);
    }

    private static Task RunSingleAsync(string saveDir, int slot)
    {
        var slot4 = slot.ToString("D4");
        var ctx = SharedProbeContext.Load(saveDir, slot4);
        var player = AnalyzePlayer(ctx.Save);
        var townMapFogs = AnalyzeTownMapFogs(ctx.Save);

        Console.WriteLine($"\n=== Mode 17: data.sav RE dump - {ctx.SlotStem} ===");
        PrintTypedStateSummary(player, in townMapFogs, "  typed: ");
        Console.WriteLine();

        foreach (var savFileName in s_savFileNames)
        {
            if (!ctx.Save.Files.TryGetValue(savFileName, out var bytes))
            {
                Console.WriteLine($"  {savFileName}: NOT FOUND");
                continue;
            }

            ctx.Save.DataSavFiles.TryGetValue(savFileName, out var dataSav);
            ctx.Save.Data2SavFiles.TryGetValue(savFileName, out var data2Sav);
            var file = Analyze(savFileName, bytes, dataSav, data2Sav);

            Console.WriteLine($"  ---- {savFileName} ({bytes.Length} bytes) ----");
            DumpSav(in file);
            Console.WriteLine();
        }

        return Task.CompletedTask;
    }

    private static Task RunRangeAsync(string saveDir, int firstSlot, int lastSlot)
    {
        Console.WriteLine($"\n=== Mode 17: data.sav/data2.sav range diff - slots {firstSlot:D4}-{lastSlot:D4} ===");

        var snapshots = new List<SlotSnapshot>();
        for (var slot = firstSlot; slot <= lastSlot; slot++)
        {
            var snapshot = TryLoadSnapshot(saveDir, slot);
            if (snapshot is null)
                continue;

            snapshots.Add(snapshot.Value);
            PrintSlotSummary(snapshot.Value);
        }

        if (snapshots.Count < 2)
        {
            Console.WriteLine("\n  Need at least 2 valid slots with save-global files to diff.");
            return Task.CompletedTask;
        }

        PrintFrontMatterFamilySummary(snapshots);
        PrintTailFamilySummary(snapshots);
        PrintData2UnresolvedFamilySummary(snapshots);

        var hotIndices = new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);
        var windowPatterns = new Dictionary<string, Dictionary<WindowPattern, int>>(StringComparer.OrdinalIgnoreCase);
        var windowTraces = new Dictionary<string, Dictionary<WindowTraceSpec, int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in s_savFileNames)
        {
            hotIndices[fileName] = new Dictionary<int, int>();
            windowPatterns[fileName] = new Dictionary<WindowPattern, int>();
            windowTraces[fileName] = new Dictionary<WindowTraceSpec, int>();
        }

        Console.WriteLine("\n  Consecutive diffs");
        Console.WriteLine(DiffSeparator);
        for (var index = 1; index < snapshots.Count; index++)
        {
            var previous = snapshots[index - 1];
            var current = snapshots[index];

            Console.WriteLine(
                $"  [{previous.SlotStem}->{current.SlotStem}] {previous.LeaderName} lv={previous.LeaderLevel} -> {current.LeaderName} lv={current.LeaderLevel}"
            );

            foreach (var fileName in s_savFileNames)
            {
                var hasPrevious = previous.Files.TryGetValue(fileName, out var before);
                var hasCurrent = current.Files.TryGetValue(fileName, out var after);

                if (!hasPrevious && !hasCurrent)
                    continue;

                if (!hasPrevious)
                {
                    Console.WriteLine(
                        $"    {fileName}: NEW bytes={after.Bytes.Length} hdr={after.Header0}/{after.Header1}"
                    );
                    continue;
                }

                if (!hasCurrent)
                {
                    Console.WriteLine(
                        $"    {fileName}: GONE bytes={before.Bytes.Length} hdr={before.Header0}/{before.Header1}"
                    );
                    continue;
                }

                PrintDiffLine(
                    fileName,
                    in before,
                    in after,
                    hotIndices[fileName],
                    windowPatterns[fileName],
                    windowTraces[fileName]
                );
            }

            PrintTypedContextLine(in previous, in current);

            Console.WriteLine();
        }

        Console.WriteLine("  Frequently changing INT32 indices");
        foreach (var fileName in s_savFileNames)
            PrintHotIndexSummary(fileName, hotIndices[fileName]);

        Console.WriteLine("  Contiguous INT32 window patterns");
        foreach (var fileName in s_savFileNames)
            PrintWindowPatternSummary(fileName, windowPatterns[fileName]);

        Console.WriteLine("  Localized window traces");
        foreach (var fileName in s_savFileNames)
            PrintWindowTraceSummary(fileName, snapshots, windowTraces[fileName]);

        return Task.CompletedTask;
    }

    private static SlotSnapshot? TryLoadSnapshot(string saveDir, int slot)
    {
        var slotStem = $"Slot{slot:D4}";
        var gsiFiles = Directory.GetFiles(saveDir, slotStem + "*.gsi");
        var tfaiPath = Path.Combine(saveDir, slotStem + ".tfai");
        var tfafPath = Path.Combine(saveDir, slotStem + ".tfaf");
        if (gsiFiles.Length == 0 || !File.Exists(tfaiPath) || !File.Exists(tfafPath))
            return null;

        LoadedSave save;
        try
        {
            save = SaveGameLoader.Load(gsiFiles[0], tfaiPath, tfafPath);
        }
        catch
        {
            Console.Error.WriteLine($"  [{slotStem}] load failed - skipped");
            return null;
        }

        var files = new Dictionary<string, SavFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in s_savFileNames)
        {
            if (save.Files.TryGetValue(fileName, out var bytes))
            {
                save.DataSavFiles.TryGetValue(fileName, out var dataSav);
                save.Data2SavFiles.TryGetValue(fileName, out var data2Sav);
                files[fileName] = Analyze(fileName, bytes, dataSav, data2Sav);
            }
        }

        return new SlotSnapshot(
            slot,
            slotStem,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            files,
            AnalyzePlayer(save),
            AnalyzeTownMapFogs(save)
        );
    }

    private static SavFileSnapshot Analyze(string fileName, byte[] bytes, DataSavFile? dataSav, Data2SavFile? data2Sav)
    {
        var rawBytes = dataSav?.RawBytes ?? bytes;
        var totalInts = rawBytes.Length / 4;
        var nonZeroCount = 0;
        for (var index = 0; index < totalInts; index++)
        {
            if (ReadInt32(rawBytes, index) != 0)
                nonZeroCount++;
        }

        return new SavFileSnapshot(
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

    private static void PrintSlotSummary(in SlotSnapshot snapshot)
    {
        Console.WriteLine($"\n  [{snapshot.SlotStem}] {snapshot.LeaderName} lv={snapshot.LeaderLevel}");
        Span<char> initial = stackalloc char[256];

        foreach (var fileName in s_savFileNames)
        {
            if (!snapshot.Files.TryGetValue(fileName, out var file))
            {
                Console.WriteLine($"    {fileName}: missing");
                continue;
            }

            var density = file.TotalInts == 0 ? 0.0 : file.NonZeroCount * 100.0 / file.TotalInts;
            var sb = new ValueStringBuilder(initial);
            sb.Append("    ");
            sb.Append(fileName);
            sb.Append(": bytes=");
            sb.Append(file.Bytes.Length);
            sb.Append(" hdr=");
            sb.Append(file.Header0);
            sb.Append('/');
            sb.Append(file.Header1);
            sb.Append(" ints=");
            sb.Append(file.TotalInts);
            sb.Append('+');
            sb.Append(file.TrailingBytes);
            sb.Append("b nz=");
            sb.Append(file.NonZeroCount);
            sb.Append(" (");
            AppendFixed1(ref sb, density);
            sb.Append("%) BEEFCAFE=");
            sb.Append(file.BeefCafeCount);
            sb.Append(" FFFFFFFF=");
            sb.Append(file.MinusOneCount);
            Console.WriteLine(sb.ToString());

            if (file.SaveIdPairs is { } saveIdPairs)
                PrintSaveIdPairSummary(in file, in saveIdPairs);

            if (file.QuadSummary is { } quadSummary)
            {
                PrintAlignedQuadCompactSummary(in quadSummary);
                PrintFrontMatterCompactSummary(in quadSummary);
                PrintTailCompactSummary(in quadSummary);
            }
        }

        var townMapFogs = snapshot.TownMapFogs;
        PrintTypedStateSummary(snapshot.Player, in townMapFogs, "    typed: ");
    }

    private static void PrintTypedContextLine(in SlotSnapshot before, in SlotSnapshot after)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        var hasContent = false;
        sb.Append("    typed: ");
        AppendTypedDeltaPreview(ref sb, ref hasContent, in before, in after);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintTypedStateSummary(
        PlayerStateSnapshot? player,
        in TownMapFogSnapshot townMapFogs,
        string prefix
    )
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append(prefix);
        if (player is { } state)
        {
            sb.Append("quests=");
            sb.Append(state.QuestCount);
            sb.Append("  rumors=");
            sb.Append(state.RumorsCount);
            sb.Append("  bless=");
            sb.Append(state.Blessings);
            sb.Append("  curse=");
            sb.Append(state.Curses);
            sb.Append("  schem=");
            sb.Append(state.Schematics);
            sb.Append("  rep=");
            if (state.Reputation is null)
                sb.Append("absent");
            else
            {
                sb.Append(state.Reputation.Count);
                sb.Append(" entries");
            }
        }
        else
        {
            sb.Append("player=missing");
        }

        sb.Append("  tmf=");
        sb.Append(townMapFogs.FileCount);
        sb.Append(" files / ");
        sb.Append(townMapFogs.RevealedTiles);
        sb.Append(" tiles");
        Console.WriteLine(sb.ToString());
    }

    private static void AppendTypedDeltaPreview(
        ref ValueStringBuilder sb,
        ref bool hasContent,
        in SlotSnapshot before,
        in SlotSnapshot after
    )
    {
        if (before.Player is { } beforePlayer && after.Player is { } afterPlayer)
        {
            AppendCountDelta(ref sb, ref hasContent, "quests", afterPlayer.QuestCount - beforePlayer.QuestCount);
            AppendCountDelta(ref sb, ref hasContent, "rumors", afterPlayer.RumorsCount - beforePlayer.RumorsCount);
            AppendCountDelta(ref sb, ref hasContent, "bless", afterPlayer.Blessings - beforePlayer.Blessings);
            AppendCountDelta(ref sb, ref hasContent, "curse", afterPlayer.Curses - beforePlayer.Curses);
            AppendCountDelta(ref sb, ref hasContent, "schem", afterPlayer.Schematics - beforePlayer.Schematics);
            AppendReputationDelta(ref sb, ref hasContent, beforePlayer.Reputation, afterPlayer.Reputation);
        }
        else if (before.Player is null && after.Player is not null)
        {
            AppendPart(ref sb, ref hasContent, "player=NEW");
        }
        else if (before.Player is not null && after.Player is null)
        {
            AppendPart(ref sb, ref hasContent, "player=LOST");
        }
        else
        {
            AppendPart(ref sb, ref hasContent, "player=missing");
        }

        var beforeTownMapFogs = before.TownMapFogs;
        var afterTownMapFogs = after.TownMapFogs;
        AppendTownMapFogDelta(ref sb, ref hasContent, in beforeTownMapFogs, in afterTownMapFogs);
    }

    private static void AppendCountDelta(ref ValueStringBuilder sb, ref bool hasContent, string label, int delta)
    {
        if (delta == 0)
            return;

        AppendPartPrefix(ref sb, ref hasContent);
        sb.Append(label);
        sb.Append('=');
        AppendSignedInt(ref sb, delta);
    }

    private static void AppendReputationDelta(
        ref ValueStringBuilder sb,
        ref bool hasContent,
        IReadOnlyDictionary<int, int>? before,
        IReadOnlyDictionary<int, int>? after
    )
    {
        if (before is null && after is null)
        {
            AppendPart(ref sb, ref hasContent, "rep=absent");
            return;
        }

        if (before is null)
        {
            AppendPartPrefix(ref sb, ref hasContent);
            sb.Append("rep=NEW(");
            sb.Append(after!.Count);
            sb.Append(')');
            return;
        }

        if (after is null)
        {
            AppendPart(ref sb, ref hasContent, "rep=LOST");
            return;
        }

        var changedSlots = new List<int>();
        foreach (var (slot, newValue) in after)
        {
            if (!before.TryGetValue(slot, out var oldValue) || oldValue != newValue)
                changedSlots.Add(slot);
        }

        foreach (var slot in before.Keys)
        {
            if (!after.ContainsKey(slot))
                changedSlots.Add(slot);
        }

        changedSlots.Sort();
        AppendPartPrefix(ref sb, ref hasContent);
        sb.Append("repChanged=");
        sb.Append(changedSlots.Count);
        if (changedSlots.Count > 0)
        {
            sb.Append(' ');
            sb.Append(SarUtils.FormatSlotList(changedSlots, 16));
        }
    }

    private static void AppendTownMapFogDelta(
        ref ValueStringBuilder sb,
        ref bool hasContent,
        in TownMapFogSnapshot before,
        in TownMapFogSnapshot after
    )
    {
        var changedFiles = 0;
        foreach (var (path, file) in after.Files)
        {
            if (!before.Files.TryGetValue(path, out var oldFile) || !oldFile.Bytes.AsSpan().SequenceEqual(file.Bytes))
                changedFiles++;
        }

        foreach (var path in before.Files.Keys)
        {
            if (!after.Files.ContainsKey(path))
                changedFiles++;
        }

        AppendPartPrefix(ref sb, ref hasContent);
        sb.Append("tmfChanged=");
        sb.Append(changedFiles);
        sb.Append(" files (");
        AppendSignedInt(ref sb, after.RevealedTiles - before.RevealedTiles);
        sb.Append(" tiles)");
    }

    private static void AppendPart(ref ValueStringBuilder sb, ref bool hasContent, string text)
    {
        AppendPartPrefix(ref sb, ref hasContent);
        sb.Append(text);
    }

    private static void AppendPartPrefix(ref ValueStringBuilder sb, ref bool hasContent)
    {
        if (hasContent)
            sb.Append("  ");
        hasContent = true;
    }

    private static void AppendSignedInt(ref ValueStringBuilder sb, int value)
    {
        if (value >= 0)
            sb.Append('+');
        sb.Append(value);
    }

    private static PlayerStateSnapshot? AnalyzePlayer(LoadedSave save)
    {
        var player = SarUtils.FindPlayerRecord(save);
        if (player is null)
            return null;

        return new PlayerStateSnapshot(
            player.QuestCount,
            player.RumorsCount,
            player.BlessingProtoElementCount,
            player.CurseProtoElementCount,
            player.SchematicsElementCount,
            BuildReputationMap(player)
        );
    }

    private static IReadOnlyDictionary<int, int>? BuildReputationMap(CharacterMdyRecord player)
    {
        var reputation = player.ReputationRaw;
        if (reputation is null)
            return null;

        var slots = player.ReputationFactionSlots;
        var result = new Dictionary<int, int>(reputation.Length);
        for (var index = 0; index < reputation.Length; index++)
        {
            var slot = slots is { Length: var count } && count == reputation.Length ? slots[index] : index;
            result[slot] = reputation[index];
        }

        return result;
    }

    private static TownMapFogSnapshot AnalyzeTownMapFogs(LoadedSave save)
    {
        var files = new Dictionary<string, TownMapFogFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        var revealedTiles = 0;
        foreach (var (path, fog) in save.TownMapFogs)
        {
            files[path] = new TownMapFogFileSnapshot(fog.RawBytes, fog.RevealedTiles);
            revealedTiles += fog.RevealedTiles;
        }

        return new TownMapFogSnapshot(files.Count, revealedTiles, files);
    }

    private static void PrintDiffLine(
        string fileName,
        in SavFileSnapshot before,
        in SavFileSnapshot after,
        Dictionary<int, int> hotIndices,
        Dictionary<WindowPattern, int> windowPatterns,
        Dictionary<WindowTraceSpec, int> windowTraces
    )
    {
        var commonInts = Math.Min(before.TotalInts, after.TotalInts);
        var prefixInts = 0;
        while (prefixInts < commonInts && ReadInt32(before.Bytes, prefixInts) == ReadInt32(after.Bytes, prefixInts))
            prefixInts++;

        Span<int> sampleIndices = stackalloc int[MaxChangedIntSamples];
        Span<int> sampleOldValues = stackalloc int[MaxChangedIntSamples];
        Span<int> sampleNewValues = stackalloc int[MaxChangedIntSamples];
        var sampleCount = 0;
        var changedInts = 0;

        for (var index = 0; index < commonInts; index++)
        {
            var oldValue = ReadInt32(before.Bytes, index);
            var newValue = ReadInt32(after.Bytes, index);
            if (oldValue == newValue)
                continue;

            changedInts++;
            hotIndices[index] = hotIndices.GetValueOrDefault(index) + 1;

            if (sampleCount >= MaxChangedIntSamples)
                continue;

            sampleIndices[sampleCount] = index;
            sampleOldValues[sampleCount] = oldValue;
            sampleNewValues[sampleCount] = newValue;
            sampleCount++;
        }

        var commonBytes = Math.Min(before.Bytes.Length, after.Bytes.Length);
        var changedTailBytes = 0;
        for (var index = commonInts * 4; index < commonBytes; index++)
        {
            if (before.Bytes[index] != after.Bytes[index])
                changedTailBytes++;
        }

        if (
            before.Bytes.Length == after.Bytes.Length
            && changedInts == 0
            && changedTailBytes == 0
            && before.TrailingBytes == after.TrailingBytes
        )
        {
            Console.WriteLine($"    {fileName}: IDENTICAL");
            return;
        }

        var addedInts = Math.Max(0, after.TotalInts - before.TotalInts);
        var removedInts = Math.Max(0, before.TotalInts - after.TotalInts);
        var window = TryDetectContiguousIntWindow(in before, in after, prefixInts);
        if (window is { } detectedWindow)
        {
            windowPatterns[
                new WindowPattern(detectedWindow.StartInt, detectedWindow.RemovedInts, detectedWindow.AddedInts)
            ] =
                windowPatterns.GetValueOrDefault(
                    new WindowPattern(detectedWindow.StartInt, detectedWindow.RemovedInts, detectedWindow.AddedInts)
                ) + 1;

            if (detectedWindow.RemovedInts == detectedWindow.AddedInts && detectedWindow.RemovedInts > 0)
            {
                var trace = new WindowTraceSpec(detectedWindow.StartInt, detectedWindow.RemovedInts);
                windowTraces[trace] = windowTraces.GetValueOrDefault(trace) + 1;
            }
        }

        var nzDelta = after.NonZeroCount - before.NonZeroCount;
        var bytesDelta = after.Bytes.Length - before.Bytes.Length;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    ");
        sb.Append(fileName);
        sb.Append(": bytes=");
        sb.Append(before.Bytes.Length);
        sb.Append("->");
        sb.Append(after.Bytes.Length);
        sb.Append(" (");
        sb.Append(bytesDelta >= 0 ? "+" : string.Empty);
        sb.Append(bytesDelta);
        sb.Append(") hdr=");
        sb.Append(before.Header0);
        sb.Append('/');
        sb.Append(before.Header1);
        sb.Append("->");
        sb.Append(after.Header0);
        sb.Append('/');
        sb.Append(after.Header1);
        sb.Append(" nz=");
        sb.Append(before.NonZeroCount);
        sb.Append("->");
        sb.Append(after.NonZeroCount);
        sb.Append(" (");
        sb.Append(nzDelta >= 0 ? "+" : string.Empty);
        sb.Append(nzDelta);
        sb.Append(") changedInts=");
        sb.Append(changedInts);
        sb.Append(" prefixInts=");
        sb.Append(prefixInts);
        if (addedInts > 0)
        {
            sb.Append(" addedInts=");
            sb.Append(addedInts);
        }

        if (removedInts > 0)
        {
            sb.Append(" removedInts=");
            sb.Append(removedInts);
        }

        if (changedTailBytes > 0 || before.TrailingBytes != after.TrailingBytes)
        {
            sb.Append(" tail=");
            sb.Append(before.TrailingBytes);
            sb.Append("b->");
            sb.Append(after.TrailingBytes);
            sb.Append("b changed=");
            sb.Append(changedTailBytes);
        }

        if (sampleCount > 0)
        {
            sb.Append("  ");
            AppendChangedPreview(ref sb, sampleIndices, sampleOldValues, sampleNewValues, sampleCount, changedInts);
        }

        Console.WriteLine(sb.ToString());

        if (window is { } contiguousWindow)
            PrintWindowLine(in contiguousWindow, before.Bytes, after.Bytes);

        if (before.QuadSummary is { } beforeQuad && after.QuadSummary is { } afterQuad)
        {
            PrintAlignedQuadDelta(in beforeQuad, in afterQuad);
            PrintFrontMatterDelta(in beforeQuad, in afterQuad);
            PrintTailDelta(in beforeQuad, in afterQuad);
        }

        if (before.SaveIdPairs is { } beforePairs && after.SaveIdPairs is { } afterPairs)
            PrintSaveIdPairDelta(in before, in beforePairs, in after, in afterPairs);
        else if (before.SaveIdPairs is { } removedPairs)
            PrintSaveIdPairPresence("LOST", in removedPairs);
        else if (after.SaveIdPairs is { } addedPairs)
            PrintSaveIdPairPresence("NEW", in addedPairs);
    }

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

    private static void PrintSaveIdPairSummary(in SavFileSnapshot file, in SaveIdPairTableSnapshot saveIdPairs)
    {
        var prefixIntCount = file.Data2Sav?.PrefixIntCount ?? GetData2PrefixIntCount(in saveIdPairs);
        var suffixIntCount = file.Data2Sav?.SuffixIntCount ?? GetData2SuffixIntCount(file.TotalInts, in saveIdPairs);
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      idPairs50000: ints=");
        sb.Append(saveIdPairs.StartInt);
        sb.Append("..");
        sb.Append(saveIdPairs.EndInt);
        sb.Append(" pairs=");
        sb.Append(saveIdPairs.PairCount);
        sb.Append(" ids=");
        sb.Append(saveIdPairs.FirstId);
        sb.Append("..");
        sb.Append(saveIdPairs.LastId);
        sb.Append(" nonZero=");
        sb.Append(saveIdPairs.NonZeroPairs);
        sb.Append(" max=");
        sb.Append(saveIdPairs.MaxValue);
        sb.Append(" prefixInts=");
        sb.Append(prefixIntCount);
        sb.Append(" suffixInts=");
        sb.Append(suffixIntCount);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintSaveIdPairPresence(string label, in SaveIdPairTableSnapshot saveIdPairs)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      idPairs50000: ");
        sb.Append(label);
        sb.Append(" ints=");
        sb.Append(saveIdPairs.StartInt);
        sb.Append("..");
        sb.Append(saveIdPairs.EndInt);
        sb.Append(" pairs=");
        sb.Append(saveIdPairs.PairCount);
        sb.Append(" nonZero=");
        sb.Append(saveIdPairs.NonZeroPairs);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintSaveIdPairDelta(
        in SavFileSnapshot beforeFile,
        in SaveIdPairTableSnapshot before,
        in SavFileSnapshot afterFile,
        in SaveIdPairTableSnapshot after
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

        var beforePrefixIntCount = GetData2PrefixIntCount(in before);
        var afterPrefixIntCount = GetData2PrefixIntCount(in after);
        var beforeSuffixIntCount = GetData2SuffixIntCount(beforeFile.TotalInts, in before);
        var afterSuffixIntCount = GetData2SuffixIntCount(afterFile.TotalInts, in after);
        var prefixChanged = CountChangedIntRegion(
            beforeFile.Bytes,
            0,
            beforePrefixIntCount,
            afterFile.Bytes,
            0,
            afterPrefixIntCount
        );
        var suffixChanged = CountChangedIntRegion(
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
            return;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      idPairs50000: start=");
        sb.Append(before.StartInt);
        sb.Append("->");
        sb.Append(after.StartInt);
        sb.Append(" pairs=");
        sb.Append(before.PairCount);
        sb.Append("->");
        sb.Append(after.PairCount);
        sb.Append(" nonZero=");
        sb.Append(before.NonZeroPairs);
        sb.Append("->");
        sb.Append(after.NonZeroPairs);
        sb.Append(" changed=");
        sb.Append(changedIds.Count);

        if (changedIds.Count > 0)
        {
            sb.Append("  ");
            var previewed = 0;
            foreach (var id in changedIds)
            {
                if (previewed > 0)
                    sb.Append(", ");

                sb.Append(id);
                sb.Append(':');
                if (before.Values.TryGetValue(id, out var oldValue))
                    sb.Append(oldValue);
                else
                    sb.Append("NA");

                sb.Append("->");
                if (after.Values.TryGetValue(id, out var newValue))
                    sb.Append(newValue);
                else
                    sb.Append("NA");

                previewed++;
                if (previewed >= MaxSaveIdPairDeltaPreview)
                    break;
            }

            if (changedIds.Count > previewed)
            {
                sb.Append(" +");
                sb.Append(changedIds.Count - previewed);
                sb.Append(" more");
            }
        }

        Console.WriteLine(sb.ToString());

        Span<char> regionInitial = stackalloc char[256];
        var region = new ValueStringBuilder(regionInitial);
        region.Append("      unresolved50000: prefix=");
        region.Append(beforePrefixIntCount);
        region.Append("->");
        region.Append(afterPrefixIntCount);
        region.Append(" changed=");
        region.Append(prefixChanged);
        region.Append("  suffix=");
        region.Append(beforeSuffixIntCount);
        region.Append("->");
        region.Append(afterSuffixIntCount);
        region.Append(" changed=");
        region.Append(suffixChanged);
        Console.WriteLine(region.ToString());
    }

    private static int GetData2PrefixIntCount(in SaveIdPairTableSnapshot saveIdPairs) =>
        Math.Max(0, saveIdPairs.StartInt);

    private static int GetData2SuffixIntCount(int totalInts, in SaveIdPairTableSnapshot saveIdPairs) =>
        Math.Max(0, totalInts - saveIdPairs.EndInt - 1);

    private static int CountChangedIntRegion(
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

    private static ContiguousIntWindow? TryDetectContiguousIntWindow(
        in SavFileSnapshot before,
        in SavFileSnapshot after,
        int prefixInts
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

        if (removedInts > MaxWindowInts || addedInts > MaxWindowInts || commonSuffixInts < MinWindowSuffixInts)
            return null;

        return new ContiguousIntWindow(prefixInts, removedInts, addedInts, commonSuffixInts);
    }

    private static void PrintWindowLine(in ContiguousIntWindow window, byte[] beforeBytes, byte[] afterBytes)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      window@");
        sb.Append(window.StartInt);
        sb.Append(": ");
        sb.Append(window.RemovedInts);
        sb.Append("->");
        sb.Append(window.AddedInts);
        sb.Append(" ints");

        if (window.RemovedInts > 0)
        {
            sb.Append(" -");
            AppendIntWindowPreview(ref sb, beforeBytes, window.StartInt, window.RemovedInts);
        }

        if (window.AddedInts > 0)
        {
            sb.Append(" +");
            AppendIntWindowPreview(ref sb, afterBytes, window.StartInt, window.AddedInts);
        }

        sb.Append(" suffixInts=");
        sb.Append(window.CommonSuffixInts);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintAlignedQuadDelta(in AlignedQuadSummary before, in AlignedQuadSummary after)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      quad16: sects=");
        sb.Append(before.SectionCount);
        sb.Append("->");
        sb.Append(after.SectionCount);
        sb.Append(" zeroSects=");
        sb.Append(before.ZeroSectionCount);
        sb.Append("->");
        sb.Append(after.ZeroSectionCount);
        sb.Append(" longZero=@");
        sb.Append(before.LongestZeroSectionStart);
        sb.Append('x');
        sb.Append(before.LongestZeroSectionLength);
        sb.Append("->@");
        sb.Append(after.LongestZeroSectionStart);
        sb.Append('x');
        sb.Append(after.LongestZeroSectionLength);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintFrontMatterDelta(in AlignedQuadSummary before, in AlignedQuadSummary after)
    {
        var beforeRuns = before.FrontMatterRuns;
        var afterRuns = after.FrontMatterRuns;
        var samePrefix = 0;
        var commonCount = Math.Min(beforeRuns.Count, afterRuns.Count);
        while (samePrefix < commonCount && HasSameFrontMatterShape(beforeRuns[samePrefix], afterRuns[samePrefix]))
            samePrefix++;

        if (
            before.FrontMatterRowCount == after.FrontMatterRowCount
            && before.FrontMatterSectionCount == after.FrontMatterSectionCount
            && samePrefix == beforeRuns.Count
            && samePrefix == afterRuns.Count
        )
            return;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      front16: rows=");
        sb.Append(before.FrontMatterRowCount);
        sb.Append("->");
        sb.Append(after.FrontMatterRowCount);
        sb.Append(" sects=");
        sb.Append(before.FrontMatterSectionCount);
        sb.Append("->");
        sb.Append(after.FrontMatterSectionCount);
        sb.Append(" samePrefix=");
        sb.Append(samePrefix);

        if (samePrefix < beforeRuns.Count || samePrefix < afterRuns.Count)
        {
            sb.Append(" next=");
            AppendFrontMatterRunOrEnd(ref sb, beforeRuns, samePrefix);
            sb.Append("->");
            AppendFrontMatterRunOrEnd(ref sb, afterRuns, samePrefix);
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintTailDelta(in AlignedQuadSummary before, in AlignedQuadSummary after)
    {
        var beforeRuns = before.TailRuns;
        var afterRuns = after.TailRuns;
        var samePrefix = 0;
        var commonCount = Math.Min(beforeRuns.Count, afterRuns.Count);
        while (samePrefix < commonCount && HasSameFrontMatterShape(beforeRuns[samePrefix], afterRuns[samePrefix]))
            samePrefix++;

        if (
            before.TailRowStart == after.TailRowStart
            && before.TailRowCount == after.TailRowCount
            && before.TailSectionCount == after.TailSectionCount
            && samePrefix == beforeRuns.Count
            && samePrefix == afterRuns.Count
        )
            return;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      tail16: start=");
        sb.Append(before.TailRowStart);
        sb.Append("->");
        sb.Append(after.TailRowStart);
        sb.Append(" rows=");
        sb.Append(before.TailRowCount);
        sb.Append("->");
        sb.Append(after.TailRowCount);
        sb.Append(" sects=");
        sb.Append(before.TailSectionCount);
        sb.Append("->");
        sb.Append(after.TailSectionCount);
        sb.Append(" samePrefix=");
        sb.Append(samePrefix);

        if (samePrefix < beforeRuns.Count || samePrefix < afterRuns.Count)
        {
            sb.Append(" next=");
            AppendFrontMatterRunOrEnd(ref sb, beforeRuns, samePrefix);
            sb.Append("->");
            AppendFrontMatterRunOrEnd(ref sb, afterRuns, samePrefix);
        }

        Console.WriteLine(sb.ToString());
    }

    private static void AppendIntWindowPreview(ref ValueStringBuilder sb, byte[] bytes, int startInt, int count)
    {
        sb.Append('[');
        var previewCount = Math.Min(count, MaxWindowValuePreview);
        for (var index = 0; index < previewCount; index++)
        {
            if (index > 0)
                sb.Append(',');

            sb.Append(ReadInt32(bytes, startInt + index));
        }

        if (count > previewCount)
        {
            sb.Append(",+");
            sb.Append(count - previewCount);
            sb.Append(" more");
        }

        sb.Append(']');
    }

    private static void AppendChangedPreview(
        ref ValueStringBuilder sb,
        ReadOnlySpan<int> indices,
        ReadOnlySpan<int> oldValues,
        ReadOnlySpan<int> newValues,
        int sampleCount,
        int changedCount
    )
    {
        for (var index = 0; index < sampleCount; index++)
        {
            if (index > 0)
                sb.Append(' ');

            sb.Append('[');
            sb.Append(indices[index]);
            sb.Append("]: ");
            sb.Append(oldValues[index]);
            sb.Append("->");
            sb.Append(newValues[index]);
        }

        if (changedCount > sampleCount)
        {
            sb.Append(" +");
            sb.Append(changedCount - sampleCount);
            sb.Append(" more");
        }
    }

    private static void PrintHotIndexSummary(string fileName, Dictionary<int, int> counts)
    {
        if (counts.Count == 0)
        {
            Console.WriteLine($"    {fileName}: no INT32 deltas across the loaded range");
            return;
        }

        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    ");
        sb.Append(fileName);
        sb.Append(": ");

        var written = 0;
        foreach (var (index, hits) in counts.OrderByDescending(static kvp => kvp.Value).ThenBy(static kvp => kvp.Key))
        {
            if (written >= MaxHotIndices)
                break;

            if (written > 0)
                sb.Append(", ");

            sb.Append('[');
            sb.Append(index);
            sb.Append("]x");
            sb.Append(hits);
            written++;
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintFrontMatterFamilySummary(IReadOnlyList<SlotSnapshot> snapshots)
    {
        var families = new Dictionary<FrontMatterFamilyKey, List<int>>();
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

        if (families.Count == 0)
            return;

        var recurringCount = families.Count(static pair => pair.Value.Count > 1);
        var uniqueCount = families.Count - recurringCount;

        Console.WriteLine("\n  Front-matter families");
        Console.WriteLine(
            $"    data.sav front16 exact families: total={families.Count} recurring={recurringCount} unique={uniqueCount}"
        );

        Span<char> initial = stackalloc char[768];
        var previewed = 0;
        foreach (
            var family in families
                .OrderByDescending(static pair => pair.Value.Count)
                .ThenBy(static pair => pair.Value[0])
        )
        {
            if (previewed >= MaxFrontMatterFamilyPreview)
                break;

            var (key, slots) = family;
            var sb = new ValueStringBuilder(initial);
            sb.Append("      fam");
            sb.Append(previewed + 1);
            sb.Append(": slots=");
            AppendSlotList(ref sb, slots);
            sb.Append(" count=");
            sb.Append(slots.Count);
            sb.Append(" rows=");
            sb.Append(key.RowCount);
            sb.Append(" sects=");
            sb.Append(key.SectionCount);
            sb.Append(" seq=");
            sb.Append(key.Sequence);
            Console.WriteLine(sb.ToString());
            previewed++;
        }

        if (families.Count > previewed)
            Console.WriteLine($"      ... {families.Count - previewed} more families omitted");
    }

    private static void PrintTailFamilySummary(IReadOnlyList<SlotSnapshot> snapshots)
    {
        var families = new Dictionary<TailFamilyKey, List<int>>();
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

        if (families.Count == 0)
            return;

        var recurringCount = families.Count(static pair => pair.Value.Count > 1);
        var uniqueCount = families.Count - recurringCount;

        Console.WriteLine("\n  Tail families");
        Console.WriteLine(
            $"    data.sav tail16 exact families: total={families.Count} recurring={recurringCount} unique={uniqueCount}"
        );

        Span<char> initial = stackalloc char[768];
        var previewed = 0;
        foreach (
            var family in families
                .OrderByDescending(static pair => pair.Value.Count)
                .ThenBy(static pair => pair.Value[0])
        )
        {
            if (previewed >= MaxFrontMatterFamilyPreview)
                break;

            var (key, slots) = family;
            var sb = new ValueStringBuilder(initial);
            sb.Append("      fam");
            sb.Append(previewed + 1);
            sb.Append(": slots=");
            AppendSlotList(ref sb, slots);
            sb.Append(" count=");
            sb.Append(slots.Count);
            sb.Append(" rows=");
            sb.Append(key.RowCount);
            sb.Append(" sects=");
            sb.Append(key.SectionCount);
            sb.Append(" seq=");
            sb.Append(key.Sequence);
            Console.WriteLine(sb.ToString());
            previewed++;
        }

        if (families.Count > previewed)
            Console.WriteLine($"      ... {families.Count - previewed} more families omitted");
    }

    private static void PrintData2UnresolvedFamilySummary(IReadOnlyList<SlotSnapshot> snapshots)
    {
        var prefixFamilies = BuildData2RegionFamilies(snapshots, isPrefix: true);
        var suffixFamilies = BuildData2RegionFamilies(snapshots, isPrefix: false);
        if (prefixFamilies.Count == 0 && suffixFamilies.Count == 0)
            return;

        Console.WriteLine("\n  data2 unresolved families");
        PrintData2RegionFamilies("prefix", prefixFamilies);
        PrintData2RegionFamilies("suffix", suffixFamilies);
    }

    private static Dictionary<Data2RegionFamilyKey, List<int>> BuildData2RegionFamilies(
        IReadOnlyList<SlotSnapshot> snapshots,
        bool isPrefix
    )
    {
        var families = new Dictionary<Data2RegionFamilyKey, List<int>>();
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

        return families;
    }

    private static bool TryBuildData2RegionFamilyKey(
        in SlotSnapshot snapshot,
        bool isPrefix,
        out Data2RegionFamilyKey key
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

        key = new Data2RegionFamilyKey(count, BuildIntSequenceKey(values), BuildIntPreview(values));
        return true;
    }

    private static void PrintData2RegionFamilies(string label, Dictionary<Data2RegionFamilyKey, List<int>> families)
    {
        if (families.Count == 0)
        {
            Console.WriteLine($"    {label} exact families: none");
            return;
        }

        var recurringCount = families.Count(static pair => pair.Value.Count > 1);
        var uniqueCount = families.Count - recurringCount;
        Console.WriteLine(
            $"    {label} exact families: total={families.Count} recurring={recurringCount} unique={uniqueCount}"
        );

        Span<char> initial = stackalloc char[768];
        var previewed = 0;
        foreach (
            var family in families
                .OrderByDescending(static pair => pair.Value.Count)
                .ThenBy(static pair => pair.Value[0])
        )
        {
            if (previewed >= MaxData2RegionFamilyPreview)
                break;

            var (key, slots) = family;
            var sb = new ValueStringBuilder(initial);
            sb.Append("      fam");
            sb.Append(previewed + 1);
            sb.Append(": slots=");
            AppendSlotList(ref sb, slots);
            sb.Append(" count=");
            sb.Append(slots.Count);
            sb.Append(" ints=");
            sb.Append(key.IntCount);
            sb.Append(" seq=");
            sb.Append(key.Preview);
            Console.WriteLine(sb.ToString());
            previewed++;
        }

        if (families.Count > previewed)
            Console.WriteLine($"      ... {families.Count - previewed} more families omitted");
    }

    private static void PrintWindowPatternSummary(string fileName, Dictionary<WindowPattern, int> patterns)
    {
        if (patterns.Count == 0)
        {
            Console.WriteLine($"    {fileName}: no compact insert/remove windows detected");
            return;
        }

        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    ");
        sb.Append(fileName);
        sb.Append(": ");

        var written = 0;
        foreach (
            var (pattern, hits) in patterns
                .OrderByDescending(static kvp => kvp.Value)
                .ThenBy(static kvp => kvp.Key.StartInt)
                .ThenBy(static kvp => kvp.Key.RemovedInts)
                .ThenBy(static kvp => kvp.Key.AddedInts)
        )
        {
            if (written >= MaxWindowPatterns)
                break;

            if (written > 0)
                sb.Append(", ");

            AppendWindowPattern(ref sb, in pattern, hits);
            written++;
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintWindowTraceSummary(
        string fileName,
        IReadOnlyList<SlotSnapshot> snapshots,
        Dictionary<WindowTraceSpec, int> traces
    )
    {
        if (traces.Count == 0)
        {
            Console.WriteLine($"    {fileName}: no same-size localized windows to trace");
            return;
        }

        Console.WriteLine($"    {fileName}:");
        Span<char> headerInitial = stackalloc char[128];
        Span<char> lineInitial = stackalloc char[256];
        var written = 0;
        foreach (
            var (trace, hits) in traces
                .OrderByDescending(static kvp => kvp.Value)
                .ThenBy(static kvp => kvp.Key.StartInt)
                .ThenBy(static kvp => kvp.Key.Width)
        )
        {
            if (hits <= 0)
                continue;

            if (written >= MaxWindowTraceSpecs)
                break;

            var header = new ValueStringBuilder(headerInitial);
            header.Append("      [");
            header.Append(trace.StartInt);
            header.Append("..");
            header.Append(trace.StartInt + trace.Width - 1);
            header.Append("] x");
            header.Append(hits);
            Console.WriteLine(header.ToString());

            foreach (var snapshot in snapshots)
            {
                var line = new ValueStringBuilder(lineInitial);
                line.Append("        ");
                line.Append(snapshot.SlotStem);
                line.Append(": ");

                if (!snapshot.Files.TryGetValue(fileName, out var file))
                {
                    line.Append("(missing)");
                    Console.WriteLine(line.ToString());
                    continue;
                }

                if (trace.StartInt < 0 || trace.StartInt + trace.Width > file.TotalInts)
                {
                    line.Append("(short)");
                    Console.WriteLine(line.ToString());
                    continue;
                }

                AppendIntWindowPreview(ref line, file.Bytes, trace.StartInt, trace.Width);
                Console.WriteLine(line.ToString());
            }

            written++;
        }
    }

    private static void AppendWindowPattern(ref ValueStringBuilder sb, in WindowPattern pattern, int hits)
    {
        if (pattern.RemovedInts == 0)
        {
            sb.Append("ins@");
            sb.Append(pattern.StartInt);
            sb.Append(" +");
            sb.Append(pattern.AddedInts);
        }
        else if (pattern.AddedInts == 0)
        {
            sb.Append("del@");
            sb.Append(pattern.StartInt);
            sb.Append(" -");
            sb.Append(pattern.RemovedInts);
        }
        else
        {
            sb.Append("win@");
            sb.Append(pattern.StartInt);
            sb.Append(' ');
            sb.Append(pattern.RemovedInts);
            sb.Append("->");
            sb.Append(pattern.AddedInts);
        }

        sb.Append(" x");
        sb.Append(hits);
    }

    private static bool TryBuildFrontMatterFamilyKey(in SlotSnapshot snapshot, out FrontMatterFamilyKey key)
    {
        if (!snapshot.Files.TryGetValue("data.sav", out var file) || file.QuadSummary is not { } quadSummary)
        {
            key = default;
            return false;
        }

        var runs = quadSummary.FrontMatterRuns;
        if (quadSummary.FrontMatterSectionCount == 0)
        {
            key = default;
            return false;
        }

        key = new FrontMatterFamilyKey(
            quadSummary.FrontMatterRowCount,
            quadSummary.FrontMatterSectionCount,
            BuildFrontMatterSequenceKey(runs)
        );
        return true;
    }

    private static bool TryBuildTailFamilyKey(in SlotSnapshot snapshot, out TailFamilyKey key)
    {
        if (!snapshot.Files.TryGetValue("data.sav", out var file) || file.QuadSummary is not { } quadSummary)
        {
            key = default;
            return false;
        }

        var runs = quadSummary.TailRuns;
        if (quadSummary.TailSectionCount == 0)
        {
            key = default;
            return false;
        }

        key = new TailFamilyKey(
            quadSummary.TailRowCount,
            quadSummary.TailSectionCount,
            BuildFrontMatterSequenceKey(runs)
        );
        return true;
    }

    private static string BuildFrontMatterSequenceKey(IReadOnlyList<AlignedQuadRunSummary> runs)
    {
        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        AppendFrontMatterSequence(ref sb, runs, runs.Count);
        return sb.ToString();
    }

    private static string BuildIntSequenceKey(ReadOnlySpan<int> values)
    {
        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
                sb.Append(',');

            sb.Append(values[index]);
        }

        return sb.ToString();
    }

    private static string BuildIntPreview(ReadOnlySpan<int> values)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        var headCount = Math.Min(values.Length, MaxData2RegionPreviewInts);
        sb.Append("head=");
        AppendIntSpanPreview(ref sb, values[..headCount]);

        if (values.Length > MaxData2RegionPreviewInts)
        {
            sb.Append(" tail=");
            AppendIntSpanPreview(ref sb, values[^MaxData2RegionPreviewInts..]);
        }

        return sb.ToString();
    }

    private static void AppendSlotList(ref ValueStringBuilder sb, IReadOnlyList<int> slots)
    {
        sb.Append('[');
        Span<char> chars = stackalloc char[4];
        for (var index = 0; index < slots.Count; index++)
        {
            if (index > 0)
                sb.Append(',');

            _ = slots[index].TryFormat(chars, out _, "D4", CultureInfo.InvariantCulture);
            sb.Append(chars);
        }

        sb.Append(']');
    }

    private static void DumpSav(in SavFileSnapshot file)
    {
        var bytes = file.Bytes;
        var totalInts = file.TotalInts;
        var trailingBytes = file.TrailingBytes;
        var saveIdPairs = file.SaveIdPairs;

        Console.WriteLine(
            $"  Header: i0={ReadInt32(bytes, 0)}  i1={ReadInt32(bytes, 1)}  ints={totalInts}  trailingBytes={trailingBytes}"
        );
        Console.WriteLine(
            $"  Sentinels: BEEFCAFE={CountValue(bytes, totalInts, unchecked((int)0xBEEFCAFE))}  FFFFFFFF={CountValue(bytes, totalInts, -1)}"
        );
        Console.WriteLine();

        PrintQuadPreview(bytes, totalInts);
        Console.WriteLine();

        Console.WriteLine("  Hex (first 256 bytes):");
        PrintHexPreview(bytes);

        if (bytes.Length > 256)
            Console.WriteLine($"  ... ({bytes.Length - 256} more bytes)");

        Console.WriteLine();
        PrintIntPreview(bytes, totalInts);

        Console.WriteLine();
        PrintNonZeroSummary(bytes, totalInts);

        if (file.QuadSummary is { } quadSummary)
        {
            Console.WriteLine();
            PrintAlignedQuadSummary(in quadSummary);
        }

        if (saveIdPairs is { } detectedPairs)
        {
            Console.WriteLine();
            PrintSaveIdPairDetails(in file, in detectedPairs);
        }

        Console.WriteLine();
        Console.WriteLine("  ASCII strings (len-prefixed, >= 4 chars):");
        var found = 0;
        for (var i = 0; i + 4 < bytes.Length; i++)
        {
            var strLen = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(i, 4));
            if (strLen is >= 4 and <= 64 && i + 4 + strLen <= bytes.Length)
            {
                var span = bytes.AsSpan(i + 4, strLen);
                var allPrintable = true;
                foreach (var b in span)
                {
                    if (b < 0x20 || b > 0x7E)
                    {
                        allPrintable = false;
                        break;
                    }
                }

                if (allPrintable)
                {
                    PrintAsciiCandidate(i, span);
                    found++;
                    if (found >= MaxAsciiPreviewStrings)
                        break;
                }
            }
        }

        if (found == 0)
            Console.WriteLine("    (none found)");
    }

    private static int ReadInt32(byte[] bytes, int intIndex) =>
        intIndex >= 0 && (intIndex + 1) * 4 <= bytes.Length
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(intIndex * 4, 4))
            : 0;

    private static int CountValue(byte[] bytes, int totalInts, int match)
    {
        var count = 0;
        for (var i = 0; i < totalInts; i++)
        {
            if (ReadInt32(bytes, i) == match)
                count++;
        }

        return count;
    }

    private static void PrintQuadPreview(byte[] bytes, int totalInts)
    {
        var quadCount = Math.Min(MaxQuadPreviewRows, Math.Max(0, (totalInts - 2) / 4));
        if (quadCount == 0)
            return;

        Console.WriteLine("  INT32[4] preview after 8-byte header:");
        Span<char> initial = stackalloc char[192];
        for (var row = 0; row < quadCount; row++)
        {
            var baseIndex = 2 + row * 4;
            var a = ReadInt32(bytes, baseIndex);
            var b = ReadInt32(bytes, baseIndex + 1);
            var c = ReadInt32(bytes, baseIndex + 2);
            var d = ReadInt32(bytes, baseIndex + 3);

            var sb = new ValueStringBuilder(initial);
            sb.Append("    [");
            sb.Append(row);
            sb.Append("] a=0x");
            sb.AppendHex((uint)a, ReadOnlySpan<char>.Empty, 8);
            sb.Append("  b=");
            sb.Append(b);
            sb.Append("  c=");
            sb.Append(c);
            sb.Append("  d=0x");
            sb.AppendHex((uint)d, ReadOnlySpan<char>.Empty, 8);
            Console.WriteLine(sb.ToString());
        }
    }

    private static void PrintHexPreview(byte[] bytes)
    {
        var rowCount = Math.Min(MaxHexRows, (bytes.Length + 15) / 16);
        Span<char> initial = stackalloc char[96];
        for (var row = 0; row < rowCount; row++)
        {
            var offset = row * 16;
            var sb = new ValueStringBuilder(initial);
            sb.Append("  ");
            sb.AppendHex((uint)offset, ReadOnlySpan<char>.Empty, 4);
            sb.Append("  ");

            for (var col = 0; col < 16; col++)
            {
                if (offset + col < bytes.Length)
                    sb.AppendHex(bytes[offset + col]);
                else
                    sb.Append("  ");

                sb.Append(' ');
                if (col == 7)
                    sb.Append(' ');
            }

            sb.Append(' ');
            for (var col = 0; col < 16 && offset + col < bytes.Length; col++)
            {
                var ch = (char)bytes[offset + col];
                sb.Append(ch is >= ' ' and <= '~' ? ch : '.');
            }

            Console.WriteLine(sb.ToString());
        }
    }

    private static void PrintIntPreview(byte[] bytes, int totalInts)
    {
        var intCount = Math.Min(MaxPreviewInts, totalInts);
        Console.WriteLine($"  INT32 values (first {intCount}):");
        Span<char> initial = stackalloc char[256];
        for (var start = 0; start < intCount; start += 8)
        {
            var end = Math.Min(start + 8, intCount);
            var sb = new ValueStringBuilder(initial);
            sb.Append("  [");
            sb.Append(start);
            sb.Append("] ");
            for (var i = start; i < end; i++)
            {
                if (i > start)
                    sb.Append(' ');

                sb.Append(ReadInt32(bytes, i));
            }

            Console.WriteLine(sb.ToString());
        }
    }

    private static void PrintNonZeroSummary(byte[] bytes, int totalInts)
    {
        Span<int> firstIdx = stackalloc int[FirstNonZeroEntries];
        Span<int> firstVals = stackalloc int[FirstNonZeroEntries];
        Span<int> lastIdx = stackalloc int[LastNonZeroEntries];
        Span<int> lastVals = stackalloc int[LastNonZeroEntries];

        var firstCount = 0;
        var lastCount = 0;
        var nonZeroCount = 0;

        for (var i = 0; i < totalInts; i++)
        {
            var val = ReadInt32(bytes, i);
            if (val == 0)
                continue;

            if (firstCount < FirstNonZeroEntries)
            {
                firstIdx[firstCount] = i;
                firstVals[firstCount] = val;
                firstCount++;
            }

            if (lastCount < LastNonZeroEntries)
            {
                lastIdx[lastCount] = i;
                lastVals[lastCount] = val;
                lastCount++;
            }
            else
            {
                var slot = nonZeroCount % LastNonZeroEntries;
                lastIdx[slot] = i;
                lastVals[slot] = val;
            }

            nonZeroCount++;
        }

        var density = totalInts == 0 ? 0.0 : nonZeroCount * 100.0 / totalInts;
        Span<char> initial = stackalloc char[96];
        var sb = new ValueStringBuilder(initial);
        sb.Append("  Non-zero INT32 count: ");
        sb.Append(nonZeroCount);
        sb.Append(" of ");
        sb.Append(totalInts);
        sb.Append(" (");
        AppendFixed1(ref sb, density);
        sb.Append("%)");
        Console.WriteLine(sb.ToString());

        if (nonZeroCount == 0)
            return;

        if (nonZeroCount <= 100)
        {
            Console.WriteLine("  Non-zero INT32 entries:");
            for (var i = 0; i < firstCount; i++)
                PrintIndexedIntEntry(firstIdx[i], firstVals[i]);
            return;
        }

        Console.WriteLine("  (too many non-zero entries to list; file is dense)");
        Console.WriteLine("  First 20 non-zero:");
        for (var i = 0; i < firstCount; i++)
            PrintIndexedIntEntry(firstIdx[i], firstVals[i]);

        Console.WriteLine("  Last 10 non-zero:");
        if (nonZeroCount <= LastNonZeroEntries)
        {
            for (var i = 0; i < lastCount; i++)
                PrintIndexedIntEntry(lastIdx[i], lastVals[i]);
            return;
        }

        var start = nonZeroCount % LastNonZeroEntries;
        for (var i = 0; i < LastNonZeroEntries; i++)
        {
            var slot = (start + i) % LastNonZeroEntries;
            PrintIndexedIntEntry(lastIdx[slot], lastVals[slot]);
        }
    }

    private static void PrintIndexedIntEntry(int idx, int val)
    {
        Span<char> initial = stackalloc char[128];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    [");
        sb.Append(idx);
        sb.Append("] offset=0x");
        sb.AppendHex((uint)(idx * 4), ReadOnlySpan<char>.Empty, 6);
        sb.Append("  val=");
        sb.Append(val);
        sb.Append("  0x");
        sb.AppendHex((uint)val, ReadOnlySpan<char>.Empty, 8);
        Console.WriteLine(sb.ToString());
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

    private static void PrintAlignedQuadCompactSummary(in AlignedQuadSummary summary)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      quad16: rows=");
        sb.Append(summary.QuadCount);
        sb.Append(" remInts=");
        sb.Append(summary.RemainderInts);
        sb.Append(" sigs=");
        sb.Append(summary.DistinctSignatures);
        sb.Append(" sects=");
        sb.Append(summary.SectionCount);
        sb.Append(" zero=@");
        sb.Append(summary.LongestZeroSectionStart);
        sb.Append('x');
        sb.Append(summary.LongestZeroSectionLength);
        if (summary.TopSignatures.Count > 0)
        {
            var top = summary.TopSignatures[0];
            var topSignature = top.Signature;
            sb.Append(" top=");
            AppendAlignedQuadSignature(ref sb, in topSignature);
            sb.Append(" x");
            sb.Append(top.Count);
            sb.Append(" run=");
            sb.Append(top.LongestRunLength);
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintFrontMatterCompactSummary(in AlignedQuadSummary summary)
    {
        if (summary.FrontMatterSectionCount == 0)
            return;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      front16: rows=");
        sb.Append(summary.FrontMatterRowCount);
        sb.Append(" sects=");
        sb.Append(summary.FrontMatterSectionCount);
        sb.Append(" seq=");
        AppendFrontMatterSequence(ref sb, summary.FrontMatterRuns, MaxFrontMatterRunPreview);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintAlignedQuadSummary(in AlignedQuadSummary summary)
    {
        Console.WriteLine("  Aligned INT32[4] summary after 8-byte header:");
        Console.WriteLine(
            $"    startInt={summary.StartInt}  rows={summary.QuadCount}  remainderInts={summary.RemainderInts}  distinct(b,c,d)={summary.DistinctSignatures}  sections={summary.SectionCount}"
        );
        Console.WriteLine(
            $"    zero sections={summary.ZeroSectionCount}  longestZero=@{summary.LongestZeroSectionStart} x{summary.LongestZeroSectionLength}"
        );

        if (summary.FrontMatterSectionCount > 0)
        {
            Console.WriteLine(
                $"    front matter before longest zero: rows={summary.FrontMatterRowCount}  sections={summary.FrontMatterSectionCount}"
            );
        }

        if (summary.TailSectionCount > 0)
        {
            Console.WriteLine(
                $"    tail after longest zero: start={summary.TailRowStart}  rows={summary.TailRowCount}  sections={summary.TailSectionCount}"
            );
        }

        if (summary.LeadingRuns.Count > 0)
        {
            Console.WriteLine("    leading contiguous sections:");
            foreach (var run in summary.LeadingRuns)
                PrintAlignedQuadRunSummary(in run);
        }

        if (summary.TopSignatures.Count > 0)
        {
            Console.WriteLine("    top signatures by row count:");
            foreach (var signature in summary.TopSignatures)
                PrintAlignedQuadSignatureSummary(in signature);
        }

        if (summary.TopRuns.Count > 0)
        {
            Console.WriteLine("    longest contiguous runs:");
            foreach (var run in summary.TopRuns)
                PrintAlignedQuadRunSummary(in run);
        }

        if (summary.TrailingRuns.Count > 0 && !ReferenceEquals(summary.LeadingRuns, summary.TrailingRuns))
        {
            Console.WriteLine("    trailing contiguous sections:");
            foreach (var run in summary.TrailingRuns)
                PrintAlignedQuadRunSummary(in run);
        }
    }

    private static void PrintAlignedQuadSignatureSummary(in AlignedQuadSignatureSummary summary)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      ");
        var signature = summary.Signature;
        AppendAlignedQuadSignature(ref sb, in signature);
        sb.Append(" x");
        sb.Append(summary.Count);
        sb.Append(" rows=");
        sb.Append(summary.FirstRow);
        sb.Append("..");
        sb.Append(summary.LastRow);
        sb.Append(" longestRun=");
        sb.Append(summary.LongestRunLength);
        sb.Append(" @");
        sb.Append(summary.LongestRunStart);
        sb.Append("  a0=0x");
        sb.AppendHex((uint)summary.FirstA, ReadOnlySpan<char>.Empty, 8);
        sb.Append("  aN=0x");
        sb.AppendHex((uint)summary.LastA, ReadOnlySpan<char>.Empty, 8);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintAlignedQuadRunSummary(in AlignedQuadRunSummary run)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      rows ");
        sb.Append(run.StartRow);
        sb.Append("..");
        sb.Append(run.StartRow + run.Length - 1);
        sb.Append(" len=");
        sb.Append(run.Length);
        sb.Append("  ");
        var signature = run.Signature;
        AppendAlignedQuadSignature(ref sb, in signature);
        sb.Append("  a0=0x");
        sb.AppendHex((uint)run.FirstA, ReadOnlySpan<char>.Empty, 8);
        sb.Append("  aN=0x");
        sb.AppendHex((uint)run.LastA, ReadOnlySpan<char>.Empty, 8);
        Console.WriteLine(sb.ToString());
    }

    private static void AppendAlignedQuadSignature(ref ValueStringBuilder sb, in AlignedQuadSignature signature)
    {
        sb.Append("b=");
        sb.Append(signature.B);
        sb.Append(" c=");
        sb.Append(signature.C);
        sb.Append(" d=0x");
        sb.AppendHex((uint)signature.D, ReadOnlySpan<char>.Empty, 8);
    }

    private static void AppendFrontMatterSequence(
        ref ValueStringBuilder sb,
        IReadOnlyList<AlignedQuadRunSummary> runs,
        int maxRuns
    )
    {
        if (runs.Count == 0)
        {
            sb.Append("(none)");
            return;
        }

        var previewCount = Math.Min(runs.Count, maxRuns);
        for (var index = 0; index < previewCount; index++)
        {
            if (index > 0)
                sb.Append(" | ");

            AppendFrontMatterRun(ref sb, runs[index]);
        }

        if (runs.Count > previewCount)
        {
            sb.Append(" | +");
            sb.Append(runs.Count - previewCount);
            sb.Append(" more");
        }
    }

    private static void AppendFrontMatterRun(ref ValueStringBuilder sb, in AlignedQuadRunSummary run)
    {
        var signature = run.Signature;
        sb.Append(signature.B);
        sb.Append('/');
        sb.Append(signature.C);
        sb.Append('/');
        sb.AppendHex((uint)signature.D, ReadOnlySpan<char>.Empty, 8);
        sb.Append('x');
        sb.Append(run.Length);
    }

    private static void AppendFrontMatterRunOrEnd(
        ref ValueStringBuilder sb,
        IReadOnlyList<AlignedQuadRunSummary> runs,
        int index
    )
    {
        if ((uint)index >= (uint)runs.Count)
        {
            sb.Append("END");
            return;
        }

        AppendFrontMatterRun(ref sb, runs[index]);
    }

    private static bool HasSameFrontMatterShape(in AlignedQuadRunSummary left, in AlignedQuadRunSummary right)
    {
        var leftSignature = left.Signature;
        var rightSignature = right.Signature;
        return left.Length == right.Length
            && leftSignature.B == rightSignature.B
            && leftSignature.C == rightSignature.C
            && leftSignature.D == rightSignature.D;
    }

    private static bool IsZeroAlignedQuadSignature(in AlignedQuadSignature signature) =>
        signature.B == 0 && signature.C == 0 && signature.D == 0;

    private static void PrintSaveIdPairDetails(in SavFileSnapshot file, in SaveIdPairTableSnapshot saveIdPairs)
    {
        var bytes = file.Bytes;
        var prefixIntCount = file.Data2Sav?.PrefixIntCount ?? GetData2PrefixIntCount(in saveIdPairs);
        var suffixIntCount = file.Data2Sav?.SuffixIntCount ?? GetData2SuffixIntCount(file.TotalInts, in saveIdPairs);
        Span<char> summaryInitial = stackalloc char[256];
        var summary = new ValueStringBuilder(summaryInitial);
        summary.Append("  50000+ ID pair table: ints=");
        summary.Append(saveIdPairs.StartInt);
        summary.Append("..");
        summary.Append(saveIdPairs.EndInt);
        summary.Append(" pairs=");
        summary.Append(saveIdPairs.PairCount);
        summary.Append(" ids=");
        summary.Append(saveIdPairs.FirstId);
        summary.Append("..");
        summary.Append(saveIdPairs.LastId);
        summary.Append(" nonZero=");
        summary.Append(saveIdPairs.NonZeroPairs);
        summary.Append(" max=");
        summary.Append(saveIdPairs.MaxValue);
        Console.WriteLine(summary.ToString());

        Span<char> regionInitial = stackalloc char[512];
        var region = new ValueStringBuilder(regionInitial);
        region.Append("    unresolved: prefixInts=");
        region.Append(prefixIntCount);
        if (prefixIntCount > 0)
            AppendData2RegionPreview(ref region, file.Data2Sav, isPrefix: true, bytes, prefixIntCount, 0);

        region.Append("  suffixInts=");
        region.Append(suffixIntCount);
        if (suffixIntCount > 0)
            AppendData2RegionPreview(
                ref region,
                file.Data2Sav,
                isPrefix: false,
                bytes,
                suffixIntCount,
                saveIdPairs.EndInt + 1
            );

        Console.WriteLine(region.ToString());

        Span<char> previewInitial = stackalloc char[512];
        var preview = new ValueStringBuilder(previewInitial);
        preview.Append("    nonZeroPairs: ");
        var previewed = 0;
        for (var index = 0; index < saveIdPairs.PairCount; index++)
        {
            var value = ReadInt32(bytes, saveIdPairs.StartInt + index * 2);
            if (value == 0)
                continue;

            var id = ReadInt32(bytes, saveIdPairs.StartInt + index * 2 + 1);
            if (previewed > 0)
                preview.Append(", ");

            preview.Append(id);
            preview.Append(':');
            preview.Append(value);
            previewed++;
            if (previewed >= MaxSaveIdPairPreview)
                break;
        }

        if (previewed == 0)
        {
            preview.Append("(none)");
        }
        else if (saveIdPairs.NonZeroPairs > previewed)
        {
            preview.Append(" +");
            preview.Append(saveIdPairs.NonZeroPairs - previewed);
            preview.Append(" more");
        }

        Console.WriteLine(preview.ToString());
    }

    private static void AppendData2RegionPreview(
        ref ValueStringBuilder sb,
        Data2SavFile? data2Sav,
        bool isPrefix,
        byte[] bytes,
        int count,
        int fallbackStartInt
    )
    {
        if (data2Sav is null)
        {
            sb.Append(isPrefix ? " head=" : " vals=");
            AppendIntWindowPreview(ref sb, bytes, fallbackStartInt, Math.Min(count, MaxData2RegionPreviewInts));
            if (isPrefix && count > MaxData2RegionPreviewInts)
            {
                sb.Append(" tail=");
                AppendIntWindowPreview(
                    ref sb,
                    bytes,
                    Math.Max(fallbackStartInt, fallbackStartInt + count - MaxData2RegionPreviewInts),
                    Math.Min(count, MaxData2RegionPreviewInts)
                );
            }

            return;
        }

        var previewCount = Math.Min(count, MaxData2RegionPreviewInts);
        var preview = new int[previewCount];
        if (isPrefix)
        {
            sb.Append(" head=");
            data2Sav.CopyPrefixInts(0, preview);
            AppendIntSpanPreview(ref sb, preview);
            if (count > MaxData2RegionPreviewInts)
            {
                var tail = new int[previewCount];
                data2Sav.CopyPrefixInts(count - previewCount, tail);
                sb.Append(" tail=");
                AppendIntSpanPreview(ref sb, tail);
            }
        }
        else
        {
            sb.Append(" vals=");
            data2Sav.CopySuffixInts(0, preview);
            AppendIntSpanPreview(ref sb, preview);
        }
    }

    private static void AppendIntSpanPreview(ref ValueStringBuilder sb, ReadOnlySpan<int> values)
    {
        sb.Append('[');
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
                sb.Append(',');

            sb.Append(values[index]);
        }

        sb.Append(']');
    }

    private static void PrintAsciiCandidate(int offset, ReadOnlySpan<byte> span)
    {
        Span<char> initial = stackalloc char[160];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    offset=0x");
        sb.AppendHex((uint)offset, ReadOnlySpan<char>.Empty, 6);
        sb.Append("  len=");
        sb.Append(span.Length);
        sb.Append("  \"");
        foreach (var value in span)
            sb.Append((char)value);
        sb.Append('"');
        Console.WriteLine(sb.ToString());
    }

    private static void AppendFixed1(ref ValueStringBuilder sb, double value)
    {
        Span<char> chars = stackalloc char[32];
        if (value.TryFormat(chars, out var written, "F1", CultureInfo.InvariantCulture))
            sb.Append(chars[..written]);
        else
            sb.Append(value.ToString("F1", CultureInfo.InvariantCulture));
    }
}
