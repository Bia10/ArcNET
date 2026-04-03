namespace ArcNET.GameObjects;

/// <summary>Armor-specific flags (OARF_ in arcanum-ce obj_flags.h).</summary>
[Flags]
public enum ObjFArmorFlags : uint
{
    /// <summary>Armor is sized for small-sized races.</summary>
    SizeSmall = 1 << 0,

    /// <summary>Armor is sized for medium-sized races.</summary>
    SizeMedium = 1 << 1,

    /// <summary>Armor is sized for large-sized races.</summary>
    SizeLarge = 1 << 2,

    /// <summary>Armor can only be worn by male characters.</summary>
    MaleOnly = 1 << 3,

    /// <summary>Armor can only be worn by female characters.</summary>
    FemaleOnly = 1 << 4,
}
