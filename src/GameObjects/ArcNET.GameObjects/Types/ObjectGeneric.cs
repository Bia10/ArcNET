using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectGeneric : ObjectItem
{
    private int _genericPadIas1Reserved;
    private long _genericPadI64As1Reserved;

    public int GenericFlags { get; internal set; }
    public int UsageBonus { get; internal set; }
    public int UsageCountRemaining { get; internal set; }

    internal static ObjectGeneric Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectGeneric();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.GenericFlags))
            obj.GenericFlags = reader.ReadInt32();
        if (Bit(ObjectField.GenericUsageBonus))
            obj.UsageBonus = reader.ReadInt32();
        if (Bit(ObjectField.GenericUsageCountRemaining))
            obj.UsageCountRemaining = reader.ReadInt32();
        if (Bit(ObjectField.GenericPadIas1))
            obj._genericPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.GenericPadI64As1))
            obj._genericPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.GenericFlags))
            writer.WriteInt32(GenericFlags);
        if (Bit(ObjectField.GenericUsageBonus))
            writer.WriteInt32(UsageBonus);
        if (Bit(ObjectField.GenericUsageCountRemaining))
            writer.WriteInt32(UsageCountRemaining);
        if (Bit(ObjectField.GenericPadIas1))
            writer.WriteInt32(_genericPadIas1Reserved);
        if (Bit(ObjectField.GenericPadI64As1))
            writer.WriteInt64(_genericPadI64As1Reserved);
    }
}
