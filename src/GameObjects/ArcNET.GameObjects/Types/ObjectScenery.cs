using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectScenery : ObjectCommon
{
    private int _sceneryPadI2Reserved;
    private int _sceneryPadIas1Reserved;
    private long _sceneryPadI64As1Reserved;

    public ObjFSceneryFlags SceneryFlags { get; internal set; }
    public GameObjectGuid WhosInMe { get; internal set; }
    public int RespawnDelay { get; internal set; }

    internal static ObjectScenery Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectScenery();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFSceneryFlags))
            obj.SceneryFlags = unchecked((ObjFSceneryFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFSceneryWhosInMe))
            obj.WhosInMe = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFSceneryRespawnDelay))
            obj.RespawnDelay = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryPadI2))
            obj._sceneryPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryPadIas1))
            obj._sceneryPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryPadI64As1))
            obj._sceneryPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.ObjFSceneryFlags))
            writer.WriteInt32(unchecked((int)SceneryFlags));
        if (Bit(ObjectField.ObjFSceneryWhosInMe))
            WhosInMe.Write(ref writer);
        if (Bit(ObjectField.ObjFSceneryRespawnDelay))
            writer.WriteInt32(RespawnDelay);
        if (Bit(ObjectField.ObjFSceneryPadI2))
            writer.WriteInt32(_sceneryPadI2Reserved);
        if (Bit(ObjectField.ObjFSceneryPadIas1))
            writer.WriteInt32(_sceneryPadIas1Reserved);
        if (Bit(ObjectField.ObjFSceneryPadI64As1))
            writer.WriteInt64(_sceneryPadI64As1Reserved);
    }
}
