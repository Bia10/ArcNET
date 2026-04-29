using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectWall : ObjectCommon
{
    private int _wallPadI1Reserved;
    private int _wallPadI2Reserved;
    private int _wallPadIas1Reserved;
    private long _wallPadI64As1Reserved;

    public int WallFlags { get; internal set; }

    internal static ObjectWall Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectWall();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFWallFlags))
            obj.WallFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadI1))
            obj._wallPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadI2))
            obj._wallPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadIas1))
            obj._wallPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFWallPadI64As1))
            obj._wallPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFWallFlags))
            writer.WriteInt32(WallFlags);
        if (Bit(ObjectField.ObjFWallPadI1))
            writer.WriteInt32(_wallPadI1Reserved);
        if (Bit(ObjectField.ObjFWallPadI2))
            writer.WriteInt32(_wallPadI2Reserved);
        if (Bit(ObjectField.ObjFWallPadIas1))
            writer.WriteInt32(_wallPadIas1Reserved);
        if (Bit(ObjectField.ObjFWallPadI64As1))
            writer.WriteInt64(_wallPadI64As1Reserved);
    }
}
