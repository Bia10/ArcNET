using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectFood : ObjectItem
{
    private int _foodPadI1Reserved;
    private int _foodPadI2Reserved;
    private int _foodPadIas1Reserved;
    private long _foodPadI64As1Reserved;

    public int FoodFlags { get; internal set; }

    internal static ObjectFood Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectFood();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.FoodFlags))
            obj.FoodFlags = reader.ReadInt32();
        if (Bit(ObjectField.FoodPadI1))
            obj._foodPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.FoodPadI2))
            obj._foodPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.FoodPadIas1))
            obj._foodPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.FoodPadI64As1))
            obj._foodPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.FoodFlags))
            writer.WriteInt32(FoodFlags);
        if (Bit(ObjectField.FoodPadI1))
            writer.WriteInt32(_foodPadI1Reserved);
        if (Bit(ObjectField.FoodPadI2))
            writer.WriteInt32(_foodPadI2Reserved);
        if (Bit(ObjectField.FoodPadIas1))
            writer.WriteInt32(_foodPadIas1Reserved);
        if (Bit(ObjectField.FoodPadI64As1))
            writer.WriteInt64(_foodPadI64As1Reserved);
    }
}
