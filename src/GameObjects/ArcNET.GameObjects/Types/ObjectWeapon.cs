using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWeapon : ObjectItem
{
    private int _weaponPadI1Reserved;
    private int _weaponPadI2Reserved;
    private int _weaponPadIas1Reserved;
    private long _weaponPadI64As1Reserved;

    public WeaponFlags WeaponFlags { get; internal set; }
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
        if (Bit(ObjectField.WeaponFlags))
            obj.WeaponFlags = unchecked((WeaponFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.WeaponPaperDollAid))
            obj.PaperDollAid = reader.ReadInt32();
        if (Bit(ObjectField.WeaponBonusToHit))
            obj.BonusToHit = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicHitAdj))
            obj.MagicHitAdj = reader.ReadInt32();
        if (Bit(ObjectField.WeaponDamageLowerIdx))
            obj.DamageLower = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.WeaponDamageUpperIdx))
            obj.DamageUpper = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.WeaponMagicDamageAdjIdx))
            obj.MagicDamageAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.WeaponSpeedFactor))
            obj.SpeedFactor = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicSpeedAdj))
            obj.MagicSpeedAdj = reader.ReadInt32();
        if (Bit(ObjectField.WeaponRange))
            obj.Range = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicRangeAdj))
            obj.MagicRangeAdj = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMinStrength))
            obj.MinStrength = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicMinStrengthAdj))
            obj.MagicMinStrengthAdj = reader.ReadInt32();
        if (Bit(ObjectField.WeaponAmmoType))
            obj.AmmoType = reader.ReadInt32();
        if (Bit(ObjectField.WeaponAmmoConsumption))
            obj.AmmoConsumption = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMissileAid))
            obj.MissileAid = reader.ReadInt32();
        if (Bit(ObjectField.WeaponVisualEffectAid))
            obj.VisualEffectAid = reader.ReadInt32();
        if (Bit(ObjectField.WeaponCritHitChart))
            obj.CritHitChart = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicCritHitChance))
            obj.MagicCritHitChance = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicCritHitEffect))
            obj.MagicCritHitEffect = reader.ReadInt32();
        if (Bit(ObjectField.WeaponCritMissChart))
            obj.CritMissChart = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicCritMissChance))
            obj.MagicCritMissChance = reader.ReadInt32();
        if (Bit(ObjectField.WeaponMagicCritMissEffect))
            obj.MagicCritMissEffect = reader.ReadInt32();
        if (Bit(ObjectField.WeaponPadI1))
            obj._weaponPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.WeaponPadI2))
            obj._weaponPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.WeaponPadIas1))
            obj._weaponPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.WeaponPadI64As1))
            obj._weaponPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.WeaponFlags))
            writer.WriteInt32(unchecked((int)WeaponFlags));
        if (Bit(ObjectField.WeaponPaperDollAid))
            writer.WriteInt32(PaperDollAid);
        if (Bit(ObjectField.WeaponBonusToHit))
            writer.WriteInt32(BonusToHit);
        if (Bit(ObjectField.WeaponMagicHitAdj))
            writer.WriteInt32(MagicHitAdj);
        if (Bit(ObjectField.WeaponDamageLowerIdx))
            WriteIndexedInts(ref writer, DamageLower);
        if (Bit(ObjectField.WeaponDamageUpperIdx))
            WriteIndexedInts(ref writer, DamageUpper);
        if (Bit(ObjectField.WeaponMagicDamageAdjIdx))
            WriteIndexedInts(ref writer, MagicDamageAdj);
        if (Bit(ObjectField.WeaponSpeedFactor))
            writer.WriteInt32(SpeedFactor);
        if (Bit(ObjectField.WeaponMagicSpeedAdj))
            writer.WriteInt32(MagicSpeedAdj);
        if (Bit(ObjectField.WeaponRange))
            writer.WriteInt32(Range);
        if (Bit(ObjectField.WeaponMagicRangeAdj))
            writer.WriteInt32(MagicRangeAdj);
        if (Bit(ObjectField.WeaponMinStrength))
            writer.WriteInt32(MinStrength);
        if (Bit(ObjectField.WeaponMagicMinStrengthAdj))
            writer.WriteInt32(MagicMinStrengthAdj);
        if (Bit(ObjectField.WeaponAmmoType))
            writer.WriteInt32(AmmoType);
        if (Bit(ObjectField.WeaponAmmoConsumption))
            writer.WriteInt32(AmmoConsumption);
        if (Bit(ObjectField.WeaponMissileAid))
            writer.WriteInt32(MissileAid);
        if (Bit(ObjectField.WeaponVisualEffectAid))
            writer.WriteInt32(VisualEffectAid);
        if (Bit(ObjectField.WeaponCritHitChart))
            writer.WriteInt32(CritHitChart);
        if (Bit(ObjectField.WeaponMagicCritHitChance))
            writer.WriteInt32(MagicCritHitChance);
        if (Bit(ObjectField.WeaponMagicCritHitEffect))
            writer.WriteInt32(MagicCritHitEffect);
        if (Bit(ObjectField.WeaponCritMissChart))
            writer.WriteInt32(CritMissChart);
        if (Bit(ObjectField.WeaponMagicCritMissChance))
            writer.WriteInt32(MagicCritMissChance);
        if (Bit(ObjectField.WeaponMagicCritMissEffect))
            writer.WriteInt32(MagicCritMissEffect);
        if (Bit(ObjectField.WeaponPadI1))
            writer.WriteInt32(_weaponPadI1Reserved);
        if (Bit(ObjectField.WeaponPadI2))
            writer.WriteInt32(_weaponPadI2Reserved);
        if (Bit(ObjectField.WeaponPadIas1))
            writer.WriteInt32(_weaponPadIas1Reserved);
        if (Bit(ObjectField.WeaponPadI64As1))
            writer.WriteInt64(_weaponPadI64As1Reserved);
    }
}
