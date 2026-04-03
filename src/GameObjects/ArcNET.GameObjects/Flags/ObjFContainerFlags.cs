namespace ArcNET.GameObjects;

/// <summary>Container-specific flags (OCOF_ in arcanum-ce obj_flags.h).</summary>
[Flags]
public enum ObjFContainerFlags : uint
{
    /// <summary>Container is locked.</summary>
    Locked = 1 << 0,

    /// <summary>Container is jammed and cannot be opened by normal means.</summary>
    Jammed = 1 << 1,

    /// <summary>Container is held shut by a magical effect.</summary>
    MagicallyHeld = 1 << 2,

    /// <summary>Container can never be locked.</summary>
    NeverLocked = 1 << 3,

    /// <summary>Container is always locked regardless of player actions.</summary>
    AlwaysLocked = 1 << 4,

    /// <summary>Container is locked during the day.</summary>
    LockedDay = 1 << 5,

    /// <summary>Container is locked at night.</summary>
    LockedNight = 1 << 6,

    /// <summary>Container lock has been busted open.</summary>
    Busted = 1 << 7,

    /// <summary>Container closes automatically after interaction.</summary>
    Sticky = 1 << 8,

    /// <summary>Inventory source spawns items only once.</summary>
    InvenSpawnOnce = 1 << 9,

    /// <summary>Inventory source spawns items independently of the container's state.</summary>
    InvenSpawnIndependent = 1 << 10,
}
