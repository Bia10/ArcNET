using System.Collections;
using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectArmor : ObjectItem
{
    public int ArmorFlags { get; set; }
    public int ArmorPaperDollAid { get; set; }
    public int ArmorAcAdj { get; set; }
    public int ArmorMagicAcAdj { get; set; }
    public int[] ArmorResistanceAdj { get; set; } = [];
    public int[] ArmorMagicResistanceAdj { get; set; } = [];
    public int ArmorSilentMoveAdj { get; set; }
    public int ArmorMagicSilentMoveAdj { get; set; }
    public int ArmorUnarmedBonusDamage { get; set; }
    public int ArmorPadI2 { get; set; }
    public int ArmorPadIas1 { get; set; }
    public long ArmorPadI64As1 { get; set; }

    internal static ObjectArmor Read(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        var obj = new ObjectArmor();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFArmorFlags))
            obj.ArmorFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPaperDollAid))
            obj.ArmorPaperDollAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorAcAdj))
            obj.ArmorAcAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorMagicAcAdj))
            obj.ArmorMagicAcAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorResistanceAdjIdx))
            obj.ArmorResistanceAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFArmorMagicResistanceAdjIdx))
            obj.ArmorMagicResistanceAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFArmorSilentMoveAdj))
            obj.ArmorSilentMoveAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorMagicSilentMoveAdj))
            obj.ArmorMagicSilentMoveAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorUnarmedBonusDamage))
            obj.ArmorUnarmedBonusDamage = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPadI2))
            obj.ArmorPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPadIas1))
            obj.ArmorPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPadI64As1))
            obj.ArmorPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFArmorFlags))
            writer.WriteInt32(ArmorFlags);
        if (Bit(ObjectField.ObjFArmorPaperDollAid))
            writer.WriteInt32(ArmorPaperDollAid);
        if (Bit(ObjectField.ObjFArmorAcAdj))
            writer.WriteInt32(ArmorAcAdj);
        if (Bit(ObjectField.ObjFArmorMagicAcAdj))
            writer.WriteInt32(ArmorMagicAcAdj);
        if (Bit(ObjectField.ObjFArmorResistanceAdjIdx))
            WriteIndexedInts(ref writer, ArmorResistanceAdj);
        if (Bit(ObjectField.ObjFArmorMagicResistanceAdjIdx))
            WriteIndexedInts(ref writer, ArmorMagicResistanceAdj);
        if (Bit(ObjectField.ObjFArmorSilentMoveAdj))
            writer.WriteInt32(ArmorSilentMoveAdj);
        if (Bit(ObjectField.ObjFArmorMagicSilentMoveAdj))
            writer.WriteInt32(ArmorMagicSilentMoveAdj);
        if (Bit(ObjectField.ObjFArmorUnarmedBonusDamage))
            writer.WriteInt32(ArmorUnarmedBonusDamage);
        if (Bit(ObjectField.ObjFArmorPadI2))
            writer.WriteInt32(ArmorPadI2);
        if (Bit(ObjectField.ObjFArmorPadIas1))
            writer.WriteInt32(ArmorPadIas1);
        if (Bit(ObjectField.ObjFArmorPadI64As1))
            writer.WriteInt64(ArmorPadI64As1);
    }
}
