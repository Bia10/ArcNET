using System.Globalization;
using ArcNET.Core;
using ArcNET.Diagnostics;
using ArcNET.Editor;
using Bia.ValueBuffers;
using Probe;
using static ArcNET.Diagnostics.SaveGlobalInt32Reader;
using PcDataSlotSnapshot = ArcNET.Diagnostics.SaveGlobalSlotSnapshot;
using SavFileSnapshot = ArcNET.Diagnostics.SaveGlobalFileSnapshot;

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

    public Task RunAsync(string saveDir, string[] args)
    {
        if (!Directory.Exists(saveDir))
        {
            Console.Error.WriteLine($"[probe] Save directory not found: {saveDir}");
            Console.Error.WriteLine("[probe] Pass --save-dir <path> to point Probe at a valid Arcanum save folder.");
            return Task.CompletedTask;
        }

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
        var typedContext = SaveTypedContextService.Create(ctx.Save);
        var townMapFogs = typedContext.TownMapFogs;

        Console.WriteLine($"\n=== Mode 17: data.sav RE dump - {ctx.SlotStem} ===");
        PrintTypedStateSummary(typedContext.Player, in townMapFogs, "  typed: ");
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
            var file = SaveGlobalAnalysisService.Analyze(savFileName, bytes, dataSav, data2Sav);

            Console.WriteLine($"  ---- {savFileName} ({bytes.Length} bytes) ----");
            DumpSav(in file);
            Console.WriteLine();
        }

        return Task.CompletedTask;
    }

    private static Task RunRangeAsync(string saveDir, int firstSlot, int lastSlot)
    {
        Console.WriteLine($"\n=== Mode 17: data.sav/data2.sav range diff - slots {firstSlot:D4}-{lastSlot:D4} ===");

        var snapshots = new List<PcDataSlotSnapshot>();
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

        var rangeAnalysis = SaveGlobalRangeAnalysisService.Analyze(snapshots, s_savFileNames);
        PrintFrontMatterFamilySummary(rangeAnalysis.FrontMatterFamilies);
        PrintTailFamilySummary(rangeAnalysis.TailFamilies);
        PrintData2UnresolvedFamilySummary(rangeAnalysis.PrefixFamilies, rangeAnalysis.SuffixFamilies);

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

                PrintDiffLine(fileName, in before, in after);
            }

            PrintTypedContextLine(in previous, in current);

            Console.WriteLine();
        }

        Console.WriteLine("  Frequently changing INT32 indices");
        foreach (var fileName in s_savFileNames)
            PrintHotIndexSummary(
                fileName,
                rangeAnalysis.HotIndices.TryGetValue(fileName, out var hotCounts)
                    ? hotCounts
                    : Array.Empty<SaveGlobalHotIndexHitSnapshot>()
            );

        Console.WriteLine("  Contiguous INT32 window patterns");
        foreach (var fileName in s_savFileNames)
            PrintWindowPatternSummary(
                fileName,
                rangeAnalysis.WindowPatterns.TryGetValue(fileName, out var patterns)
                    ? patterns
                    : Array.Empty<SaveGlobalWindowPatternHitSnapshot>()
            );

        Console.WriteLine("  Localized window traces");
        foreach (var fileName in s_savFileNames)
            PrintWindowTraceSummary(
                fileName,
                snapshots,
                rangeAnalysis.WindowTraces.TryGetValue(fileName, out var traces)
                    ? traces
                    : Array.Empty<SaveGlobalWindowTraceHitSnapshot>()
            );

        return Task.CompletedTask;
    }

    private static PcDataSlotSnapshot? TryLoadSnapshot(string saveDir, int slot)
    {
        var slotStem = $"Slot{slot:D4}";
        SaveSlotLoadSnapshot loaded;
        try
        {
            loaded = SaveSlotLoadService.Load(saveDir, slot);
        }
        catch
        {
            Console.Error.WriteLine($"  [{slotStem}] load failed - skipped");
            return null;
        }

        return SaveGlobalAnalysisService.CreateSlotSnapshot(slot, loaded.SlotStem, loaded.Save);
    }

    private static void PrintSlotSummary(in PcDataSlotSnapshot snapshot)
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

    private static void PrintTypedContextLine(in PcDataSlotSnapshot before, in PcDataSlotSnapshot after)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        var hasContent = false;
        sb.Append("    typed: ");
        AppendTypedDeltaPreview(ref sb, ref hasContent, in before, in after);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintTypedStateSummary(
        SaveTypedPlayerStateSnapshot? player,
        in SaveTownMapFogSnapshot townMapFogs,
        string prefix
    )
    {
        var overview = SaveTypedContextAnalysisService.CreateOverview(player, townMapFogs);
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append(prefix);
        if (overview.HasPlayer)
        {
            sb.Append("quests=");
            sb.Append(overview.QuestCount);
            sb.Append("  rumors=");
            sb.Append(overview.RumorsCount);
            sb.Append("  bless=");
            sb.Append(overview.Blessings);
            sb.Append("  curse=");
            sb.Append(overview.Curses);
            sb.Append("  schem=");
            sb.Append(overview.Schematics);
            sb.Append("  rep=");
            if (overview.ReputationCount is null)
                sb.Append("absent");
            else
            {
                sb.Append(overview.ReputationCount.Value);
                sb.Append(" entries");
            }
        }
        else
        {
            sb.Append("player=missing");
        }

        sb.Append("  tmf=");
        sb.Append(overview.TownMapFogFileCount);
        sb.Append(" files / ");
        sb.Append(overview.RevealedTiles);
        sb.Append(" tiles");
        Console.WriteLine(sb.ToString());
    }

    private static void AppendTypedDeltaPreview(
        ref ValueStringBuilder sb,
        ref bool hasContent,
        in PcDataSlotSnapshot before,
        in PcDataSlotSnapshot after
    )
    {
        var delta = SaveTypedContextAnalysisService.CreateDelta(
            before.Player,
            before.TownMapFogs,
            after.Player,
            after.TownMapFogs
        );

        switch (delta.Player.Kind)
        {
            case SaveTypedPlayerDeltaKind.Changed:
                AppendCountDelta(ref sb, ref hasContent, "quests", delta.Player.QuestDelta);
                AppendCountDelta(ref sb, ref hasContent, "rumors", delta.Player.RumorsDelta);
                AppendCountDelta(ref sb, ref hasContent, "bless", delta.Player.BlessingsDelta);
                AppendCountDelta(ref sb, ref hasContent, "curse", delta.Player.CursesDelta);
                AppendCountDelta(ref sb, ref hasContent, "schem", delta.Player.SchematicsDelta);
                AppendReputationDelta(ref sb, ref hasContent, delta.Player.Reputation);
                break;
            case SaveTypedPlayerDeltaKind.Added:
                AppendPart(ref sb, ref hasContent, "player=NEW");
                break;
            case SaveTypedPlayerDeltaKind.Removed:
                AppendPart(ref sb, ref hasContent, "player=LOST");
                break;
            default:
                AppendPart(ref sb, ref hasContent, "player=missing");
                break;
        }

        AppendTownMapFogDelta(ref sb, ref hasContent, delta.TownMapFogs);
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
        SaveTypedReputationDeltaSnapshot reputation
    )
    {
        if (reputation.Kind == SaveTypedReputationDeltaKind.Absent)
        {
            AppendPart(ref sb, ref hasContent, "rep=absent");
            return;
        }

        if (reputation.Kind == SaveTypedReputationDeltaKind.Added)
        {
            AppendPartPrefix(ref sb, ref hasContent);
            sb.Append("rep=NEW(");
            sb.Append(reputation.Count);
            sb.Append(')');
            return;
        }

        if (reputation.Kind == SaveTypedReputationDeltaKind.Removed)
        {
            AppendPart(ref sb, ref hasContent, "rep=LOST");
            return;
        }

        AppendPartPrefix(ref sb, ref hasContent);
        sb.Append("repChanged=");
        sb.Append(reputation.Count);
        if (reputation.ChangedSlots.Count > 0)
        {
            sb.Append(' ');
            sb.Append(SarUtils.FormatSlotList(reputation.ChangedSlots, 16));
        }
    }

    private static void AppendTownMapFogDelta(
        ref ValueStringBuilder sb,
        ref bool hasContent,
        SaveTownMapFogDeltaSnapshot townMapFogs
    )
    {
        AppendPartPrefix(ref sb, ref hasContent);
        sb.Append("tmfChanged=");
        sb.Append(townMapFogs.ChangedFiles);
        sb.Append(" files (");
        AppendSignedInt(ref sb, townMapFogs.RevealedTileDelta);
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

    private static void PrintDiffLine(string fileName, in SavFileSnapshot before, in SavFileSnapshot after)
    {
        var diff = SaveGlobalDiffService.Compare(
            in before,
            in after,
            MaxChangedIntSamples,
            MaxWindowInts,
            MinWindowSuffixInts,
            MaxSaveIdPairDeltaPreview
        );

        if (diff.IsIdentical)
        {
            Console.WriteLine($"    {fileName}: IDENTICAL");
            return;
        }

        var nzDelta = diff.AfterNonZeroCount - diff.BeforeNonZeroCount;
        var bytesDelta = diff.AfterByteLength - diff.BeforeByteLength;

        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    ");
        sb.Append(fileName);
        sb.Append(": bytes=");
        sb.Append(diff.BeforeByteLength);
        sb.Append("->");
        sb.Append(diff.AfterByteLength);
        sb.Append(" (");
        sb.Append(bytesDelta >= 0 ? "+" : string.Empty);
        sb.Append(bytesDelta);
        sb.Append(") hdr=");
        sb.Append(diff.BeforeHeader0);
        sb.Append('/');
        sb.Append(diff.BeforeHeader1);
        sb.Append("->");
        sb.Append(diff.AfterHeader0);
        sb.Append('/');
        sb.Append(diff.AfterHeader1);
        sb.Append(" nz=");
        sb.Append(diff.BeforeNonZeroCount);
        sb.Append("->");
        sb.Append(diff.AfterNonZeroCount);
        sb.Append(" (");
        sb.Append(nzDelta >= 0 ? "+" : string.Empty);
        sb.Append(nzDelta);
        sb.Append(") changedInts=");
        sb.Append(diff.ChangedInts);
        sb.Append(" prefixInts=");
        sb.Append(diff.PrefixInts);
        if (diff.AddedInts > 0)
        {
            sb.Append(" addedInts=");
            sb.Append(diff.AddedInts);
        }

        if (diff.RemovedInts > 0)
        {
            sb.Append(" removedInts=");
            sb.Append(diff.RemovedInts);
        }

        if (diff.ChangedTailBytes > 0 || diff.BeforeTrailingBytes != diff.AfterTrailingBytes)
        {
            sb.Append(" tail=");
            sb.Append(diff.BeforeTrailingBytes);
            sb.Append("b->");
            sb.Append(diff.AfterTrailingBytes);
            sb.Append("b changed=");
            sb.Append(diff.ChangedTailBytes);
        }

        if (diff.ChangedSamples.Count > 0)
        {
            sb.Append("  ");
            AppendChangedPreview(ref sb, diff.ChangedSamples, diff.ChangedInts);
        }

        Console.WriteLine(sb.ToString());

        if (diff.Window is { } contiguousWindow)
            PrintWindowLine(in contiguousWindow, before.Bytes, after.Bytes);

        if (diff.AlignedQuad is { } alignedQuad)
        {
            PrintAlignedQuadDelta(in alignedQuad);
            if (alignedQuad.FrontMatter is { } frontMatter)
                PrintFrontMatterDelta(in frontMatter);
            if (alignedQuad.Tail is { } tail)
                PrintTailDelta(in tail);
        }

        if (diff.SaveIdPairs is { } saveIdPairDiff)
            PrintSaveIdPairDelta(in saveIdPairDiff);
        else if (before.SaveIdPairs is { } removedPairs)
            PrintSaveIdPairPresence("LOST", in removedPairs);
        else if (after.SaveIdPairs is { } addedPairs)
            PrintSaveIdPairPresence("NEW", in addedPairs);
    }

    private static void PrintSaveIdPairSummary(in SavFileSnapshot file, in SaveIdPairTableSnapshot saveIdPairs)
    {
        var prefixIntCount =
            file.Data2Sav?.PrefixIntCount ?? SaveGlobalAnalysisService.GetData2PrefixIntCount(in saveIdPairs);
        var suffixIntCount =
            file.Data2Sav?.SuffixIntCount
            ?? SaveGlobalAnalysisService.GetData2SuffixIntCount(file.TotalInts, in saveIdPairs);
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

    private static void PrintSaveIdPairDelta(in SaveGlobalSaveIdPairDiffSnapshot diff)
    {
        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      idPairs50000: start=");
        sb.Append(diff.BeforeStartInt);
        sb.Append("->");
        sb.Append(diff.AfterStartInt);
        sb.Append(" pairs=");
        sb.Append(diff.BeforePairCount);
        sb.Append("->");
        sb.Append(diff.AfterPairCount);
        sb.Append(" nonZero=");
        sb.Append(diff.BeforeNonZeroPairs);
        sb.Append("->");
        sb.Append(diff.AfterNonZeroPairs);
        sb.Append(" changed=");
        sb.Append(diff.TotalChangedPairs);

        if (diff.TotalChangedPairs > 0)
        {
            sb.Append("  ");
            for (var pairIndex = 0; pairIndex < diff.ChangedPairs.Count; pairIndex++)
            {
                if (pairIndex > 0)
                    sb.Append(", ");

                var pair = diff.ChangedPairs[pairIndex];
                sb.Append(pair.Id);
                sb.Append(':');
                if (pair.BeforeValue is { } oldValue)
                    sb.Append(oldValue);
                else
                    sb.Append("NA");

                sb.Append("->");
                if (pair.AfterValue is { } newValue)
                    sb.Append(newValue);
                else
                    sb.Append("NA");
            }

            if (diff.TotalChangedPairs > diff.ChangedPairs.Count)
            {
                sb.Append(" +");
                sb.Append(diff.TotalChangedPairs - diff.ChangedPairs.Count);
                sb.Append(" more");
            }
        }

        Console.WriteLine(sb.ToString());

        Span<char> regionInitial = stackalloc char[256];
        var region = new ValueStringBuilder(regionInitial);
        region.Append("      unresolved50000: prefix=");
        region.Append(diff.BeforePrefixIntCount);
        region.Append("->");
        region.Append(diff.AfterPrefixIntCount);
        region.Append(" changed=");
        region.Append(diff.PrefixChangedInts);
        region.Append("  suffix=");
        region.Append(diff.BeforeSuffixIntCount);
        region.Append("->");
        region.Append(diff.AfterSuffixIntCount);
        region.Append(" changed=");
        region.Append(diff.SuffixChangedInts);
        Console.WriteLine(region.ToString());
    }

    private static void PrintWindowLine(in SaveGlobalContiguousIntWindow window, byte[] beforeBytes, byte[] afterBytes)
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

    private static void PrintAlignedQuadDelta(in SaveGlobalAlignedQuadDiffSnapshot diff)
    {
        Span<char> initial = stackalloc char[256];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      quad16: sects=");
        sb.Append(diff.BeforeSectionCount);
        sb.Append("->");
        sb.Append(diff.AfterSectionCount);
        sb.Append(" zeroSects=");
        sb.Append(diff.BeforeZeroSectionCount);
        sb.Append("->");
        sb.Append(diff.AfterZeroSectionCount);
        sb.Append(" longZero=@");
        sb.Append(diff.BeforeLongestZeroSectionStart);
        sb.Append('x');
        sb.Append(diff.BeforeLongestZeroSectionLength);
        sb.Append("->@");
        sb.Append(diff.AfterLongestZeroSectionStart);
        sb.Append('x');
        sb.Append(diff.AfterLongestZeroSectionLength);
        Console.WriteLine(sb.ToString());
    }

    private static void PrintFrontMatterDelta(in SaveGlobalFrontMatterDiffSnapshot diff)
    {
        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      front16: rows=");
        sb.Append(diff.BeforeRowCount);
        sb.Append("->");
        sb.Append(diff.AfterRowCount);
        sb.Append(" sects=");
        sb.Append(diff.BeforeSectionCount);
        sb.Append("->");
        sb.Append(diff.AfterSectionCount);
        sb.Append(" samePrefix=");
        sb.Append(diff.SamePrefixCount);

        if (diff.BeforeNextRun is not null || diff.AfterNextRun is not null)
        {
            sb.Append(" next=");
            AppendFrontMatterRunOrEnd(ref sb, diff.BeforeNextRun);
            sb.Append("->");
            AppendFrontMatterRunOrEnd(ref sb, diff.AfterNextRun);
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintTailDelta(in SaveGlobalTailDiffSnapshot diff)
    {
        Span<char> initial = stackalloc char[512];
        var sb = new ValueStringBuilder(initial);
        sb.Append("      tail16: start=");
        sb.Append(diff.BeforeStartRow);
        sb.Append("->");
        sb.Append(diff.AfterStartRow);
        sb.Append(" rows=");
        sb.Append(diff.BeforeRowCount);
        sb.Append("->");
        sb.Append(diff.AfterRowCount);
        sb.Append(" sects=");
        sb.Append(diff.BeforeSectionCount);
        sb.Append("->");
        sb.Append(diff.AfterSectionCount);
        sb.Append(" samePrefix=");
        sb.Append(diff.SamePrefixCount);

        if (diff.BeforeNextRun is not null || diff.AfterNextRun is not null)
        {
            sb.Append(" next=");
            AppendFrontMatterRunOrEnd(ref sb, diff.BeforeNextRun);
            sb.Append("->");
            AppendFrontMatterRunOrEnd(ref sb, diff.AfterNextRun);
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
        IReadOnlyList<SaveGlobalChangedIntSampleSnapshot> samples,
        int changedCount
    )
    {
        for (var index = 0; index < samples.Count; index++)
        {
            if (index > 0)
                sb.Append(' ');

            var sample = samples[index];
            sb.Append('[');
            sb.Append(sample.Index);
            sb.Append("]: ");
            sb.Append(sample.BeforeValue);
            sb.Append("->");
            sb.Append(sample.AfterValue);
        }

        if (changedCount > samples.Count)
        {
            sb.Append(" +");
            sb.Append(changedCount - samples.Count);
            sb.Append(" more");
        }
    }

    private static void PrintHotIndexSummary(string fileName, IReadOnlyList<SaveGlobalHotIndexHitSnapshot> counts)
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
        foreach (var hit in counts)
        {
            if (written >= MaxHotIndices)
                break;

            if (written > 0)
                sb.Append(", ");

            sb.Append('[');
            sb.Append(hit.Index);
            sb.Append("]x");
            sb.Append(hit.Hits);
            written++;
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintFrontMatterFamilySummary(IReadOnlyList<SaveGlobalFrontMatterFamilySnapshot> families)
    {
        if (families.Count == 0)
            return;

        var recurringCount = families.Count(static family => family.Slots.Count > 1);
        var uniqueCount = families.Count - recurringCount;

        Console.WriteLine("\n  Front-matter families");
        Console.WriteLine(
            $"    data.sav front16 exact families: total={families.Count} recurring={recurringCount} unique={uniqueCount}"
        );

        Span<char> initial = stackalloc char[768];
        var previewed = 0;
        foreach (var family in families)
        {
            if (previewed >= MaxFrontMatterFamilyPreview)
                break;

            var sb = new ValueStringBuilder(initial);
            sb.Append("      fam");
            sb.Append(previewed + 1);
            sb.Append(": slots=");
            AppendSlotList(ref sb, family.Slots);
            sb.Append(" count=");
            sb.Append(family.Slots.Count);
            sb.Append(" rows=");
            sb.Append(family.RowCount);
            sb.Append(" sects=");
            sb.Append(family.SectionCount);
            sb.Append(" seq=");
            sb.Append(family.Sequence);
            Console.WriteLine(sb.ToString());
            previewed++;
        }

        if (families.Count > previewed)
            Console.WriteLine($"      ... {families.Count - previewed} more families omitted");
    }

    private static void PrintTailFamilySummary(IReadOnlyList<SaveGlobalTailFamilySnapshot> families)
    {
        if (families.Count == 0)
            return;

        var recurringCount = families.Count(static family => family.Slots.Count > 1);
        var uniqueCount = families.Count - recurringCount;

        Console.WriteLine("\n  Tail families");
        Console.WriteLine(
            $"    data.sav tail16 exact families: total={families.Count} recurring={recurringCount} unique={uniqueCount}"
        );

        Span<char> initial = stackalloc char[768];
        var previewed = 0;
        foreach (var family in families)
        {
            if (previewed >= MaxFrontMatterFamilyPreview)
                break;

            var sb = new ValueStringBuilder(initial);
            sb.Append("      fam");
            sb.Append(previewed + 1);
            sb.Append(": slots=");
            AppendSlotList(ref sb, family.Slots);
            sb.Append(" count=");
            sb.Append(family.Slots.Count);
            sb.Append(" rows=");
            sb.Append(family.RowCount);
            sb.Append(" sects=");
            sb.Append(family.SectionCount);
            sb.Append(" seq=");
            sb.Append(family.Sequence);
            Console.WriteLine(sb.ToString());
            previewed++;
        }

        if (families.Count > previewed)
            Console.WriteLine($"      ... {families.Count - previewed} more families omitted");
    }

    private static void PrintData2UnresolvedFamilySummary(
        IReadOnlyList<SaveGlobalData2RegionFamilySnapshot> prefixFamilies,
        IReadOnlyList<SaveGlobalData2RegionFamilySnapshot> suffixFamilies
    )
    {
        if (prefixFamilies.Count == 0 && suffixFamilies.Count == 0)
            return;

        Console.WriteLine("\n  data2 unresolved families");
        PrintData2RegionFamilies("prefix", prefixFamilies);
        PrintData2RegionFamilies("suffix", suffixFamilies);
    }

    private static void PrintData2RegionFamilies(
        string label,
        IReadOnlyList<SaveGlobalData2RegionFamilySnapshot> families
    )
    {
        if (families.Count == 0)
        {
            Console.WriteLine($"    {label} exact families: none");
            return;
        }

        var recurringCount = families.Count(static family => family.Slots.Count > 1);
        var uniqueCount = families.Count - recurringCount;
        Console.WriteLine(
            $"    {label} exact families: total={families.Count} recurring={recurringCount} unique={uniqueCount}"
        );

        Span<char> initial = stackalloc char[768];
        var previewed = 0;
        foreach (var family in families)
        {
            if (previewed >= MaxData2RegionFamilyPreview)
                break;

            var sb = new ValueStringBuilder(initial);
            sb.Append("      fam");
            sb.Append(previewed + 1);
            sb.Append(": slots=");
            AppendSlotList(ref sb, family.Slots);
            sb.Append(" count=");
            sb.Append(family.Slots.Count);
            sb.Append(" ints=");
            sb.Append(family.IntCount);
            sb.Append(" seq=");
            sb.Append(family.Preview);
            Console.WriteLine(sb.ToString());
            previewed++;
        }

        if (families.Count > previewed)
            Console.WriteLine($"      ... {families.Count - previewed} more families omitted");
    }

    private static void PrintWindowPatternSummary(
        string fileName,
        IReadOnlyList<SaveGlobalWindowPatternHitSnapshot> patterns
    )
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
        foreach (var pattern in patterns)
        {
            if (written >= MaxWindowPatterns)
                break;

            if (written > 0)
                sb.Append(", ");

            AppendWindowPattern(ref sb, in pattern);
            written++;
        }

        Console.WriteLine(sb.ToString());
    }

    private static void PrintWindowTraceSummary(
        string fileName,
        IReadOnlyList<PcDataSlotSnapshot> snapshots,
        IReadOnlyList<SaveGlobalWindowTraceHitSnapshot> traces
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
        foreach (var trace in traces)
        {
            if (trace.Hits <= 0)
                continue;

            if (written >= MaxWindowTraceSpecs)
                break;

            var header = new ValueStringBuilder(headerInitial);
            header.Append("      [");
            header.Append(trace.StartInt);
            header.Append("..");
            header.Append(trace.StartInt + trace.Width - 1);
            header.Append("] x");
            header.Append(trace.Hits);
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

    private static void AppendWindowPattern(ref ValueStringBuilder sb, in SaveGlobalWindowPatternHitSnapshot pattern)
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
        sb.Append(pattern.Hits);
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
        var dump = SaveGlobalDumpService.Create(
            in file,
            MaxQuadPreviewRows,
            MaxHexRows,
            MaxPreviewInts,
            FirstNonZeroEntries,
            LastNonZeroEntries,
            MaxAsciiPreviewStrings,
            MaxSaveIdPairPreview,
            MaxData2RegionPreviewInts
        );

        Console.WriteLine(
            $"  Header: i0={dump.Header0}  i1={dump.Header1}  ints={dump.TotalInts}  trailingBytes={dump.TrailingBytes}"
        );
        Console.WriteLine($"  Sentinels: BEEFCAFE={dump.BeefCafeCount}  FFFFFFFF={dump.MinusOneCount}");
        Console.WriteLine();

        PrintQuadPreview(dump.QuadPreviewRows);
        Console.WriteLine();

        Console.WriteLine("  Hex (first 256 bytes):");
        PrintHexPreview(dump.HexRows);

        if (dump.HexOmittedBytes > 0)
            Console.WriteLine($"  ... ({dump.HexOmittedBytes} more bytes)");

        Console.WriteLine();
        PrintIntPreview(dump.IntRows);

        Console.WriteLine();
        PrintNonZeroSummary(dump.NonZeroSummary);

        if (file.QuadSummary is { } quadSummary)
        {
            Console.WriteLine();
            PrintAlignedQuadSummary(in quadSummary);
        }

        if (dump.SaveIdPairDetails is { } detectedPairs)
        {
            Console.WriteLine();
            PrintSaveIdPairDetails(detectedPairs);
        }

        Console.WriteLine();
        Console.WriteLine("  ASCII strings (len-prefixed, >= 4 chars):");
        if (dump.AsciiCandidates.Count == 0)
        {
            Console.WriteLine("    (none found)");
            return;
        }

        foreach (var candidate in dump.AsciiCandidates)
            PrintAsciiCandidate(candidate);
    }

    private static void PrintQuadPreview(IReadOnlyList<SaveGlobalQuadPreviewRowSnapshot> rows)
    {
        if (rows.Count == 0)
            return;

        Console.WriteLine("  INT32[4] preview after 8-byte header:");
        Span<char> initial = stackalloc char[192];
        foreach (var row in rows)
        {
            var sb = new ValueStringBuilder(initial);
            sb.Append("    [");
            sb.Append(row.RowIndex);
            sb.Append("] a=0x");
            sb.AppendHex((uint)row.A, ReadOnlySpan<char>.Empty, 8);
            sb.Append("  b=");
            sb.Append(row.B);
            sb.Append("  c=");
            sb.Append(row.C);
            sb.Append("  d=0x");
            sb.AppendHex((uint)row.D, ReadOnlySpan<char>.Empty, 8);
            Console.WriteLine(sb.ToString());
        }
    }

    private static void PrintHexPreview(IReadOnlyList<SaveGlobalHexPreviewRowSnapshot> rows)
    {
        Span<char> initial = stackalloc char[96];
        foreach (var row in rows)
        {
            var sb = new ValueStringBuilder(initial);
            sb.Append("  ");
            sb.AppendHex((uint)row.Offset, ReadOnlySpan<char>.Empty, 4);
            sb.Append("  ");

            for (var col = 0; col < 16; col++)
            {
                if (col < row.Bytes.Length)
                    sb.AppendHex(row.Bytes[col]);
                else
                    sb.Append("  ");

                sb.Append(' ');
                if (col == 7)
                    sb.Append(' ');
            }

            sb.Append(' ');
            for (var col = 0; col < row.Bytes.Length; col++)
            {
                var ch = (char)row.Bytes[col];
                sb.Append(ch is >= ' ' and <= '~' ? ch : '.');
            }

            Console.WriteLine(sb.ToString());
        }
    }

    private static void PrintIntPreview(IReadOnlyList<SaveGlobalIntPreviewRowSnapshot> rows)
    {
        var intCount = rows.Sum(static row => row.Values.Count);
        Console.WriteLine($"  INT32 values (first {intCount}):");
        Span<char> initial = stackalloc char[256];
        foreach (var row in rows)
        {
            var sb = new ValueStringBuilder(initial);
            sb.Append("  [");
            sb.Append(row.StartIndex);
            sb.Append("] ");
            for (var index = 0; index < row.Values.Count; index++)
            {
                if (index > 0)
                    sb.Append(' ');

                sb.Append(row.Values[index]);
            }

            Console.WriteLine(sb.ToString());
        }
    }

    private static void PrintNonZeroSummary(SaveGlobalNonZeroSummarySnapshot summary)
    {
        Span<char> initial = stackalloc char[96];
        var sb = new ValueStringBuilder(initial);
        sb.Append("  Non-zero INT32 count: ");
        sb.Append(summary.Count);
        sb.Append(" of ");
        sb.Append(summary.TotalInts);
        sb.Append(" (");
        AppendFixed1(ref sb, summary.Density);
        sb.Append("%)");
        Console.WriteLine(sb.ToString());

        if (summary.Count == 0)
            return;

        if (!summary.IsDense)
        {
            Console.WriteLine("  Non-zero INT32 entries:");
            foreach (var entry in summary.Entries)
                PrintIndexedIntEntry(entry);
            return;
        }

        Console.WriteLine("  (too many non-zero entries to list; file is dense)");
        Console.WriteLine("  First 20 non-zero:");
        foreach (var entry in summary.FirstEntries)
            PrintIndexedIntEntry(entry);

        Console.WriteLine("  Last 10 non-zero:");
        foreach (var entry in summary.LastEntries)
            PrintIndexedIntEntry(entry);
    }

    private static void PrintIndexedIntEntry(SaveGlobalIndexedIntSnapshot entry)
    {
        Span<char> initial = stackalloc char[128];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    [");
        sb.Append(entry.Index);
        sb.Append("] offset=0x");
        sb.AppendHex((uint)(entry.Index * 4), ReadOnlySpan<char>.Empty, 6);
        sb.Append("  val=");
        sb.Append(entry.Value);
        sb.Append("  0x");
        sb.AppendHex((uint)entry.Value, ReadOnlySpan<char>.Empty, 8);
        Console.WriteLine(sb.ToString());
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

    private static void AppendFrontMatterRunOrEnd(ref ValueStringBuilder sb, AlignedQuadRunSummary? run)
    {
        if (run is not { } value)
        {
            sb.Append("END");
            return;
        }

        AppendFrontMatterRun(ref sb, value);
    }

    private static void PrintSaveIdPairDetails(SaveGlobalSaveIdPairDetailsSnapshot saveIdPairs)
    {
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
        region.Append(saveIdPairs.PrefixPreview?.IntCount ?? 0);
        if (saveIdPairs.PrefixPreview is { } prefixPreview)
            AppendData2RegionPreview(ref region, prefixPreview, isPrefix: true);

        region.Append("  suffixInts=");
        region.Append(saveIdPairs.SuffixPreview?.IntCount ?? 0);
        if (saveIdPairs.SuffixPreview is { } suffixPreview)
            AppendData2RegionPreview(ref region, suffixPreview, isPrefix: false);

        Console.WriteLine(region.ToString());

        Span<char> previewInitial = stackalloc char[512];
        var preview = new ValueStringBuilder(previewInitial);
        preview.Append("    nonZeroPairs: ");
        for (var index = 0; index < saveIdPairs.NonZeroPairPreview.Count; index++)
        {
            if (index > 0)
                preview.Append(", ");

            var pair = saveIdPairs.NonZeroPairPreview[index];
            preview.Append(pair.Id);
            preview.Append(':');
            preview.Append(pair.Value);
        }

        if (saveIdPairs.NonZeroPairPreview.Count == 0)
        {
            preview.Append("(none)");
        }
        else if (saveIdPairs.OmittedNonZeroPairCount > 0)
        {
            preview.Append(" +");
            preview.Append(saveIdPairs.OmittedNonZeroPairCount);
            preview.Append(" more");
        }

        Console.WriteLine(preview.ToString());
    }

    private static void AppendData2RegionPreview(
        ref ValueStringBuilder sb,
        SaveGlobalData2RegionPreviewSnapshot preview,
        bool isPrefix
    )
    {
        if (isPrefix)
        {
            sb.Append(" head=");
            AppendIntSpanPreview(ref sb, preview.HeadValues);
            if (preview.TailValues.Count > 0)
            {
                sb.Append(" tail=");
                AppendIntSpanPreview(ref sb, preview.TailValues);
            }
        }
        else
        {
            sb.Append(" vals=");
            AppendIntSpanPreview(ref sb, preview.HeadValues);
        }
    }

    private static void AppendIntSpanPreview(ref ValueStringBuilder sb, IReadOnlyList<int> values)
    {
        sb.Append('[');
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
                sb.Append(',');

            sb.Append(values[index]);
        }

        sb.Append(']');
    }

    private static void PrintAsciiCandidate(SaveGlobalAsciiCandidateSnapshot candidate)
    {
        Span<char> initial = stackalloc char[160];
        var sb = new ValueStringBuilder(initial);
        sb.Append("    offset=0x");
        sb.AppendHex((uint)candidate.Offset, ReadOnlySpan<char>.Empty, 6);
        sb.Append("  len=");
        sb.Append(candidate.Length);
        sb.Append("  \"");
        sb.Append(candidate.Text);
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
