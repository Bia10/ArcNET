using System.Globalization;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Projects canonical Arcanum spell metadata in the same numeric order used by CE and the original executable.
/// </summary>
public static class WorkspaceSpellCatalogBuilder
{
    private const int SpellMaxLevel = 5;

    public static IReadOnlyList<WorkspaceSpellCatalogEntry> Build(GameDataStore gameData)
    {
        ArgumentNullException.ThrowIfNull(gameData);

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        var entries = new WorkspaceSpellCatalogEntry[s_spellTokens.Length];
        for (var spellId = 0; spellId < s_spellTokens.Length; spellId++)
        {
            var collegeId = spellId / SpellMaxLevel;
            entries[spellId] = new WorkspaceSpellCatalogEntry(
                spellId,
                textInfo.ToTitleCase(s_spellTokens[spellId].Replace('_', ' ').ToLowerInvariant()),
                collegeId,
                s_spellCollegeNames[collegeId],
                (spellId % SpellMaxLevel) + 1
            );
        }

        return entries;
    }

    private static readonly string[] s_spellTokens =
    [
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
    ];

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
}
