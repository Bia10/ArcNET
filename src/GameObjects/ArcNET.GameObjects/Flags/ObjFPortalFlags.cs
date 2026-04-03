namespace ArcNET.GameObjects;

/// <summary>Portal-specific flags (OPF_ in arcanum-ce obj_flags.h).</summary>
[Flags]
public enum ObjFPortalFlags : uint
{
    /// <summary>Portal is locked.</summary>
    Locked = 1 << 0,

    /// <summary>Portal is jammed and cannot be opened by normal means.</summary>
    Jammed = 1 << 1,

    /// <summary>Portal is held shut by a magical effect.</summary>
    MagicallyHeld = 1 << 2,

    /// <summary>Portal can never be locked.</summary>
    NeverLocked = 1 << 3,

    /// <summary>Portal is always locked regardless of player actions.</summary>
    AlwaysLocked = 1 << 4,

    /// <summary>Portal is locked during the day.</summary>
    LockedDay = 1 << 5,

    /// <summary>Portal is locked at night.</summary>
    LockedNight = 1 << 6,

    /// <summary>Portal lock has been busted open.</summary>
    Busted = 1 << 7,

    /// <summary>Portal closes automatically after interaction.</summary>
    Sticky = 1 << 8,
}
