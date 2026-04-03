namespace ArcNET.GameObjects;

/// <summary>Weapon-specific flags (OWF_).</summary>
[Flags]
public enum ObjFWeaponFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Weapon makes noise when used.</summary>
    Loud = 0x1,

    /// <summary>Weapon is silent when used.</summary>
    Silent = 0x2,

    /// <summary>Weapon requires two hands to wield.</summary>
    TwoHanded = 0x4,

    /// <summary>Hand count for this weapon is fixed and cannot be changed.</summary>
    HandCountFixed = 0x8,

    /// <summary>Weapon can be thrown.</summary>
    Throwable = 0x10,

    /// <summary>Weapon's projectile is transparent.</summary>
    TransProjectile = 0x20,

    /// <summary>Weapon boomerangs back to the attacker after being thrown.</summary>
    Boomerangs = 0x40,

    /// <summary>Weapon ignores damage resistance.</summary>
    IgnoreResistance = 0x80,

    /// <summary>Weapon damages the target's armor.</summary>
    DamageArmor = 0x100,

    /// <summary>Weapon defaults to throwing mode.</summary>
    DefaultThrows = 0x200,
}
