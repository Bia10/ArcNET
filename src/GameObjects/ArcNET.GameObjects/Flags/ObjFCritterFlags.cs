namespace ArcNET.GameObjects;

/// <summary>Critter-specific flags (first set).</summary>
[Flags]
public enum ObjFCritterFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Critter is concealed / hidden.</summary>
    IsConcealed = 0x1,

    /// <summary>Critter is moving silently.</summary>
    MovingSilently = 0x2,

    /// <summary>Critter is undead.</summary>
    Undead = 0x4,

    /// <summary>Critter is an animal.</summary>
    Animal = 0x8,

    /// <summary>Critter is fleeing combat.</summary>
    Fleeing = 0x10,

    /// <summary>Critter is stunned.</summary>
    Stunned = 0x20,

    /// <summary>Critter is paralyzed.</summary>
    Paralyzed = 0x40,

    /// <summary>Critter is blinded.</summary>
    Blinded = 0x80,

    /// <summary>One arm is crippled.</summary>
    CrippledArmsOne = 0x100,

    /// <summary>Both arms are crippled.</summary>
    CrippledArmsBoth = 0x200,

    /// <summary>Both legs are crippled.</summary>
    CrippledLegsBoth = 0x400,

    /// <summary>Unused flag slot.</summary>
    Unused = 0x800,

    /// <summary>Critter is sleeping.</summary>
    Sleeping = 0x1000,

    /// <summary>Critter cannot speak.</summary>
    Mute = 0x2000,

    /// <summary>Critter has surrendered.</summary>
    Surrendered = 0x4000,

    /// <summary>Critter is a monster-type NPC.</summary>
    Monster = 0x8000,

    /// <summary>Critter is fleeing due to a spell.</summary>
    SpellFlee = 0x10000,

    /// <summary>Critter is a random encounter spawn.</summary>
    Encounter = 0x20000,

    /// <summary>Critter is in active combat mode.</summary>
    CombatModeActive = 0x40000,

    /// <summary>Critter emits a small light.</summary>
    LightSmall = 0x80000,

    /// <summary>Critter emits a medium light.</summary>
    LightMedium = 0x100000,

    /// <summary>Critter emits a large light.</summary>
    LightLarge = 0x200000,

    /// <summary>Critter emits an extra-large light.</summary>
    LightXLarge = 0x400000,

    /// <summary>Critter cannot be revived.</summary>
    Unrevivifiable = 0x800000,

    /// <summary>Critter cannot be resurrected.</summary>
    Unressurectable = 0x1000000,

    /// <summary>Critter is a demon.</summary>
    Demon = 0x2000000,

    /// <summary>Critter is immune to fatigue.</summary>
    FatigueImmune = 0x4000000,

    /// <summary>Critter will never flee.</summary>
    NoFlee = 0x8000000,

    /// <summary>Critter fights non-lethally.</summary>
    NonLethalCombat = 0x10000000,

    /// <summary>Critter is mechanical.</summary>
    Mechanical = 0x20000000,

    /// <summary>Critter has an animal-enshroud effect active.</summary>
    AnimalEnshroud = 0x40000000,

    /// <summary>Critter has limited fatigue regeneration.</summary>
    FatigueLimiting = 1u << 31,
}
