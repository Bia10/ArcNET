using ArcNET.Editor;

namespace ArcNET.Diagnostics;

public static class SaveCharacterCatalogService
{
    public static SaveCharacterCatalogSnapshot Create(LoadedSave save)
    {
        ArgumentNullException.ThrowIfNull(save);

        List<SaveCharacterCatalogRecordSnapshot> records = [];
        foreach (
            var (path, file) in save.MobileMdys.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            for (var index = 0; index < file.Records.Count; index++)
            {
                var record = file.Records[index];
                if (!record.IsCharacter)
                    continue;

                var character = record.Character!;
                records.Add(
                    new SaveCharacterCatalogRecordSnapshot(
                        path,
                        index,
                        character.HasCompleteData,
                        character.Name,
                        ReadStat(character.Stats, 17),
                        ReadStat(character.Stats, 18),
                        ReadStat(character.Stats, 19),
                        ReadStat(character.Stats, 27),
                        DescribeRace(ReadStat(character.Stats, 27)),
                        ReadStat(character.Stats, 26),
                        DescribeGender(ReadStat(character.Stats, 26)),
                        ReadStat(character.Stats, 22),
                        ReadStat(character.Stats, 23),
                        character.Gold,
                        character.Bullets,
                        character.PowerCells,
                        character.HpDamage,
                        character.FatigueDamage,
                        character.RawBytes.Length,
                        CreateNonZeroBasicSkills(character.BasicSkills)
                    )
                );
            }
        }

        return new SaveCharacterCatalogSnapshot(
            DateTimeOffset.UtcNow,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            records
        );
    }

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateNonZeroBasicSkills(int[] skills)
    {
        List<PlayerIndexedValueSnapshot> result = [];
        for (var index = 0; index < Math.Min(skills.Length, s_basicSkillLabels.Length); index++)
        {
            if (skills[index] > 0)
                result.Add(new PlayerIndexedValueSnapshot(index, s_basicSkillLabels[index], skills[index]));
        }

        return result;
    }

    private static string DescribeRace(int raceId) =>
        raceId >= 0 && raceId < s_raceNames.Length ? s_raceNames[raceId] : $"race={raceId}";

    private static string DescribeGender(int genderId) =>
        genderId >= 0 && genderId < s_genderNames.Length ? s_genderNames[genderId] : $"g={genderId}";

    private static int ReadStat(int[] values, int index) => values.Length > index ? values[index] : 0;

    private static readonly string[] s_raceNames =
    [
        "Human",
        "Halfling",
        "HalfElf",
        "Half-Ogre",
        "Dwarf",
        "Gnome",
        "HalfOrc",
        "?",
        "DarkElf",
        "Elf",
        "?",
    ];

    private static readonly string[] s_genderNames = ["Male", "Female"];

    private static readonly string[] s_basicSkillLabels =
    [
        "BOW",
        "DODGE",
        "MELEE",
        "THROW",
        "BKSTB",
        "PPKT",
        "PROWL",
        "STRAP",
        "GAMBL",
        "HAGGL",
        "HEAL",
        "PERS",
    ];
}
