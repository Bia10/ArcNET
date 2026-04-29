using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectContainer : ObjectCommon
{
    private int _inventoryCountReserved;
    private int _containerPadI1Reserved;
    private int _containerPadI2Reserved;
    private int _containerPadIas1Reserved;
    private long _containerPadI64As1Reserved;

    public ObjFContainerFlags ContainerFlags { get; internal set; }
    public int LockDifficulty { get; internal set; }
    public int KeyId { get; internal set; }
    public GameObjectGuid[] InventoryList { get; internal set; } = [];
    public int InventorySource { get; internal set; }
    public int NotifyNpc { get; internal set; }

    internal int InventoryCountReserved
    {
        get => _inventoryCountReserved;
        set => _inventoryCountReserved = value;
    }

    internal int ContainerPadI1Reserved
    {
        get => _containerPadI1Reserved;
        set => _containerPadI1Reserved = value;
    }

    internal int ContainerPadI2Reserved
    {
        get => _containerPadI2Reserved;
        set => _containerPadI2Reserved = value;
    }

    internal int ContainerPadIas1Reserved
    {
        get => _containerPadIas1Reserved;
        set => _containerPadIas1Reserved = value;
    }

    internal long ContainerPadI64As1Reserved
    {
        get => _containerPadI64As1Reserved;
        set => _containerPadI64As1Reserved = value;
    }

    internal static ObjectContainer Read(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectContainerCodec.Read(ref reader, bitmap, isPrototype);

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectContainerCodec.Write(this, ref writer, bitmap, isPrototype);
}
