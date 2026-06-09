using ArcNET.Editor;

namespace ArcNET.Diagnostics;

public static class SaveTypedContextService
{
    public static SaveTypedContextSnapshot Create(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var questBook = SavePlayerQuestBookService.Create(save);
        return new SaveTypedContextSnapshot(DateTimeOffset.UtcNow, CreatePlayer(questBook), CreateTownMapFogs(save));
    }

    private static SaveTypedPlayerStateSnapshot? CreatePlayer(PlayerQuestBookSnapshot snapshot)
    {
        if (snapshot.Player is null)
            return null;

        IReadOnlyDictionary<int, int>? reputation =
            snapshot.Player.ReputationCount == 0
                ? null
                : snapshot.Reputation.ToDictionary(static entry => entry.Slot, static entry => entry.Value);

        return new SaveTypedPlayerStateSnapshot(
            snapshot.Player.QuestCount,
            snapshot.Player.RumorsCount,
            snapshot.Player.BlessingCount,
            snapshot.Player.CurseCount,
            snapshot.Player.SchematicsCount,
            reputation
        );
    }

    private static SaveTownMapFogSnapshot CreateTownMapFogs(LoadedSave save)
    {
        Dictionary<string, SaveTownMapFogFileSnapshot> files = new(StringComparer.OrdinalIgnoreCase);
        var revealedTiles = 0;
        foreach (var (path, fog) in save.TownMapFogs)
        {
            files[path] = new SaveTownMapFogFileSnapshot(fog.RawBytes, fog.RevealedTiles);
            revealedTiles += fog.RevealedTiles;
        }

        return new SaveTownMapFogSnapshot(files.Count, revealedTiles, files);
    }
}
