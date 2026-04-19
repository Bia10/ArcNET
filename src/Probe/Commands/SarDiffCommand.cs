using ArcNET.Core;
using ArcNET.Editor;
using Probe;

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

        var snapshots = new List<SlotSnapshot>();

        for (var slot = firstSlot; slot <= lastSlot; slot++)
        {
            var stem = $"Slot{slot:D4}";
            var gsiFiles = Directory.GetFiles(saveDir, stem + "*.gsi");
            var tfaiPath = Path.Combine(saveDir, stem + ".tfai");
            var tfafPath = Path.Combine(saveDir, stem + ".tfaf");
            if (gsiFiles.Length == 0 || !File.Exists(tfaiPath) || !File.Exists(tfafPath))
                continue;

            LoadedSave save;
            try
            {
                save = SaveGameLoader.Load(gsiFiles[0], tfaiPath, tfafPath);
            }
            catch
            {
                Console.Error.WriteLine($"  [{stem}] load failed - skipped");
                continue;
            }

            var playerRec = SarUtils.FindPlayerRecord(save);
            if (playerRec is null)
                continue;

            var sars = SarUtils.ParseSars(playerRec.RawBytes);
            var level = playerRec.Stats.Length > 17 ? playerRec.Stats[17] : -1;
            snapshots.Add(
                new SlotSnapshot(slot, level, playerRec.RawBytes.Length, sars, save.Info.LeaderName, playerRec)
            );
            Console.Error.WriteLine(
                $"  [{stem}] {save.Info.LeaderName} lv={level} RawBytes={playerRec.RawBytes.Length}B sars={sars.Count}"
            );
        }

        if (snapshots.Count < 2)
        {
            Console.WriteLine("  Need at least 2 valid snapshots to diff.");
            return Task.CompletedTask;
        }

        static string TrackKey(string fingerprint, int trackIndex) =>
            trackIndex == 0 ? fingerprint : $"{fingerprint}#{trackIndex + 1}";

        static string BaseFingerprint(string fingerprintKey) => fingerprintKey.Split('#')[0];

        static string PairLabel(string fingerprint, int indexA, int indexB, int countA, int countB) =>
            countA > 1 || countB > 1 ? $"{fingerprint}[a{indexA + 1}->b{indexB + 1}]" : fingerprint;

        static string UnmatchedLabel(string fingerprint, char side, int index, int countA, int countB) =>
            countA > 1 || countB > 1 ? $"{fingerprint}[{side}{index + 1}]" : fingerprint;

        static string SummarizeFingerprintCounts(IEnumerable<string> fingerprints, int maxShow = 4)
        {
            var top = fingerprints
                .GroupBy(fingerprint => fingerprint)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Take(maxShow)
                .Select(group => $"{group.Key}x{group.Count()}")
                .ToList();
            return top.Count == 0 ? string.Empty : ValueBufferText.JoinText(top, ", ");
        }

        static Dictionary<string, List<(int Slot, SarEntry Sar)>> BuildFingerprintHistory(
            IReadOnlyList<SlotSnapshot> snaps
        )
        {
            var history = new Dictionary<string, List<(int Slot, SarEntry Sar)>>();
            var fingerprints = snaps
                .SelectMany(snapshot => snapshot.Sars.Where(sar => !sar.IsFiller).Select(sar => sar.Fingerprint))
                .Distinct()
                .OrderBy(fingerprint => fingerprint);

            foreach (var fingerprint in fingerprints)
            {
                var tracks = new List<List<(int Slot, SarEntry Sar)>>();
                List<SarEntry> prevEntries = [];
                List<int> prevTrackIds = [];

                foreach (var snapshot in snaps)
                {
                    var currentEntries = snapshot
                        .Sars.Where(sar => !sar.IsFiller && sar.Fingerprint == fingerprint)
                        .ToList();
                    if (currentEntries.Count == 0)
                    {
                        prevEntries = [];
                        prevTrackIds = [];
                        continue;
                    }

                    var currentTrackIds = Enumerable.Repeat(-1, currentEntries.Count).ToArray();
                    if (prevEntries.Count == 0)
                    {
                        for (var i = 0; i < currentEntries.Count; i++)
                        {
                            tracks.Add([(snapshot.Slot, currentEntries[i])]);
                            currentTrackIds[i] = tracks.Count - 1;
                        }
                    }
                    else
                    {
                        var matches = SarUtils.MatchSarGroups(prevEntries, currentEntries);
                        foreach (var match in matches)
                        {
                            var trackId = prevTrackIds[match.IndexA];
                            tracks[trackId].Add((snapshot.Slot, currentEntries[match.IndexB]));
                            currentTrackIds[match.IndexB] = trackId;
                        }

                        for (var i = 0; i < currentEntries.Count; i++)
                        {
                            if (currentTrackIds[i] >= 0)
                                continue;

                            tracks.Add([(snapshot.Slot, currentEntries[i])]);
                            currentTrackIds[i] = tracks.Count - 1;
                        }
                    }

                    prevEntries = currentEntries;
                    prevTrackIds = currentTrackIds.ToList();
                }

                for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
                    history[TrackKey(fingerprint, trackIndex)] = tracks[trackIndex];
            }

            return history;
        }

        var fpHistory = BuildFingerprintHistory(snapshots);
        var totalSlots = snapshots.Count;

        var lifecycleRows = fpHistory
            .Select(kvp =>
            {
                var fingerprintKey = kvp.Key;
                var fingerprint = BaseFingerprint(fingerprintKey);
                var history = kvp.Value;
                var presentCount = history.Count;
                var firstSlotNumber = history[0].Slot;
                var lastSlotNumber = history[^1].Slot;
                var eCnts = history.Select(item => item.Sar.ECnt).Distinct().OrderBy(x => x).ToList();
                var bsCnts = history.Select(item => item.Sar.BCnt).Distinct().OrderBy(x => x).ToList();
                var valFirst = history[0].Sar.ValueSummary;
                var valLast = history[^1].Sar.ValueSummary;
                var valueChanged = valFirst != valLast;
                var eCntRange = eCnts.Count == 1 ? eCnts[0].ToString() : $"{eCnts[0]}-{eCnts[^1]}";
                var bsCntStr = ValueBufferText.JoinInt32(bsCnts, "/");
                var eCntGrows = eCnts.Count > 1 && eCnts[^1] > eCnts[0];
                var distinctBsIds = history.Select(item => item.Sar.BsId).Distinct().OrderBy(x => x).ToList();
                var bsIdStr = distinctBsIds.Count == 1 ? $"0x{distinctBsIds[0]:X4}" : "varies";
                var valueAnnotation = SarUtils.AnnotateSarValue(history[0].Sar);
                string lifecycle;
                if (presentCount == totalSlots)
                {
                    lifecycle = "ALL";
                }
                else if (presentCount == 1)
                {
                    lifecycle = $"only@{firstSlotNumber:D4}";
                }
                else
                {
                    var appeared = firstSlotNumber > snapshots[0].Slot;
                    var disappeared = lastSlotNumber < snapshots[^1].Slot;
                    lifecycle =
                        appeared && disappeared ? $"NEW@{firstSlotNumber:D4}/GONE@{lastSlotNumber:D4}"
                        : appeared ? $"NEW@{firstSlotNumber:D4}"
                        : disappeared ? $"GONE@{lastSlotNumber:D4}"
                        : $"{presentCount}/{totalSlots}";
                }

                return new
                {
                    FingerprintKey = fingerprintKey,
                    Fingerprint = fingerprint,
                    PresentCount = presentCount,
                    FirstSlot = firstSlotNumber,
                    LastSlot = lastSlotNumber,
                    ECntRange = eCntRange + (eCntGrows ? " ↑" : string.Empty),
                    BsCntStr = bsCntStr,
                    BsIdStr = bsIdStr,
                    ValFirst = valFirst,
                    ValLast = valLast,
                    ValueChanged = valueChanged,
                    Lifecycle = lifecycle,
                    ValueAnnotation = valueAnnotation,
                };
            })
            .ToList();

        var lifecycleSummary = lifecycleRows
            .GroupBy(row => row.Fingerprint)
            .Select(group =>
            {
                var countsBySnapshot = snapshots
                    .Select(snapshot => snapshot.Sars.Count(sar => !sar.IsFiller && sar.Fingerprint == group.Key))
                    .Where(count => count > 0)
                    .ToList();

                return new
                {
                    Fingerprint = group.Key,
                    Annotation = SarUtils.AnnotateFingerprint(group.Key),
                    FirstSlot = group.Min(row => row.FirstSlot),
                    LastSlot = group.Max(row => row.LastSlot),
                    MinDup = countsBySnapshot.Count > 0 ? countsBySnapshot.Min() : 0,
                    MaxDup = countsBySnapshot.Count > 0 ? countsBySnapshot.Max() : 0,
                    TrackCount = group.Count(),
                    RecurringTrackCount = group.Count(row => row.PresentCount > 1),
                    SingleSlotTrackCount = group.Count(row => row.PresentCount == 1),
                    ChangedTrackCount = group.Count(row => row.ValueChanged),
                };
            })
            .Where(summary => summary.RecurringTrackCount > 0 || summary.MaxDup > 1)
            .OrderByDescending(summary => summary.ChangedTrackCount)
            .ThenByDescending(summary => summary.MaxDup)
            .ThenByDescending(summary => summary.TrackCount)
            .ThenBy(summary => summary.Fingerprint)
            .ToList();

        var recurringFingerprints = lifecycleSummary.Select(summary => summary.Fingerprint).ToHashSet();
        var detailedLifecycleRows = lifecycleRows
            .Where(row => row.PresentCount > 1 && recurringFingerprints.Contains(row.Fingerprint))
            .OrderByDescending(row => row.PresentCount)
            .ThenBy(row => row.FingerprintKey)
            .ToList();

        var omittedTrackRows = lifecycleRows.Count - detailedLifecycleRows.Count;
        var omittedSingletonFingerprints = lifecycleRows
            .Where(row => !recurringFingerprints.Contains(row.Fingerprint) && row.PresentCount == 1)
            .Select(row => row.Fingerprint)
            .Distinct()
            .Count();

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
        foreach (var summary in lifecycleSummary)
        {
            var annotation = ValueBufferText.TruncateText(summary.Annotation, 32);
            var slotSpan =
                summary.FirstSlot == summary.LastSlot
                    ? $"{summary.FirstSlot:D4}"
                    : $"{summary.FirstSlot:D4}-{summary.LastSlot:D4}";
            var dupRange =
                summary.MinDup == summary.MaxDup ? summary.MinDup.ToString() : $"{summary.MinDup}-{summary.MaxDup}";
            Console.WriteLine(
                "  "
                    + summary.Fingerprint.PadRight(16)
                    + "  "
                    + annotation.PadRight(32)
                    + "  "
                    + slotSpan.PadRight(11)
                    + "  "
                    + dupRange.PadRight(11)
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

        if (omittedTrackRows > 0)
        {
            Console.WriteLine(
                $"\n  Omitted {omittedTrackRows} one-slot lifecycle rows from detail output; {omittedSingletonFingerprints} fingerprints only appear as single-slot singletons."
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
        foreach (var row in detailedLifecycleRows)
        {
            var annotation = ValueBufferText.TruncateText(row.ValueAnnotation, 36);
            var valueChange = row.ValueChanged ? "  <- changed" : string.Empty;
            Console.WriteLine(
                $"  {row.FingerprintKey, -16}  {annotation, -36}  {row.Lifecycle, -20}  ECnt={row.ECntRange, -10}  bsCnt={row.BsCntStr, -5}  {row.BsIdStr, -10}  {row.ValFirst} -> {row.ValLast}{valueChange}"
            );
        }

        Console.WriteLine("\n\n    Slot A   Slot B   Level->Level  bytes      Changes");
        Console.WriteLine(new string('-', 120));
        for (var snapshotIndex = 0; snapshotIndex < snapshots.Count - 1; snapshotIndex++)
        {
            var snapA = snapshots[snapshotIndex];
            var snapB = snapshots[snapshotIndex + 1];
            var groupA = snapA
                .Sars.Where(sar => !sar.IsFiller)
                .GroupBy(sar => sar.Fingerprint)
                .ToDictionary(group => group.Key, group => group.ToList());
            var groupB = snapB
                .Sars.Where(sar => !sar.IsFiller)
                .GroupBy(sar => sar.Fingerprint)
                .ToDictionary(group => group.Key, group => group.ToList());

            var appeared = new List<(string Label, string Fingerprint, string Annotation)>();
            var disappeared = new List<(string Label, string Fingerprint, string Annotation)>();
            var movedSars = new List<(string Label, string Fingerprint, string Annotation)>();
            var changedSars =
                new List<(
                    string Label,
                    string Fingerprint,
                    string ValueAnnotation,
                    List<(int Idx, int VA, int VB)>? ElemDiffs,
                    string? Detail
                )>();

            foreach (var fingerprint in groupA.Keys.Union(groupB.Keys).OrderBy(x => x))
            {
                var listA = groupA.TryGetValue(fingerprint, out var tmpA) ? tmpA : [];
                var listB = groupB.TryGetValue(fingerprint, out var tmpB) ? tmpB : [];
                var matches = SarUtils.MatchSarGroups(listA, listB);
                var matchedA = matches.Select(match => match.IndexA).ToHashSet();
                var matchedB = matches.Select(match => match.IndexB).ToHashSet();

                foreach (var match in matches.OrderBy(match => match.IndexA).ThenBy(match => match.IndexB))
                {
                    var sarA = listA[match.IndexA];
                    var sarB = listB[match.IndexB];
                    var label = PairLabel(fingerprint, match.IndexA, match.IndexB, listA.Count, listB.Count);
                    var bitSlotsChanged = !sarA.BitSlots.SequenceEqual(sarB.BitSlots);
                    var reorderedOnly = (listA.Count > 1 || listB.Count > 1) && match.IndexA != match.IndexB;
                    var detailParts = new List<string>();
                    if (bitSlotsChanged)
                    {
                        detailParts.Add(
                            $"slots:{SarUtils.FormatSlotList(sarA.BitSlots)}->{SarUtils.FormatSlotList(sarB.BitSlots)}"
                        );
                    }

                    var slotDiffDetail = detailParts.Count > 0 ? ValueBufferText.JoinText(detailParts, "  ") : null;

                    if (sarA.ESize == 4 && sarA.ECnt == sarB.ECnt && sarA.FirstVals.Length > 0)
                    {
                        var allDiffs = SarUtils.CompareElements(sarA, sarB);
                        var (diffs, ptrCount) = SarUtils.PartitionElementDiffs(allDiffs);
                        var ptrNote =
                            ptrCount > 0 ? $"({ptrCount} ptr-noise diff{(ptrCount == 1 ? "" : "s")} suppressed)" : null;
                        var hasSemanticChange = diffs.Count > 0 || slotDiffDetail is not null;
                        if (hasSemanticChange)
                        {
                            var detail =
                                ptrNote is not null && diffs.Count == 0 && slotDiffDetail is null ? ptrNote
                                : ptrNote is not null ? $"{slotDiffDetail}  {ptrNote}"
                                : slotDiffDetail;
                            var ann = SarUtils.AnnotateSarValue(sarB);
                            changedSars.Add((label, fingerprint, ann, diffs.Count > 0 ? diffs : null, detail));
                        }
                        else if (reorderedOnly)
                            movedSars.Add((label, fingerprint, SarUtils.AnnotateSarValue(sarB)));
                    }
                    else if (sarA.ECnt != sarB.ECnt || sarA.ValueSummary != sarB.ValueSummary || bitSlotsChanged)
                    {
                        var detail = $"eCnt={sarA.ECnt}->{sarB.ECnt}  {sarA.ValueSummary} -> {sarB.ValueSummary}";
                        if (slotDiffDetail is not null)
                            detail += $"  {slotDiffDetail}";
                        var ann2 = SarUtils.AnnotateSarValue(sarB);
                        changedSars.Add((label, fingerprint, ann2, null, detail));
                    }
                    else if (reorderedOnly)
                    {
                        movedSars.Add((label, fingerprint, SarUtils.AnnotateSarValue(sarB)));
                    }
                }

                for (var index = 0; index < listB.Count; index++)
                {
                    if (!matchedB.Contains(index))
                        appeared.Add(
                            (
                                UnmatchedLabel(fingerprint, 'b', index, listA.Count, listB.Count),
                                fingerprint,
                                SarUtils.AnnotateSarValue(listB[index])
                            )
                        );
                }

                for (var index = 0; index < listA.Count; index++)
                {
                    if (!matchedA.Contains(index))
                        disappeared.Add(
                            (
                                UnmatchedLabel(fingerprint, 'a', index, listA.Count, listB.Count),
                                fingerprint,
                                SarUtils.AnnotateSarValue(listA[index])
                            )
                        );
                }
            }

            if (appeared.Count == 0 && disappeared.Count == 0 && movedSars.Count == 0 && changedSars.Count == 0)
                continue;

            var isDiscontinuous = snapB.Level < snapA.Level - 3;
            var discMark = isDiscontinuous ? " [DISC]" : string.Empty;
            Console.WriteLine(
                $"  {snapA.Slot:D4}->{snapB.Slot:D4}  lv{snapA.Level}->{snapB.Level}  bytes={snapA.RawBytesLen}->{snapB.RawBytesLen}{discMark}"
            );
            Console.WriteLine(
                $"    SUM: new={appeared.Count}  gone={disappeared.Count}  move={movedSars.Count}  chg={changedSars.Count}"
            );

            var moveSummary = SummarizeFingerprintCounts(movedSars.Select(item => item.Fingerprint));
            if (!string.IsNullOrEmpty(moveSummary))
                Console.WriteLine($"    MOVE fp: {moveSummary}");

            var changeSummary = SummarizeFingerprintCounts(changedSars.Select(item => item.Fingerprint));
            if (!string.IsNullOrEmpty(changeSummary))
                Console.WriteLine($"    CHG fp:  {changeSummary}");

            if (appeared.Count > 0)
            {
                Console.Write("    NEW: ");
                foreach (var (label, _, annotation) in appeared)
                    Console.Write($" {label}({annotation.TruncateAnnotation()})");
                Console.WriteLine();
            }

            if (disappeared.Count > 0)
            {
                Console.Write("    GONE:");
                foreach (var (label, _, annotation) in disappeared)
                    Console.Write($" {label}({annotation.TruncateAnnotation()})");
                Console.WriteLine();
            }

            if (movedSars.Count > 0)
            {
                Console.Write("    MOVE:");
                foreach (var (label, _, annotation) in movedSars.Take(12))
                    Console.Write($" {label}({annotation.TruncateAnnotation()})");
                if (movedSars.Count > 12)
                    Console.Write($" +{movedSars.Count - 12}more");
                Console.WriteLine();
            }

            foreach (var (label, fingerprint, valueAnnotation, diffs, detail) in changedSars)
            {
                var annotation = !string.IsNullOrEmpty(valueAnnotation)
                    ? valueAnnotation
                    : SarUtils.AnnotateFingerprint(fingerprint);
                var annotationPart = !string.IsNullOrEmpty(annotation)
                    ? $" [{annotation.TruncateAnnotation()}]"
                    : string.Empty;
                if (diffs is { Count: > 0 })
                {
                    Console.Write($"    CHG: {label}{annotationPart} ");
                    foreach (var (index, valueA, valueB) in diffs.Take(12))
                    {
                        var fieldLabel = SarUtils.GetElementLabel(fingerprint, index);
                        Console.Write($" [{fieldLabel}]:{valueA}->{valueB}");
                    }
                    if (diffs.Count > 12)
                        Console.Write($" +{diffs.Count - 12}more");
                    if (!string.IsNullOrEmpty(detail))
                        Console.Write($"  {detail}");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"    CHG: {label}{annotationPart} {detail}");
                }
            }
        }

        return Task.CompletedTask;
    }
}
