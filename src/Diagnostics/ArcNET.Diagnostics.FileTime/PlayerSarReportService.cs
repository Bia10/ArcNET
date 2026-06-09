namespace ArcNET.Diagnostics;

public static class PlayerSarReportService
{
    public static PlayerSarLifecycleReportSnapshot CreateLifecycleReport(
        PlayerSarHistorySnapshot history,
        PlayerSarLifecycleAnalysisSnapshot? analysis = null
    )
    {
        analysis ??= PlayerSarAnalysisService.CreateLifecycleAnalysis(history);

        var fingerprintRows = analysis
            .Fingerprints.Where(summary => summary.RecurringTrackCount > 0 || summary.MaxDuplicateCount > 1)
            .Select(static summary => new PlayerSarFingerprintSummaryRowSnapshot(
                summary.Fingerprint,
                summary.Annotation,
                summary.FirstSlot == summary.LastSlot
                    ? $"{summary.FirstSlot:D4}"
                    : $"{summary.FirstSlot:D4}-{summary.LastSlot:D4}",
                summary.MinDuplicateCount == summary.MaxDuplicateCount
                    ? summary.MinDuplicateCount.ToString()
                    : $"{summary.MinDuplicateCount}-{summary.MaxDuplicateCount}",
                summary.TrackCount,
                summary.RecurringTrackCount,
                summary.SingleSlotTrackCount,
                summary.ChangedTrackCount
            ))
            .ToList();

        var recurringFingerprints = fingerprintRows.Select(static row => row.Fingerprint).ToHashSet();
        var detailedTracks = analysis
            .Tracks.Where(track => track.PresentCount > 1 && recurringFingerprints.Contains(track.Fingerprint))
            .OrderByDescending(static track => track.PresentCount)
            .ThenBy(static track => track.FingerprintKey)
            .Select(track => new PlayerSarTrackDetailRowSnapshot(
                track.FingerprintKey,
                track.Fingerprint,
                track.ValueAnnotation,
                FormatLifecycle(
                    track.Lifecycle,
                    track.PresentCount,
                    analysis.TotalSlots,
                    track.FirstSlot,
                    track.LastSlot
                ),
                FormatElementCountRange(track.MinElementCount, track.MaxElementCount, track.ElementCountGrows),
                string.Join("/", track.BitsetWordCounts),
                track.BitsetIds.Count == 1 ? $"0x{track.BitsetIds[0]:X4}" : "varies",
                track.FirstValueSummary,
                track.LastValueSummary,
                track.ValueChanged,
                track.PresentCount
            ))
            .ToList();

        var omittedTrackRows = analysis.Tracks.Count - detailedTracks.Count;
        var omittedSingletonFingerprintCount = analysis
            .Tracks.Where(track => !recurringFingerprints.Contains(track.Fingerprint) && track.PresentCount == 1)
            .Select(static track => track.Fingerprint)
            .Distinct()
            .Count();

        return new PlayerSarLifecycleReportSnapshot(
            fingerprintRows,
            detailedTracks,
            omittedTrackRows,
            omittedSingletonFingerprintCount
        );
    }

    public static PlayerSarTransitionReportSnapshot CreateTransitionReport(PlayerSarTransitionAnalysisSnapshot analysis)
    {
        var transitions = analysis
            .Transitions.Select(transition =>
            {
                var added = transition
                    .Changes.Where(static change => change.Kind == PlayerSarTransitionChangeKind.Added)
                    .Select(static change => new PlayerSarTransitionListEntrySnapshot(
                        change.Label,
                        change.Fingerprint,
                        change.Annotation
                    ))
                    .ToList();
                var removed = transition
                    .Changes.Where(static change => change.Kind == PlayerSarTransitionChangeKind.Removed)
                    .Select(static change => new PlayerSarTransitionListEntrySnapshot(
                        change.Label,
                        change.Fingerprint,
                        change.Annotation
                    ))
                    .ToList();
                var moved = transition
                    .Changes.Where(static change => change.Kind == PlayerSarTransitionChangeKind.Moved)
                    .Select(static change => new PlayerSarTransitionListEntrySnapshot(
                        change.Label,
                        change.Fingerprint,
                        change.Annotation
                    ))
                    .ToList();
                var changed = transition
                    .Changes.Where(static change => change.Kind == PlayerSarTransitionChangeKind.Changed)
                    .Select(change => new PlayerSarTransitionChangedEntrySnapshot(
                        change.Label,
                        change.Fingerprint,
                        change.Annotation,
                        change.BeforeElementCount,
                        change.AfterElementCount,
                        change.BeforeValueSummary,
                        change.AfterValueSummary,
                        [
                            .. change.ElementDiffs.Select(diff => new PlayerSarTransitionFieldDiffSnapshot(
                                CharacterSarDiagnostics.GetElementLabel(change.Fingerprint, diff.Index),
                                diff.BeforeValue,
                                diff.AfterValue
                            )),
                        ],
                        change.BeforeBitSlots,
                        change.AfterBitSlots,
                        change.PointerNoiseSuppressedCount
                    ))
                    .ToList();

                return new PlayerSarTransitionReportEntrySnapshot(
                    transition.FromSlot,
                    transition.ToSlot,
                    transition.FromLevel,
                    transition.ToLevel,
                    transition.FromRawBytesLength,
                    transition.ToRawBytesLength,
                    transition.IsDiscontinuous,
                    new PlayerSarTransitionSummarySnapshot(
                        added.Count,
                        removed.Count,
                        moved.Count,
                        changed.Count,
                        SummarizeFingerprints(moved.Select(static change => change.Fingerprint)),
                        SummarizeFingerprints(changed.Select(static change => change.Fingerprint))
                    ),
                    added,
                    removed,
                    moved,
                    changed
                );
            })
            .ToList();

        return new PlayerSarTransitionReportSnapshot(transitions);
    }

    private static IReadOnlyList<PlayerSarFingerprintCountSnapshot> SummarizeFingerprints(
        IEnumerable<string> fingerprints,
        int maxShow = 4
    ) =>
        fingerprints
            .GroupBy(static fingerprint => fingerprint)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key)
            .Take(maxShow)
            .Select(static group => new PlayerSarFingerprintCountSnapshot(group.Key, group.Count()))
            .ToList();

    private static string FormatLifecycle(
        PlayerSarLifecycleKind lifecycle,
        int presentCount,
        int totalSlots,
        int firstSlot,
        int lastSlot
    ) =>
        lifecycle switch
        {
            PlayerSarLifecycleKind.AllSlots => "ALL",
            PlayerSarLifecycleKind.OnlySlot => $"only@{firstSlot:D4}",
            PlayerSarLifecycleKind.AppearedAndDisappeared => $"NEW@{firstSlot:D4}/GONE@{lastSlot:D4}",
            PlayerSarLifecycleKind.Appeared => $"NEW@{firstSlot:D4}",
            PlayerSarLifecycleKind.Disappeared => $"GONE@{lastSlot:D4}",
            _ => $"{presentCount}/{totalSlots}",
        };

    private static string FormatElementCountRange(int minElementCount, int maxElementCount, bool grows)
    {
        var range =
            minElementCount == maxElementCount ? minElementCount.ToString() : $"{minElementCount}-{maxElementCount}";
        return grows ? range + " ↑" : range;
    }
}
