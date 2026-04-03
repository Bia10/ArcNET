namespace ArcNET.GameObjects;

/// <summary>General object flags shared by all game-object types.</summary>
[Flags]
public enum ObjFFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Object has been destroyed.</summary>
    Destroyed = 0x1,

    /// <summary>Object is switched off.</summary>
    Off = 0x2,

    /// <summary>Object is flat on the ground.</summary>
    Flat = 0x4,

    /// <summary>Object has a floating text label.</summary>
    Text = 0x8,

    /// <summary>Object can be seen through.</summary>
    SeeThrough = 0x10,

    /// <summary>Projectiles can pass through this object.</summary>
    ShootThrough = 0x20,

    /// <summary>Object is rendered translucently.</summary>
    Translucent = 0x40,

    /// <summary>Object is displayed at a reduced scale.</summary>
    Shrunk = 0x80,

    /// <summary>Object is not drawn.</summary>
    DontDraw = 0x100,

    /// <summary>Object is invisible.</summary>
    Invisible = 0x200,

    /// <summary>Object does not block movement.</summary>
    NoBlock = 0x400,

    /// <summary>Mouse clicks pass through this object.</summary>
    ClickThrough = 0x800,

    /// <summary>Object is stored in an inventory.</summary>
    Inventory = 0x1000,

    /// <summary>Object can be moved dynamically.</summary>
    Dynamic = 0x2000,

    /// <summary>Object provides partial cover.</summary>
    ProvidesCover = 0x4000,

    /// <summary>Object has overlay art layers.</summary>
    HasOverlays = 0x8000,

    /// <summary>Object has underlay art layers.</summary>
    HasUnderlays = 0x10000,

    /// <summary>Object wades through water.</summary>
    Wading = 0x20000,

    /// <summary>Object walks on water surfaces.</summary>
    WaterWalking = 0x40000,

    /// <summary>Object has been turned to stone.</summary>
    Stoned = 0x80000,

    /// <summary>Object does not receive dynamic lighting.</summary>
    DontLight = 0x100000,

    /// <summary>Object has a floating text effect.</summary>
    TextFloater = 0x200000,

    /// <summary>Object cannot take damage.</summary>
    Invulnerable = 0x400000,

    /// <summary>Object has been removed from existence.</summary>
    Extinct = 0x800000,

    /// <summary>Object is a player-character trap.</summary>
    TrapPc = 0x1000000,

    /// <summary>Trap has been spotted by a critter.</summary>
    TrapSpotted = 0x2000000,

    /// <summary>Object cannot be waded through.</summary>
    DisallowWading = 0x4000000,

    /// <summary>Object is locked for multiplayer synchronization.</summary>
    MultiplayerLock = 0x8000000,

    /// <summary>Object is frozen in place.</summary>
    Frozen = 0x10000000,

    /// <summary>Object is an animated corpse.</summary>
    AnimatedDead = 0x20000000,

    /// <summary>Object has been teleported this frame.</summary>
    Teleported = 0x40000000,
}
