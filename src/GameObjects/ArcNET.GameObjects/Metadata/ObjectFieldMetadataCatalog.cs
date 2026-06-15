using System.Globalization;
using ArcNET.GameObjects;

namespace ArcNET.GameObjects.Metadata;

public static class ObjectFieldMetadataCatalog
{
    public static IReadOnlyList<ObjectFieldDescriptor> Fields => s_fields;

    public static string RawName(int fieldId) =>
        fieldId >= 0 && fieldId < s_objectFieldNames.Length ? s_objectFieldNames[fieldId] : $"OBJ_F_UNKNOWN_{fieldId}";

    public static string RawName(ObjectField field) => RawName((int)field);

    public static bool TryGetFieldId(string rawName, out int fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawName);

        if (s_fieldIdsByRawName.TryGetValue(rawName, out var resolvedFieldId))
        {
            fieldId = resolvedFieldId;
            return true;
        }

        fieldId = -1;
        return false;
    }

    public static bool TryGetField(string rawName, out ObjectField field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawName);
        return s_objectFieldsByRawName.TryGetValue(rawName, out field);
    }

    public static string DisplayName(int fieldId)
    {
        if (fieldId < 0 || fieldId >= s_objectFieldNames.Length)
            return $"ObjectField[{fieldId}]";

        var rawName = s_objectFieldNames[fieldId];
        var trimmed = rawName.StartsWith("OBJ_F_", StringComparison.Ordinal) ? rawName[6..] : rawName;
        return string.Join(' ', trimmed.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(HumanizeToken));
    }

    public static string DisplayName(ObjectField field) => DisplayName((int)field);

    public static string CollectionName(int fieldId)
    {
        var displayName = DisplayName(fieldId);
        return displayName.EndsWith(" Index", StringComparison.Ordinal) ? displayName[..^6] : displayName;
    }

    public static string CollectionName(ObjectField field) => CollectionName((int)field);

    public static string ArrayElementName(int fieldId, int index) =>
        RawName(fieldId) switch
        {
            "OBJ_F_RESISTANCE_IDX"
            or "OBJ_F_ARMOR_RESISTANCE_ADJ_IDX"
            or "OBJ_F_ARMOR_MAGIC_RESISTANCE_ADJ_IDX"
            or "OBJ_F_WEAPON_MAGIC_DAMAGE_ADJ_IDX" => ResistanceName(index),
            "OBJ_F_SCRIPTS_IDX" => GameObjectRuntimeMetadata.AttachmentPointName(index),
            "OBJ_F_CRITTER_STAT_BASE_IDX" => CharacterSheetMetadata.StatName(index),
            "OBJ_F_CRITTER_BASIC_SKILL_IDX" => BasicSkillName(index),
            "OBJ_F_CRITTER_TECH_SKILL_IDX" => TechSkillName(index),
            "OBJ_F_CRITTER_SPELL_TECH_IDX" => SpellTechSlotName(index),
            "OBJ_F_PC_QUEST_IDX" => $"Quest {index}",
            "OBJ_F_CRITTER_EFFECTS_IDX" => $"Effect Slot {index}",
            "OBJ_F_CRITTER_EFFECT_CAUSE_IDX" => $"Effect Cause Slot {index}",
            "OBJ_F_CONTAINER_INVENTORY_LIST_IDX" or "OBJ_F_CRITTER_INVENTORY_LIST_IDX" => $"Inventory Slot {index}",
            "OBJ_F_CRITTER_FOLLOWER_IDX" => $"Follower Slot {index}",
            "OBJ_F_PC_BLESSING_IDX" or "OBJ_F_PC_BLESSING_TS_IDX" => $"Blessing Slot {index}",
            "OBJ_F_PC_CURSE_IDX" or "OBJ_F_PC_CURSE_TS_IDX" => $"Curse Slot {index}",
            "OBJ_F_PC_REPUTATION_IDX" or "OBJ_F_PC_REPUTATION_TS_IDX" => $"Reputation Slot {index}",
            "OBJ_F_PC_SCHEMATICS_FOUND_IDX" => $"Schematic Slot {index}",
            _ => $"Index {index}",
        };

    public static bool IsNoiseField(int fieldId)
    {
        var rawName = RawName(fieldId);
        return rawName switch
        {
            "OBJ_F_BEGIN"
            or "OBJ_F_END"
            or "OBJ_F_TOTAL_NORMAL"
            or "OBJ_F_TRANSIENT_BEGIN"
            or "OBJ_F_TRANSIENT_END"
            or "OBJ_F_INTERNAL_FLAGS"
            or "OBJ_F_TEMP_ID"
            or "OBJ_F_LIGHT_HANDLE"
            or "OBJ_F_OVERLAY_LIGHT_HANDLES"
            or "OBJ_F_SHADOW_HANDLES"
            or "OBJ_F_FIND_NODE"
            or "OBJ_F_MAX" => true,
            _ when rawName.EndsWith("_BEGIN", StringComparison.Ordinal)
                    || rawName.EndsWith("_END", StringComparison.Ordinal)
                    || rawName.StartsWith("OBJ_F_RENDER_", StringComparison.Ordinal) => true,
            _ => false,
        };
    }

    public static string ResistanceName(int index) => CharacterSheetMetadata.ResistanceName(index);

    public static string BasicSkillName(int index) => CharacterSheetMetadata.BasicSkillName(index);

    public static string TechSkillName(int index) => CharacterSheetMetadata.TechSkillName(index);

    public static string SpellCollegeName(int index) => CharacterSheetMetadata.SpellCollegeName(index);

    public static string TrainingName(int training) => CharacterSheetMetadata.TrainingName(training);

    private static string SpellTechSlotName(int index) => CharacterSheetMetadata.SpellTechSlotName(index);

    private static string HumanizeToken(string token) =>
        token switch
        {
            "PC" => "PC",
            "NPC" => "NPC",
            "AI" => "AI",
            "AC" => "AC",
            "AID" => "ArtId",
            "ID" => "ID",
            "IDX" => "Index",
            "IAS" => "IAS",
            "TS" => "Timestamp",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token.ToLowerInvariant()),
        };

    private static Dictionary<string, ObjectField> CreateObjectFieldsByRawName()
    {
        Dictionary<string, ObjectField> fieldsByRawName = new(StringComparer.OrdinalIgnoreCase);
        foreach (var rawName in s_objectFieldNames)
        {
            if (TryConvertRawNameToObjectField(rawName, out var field))
                fieldsByRawName[rawName] = field;
        }

        return fieldsByRawName;
    }

    private static bool TryConvertRawNameToObjectField(string rawName, out ObjectField field)
    {
        const string prefix = "OBJ_F_";
        if (!rawName.StartsWith(prefix, StringComparison.Ordinal))
        {
            field = default;
            return false;
        }

        var memberName = rawName[prefix.Length..] switch
        {
            "FLAGS" => nameof(ObjectField.ObjectFlags),
            "NPC_SHIT_LIST_IDX" => nameof(ObjectField.NpcHostileListIdx),
            var token => ToObjectFieldMemberName(token),
        };
        return Enum.TryParse(memberName, ignoreCase: false, out field);
    }

    private static string ToObjectFieldMemberName(string rawName) =>
        string.Concat(rawName.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(ToObjectFieldMemberToken));

    private static string ToObjectFieldMemberToken(string token) =>
        token switch
        {
            "AC" => "Ac",
            "AI" => "Ai",
            "AID" => "Aid",
            "DC" => "Dc",
            "HP" => "Hp",
            "IAS" => "Ias",
            "I64AS" => "I64As",
            "ID" => "Id",
            "IDX" => "Idx",
            "NPC" => "Npc",
            "PC" => "Pc",
            "TS" => "Ts",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token.ToLowerInvariant()),
        };

    private static readonly string[] s_objectFieldNames =
    [
        "OBJ_F_BEGIN",
        "OBJ_F_CURRENT_AID",
        "OBJ_F_LOCATION",
        "OBJ_F_OFFSET_X",
        "OBJ_F_OFFSET_Y",
        "OBJ_F_SHADOW",
        "OBJ_F_OVERLAY_FORE",
        "OBJ_F_OVERLAY_BACK",
        "OBJ_F_UNDERLAY",
        "OBJ_F_BLIT_FLAGS",
        "OBJ_F_BLIT_COLOR",
        "OBJ_F_BLIT_ALPHA",
        "OBJ_F_BLIT_SCALE",
        "OBJ_F_LIGHT_FLAGS",
        "OBJ_F_LIGHT_AID",
        "OBJ_F_LIGHT_COLOR",
        "OBJ_F_OVERLAY_LIGHT_FLAGS",
        "OBJ_F_OVERLAY_LIGHT_AID",
        "OBJ_F_OVERLAY_LIGHT_COLOR",
        "OBJ_F_FLAGS",
        "OBJ_F_SPELL_FLAGS",
        "OBJ_F_BLOCKING_MASK",
        "OBJ_F_NAME",
        "OBJ_F_DESCRIPTION",
        "OBJ_F_AID",
        "OBJ_F_DESTROYED_AID",
        "OBJ_F_AC",
        "OBJ_F_HP_PTS",
        "OBJ_F_HP_ADJ",
        "OBJ_F_HP_DAMAGE",
        "OBJ_F_MATERIAL",
        "OBJ_F_RESISTANCE_IDX",
        "OBJ_F_SCRIPTS_IDX",
        "OBJ_F_SOUND_EFFECT",
        "OBJ_F_CATEGORY",
        "OBJ_F_PAD_IAS_1",
        "OBJ_F_PAD_I64AS_1",
        "OBJ_F_END",
        "OBJ_F_WALL_BEGIN",
        "OBJ_F_WALL_FLAGS",
        "OBJ_F_WALL_PAD_I_1",
        "OBJ_F_WALL_PAD_I_2",
        "OBJ_F_WALL_PAD_IAS_1",
        "OBJ_F_WALL_PAD_I64AS_1",
        "OBJ_F_WALL_END",
        "OBJ_F_PORTAL_BEGIN",
        "OBJ_F_PORTAL_FLAGS",
        "OBJ_F_PORTAL_LOCK_DIFFICULTY",
        "OBJ_F_PORTAL_KEY_ID",
        "OBJ_F_PORTAL_NOTIFY_NPC",
        "OBJ_F_PORTAL_PAD_I_1",
        "OBJ_F_PORTAL_PAD_I_2",
        "OBJ_F_PORTAL_PAD_IAS_1",
        "OBJ_F_PORTAL_PAD_I64AS_1",
        "OBJ_F_PORTAL_END",
        "OBJ_F_CONTAINER_BEGIN",
        "OBJ_F_CONTAINER_FLAGS",
        "OBJ_F_CONTAINER_LOCK_DIFFICULTY",
        "OBJ_F_CONTAINER_KEY_ID",
        "OBJ_F_CONTAINER_INVENTORY_NUM",
        "OBJ_F_CONTAINER_INVENTORY_LIST_IDX",
        "OBJ_F_CONTAINER_INVENTORY_SOURCE",
        "OBJ_F_CONTAINER_NOTIFY_NPC",
        "OBJ_F_CONTAINER_PAD_I_1",
        "OBJ_F_CONTAINER_PAD_I_2",
        "OBJ_F_CONTAINER_PAD_IAS_1",
        "OBJ_F_CONTAINER_PAD_I64AS_1",
        "OBJ_F_CONTAINER_END",
        "OBJ_F_SCENERY_BEGIN",
        "OBJ_F_SCENERY_FLAGS",
        "OBJ_F_SCENERY_WHOS_IN_ME",
        "OBJ_F_SCENERY_RESPAWN_DELAY",
        "OBJ_F_SCENERY_PAD_I_2",
        "OBJ_F_SCENERY_PAD_IAS_1",
        "OBJ_F_SCENERY_PAD_I64AS_1",
        "OBJ_F_SCENERY_END",
        "OBJ_F_PROJECTILE_BEGIN",
        "OBJ_F_PROJECTILE_FLAGS_COMBAT",
        "OBJ_F_PROJECTILE_FLAGS_COMBAT_DAMAGE",
        "OBJ_F_PROJECTILE_HIT_LOC",
        "OBJ_F_PROJECTILE_PARENT_WEAPON",
        "OBJ_F_PROJECTILE_PAD_I_1",
        "OBJ_F_PROJECTILE_PAD_I_2",
        "OBJ_F_PROJECTILE_PAD_IAS_1",
        "OBJ_F_PROJECTILE_PAD_I64AS_1",
        "OBJ_F_PROJECTILE_END",
        "OBJ_F_ITEM_BEGIN",
        "OBJ_F_ITEM_FLAGS",
        "OBJ_F_ITEM_PARENT",
        "OBJ_F_ITEM_WEIGHT",
        "OBJ_F_ITEM_MAGIC_WEIGHT_ADJ",
        "OBJ_F_ITEM_WORTH",
        "OBJ_F_ITEM_MANA_STORE",
        "OBJ_F_ITEM_INV_AID",
        "OBJ_F_ITEM_INV_LOCATION",
        "OBJ_F_ITEM_USE_AID_FRAGMENT",
        "OBJ_F_ITEM_MAGIC_TECH_COMPLEXITY",
        "OBJ_F_ITEM_DISCIPLINE",
        "OBJ_F_ITEM_DESCRIPTION_UNKNOWN",
        "OBJ_F_ITEM_DESCRIPTION_EFFECTS",
        "OBJ_F_ITEM_SPELL_1",
        "OBJ_F_ITEM_SPELL_2",
        "OBJ_F_ITEM_SPELL_3",
        "OBJ_F_ITEM_SPELL_4",
        "OBJ_F_ITEM_SPELL_5",
        "OBJ_F_ITEM_SPELL_MANA_STORE",
        "OBJ_F_ITEM_AI_ACTION",
        "OBJ_F_ITEM_PAD_I_1",
        "OBJ_F_ITEM_PAD_IAS_1",
        "OBJ_F_ITEM_PAD_I64AS_1",
        "OBJ_F_ITEM_END",
        "OBJ_F_WEAPON_BEGIN",
        "OBJ_F_WEAPON_FLAGS",
        "OBJ_F_WEAPON_PAPER_DOLL_AID",
        "OBJ_F_WEAPON_BONUS_TO_HIT",
        "OBJ_F_WEAPON_MAGIC_HIT_ADJ",
        "OBJ_F_WEAPON_DAMAGE_LOWER_IDX",
        "OBJ_F_WEAPON_DAMAGE_UPPER_IDX",
        "OBJ_F_WEAPON_MAGIC_DAMAGE_ADJ_IDX",
        "OBJ_F_WEAPON_SPEED_FACTOR",
        "OBJ_F_WEAPON_MAGIC_SPEED_ADJ",
        "OBJ_F_WEAPON_RANGE",
        "OBJ_F_WEAPON_MAGIC_RANGE_ADJ",
        "OBJ_F_WEAPON_MIN_STRENGTH",
        "OBJ_F_WEAPON_MAGIC_MIN_STRENGTH_ADJ",
        "OBJ_F_WEAPON_AMMO_TYPE",
        "OBJ_F_WEAPON_AMMO_CONSUMPTION",
        "OBJ_F_WEAPON_MISSILE_AID",
        "OBJ_F_WEAPON_VISUAL_EFFECT_AID",
        "OBJ_F_WEAPON_CRIT_HIT_CHART",
        "OBJ_F_WEAPON_MAGIC_CRIT_HIT_CHANCE",
        "OBJ_F_WEAPON_MAGIC_CRIT_HIT_EFFECT",
        "OBJ_F_WEAPON_CRIT_MISS_CHART",
        "OBJ_F_WEAPON_MAGIC_CRIT_MISS_CHANCE",
        "OBJ_F_WEAPON_MAGIC_CRIT_MISS_EFFECT",
        "OBJ_F_WEAPON_PAD_I_1",
        "OBJ_F_WEAPON_PAD_I_2",
        "OBJ_F_WEAPON_PAD_IAS_1",
        "OBJ_F_WEAPON_PAD_I64AS_1",
        "OBJ_F_WEAPON_END",
        "OBJ_F_AMMO_BEGIN",
        "OBJ_F_AMMO_FLAGS",
        "OBJ_F_AMMO_QUANTITY",
        "OBJ_F_AMMO_TYPE",
        "OBJ_F_AMMO_PAD_I_1",
        "OBJ_F_AMMO_PAD_I_2",
        "OBJ_F_AMMO_PAD_IAS_1",
        "OBJ_F_AMMO_PAD_I64AS_1",
        "OBJ_F_AMMO_END",
        "OBJ_F_ARMOR_BEGIN",
        "OBJ_F_ARMOR_FLAGS",
        "OBJ_F_ARMOR_PAPER_DOLL_AID",
        "OBJ_F_ARMOR_AC_ADJ",
        "OBJ_F_ARMOR_MAGIC_AC_ADJ",
        "OBJ_F_ARMOR_RESISTANCE_ADJ_IDX",
        "OBJ_F_ARMOR_MAGIC_RESISTANCE_ADJ_IDX",
        "OBJ_F_ARMOR_SILENT_MOVE_ADJ",
        "OBJ_F_ARMOR_MAGIC_SILENT_MOVE_ADJ",
        "OBJ_F_ARMOR_UNARMED_BONUS_DAMAGE",
        "OBJ_F_ARMOR_PAD_I_2",
        "OBJ_F_ARMOR_PAD_IAS_1",
        "OBJ_F_ARMOR_PAD_I64AS_1",
        "OBJ_F_ARMOR_END",
        "OBJ_F_GOLD_BEGIN",
        "OBJ_F_GOLD_FLAGS",
        "OBJ_F_GOLD_QUANTITY",
        "OBJ_F_GOLD_PAD_I_1",
        "OBJ_F_GOLD_PAD_I_2",
        "OBJ_F_GOLD_PAD_IAS_1",
        "OBJ_F_GOLD_PAD_I64AS_1",
        "OBJ_F_GOLD_END",
        "OBJ_F_FOOD_BEGIN",
        "OBJ_F_FOOD_FLAGS",
        "OBJ_F_FOOD_PAD_I_1",
        "OBJ_F_FOOD_PAD_I_2",
        "OBJ_F_FOOD_PAD_IAS_1",
        "OBJ_F_FOOD_PAD_I64AS_1",
        "OBJ_F_FOOD_END",
        "OBJ_F_SCROLL_BEGIN",
        "OBJ_F_SCROLL_FLAGS",
        "OBJ_F_SCROLL_PAD_I_1",
        "OBJ_F_SCROLL_PAD_I_2",
        "OBJ_F_SCROLL_PAD_IAS_1",
        "OBJ_F_SCROLL_PAD_I64AS_1",
        "OBJ_F_SCROLL_END",
        "OBJ_F_KEY_BEGIN",
        "OBJ_F_KEY_KEY_ID",
        "OBJ_F_KEY_PAD_I_1",
        "OBJ_F_KEY_PAD_I_2",
        "OBJ_F_KEY_PAD_IAS_1",
        "OBJ_F_KEY_PAD_I64AS_1",
        "OBJ_F_KEY_END",
        "OBJ_F_KEY_RING_BEGIN",
        "OBJ_F_KEY_RING_FLAGS",
        "OBJ_F_KEY_RING_LIST_IDX",
        "OBJ_F_KEY_RING_PAD_I_1",
        "OBJ_F_KEY_RING_PAD_I_2",
        "OBJ_F_KEY_RING_PAD_IAS_1",
        "OBJ_F_KEY_RING_PAD_I64AS_1",
        "OBJ_F_KEY_RING_END",
        "OBJ_F_WRITTEN_BEGIN",
        "OBJ_F_WRITTEN_FLAGS",
        "OBJ_F_WRITTEN_SUBTYPE",
        "OBJ_F_WRITTEN_TEXT_START_LINE",
        "OBJ_F_WRITTEN_TEXT_END_LINE",
        "OBJ_F_WRITTEN_PAD_I_1",
        "OBJ_F_WRITTEN_PAD_I_2",
        "OBJ_F_WRITTEN_PAD_IAS_1",
        "OBJ_F_WRITTEN_PAD_I64AS_1",
        "OBJ_F_WRITTEN_END",
        "OBJ_F_GENERIC_BEGIN",
        "OBJ_F_GENERIC_FLAGS",
        "OBJ_F_GENERIC_USAGE_BONUS",
        "OBJ_F_GENERIC_USAGE_COUNT_REMAINING",
        "OBJ_F_GENERIC_PAD_IAS_1",
        "OBJ_F_GENERIC_PAD_I64AS_1",
        "OBJ_F_GENERIC_END",
        "OBJ_F_CRITTER_BEGIN",
        "OBJ_F_CRITTER_FLAGS",
        "OBJ_F_CRITTER_FLAGS2",
        "OBJ_F_CRITTER_STAT_BASE_IDX",
        "OBJ_F_CRITTER_BASIC_SKILL_IDX",
        "OBJ_F_CRITTER_TECH_SKILL_IDX",
        "OBJ_F_CRITTER_SPELL_TECH_IDX",
        "OBJ_F_CRITTER_FATIGUE_PTS",
        "OBJ_F_CRITTER_FATIGUE_ADJ",
        "OBJ_F_CRITTER_FATIGUE_DAMAGE",
        "OBJ_F_CRITTER_CRIT_HIT_CHART",
        "OBJ_F_CRITTER_EFFECTS_IDX",
        "OBJ_F_CRITTER_EFFECT_CAUSE_IDX",
        "OBJ_F_CRITTER_FLEEING_FROM",
        "OBJ_F_CRITTER_PORTRAIT",
        "OBJ_F_CRITTER_GOLD",
        "OBJ_F_CRITTER_ARROWS",
        "OBJ_F_CRITTER_BULLETS",
        "OBJ_F_CRITTER_POWER_CELLS",
        "OBJ_F_CRITTER_FUEL",
        "OBJ_F_CRITTER_INVENTORY_NUM",
        "OBJ_F_CRITTER_INVENTORY_LIST_IDX",
        "OBJ_F_CRITTER_INVENTORY_SOURCE",
        "OBJ_F_CRITTER_DESCRIPTION_UNKNOWN",
        "OBJ_F_CRITTER_FOLLOWER_IDX",
        "OBJ_F_CRITTER_TELEPORT_DEST",
        "OBJ_F_CRITTER_TELEPORT_MAP",
        "OBJ_F_CRITTER_DEATH_TIME",
        "OBJ_F_CRITTER_AUTO_LEVEL_SCHEME",
        "OBJ_F_CRITTER_PAD_I_1",
        "OBJ_F_CRITTER_PAD_I_2",
        "OBJ_F_CRITTER_PAD_I_3",
        "OBJ_F_CRITTER_PAD_IAS_1",
        "OBJ_F_CRITTER_PAD_I64AS_1",
        "OBJ_F_CRITTER_END",
        "OBJ_F_PC_BEGIN",
        "OBJ_F_PC_FLAGS",
        "OBJ_F_PC_FLAGS_FATE",
        "OBJ_F_PC_REPUTATION_IDX",
        "OBJ_F_PC_REPUTATION_TS_IDX",
        "OBJ_F_PC_BACKGROUND",
        "OBJ_F_PC_BACKGROUND_TEXT",
        "OBJ_F_PC_QUEST_IDX",
        "OBJ_F_PC_BLESSING_IDX",
        "OBJ_F_PC_BLESSING_TS_IDX",
        "OBJ_F_PC_CURSE_IDX",
        "OBJ_F_PC_CURSE_TS_IDX",
        "OBJ_F_PC_PARTY_ID",
        "OBJ_F_PC_RUMOR_IDX",
        "OBJ_F_PC_PAD_IAS_2",
        "OBJ_F_PC_SCHEMATICS_FOUND_IDX",
        "OBJ_F_PC_LOGBOOK_EGO_IDX",
        "OBJ_F_PC_FOG_MASK",
        "OBJ_F_PC_PLAYER_NAME",
        "OBJ_F_PC_BANK_MONEY",
        "OBJ_F_PC_GLOBAL_FLAGS",
        "OBJ_F_PC_GLOBAL_VARIABLES",
        "OBJ_F_PC_PAD_I_1",
        "OBJ_F_PC_PAD_I_2",
        "OBJ_F_PC_PAD_IAS_1",
        "OBJ_F_PC_PAD_I64AS_1",
        "OBJ_F_PC_END",
        "OBJ_F_NPC_BEGIN",
        "OBJ_F_NPC_FLAGS",
        "OBJ_F_NPC_LEADER",
        "OBJ_F_NPC_AI_DATA",
        "OBJ_F_NPC_COMBAT_FOCUS",
        "OBJ_F_NPC_WHO_HIT_ME_LAST",
        "OBJ_F_NPC_EXPERIENCE_WORTH",
        "OBJ_F_NPC_EXPERIENCE_POOL",
        "OBJ_F_NPC_WAYPOINTS_IDX",
        "OBJ_F_NPC_WAYPOINT_CURRENT",
        "OBJ_F_NPC_STANDPOINT_DAY",
        "OBJ_F_NPC_STANDPOINT_NIGHT",
        "OBJ_F_NPC_ORIGIN",
        "OBJ_F_NPC_FACTION",
        "OBJ_F_NPC_RETAIL_PRICE_MULTIPLIER",
        "OBJ_F_NPC_SUBSTITUTE_INVENTORY",
        "OBJ_F_NPC_REACTION_BASE",
        "OBJ_F_NPC_SOCIAL_CLASS",
        "OBJ_F_NPC_REACTION_PC_IDX",
        "OBJ_F_NPC_REACTION_LEVEL_IDX",
        "OBJ_F_NPC_REACTION_TIME_IDX",
        "OBJ_F_NPC_WAIT",
        "OBJ_F_NPC_GENERATOR_DATA",
        "OBJ_F_NPC_PAD_I_1",
        "OBJ_F_NPC_DAMAGE_IDX",
        "OBJ_F_NPC_SHIT_LIST_IDX",
        "OBJ_F_NPC_END",
        "OBJ_F_TRAP_BEGIN",
        "OBJ_F_TRAP_FLAGS",
        "OBJ_F_TRAP_DIFFICULTY",
        "OBJ_F_TRAP_PAD_I_2",
        "OBJ_F_TRAP_PAD_IAS_1",
        "OBJ_F_TRAP_PAD_I64AS_1",
        "OBJ_F_TRAP_END",
        "OBJ_F_TOTAL_NORMAL",
        "OBJ_F_TRANSIENT_BEGIN",
        "OBJ_F_RENDER_COLOR",
        "OBJ_F_RENDER_COLORS",
        "OBJ_F_RENDER_PALETTE",
        "OBJ_F_RENDER_SCALE",
        "OBJ_F_RENDER_ALPHA",
        "OBJ_F_RENDER_X",
        "OBJ_F_RENDER_Y",
        "OBJ_F_RENDER_WIDTH",
        "OBJ_F_RENDER_HEIGHT",
        "OBJ_F_PALETTE",
        "OBJ_F_COLOR",
        "OBJ_F_COLORS",
        "OBJ_F_RENDER_FLAGS",
        "OBJ_F_TEMP_ID",
        "OBJ_F_LIGHT_HANDLE",
        "OBJ_F_OVERLAY_LIGHT_HANDLES",
        "OBJ_F_SHADOW_HANDLES",
        "OBJ_F_INTERNAL_FLAGS",
        "OBJ_F_FIND_NODE",
        "OBJ_F_TRANSIENT_END",
        "OBJ_F_TYPE",
        "OBJ_F_PROTOTYPE_HANDLE",
        "OBJ_F_MAX",
    ];

    private static readonly ObjectFieldDescriptor[] s_fields =
    [
        .. Enumerable
            .Range(0, s_objectFieldNames.Length)
            .Select(static fieldId => new ObjectFieldDescriptor(
                fieldId,
                RawName(fieldId),
                DisplayName(fieldId),
                CollectionName(fieldId),
                IsNoiseField(fieldId)
            )),
    ];

    private static readonly Dictionary<string, int> s_fieldIdsByRawName = Enumerable
        .Range(0, s_objectFieldNames.Length)
        .ToDictionary(static fieldId => RawName(fieldId), static fieldId => fieldId, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ObjectField> s_objectFieldsByRawName = CreateObjectFieldsByRawName();
}
