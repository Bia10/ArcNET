namespace ArcNET.GameObjects;

/// <summary>The major category of a game object.</summary>
public enum ObjectType : uint
{
    /// <summary>Wall objects that block movement.</summary>
    Wall = 0,

    /// <summary>Portal / door objects.</summary>
    Portal,

    /// <summary>Container objects (chests, bags).</summary>
    Container,

    /// <summary>Scenery objects (decorations).</summary>
    Scenery,

    /// <summary>Projectile objects (arrows, bullets).</summary>
    Projectile,

    /// <summary>Weapon items.</summary>
    Weapon,

    /// <summary>Ammunition items.</summary>
    Ammo,

    /// <summary>Armor and clothing items.</summary>
    Armor,

    /// <summary>Gold currency.</summary>
    Gold,

    /// <summary>Food items.</summary>
    Food,

    /// <summary>Scroll items.</summary>
    Scroll,

    /// <summary>Key items.</summary>
    Key,

    /// <summary>Key ring items.</summary>
    KeyRing,

    /// <summary>Written items (books, notes).</summary>
    Written,

    /// <summary>Generic items.</summary>
    Generic,

    /// <summary>Player character.</summary>
    Pc,

    /// <summary>Non-player character.</summary>
    Npc,

    /// <summary>Trap objects.</summary>
    Trap,
}
