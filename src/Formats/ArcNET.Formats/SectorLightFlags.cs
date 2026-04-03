namespace ArcNET.Formats;

/// <summary>
/// Behaviour flags on a sector light source, matching the <c>LF_*</c> constants from
/// <c>arcanum-ce/src/game/light.h</c>.
/// </summary>
[Flags]
public enum SectorLightFlags : uint
{
    /// <summary>No flags set — light is active and behaves normally.</summary>
    None = 0,

    /// <summary>Light is currently switched off.</summary>
    Off = 0x01,

    /// <summary>Dark light — subtracts brightness rather than adding it.</summary>
    Dark = 0x02,

    /// <summary>Light is animated (e.g. flickering torch or pulsing magic glow).</summary>
    Animating = 0x04,

    /// <summary>Light is only active while the party is indoors.</summary>
    Indoor = 0x08,

    /// <summary>Light is only active while the party is outdoors.</summary>
    Outdoor = 0x10,
}
