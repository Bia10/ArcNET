using System.Collections;
using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWritten : ObjectItem
{
    public int WrittenFlags { get; set; }
    public int WrittenSubtype { get; set; }
    public int WrittenTextStartLine { get; set; }
    public int WrittenTextEndLine { get; set; }
    public int WrittenPadI1 { get; set; }
    public int WrittenPadI2 { get; set; }
    public int WrittenPadIas1 { get; set; }
    public long WrittenPadI64As1 { get; set; }

    internal static ObjectWritten Read(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        var obj = new ObjectWritten();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFWrittenFlags))
            obj.WrittenFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenSubtype))
            obj.WrittenSubtype = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenTextStartLine))
            obj.WrittenTextStartLine = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenTextEndLine))
            obj.WrittenTextEndLine = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenPadI1))
            obj.WrittenPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenPadI2))
            obj.WrittenPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenPadIas1))
            obj.WrittenPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWrittenPadI64As1))
            obj.WrittenPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFWrittenFlags))
            writer.WriteInt32(WrittenFlags);
        if (Bit(ObjectField.ObjFWrittenSubtype))
            writer.WriteInt32(WrittenSubtype);
        if (Bit(ObjectField.ObjFWrittenTextStartLine))
            writer.WriteInt32(WrittenTextStartLine);
        if (Bit(ObjectField.ObjFWrittenTextEndLine))
            writer.WriteInt32(WrittenTextEndLine);
        if (Bit(ObjectField.ObjFWrittenPadI1))
            writer.WriteInt32(WrittenPadI1);
        if (Bit(ObjectField.ObjFWrittenPadI2))
            writer.WriteInt32(WrittenPadI2);
        if (Bit(ObjectField.ObjFWrittenPadIas1))
            writer.WriteInt32(WrittenPadIas1);
        if (Bit(ObjectField.ObjFWrittenPadI64As1))
            writer.WriteInt64(WrittenPadI64As1);
    }
}
