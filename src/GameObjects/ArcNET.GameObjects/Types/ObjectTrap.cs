using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectTrap : ObjectCommon
{
    private int _trapPadI2Reserved;
    private int _trapPadIas1Reserved;
    private long _trapPadI64As1Reserved;

    public int TrapFlags { get; internal set; }
    public int Difficulty { get; internal set; }

    internal static ObjectTrap Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectTrap();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.TrapFlags))
            obj.TrapFlags = reader.ReadInt32();
        if (Bit(ObjectField.TrapDifficulty))
            obj.Difficulty = reader.ReadInt32();
        if (Bit(ObjectField.TrapPadI2))
            obj._trapPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.TrapPadIas1))
            obj._trapPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.TrapPadI64As1))
            obj._trapPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.TrapFlags))
            writer.WriteInt32(TrapFlags);
        if (Bit(ObjectField.TrapDifficulty))
            writer.WriteInt32(Difficulty);
        if (Bit(ObjectField.TrapPadI2))
            writer.WriteInt32(_trapPadI2Reserved);
        if (Bit(ObjectField.TrapPadIas1))
            writer.WriteInt32(_trapPadIas1Reserved);
        if (Bit(ObjectField.TrapPadI64As1))
            writer.WriteInt64(_trapPadI64As1Reserved);
    }
}
