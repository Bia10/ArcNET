using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectAmmo : ObjectItem
{
    private int _ammoPadI1Reserved;
    private int _ammoPadI2Reserved;
    private int _ammoPadIas1Reserved;
    private long _ammoPadI64As1Reserved;

    public int AmmoFlags { get; internal set; }
    public int Quantity { get; internal set; }
    public int Type { get; internal set; }

    internal static ObjectAmmo Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectAmmo();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.AmmoFlags))
            obj.AmmoFlags = reader.ReadInt32();
        if (Bit(ObjectField.AmmoQuantity))
            obj.Quantity = reader.ReadInt32();
        if (Bit(ObjectField.AmmoType))
            obj.Type = reader.ReadInt32();
        if (Bit(ObjectField.AmmoPadI1))
            obj._ammoPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.AmmoPadI2))
            obj._ammoPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.AmmoPadIas1))
            obj._ammoPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.AmmoPadI64As1))
            obj._ammoPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.AmmoFlags))
            writer.WriteInt32(AmmoFlags);
        if (Bit(ObjectField.AmmoQuantity))
            writer.WriteInt32(Quantity);
        if (Bit(ObjectField.AmmoType))
            writer.WriteInt32(Type);
        if (Bit(ObjectField.AmmoPadI1))
            writer.WriteInt32(_ammoPadI1Reserved);
        if (Bit(ObjectField.AmmoPadI2))
            writer.WriteInt32(_ammoPadI2Reserved);
        if (Bit(ObjectField.AmmoPadIas1))
            writer.WriteInt32(_ammoPadIas1Reserved);
        if (Bit(ObjectField.AmmoPadI64As1))
            writer.WriteInt64(_ammoPadI64As1Reserved);
    }
}
