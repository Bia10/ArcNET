using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectScenery : ObjectCommon
{
    private int _sceneryPadI2Reserved;
    private int _sceneryPadIas1Reserved;
    private long _sceneryPadI64As1Reserved;

    public SceneryFlags SceneryFlags { get; internal set; }
    public GameObjectGuid WhosInMe { get; internal set; }
    public int RespawnDelay { get; internal set; }

    internal static ObjectScenery Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectScenery();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.SceneryFlags))
            obj.SceneryFlags = unchecked((SceneryFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.SceneryWhosInMe))
            obj.WhosInMe = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.SceneryRespawnDelay))
            obj.RespawnDelay = reader.ReadInt32();
        if (Bit(ObjectField.SceneryPadI2))
            obj._sceneryPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.SceneryPadIas1))
            obj._sceneryPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.SceneryPadI64As1))
            obj._sceneryPadI64As1Reserved = reader.ReadInt64();
        return obj;
    }

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        if (Bit(ObjectField.SceneryFlags))
            writer.WriteInt32(unchecked((int)SceneryFlags));
        if (Bit(ObjectField.SceneryWhosInMe))
            WhosInMe.Write(ref writer);
        if (Bit(ObjectField.SceneryRespawnDelay))
            writer.WriteInt32(RespawnDelay);
        if (Bit(ObjectField.SceneryPadI2))
            writer.WriteInt32(_sceneryPadI2Reserved);
        if (Bit(ObjectField.SceneryPadIas1))
            writer.WriteInt32(_sceneryPadIas1Reserved);
        if (Bit(ObjectField.SceneryPadI64As1))
            writer.WriteInt64(_sceneryPadI64As1Reserved);
    }
}
