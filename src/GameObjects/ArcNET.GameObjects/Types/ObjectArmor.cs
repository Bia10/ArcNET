using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectArmor : ObjectItem
{
    private int _armorPadI2Reserved;
    private int _armorPadIas1Reserved;
    private long _armorPadI64As1Reserved;

    public ObjFArmorFlags ArmorFlags { get; internal set; }
    public int PaperDollAid { get; internal set; }
    public int AcAdj { get; internal set; }
    public int MagicAcAdj { get; internal set; }
    public int[] ResistanceAdj { get; internal set; } = [];
    public int[] MagicResistanceAdj { get; internal set; } = [];
    public int SilentMoveAdj { get; internal set; }
    public int MagicSilentMoveAdj { get; internal set; }
    public int UnarmedBonusDamage { get; internal set; }

    internal static ObjectArmor Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectArmor();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFArmorFlags))
            obj.ArmorFlags = unchecked((ObjFArmorFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFArmorPaperDollAid))
            obj.PaperDollAid = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorAcAdj))
            obj.AcAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorMagicAcAdj))
            obj.MagicAcAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorResistanceAdjIdx))
            obj.ResistanceAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFArmorMagicResistanceAdjIdx))
            obj.MagicResistanceAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFArmorSilentMoveAdj))
            obj.SilentMoveAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorMagicSilentMoveAdj))
            obj.MagicSilentMoveAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorUnarmedBonusDamage))
            obj.UnarmedBonusDamage = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPadI2))
            obj._armorPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPadIas1))
            obj._armorPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFArmorPadI64As1))
            obj._armorPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFArmorFlags))
            writer.WriteInt32(unchecked((int)ArmorFlags));
        if (Bit(ObjectField.ObjFArmorPaperDollAid))
            writer.WriteInt32(PaperDollAid);
        if (Bit(ObjectField.ObjFArmorAcAdj))
            writer.WriteInt32(AcAdj);
        if (Bit(ObjectField.ObjFArmorMagicAcAdj))
            writer.WriteInt32(MagicAcAdj);
        if (Bit(ObjectField.ObjFArmorResistanceAdjIdx))
            WriteIndexedInts(ref writer, ResistanceAdj);
        if (Bit(ObjectField.ObjFArmorMagicResistanceAdjIdx))
            WriteIndexedInts(ref writer, MagicResistanceAdj);
        if (Bit(ObjectField.ObjFArmorSilentMoveAdj))
            writer.WriteInt32(SilentMoveAdj);
        if (Bit(ObjectField.ObjFArmorMagicSilentMoveAdj))
            writer.WriteInt32(MagicSilentMoveAdj);
        if (Bit(ObjectField.ObjFArmorUnarmedBonusDamage))
            writer.WriteInt32(UnarmedBonusDamage);
        if (Bit(ObjectField.ObjFArmorPadI2))
            writer.WriteInt32(_armorPadI2Reserved);
        if (Bit(ObjectField.ObjFArmorPadIas1))
            writer.WriteInt32(_armorPadIas1Reserved);
        if (Bit(ObjectField.ObjFArmorPadI64As1))
            writer.WriteInt64(_armorPadI64As1Reserved);
    }
}
