using System.Collections;
using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectScenery : ObjectCommon
{
    public int SceneryFlags { get; set; }
    public GameObjectGuid SceneryWhosInMe { get; set; }
    public int SceneryRespawnDelay { get; set; }
    public int SceneryPadI2 { get; set; }
    public int SceneryPadIas1 { get; set; }
    public long SceneryPadI64As1 { get; set; }

    internal static ObjectScenery Read(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        var obj = new ObjectScenery();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFSceneryFlags))
            obj.SceneryFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryWhosInMe))
            obj.SceneryWhosInMe = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFSceneryRespawnDelay))
            obj.SceneryRespawnDelay = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryPadI2))
            obj.SceneryPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryPadIas1))
            obj.SceneryPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFSceneryPadI64As1))
            obj.SceneryPadI64As1 = reader.ReadInt64();
        return obj;
    }

    internal void Write(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;
        if (Bit(ObjectField.ObjFSceneryFlags))
            writer.WriteInt32(SceneryFlags);
        if (Bit(ObjectField.ObjFSceneryWhosInMe))
            SceneryWhosInMe.Write(ref writer);
        if (Bit(ObjectField.ObjFSceneryRespawnDelay))
            writer.WriteInt32(SceneryRespawnDelay);
        if (Bit(ObjectField.ObjFSceneryPadI2))
            writer.WriteInt32(SceneryPadI2);
        if (Bit(ObjectField.ObjFSceneryPadIas1))
            writer.WriteInt32(SceneryPadIas1);
        if (Bit(ObjectField.ObjFSceneryPadI64As1))
            writer.WriteInt64(SceneryPadI64As1);
    }
}
