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
        if (Bit(ObjectField.ObjFGenericFlags))
            obj.GenericFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericUsageBonus))
            obj.UsageBonus = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericUsageCountRemaining))
            obj.UsageCountRemaining = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericPadIas1))
            obj._genericPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericPadI64As1))
            obj._genericPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFGenericFlags))
            writer.WriteInt32(GenericFlags);
        if (Bit(ObjectField.ObjFGenericUsageBonus))
            writer.WriteInt32(UsageBonus);
        if (Bit(ObjectField.ObjFGenericUsageCountRemaining))
            writer.WriteInt32(UsageCountRemaining);
        if (Bit(ObjectField.ObjFGenericPadIas1))
            writer.WriteInt32(_genericPadIas1Reserved);
        if (Bit(ObjectField.ObjFGenericPadI64As1))
            writer.WriteInt64(_genericPadI64As1Reserved);
    }
}
