using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectArmor : ObjectItem
{
    private int _armorPadI2Reserved;
    private int _armorPadIas1Reserved;
    private long _armorPadI64As1Reserved;

    public ArmorFlags ArmorFlags { get; internal set; }
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
        if (Bit(ObjectField.ArmorFlags))
            obj.ArmorFlags = unchecked((ArmorFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ArmorPaperDollAid))
            obj.PaperDollAid = reader.ReadInt32();
        if (Bit(ObjectField.ArmorAcAdj))
            obj.AcAdj = reader.ReadInt32();
        if (Bit(ObjectField.ArmorMagicAcAdj))
            obj.MagicAcAdj = reader.ReadInt32();
        if (Bit(ObjectField.ArmorResistanceAdjIdx))
            obj.ResistanceAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ArmorMagicResistanceAdjIdx))
            obj.MagicResistanceAdj = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ArmorSilentMoveAdj))
            obj.SilentMoveAdj = reader.ReadInt32();
        if (Bit(ObjectField.ArmorMagicSilentMoveAdj))
            obj.MagicSilentMoveAdj = reader.ReadInt32();
        if (Bit(ObjectField.ArmorUnarmedBonusDamage))
            obj.UnarmedBonusDamage = reader.ReadInt32();
        if (Bit(ObjectField.ArmorPadI2))
            obj._armorPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ArmorPadIas1))
            obj._armorPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ArmorPadI64As1))
            obj._armorPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ArmorFlags))
            writer.WriteInt32(unchecked((int)ArmorFlags));
        if (Bit(ObjectField.ArmorPaperDollAid))
            writer.WriteInt32(PaperDollAid);
        if (Bit(ObjectField.ArmorAcAdj))
            writer.WriteInt32(AcAdj);
        if (Bit(ObjectField.ArmorMagicAcAdj))
            writer.WriteInt32(MagicAcAdj);
        if (Bit(ObjectField.ArmorResistanceAdjIdx))
            WriteIndexedInts(ref writer, ResistanceAdj);
        if (Bit(ObjectField.ArmorMagicResistanceAdjIdx))
            WriteIndexedInts(ref writer, MagicResistanceAdj);
        if (Bit(ObjectField.ArmorSilentMoveAdj))
            writer.WriteInt32(SilentMoveAdj);
        if (Bit(ObjectField.ArmorMagicSilentMoveAdj))
            writer.WriteInt32(MagicSilentMoveAdj);
        if (Bit(ObjectField.ArmorUnarmedBonusDamage))
            writer.WriteInt32(UnarmedBonusDamage);
        if (Bit(ObjectField.ArmorPadI2))
            writer.WriteInt32(_armorPadI2Reserved);
        if (Bit(ObjectField.ArmorPadIas1))
            writer.WriteInt32(_armorPadIas1Reserved);
        if (Bit(ObjectField.ArmorPadI64As1))
            writer.WriteInt64(_armorPadI64As1Reserved);
    }
}
