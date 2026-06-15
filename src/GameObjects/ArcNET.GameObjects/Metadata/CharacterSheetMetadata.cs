namespace ArcNET.GameObjects.Metadata;

/// <summary>
/// Stable labels for character stats, skills, spell colleges, tech disciplines, and training ranks.
/// </summary>
public static class CharacterSheetMetadata
{
    public static string StatName(int stat) =>
        stat >= 0 && stat < s_statNames.Length ? s_statNames[stat] : $"Stat[{stat}]";

    public static string BasicSkillName(int index) =>
        index >= 0 && index < s_basicSkillNames.Length ? s_basicSkillNames[index] : $"BasicSkill[{index}]";

    public static string TechSkillName(int index) =>
        index >= 0 && index < s_techSkillNames.Length ? s_techSkillNames[index] : $"TechSkill[{index}]";

    public static string ResistanceName(int index) =>
        index >= 0 && index < s_resistanceNames.Length ? s_resistanceNames[index] : $"Resistance[{index}]";

    public static string SpellCollegeName(int index) =>
        index >= 0 && index < s_spellCollegeNames.Length ? s_spellCollegeNames[index] : $"College[{index}]";

    public static string TechDisciplineName(int index) =>
        index >= 0 && index < s_techDisciplineNames.Length ? s_techDisciplineNames[index] : $"Discipline[{index}]";

    public static string TrainingName(int training) =>
        training >= 0 && training < s_trainingNames.Length ? s_trainingNames[training] : $"Training[{training}]";

    public static string RaceName(int race) =>
        race >= 0 && race < s_raceNames.Length ? s_raceNames[race] : $"Race[{race}]";

    public static string GenderName(int gender) =>
        gender >= 0 && gender < s_genderNames.Length ? s_genderNames[gender] : $"Gender[{gender}]";

    public static string SpellTechSlotName(int index)
    {
        if (index >= 0 && index < s_spellCollegeNames.Length)
            return $"{SpellCollegeName(index)} College";

        if (index == s_spellCollegeNames.Length)
            return "Spell Mastery";

        var techIndex = index - s_spellCollegeNames.Length - 1;
        return techIndex >= 0 && techIndex < s_techDisciplineNames.Length
            ? $"{s_techDisciplineNames[techIndex]} Discipline"
            : $"SpellTech[{index}]";
    }

    private static readonly string[] s_statNames =
    [
        "Strength",
        "Dexterity",
        "Constitution",
        "Beauty",
        "Intelligence",
        "Perception",
        "Willpower",
        "Charisma",
        "CarryWeight",
        "DamageBonus",
        "AcAdjustment",
        "Speed",
        "HealRate",
        "PoisonRecovery",
        "ReactionModifier",
        "MaxFollowers",
        "MagickTechAptitude",
        "Level",
        "ExperiencePoints",
        "Alignment",
        "FatePoints",
        "UnspentPoints",
        "MagickPoints",
        "TechPoints",
        "PoisonLevel",
        "Age",
        "Gender",
        "Race",
    ];

    private static readonly string[] s_basicSkillNames =
    [
        "Bow",
        "Dodge",
        "Melee",
        "Throwing",
        "Backstab",
        "Pick Pocket",
        "Prowling",
        "Spot Trap",
        "Gambling",
        "Haggle",
        "Heal",
        "Persuasion",
    ];

    private static readonly string[] s_techSkillNames = ["Repair", "Firearms", "Pick Locks", "Disarm Traps"];

    private static readonly string[] s_resistanceNames = ["Normal", "Fire", "Electrical", "Poison", "Magic"];

    private static readonly string[] s_spellCollegeNames =
    [
        "Conveyance",
        "Divination",
        "Air",
        "Earth",
        "Fire",
        "Water",
        "Force",
        "Mental",
        "Meta",
        "Morph",
        "Nature",
        "Necromantic Black",
        "Necromantic White",
        "Phantasm",
        "Summoning",
        "Temporal",
    ];

    private static readonly string[] s_techDisciplineNames =
    [
        "Herbology",
        "Chemistry",
        "Electric",
        "Explosives",
        "Gun Smithy",
        "Mechanical",
        "Smithy",
        "Therapeutics",
    ];

    private static readonly string[] s_trainingNames = ["None", "Apprentice", "Expert", "Master"];

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
}
