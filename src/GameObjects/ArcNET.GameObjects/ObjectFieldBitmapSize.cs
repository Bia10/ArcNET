namespace ArcNET.GameObjects;

/// <summary>Bitmap byte-length per object type used when reading object headers.</summary>
public static class ObjectFieldBitmapSize
{
    /// <summary>Returns the number of bytes in the field bitmap for <paramref name="type"/>.</summary>
    public static int For(ObjectType type) =>
        type switch
        {
            ObjectType.Wall => 12,
            ObjectType.Portal => 12,
            ObjectType.Container => 12,
            ObjectType.Scenery => 12,
            ObjectType.Projectile => 12,
            ObjectType.Trap => 12,
            ObjectType.Weapon => 16,
            ObjectType.Ammo => 16,
            ObjectType.Armor => 16,
            ObjectType.Gold => 16,
            ObjectType.Food => 16,
            ObjectType.Scroll => 16,
            ObjectType.Key => 16,
            ObjectType.KeyRing => 16,
            ObjectType.Written => 16,
            ObjectType.Generic => 16,
            ObjectType.Pc => 20,
            ObjectType.Npc => 20,
            // Fail gracefully for unknown types rather than crashing the parser.
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown object type: {(int)type}"),
        };
}
