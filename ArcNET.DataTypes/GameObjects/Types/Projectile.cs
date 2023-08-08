using ArcNET.DataTypes.Common;

namespace ArcNET.DataTypes.GameObjects.Types;

public class Projectile : Common
{
    [Order(37)] public int ObjFProjectileFlagsCombat { get; set; }
    [Order(38)] public int ObjFProjectileFlagsCombatDamage { get; set; }
    [Order(39)] public Location ObjFProjectileHitLoc { get; set; }
    [Order(40)] public int ObjFProjectileParentWeapon { get; set; }
    [Order(41)] public int ObjFProjectilePadI1 { get; set; }
    [Order(42)] public int ObjFProjectilePadI2 { get; set; }
    [Order(43)] public Unknown ObjFProjectilePadIas1 { get; set; }
    [Order(44)] public Unknown ObjFProjectilePadI64As1 { get; set; }
}