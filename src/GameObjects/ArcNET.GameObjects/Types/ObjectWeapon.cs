using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWeapon : ObjectItem
{
    private int _weaponPadI1Reserved;
    private int _weaponPadI2Reserved;
    private int _weaponPadIas1Reserved;
    private long _weaponPadI64As1Reserved;

    public ObjFWeaponFlags WeaponFlags { get; internal set; }
    public int PaperDollAid { get; internal set; }
    public int BonusToHit { get; internal set; }
    public int MagicHitAdj { get; internal set; }
    public int[] DamageLower { get; internal set; } = [];
    public int[] DamageUpper { get; internal set; } = [];
    public int[] MagicDamageAdj { get; internal set; } = [];
    public int SpeedFactor { get; internal set; }
    public int MagicSpeedAdj { get; internal set; }
    public int Range { get; internal set; }
    public int MagicRangeAdj { get; internal set; }
    public int MinStrength { get; internal set; }
    public int MagicMinStrengthAdj { get; internal set; }
    public int AmmoType { get; internal set; }
    public int AmmoConsumption { get; internal set; }
    public int MissileAid { get; internal set; }
    public int VisualEffectAid { get; internal set; }
    public int CritHitChart { get; internal set; }
    public int MagicCritHitChance { get; internal set; }
    public int MagicCritHitEffect { get; internal set; }
    public int CritMissChart { get; internal set; }
    public int MagicCritMissChance { get; internal set; }
    public int MagicCritMissEffect { get; internal set; }

    internal static ObjectWeapon Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectWeapon();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFWeaponFlags))
            obj.WeaponFlags = unchecked((ObjFWeaponFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFWeaponPaperDollAid))
            obj.PaperDollAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponBonusToHit))
            obj.BonusToHit = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicHitAdj))
            obj.MagicHitAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponDamageLowerIdx))
            obj.DamageLower = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFWeaponDamageUpperIdx))
            obj.DamageUpper = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFWeaponMagicDamageAdjIdx))
            obj.MagicDamageAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFWeaponSpeedFactor))
            obj.SpeedFactor = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicSpeedAdj))
            obj.MagicSpeedAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponRange))
            obj.Range = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicRangeAdj))
            obj.MagicRangeAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMinStrength))
            obj.MinStrength = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicMinStrengthAdj))
            obj.MagicMinStrengthAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponAmmoType))
            obj.AmmoType = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponAmmoConsumption))
            obj.AmmoConsumption = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMissileAid))
            obj.MissileAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponVisualEffectAid))
            obj.VisualEffectAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponCritHitChart))
            obj.CritHitChart = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritHitChance))
            obj.MagicCritHitChance = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritHitEffect))
            obj.MagicCritHitEffect = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponCritMissChart))
            obj.CritMissChart = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritMissChance))
            obj.MagicCritMissChance = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritMissEffect))
            obj.MagicCritMissEffect = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadI1))
            obj._weaponPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadI2))
            obj._weaponPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadIas1))
            obj._weaponPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadI64As1))
            obj._weaponPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFWeaponFlags))
            writer.WriteInt32(unchecked((int)WeaponFlags));
        if (Bit(ObjectField.ObjFWeaponPaperDollAid))
            writer.WriteInt32(PaperDollAid);
        if (Bit(ObjectField.ObjFWeaponBonusToHit))
            writer.WriteInt32(BonusToHit);
        if (Bit(ObjectField.ObjFWeaponMagicHitAdj))
            writer.WriteInt32(MagicHitAdj);
        if (Bit(ObjectField.ObjFWeaponDamageLowerIdx))
            WriteIndexedInts(ref writer, DamageLower);
        if (Bit(ObjectField.ObjFWeaponDamageUpperIdx))
            WriteIndexedInts(ref writer, DamageUpper);
        if (Bit(ObjectField.ObjFWeaponMagicDamageAdjIdx))
            WriteIndexedInts(ref writer, MagicDamageAdj);
        if (Bit(ObjectField.ObjFWeaponSpeedFactor))
            writer.WriteInt32(SpeedFactor);
        if (Bit(ObjectField.ObjFWeaponMagicSpeedAdj))
            writer.WriteInt32(MagicSpeedAdj);
        if (Bit(ObjectField.ObjFWeaponRange))
            writer.WriteInt32(Range);
        if (Bit(ObjectField.ObjFWeaponMagicRangeAdj))
            writer.WriteInt32(MagicRangeAdj);
        if (Bit(ObjectField.ObjFWeaponMinStrength))
            writer.WriteInt32(MinStrength);
        if (Bit(ObjectField.ObjFWeaponMagicMinStrengthAdj))
            writer.WriteInt32(MagicMinStrengthAdj);
        if (Bit(ObjectField.ObjFWeaponAmmoType))
            writer.WriteInt32(AmmoType);
        if (Bit(ObjectField.ObjFWeaponAmmoConsumption))
            writer.WriteInt32(AmmoConsumption);
        if (Bit(ObjectField.ObjFWeaponMissileAid))
            writer.WriteInt32(MissileAid);
        if (Bit(ObjectField.ObjFWeaponVisualEffectAid))
            writer.WriteInt32(VisualEffectAid);
        if (Bit(ObjectField.ObjFWeaponCritHitChart))
            writer.WriteInt32(CritHitChart);
        if (Bit(ObjectField.ObjFWeaponMagicCritHitChance))
            writer.WriteInt32(MagicCritHitChance);
        if (Bit(ObjectField.ObjFWeaponMagicCritHitEffect))
            writer.WriteInt32(MagicCritHitEffect);
        if (Bit(ObjectField.ObjFWeaponCritMissChart))
            writer.WriteInt32(CritMissChart);
        if (Bit(ObjectField.ObjFWeaponMagicCritMissChance))
            writer.WriteInt32(MagicCritMissChance);
        if (Bit(ObjectField.ObjFWeaponMagicCritMissEffect))
            writer.WriteInt32(MagicCritMissEffect);
        if (Bit(ObjectField.ObjFWeaponPadI1))
            writer.WriteInt32(_weaponPadI1Reserved);
        if (Bit(ObjectField.ObjFWeaponPadI2))
            writer.WriteInt32(_weaponPadI2Reserved);
        if (Bit(ObjectField.ObjFWeaponPadIas1))
            writer.WriteInt32(_weaponPadIas1Reserved);
        if (Bit(ObjectField.ObjFWeaponPadI64As1))
            writer.WriteInt64(_weaponPadI64As1Reserved);
    }
}
