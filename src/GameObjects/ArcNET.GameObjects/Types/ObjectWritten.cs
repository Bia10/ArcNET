using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWritten : ObjectItem
{
    private int _writtenPadI1Reserved;
    private int _writtenPadI2Reserved;
    private int _writtenPadIas1Reserved;
    private long _writtenPadI64As1Reserved;

    public int WrittenFlags { get; internal set; }
    public int Subtype { get; internal set; }
    public int TextStartLine { get; internal set; }
    public int TextEndLine { get; internal set; }

    internal static ObjectWritten Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectWritten();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.WrittenFlags))
            obj.WrittenFlags = reader.ReadInt32();
        if (Bit(ObjectField.WrittenSubtype))
            obj.Subtype = reader.ReadInt32();
        if (Bit(ObjectField.WrittenTextStartLine))
            obj.TextStartLine = reader.ReadInt32();
        if (Bit(ObjectField.WrittenTextEndLine))
            obj.TextEndLine = reader.ReadInt32();
        if (Bit(ObjectField.WrittenPadI1))
            obj._writtenPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.WrittenPadI2))
            obj._writtenPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.WrittenPadIas1))
            obj._writtenPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.WrittenPadI64As1))
            obj._writtenPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.WrittenFlags))
            writer.WriteInt32(WrittenFlags);
        if (Bit(ObjectField.WrittenSubtype))
            writer.WriteInt32(Subtype);
        if (Bit(ObjectField.WrittenTextStartLine))
            writer.WriteInt32(TextStartLine);
        if (Bit(ObjectField.WrittenTextEndLine))
            writer.WriteInt32(TextEndLine);
        if (Bit(ObjectField.WrittenPadI1))
            writer.WriteInt32(_writtenPadI1Reserved);
        if (Bit(ObjectField.WrittenPadI2))
            writer.WriteInt32(_writtenPadI2Reserved);
        if (Bit(ObjectField.WrittenPadIas1))
            writer.WriteInt32(_writtenPadIas1Reserved);
        if (Bit(ObjectField.WrittenPadI64As1))
            writer.WriteInt64(_writtenPadI64As1Reserved);
    }
}
