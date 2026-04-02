namespace ArcNET.GameObjects;

/// <summary>Critter-specific flags (second set).</summary>
[Flags]
public enum ObjFCritterFlags2 : uint
{
    /// <summary>Item was stolen.</summary>
    ItemStolen = 1 << 0,

    /// <summary>Critter auto-animates.</summary>
    AutoAnimates = 1 << 1,

    /// <summary>Critter is using a boomerang weapon.</summary>
    UsingBoomerang = 1 << 2,

    /// <summary>Critter drains fatigue from others.</summary>
    FatigueDraining = 1 << 3,

    /// <summary>Critter slows the player party.</summary>
    SlowParty = 1 << 4,

    /// <summary>Combat toggle visual effect is active.</summary>
    CombatToggleFx = 1 << 5,

    /// <summary>Critter's corpse does not decay.</summary>
    NoDecay = 1 << 6,

    /// <summary>Critter cannot be pickpocketed.</summary>
    NoPickpocket = 1 << 7,

    /// <summary>Critter does not leave blood splotches.</summary>
    NoBloodSplotches = 1 << 8,

    /// <summary>Critter is nearly invulnerable.</summary>
    NighInvulnerable = 1 << 9,

    /// <summary>Critter is an elemental.</summary>
    Elemental = 1 << 10,

    /// <summary>Critter has dark-sight ability.</summary>
    DarkSight = 1 << 11,

    /// <summary>Critter does not slip on ice.</summary>
    NoSlip = 1 << 12,

    /// <summary>Critter is immune to disintegrate effects.</summary>
    NoDisintegrate = 1 << 13,

    /// <summary>Critter reaction level 0.</summary>
    Reaction0 = 1 << 14,

    /// <summary>Critter reaction level 1.</summary>
    Reaction1 = 1 << 15,

    /// <summary>Critter reaction level 2.</summary>
    Reaction2 = 1 << 16,

    /// <summary>Critter reaction level 3.</summary>
    Reaction3 = 1 << 17,

    /// <summary>Critter reaction level 4.</summary>
    Reaction4 = 1 << 18,

    /// <summary>Critter reaction level 5.</summary>
    Reaction5 = 1 << 19,

    /// <summary>Critter reaction level 6.</summary>
    Reaction6 = 1 << 20,

    /// <summary>Critter is the current target lock.</summary>
    TargetLock = 1 << 21,

    /// <summary>Critter is permanently polymorphed.</summary>
    PermaPolymorph = 1 << 22,

    /// <summary>Critter is safely off-screen.</summary>
    SafeOff = 1 << 23,

    /// <summary>Reaction check: bad threshold.</summary>
    CheckReactionBad = 1 << 24,

    /// <summary>Alignment check: good.</summary>
    CheckAlignGood = 1 << 25,

    /// <summary>Alignment check: bad.</summary>
    CheckAlignBad = 1 << 26,
}
