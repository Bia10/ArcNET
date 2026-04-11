using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectGold : ObjectItem
{
    public int GoldFlags { get; internal set; }
    public int GoldQuantity { get; internal set; }
    public int GoldPadI1 { get; internal set; }
    public int GoldPadI2 { get; internal set; }
    public int GoldPadIas1 { get; internal set; }
    public long GoldPadI64As1 { get; internal set; }

    internal static ObjectGold Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectGold();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFGoldFlags))
            obj.GoldFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGoldQuantity))
            obj.GoldQuantity = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGoldPadI1))
            obj.GoldPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGoldPadI2))
            obj.GoldPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGoldPadIas1))
            obj.GoldPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGoldPadI64As1))
            obj.GoldPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFGoldFlags))
            writer.WriteInt32(GoldFlags);
        if (Bit(ObjectField.ObjFGoldQuantity))
            writer.WriteInt32(GoldQuantity);
        if (Bit(ObjectField.ObjFGoldPadI1))
            writer.WriteInt32(GoldPadI1);
        if (Bit(ObjectField.ObjFGoldPadI2))
            writer.WriteInt32(GoldPadI2);
        if (Bit(ObjectField.ObjFGoldPadIas1))
            writer.WriteInt32(GoldPadIas1);
        if (Bit(ObjectField.ObjFGoldPadI64As1))
            writer.WriteInt64(GoldPadI64As1);
    }
}
