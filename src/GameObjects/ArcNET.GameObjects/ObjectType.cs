namespace ArcNET.GameObjects;

/// <summary>The major category of a game object.</summary>
public enum ObjectType : byte
{
    /// <summary>Wall objects that block movement.</summary>
    Wall = 0,

    /// <summary>Portal / door objects.</summary>
    Portal = 1,

    /// <summary>Container objects (chests, bags).</summary>
    Container = 2,

    /// <summary>Scenery objects (decorations).</summary>
    Scenery = 3,

    /// <summary>Projectile objects (arrows, bullets).</summary>
    Projectile = 4,

    /// <summary>Weapon items.</summary>
    Weapon = 5,

    /// <summary>Ammunition items.</summary>
    Ammo = 6,

    /// <summary>Armor and clothing items.</summary>
    Armor = 7,

    /// <summary>Gold currency.</summary>
    Gold = 8,

    /// <summary>Food items.</summary>
    Food = 9,

    /// <summary>Scroll items.</summary>
    Scroll = 10,

    /// <summary>Key items.</summary>
    Key = 11,

    /// <summary>Key ring items.</summary>
    KeyRing = 12,

    /// <summary>Written items (books, notes).</summary>
    Written = 13,

    /// <summary>Generic items.</summary>
    Generic = 14,

    /// <summary>Player character.</summary>
    Pc = 15,

    /// <summary>Non-player character.</summary>
    Npc = 16,

    /// <summary>Trap objects.</summary>
    Trap = 17,
}
