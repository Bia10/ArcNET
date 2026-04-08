using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWeapon : ObjectItem
{
    public int WeaponFlags { get; internal set; }
    public int WeaponPaperDollAid { get; internal set; }
    public int WeaponBonusToHit { get; internal set; }
    public int WeaponMagicHitAdj { get; internal set; }
    public int[] WeaponDamageLower { get; internal set; } = [];
    public int[] WeaponDamageUpper { get; internal set; } = [];
    public int[] WeaponMagicDamageAdj { get; internal set; } = [];
    public int WeaponSpeedFactor { get; internal set; }
    public int WeaponMagicSpeedAdj { get; internal set; }
    public int WeaponRange { get; internal set; }
    public int WeaponMagicRangeAdj { get; internal set; }
    public int WeaponMinStrength { get; internal set; }
    public int WeaponMagicMinStrengthAdj { get; internal set; }
    public int WeaponAmmoType { get; internal set; }
    public int WeaponAmmoConsumption { get; internal set; }
    public int WeaponMissileAid { get; internal set; }
    public int WeaponVisualEffectAid { get; internal set; }
    public int WeaponCritHitChart { get; internal set; }
    public int WeaponMagicCritHitChance { get; internal set; }
    public int WeaponMagicCritHitEffect { get; internal set; }
    public int WeaponCritMissChart { get; internal set; }
    public int WeaponMagicCritMissChance { get; internal set; }
    public int WeaponMagicCritMissEffect { get; internal set; }
    public int WeaponPadI1 { get; internal set; }
    public int WeaponPadI2 { get; internal set; }
    public int WeaponPadIas1 { get; internal set; }
    public long WeaponPadI64As1 { get; internal set; }

    internal static ObjectWeapon Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectWeapon();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFWeaponFlags))
            obj.WeaponFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPaperDollAid))
            obj.WeaponPaperDollAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponBonusToHit))
            obj.WeaponBonusToHit = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicHitAdj))
            obj.WeaponMagicHitAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponDamageLowerIdx))
            obj.WeaponDamageLower = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFWeaponDamageUpperIdx))
            obj.WeaponDamageUpper = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFWeaponMagicDamageAdjIdx))
            obj.WeaponMagicDamageAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFWeaponSpeedFactor))
            obj.WeaponSpeedFactor = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicSpeedAdj))
            obj.WeaponMagicSpeedAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponRange))
            obj.WeaponRange = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicRangeAdj))
            obj.WeaponMagicRangeAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMinStrength))
            obj.WeaponMinStrength = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicMinStrengthAdj))
            obj.WeaponMagicMinStrengthAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponAmmoType))
            obj.WeaponAmmoType = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponAmmoConsumption))
            obj.WeaponAmmoConsumption = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMissileAid))
            obj.WeaponMissileAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponVisualEffectAid))
            obj.WeaponVisualEffectAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponCritHitChart))
            obj.WeaponCritHitChart = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritHitChance))
            obj.WeaponMagicCritHitChance = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritHitEffect))
            obj.WeaponMagicCritHitEffect = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponCritMissChart))
            obj.WeaponCritMissChart = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritMissChance))
            obj.WeaponMagicCritMissChance = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponMagicCritMissEffect))
            obj.WeaponMagicCritMissEffect = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadI1))
            obj.WeaponPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadI2))
            obj.WeaponPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadIas1))
            obj.WeaponPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWeaponPadI64As1))
            obj.WeaponPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFWeaponFlags))
            writer.WriteInt32(WeaponFlags);
        if (Bit(ObjectField.ObjFWeaponPaperDollAid))
            writer.WriteInt32(WeaponPaperDollAid);
        if (Bit(ObjectField.ObjFWeaponBonusToHit))
            writer.WriteInt32(WeaponBonusToHit);
        if (Bit(ObjectField.ObjFWeaponMagicHitAdj))
            writer.WriteInt32(WeaponMagicHitAdj);
        if (Bit(ObjectField.ObjFWeaponDamageLowerIdx))
            WriteIndexedInts(ref writer, WeaponDamageLower);
        if (Bit(ObjectField.ObjFWeaponDamageUpperIdx))
            WriteIndexedInts(ref writer, WeaponDamageUpper);
        if (Bit(ObjectField.ObjFWeaponMagicDamageAdjIdx))
            WriteIndexedInts(ref writer, WeaponMagicDamageAdj);
        if (Bit(ObjectField.ObjFWeaponSpeedFactor))
            writer.WriteInt32(WeaponSpeedFactor);
        if (Bit(ObjectField.ObjFWeaponMagicSpeedAdj))
            writer.WriteInt32(WeaponMagicSpeedAdj);
        if (Bit(ObjectField.ObjFWeaponRange))
            writer.WriteInt32(WeaponRange);
        if (Bit(ObjectField.ObjFWeaponMagicRangeAdj))
            writer.WriteInt32(WeaponMagicRangeAdj);
        if (Bit(ObjectField.ObjFWeaponMinStrength))
            writer.WriteInt32(WeaponMinStrength);
        if (Bit(ObjectField.ObjFWeaponMagicMinStrengthAdj))
            writer.WriteInt32(WeaponMagicMinStrengthAdj);
        if (Bit(ObjectField.ObjFWeaponAmmoType))
            writer.WriteInt32(WeaponAmmoType);
        if (Bit(ObjectField.ObjFWeaponAmmoConsumption))
            writer.WriteInt32(WeaponAmmoConsumption);
        if (Bit(ObjectField.ObjFWeaponMissileAid))
            writer.WriteInt32(WeaponMissileAid);
        if (Bit(ObjectField.ObjFWeaponVisualEffectAid))
            writer.WriteInt32(WeaponVisualEffectAid);
        if (Bit(ObjectField.ObjFWeaponCritHitChart))
            writer.WriteInt32(WeaponCritHitChart);
        if (Bit(ObjectField.ObjFWeaponMagicCritHitChance))
            writer.WriteInt32(WeaponMagicCritHitChance);
        if (Bit(ObjectField.ObjFWeaponMagicCritHitEffect))
            writer.WriteInt32(WeaponMagicCritHitEffect);
        if (Bit(ObjectField.ObjFWeaponCritMissChart))
            writer.WriteInt32(WeaponCritMissChart);
        if (Bit(ObjectField.ObjFWeaponMagicCritMissChance))
            writer.WriteInt32(WeaponMagicCritMissChance);
        if (Bit(ObjectField.ObjFWeaponMagicCritMissEffect))
            writer.WriteInt32(WeaponMagicCritMissEffect);
        if (Bit(ObjectField.ObjFWeaponPadI1))
            writer.WriteInt32(WeaponPadI1);
        if (Bit(ObjectField.ObjFWeaponPadI2))
            writer.WriteInt32(WeaponPadI2);
        if (Bit(ObjectField.ObjFWeaponPadIas1))
            writer.WriteInt32(WeaponPadIas1);
        if (Bit(ObjectField.ObjFWeaponPadI64As1))
            writer.WriteInt64(WeaponPadI64As1);
    }
}
