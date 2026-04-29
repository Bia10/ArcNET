namespace ArcNET.Editor.Runtime;

/// <summary>
/// Named field layouts for the live character-sheet substructures described by the
/// supplied Cheat Engine table.
/// </summary>
public static class CharacterSheetRuntimeLayout
{
    private static readonly RuntimeFieldDescriptor[] MainStatsFieldsInternal =
    [
        new("Strength", 0x0C),
        new("Dexterity", 0x10),
        new("Constitution", 0x14),
        new("Beauty", 0x18),
        new("Intelligence", 0x1C),
        new("Perception", 0x20),
        new("Willpower", 0x24),
        new("Charisma", 0x28),
        new("Level", 0x50),
        new("Experience", 0x54),
        new("Alignment", 0x58),
        new("FatePoints", 0x5C),
        new("SkillPoints", 0x60),
    ];

    private static readonly RuntimeFieldDescriptor[] BasicSkillsFieldsInternal =
    [
        new("Bow", 0x0C),
        new("Dodge", 0x10),
        new("Melee", 0x14),
        new("Throwing", 0x18),
        new("Backstab", 0x1C),
        new("Pickpocket", 0x20),
        new("Prowling", 0x24),
        new("SpotTrap", 0x28),
        new("Gambling", 0x2C),
        new("Haggle", 0x30),
        new("Heal", 0x34),
        new("Persuasion", 0x38),
    ];

    private static readonly RuntimeFieldDescriptor[] TechSkillsFieldsInternal =
    [
        new("Repair", 0x0C),
        new("Firearms", 0x10),
        new("PickLocks", 0x14),
        new("DisarmTraps", 0x18),
    ];

    private static readonly RuntimeFieldDescriptor[] SpellAndTechFieldsInternal =
    [
        new("Conveyance", 0x0C),
        new("Divination", 0x10),
        new("Air", 0x14),
        new("Earth", 0x18),
        new("Fire", 0x1C),
        new("Water", 0x20),
        new("Force", 0x24),
        new("Mental", 0x28),
        new("Meta", 0x2C),
        new("Morph", 0x30),
        new("Nature", 0x34),
        new("NecroBlack", 0x38),
        new("NecroWhite", 0x3C),
        new("Phantasm", 0x40),
        new("Summoning", 0x44),
        new("Temporal", 0x48),
        new("Herbology", 0x50),
        new("Chemistry", 0x54),
        new("Electric", 0x58),
        new("Explosives", 0x5C),
        new("GunSmithy", 0x60),
        new("Mechanical", 0x64),
        new("Smithy", 0x68),
        new("Therapeutics", 0x6C),
    ];

    /// <summary>Main runtime stats view fields.</summary>
    public static IReadOnlyList<RuntimeFieldDescriptor> MainStatsFields => MainStatsFieldsInternal;

    /// <summary>Basic skill runtime fields.</summary>
    public static IReadOnlyList<RuntimeFieldDescriptor> BasicSkillsFields => BasicSkillsFieldsInternal;

    /// <summary>Tech skill runtime fields.</summary>
    public static IReadOnlyList<RuntimeFieldDescriptor> TechSkillsFields => TechSkillsFieldsInternal;

    /// <summary>Spell colleges and tech disciplines runtime fields.</summary>
    public static IReadOnlyList<RuntimeFieldDescriptor> SpellAndTechFields => SpellAndTechFieldsInternal;
}
