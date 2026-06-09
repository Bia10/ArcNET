namespace ArcNET.Diagnostics;

public static class SaveTypedContextAnalysisService
{
    public static SaveTypedContextOverviewSnapshot CreateOverview(
        SaveTypedPlayerStateSnapshot? player,
        SaveTownMapFogSnapshot townMapFogs
    ) =>
        player is { } state
            ? new SaveTypedContextOverviewSnapshot(
                true,
                state.QuestCount,
                state.RumorsCount,
                state.Blessings,
                state.Curses,
                state.Schematics,
                state.Reputation?.Count,
                townMapFogs.FileCount,
                townMapFogs.RevealedTiles
            )
            : new SaveTypedContextOverviewSnapshot(
                false,
                0,
                0,
                0,
                0,
                0,
                null,
                townMapFogs.FileCount,
                townMapFogs.RevealedTiles
            );

    public static SaveTypedContextDeltaSnapshot CreateDelta(
        SaveTypedPlayerStateSnapshot? beforePlayer,
        SaveTownMapFogSnapshot beforeTownMapFogs,
        SaveTypedPlayerStateSnapshot? afterPlayer,
        SaveTownMapFogSnapshot afterTownMapFogs
    ) => new(CreatePlayerDelta(beforePlayer, afterPlayer), CreateTownMapFogDelta(beforeTownMapFogs, afterTownMapFogs));

    private static SaveTypedPlayerDeltaSnapshot CreatePlayerDelta(
        SaveTypedPlayerStateSnapshot? beforePlayer,
        SaveTypedPlayerStateSnapshot? afterPlayer
    )
    {
        if (beforePlayer is null && afterPlayer is not null)
            return new SaveTypedPlayerDeltaSnapshot(
                SaveTypedPlayerDeltaKind.Added,
                0,
                0,
                0,
                0,
                0,
                new SaveTypedReputationDeltaSnapshot(
                    afterPlayer.Reputation is null
                        ? SaveTypedReputationDeltaKind.Absent
                        : SaveTypedReputationDeltaKind.Added,
                    afterPlayer.Reputation?.Count ?? 0,
                    []
                )
            );

        if (beforePlayer is not null && afterPlayer is null)
            return new SaveTypedPlayerDeltaSnapshot(
                SaveTypedPlayerDeltaKind.Removed,
                0,
                0,
                0,
                0,
                0,
                new SaveTypedReputationDeltaSnapshot(
                    beforePlayer.Reputation is null
                        ? SaveTypedReputationDeltaKind.Absent
                        : SaveTypedReputationDeltaKind.Removed,
                    beforePlayer.Reputation?.Count ?? 0,
                    []
                )
            );

        if (beforePlayer is null || afterPlayer is null)
            return new SaveTypedPlayerDeltaSnapshot(
                SaveTypedPlayerDeltaKind.Missing,
                0,
                0,
                0,
                0,
                0,
                new SaveTypedReputationDeltaSnapshot(SaveTypedReputationDeltaKind.Absent, 0, [])
            );

        return new SaveTypedPlayerDeltaSnapshot(
            SaveTypedPlayerDeltaKind.Changed,
            afterPlayer.QuestCount - beforePlayer.QuestCount,
            afterPlayer.RumorsCount - beforePlayer.RumorsCount,
            afterPlayer.Blessings - beforePlayer.Blessings,
            afterPlayer.Curses - beforePlayer.Curses,
            afterPlayer.Schematics - beforePlayer.Schematics,
            CreateReputationDelta(beforePlayer.Reputation, afterPlayer.Reputation)
        );
    }

    private static SaveTypedReputationDeltaSnapshot CreateReputationDelta(
        IReadOnlyDictionary<int, int>? before,
        IReadOnlyDictionary<int, int>? after
    )
    {
        if (before is null && after is null)
            return new SaveTypedReputationDeltaSnapshot(SaveTypedReputationDeltaKind.Absent, 0, []);

        if (before is null)
            return new SaveTypedReputationDeltaSnapshot(SaveTypedReputationDeltaKind.Added, after!.Count, []);

        if (after is null)
            return new SaveTypedReputationDeltaSnapshot(SaveTypedReputationDeltaKind.Removed, before.Count, []);

        List<int> changedSlots = [];
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
        return new SaveTypedReputationDeltaSnapshot(
            SaveTypedReputationDeltaKind.Changed,
            changedSlots.Count,
            changedSlots
        );
    }

    private static SaveTownMapFogDeltaSnapshot CreateTownMapFogDelta(
        SaveTownMapFogSnapshot before,
        SaveTownMapFogSnapshot after
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

        return new SaveTownMapFogDeltaSnapshot(changedFiles, after.RevealedTiles - before.RevealedTiles);
    }
}
