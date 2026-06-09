using ArcNET.Editor;
using ArcNET.Formats;

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
        var character = CharacterRecord.From(record);

        return new SavePlayerCharacterSummarySnapshot(
            DateTimeOffset.UtcNow,
            save.Info.LeaderName,
            save.Info.LeaderLevel,
            resolution.Path,
            record.HasCompleteData,
            CreatePrimaryAttributes(character),
            CreateDerivedStats(character),
            new PlayerCharacterProgressionSnapshot(
                character.Name,
                character.Level,
                character.ExperiencePoints,
                character.Alignment,
                character.FatePoints,
                character.Race,
                character.Gender,
                character.Age,
                character.PoisonLevel,
                character.UnspentPoints,
                character.MagickPoints,
                character.TechPoints,
                character.Gold,
                character.Arrows,
                character.TotalKills,
                character.Bullets,
                character.PowerCells,
                character.HpDamage,
                character.FatigueDamage
            ),
            CreateBasicSkills(character),
            CreateTechSkills(character),
            CreateSpellColleges(character),
            CreateTechDisciplines(character),
            new SaveQuestLogSummarySnapshot(
                character.QuestCount,
                character.QuestDataRaw?.Length,
                character.QuestBitsetRaw?.Length
            ),
            CreateReputation(record),
            CopyInts(character.BlessingRaw),
            CopyInts(character.CurseRaw),
            CopyInts(character.SchematicsRaw),
            new SaveRumorSummarySnapshot(character.RumorsCount, character.RumorsRaw?.Length)
        );
    }

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreatePrimaryAttributes(CharacterRecord character) =>
        [
            new(0, "STR", character.Strength),
            new(1, "DEX", character.Dexterity),
            new(2, "CON", character.Constitution),
            new(3, "BEA", character.Beauty),
            new(4, "INT", character.Intelligence),
            new(5, "PER", character.Perception),
            new(6, "WIL", character.Willpower),
            new(7, "CHA", character.Charisma),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateDerivedStats(CharacterRecord character) =>
        [
            new(0, "carry", character.CarryWeight),
            new(1, "dmgBonus", character.DamageBonus),
            new(2, "acAdj", character.AcAdjustment),
            new(3, "speed", character.Speed),
            new(4, "healRate", character.HealRate),
            new(5, "poisonRec", character.PoisonRecovery),
            new(6, "reactMod", character.ReactionModifier),
            new(7, "maxFoll", character.MaxFollowers),
            new(8, "mtApt", character.MagickTechAptitude),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateBasicSkills(CharacterRecord character) =>
        [
            new(0, "bow", character.SkillBow),
            new(1, "dodge", character.SkillDodge),
            new(2, "melee", character.SkillMelee),
            new(3, "throw", character.SkillThrowing),
            new(4, "backstab", character.SkillBackstab),
            new(5, "pickpocket", character.SkillPickPocket),
            new(6, "prowl", character.SkillProwling),
            new(7, "spotTrap", character.SkillSpotTrap),
            new(8, "gamble", character.SkillGambling),
            new(9, "haggle", character.SkillHaggle),
            new(10, "heal", character.SkillHeal),
            new(11, "persuade", character.SkillPersuasion),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateTechSkills(CharacterRecord character) =>
        [
            new(0, "repair", character.SkillRepair),
            new(1, "firearms", character.SkillFirearms),
            new(2, "pickLocks", character.SkillPickLocks),
            new(3, "disarmTraps", character.SkillDisarmTraps),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateSpellColleges(CharacterRecord character) =>
        [
            new(0, "conv", character.SpellConveyance),
            new(1, "div", character.SpellDivination),
            new(2, "air", character.SpellAir),
            new(3, "earth", character.SpellEarth),
            new(4, "fire", character.SpellFire),
            new(5, "water", character.SpellWater),
            new(6, "force", character.SpellForce),
            new(7, "mental", character.SpellMental),
            new(8, "meta", character.SpellMeta),
            new(9, "morph", character.SpellMorph),
            new(10, "nature", character.SpellNature),
            new(11, "necroBlk", character.SpellNecroBlack),
            new(12, "necroWht", character.SpellNecroWhite),
            new(13, "phantasm", character.SpellPhantasm),
            new(14, "summon", character.SpellSummoning),
            new(15, "temporal", character.SpellTemporal),
            new(16, "mastery", character.SpellMastery),
        ];

    private static IReadOnlyList<PlayerIndexedValueSnapshot> CreateTechDisciplines(CharacterRecord character) =>
        [
            new(0, "herb", character.TechHerbology),
            new(1, "chem", character.TechChemistry),
            new(2, "elec", character.TechElectric),
            new(3, "exp", character.TechExplosives),
            new(4, "gun", character.TechGun),
            new(5, "mech", character.TechMechanical),
            new(6, "smith", character.TechSmithy),
            new(7, "therap", character.TechTherapeutics),
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

    private static IReadOnlyList<int> CopyInts(int[]? values) => values is null ? [] : [.. values];
}
