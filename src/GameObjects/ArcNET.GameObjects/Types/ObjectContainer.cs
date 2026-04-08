using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectContainer : ObjectCommon
{
    public int ContainerFlags { get; internal set; }
    public int ContainerLockDifficulty { get; internal set; }
    public int ContainerKeyId { get; internal set; }
    public int ContainerInventoryNum { get; internal set; }
    public GameObjectGuid[] ContainerInventoryList { get; internal set; } = [];
    public int ContainerInventorySource { get; internal set; }
    public int ContainerNotifyNpc { get; internal set; }
    public int ContainerPadI1 { get; internal set; }
    public int ContainerPadI2 { get; internal set; }
    public int ContainerPadIas1 { get; internal set; }
    public long ContainerPadI64As1 { get; internal set; }

    internal static ObjectContainer Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectContainer();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFContainerFlags))
            obj.ContainerFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerLockDifficulty))
            obj.ContainerLockDifficulty = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerKeyId))
            obj.ContainerKeyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerInventoryNum))
            obj.ContainerInventoryNum = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerInventoryListIdx))
            obj.ContainerInventoryList = ReadGuidArray(ref reader, obj.ContainerInventoryNum);
        if (Bit(ObjectField.ObjFContainerInventorySource))
            obj.ContainerInventorySource = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerNotifyNpc))
            obj.ContainerNotifyNpc = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerPadI1))
            obj.ContainerPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerPadI2))
            obj.ContainerPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerPadIas1))
            obj.ContainerPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFContainerPadI64As1))
            obj.ContainerPadI64As1 = reader.ReadInt64();
        return obj;
    }

    private static GameObjectGuid[] ReadGuidArray(ref SpanReader reader, int count)
    {
        if (count == 0)
            return [];
        var result = new GameObjectGuid[count];
        for (var i = 0; i < count; i++)
            result[i] = reader.ReadGameObjectGuid();
        return result;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;
        if (Bit(ObjectField.ObjFContainerFlags))
            writer.WriteInt32(ContainerFlags);
        if (Bit(ObjectField.ObjFContainerLockDifficulty))
            writer.WriteInt32(ContainerLockDifficulty);
        if (Bit(ObjectField.ObjFContainerKeyId))
            writer.WriteInt32(ContainerKeyId);
        if (Bit(ObjectField.ObjFContainerInventoryNum))
            writer.WriteInt32(ContainerInventoryNum);
        if (Bit(ObjectField.ObjFContainerInventoryListIdx))
        {
            foreach (var g in ContainerInventoryList)
                g.Write(ref writer);
        }
        if (Bit(ObjectField.ObjFContainerInventorySource))
            writer.WriteInt32(ContainerInventorySource);
        if (Bit(ObjectField.ObjFContainerNotifyNpc))
            writer.WriteInt32(ContainerNotifyNpc);
        if (Bit(ObjectField.ObjFContainerPadI1))
            writer.WriteInt32(ContainerPadI1);
        if (Bit(ObjectField.ObjFContainerPadI2))
            writer.WriteInt32(ContainerPadI2);
        if (Bit(ObjectField.ObjFContainerPadIas1))
            writer.WriteInt32(ContainerPadIas1);
        if (Bit(ObjectField.ObjFContainerPadI64As1))
            writer.WriteInt64(ContainerPadI64As1);
    }
}
