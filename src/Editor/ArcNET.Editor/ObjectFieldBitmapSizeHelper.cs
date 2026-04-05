using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Editor-local mirror of the internal <c>ObjectFieldBitmapSize</c> from ArcNET.GameObjects.
/// Returns the bitmap byte-length for each <see cref="ObjectType"/> so that
/// builders can construct correct <see cref="GameObjectHeader"/> instances without
/// depending on the internal API.
/// </summary>
internal static class ObjectFieldBitmapSizeHelper
{
    internal static int For(ObjectType type) =>
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
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown object type"),
        };
}
