using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using ArcNET.GameObjects.Metadata;

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
            new(0, CharacterSheetMetadata.StatName(0), ReadStat(record, 0)),
            new(1, CharacterSheetMetadata.StatName(1), ReadStat(record, 1)),
            new(2, CharacterSheetMetadata.StatName(2), ReadStat(record, 2)),
            new(3, CharacterSheetMetadata.StatName(3), ReadStat(record, 3)),
            new(4, CharacterSheetMetadata.StatName(4), ReadStat(record, 4)),
            new(5, CharacterSheetMetadata.StatName(5), ReadStat(record, 5)),
            new(6, CharacterSheetMetadata.StatName(6), ReadStat(record, 6)),
            new(7, CharacterSheetMetadata.StatName(7), ReadStat(record, 7)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateDerivedStats(CharacterMdyRecord record) =>
        [
            new(0, CharacterSheetMetadata.StatName(8), ReadStat(record, 8)),
            new(1, CharacterSheetMetadata.StatName(9), ReadStat(record, 9)),
            new(2, CharacterSheetMetadata.StatName(10), ReadStat(record, 10)),
            new(3, CharacterSheetMetadata.StatName(11), ReadStat(record, 11)),
            new(4, CharacterSheetMetadata.StatName(12), ReadStat(record, 12)),
            new(5, CharacterSheetMetadata.StatName(13), ReadStat(record, 13)),
            new(6, CharacterSheetMetadata.StatName(14), ReadStat(record, 14)),
            new(7, CharacterSheetMetadata.StatName(15), ReadStat(record, 15)),
            new(8, CharacterSheetMetadata.StatName(16), ReadStat(record, 16)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateBasicSkills(CharacterMdyRecord record) =>
        [
            new(0, CharacterSheetMetadata.BasicSkillName(0), ReadBasicSkill(record, 0)),
            new(1, CharacterSheetMetadata.BasicSkillName(1), ReadBasicSkill(record, 1)),
            new(2, CharacterSheetMetadata.BasicSkillName(2), ReadBasicSkill(record, 2)),
            new(3, CharacterSheetMetadata.BasicSkillName(3), ReadBasicSkill(record, 3)),
            new(4, CharacterSheetMetadata.BasicSkillName(4), ReadBasicSkill(record, 4)),
            new(5, CharacterSheetMetadata.BasicSkillName(5), ReadBasicSkill(record, 5)),
            new(6, CharacterSheetMetadata.BasicSkillName(6), ReadBasicSkill(record, 6)),
            new(7, CharacterSheetMetadata.BasicSkillName(7), ReadBasicSkill(record, 7)),
            new(8, CharacterSheetMetadata.BasicSkillName(8), ReadBasicSkill(record, 8)),
            new(9, CharacterSheetMetadata.BasicSkillName(9), ReadBasicSkill(record, 9)),
            new(10, CharacterSheetMetadata.BasicSkillName(10), ReadBasicSkill(record, 10)),
            new(11, CharacterSheetMetadata.BasicSkillName(11), ReadBasicSkill(record, 11)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateTechSkills(CharacterMdyRecord record) =>
        [
            new(0, CharacterSheetMetadata.TechSkillName(0), ReadTechSkill(record, 0)),
            new(1, CharacterSheetMetadata.TechSkillName(1), ReadTechSkill(record, 1)),
            new(2, CharacterSheetMetadata.TechSkillName(2), ReadTechSkill(record, 2)),
            new(3, CharacterSheetMetadata.TechSkillName(3), ReadTechSkill(record, 3)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateSpellColleges(CharacterMdyRecord record) =>
        [
            new(0, CharacterSheetMetadata.SpellCollegeName(0), ReadSpellTech(record, 0)),
            new(1, CharacterSheetMetadata.SpellCollegeName(1), ReadSpellTech(record, 1)),
            new(2, CharacterSheetMetadata.SpellCollegeName(2), ReadSpellTech(record, 2)),
            new(3, CharacterSheetMetadata.SpellCollegeName(3), ReadSpellTech(record, 3)),
            new(4, CharacterSheetMetadata.SpellCollegeName(4), ReadSpellTech(record, 4)),
            new(5, CharacterSheetMetadata.SpellCollegeName(5), ReadSpellTech(record, 5)),
            new(6, CharacterSheetMetadata.SpellCollegeName(6), ReadSpellTech(record, 6)),
            new(7, CharacterSheetMetadata.SpellCollegeName(7), ReadSpellTech(record, 7)),
            new(8, CharacterSheetMetadata.SpellCollegeName(8), ReadSpellTech(record, 8)),
            new(9, CharacterSheetMetadata.SpellCollegeName(9), ReadSpellTech(record, 9)),
            new(10, CharacterSheetMetadata.SpellCollegeName(10), ReadSpellTech(record, 10)),
            new(11, CharacterSheetMetadata.SpellCollegeName(11), ReadSpellTech(record, 11)),
            new(12, CharacterSheetMetadata.SpellCollegeName(12), ReadSpellTech(record, 12)),
            new(13, CharacterSheetMetadata.SpellCollegeName(13), ReadSpellTech(record, 13)),
            new(14, CharacterSheetMetadata.SpellCollegeName(14), ReadSpellTech(record, 14)),
            new(15, CharacterSheetMetadata.SpellCollegeName(15), ReadSpellTech(record, 15)),
            new(16, CharacterSheetMetadata.SpellTechSlotName(16), ReadSpellTech(record, 16)),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateTechDisciplines(CharacterMdyRecord record) =>
        [
            new(0, CharacterSheetMetadata.TechDisciplineName(0), ReadSpellTech(record, 17)),
            new(1, CharacterSheetMetadata.TechDisciplineName(1), ReadSpellTech(record, 18)),
            new(2, CharacterSheetMetadata.TechDisciplineName(2), ReadSpellTech(record, 19)),
            new(3, CharacterSheetMetadata.TechDisciplineName(3), ReadSpellTech(record, 20)),
            new(4, CharacterSheetMetadata.TechDisciplineName(4), ReadSpellTech(record, 21)),
            new(5, CharacterSheetMetadata.TechDisciplineName(5), ReadSpellTech(record, 22)),
            new(6, CharacterSheetMetadata.TechDisciplineName(6), ReadSpellTech(record, 23)),
            new(7, CharacterSheetMetadata.TechDisciplineName(7), ReadSpellTech(record, 24)),
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
