using ArcNET.Editor;
using Probe;

namespace Probe.Commands;

internal sealed class SarDiffCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var firstSlot = 13;
        var lastSlot = 120;
        if (args.Length >= 1)
            _ = int.TryParse(args[0], out firstSlot);
        if (args.Length >= 2)
            _ = int.TryParse(args[1], out lastSlot);
        if (firstSlot > lastSlot)
            (firstSlot, lastSlot) = (lastSlot, firstSlot);

        Console.WriteLine($"\n=== Mode 9: SAR Diff Engine — slots {firstSlot:D4}–{lastSlot:D4} ===");

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
                Console.Error.WriteLine($"  [{stem}] load failed — skipped");
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
                .Select(group => $"{group.Key}×{group.Count()}")
                .ToList();
            return top.Count == 0 ? string.Empty : string.Join(", ", top);
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
                var eCntRange = eCnts.Count == 1 ? eCnts[0].ToString() : $"{eCnts[0]}–{eCnts[^1]}";
                var bsCntStr = string.Join("/", bsCnts);
                var eCntGrows = eCnts.Count > 1 && eCnts[^1] > eCnts[0];

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
                    ValFirst = valFirst,
                    ValLast = valLast,
                    ValueChanged = valueChanged,
                    Lifecycle = lifecycle,
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
            var annotation = SarUtils.TruncateText(summary.Annotation, 32);
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
                + "Ann".PadRight(32)
                + "  "
                + "Lifecycle".PadRight(20)
                + "  "
                + "eCnt range".PadRight(15)
                + "bsCnt"
                + "  Value@first → Value@last"
        );
        Console.WriteLine(new string('-', 144));
        foreach (var row in detailedLifecycleRows)
        {
            var annotation = SarUtils.AnnotateFingerprint(row.Fingerprint);
            var valueChange = row.ValueChanged ? "  ← changed" : string.Empty;
            Console.WriteLine(
                $"  {row.FingerprintKey, -16}  {annotation, -32}  {row.Lifecycle, -20}  ECnt={row.ECntRange, -10}  bsCnt={row.BsCntStr, -5}  {row.ValFirst} → {row.ValLast}{valueChange}"
            );
        }

        Console.WriteLine("\n\n    Slot A   Slot B   Level→Level  bytes      Changes");
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

            var appeared = new List<(string Label, string Fingerprint)>();
            var disappeared = new List<(string Label, string Fingerprint)>();
            var movedSars = new List<(string Label, string Fingerprint)>();
            var changedSars =
                new List<(
                    string Label,
                    string Fingerprint,
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
                            $"slots:{SarUtils.FormatSlotList(sarA.BitSlots)}→{SarUtils.FormatSlotList(sarB.BitSlots)}"
                        );
                    }

                    var slotDiffDetail = detailParts.Count > 0 ? string.Join("  ", detailParts) : null;

                    if (sarA.ESize == 4 && sarA.ECnt == sarB.ECnt && sarA.FirstVals.Length > 0)
                    {
                        var diffs = SarUtils.CompareElements(sarA, sarB);
                        if (diffs.Count > 0 || slotDiffDetail is not null)
                            changedSars.Add((label, fingerprint, diffs.Count > 0 ? diffs : null, slotDiffDetail));
                        else if (reorderedOnly)
                            movedSars.Add((label, fingerprint));
                    }
                    else if (sarA.ECnt != sarB.ECnt || sarA.ValueSummary != sarB.ValueSummary || bitSlotsChanged)
                    {
                        var detail = $"eCnt={sarA.ECnt}→{sarB.ECnt}  {sarA.ValueSummary} → {sarB.ValueSummary}";
                        if (slotDiffDetail is not null)
                            detail += $"  {slotDiffDetail}";
                        changedSars.Add((label, fingerprint, null, detail));
                    }
                    else if (reorderedOnly)
                    {
                        movedSars.Add((label, fingerprint));
                    }
                }

                for (var index = 0; index < listB.Count; index++)
                {
                    if (!matchedB.Contains(index))
                        appeared.Add((UnmatchedLabel(fingerprint, 'b', index, listA.Count, listB.Count), fingerprint));
                }

                for (var index = 0; index < listA.Count; index++)
                {
                    if (!matchedA.Contains(index))
                        disappeared.Add(
                            (UnmatchedLabel(fingerprint, 'a', index, listA.Count, listB.Count), fingerprint)
                        );
                }
            }

            if (appeared.Count == 0 && disappeared.Count == 0 && movedSars.Count == 0 && changedSars.Count == 0)
                continue;

            Console.WriteLine(
                $"  {snapA.Slot:D4}→{snapB.Slot:D4}  lv{snapA.Level}→{snapB.Level}  bytes={snapA.RawBytesLen}→{snapB.RawBytesLen}"
            );
            Console.WriteLine(
                $"    Σ: new={appeared.Count}  gone={disappeared.Count}  move={movedSars.Count}  chg={changedSars.Count}"
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
                foreach (var (label, fingerprint) in appeared)
                    Console.Write($" {label}({SarUtils.AnnotateFingerprint(fingerprint).TruncateAnnotation()})");
                Console.WriteLine();
            }

            if (disappeared.Count > 0)
            {
                Console.Write("    GONE:");
                foreach (var (label, fingerprint) in disappeared)
                    Console.Write($" {label}({SarUtils.AnnotateFingerprint(fingerprint).TruncateAnnotation()})");
                Console.WriteLine();
            }

            if (movedSars.Count > 0)
            {
                Console.Write("    MOVE:");
                foreach (var (label, fingerprint) in movedSars.Take(12))
                    Console.Write($" {label}({SarUtils.AnnotateFingerprint(fingerprint).TruncateAnnotation()})");
                if (movedSars.Count > 12)
                    Console.Write($" +{movedSars.Count - 12}more");
                Console.WriteLine();
            }

            foreach (var (label, fingerprint, diffs, detail) in changedSars)
            {
                var annotation = SarUtils.AnnotateFingerprint(fingerprint);
                var annotationPart = !string.IsNullOrEmpty(annotation)
                    ? $" [{annotation.TruncateAnnotation()}]"
                    : string.Empty;
                if (diffs is { Count: > 0 })
                {
                    Console.Write($"    CHG: {label}{annotationPart} ");
                    foreach (var (index, valueA, valueB) in diffs.Take(12))
                        Console.Write($" [{index}]:{valueA}→{valueB}");
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
