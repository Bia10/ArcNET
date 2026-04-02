namespace ArcNET.GameObjects;

/// <summary>General object flags shared by all game-object types.</summary>
[Flags]
public enum ObjFFlags : uint
{
    /// <summary>Object has been destroyed.</summary>
    Destroyed = 1 << 0,

    /// <summary>Object is switched off.</summary>
    Off = 1 << 1,

    /// <summary>Object is flat on the ground.</summary>
    Flat = 1 << 2,

    /// <summary>Object has a floating text label.</summary>
    Text = 1 << 3,

    /// <summary>Object can be seen through.</summary>
    SeeThrough = 1 << 4,

    /// <summary>Projectiles can pass through this object.</summary>
    ShootThrough = 1 << 5,

    /// <summary>Object is rendered translucently.</summary>
    Translucent = 1 << 6,

    /// <summary>Object is displayed at a reduced scale.</summary>
    Shrunk = 1 << 7,

    /// <summary>Object is not drawn.</summary>
    DontDraw = 1 << 8,

    /// <summary>Object is invisible.</summary>
    Invisible = 1 << 9,

    /// <summary>Object does not block movement.</summary>
    NoBlock = 1 << 10,

    /// <summary>Mouse clicks pass through this object.</summary>
    ClickThrough = 1 << 11,

    /// <summary>Object is stored in an inventory.</summary>
    Inventory = 1 << 12,

    /// <summary>Object can be moved dynamically.</summary>
    Dynamic = 1 << 13,

    /// <summary>Object provides partial cover.</summary>
    ProvidesCover = 1 << 14,

    /// <summary>Object has overlay art layers.</summary>
    HasOverlays = 1 << 15,

    /// <summary>Object has underlay art layers.</summary>
    HasUnderlays = 1 << 16,

    /// <summary>Object wades through water.</summary>
    Wading = 1 << 17,

    /// <summary>Object walks on water surfaces.</summary>
    WaterWalking = 1 << 18,

    /// <summary>Object has been turned to stone.</summary>
    Stoned = 1 << 19,

    /// <summary>Object does not receive dynamic lighting.</summary>
    DontLight = 1 << 20,

    /// <summary>Object has a floating text effect.</summary>
    TextFloater = 1 << 21,

    /// <summary>Object cannot take damage.</summary>
    Invulnerable = 1 << 22,

    /// <summary>Object has been removed from existence.</summary>
    Extinct = 1 << 23,

    /// <summary>Object is a player-character trap.</summary>
    TrapPc = 1 << 24,

    /// <summary>Trap has been spotted by a critter.</summary>
    TrapSpotted = 1 << 25,

    /// <summary>Object cannot be waded through.</summary>
    DisallowWading = 1 << 26,

    /// <summary>Object is locked for multiplayer synchronization.</summary>
    MultiplayerLock = 1 << 27,

    /// <summary>Object is frozen in place.</summary>
    Frozen = 1 << 28,

    /// <summary>Object is an animated corpse.</summary>
    AnimatedDead = 1 << 29,

    /// <summary>Object has been teleported this frame.</summary>
    Teleported = 1 << 30,
}
