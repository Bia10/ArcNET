using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectKeyRing : ObjectItem
{
    private int _keyRingPadI1Reserved;
    private int _keyRingPadI2Reserved;
    private int _keyRingPadIas1Reserved;
    private long _keyRingPadI64As1Reserved;

    public int KeyRingFlags { get; internal set; }
    public int[] List { get; internal set; } = [];

    internal static ObjectKeyRing Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectKeyRing();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFKeyRingFlags))
            obj.KeyRingFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingListIdx))
            obj.List = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFKeyRingPadI1))
            obj._keyRingPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingPadI2))
            obj._keyRingPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingPadIas1))
            obj._keyRingPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingPadI64As1))
            obj._keyRingPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFKeyRingFlags))
            writer.WriteInt32(KeyRingFlags);
        if (Bit(ObjectField.ObjFKeyRingListIdx))
            WriteIndexedInts(ref writer, List);
        if (Bit(ObjectField.ObjFKeyRingPadI1))
            writer.WriteInt32(_keyRingPadI1Reserved);
        if (Bit(ObjectField.ObjFKeyRingPadI2))
            writer.WriteInt32(_keyRingPadI2Reserved);
        if (Bit(ObjectField.ObjFKeyRingPadIas1))
            writer.WriteInt32(_keyRingPadIas1Reserved);
        if (Bit(ObjectField.ObjFKeyRingPadI64As1))
            writer.WriteInt64(_keyRingPadI64As1Reserved);
    }
}
