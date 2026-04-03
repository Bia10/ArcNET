namespace ArcNET.Formats;

/// <summary>
/// Bitfield flags from the ART file header that describe the art type and rendering behaviour.
/// </summary>
[Flags]
public enum ArtFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Static art with a single direction (1 rotation).</summary>
    Static = 0x1,

    /// <summary>Critter art with eight directions (8 rotations).</summary>
    Critter = 0x2,

    /// <summary>Font art — glyphs packed into frames.</summary>
    Font = 0x4,
}
