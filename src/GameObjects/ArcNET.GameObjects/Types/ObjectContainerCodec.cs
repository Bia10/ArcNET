using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

internal static class ObjectContainerCodec
{
    public static ObjectContainer Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectContainer();
        ObjectCommonFieldsCodec.Read(obj, ref reader, bitmap, isPrototype);
        ReadFields(obj, ref reader, bitmap, isPrototype);
        return obj;
    }

    public static void Write(ObjectContainer obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        ObjectCommonFieldsCodec.Write(obj, ref writer, bitmap, isPrototype);
        WriteFields(obj, ref writer, bitmap, isPrototype);
    }

    public static void ReadFields(ObjectContainer obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ContainerFlags))
            obj.ContainerFlags = unchecked((ContainerFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ContainerLockDifficulty))
            obj.LockDifficulty = reader.ReadInt32();
        if (Bit(ObjectField.ContainerKeyId))
            obj.KeyId = reader.ReadInt32();

        var inventory = ObjectInventoryGuidListCodec.Read(
            ref reader,
            Bit(ObjectField.ContainerInventoryNum),
            Bit(ObjectField.ContainerInventoryListIdx),
            obj.InventoryCountReserved,
            obj.InventoryList,
            "Container"
        );
        obj.InventoryCountReserved = inventory.ReservedCount;
        obj.InventoryList = inventory.Values;

        if (Bit(ObjectField.ContainerInventorySource))
            obj.InventorySource = reader.ReadInt32();
        if (Bit(ObjectField.ContainerNotifyNpc))
            obj.NotifyNpc = reader.ReadInt32();
        if (Bit(ObjectField.ContainerPadI1))
            obj.ContainerPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ContainerPadI2))
            obj.ContainerPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ContainerPadIas1))
            obj.ContainerPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ContainerPadI64As1))
            obj.ContainerPadI64As1Reserved = reader.ReadInt64();
    }

    public static void WriteFields(ObjectContainer obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        var hasInventoryCount = Bit(ObjectField.ContainerInventoryNum);
        var hasInventoryList = Bit(ObjectField.ContainerInventoryListIdx);

        if (Bit(ObjectField.ContainerFlags))
            writer.WriteInt32(unchecked((int)obj.ContainerFlags));
        if (Bit(ObjectField.ContainerLockDifficulty))
            writer.WriteInt32(obj.LockDifficulty);
        if (Bit(ObjectField.ContainerKeyId))
            writer.WriteInt32(obj.KeyId);

        ObjectInventoryGuidListCodec.Write(
            ref writer,
            hasInventoryCount,
            hasInventoryList,
            obj.InventoryCountReserved,
            obj.InventoryList,
            "Container"
        );

        if (Bit(ObjectField.ContainerInventorySource))
            writer.WriteInt32(obj.InventorySource);
        if (Bit(ObjectField.ContainerNotifyNpc))
            writer.WriteInt32(obj.NotifyNpc);
        if (Bit(ObjectField.ContainerPadI1))
            writer.WriteInt32(obj.ContainerPadI1Reserved);
        if (Bit(ObjectField.ContainerPadI2))
            writer.WriteInt32(obj.ContainerPadI2Reserved);
        if (Bit(ObjectField.ContainerPadIas1))
            writer.WriteInt32(obj.ContainerPadIas1Reserved);
        if (Bit(ObjectField.ContainerPadI64As1))
            writer.WriteInt64(obj.ContainerPadI64As1Reserved);
    }
}
