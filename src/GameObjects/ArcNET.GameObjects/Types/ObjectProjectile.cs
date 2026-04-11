using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectProjectile : ObjectCommon
{
    public int ProjectileFlagsCombat { get; internal set; }
    public int ProjectileFlagsCombatDamage { get; internal set; }
    public Location ProjectileHitLoc { get; internal set; }
    public int ProjectileParentWeapon { get; internal set; }
    public int ProjectilePadI1 { get; internal set; }
    public int ProjectilePadI2 { get; internal set; }
    public int ProjectilePadIas1 { get; internal set; }
    public long ProjectilePadI64As1 { get; internal set; }

    internal static ObjectProjectile Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectProjectile();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFProjectileFlagsCombat))
            obj.ProjectileFlagsCombat = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectileFlagsCombatDamage))
            obj.ProjectileFlagsCombatDamage = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectileHitLoc))
            obj.ProjectileHitLoc = reader.ReadLocation();
        if (Bit(ObjectField.ObjFProjectileParentWeapon))
            obj.ProjectileParentWeapon = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadI1))
            obj.ProjectilePadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadI2))
            obj.ProjectilePadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadIas1))
            obj.ProjectilePadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFProjectilePadI64As1))
            obj.ProjectilePadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFProjectileFlagsCombat))
            writer.WriteInt32(ProjectileFlagsCombat);
        if (Bit(ObjectField.ObjFProjectileFlagsCombatDamage))
            writer.WriteInt32(ProjectileFlagsCombatDamage);
        if (Bit(ObjectField.ObjFProjectileHitLoc))
            ProjectileHitLoc.Write(ref writer);
        if (Bit(ObjectField.ObjFProjectileParentWeapon))
            writer.WriteInt32(ProjectileParentWeapon);
        if (Bit(ObjectField.ObjFProjectilePadI1))
            writer.WriteInt32(ProjectilePadI1);
        if (Bit(ObjectField.ObjFProjectilePadI2))
            writer.WriteInt32(ProjectilePadI2);
        if (Bit(ObjectField.ObjFProjectilePadIas1))
            writer.WriteInt32(ProjectilePadIas1);
        if (Bit(ObjectField.ObjFProjectilePadI64As1))
            writer.WriteInt64(ProjectilePadI64As1);
    }
}
