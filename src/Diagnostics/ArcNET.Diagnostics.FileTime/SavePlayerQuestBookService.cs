using ArcNET.Editor;
using ArcNET.Formats;

namespace ArcNET.Diagnostics;

public static class SavePlayerQuestBookService
{
    public static PlayerQuestBookSnapshot Create(LoadedSave save, QuestLabelCatalogSnapshot? questCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(save);

        var selected = SavePlayerCharacterResolver.Resolve(save);
        var player = selected is null ? null : CreateCharacterSnapshot(selected.Path, selected.Record, true);

        List<PlayerCharacterSnapshot> questCharacters = [];
        foreach (
            var (path, file) in save.MobileMdys.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            foreach (var record in file.Records)
            {
                if (!record.IsCharacter || record.Character!.QuestCount <= 0)
                    continue;

                questCharacters.Add(
                    CreateCharacterSnapshot(
                        path,
                        record.Character,
                        selected is not null && ReferenceEquals(record.Character, selected.Record)
                    )
                );
            }
        }

        questCharacters =
        [
            .. questCharacters
                .OrderByDescending(static character => character.IsSelectedPlayer)
                .ThenBy(static character => character.Path, StringComparer.OrdinalIgnoreCase),
        ];

        IReadOnlyList<PlayerQuestEntrySnapshot> quests = selected?.Record.QuestEntries is { } entries
            ? [.. entries.Select(entry => CreateQuestEntry(entry, questCatalog))]
            : [];
        IReadOnlyList<PlayerReputationEntrySnapshot> reputation = selected is null
            ? []
            : CreateReputation(selected.Record);

        return new(
            DateTimeOffset.UtcNow,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            questCatalog,
            player,
            questCharacters,
            quests,
            reputation,
            CopyInts(selected?.Record.BlessingRaw),
            CopyInts(selected?.Record.CurseRaw),
            CopyInts(selected?.Record.SchematicsRaw)
        );
    }

    private static PlayerCharacterSnapshot CreateCharacterSnapshot(
        string path,
        CharacterMdyRecord record,
        bool isSelectedPlayer
    ) =>
        new(
            path,
            record.Name,
            ReadStat(record.Stats, 17),
            ReadStat(record.Stats, 27),
            record.RawBytes.Length,
            record.HasCompleteData,
            isSelectedPlayer,
            record.QuestCount,
            record.ReputationRaw?.Length ?? 0,
            record.BlessingProtoElementCount,
            record.CurseProtoElementCount,
            record.SchematicsElementCount,
            record.RumorsCount
        );

    private static PlayerQuestEntrySnapshot CreateQuestEntry(
        (int ProtoId, int Context, int Timestamp, int State) entry,
        QuestLabelCatalogSnapshot? questCatalog
    ) =>
        new(
            entry.ProtoId,
            questCatalog?.Resolve(entry.ProtoId),
            entry.Context,
            entry.Timestamp,
            entry.State,
            QuestStateFormatter.Format(entry.State)
        );

    private static IReadOnlyList<PlayerReputationEntrySnapshot> CreateReputation(CharacterMdyRecord record)
    {
        var reputation = record.ReputationRaw;
        if (reputation is null)
            return [];

        var slots = record.ReputationFactionSlots;
        List<PlayerReputationEntrySnapshot> snapshots = [];
        for (var index = 0; index < reputation.Length; index++)
        {
            snapshots.Add(
                new PlayerReputationEntrySnapshot(slots is { Length: > 0 } ? slots[index] : index, reputation[index])
            );
        }

        return snapshots;
    }

    private static IReadOnlyList<int> CopyInts(int[]? values) => values is null ? [] : [.. values];

    private static int ReadStat(int[] values, int index) => values.Length > index ? values[index] : 0;
}
