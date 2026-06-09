using System.Globalization;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics;

public static class ObjectValueFormatter
{
    public static string FormatFieldInt32(int fieldId, int value) =>
        ObjectFieldCatalog.RawName(fieldId) switch
        {
            "OBJ_F_FLAGS" => FormatFlags<ObjectFlags>(value),
            "OBJ_F_SPELL_FLAGS" => FormatFlags<SpellFlags>(value),
            "OBJ_F_BLIT_FLAGS" => FormatFlags<BlitFlags>(value),
            "OBJ_F_PORTAL_FLAGS" => FormatFlags<PortalFlags>(value),
            "OBJ_F_CONTAINER_FLAGS" => FormatFlags<ContainerFlags>(value),
            "OBJ_F_SCENERY_FLAGS" => FormatFlags<SceneryFlags>(value),
            "OBJ_F_ITEM_FLAGS" => FormatFlags<ItemFlags>(value),
            "OBJ_F_WEAPON_FLAGS" => FormatFlags<WeaponFlags>(value),
            "OBJ_F_ARMOR_FLAGS" => FormatFlags<ArmorFlags>(value),
            "OBJ_F_CRITTER_FLAGS" => FormatFlags<CritterFlags>(value),
            "OBJ_F_CRITTER_FLAGS2" => FormatFlags<CritterFlags2>(value),
            "OBJ_F_NPC_FLAGS" => FormatFlags<NpcFlags>(value),
            "OBJ_F_ITEM_INV_LOCATION" =>
                $"{RuntimeSemanticCatalog.InventoryLocationName(value)} ({value.ToString(CultureInfo.InvariantCulture)})",
            "OBJ_F_LIGHT_COLOR" or "OBJ_F_OVERLAY_LIGHT_COLOR" or "OBJ_F_BLIT_COLOR" or "OBJ_F_COLOR" =>
                $"0x{unchecked((uint)value):X8}",
            "OBJ_F_CURRENT_AID"
            or "OBJ_F_AID"
            or "OBJ_F_DESTROYED_AID"
            or "OBJ_F_LIGHT_AID"
            or "OBJ_F_OVERLAY_LIGHT_AID"
            or "OBJ_F_ITEM_INV_AID"
            or "OBJ_F_WEAPON_PAPER_DOLL_AID"
            or "OBJ_F_WEAPON_MISSILE_AID"
            or "OBJ_F_WEAPON_VISUAL_EFFECT_AID"
            or "OBJ_F_ARMOR_PAPER_DOLL_AID" =>
                $"{value.ToString(CultureInfo.InvariantCulture)} (0x{unchecked((uint)value):X8})",
            _ => value.ToString(CultureInfo.InvariantCulture),
        };

    public static string FormatArrayInt32(int fieldId, int index, int value) =>
        ObjectFieldCatalog.RawName(fieldId) switch
        {
            "OBJ_F_CRITTER_BASIC_SKILL_IDX" => FormatEncodedSkillValue(ObjectFieldCatalog.BasicSkillName(index), value),
            "OBJ_F_CRITTER_TECH_SKILL_IDX" => FormatEncodedSkillValue(ObjectFieldCatalog.TechSkillName(index), value),
            "OBJ_F_CRITTER_SPELL_TECH_IDX" => FormatSpellTechValue(index, value),
            _ => FormatFieldInt32(fieldId, value),
        };

    public static string FormatArrayUInt32(int fieldId, int index, uint value)
    {
        var signedValue = unchecked((int)value);
        return ObjectFieldCatalog.RawName(fieldId) switch
        {
            "OBJ_F_CRITTER_SPELL_TECH_IDX" => FormatSpellTechValue(index, signedValue),
            "OBJ_F_CRITTER_EFFECTS_IDX" =>
                $"{RuntimeWatchValueCatalog.FallbackEffectName(signedValue)} [{signedValue}]",
            "OBJ_F_CRITTER_EFFECT_CAUSE_IDX" => RuntimeWatchValueCatalog.EffectCauseName(signedValue),
            "OBJ_F_PC_BLESSING_IDX" => $"Blessing {signedValue}",
            "OBJ_F_PC_CURSE_IDX" => $"Curse {signedValue}",
            "OBJ_F_PC_SCHEMATICS_FOUND_IDX" => $"Schematic {signedValue}",
            _ => value.ToString(CultureInfo.InvariantCulture),
        };
    }

    public static string FormatSkillTraining(int training) =>
        training == 0 ? "Untrained" : ObjectFieldCatalog.TrainingName(training);

    private static string FormatEncodedSkillValue(string name, int value)
    {
        var level = value & 63;
        var training = (value >> 6) & 3;
        return $"{name} level {level}, training {FormatSkillTraining(training)}";
    }

    private static string FormatSpellTechValue(int index, int value)
    {
        if (index is >= 0 and < SpellCollegeCount)
            return $"level {value}";

        if (index == SpellCollegeCount)
            return value is >= 0 and < SpellCollegeCount ? ObjectFieldCatalog.SpellCollegeName(value) : "None";

        var disciplineIndex = index - SpellCollegeCount - 1;
        return disciplineIndex is >= 0 and < TechDisciplineCount
            ? $"degree {value}"
            : value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatFlags<TEnum>(int value)
        where TEnum : struct, Enum
    {
        var rawValue = unchecked((uint)value);
        var enumValue = Enum.ToObject(typeof(TEnum), rawValue);
        return $"{Enum.Format(typeof(TEnum), enumValue, "F")} (0x{rawValue:X8})";
    }

    private const int SpellCollegeCount = 16;
    private const int TechDisciplineCount = 8;
}
