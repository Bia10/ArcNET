namespace ArcNET.GameObjects;

/// <summary>Active magical spell / status effect flags on a critter.</summary>
[Flags]
public enum ObjFSpellFlags : uint
{
    /// <summary>Critter is invisible.</summary>
    Invisible = 1 << 0,

    /// <summary>Critter is floating in the air.</summary>
    Floating = 1 << 1,

    /// <summary>Body-of-Air transformation is active.</summary>
    BodyOfAir = 1 << 2,

    /// <summary>Body-of-Earth transformation is active.</summary>
    BodyOfEarth = 1 << 3,

    /// <summary>Body-of-Fire transformation is active.</summary>
    BodyOfFire = 1 << 4,

    /// <summary>Body-of-Water transformation is active.</summary>
    BodyOfWater = 1 << 5,

    /// <summary>Critter is detecting magic.</summary>
    DetectingMagic = 1 << 6,

    /// <summary>Critter is detecting alignment.</summary>
    DetectingAlignment = 1 << 7,

    /// <summary>Critter is detecting traps.</summary>
    DetectingTraps = 1 << 8,

    /// <summary>Critter is detecting invisible creatures.</summary>
    DetectingInvisible = 1 << 9,

    /// <summary>Critter has a magical shield active.</summary>
    Shielded = 1 << 10,

    /// <summary>Anti-magic shell is active.</summary>
    AntiMagicShell = 1 << 11,

    /// <summary>Bonds-of-magic effect is active.</summary>
    BondsOfMagic = 1 << 12,

    /// <summary>Full spell reflection is active.</summary>
    FullReflection = 1 << 13,

    /// <summary>Critter was magically summoned.</summary>
    Summoned = 1 << 14,

    /// <summary>Critter is an illusion.</summary>
    Illusion = 1 << 15,

    /// <summary>Critter has been turned to stone.</summary>
    Stoned = 1 << 16,

    /// <summary>Critter is polymorphed.</summary>
    Polymorphed = 1 << 17,

    /// <summary>Mirror image effect active.</summary>
    Mirrored = 1 << 18,

    /// <summary>Critter is magically shrunk.</summary>
    Shrunk = 1 << 19,

    /// <summary>Critter is inside a passwall effect.</summary>
    Passwalled = 1 << 20,

    /// <summary>Critter is walking on water.</summary>
    WaterWalking = 1 << 21,

    /// <summary>Magnetic inversion effect active.</summary>
    MagneticInversion = 1 << 22,

    /// <summary>Critter is charmed.</summary>
    Charmed = 1 << 23,

    /// <summary>Critter is entangled.</summary>
    Entangled = 1 << 24,

    /// <summary>Critter has spoken with dead.</summary>
    SpokenWithDead = 1 << 25,

    /// <summary>Tempus fugit (time stop) effect active.</summary>
    TempusFugit = 1 << 26,

    /// <summary>Critter is under mind control.</summary>
    MindControlled = 1 << 27,

    /// <summary>Critter is drunk.</summary>
    Drunk = 1 << 28,

    /// <summary>Enshroud effect active.</summary>
    Enshrouded = 1 << 29,

    /// <summary>Critter is a familiar.</summary>
    Familiar = 1 << 30,

    /// <summary>Hardened hands effect active.</summary>
    HardenedHands = 1u << 31,
}
