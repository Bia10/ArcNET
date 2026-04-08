using System.Buffers;
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

    /// <summary>
    /// Serialises this game object back to its binary on-disk representation.
    /// The format is identical to the OFF format used by <c>MobFormat</c> and <c>ProtoFormat</c>:
    /// a <see cref="GameObjectHeader"/> followed by type-specific field data in bitmap order.
    /// </summary>
    public byte[] WriteToArray()
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Header.Write(ref writer);
        var bitmap = Header.Bitmap;
        var isProto = Header.IsPrototype;
        switch (Common)
        {
            case ObjectPc pc:
                pc.Write(ref writer, bitmap, isProto);
                break;
            case ObjectNpc npc:
                npc.Write(ref writer, bitmap, isProto);
                break;
            case ObjectWall wall:
                wall.Write(ref writer, bitmap, isProto);
                break;
            case ObjectPortal portal:
                portal.Write(ref writer, bitmap, isProto);
                break;
            case ObjectContainer container:
                container.Write(ref writer, bitmap, isProto);
                break;
            case ObjectScenery scenery:
                scenery.Write(ref writer, bitmap, isProto);
                break;
            case ObjectTrap trap:
                trap.Write(ref writer, bitmap, isProto);
                break;
            case ObjectProjectile projectile:
                projectile.Write(ref writer, bitmap, isProto);
                break;
            case ObjectWeapon weapon:
                weapon.Write(ref writer, bitmap, isProto);
                break;
            case ObjectAmmo ammo:
                ammo.Write(ref writer, bitmap, isProto);
                break;
            case ObjectArmor armor:
                armor.Write(ref writer, bitmap, isProto);
                break;
            case ObjectGold gold:
                gold.Write(ref writer, bitmap, isProto);
                break;
            case ObjectFood food:
                food.Write(ref writer, bitmap, isProto);
                break;
            case ObjectScroll scroll:
                scroll.Write(ref writer, bitmap, isProto);
                break;
            case ObjectKey key:
                key.Write(ref writer, bitmap, isProto);
                break;
            case ObjectKeyRing keyRing:
                keyRing.Write(ref writer, bitmap, isProto);
                break;
            case ObjectWritten written:
                written.Write(ref writer, bitmap, isProto);
                break;
            case ObjectGeneric generic:
                generic.Write(ref writer, bitmap, isProto);
                break;
            case ObjectUnknown unknown:
                unknown.Write(ref writer, bitmap, isProto);
                break;
        }
        return buf.WrittenSpan.ToArray();
    }
}
