using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Diagnostics;

public static class MobItemAnalysisService
{
    public static MobItemAnalysisSnapshot Analyze(MobData mob)
    {
        ArgumentNullException.ThrowIfNull(mob);

        return new MobItemAnalysisSnapshot(
            mob.Header.GameObjectType,
            GetPropInt32(mob, ObjectField.ItemWeight),
            GetPropInt32(mob, ObjectField.ItemWorth),
            GetPropInt32(mob, ObjectField.ItemFlags),
            GetFlagNames<ItemFlags>(GetPropInt32(mob, ObjectField.ItemFlags)),
            GetPropInt32(mob, ObjectField.ItemDiscipline),
            GetDisciplineLabel(GetPropInt32(mob, ObjectField.ItemDiscipline)),
            GetPropInt32(mob, ObjectField.ItemMagicTechComplexity),
            GetSpellEffects(mob),
            AnalyzeSpecific(mob)
        );
    }

    private static IReadOnlyList<MobItemSpellEffectSnapshot> GetSpellEffects(MobData mob)
    {
        ObjectField[] fields =
        [
            ObjectField.ItemSpell1,
            ObjectField.ItemSpell2,
            ObjectField.ItemSpell3,
            ObjectField.ItemSpell4,
            ObjectField.ItemSpell5,
        ];

        List<MobItemSpellEffectSnapshot> effects = [];
        for (var index = 0; index < fields.Length; index++)
        {
            var spellId = GetPropInt32(mob, fields[index]);
            if (spellId is > 0)
                effects.Add(new MobItemSpellEffectSnapshot(index + 1, spellId.Value));
        }

        return effects;
    }

    private static MobItemSpecificAnalysisSnapshot? AnalyzeSpecific(MobData mob) =>
        mob.Header.GameObjectType switch
        {
            ObjectType.Weapon => new WeaponItemAnalysisSnapshot(
                GetPropInt32(mob, ObjectField.WeaponDamageLowerIdx),
                GetPropInt32(mob, ObjectField.WeaponDamageUpperIdx),
                GetPropInt32(mob, ObjectField.WeaponMagicDamageAdjIdx),
                GetPropInt32(mob, ObjectField.WeaponSpeedFactor),
                GetPropInt32(mob, ObjectField.WeaponMagicSpeedAdj),
                GetPropInt32(mob, ObjectField.WeaponRange),
                GetPropInt32(mob, ObjectField.WeaponBonusToHit),
                GetPropInt32(mob, ObjectField.WeaponMagicHitAdj),
                GetPropInt32(mob, ObjectField.WeaponMinStrength)
            ),
            ObjectType.Armor => new ArmorItemAnalysisSnapshot(
                GetPropInt32(mob, ObjectField.ArmorAcAdj),
                GetPropInt32(mob, ObjectField.ArmorMagicAcAdj),
                GetPropInt32(mob, ObjectField.ArmorSilentMoveAdj)
            ),
            ObjectType.Gold => new GoldItemAnalysisSnapshot(MobGoldResolver.GetGoldQuantity(mob)),
            ObjectType.Food => new FoodItemAnalysisSnapshot(GetPropInt32(mob, ObjectField.FoodFlags)),
            ObjectType.Scroll => new ScrollItemAnalysisSnapshot(GetPropInt32(mob, ObjectField.ScrollFlags)),
            ObjectType.Ammo => new AmmoItemAnalysisSnapshot(
                GetPropInt32(mob, ObjectField.AmmoQuantity),
                GetPropInt32(mob, ObjectField.AmmoType)
            ),
            ObjectType.Key => new KeyItemAnalysisSnapshot(GetPropInt32(mob, ObjectField.KeyKeyId)),
            ObjectType.Written => new WrittenItemAnalysisSnapshot(
                GetPropInt32(mob, ObjectField.WrittenSubtype),
                GetWrittenSubtypeLabel(GetPropInt32(mob, ObjectField.WrittenSubtype)),
                GetPropInt32(mob, ObjectField.WrittenTextStartLine),
                GetPropInt32(mob, ObjectField.WrittenTextEndLine)
            ),
            ObjectType.Generic => new GenericItemAnalysisSnapshot(
                GetPropInt32(mob, ObjectField.GenericUsageBonus),
                GetPropInt32(mob, ObjectField.GenericUsageCountRemaining)
            ),
            _ => null,
        };

    private static int? GetPropInt32(MobData mob, ObjectField field)
    {
        var prop = mob.Properties.FirstOrDefault(property => property.Field == field);
        return prop?.GetInt32();
    }

    private static string? GetDisciplineLabel(int? discipline) =>
        discipline switch
        {
            1 => "magical",
            2 => "technological",
            int value when value > 0 => value.ToString(),
            _ => null,
        };

    private static string? GetWrittenSubtypeLabel(int? subtype) =>
        subtype switch
        {
            0 => "book",
            1 => "note",
            2 => "letter",
            3 => "manual",
            int value => value.ToString(),
            _ => null,
        };

    private static IReadOnlyList<string> GetFlagNames<T>(int? value)
        where T : struct, Enum
    {
        if (value is not > 0)
            return [];

        var flagValue = unchecked((uint)value.Value);
        List<string> names = [];
        foreach (var flag in Enum.GetValues<T>())
        {
            var numeric = Convert.ToUInt32(flag);
            if (numeric != 0 && (flagValue & numeric) == numeric)
                names.Add(flag.ToString());
        }

        return names;
    }
}
