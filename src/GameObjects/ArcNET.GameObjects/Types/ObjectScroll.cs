using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectScroll : ObjectItem
{
    private int _scrollPadI1Reserved;
    private int _scrollPadI2Reserved;
    private int _scrollPadIas1Reserved;
    private long _scrollPadI64As1Reserved;

    public int ScrollFlags { get; internal set; }

    internal static ObjectScroll Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectScroll();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFScrollFlags))
            obj.ScrollFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadI1))
            obj._scrollPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadI2))
            obj._scrollPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadIas1))
            obj._scrollPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFScrollPadI64As1))
            obj._scrollPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFScrollFlags))
            writer.WriteInt32(ScrollFlags);
        if (Bit(ObjectField.ObjFScrollPadI1))
            writer.WriteInt32(_scrollPadI1Reserved);
        if (Bit(ObjectField.ObjFScrollPadI2))
            writer.WriteInt32(_scrollPadI2Reserved);
        if (Bit(ObjectField.ObjFScrollPadIas1))
            writer.WriteInt32(_scrollPadIas1Reserved);
        if (Bit(ObjectField.ObjFScrollPadI64As1))
            writer.WriteInt64(_scrollPadI64As1Reserved);
    }
}
