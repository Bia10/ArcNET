namespace ArcNET.Editor;

/// <summary>
/// Staged critter-progression pane update for one object/proto inspector target.
/// Null properties preserve the current value.
/// </summary>
public sealed class EditorObjectInspectorCritterProgressionUpdate
{
    public int? FatiguePoints { get; init; }

    public int? FatigueAdjustment { get; init; }

    public int? Level { get; init; }

    public int? ExperiencePoints { get; init; }

    public int? Alignment { get; init; }

    public int? FatePoints { get; init; }

    public int? UnspentPoints { get; init; }

    public int? MagickPoints { get; init; }

    public int? TechPoints { get; init; }

    public int? PoisonLevel { get; init; }

    public int? Age { get; init; }

    public int? Gender { get; init; }

    public int? Race { get; init; }

    public int? SkillBow { get; init; }

    public int? SkillDodge { get; init; }

    public int? SkillMelee { get; init; }

    public int? SkillThrowing { get; init; }

    public int? SkillBackstab { get; init; }

    public int? SkillPickPocket { get; init; }

    public int? SkillProwling { get; init; }

    public int? SkillSpotTrap { get; init; }

    public int? SkillGambling { get; init; }

    public int? SkillHaggle { get; init; }

    public int? SkillHeal { get; init; }

    public int? SkillPersuasion { get; init; }

    public int? SkillRepair { get; init; }

    public int? SkillFirearms { get; init; }

    public int? SkillPickLocks { get; init; }

    public int? SkillDisarmTraps { get; init; }

    public int? SpellConveyance { get; init; }

    public int? SpellDivination { get; init; }

    public int? SpellAir { get; init; }

    public int? SpellEarth { get; init; }

    public int? SpellFire { get; init; }

    public int? SpellWater { get; init; }

    public int? SpellForce { get; init; }

    public int? SpellMental { get; init; }

    public int? SpellMeta { get; init; }

    public int? SpellMorph { get; init; }

    public int? SpellNature { get; init; }

    public int? SpellNecroBlack { get; init; }

    public int? SpellNecroWhite { get; init; }

    public int? SpellPhantasm { get; init; }

    public int? SpellSummoning { get; init; }

    public int? SpellTemporal { get; init; }

    public int? SpellMastery { get; init; }

    public int? TechHerbology { get; init; }

    public int? TechChemistry { get; init; }

    public int? TechElectric { get; init; }

    public int? TechExplosives { get; init; }

    public int? TechGun { get; init; }

    public int? TechMechanical { get; init; }

    public int? TechSmithy { get; init; }

    public int? TechTherapeutics { get; init; }

    public bool HasChanges =>
        FatiguePoints.HasValue
        || FatigueAdjustment.HasValue
        || Level.HasValue
        || ExperiencePoints.HasValue
        || Alignment.HasValue
        || FatePoints.HasValue
        || UnspentPoints.HasValue
        || MagickPoints.HasValue
        || TechPoints.HasValue
        || PoisonLevel.HasValue
        || Age.HasValue
        || Gender.HasValue
        || Race.HasValue
        || SkillBow.HasValue
        || SkillDodge.HasValue
        || SkillMelee.HasValue
        || SkillThrowing.HasValue
        || SkillBackstab.HasValue
        || SkillPickPocket.HasValue
        || SkillProwling.HasValue
        || SkillSpotTrap.HasValue
        || SkillGambling.HasValue
        || SkillHaggle.HasValue
        || SkillHeal.HasValue
        || SkillPersuasion.HasValue
        || SkillRepair.HasValue
        || SkillFirearms.HasValue
        || SkillPickLocks.HasValue
        || SkillDisarmTraps.HasValue
        || SpellConveyance.HasValue
        || SpellDivination.HasValue
        || SpellAir.HasValue
        || SpellEarth.HasValue
        || SpellFire.HasValue
        || SpellWater.HasValue
        || SpellForce.HasValue
        || SpellMental.HasValue
        || SpellMeta.HasValue
        || SpellMorph.HasValue
        || SpellNature.HasValue
        || SpellNecroBlack.HasValue
        || SpellNecroWhite.HasValue
        || SpellPhantasm.HasValue
        || SpellSummoning.HasValue
        || SpellTemporal.HasValue
        || SpellMastery.HasValue
        || TechHerbology.HasValue
        || TechChemistry.HasValue
        || TechElectric.HasValue
        || TechExplosives.HasValue
        || TechGun.HasValue
        || TechMechanical.HasValue
        || TechSmithy.HasValue
        || TechTherapeutics.HasValue;
}
