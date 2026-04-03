namespace ArcNET.GameObjects;

/// <summary>Container-specific flags (OCOF_).</summary>
[Flags]
public enum ObjFContainerFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Container is locked.</summary>
    Locked = 0x1,

    /// <summary>Container is jammed and cannot be opened by normal means.</summary>
    Jammed = 0x2,

    /// <summary>Container is held shut by a magical effect.</summary>
    MagicallyHeld = 0x4,

    /// <summary>Container can never be locked.</summary>
    NeverLocked = 0x8,

    /// <summary>Container is always locked regardless of player actions.</summary>
    AlwaysLocked = 0x10,

    /// <summary>Container is locked during the day.</summary>
    LockedDay = 0x20,

    /// <summary>Container is locked at night.</summary>
    LockedNight = 0x40,

    /// <summary>Container lock has been busted open.</summary>
    Busted = 0x80,

    /// <summary>Container closes automatically after interaction.</summary>
    Sticky = 0x100,

    /// <summary>Inventory source spawns items only once.</summary>
    InvenSpawnOnce = 0x200,

    /// <summary>Inventory source spawns items independently of the container's state.</summary>
    InvenSpawnIndependent = 0x400,
}
