using System.Collections.Frozen;
using ArcNET.Core;
using ArcNET.GameObjects.Types;

namespace ArcNET.GameObjects;

internal static class ObjectTypeRegistry
{
    private delegate ObjectCommon ObjectReader(ref SpanReader reader, byte[] bitmap, bool isPrototype);
    private delegate void ObjectWriter(ObjectCommon value, ref SpanWriter writer, byte[] bitmap, bool isPrototype);

    private sealed record Registration(ObjectType ObjectType, Type ModelType, ObjectReader Read, ObjectWriter Write);

    private static readonly Registration[] s_registrations =
    [
        Register<ObjectWall>(ObjectType.Wall, ObjectWall.Read),
        Register<ObjectPortal>(ObjectType.Portal, ObjectPortal.Read),
        Register<ObjectContainer>(ObjectType.Container, ObjectContainer.Read),
        Register<ObjectScenery>(ObjectType.Scenery, ObjectScenery.Read),
        Register<ObjectProjectile>(ObjectType.Projectile, ObjectProjectile.Read),
        Register<ObjectTrap>(ObjectType.Trap, ObjectTrap.Read),
        Register<ObjectWeapon>(ObjectType.Weapon, ObjectWeapon.Read),
        Register<ObjectAmmo>(ObjectType.Ammo, ObjectAmmo.Read),
        Register<ObjectArmor>(ObjectType.Armor, ObjectArmor.Read),
        Register<ObjectGold>(ObjectType.Gold, ObjectGold.Read),
        Register<ObjectFood>(ObjectType.Food, ObjectFood.Read),
        Register<ObjectScroll>(ObjectType.Scroll, ObjectScroll.Read),
        Register<ObjectKey>(ObjectType.Key, ObjectKey.Read),
        Register<ObjectKeyRing>(ObjectType.KeyRing, ObjectKeyRing.Read),
        Register<ObjectWritten>(ObjectType.Written, ObjectWritten.Read),
        Register<ObjectGeneric>(ObjectType.Generic, ObjectGeneric.Read),
        Register<ObjectPc>(ObjectType.Pc, ObjectPc.Read),
        Register<ObjectNpc>(ObjectType.Npc, ObjectNpc.Read),
    ];

    private static readonly FrozenDictionary<ObjectType, Registration> s_registrationsByObjectType =
        s_registrations.ToFrozenDictionary(static registration => registration.ObjectType);

    private static readonly FrozenDictionary<Type, Registration> s_registrationsByModelType =
        s_registrations.ToFrozenDictionary(static registration => registration.ModelType);

    public static ObjectCommon Read(ObjectType objectType, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var registration = GetRegistration(objectType);
        return registration.Read(ref reader, bitmap, isPrototype);
    }

    public static void Validate(ObjectType objectType, ObjectCommon value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var expected = GetRegistration(objectType);
        if (!s_registrationsByModelType.TryGetValue(value.GetType(), out var actual))
        {
            throw new InvalidDataException(
                $"Unsupported object model type '{value.GetType().Name}' has no registered codec."
            );
        }

        if (!ReferenceEquals(expected, actual))
        {
            throw new InvalidDataException(
                $"Object header type '{objectType}' requires body model '{expected.ModelType.Name}', but got '{value.GetType().Name}'."
            );
        }
    }

    public static void Write(
        ObjectType objectType,
        ObjectCommon value,
        ref SpanWriter writer,
        byte[] bitmap,
        bool isPrototype
    )
    {
        Validate(objectType, value);
        var registration = s_registrationsByObjectType[objectType];
        registration.Write(value, ref writer, bitmap, isPrototype);
    }

    private static Registration GetRegistration(ObjectType objectType)
    {
        if (!s_registrationsByObjectType.TryGetValue(objectType, out var registration))
            throw new InvalidDataException($"Unsupported object type '{objectType}' has no registered codec.");

        return registration;
    }

    private static Registration Register<TObject>(ObjectType objectType, ObjectReader read)
        where TObject : ObjectCommon => new(objectType, typeof(TObject), read, Write<TObject>());

    private static ObjectWriter Write<TObject>()
        where TObject : ObjectCommon =>
        static (ObjectCommon value, ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
            ((TObject)value).Write(ref writer, bitmap, isPrototype);
}
