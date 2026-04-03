namespace ArcNET.GameObjects;

/// <summary>Portal-specific flags (OPF_).</summary>
[Flags]
public enum ObjFPortalFlags : ushort
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Portal is locked.</summary>
    Locked = 0x1,

    /// <summary>Portal is jammed and cannot be opened by normal means.</summary>
    Jammed = 0x2,

    /// <summary>Portal is held shut by a magical effect.</summary>
    MagicallyHeld = 0x4,

    /// <summary>Portal can never be locked.</summary>
    NeverLocked = 0x8,

    /// <summary>Portal is always locked regardless of player actions.</summary>
    AlwaysLocked = 0x10,

    /// <summary>Portal is locked during the day.</summary>
    LockedDay = 0x20,

    /// <summary>Portal is locked at night.</summary>
    LockedNight = 0x40,

    /// <summary>Portal lock has been busted open.</summary>
    Busted = 0x80,

    /// <summary>Portal closes automatically after interaction.</summary>
    Sticky = 0x100,
}
