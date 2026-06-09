namespace ArcNET.Diagnostics;

public static class PlayerSarHistoryService
{
    public static PlayerSarHistorySnapshot Create(
        string saveDir,
        int firstSlot,
        int lastSlot,
        Action<string>? log = null
    )
    {
        List<PlayerSarSlotSnapshot> slots = [];
        for (var slot = firstSlot; slot <= lastSlot; slot++)
        {
            var stem = $"Slot{slot:D4}";
            SaveSlotLoadSnapshot loaded;
            try
            {
                loaded = SaveSlotLoadService.Load(saveDir, slot);
            }
            catch
            {
                log?.Invoke($"  [{stem}] load failed - skipped");
                continue;
            }

            var save = loaded.Save;
            var playerRec = SavePlayerCharacterResolver.Resolve(save)?.Record;
            if (playerRec is null)
                continue;

            var sars = CharacterSarDiagnostics.Parse(playerRec.RawBytes);
            var level = playerRec.Stats.Length > 17 ? playerRec.Stats[17] : -1;
            slots.Add(new PlayerSarSlotSnapshot(slot, level, playerRec.RawBytes.Length, sars, save.Info.LeaderName));
            log?.Invoke(
                $"  [{stem}] {save.Info.LeaderName} lv={level} RawBytes={playerRec.RawBytes.Length}B sars={sars.Count}"
            );
        }

        return new PlayerSarHistorySnapshot(slots, BuildTracks(slots));
    }

    private static IReadOnlyList<PlayerSarTrackSnapshot> BuildTracks(IReadOnlyList<PlayerSarSlotSnapshot> slots)
    {
        static string TrackKey(string fingerprint, int trackIndex) =>
            trackIndex == 0 ? fingerprint : $"{fingerprint}#{trackIndex + 1}";

        List<PlayerSarTrackSnapshot> tracks = [];
        var fingerprints = slots
            .SelectMany(snapshot =>
                snapshot.Sars.Where(static sar => !sar.IsFiller).Select(static sar => sar.Fingerprint)
            )
            .Distinct()
            .OrderBy(static fingerprint => fingerprint);

        foreach (var fingerprint in fingerprints)
        {
            var groupedTracks = new List<List<PlayerSarTrackPointSnapshot>>();
            List<CharacterSarEntrySnapshot> previousEntries = [];
            List<int> previousTrackIds = [];

            foreach (var snapshot in slots)
            {
                var currentEntries = snapshot
                    .Sars.Where(sar => !sar.IsFiller && sar.Fingerprint == fingerprint)
                    .ToList();
                if (currentEntries.Count == 0)
                {
                    previousEntries = [];
                    previousTrackIds = [];
                    continue;
                }

                var currentTrackIds = Enumerable.Repeat(-1, currentEntries.Count).ToArray();
                if (previousEntries.Count == 0)
                {
                    for (var index = 0; index < currentEntries.Count; index++)
                    {
                        groupedTracks.Add([new PlayerSarTrackPointSnapshot(snapshot.Slot, currentEntries[index])]);
                        currentTrackIds[index] = groupedTracks.Count - 1;
                    }
                }
                else
                {
                    var matches = CharacterSarDiagnostics.MatchGroups(previousEntries, currentEntries);
                    foreach (var match in matches)
                    {
                        var trackId = previousTrackIds[match.IndexA];
                        groupedTracks[trackId]
                            .Add(new PlayerSarTrackPointSnapshot(snapshot.Slot, currentEntries[match.IndexB]));
                        currentTrackIds[match.IndexB] = trackId;
                    }

                    for (var index = 0; index < currentEntries.Count; index++)
                    {
                        if (currentTrackIds[index] >= 0)
                            continue;

                        groupedTracks.Add([new PlayerSarTrackPointSnapshot(snapshot.Slot, currentEntries[index])]);
                        currentTrackIds[index] = groupedTracks.Count - 1;
                    }
                }

                previousEntries = currentEntries;
                previousTrackIds = [.. currentTrackIds];
            }

            for (var index = 0; index < groupedTracks.Count; index++)
                tracks.Add(new PlayerSarTrackSnapshot(TrackKey(fingerprint, index), fingerprint, groupedTracks[index]));
        }

        return tracks;
    }
}
