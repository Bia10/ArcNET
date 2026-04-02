namespace ArcNET.GameObjects;

/// <summary>Critter-specific flags (first set).</summary>
[Flags]
public enum ObjFCritterFlags : uint
{
    /// <summary>Critter is concealed / hidden.</summary>
    IsConcealed = 1 << 0,

    /// <summary>Critter is moving silently.</summary>
    MovingSilently = 1 << 1,

    /// <summary>Critter is undead.</summary>
    Undead = 1 << 2,

    /// <summary>Critter is an animal.</summary>
    Animal = 1 << 3,

    /// <summary>Critter is fleeing combat.</summary>
    Fleeing = 1 << 4,

    /// <summary>Critter is stunned.</summary>
    Stunned = 1 << 5,

    /// <summary>Critter is paralyzed.</summary>
    Paralyzed = 1 << 6,

    /// <summary>Critter is blinded.</summary>
    Blinded = 1 << 7,

    /// <summary>One arm is crippled.</summary>
    CrippledArmsOne = 1 << 8,

    /// <summary>Both arms are crippled.</summary>
    CrippledArmsBoth = 1 << 9,

    /// <summary>Both legs are crippled.</summary>
    CrippledLegsBoth = 1 << 10,

    /// <summary>Unused flag slot.</summary>
    Unused = 1 << 11,

    /// <summary>Critter is sleeping.</summary>
    Sleeping = 1 << 12,

    /// <summary>Critter cannot speak.</summary>
    Mute = 1 << 13,

    /// <summary>Critter has surrendered.</summary>
    Surrendered = 1 << 14,

    /// <summary>Critter is a monster-type NPC.</summary>
    Monster = 1 << 15,

    /// <summary>Critter is fleeing due to a spell.</summary>
    SpellFlee = 1 << 16,

    /// <summary>Critter is a random encounter spawn.</summary>
    Encounter = 1 << 17,

    /// <summary>Critter is in active combat mode.</summary>
    CombatModeActive = 1 << 18,

    /// <summary>Critter emits a small light.</summary>
    LightSmall = 1 << 19,

    /// <summary>Critter emits a medium light.</summary>
    LightMedium = 1 << 20,

    /// <summary>Critter emits a large light.</summary>
    LightLarge = 1 << 21,

    /// <summary>Critter emits an extra-large light.</summary>
    LightXLarge = 1 << 22,

    /// <summary>Critter cannot be revived.</summary>
    Unrevivifiable = 1 << 23,

    /// <summary>Critter cannot be resurrected.</summary>
    Unressurectable = 1 << 24,

    /// <summary>Critter is a demon.</summary>
    Demon = 1 << 25,

    /// <summary>Critter is immune to fatigue.</summary>
    FatigueImmune = 1 << 26,

    /// <summary>Critter will never flee.</summary>
    NoFlee = 1 << 27,

    /// <summary>Critter fights non-lethally.</summary>
    NonLethalCombat = 1 << 28,

    /// <summary>Critter is mechanical.</summary>
    Mechanical = 1 << 29,

    /// <summary>Critter has an animal-enshroud effect active.</summary>
    AnimalEnshroud = 1 << 30,

    /// <summary>Critter has limited fatigue regeneration.</summary>
    FatigueLimiting = 1u << 31,
}
