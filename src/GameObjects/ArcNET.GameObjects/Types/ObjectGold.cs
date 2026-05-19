using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectGold : ObjectItem
{
    private int _goldPadI1Reserved;
    private int _goldPadI2Reserved;
    private int _goldPadIas1Reserved;
    private long _goldPadI64As1Reserved;

    public int GoldFlags { get; internal set; }
    public int Quantity { get; internal set; }

    internal static ObjectGold Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectGold();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.GoldFlags))
            obj.GoldFlags = reader.ReadInt32();
        if (Bit(ObjectField.GoldQuantity))
            obj.Quantity = reader.ReadInt32();
        if (Bit(ObjectField.GoldPadI1))
            obj._goldPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.GoldPadI2))
            obj._goldPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.GoldPadIas1))
            obj._goldPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.GoldPadI64As1))
            obj._goldPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.GoldFlags))
            writer.WriteInt32(GoldFlags);
        if (Bit(ObjectField.GoldQuantity))
            writer.WriteInt32(Quantity);
        if (Bit(ObjectField.GoldPadI1))
            writer.WriteInt32(_goldPadI1Reserved);
        if (Bit(ObjectField.GoldPadI2))
            writer.WriteInt32(_goldPadI2Reserved);
        if (Bit(ObjectField.GoldPadIas1))
            writer.WriteInt32(_goldPadIas1Reserved);
        if (Bit(ObjectField.GoldPadI64As1))
            writer.WriteInt64(_goldPadI64As1Reserved);
    }
}
