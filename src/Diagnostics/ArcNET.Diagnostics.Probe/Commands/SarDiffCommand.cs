using ArcNET.Core;
using ArcNET.Diagnostics;

namespace Probe.Commands;

internal sealed class SarDiffCommand : IProbeCommand
{
    private const int DefaultRecentSlotCount = 8;

    public Task RunAsync(string saveDir, string[] args)
    {
        int firstSlot;
        int lastSlot;
        if (args.Length == 0)
        {
            (firstSlot, lastSlot) = ProbeConfig.ResolveRecentSlotRange(saveDir, DefaultRecentSlotCount);
            Console.Error.WriteLine(
                $"[probe] No slot range specified for sar-diff; defaulting to the most recent {DefaultRecentSlotCount} available slots ({firstSlot:D4}-{lastSlot:D4})."
            );
        }
        else if (args.Length == 1)
        {
            Console.WriteLine("  Usage: probe 9 <firstSlot> <lastSlot>");
            Console.WriteLine(
                $"  Or omit both slots to scan the most recent {DefaultRecentSlotCount} available saves safely."
            );
            return Task.CompletedTask;
        }
        else
        {
            firstSlot = 13;
            lastSlot = 13;
            _ = int.TryParse(args[0], out firstSlot);
            _ = int.TryParse(args[1], out lastSlot);
        }

        if (firstSlot > lastSlot)
            (firstSlot, lastSlot) = (lastSlot, firstSlot);

        Console.WriteLine($"\n=== Mode 9: SAR Diff Engine - slots {firstSlot:D4}-{lastSlot:D4} ===");

        var history = PlayerSarHistoryService.Create(saveDir, firstSlot, lastSlot, Console.Error.WriteLine);
        if (history.Slots.Count < 2)
        {
            Console.WriteLine("  Need at least 2 valid snapshots to diff.");
            return Task.CompletedTask;
        }

        var lifecycleAnalysis = PlayerSarAnalysisService.CreateLifecycleAnalysis(history);
        var transitionAnalysis = PlayerSarAnalysisService.CreateTransitionAnalysis(history);

        PrintLifecycleReport(PlayerSarReportService.CreateLifecycleReport(history, lifecycleAnalysis));
        PrintTransitionReport(PlayerSarReportService.CreateTransitionReport(transitionAnalysis));

        return Task.CompletedTask;
    }

    private static void PrintLifecycleReport(PlayerSarLifecycleReportSnapshot report)
    {
        Console.WriteLine(
            "\n  Fingerprint summary (recurring or duplicate-heavy fingerprints; one-slot singleton fingerprints omitted)"
        );
        Console.WriteLine(
            "\n  "
                + "Fingerprint".PadRight(16)
                + "  "
                + "Ann".PadRight(32)
                + "  "
                + "Slots".PadRight(11)
                + "  "
                + "dup@present".PadRight(11)
                + "  "
                + "tracks".PadLeft(6)
                + "  "
                + "multi".PadLeft(5)
                + "  "
                + "one".PadLeft(3)
                + "  "
                + "chg".PadLeft(3)
        );
        Console.WriteLine(new string('-', 104));
        foreach (var summary in report.Fingerprints)
        {
            var annotation = ValueBufferText.TruncateText(summary.Annotation, 32);
            Console.WriteLine(
                "  "
                    + summary.Fingerprint.PadRight(16)
                    + "  "
                    + annotation.PadRight(32)
                    + "  "
                    + summary.SlotSpan.PadRight(11)
                    + "  "
                    + summary.DuplicateRange.PadRight(11)
                    + "  "
                    + summary.TrackCount.ToString().PadLeft(6)
                    + "  "
                    + summary.RecurringTrackCount.ToString().PadLeft(5)
                    + "  "
                    + summary.SingleSlotTrackCount.ToString().PadLeft(3)
                    + "  "
                    + summary.ChangedTrackCount.ToString().PadLeft(3)
            );
        }

        if (report.OmittedTrackRowCount > 0)
        {
            Console.WriteLine(
                $"\n  Omitted {report.OmittedTrackRowCount} one-slot lifecycle rows from detail output; {report.OmittedSingletonFingerprintCount} fingerprints only appear as single-slot singletons."
            );
        }

        Console.WriteLine("\n  Multi-slot track detail");
        Console.WriteLine(
            "\n  "
                + "Fingerprint".PadRight(16)
                + "  "
                + "Ann".PadRight(36)
                + "  "
                + "Lifecycle".PadRight(20)
                + "  "
                + "eCnt range".PadRight(15)
                + "bsCnt"
                + "  "
                + "bsId".PadRight(10)
                + "  Value@first -> Value@last"
        );
        Console.WriteLine(new string('-', 163));
        foreach (var track in report.Tracks)
        {
            var annotation = ValueBufferText.TruncateText(track.Annotation, 36);
            var valueChange = track.ValueChanged ? "  <- changed" : string.Empty;
            Console.WriteLine(
                $"  {track.FingerprintKey, -16}  {annotation, -36}  {track.Lifecycle, -20}  ECnt={track.ElementCountRange, -10}  bsCnt={track.BitsetWordCounts, -5}  {track.BitsetIdLabel, -10}  {track.FirstValueSummary} -> {track.LastValueSummary}{valueChange}"
            );
        }
    }

    private static void PrintTransitionReport(PlayerSarTransitionReportSnapshot report)
    {
        Console.WriteLine("\n\n    Slot A   Slot B   Level->Level  bytes      Changes");
        Console.WriteLine(new string('-', 120));
        foreach (var transition in report.Transitions)
        {
            var discontinuity = transition.IsDiscontinuous ? " [DISC]" : string.Empty;
            Console.WriteLine(
                $"  {transition.FromSlot:D4}->{transition.ToSlot:D4}  lv{transition.FromLevel}->{transition.ToLevel}  bytes={transition.FromRawBytesLength}->{transition.ToRawBytesLength}{discontinuity}"
            );
            Console.WriteLine(
                $"    SUM: new={transition.Summary.AddedCount}  gone={transition.Summary.RemovedCount}  move={transition.Summary.MovedCount}  chg={transition.Summary.ChangedCount}"
            );

            var moveSummary = FormatFingerprintSummary(transition.Summary.MovedFingerprints);
            if (!string.IsNullOrEmpty(moveSummary))
                Console.WriteLine($"    MOVE fp: {moveSummary}");

            var changeSummary = FormatFingerprintSummary(transition.Summary.ChangedFingerprints);
            if (!string.IsNullOrEmpty(changeSummary))
                Console.WriteLine($"    CHG fp:  {changeSummary}");

            PrintTransitionList("    NEW: ", transition.Added);
            PrintTransitionList("    GONE:", transition.Removed);
            PrintTransitionList("    MOVE:", transition.Moved, 12);

            foreach (var changed in transition.Changed)
                PrintChangedTransition(changed);
        }
    }

    private static void PrintTransitionList(
        string prefix,
        IReadOnlyList<PlayerSarTransitionListEntrySnapshot> entries,
        int maxShow = int.MaxValue
    )
    {
        if (entries.Count == 0)
            return;

        Console.Write(prefix);
        foreach (var entry in entries.Take(maxShow))
            Console.Write($" {entry.Label}({entry.Annotation.TruncateAnnotation()})");
        if (entries.Count > maxShow)
            Console.Write($" +{entries.Count - maxShow}more");
        Console.WriteLine();
    }

    private static void PrintChangedTransition(PlayerSarTransitionChangedEntrySnapshot changed)
    {
        var annotation = !string.IsNullOrEmpty(changed.Annotation)
            ? changed.Annotation
            : CharacterSarDiagnostics.AnnotateFingerprint(changed.Fingerprint);
        var annotationPart = !string.IsNullOrEmpty(annotation) ? $" [{annotation.TruncateAnnotation()}]" : string.Empty;
        var detail = BuildChangedDetail(changed);

        if (changed.FieldDiffs.Count > 0)
        {
            Console.Write($"    CHG: {changed.Label}{annotationPart}");
            foreach (var diff in changed.FieldDiffs.Take(12))
                Console.Write($" [{diff.FieldLabel}]:{diff.BeforeValue}->{diff.AfterValue}");
            if (changed.FieldDiffs.Count > 12)
                Console.Write($" +{changed.FieldDiffs.Count - 12}more");
            if (!string.IsNullOrEmpty(detail))
                Console.Write($"  {detail}");
            Console.WriteLine();
            return;
        }

        Console.WriteLine(
            string.IsNullOrEmpty(detail)
                ? $"    CHG: {changed.Label}{annotationPart}"
                : $"    CHG: {changed.Label}{annotationPart} {detail}"
        );
    }

    private static string FormatFingerprintSummary(IReadOnlyList<PlayerSarFingerprintCountSnapshot> counts) =>
        counts.Count == 0
            ? string.Empty
            : string.Join(", ", counts.Select(static count => $"{count.Fingerprint}x{count.Count}"));

    private static string? BuildChangedDetail(PlayerSarTransitionChangedEntrySnapshot changed)
    {
        var slotDiff = BuildSlotDiff(changed.BeforeBitSlots, changed.AfterBitSlots);
        var pointerNoise =
            changed.PointerNoiseSuppressedCount > 0
                ? $"({changed.PointerNoiseSuppressedCount} ptr-noise diff{(changed.PointerNoiseSuppressedCount == 1 ? string.Empty : "s")} suppressed)"
                : null;
        if (changed.FieldDiffs.Count > 0)
            return JoinDetailParts(slotDiff, pointerNoise);

        var valueSummaryChanged =
            changed.BeforeElementCount != changed.AfterElementCount
            || changed.BeforeValueSummary != changed.AfterValueSummary;
        var valueSummary = valueSummaryChanged
            ? $"eCnt={changed.BeforeElementCount}->{changed.AfterElementCount}  {changed.BeforeValueSummary} -> {changed.AfterValueSummary}"
            : null;
        return JoinDetailParts(valueSummary, slotDiff, pointerNoise);
    }

    private static string? BuildSlotDiff(IReadOnlyList<int> before, IReadOnlyList<int> after)
    {
        var beforeLabel = SarUtils.FormatSlotList(before);
        var afterLabel = SarUtils.FormatSlotList(after);
        return beforeLabel == afterLabel ? null : $"slots:{beforeLabel}->{afterLabel}";
    }

    private static string? JoinDetailParts(params string?[] parts)
    {
        var values = parts.Where(static part => !string.IsNullOrWhiteSpace(part)).ToList();
        return values.Count == 0 ? null : string.Join("  ", values);
    }
}
