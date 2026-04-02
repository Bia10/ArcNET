using System.Collections;
using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectTrap : ObjectCommon
{
    public int TrapFlags { get; set; }
    public int TrapDifficulty { get; set; }
    public int TrapPadI2 { get; set; }
    public int TrapPadIas1 { get; set; }
    public long TrapPadI64As1 { get; set; }

    internal static ObjectTrap Read(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        var obj = new ObjectTrap();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFTrapFlags))
            obj.TrapFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFTrapDifficulty))
            obj.TrapDifficulty = reader.ReadInt32();
        if (Bit(ObjectField.ObjFTrapPadI2))
            obj.TrapPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFTrapPadIas1))
            obj.TrapPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFTrapPadI64As1))
            obj.TrapPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFTrapFlags))
            writer.WriteInt32(TrapFlags);
        if (Bit(ObjectField.ObjFTrapDifficulty))
            writer.WriteInt32(TrapDifficulty);
        if (Bit(ObjectField.ObjFTrapPadI2))
            writer.WriteInt32(TrapPadI2);
        if (Bit(ObjectField.ObjFTrapPadIas1))
            writer.WriteInt32(TrapPadIas1);
        if (Bit(ObjectField.ObjFTrapPadI64As1))
            writer.WriteInt64(TrapPadI64As1);
    }
}
