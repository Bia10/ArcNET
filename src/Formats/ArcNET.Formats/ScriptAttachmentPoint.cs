namespace ArcNET.Formats;

/// <summary>
/// Script attachment points (SAP_* enum).
/// Determines when a script entry is evaluated (e.g. on examine, on use, on dialog).
/// </summary>
public enum ScriptAttachmentPoint : byte
{
    /// <summary>Triggered when the object is examined.</summary>
    Examine = 0,

    /// <summary>Triggered when the object is used.</summary>
    Use = 1,

    /// <summary>Triggered when the object is destroyed.</summary>
    Destroy = 2,

    /// <summary>Triggered when the object is unlocked.</summary>
    Unlock = 3,

    /// <summary>Triggered when the object is picked up.</summary>
    Get = 4,

    /// <summary>Triggered when the object is dropped.</summary>
    Drop = 5,

    /// <summary>Triggered when the object is thrown.</summary>
    Throw = 6,

    /// <summary>Triggered when the object is hit.</summary>
    Hit = 7,

    /// <summary>Triggered when an attack misses.</summary>
    Miss = 8,

    /// <summary>Triggered when the object's dialog begins.</summary>
    Dialog = 9,

    /// <summary>Triggered on the first heartbeat after spawn.</summary>
    FirstHeartbeat = 10,

    /// <summary>Triggered when a PC is caught stealing from this NPC.</summary>
    CatchingThiefPc = 11,

    /// <summary>Triggered when the object or NPC is dying.</summary>
    Dying = 12,

    /// <summary>Triggered when entering combat.</summary>
    EnterCombat = 13,

    /// <summary>Triggered when exiting combat.</summary>
    ExitCombat = 14,

    /// <summary>Triggered at the start of a combat round.</summary>
    StartCombat = 15,

    /// <summary>Triggered at the end of a combat round.</summary>
    EndCombat = 16,

    /// <summary>Triggered when an item is purchased from this NPC.</summary>
    BuyObject = 17,

    /// <summary>Triggered when a critter is resurrected.</summary>
    Resurrect = 18,

    /// <summary>Triggered each heartbeat tick.</summary>
    Heartbeat = 19,

    /// <summary>Triggered when the party leader kills something.</summary>
    LeaderKilling = 20,

    /// <summary>Triggered when an item is inserted into a container.</summary>
    InsertItem = 21,

    /// <summary>Triggered to check whether the NPC should attack on sight.</summary>
    WillKos = 22,

    /// <summary>Triggered when the object or NPC is taking damage.</summary>
    TakingDamage = 23,

    /// <summary>Triggered when a weapon is wielded.</summary>
    WieldOn = 24,

    /// <summary>Triggered when a weapon is unwielded.</summary>
    WieldOff = 25,

    /// <summary>Triggered when a critter successfully hits a target.</summary>
    CritterHits = 26,

    /// <summary>Triggered when entering a new sector.</summary>
    NewSector = 27,

    /// <summary>Triggered when an item is removed from a container.</summary>
    RemoveItem = 28,

    /// <summary>Triggered when the party leader sleeps.</summary>
    LeaderSleeping = 29,

    /// <summary>Triggered when a container or portal is busted open.</summary>
    Bust = 30,

    /// <summary>Triggered to override dialog behavior.</summary>
    DialogOverride = 31,

    /// <summary>Triggered when the object is transferred.</summary>
    Transfer = 32,

    /// <summary>Triggered when a thief is caught.</summary>
    CaughtThief = 33,

    /// <summary>Triggered on a critical hit.</summary>
    CriticalHit = 34,

    /// <summary>Triggered on a critical miss.</summary>
    CriticalMiss = 35,
}
