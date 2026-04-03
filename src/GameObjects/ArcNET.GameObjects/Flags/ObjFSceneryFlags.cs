namespace ArcNET.GameObjects;

/// <summary>Scenery-specific flags (OSCF_ in arcanum-ce obj_flags.h).</summary>
[Flags]
public enum ObjFSceneryFlags : uint
{
    /// <summary>Scenery does not auto-animate.</summary>
    NoAutoAnimate = 1 << 0,

    /// <summary>Scenery has been destroyed / busted.</summary>
    Busted = 1 << 1,

    /// <summary>Scenery is only active/visible at night.</summary>
    Nocturnal = 1 << 2,

    /// <summary>Scenery marks a town-map location.</summary>
    MarksTownmap = 1 << 3,

    /// <summary>Scenery is a fire object.</summary>
    IsFire = 1 << 4,

    /// <summary>Scenery can respawn after being destroyed.</summary>
    Respawnable = 1 << 5,

    /// <summary>Scenery emits small ambient sound.</summary>
    SoundSmall = 1 << 6,

    /// <summary>Scenery emits medium ambient sound.</summary>
    SoundMedium = 1 << 7,

    /// <summary>Scenery emits extra-large ambient sound.</summary>
    SoundExtraLarge = 1 << 8,

    /// <summary>Scenery is rendered beneath all other objects.</summary>
    UnderAll = 1 << 9,

    /// <summary>Scenery is currently in the process of respawning.</summary>
    Respawning = 1 << 10,
}
