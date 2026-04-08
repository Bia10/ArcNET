using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectKeyRing : ObjectItem
{
    public int KeyRingFlags { get; internal set; }
    public int[] KeyRingList { get; internal set; } = [];
    public int KeyRingPadI1 { get; internal set; }
    public int KeyRingPadI2 { get; internal set; }
    public int KeyRingPadIas1 { get; internal set; }
    public long KeyRingPadI64As1 { get; internal set; }

    internal static ObjectKeyRing Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectKeyRing();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadItemFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFKeyRingFlags))
            obj.KeyRingFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingListIdx))
            obj.KeyRingList = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFKeyRingPadI1))
            obj.KeyRingPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingPadI2))
            obj.KeyRingPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingPadIas1))
            obj.KeyRingPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFKeyRingPadI64As1))
            obj.KeyRingPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteItemFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFKeyRingFlags))
            writer.WriteInt32(KeyRingFlags);
        if (Bit(ObjectField.ObjFKeyRingListIdx))
            WriteIndexedInts(ref writer, KeyRingList);
        if (Bit(ObjectField.ObjFKeyRingPadI1))
            writer.WriteInt32(KeyRingPadI1);
        if (Bit(ObjectField.ObjFKeyRingPadI2))
            writer.WriteInt32(KeyRingPadI2);
        if (Bit(ObjectField.ObjFKeyRingPadIas1))
            writer.WriteInt32(KeyRingPadIas1);
        if (Bit(ObjectField.ObjFKeyRingPadI64As1))
            writer.WriteInt64(KeyRingPadI64As1);
    }
}
