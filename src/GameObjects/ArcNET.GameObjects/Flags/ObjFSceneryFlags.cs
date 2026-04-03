namespace ArcNET.GameObjects;

/// <summary>Scenery-specific flags (OSCF_).</summary>
[Flags]
public enum ObjFSceneryFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Scenery does not auto-animate.</summary>
    NoAutoAnimate = 0x1,

    /// <summary>Scenery has been destroyed / busted.</summary>
    Busted = 0x2,

    /// <summary>Scenery is only active/visible at night.</summary>
    Nocturnal = 0x4,

    /// <summary>Scenery marks a town-map location.</summary>
    MarksTownmap = 0x8,

    /// <summary>Scenery is a fire object.</summary>
    IsFire = 0x10,

    /// <summary>Scenery can respawn after being destroyed.</summary>
    Respawnable = 0x20,

    /// <summary>Scenery emits small ambient sound.</summary>
    SoundSmall = 0x40,

    /// <summary>Scenery emits medium ambient sound.</summary>
    SoundMedium = 0x80,

    /// <summary>Scenery emits extra-large ambient sound.</summary>
    SoundExtraLarge = 0x100,

    /// <summary>Scenery is rendered beneath all other objects.</summary>
    UnderAll = 0x200,

    /// <summary>Scenery is currently in the process of respawning.</summary>
    Respawning = 0x400,
}
