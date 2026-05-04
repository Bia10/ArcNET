using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Typed critter-progression pane contract for one object/proto inspector target.
/// Non-critter targets return one empty summary owned by the current inspector.
/// </summary>
public sealed class EditorObjectInspectorCritterProgressionSummary
{
    public required EditorObjectInspectorSummary Inspector { get; init; }

    public int FatiguePoints { get; init; }

    public int FatigueAdjustment { get; init; }

    public int Level { get; init; }

    public int ExperiencePoints { get; init; }

    public int Alignment { get; init; }

    public int FatePoints { get; init; }

    public int UnspentPoints { get; init; }

    public int MagickPoints { get; init; }

    public int TechPoints { get; init; }

    public int PoisonLevel { get; init; }

    public int Age { get; init; }

    public int Gender { get; init; }

    public int Race { get; init; }

    public int SkillBow { get; init; }

    public int SkillDodge { get; init; }

    public int SkillMelee { get; init; }

    public int SkillThrowing { get; init; }

    public int SkillBackstab { get; init; }

    public int SkillPickPocket { get; init; }

    public int SkillProwling { get; init; }

    public int SkillSpotTrap { get; init; }

    public int SkillGambling { get; init; }

    public int SkillHaggle { get; init; }

    public int SkillHeal { get; init; }

    public int SkillPersuasion { get; init; }

    public int SkillRepair { get; init; }

    public int SkillFirearms { get; init; }

    public int SkillPickLocks { get; init; }

    public int SkillDisarmTraps { get; init; }

    public int SpellConveyance { get; init; }

    public int SpellDivination { get; init; }

    public int SpellAir { get; init; }

    public int SpellEarth { get; init; }

    public int SpellFire { get; init; }

    public int SpellWater { get; init; }

    public int SpellForce { get; init; }

    public int SpellMental { get; init; }

    public int SpellMeta { get; init; }

    public int SpellMorph { get; init; }

    public int SpellNature { get; init; }

    public int SpellNecroBlack { get; init; }

    public int SpellNecroWhite { get; init; }

    public int SpellPhantasm { get; init; }

    public int SpellSummoning { get; init; }

    public int SpellTemporal { get; init; }

    public int SpellMastery { get; init; }

    public int TechHerbology { get; init; }

    public int TechChemistry { get; init; }

    public int TechElectric { get; init; }

    public int TechExplosives { get; init; }

    public int TechGun { get; init; }

    public int TechMechanical { get; init; }

    public int TechSmithy { get; init; }

    public int TechTherapeutics { get; init; }

    public bool IsCritterTarget => Inspector.TargetObjectType is ObjectType.Pc or ObjectType.Npc;

    internal static EditorObjectInspectorCritterProgressionSummary Create(
        EditorObjectInspectorSummary inspector,
        IReadOnlyList<ObjectProperty> properties
    )
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(properties);

        var baseStats = ReadInt32Array(properties, ObjectField.ObjFCritterStatBaseIdx);
        var basicSkills = ReadInt32Array(properties, ObjectField.ObjFCritterBasicSkillIdx);
        var techSkills = ReadInt32Array(properties, ObjectField.ObjFCritterTechSkillIdx);
        var spellTech = ReadInt32Array(properties, ObjectField.ObjFCritterSpellTechIdx);

        return new EditorObjectInspectorCritterProgressionSummary
        {
            Inspector = inspector,
            FatiguePoints = ReadInt32(properties, ObjectField.ObjFCritterFatiguePts),
            FatigueAdjustment = ReadInt32(properties, ObjectField.ObjFCritterFatigueAdj),
            Level = ReadAt(baseStats, 17),
            ExperiencePoints = ReadAt(baseStats, 18),
            Alignment = ReadAt(baseStats, 19),
            FatePoints = ReadAt(baseStats, 20),
            UnspentPoints = ReadAt(baseStats, 21),
            MagickPoints = ReadAt(baseStats, 22),
            TechPoints = ReadAt(baseStats, 23),
            PoisonLevel = ReadAt(baseStats, 24),
            Age = ReadAt(baseStats, 25),
            Gender = ReadAt(baseStats, 26),
            Race = ReadAt(baseStats, 27),
            SkillBow = ReadAt(basicSkills, 0),
            SkillDodge = ReadAt(basicSkills, 1),
            SkillMelee = ReadAt(basicSkills, 2),
            SkillThrowing = ReadAt(basicSkills, 3),
            SkillBackstab = ReadAt(basicSkills, 4),
            SkillPickPocket = ReadAt(basicSkills, 5),
            SkillProwling = ReadAt(basicSkills, 6),
            SkillSpotTrap = ReadAt(basicSkills, 7),
            SkillGambling = ReadAt(basicSkills, 8),
            SkillHaggle = ReadAt(basicSkills, 9),
            SkillHeal = ReadAt(basicSkills, 10),
            SkillPersuasion = ReadAt(basicSkills, 11),
            SkillRepair = ReadAt(techSkills, 0),
            SkillFirearms = ReadAt(techSkills, 1),
            SkillPickLocks = ReadAt(techSkills, 2),
            SkillDisarmTraps = ReadAt(techSkills, 3),
            SpellConveyance = ReadAt(spellTech, 0),
            SpellDivination = ReadAt(spellTech, 1),
            SpellAir = ReadAt(spellTech, 2),
            SpellEarth = ReadAt(spellTech, 3),
            SpellFire = ReadAt(spellTech, 4),
            SpellWater = ReadAt(spellTech, 5),
            SpellForce = ReadAt(spellTech, 6),
            SpellMental = ReadAt(spellTech, 7),
            SpellMeta = ReadAt(spellTech, 8),
            SpellMorph = ReadAt(spellTech, 9),
            SpellNature = ReadAt(spellTech, 10),
            SpellNecroBlack = ReadAt(spellTech, 11),
            SpellNecroWhite = ReadAt(spellTech, 12),
            SpellPhantasm = ReadAt(spellTech, 13),
            SpellSummoning = ReadAt(spellTech, 14),
            SpellTemporal = ReadAt(spellTech, 15),
            SpellMastery = ReadAt(spellTech, 16),
            TechHerbology = ReadAt(spellTech, 17),
            TechChemistry = ReadAt(spellTech, 18),
            TechElectric = ReadAt(spellTech, 19),
            TechExplosives = ReadAt(spellTech, 20),
            TechGun = ReadAt(spellTech, 21),
            TechMechanical = ReadAt(spellTech, 22),
            TechSmithy = ReadAt(spellTech, 23),
            TechTherapeutics = ReadAt(spellTech, 24),
        };
    }

    private static int ReadInt32(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
                return property.GetInt32();
        }

        return 0;
    }

    private static int[] ReadInt32Array(IReadOnlyList<ObjectProperty> properties, ObjectField field)
    {
        for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        {
            var property = properties[propertyIndex];
            if (property.Field == field)
                return property.GetInt32Array();
        }

        return [];
    }

    private static int ReadAt(IReadOnlyList<int> values, int index) => index < values.Count ? values[index] : 0;
}
