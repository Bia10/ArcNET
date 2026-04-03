namespace ArcNET.GameObjects;

/// <summary>Active magical spell / status effect flags on a critter.</summary>
[Flags]
public enum ObjFSpellFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Critter is invisible.</summary>
    Invisible = 0x1,

    /// <summary>Critter is floating in the air.</summary>
    Floating = 0x2,

    /// <summary>Body-of-Air transformation is active.</summary>
    BodyOfAir = 0x4,

    /// <summary>Body-of-Earth transformation is active.</summary>
    BodyOfEarth = 0x8,

    /// <summary>Body-of-Fire transformation is active.</summary>
    BodyOfFire = 0x10,

    /// <summary>Body-of-Water transformation is active.</summary>
    BodyOfWater = 0x20,

    /// <summary>Critter is detecting magic.</summary>
    DetectingMagic = 0x40,

    /// <summary>Critter is detecting alignment.</summary>
    DetectingAlignment = 0x80,

    /// <summary>Critter is detecting traps.</summary>
    DetectingTraps = 0x100,

    /// <summary>Critter is detecting invisible creatures.</summary>
    DetectingInvisible = 0x200,

    /// <summary>Critter has a magical shield active.</summary>
    Shielded = 0x400,

    /// <summary>Anti-magic shell is active.</summary>
    AntiMagicShell = 0x800,

    /// <summary>Bonds-of-magic effect is active.</summary>
    BondsOfMagic = 0x1000,

    /// <summary>Full spell reflection is active.</summary>
    FullReflection = 0x2000,

    /// <summary>Critter was magically summoned.</summary>
    Summoned = 0x4000,

    /// <summary>Critter is an illusion.</summary>
    Illusion = 0x8000,

    /// <summary>Critter has been turned to stone.</summary>
    Stoned = 0x10000,

    /// <summary>Critter is polymorphed.</summary>
    Polymorphed = 0x20000,

    /// <summary>Mirror image effect active.</summary>
    Mirrored = 0x40000,

    /// <summary>Critter is magically shrunk.</summary>
    Shrunk = 0x80000,

    /// <summary>Critter is inside a passwall effect.</summary>
    Passwalled = 0x100000,

    /// <summary>Critter is walking on water.</summary>
    WaterWalking = 0x200000,

    /// <summary>Magnetic inversion effect active.</summary>
    MagneticInversion = 0x400000,

    /// <summary>Critter is charmed.</summary>
    Charmed = 0x800000,

    /// <summary>Critter is entangled.</summary>
    Entangled = 0x1000000,

    /// <summary>Critter has spoken with dead.</summary>
    SpokenWithDead = 0x2000000,

    /// <summary>Tempus fugit (time stop) effect active.</summary>
    TempusFugit = 0x4000000,

    /// <summary>Critter is under mind control.</summary>
    MindControlled = 0x8000000,

    /// <summary>Critter is drunk.</summary>
    Drunk = 0x10000000,

    /// <summary>Enshroud effect active.</summary>
    Enshrouded = 0x20000000,

    /// <summary>Critter is a familiar.</summary>
    Familiar = 0x40000000,

    /// <summary>Hardened hands effect active.</summary>
    HardenedHands = 1u << 31,
}
