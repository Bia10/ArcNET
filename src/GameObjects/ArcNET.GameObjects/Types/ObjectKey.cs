using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectKey : ObjectItem
{
    private int _keyPadI1Reserved;
    private int _keyPadI2Reserved;
    private int _keyPadIas1Reserved;
    private long _keyPadI64As1Reserved;

    public int KeyId { get; internal set; }

    internal static ObjectKey Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectKey();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFKeyKeyId))
            obj.KeyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadI1))
            obj._keyPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadI2))
            obj._keyPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadIas1))
            obj._keyPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadI64As1))
            obj._keyPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFKeyKeyId))
            writer.WriteInt32(KeyId);
        if (Bit(ObjectField.ObjFKeyPadI1))
            writer.WriteInt32(_keyPadI1Reserved);
        if (Bit(ObjectField.ObjFKeyPadI2))
            writer.WriteInt32(_keyPadI2Reserved);
        if (Bit(ObjectField.ObjFKeyPadIas1))
            writer.WriteInt32(_keyPadIas1Reserved);
        if (Bit(ObjectField.ObjFKeyPadI64As1))
            writer.WriteInt64(_keyPadI64As1Reserved);
    }
}
