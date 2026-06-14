using ArcNET.Formats;
using ArcNET.GameData.SaveGames;

namespace ArcNET.Diagnostics;

public static class SavePlayerCharacterSummaryService
{
    public static SavePlayerCharacterSummarySnapshot? Create(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var resolution = SavePlayerCharacterResolver.Resolve(save);
        if (resolution is null)
            return null;

        var record = resolution.Record;
        return new SavePlayerCharacterSummarySnapshot(
            DateTimeOffset.UtcNow,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            resolution.Path,
            record.HasCompleteData,
            CreatePrimaryAttributes(record),
            CreateDerivedStats(record),
            new PlayerCharacterProgressionSnapshot(
                record.Name,
                ReadStat(record, 17),
                ReadStat(record, 18),
                ReadStat(record, 19),
                ReadStat(record, 20),
                ReadStat(record, 27),
                ReadStat(record, 26),
                ReadStat(record, 25),
                ReadStat(record, 24),
                ReadStat(record, 21),
                ReadStat(record, 22),
                ReadStat(record, 23),
                record.Gold,
                record.Arrows,
                record.TotalKills,
                record.Bullets,
                record.PowerCells,
                record.HpDamage,
                record.FatigueDamage
            ),
            CreateBasicSkills(record),
            CreateTechSkills(record),
            CreateSpellColleges(record),
            CreateTechDisciplines(record),
            new SaveQuestLogSummarySnapshot(
                record.QuestCount,
                record.QuestDataRaw?.Length,
                record.QuestBitsetRaw?.Length
            ),
            CreateReputation(record),
            CopyInts(record.BlessingRaw),
            CopyInts(record.CurseRaw),
            CopyInts(record.SchematicsRaw),
            new SaveRumorSummarySnapshot(record.RumorsCount, record.RumorsRaw?.Length)
        );
    }

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreatePrimaryAttributes(CharacterMdyRecord record) =>
        [
            new(0, "STR", ReadStat(record, 0)),
            new(1, "DEX", ReadStat(record, 1)),
            new(2, "CON", ReadStat(record, 2)),
            new(3, "BEA", ReadStat(record, 3)),
            new(4, "INT", ReadStat(record, 4)),
            new(5, "PER", ReadStat(record, 5)),
            new(6, "WIL", ReadStat(record, 6)),
            new(7, "CHA", ReadStat(record, 7)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateDerivedStats(CharacterMdyRecord record) =>
        [
            new(0, "carry", ReadStat(record, 8)),
            new(1, "dmgBonus", ReadStat(record, 9)),
            new(2, "acAdj", ReadStat(record, 10)),
            new(3, "speed", ReadStat(record, 11)),
            new(4, "healRate", ReadStat(record, 12)),
            new(5, "poisonRec", ReadStat(record, 13)),
            new(6, "reactMod", ReadStat(record, 14)),
            new(7, "maxFoll", ReadStat(record, 15)),
            new(8, "mtApt", ReadStat(record, 16)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateBasicSkills(CharacterMdyRecord record) =>
        [
            new(0, "bow", ReadBasicSkill(record, 0)),
            new(1, "dodge", ReadBasicSkill(record, 1)),
            new(2, "melee", ReadBasicSkill(record, 2)),
            new(3, "throw", ReadBasicSkill(record, 3)),
            new(4, "backstab", ReadBasicSkill(record, 4)),
            new(5, "pickpocket", ReadBasicSkill(record, 5)),
            new(6, "prowl", ReadBasicSkill(record, 6)),
            new(7, "spotTrap", ReadBasicSkill(record, 7)),
            new(8, "gamble", ReadBasicSkill(record, 8)),
            new(9, "haggle", ReadBasicSkill(record, 9)),
            new(10, "heal", ReadBasicSkill(record, 10)),
            new(11, "persuade", ReadBasicSkill(record, 11)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateTechSkills(CharacterMdyRecord record) =>
        [
            new(0, "repair", ReadTechSkill(record, 0)),
            new(1, "firearms", ReadTechSkill(record, 1)),
            new(2, "pickLocks", ReadTechSkill(record, 2)),
            new(3, "disarmTraps", ReadTechSkill(record, 3)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateSpellColleges(CharacterMdyRecord record) =>
        [
            new(0, "conv", ReadSpellTech(record, 0)),
            new(1, "div", ReadSpellTech(record, 1)),
            new(2, "air", ReadSpellTech(record, 2)),
            new(3, "earth", ReadSpellTech(record, 3)),
            new(4, "fire", ReadSpellTech(record, 4)),
            new(5, "water", ReadSpellTech(record, 5)),
            new(6, "force", ReadSpellTech(record, 6)),
            new(7, "mental", ReadSpellTech(record, 7)),
            new(8, "meta", ReadSpellTech(record, 8)),
            new(9, "morph", ReadSpellTech(record, 9)),
            new(10, "nature", ReadSpellTech(record, 10)),
            new(11, "necroBlk", ReadSpellTech(record, 11)),
            new(12, "necroWht", ReadSpellTech(record, 12)),
            new(13, "phantasm", ReadSpellTech(record, 13)),
            new(14, "summon", ReadSpellTech(record, 14)),
            new(15, "temporal", ReadSpellTech(record, 15)),
            new(16, "mastery", ReadSpellTech(record, 16)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateTechDisciplines(CharacterMdyRecord record) =>
        [
            new(0, "herb", ReadSpellTech(record, 17)),
            new(1, "chem", ReadSpellTech(record, 18)),
            new(2, "elec", ReadSpellTech(record, 19)),
            new(3, "exp", ReadSpellTech(record, 20)),
            new(4, "gun", ReadSpellTech(record, 21)),
            new(5, "mech", ReadSpellTech(record, 22)),
            new(6, "smith", ReadSpellTech(record, 23)),
            new(7, "therap", ReadSpellTech(record, 24)),
        ];

    private static IReadOnlyList<PlayerReputationEntrySnapshot> CreateReputation(CharacterMdyRecord record)
    {
        var reputation = record.ReputationRaw;
        if (reputation is null)
            return [];

        var slots = record.ReputationFactionSlots;
        List<PlayerReputationEntrySnapshot> entries = [];
        for (var index = 0; index < reputation.Length; index++)
        {
            entries.Add(
                new PlayerReputationEntrySnapshot(slots is { Length: > 0 } ? slots[index] : index, reputation[index])
            );
        }

        return entries;
    }

    private static int ReadStat(CharacterMdyRecord record, int index) => ReadIndexed(record.Stats, index);

    private static int ReadBasicSkill(CharacterMdyRecord record, int index) => ReadIndexed(record.BasicSkills, index);

    private static int ReadTechSkill(CharacterMdyRecord record, int index) => ReadIndexed(record.TechSkills, index);

    private static int ReadSpellTech(CharacterMdyRecord record, int index) => ReadIndexed(record.SpellTech, index);

    private static int ReadIndexed(int[] values, int index) => index < values.Length ? values[index] : 0;

    private static IReadOnlyList<int> CopyInts(int[]? values) => values is null ? [] : [.. values];
}
