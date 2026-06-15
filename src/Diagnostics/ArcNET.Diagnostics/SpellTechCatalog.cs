using System.Globalization;
using System.Linq;
using ArcNET.GameObjects.Metadata;

namespace ArcNET.Diagnostics;

public static class SpellTechCatalog
{
    public static IReadOnlyList<SpellDescriptor> EnumerateSpells() => [.. s_spells];

    public static IReadOnlyList<string> EnumerateSpellCollegeNames() =>
        [.. Enumerable.Range(0, SpellCollegeCount).Select(SpellCollegeName)];

    public static IReadOnlyList<string> EnumerateTechDisciplineNames() =>
        [.. Enumerable.Range(0, TechDisciplineCount).Select(CharacterSheetMetadata.TechDisciplineName)];

    public static IReadOnlyList<string> EnumerateTechSkillNames() =>
        [.. Enumerable.Range(0, TechSkillCount).Select(CharacterSheetMetadata.TechSkillName)];

    public static SpellDescriptor ParseSpell(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Enter one spell id or spell name such as teleportation or harm.");

        var trimmed = token.Trim();
        if (TryParseInt32(trimmed, out var numericId))
        {
            if (numericId >= 0 && numericId < s_spells.Length)
                return s_spells[numericId];

            throw new InvalidOperationException(
                $"Spell id '{token}' is outside the supported range 0-{(s_spells.Length - 1).ToString(CultureInfo.InvariantCulture)}."
            );
        }

        var normalized = Normalize(trimmed);
        if (s_spellsByAlias.TryGetValue(normalized, out var spell))
            return spell;

        throw new InvalidOperationException(
            $"Unknown spell '{token}'. Try a spell id between 0 and {(s_spells.Length - 1).ToString(CultureInfo.InvariantCulture)} or a name like teleportation, harm, or tempus-fugit."
        );
    }

    public static int ParseSpellCollegeId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "Enter one spell college such as fire, temporal, or necromantic-white."
            );

        var normalized = Normalize(token);
        for (var index = 0; index < SpellCollegeCount; index++)
        {
            if (Normalize(CharacterSheetMetadata.SpellCollegeName(index)) == normalized)
                return index;
        }

        if (TryParseInt32(token.Trim(), out var numericId) && numericId >= 0 && numericId < SpellCollegeCount)
            return numericId;

        throw new InvalidOperationException(
            $"Unknown spell college '{token}'. Try names like fire, conveyance, or temporal, or a numeric id between 0 and 15."
        );
    }

    public static int ParseTechDisciplineId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "Enter one tech discipline such as mechanical, herbology, or therapeutics."
            );

        var normalized = Normalize(token);
        if (s_techDisciplineAliases.TryGetValue(normalized, out var aliasedDiscipline))
            return aliasedDiscipline;

        for (var index = 0; index < TechDisciplineCount; index++)
        {
            if (Normalize(CharacterSheetMetadata.TechDisciplineName(index)) == normalized)
                return index;
        }

        if (TryParseInt32(token.Trim(), out var numericId) && numericId >= 0 && numericId < TechDisciplineCount)
            return numericId;

        throw new InvalidOperationException(
            $"Unknown tech discipline '{token}'. Try names like chemistry, mechanical, or gun-smithy, or a numeric id between 0 and 7."
        );
    }

    public static int ParseTechSkillId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Enter one tech skill such as repair, firearms, or disarm-traps.");

        var normalized = Normalize(token);
        if (s_techSkillAliases.TryGetValue(normalized, out var aliasedSkill))
            return aliasedSkill;

        for (var index = 0; index < TechSkillCount; index++)
        {
            if (Normalize(CharacterSheetMetadata.TechSkillName(index)) == normalized)
                return index;
        }

        if (TryParseInt32(token.Trim(), out var numericId) && numericId >= 0 && numericId < TechSkillCount)
            return numericId;

        throw new InvalidOperationException(
            $"Unknown tech skill '{token}'. Try names like repair, firearms, pick-locks, or disarm-traps, or a numeric id between 0 and 3."
        );
    }

    public static string SpellCollegeName(int collegeId) => CharacterSheetMetadata.SpellCollegeName(collegeId);

    public static string TechDisciplineName(int disciplineId) =>
        CharacterSheetMetadata.TechDisciplineName(disciplineId);

    public static string TechSkillName(int skillId) => CharacterSheetMetadata.TechSkillName(skillId);

    public static int ParseLevel(string? token, string label, int minimum, int maximumInclusive)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException($"{label} is required.");

        if (
            TryParseInt32(token.Trim(), out var numericValue)
            && numericValue >= minimum
            && numericValue <= maximumInclusive
        )
            return numericValue;

        throw new InvalidOperationException(
            $"{label} must be between {minimum.ToString(CultureInfo.InvariantCulture)} and {maximumInclusive.ToString(CultureInfo.InvariantCulture)}."
        );
    }

    public static int ParseSchematicId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Enter one schematic id such as 4000 or 4410.");

        if (TryParseInt32(token.Trim(), out var numericValue) && numericValue > 0)
            return numericValue;

        throw new InvalidOperationException($"Schematic id '{token}' is not a valid positive integer.");
    }

    public static int ParseTechSkillPoints(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Enter one tech skill point value between 0 and 63.");

        if (
            TryParseInt32(token.Trim(), out var numericValue)
            && numericValue >= 0
            && numericValue <= TechSkillPointMask
        )
            return numericValue;

        throw new InvalidOperationException("Tech skill points must be between 0 and 63.");
    }

    public static string Normalize(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private static bool TryParseInt32(string token, out int value)
    {
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return int.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static SpellDescriptor[] BuildSpells()
    {
        var rawNames = new[]
        {
            "DISARM",
            "UNLOCKING_CANTRIP",
            "UNSEEN_FORCE",
            "SPATIAL_DISTORTION",
            "TELEPORTATION",
            "SENSE_ALIGNMENT",
            "SEE_CONTENTS",
            "READ_AURA",
            "SENSE_HIDDEN",
            "DIVINE_MAGICK",
            "VITALITY_OF_AIR",
            "POISON_VAPOURS",
            "CALL_WINDS",
            "BODY_OF_AIR",
            "CALL_AIR_ELEMENTAL",
            "STRENGTH_OF_EARTH",
            "STONE_THROW",
            "WALL_OF_STONE",
            "BODY_OF_STONE",
            "CALL_EARTH_ELEMENTAL",
            "AGILITY_OF_FIRE",
            "WALL_OF_FIRE",
            "FIREFLASH",
            "BODY_OF_FIRE",
            "CALL_FIRE_ELEMENTAL",
            "PURITY_OF_WATER",
            "CALL_FOG",
            "SQUALL_OF_ICE",
            "BODY_OF_WATER",
            "CALL_WATER_ELEMENTAL",
            "SHIELD_OF_PROTECTION",
            "JOLT",
            "WALL_OF_FORCE",
            "BOLT_OF_LIGHTNING",
            "DISINTEGRATE",
            "CHARM",
            "STUN",
            "DRAIN_WILL",
            "NIGHTMARE",
            "DOMINATE_WILL",
            "RESIST_MAGICK",
            "DISPERSE_MAGICK",
            "DWEOMER_SHIELD",
            "BONDS_OF_MAGICK",
            "REFLECTION_SHIELD",
            "HARDENED_HANDS",
            "WEAKEN",
            "SHRINK",
            "FLESH_TO_STONE",
            "POLYMORPH",
            "CHARM_BEAST",
            "ENTANGLE",
            "CONTROL_BEAST",
            "SUCCOUR_BEAST",
            "REGENERATE",
            "HARM",
            "CONJURE_SPIRIT",
            "SUMMON_UNDEAD",
            "CREATE_UNDEAD",
            "QUENCH_LIFE",
            "MINOR_HEALING",
            "HALT_POISON",
            "MAJOR_HEALING",
            "SANCTUARY",
            "RESURRECT",
            "ILLUMINATE",
            "FLASH",
            "BLUR_SIGHT",
            "PHANTASMAL_FIEND",
            "INVISIBILITY",
            "PLAGUE_OF_INSECTS",
            "ORCISH_CHAMPION",
            "GUARDIAN_OGRE",
            "HELLGATE",
            "FAMILIAR",
            "MAGELOCK",
            "CONGEAL_TIME",
            "HASTEN",
            "STASIS",
            "TEMPUS_FUGIT",
        };
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        var spells = new SpellDescriptor[rawNames.Length];
        for (var index = 0; index < rawNames.Length; index++)
        {
            var displayName = textInfo.ToTitleCase(rawNames[index].Replace('_', ' ').ToLowerInvariant());
            var collegeId = index / SpellMaxLevel;
            spells[index] = new SpellDescriptor(
                index,
                displayName,
                collegeId,
                SpellCollegeName(collegeId),
                (index % SpellMaxLevel) + 1
            );
        }

        return spells;
    }

    private static Dictionary<string, SpellDescriptor> BuildSpellAliasMap()
    {
        Dictionary<string, SpellDescriptor> map = new(StringComparer.OrdinalIgnoreCase);
        foreach (var spell in s_spells)
        {
            map[Normalize(spell.Name)] = spell;
            map[Normalize($"{spell.CollegeName}{spell.Level.ToString(CultureInfo.InvariantCulture)}")] = spell;
        }

        map["teleport"] = s_spells[4];
        map["lightning"] = s_spells[33];
        map["healminor"] = s_spells[60];
        map["healmajor"] = s_spells[62];
        return map;
    }

    public const int SpellCollegeCount = 16;
    public const int SpellMaxLevel = 5;
    public const int TechDisciplineBaseIndex = SpellCollegeCount + 1;
    public const int TechSkillPointMask = 63;
    private const int TechSkillCount = 4;
    private const int TechDisciplineCount = 8;

    private static readonly SpellDescriptor[] s_spells = BuildSpells();

    private static readonly Dictionary<string, SpellDescriptor> s_spellsByAlias = BuildSpellAliasMap();

    private static readonly Dictionary<string, int> s_techSkillAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["repair"] = 0,
        ["firearms"] = 1,
        ["firearm"] = 1,
        ["picklocks"] = 2,
        ["picklock"] = 2,
        ["disarmtraps"] = 3,
        ["disarmtrap"] = 3,
    };

    private static readonly Dictionary<string, int> s_techDisciplineAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["herbology"] = 0,
        ["chemistry"] = 1,
        ["electric"] = 2,
        ["explosives"] = 3,
        ["gunsmithy"] = 4,
        ["gunsmith"] = 4,
        ["mechanical"] = 5,
        ["smithy"] = 6,
        ["therapeutics"] = 7,
        ["therapeutic"] = 7,
    };
}
