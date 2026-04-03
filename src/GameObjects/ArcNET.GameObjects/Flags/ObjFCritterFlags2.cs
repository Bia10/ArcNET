namespace ArcNET.GameObjects;

/// <summary>Critter-specific flags (second set).</summary>
[Flags]
public enum ObjFCritterFlags2 : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Item was stolen.</summary>
    ItemStolen = 0x1,

    /// <summary>Critter auto-animates.</summary>
    AutoAnimates = 0x2,

    /// <summary>Critter is using a boomerang weapon.</summary>
    UsingBoomerang = 0x4,

    /// <summary>Critter drains fatigue from others.</summary>
    FatigueDraining = 0x8,

    /// <summary>Critter slows the player party.</summary>
    SlowParty = 0x10,

    /// <summary>Combat toggle visual effect is active.</summary>
    CombatToggleFx = 0x20,

    /// <summary>Critter's corpse does not decay.</summary>
    NoDecay = 0x40,

    /// <summary>Critter cannot be pickpocketed.</summary>
    NoPickpocket = 0x80,

    /// <summary>Critter does not leave blood splotches.</summary>
    NoBloodSplotches = 0x100,

    /// <summary>Critter is nearly invulnerable.</summary>
    NighInvulnerable = 0x200,

    /// <summary>Critter is an elemental.</summary>
    Elemental = 0x400,

    /// <summary>Critter has dark-sight ability.</summary>
    DarkSight = 0x800,

    /// <summary>Critter does not slip on ice.</summary>
    NoSlip = 0x1000,

    /// <summary>Critter is immune to disintegrate effects.</summary>
    NoDisintegrate = 0x2000,

    /// <summary>Critter reaction level 0.</summary>
    Reaction0 = 0x4000,

    /// <summary>Critter reaction level 1.</summary>
    Reaction1 = 0x8000,

    /// <summary>Critter reaction level 2.</summary>
    Reaction2 = 0x10000,

    /// <summary>Critter reaction level 3.</summary>
    Reaction3 = 0x20000,

    /// <summary>Critter reaction level 4.</summary>
    Reaction4 = 0x40000,

    /// <summary>Critter reaction level 5.</summary>
    Reaction5 = 0x80000,

    /// <summary>Critter reaction level 6.</summary>
    Reaction6 = 0x100000,

    /// <summary>Critter is the current target lock.</summary>
    TargetLock = 0x200000,

    /// <summary>Critter is permanently polymorphed.</summary>
    PermaPolymorph = 0x400000,

    /// <summary>Critter is safely off-screen.</summary>
    SafeOff = 0x800000,

    /// <summary>Reaction check: bad threshold.</summary>
    CheckReactionBad = 0x1000000,

    /// <summary>Alignment check: good.</summary>
    CheckAlignGood = 0x2000000,

    /// <summary>Alignment check: bad.</summary>
    CheckAlignBad = 0x4000000,
}
