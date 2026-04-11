using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectGeneric : ObjectItem
{
    public int GenericFlags { get; internal set; }
    public int GenericUsageBonus { get; internal set; }
    public int GenericUsageCountRemaining { get; internal set; }
    public int GenericPadIas1 { get; internal set; }
    public long GenericPadI64As1 { get; internal set; }

    internal static ObjectGeneric Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectGeneric();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFGenericFlags))
            obj.GenericFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericUsageBonus))
            obj.GenericUsageBonus = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericUsageCountRemaining))
            obj.GenericUsageCountRemaining = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericPadIas1))
            obj.GenericPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFGenericPadI64As1))
            obj.GenericPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFGenericFlags))
            writer.WriteInt32(GenericFlags);
        if (Bit(ObjectField.ObjFGenericUsageBonus))
            writer.WriteInt32(GenericUsageBonus);
        if (Bit(ObjectField.ObjFGenericUsageCountRemaining))
            writer.WriteInt32(GenericUsageCountRemaining);
        if (Bit(ObjectField.ObjFGenericPadIas1))
            writer.WriteInt32(GenericPadIas1);
        if (Bit(ObjectField.ObjFGenericPadI64As1))
            writer.WriteInt64(GenericPadI64As1);
    }
}
