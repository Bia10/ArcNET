namespace ArcNET.GameObjects;

/// <summary>Weapon-specific flags (OWF_ in arcanum-ce obj_flags.h).</summary>
[Flags]
public enum ObjFWeaponFlags : uint
{
    /// <summary>Weapon makes noise when used.</summary>
    Loud = 1 << 0,

    /// <summary>Weapon is silent when used.</summary>
    Silent = 1 << 1,

    /// <summary>Weapon requires two hands to wield.</summary>
    TwoHanded = 1 << 2,

    /// <summary>Hand count for this weapon is fixed and cannot be changed.</summary>
    HandCountFixed = 1 << 3,

    /// <summary>Weapon can be thrown.</summary>
    Throwable = 1 << 4,

    /// <summary>Weapon's projectile is transparent.</summary>
    TransProjectile = 1 << 5,

    /// <summary>Weapon boomerangs back to the attacker after being thrown.</summary>
    Boomerangs = 1 << 6,

    /// <summary>Weapon ignores damage resistance.</summary>
    IgnoreResistance = 1 << 7,

    /// <summary>Weapon damages the target's armor.</summary>
    DamageArmor = 1 << 8,

    /// <summary>Weapon defaults to throwing mode.</summary>
    DefaultThrows = 1 << 9,
}
