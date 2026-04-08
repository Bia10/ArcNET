using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWall : ObjectCommon
{
    public int WallFlags { get; internal set; }
    public int WallPadI1 { get; internal set; }
    public int WallPadI2 { get; internal set; }
    public int WallPadIas1 { get; internal set; }
    public long WallPadI64As1 { get; internal set; }

    internal static ObjectWall Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectWall();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFWallFlags))
            obj.WallFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadI1))
            obj.WallPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadI2))
            obj.WallPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadIas1))
            obj.WallPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadI64As1))
            obj.WallPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFWallFlags))
            writer.WriteInt32(WallFlags);
        if (Bit(ObjectField.ObjFWallPadI1))
            writer.WriteInt32(WallPadI1);
        if (Bit(ObjectField.ObjFWallPadI2))
            writer.WriteInt32(WallPadI2);
        if (Bit(ObjectField.ObjFWallPadIas1))
            writer.WriteInt32(WallPadIas1);
        if (Bit(ObjectField.ObjFWallPadI64As1))
            writer.WriteInt64(WallPadI64As1);
    }
}
