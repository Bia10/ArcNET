using System.Collections;
using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectScroll : ObjectItem
{
    public int ScrollFlags { get; set; }
    public int ScrollPadI1 { get; set; }
    public int ScrollPadI2 { get; set; }
    public int ScrollPadIas1 { get; set; }
    public long ScrollPadI64As1 { get; set; }

    internal static ObjectScroll Read(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        var obj = new ObjectScroll();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
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

    internal void Write(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
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
