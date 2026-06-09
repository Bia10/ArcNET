using ArcNET.Editor;
using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public static class SavePlayerCharacterResolver
{
    public static SavePlayerCharacterResolution? Resolve(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        List<SavePlayerCharacterResolution> candidates = [];
        foreach (var (path, file) in save.MobileMdys)
        {
            foreach (var record in file.Records)
            {
                if (record.IsCharacter)
                    candidates.Add(new SavePlayerCharacterResolution(path, record.Character!));
            }
        }

        if (candidates.Count == 0)
            return null;

        bool LevelMatches(SavePlayerCharacterResolution candidate) =>
            candidate.Record.Stats.Length > 17 && candidate.Record.Stats[17] == save.Info.LeaderLevel;

        var exactQuestMatch = candidates.FirstOrDefault(candidate =>
            candidate.Record.QuestCount > 0 && LevelMatches(candidate)
        );
        if (exactQuestMatch is not null)
            return exactQuestMatch;

        var questCandidates = candidates.Where(static candidate => candidate.Record.QuestCount > 0).ToArray();
        if (questCandidates.Length > 0)
            return questCandidates.FirstOrDefault(LevelMatches)
                ?? questCandidates.MaxBy(static candidate => candidate.Record.QuestCount)!;

        var reputationMatch = candidates.FirstOrDefault(candidate =>
            candidate.Record.ReputationRaw is not null && LevelMatches(candidate)
        );
        if (reputationMatch is not null)
            return reputationMatch;

        var namedLevelMatch = candidates.FirstOrDefault(candidate =>
            candidate.Record.Name is { Length: > 0 } && LevelMatches(candidate)
        );
        if (namedLevelMatch is not null)
            return namedLevelMatch;

        var namedCandidate = candidates.FirstOrDefault(static candidate => candidate.Record.Name is { Length: > 0 });
        if (namedCandidate is not null)
            return namedCandidate;

        return candidates.Where(LevelMatches).MaxBy(static candidate => candidate.Record.RawBytes.Length)
            ?? candidates.MaxBy(static candidate => candidate.Record.RawBytes.Length);
    }
}
