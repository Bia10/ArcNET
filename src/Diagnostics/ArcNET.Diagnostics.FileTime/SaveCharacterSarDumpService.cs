using ArcNET.GameData.SaveGames;

namespace ArcNET.Diagnostics;

public static class SaveCharacterSarDumpService
{
    public static SaveCharacterSarDumpSnapshot Create(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        List<SaveCharacterSarRecordSnapshot> records = [];
        foreach (
            var (path, file) in save.MobileMdys.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            foreach (var record in file.Records)
            {
                if (!record.IsCharacter)
                    continue;

                var character = record.Character!;
                records.Add(
                    new SaveCharacterSarRecordSnapshot(
                        path,
                        character.HasCompleteData,
                        character.Name,
                        ReadStat(character.Stats, 17),
                        character.RawBytes.Length,
                        character.Gold,
                        character.Arrows,
                        character.Bullets,
                        character.PowerCells,
                        character.TotalKills,
                        character.PortraitIndex,
                        character.MaxFollowers,
                        character.HpDamage,
                        character.FatigueDamage,
                        character.QuestCount,
                        character.QuestDataRaw?.Length,
                        character.QuestBitsetRaw?.Length,
                        CreateQuestSlotIds(character.QuestBitsetRaw),
                        CopyInts(character.ReputationRaw),
                        CopyInts(character.BlessingRaw),
                        CopyInts(character.CurseRaw),
                        CopyInts(character.SchematicsRaw),
                        CharacterSarDiagnostics.CreateDumpEntries(character.RawBytes)
                    )
                );
            }
        }

        return new SaveCharacterSarDumpSnapshot(
            DateTimeOffset.UtcNow,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            records
        );
    }

    private static IReadOnlyList<int> CreateQuestSlotIds(int[]? questBitset)
    {
        if (questBitset is null)
            return [];

        List<int> activeSlots = [];
        for (var wordIndex = 0; wordIndex < questBitset.Length; wordIndex++)
        {
            for (var bitIndex = 0; bitIndex < 32; bitIndex++)
            {
                if ((questBitset[wordIndex] & (1 << bitIndex)) != 0)
                    activeSlots.Add(wordIndex * 32 + bitIndex);
            }
        }

        return activeSlots;
    }

    private static IReadOnlyList<int> CopyInts(int[]? values) => values is null ? [] : [.. values];

    private static int ReadStat(int[] values, int index) => values.Length > index ? values[index] : 0;
}
