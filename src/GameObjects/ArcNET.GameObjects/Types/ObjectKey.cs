using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectKey : ObjectItem
{
    public int KeyKeyId { get; internal set; }
    public int KeyPadI1 { get; internal set; }
    public int KeyPadI2 { get; internal set; }
    public int KeyPadIas1 { get; internal set; }
    public long KeyPadI64As1 { get; internal set; }

    internal static ObjectKey Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectKey();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFKeyKeyId))
            obj.KeyKeyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadI1))
            obj.KeyPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadI2))
            obj.KeyPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadIas1))
            obj.KeyPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyPadI64As1))
            obj.KeyPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFKeyKeyId))
            writer.WriteInt32(KeyKeyId);
        if (Bit(ObjectField.ObjFKeyPadI1))
            writer.WriteInt32(KeyPadI1);
        if (Bit(ObjectField.ObjFKeyPadI2))
            writer.WriteInt32(KeyPadI2);
        if (Bit(ObjectField.ObjFKeyPadIas1))
            writer.WriteInt32(KeyPadIas1);
        if (Bit(ObjectField.ObjFKeyPadI64As1))
            writer.WriteInt64(KeyPadI64As1);
    }
}
