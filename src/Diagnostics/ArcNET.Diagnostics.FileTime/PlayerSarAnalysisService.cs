namespace ArcNET.Diagnostics;

public static class PlayerSarAnalysisService
{
    public static PlayerSarLifecycleAnalysisSnapshot CreateLifecycleAnalysis(PlayerSarHistorySnapshot history)
    {
        var totalSlots = history.Slots.Count;
        var tracks = history
            .Tracks.Select(track =>
            {
                var presentCount = track.History.Count;
                var firstSlot = track.History[0].Slot;
                var lastSlot = track.History[^1].Slot;
                var elementCounts = track
                    .History.Select(point => point.Sar.ElementCount)
                    .Distinct()
                    .OrderBy(static value => value)
                    .ToList();
                var bitsetWordCounts = track
                    .History.Select(point => point.Sar.BitsetWordCount)
                    .Distinct()
                    .OrderBy(static value => value)
                    .ToList();
                var bitsetIds = track
                    .History.Select(point => point.Sar.BitsetId)
                    .Distinct()
                    .OrderBy(static value => value)
                    .ToList();
                var firstValueSummary = track.History[0].Sar.ValueSummary;
                var lastValueSummary = track.History[^1].Sar.ValueSummary;

                return new PlayerSarLifecycleTrackSummarySnapshot(
                    track.FingerprintKey,
                    track.Fingerprint,
                    presentCount,
                    firstSlot,
                    lastSlot,
                    elementCounts[0],
                    elementCounts[^1],
                    elementCounts.Count > 1 && elementCounts[^1] > elementCounts[0],
                    bitsetWordCounts,
                    bitsetIds,
                    firstValueSummary,
                    lastValueSummary,
                    firstValueSummary != lastValueSummary,
                    CharacterSarDiagnostics.AnnotateSarValue(track.History[0].Sar),
                    ClassifyLifecycle(
                        totalSlots,
                        history.Slots[0].Slot,
                        history.Slots[^1].Slot,
                        presentCount,
                        firstSlot,
                        lastSlot
                    )
                );
            })
            .ToList();

        var fingerprints = tracks
            .GroupBy(track => track.Fingerprint)
            .Select(group =>
            {
                var countsBySnapshot = history
                    .Slots.Select(slot => slot.Sars.Count(sar => !sar.IsFiller && sar.Fingerprint == group.Key))
                    .Where(static count => count > 0)
                    .ToList();

                return new PlayerSarFingerprintAggregateSnapshot(
                    group.Key,
                    CharacterSarDiagnostics.AnnotateFingerprint(group.Key),
                    group.Min(track => track.FirstSlot),
                    group.Max(track => track.LastSlot),
                    countsBySnapshot.Count > 0 ? countsBySnapshot.Min() : 0,
                    countsBySnapshot.Count > 0 ? countsBySnapshot.Max() : 0,
                    group.Count(),
                    group.Count(static track => track.PresentCount > 1),
                    group.Count(static track => track.PresentCount == 1),
                    group.Count(static track => track.ValueChanged)
                );
            })
            .OrderByDescending(static summary => summary.ChangedTrackCount)
            .ThenByDescending(static summary => summary.MaxDuplicateCount)
            .ThenByDescending(static summary => summary.TrackCount)
            .ThenBy(static summary => summary.Fingerprint)
            .ToList();

        return new PlayerSarLifecycleAnalysisSnapshot(totalSlots, tracks, fingerprints);
    }

    public static PlayerSarTransitionAnalysisSnapshot CreateTransitionAnalysis(PlayerSarHistorySnapshot history)
    {
        List<PlayerSarTransitionSnapshot> transitions = [];
        for (var snapshotIndex = 0; snapshotIndex < history.Slots.Count - 1; snapshotIndex++)
        {
            var left = history.Slots[snapshotIndex];
            var right = history.Slots[snapshotIndex + 1];
            var changes = CreateTransitionChanges(left, right);
            if (changes.Count == 0)
                continue;

            transitions.Add(
                new PlayerSarTransitionSnapshot(
                    left.Slot,
                    right.Slot,
                    left.Level,
                    right.Level,
                    left.RawBytesLength,
                    right.RawBytesLength,
                    right.Level < left.Level - 3,
                    changes
                )
            );
        }

        return new PlayerSarTransitionAnalysisSnapshot(transitions);
    }

    private static List<PlayerSarTransitionChangeSnapshot> CreateTransitionChanges(
        PlayerSarSlotSnapshot left,
        PlayerSarSlotSnapshot right
    )
    {
        static string PairLabel(string fingerprint, int indexA, int indexB, int countA, int countB) =>
            countA > 1 || countB > 1 ? $"{fingerprint}[a{indexA + 1}->b{indexB + 1}]" : fingerprint;

        static string UnmatchedLabel(string fingerprint, char side, int index, int countA, int countB) =>
            countA > 1 || countB > 1 ? $"{fingerprint}[{side}{index + 1}]" : fingerprint;

        var groupA = left
            .Sars.Where(static sar => !sar.IsFiller)
            .GroupBy(static sar => sar.Fingerprint)
            .ToDictionary(static group => group.Key, static group => group.ToList());
        var groupB = right
            .Sars.Where(static sar => !sar.IsFiller)
            .GroupBy(static sar => sar.Fingerprint)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        List<PlayerSarTransitionChangeSnapshot> changes = [];
        foreach (var fingerprint in groupA.Keys.Union(groupB.Keys).OrderBy(static value => value))
        {
            var listA = groupA.TryGetValue(fingerprint, out var tmpA) ? tmpA : [];
            var listB = groupB.TryGetValue(fingerprint, out var tmpB) ? tmpB : [];
            var matches = CharacterSarDiagnostics.MatchGroups(listA, listB);
            var matchedA = matches.Select(static match => match.IndexA).ToHashSet();
            var matchedB = matches.Select(static match => match.IndexB).ToHashSet();

            foreach (var match in matches.OrderBy(static match => match.IndexA).ThenBy(static match => match.IndexB))
            {
                var sarA = listA[match.IndexA];
                var sarB = listB[match.IndexB];
                var label = PairLabel(fingerprint, match.IndexA, match.IndexB, listA.Count, listB.Count);
                var bitSlotsChanged = !sarA.BitSlots.SequenceEqual(sarB.BitSlots);
                var reorderedOnly = (listA.Count > 1 || listB.Count > 1) && match.IndexA != match.IndexB;

                if (sarA.ElementSize == 4 && sarA.ElementCount == sarB.ElementCount && sarA.Values.Count > 0)
                {
                    var allDiffs = CharacterSarDiagnostics.CompareElements(sarA, sarB);
                    var (diffs, ptrCount) = CharacterSarDiagnostics.PartitionElementDiffs(allDiffs);
                    if (diffs.Count > 0 || bitSlotsChanged)
                    {
                        changes.Add(
                            new PlayerSarTransitionChangeSnapshot(
                                PlayerSarTransitionChangeKind.Changed,
                                label,
                                fingerprint,
                                CharacterSarDiagnostics.AnnotateSarValue(sarB),
                                sarA.ElementCount,
                                sarB.ElementCount,
                                sarA.ValueSummary,
                                sarB.ValueSummary,
                                [
                                    .. diffs.Select(static diff => new CharacterSarElementValueDiffSnapshot(
                                        diff.Idx,
                                        diff.VA,
                                        diff.VB
                                    )),
                                ],
                                sarA.BitSlots,
                                sarB.BitSlots,
                                ptrCount
                            )
                        );
                    }
                    else if (reorderedOnly)
                    {
                        changes.Add(
                            new PlayerSarTransitionChangeSnapshot(
                                PlayerSarTransitionChangeKind.Moved,
                                label,
                                fingerprint,
                                CharacterSarDiagnostics.AnnotateSarValue(sarB),
                                sarA.ElementCount,
                                sarB.ElementCount,
                                sarA.ValueSummary,
                                sarB.ValueSummary,
                                [],
                                sarA.BitSlots,
                                sarB.BitSlots,
                                0
                            )
                        );
                    }
                }
                else if (
                    sarA.ElementCount != sarB.ElementCount
                    || sarA.ValueSummary != sarB.ValueSummary
                    || bitSlotsChanged
                )
                {
                    changes.Add(
                        new PlayerSarTransitionChangeSnapshot(
                            PlayerSarTransitionChangeKind.Changed,
                            label,
                            fingerprint,
                            CharacterSarDiagnostics.AnnotateSarValue(sarB),
                            sarA.ElementCount,
                            sarB.ElementCount,
                            sarA.ValueSummary,
                            sarB.ValueSummary,
                            [],
                            sarA.BitSlots,
                            sarB.BitSlots,
                            0
                        )
                    );
                }
                else if (reorderedOnly)
                {
                    changes.Add(
                        new PlayerSarTransitionChangeSnapshot(
                            PlayerSarTransitionChangeKind.Moved,
                            label,
                            fingerprint,
                            CharacterSarDiagnostics.AnnotateSarValue(sarB),
                            sarA.ElementCount,
                            sarB.ElementCount,
                            sarA.ValueSummary,
                            sarB.ValueSummary,
                            [],
                            sarA.BitSlots,
                            sarB.BitSlots,
                            0
                        )
                    );
                }
            }

            for (var index = 0; index < listB.Count; index++)
            {
                if (matchedB.Contains(index))
                    continue;

                var sar = listB[index];
                changes.Add(
                    new PlayerSarTransitionChangeSnapshot(
                        PlayerSarTransitionChangeKind.Added,
                        UnmatchedLabel(fingerprint, 'b', index, listA.Count, listB.Count),
                        fingerprint,
                        CharacterSarDiagnostics.AnnotateSarValue(sar),
                        null,
                        sar.ElementCount,
                        null,
                        sar.ValueSummary,
                        [],
                        [],
                        sar.BitSlots,
                        0
                    )
                );
            }

            for (var index = 0; index < listA.Count; index++)
            {
                if (matchedA.Contains(index))
                    continue;

                var sar = listA[index];
                changes.Add(
                    new PlayerSarTransitionChangeSnapshot(
                        PlayerSarTransitionChangeKind.Removed,
                        UnmatchedLabel(fingerprint, 'a', index, listA.Count, listB.Count),
                        fingerprint,
                        CharacterSarDiagnostics.AnnotateSarValue(sar),
                        sar.ElementCount,
                        null,
                        sar.ValueSummary,
                        null,
                        [],
                        sar.BitSlots,
                        [],
                        0
                    )
                );
            }
        }

        return changes;
    }

    private static PlayerSarLifecycleKind ClassifyLifecycle(
        int totalSlots,
        int firstLoadedSlot,
        int lastLoadedSlot,
        int presentCount,
        int firstSlot,
        int lastSlot
    )
    {
        if (presentCount == totalSlots)
            return PlayerSarLifecycleKind.AllSlots;

        if (presentCount == 1)
            return PlayerSarLifecycleKind.OnlySlot;

        var appeared = firstSlot > firstLoadedSlot;
        var disappeared = lastSlot < lastLoadedSlot;
        if (appeared && disappeared)
            return PlayerSarLifecycleKind.AppearedAndDisappeared;
        if (appeared)
            return PlayerSarLifecycleKind.Appeared;
        if (disappeared)
            return PlayerSarLifecycleKind.Disappeared;
        return PlayerSarLifecycleKind.PartialRange;
    }
}
