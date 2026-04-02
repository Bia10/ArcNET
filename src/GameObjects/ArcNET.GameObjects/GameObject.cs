using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects.Types;

namespace ArcNET.GameObjects;

/// <summary>
/// A fully parsed game object: header + type-specific data.  Construct via
/// <see cref="Read(ref SpanReader)"/>.
/// </summary>
public sealed class GameObject : IGameObject
{
    public required GameObjectHeader Header { get; init; }
    public required ObjectCommon Common { get; init; }

    public ObjectType Type => Header.GameObjectType;
    public GameObjectGuid ObjectId => Header.ObjectId;
    public GameObjectGuid ProtoId => Header.ProtoId;
    public bool IsPrototype => Header.IsPrototype;

    /// <summary>
    /// Reads a complete game object (header + type body) from <paramref name="reader"/>.
    /// Prototype objects (where <see cref="GameObjectHeader.IsPrototype"/> is true) include all
    /// fields; non-prototype objects inherit absent fields from the prototype.
    /// </summary>
    public static GameObject Read(ref SpanReader reader)
    {
        var header = GameObjectHeader.Read(ref reader);
        var bitmap = header.Bitmap;
        var isProto = header.IsPrototype;

        ObjectCommon common = header.GameObjectType switch
        {
            ObjectType.Wall => ObjectWall.Read(ref reader, bitmap, isProto),
            ObjectType.Portal => ObjectPortal.Read(ref reader, bitmap, isProto),
            ObjectType.Container => ObjectContainer.Read(ref reader, bitmap, isProto),
            ObjectType.Scenery => ObjectScenery.Read(ref reader, bitmap, isProto),
            ObjectType.Projectile => ObjectProjectile.Read(ref reader, bitmap, isProto),
            ObjectType.Trap => ObjectTrap.Read(ref reader, bitmap, isProto),
            ObjectType.Weapon => ObjectWeapon.Read(ref reader, bitmap, isProto),
            ObjectType.Ammo => ObjectAmmo.Read(ref reader, bitmap, isProto),
            ObjectType.Armor => ObjectArmor.Read(ref reader, bitmap, isProto),
            ObjectType.Gold => ObjectGold.Read(ref reader, bitmap, isProto),
            ObjectType.Food => ObjectFood.Read(ref reader, bitmap, isProto),
            ObjectType.Scroll => ObjectScroll.Read(ref reader, bitmap, isProto),
            ObjectType.Key => ObjectKey.Read(ref reader, bitmap, isProto),
            ObjectType.KeyRing => ObjectKeyRing.Read(ref reader, bitmap, isProto),
            ObjectType.Written => ObjectWritten.Read(ref reader, bitmap, isProto),
            ObjectType.Generic => ObjectGeneric.Read(ref reader, bitmap, isProto),
            ObjectType.Pc => ObjectPc.Read(ref reader, bitmap, isProto),
            ObjectType.Npc => ObjectNpc.Read(ref reader, bitmap, isProto),
            _ => new ObjectUnknown(),
        };

        return new GameObject { Header = header, Common = common };
    }
}
