namespace ArcNET.GameObjects;

/// <summary>Armor-specific flags (OARF_).</summary>
[Flags]
public enum ObjFArmorFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Armor is sized for small-sized races.</summary>
    SizeSmall = 0x1,

    /// <summary>Armor is sized for medium-sized races.</summary>
    SizeMedium = 0x2,

    /// <summary>Armor is sized for large-sized races.</summary>
    SizeLarge = 0x4,

    /// <summary>Armor can only be worn by male characters.</summary>
    MaleOnly = 0x8,

    /// <summary>Armor can only be worn by female characters.</summary>
    FemaleOnly = 0x10,
}
