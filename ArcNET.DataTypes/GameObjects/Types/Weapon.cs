using System;

namespace ArcNET.DataTypes.GameObjects.Types;

public class Weapon : Item
{
    [Order(60)] public int ObjFWeaponFlags { get; set; }
    [Order(61)] public int ObjFWeaponPaperDollAid { get; set; } //string
    [Order(62)] public int ObjFWeaponBonusToHit { get; set; }
    [Order(63)] public int ObjFWeaponMagicHitAdj { get; set; }
    [Order(64)] public Tuple<int[], int[]> ObjFWeaponDamageLowerIdx { get; set; }
    [Order(65)] public Tuple<int[], int[]> ObjFWeaponDamageUpperIdx { get; set; }
    [Order(66)] public Tuple<int[], int[]> ObjFWeaponMagicDamageAdjIdx { get; set; }
    [Order(67)] public int ObjFWeaponSpeedFactor { get; set; }
    [Order(68)] public int ObjFWeaponMagicSpeedAdj { get; set; }
    [Order(69)] public int ObjFWeaponRange { get; set; }
    [Order(70)] public int ObjFWeaponMagicRangeAdj { get; set; }
    [Order(71)] public int ObjFWeaponMinStrength { get; set; }
    [Order(72)] public int ObjFWeaponMagicMinStrengthAdj { get; set; }
    [Order(73)] public int ObjFWeaponAmmoType { get; set; } //enum
    [Order(74)] public int ObjFWeaponAmmoConsumption { get; set; }
    [Order(75)] public int ObjFWeaponMissileAid { get; set; } //string
    [Order(76)] public int ObjFWeaponVisualEffectAid { get; set; } //string
    [Order(77)] public int ObjFWeaponCritHitChart { get; set; }
    [Order(78)] public int ObjFWeaponMagicCritHitChance { get; set; }
    [Order(79)] public int ObjFWeaponMagicCritHitEffect { get; set; }
    [Order(80)] public int ObjFWeaponCritMissChart { get; set; }
    [Order(81)] public int ObjFWeaponMagicCritMissChance { get; set; }
    [Order(82)] public int ObjFWeaponMagicCritMissEffect { get; set; }
    [Order(83)] public int ObjFWeaponPadI1 { get; set; }
    [Order(84)] public int ObjFWeaponPadI2 { get; set; }
    [Order(85)] public Unknown ObjFWeaponPadIas1 { get; set; }
    [Order(86)] public Unknown ObjFWeaponPadI64As1 { get; set; }
}