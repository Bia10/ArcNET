using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectScroll : ObjectItem
{
    public int ScrollFlags { get; internal set; }
    public int ScrollPadI1 { get; internal set; }
    public int ScrollPadI2 { get; internal set; }
    public int ScrollPadIas1 { get; internal set; }
    public long ScrollPadI64As1 { get; internal set; }

    internal static ObjectScroll Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectScroll();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFScrollFlags))
            obj.ScrollFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadI1))
            obj.ScrollPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadI2))
            obj.ScrollPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadIas1))
            obj.ScrollPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadI64As1))
            obj.ScrollPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFScrollFlags))
            writer.WriteInt32(ScrollFlags);
        if (Bit(ObjectField.ObjFScrollPadI1))
            writer.WriteInt32(ScrollPadI1);
        if (Bit(ObjectField.ObjFScrollPadI2))
            writer.WriteInt32(ScrollPadI2);
        if (Bit(ObjectField.ObjFScrollPadIas1))
            writer.WriteInt32(ScrollPadIas1);
        if (Bit(ObjectField.ObjFScrollPadI64As1))
            writer.WriteInt64(ScrollPadI64As1);
    }
}
