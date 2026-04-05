using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectFood : ObjectItem
{
    public int FoodFlags { get; set; }
    public int FoodPadI1 { get; set; }
    public int FoodPadI2 { get; set; }
    public int FoodPadIas1 { get; set; }
    public long FoodPadI64As1 { get; set; }

    internal static ObjectFood Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectFood();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFFoodFlags))
            obj.FoodFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFFoodPadI1))
            obj.FoodPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFFoodPadI2))
            obj.FoodPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFFoodPadIas1))
            obj.FoodPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFFoodPadI64As1))
            obj.FoodPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFFoodFlags))
            writer.WriteInt32(FoodFlags);
        if (Bit(ObjectField.ObjFFoodPadI1))
            writer.WriteInt32(FoodPadI1);
        if (Bit(ObjectField.ObjFFoodPadI2))
            writer.WriteInt32(FoodPadI2);
        if (Bit(ObjectField.ObjFFoodPadIas1))
            writer.WriteInt32(FoodPadIas1);
        if (Bit(ObjectField.ObjFFoodPadI64As1))
            writer.WriteInt64(FoodPadI64As1);
    }
}
