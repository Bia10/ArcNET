using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectProjectile : ObjectCommon
{
    private int _projectilePadI1Reserved;
    private int _projectilePadI2Reserved;
    private int _projectilePadIas1Reserved;
    private long _projectilePadI64As1Reserved;

    public int CombatFlags { get; internal set; }
    public int CombatDamageFlags { get; internal set; }
    public Location HitLoc { get; internal set; }
    public int ParentWeapon { get; internal set; }

    internal static ObjectProjectile Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectProjectile();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFProjectileFlagsCombat))
            obj.CombatFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectileFlagsCombatDamage))
            obj.CombatDamageFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectileHitLoc))
            obj.HitLoc = reader.ReadLocation();
        if (Bit(ObjectField.ObjFProjectileParentWeapon))
            obj.ParentWeapon = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadI1))
            obj._projectilePadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadI2))
            obj._projectilePadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadIas1))
            obj._projectilePadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadI64As1))
            obj._projectilePadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFProjectileFlagsCombat))
            writer.WriteInt32(CombatFlags);
        if (Bit(ObjectField.ObjFProjectileFlagsCombatDamage))
            writer.WriteInt32(CombatDamageFlags);
        if (Bit(ObjectField.ObjFProjectileHitLoc))
            HitLoc.Write(ref writer);
        if (Bit(ObjectField.ObjFProjectileParentWeapon))
            writer.WriteInt32(ParentWeapon);
        if (Bit(ObjectField.ObjFProjectilePadI1))
            writer.WriteInt32(_projectilePadI1Reserved);
        if (Bit(ObjectField.ObjFProjectilePadI2))
            writer.WriteInt32(_projectilePadI2Reserved);
        if (Bit(ObjectField.ObjFProjectilePadIas1))
            writer.WriteInt32(_projectilePadIas1Reserved);
        if (Bit(ObjectField.ObjFProjectilePadI64As1))
            writer.WriteInt64(_projectilePadI64As1Reserved);
    }
}
