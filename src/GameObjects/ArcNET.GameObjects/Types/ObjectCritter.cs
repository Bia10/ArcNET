using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public class ObjectCritter : ObjectCommon
{
    private int _inventoryCountReserved;
    private int _critterPadI1Reserved;
    private int _critterPadI2Reserved;
    private int _critterPadI3Reserved;
    private int _critterPadIas1Reserved;
    private long _critterPadI64As1Reserved;

    public ObjFCritterFlags CritterFlags { get; internal set; }
    public ObjFCritterFlags2 CritterFlags2 { get; internal set; }
    public int[] CritterStatBase { get; internal set; } = [];
    public int[] CritterBasicSkill { get; internal set; } = [];
    public int[] CritterTechSkill { get; internal set; } = [];
    public int[] CritterSpellTech { get; internal set; } = [];
    public int CritterFatiguePts { get; internal set; }
    public int CritterFatigueAdj { get; internal set; }
    public int CritterFatigueDamage { get; internal set; }
    public int CritterCritHitChart { get; internal set; }
    public int[] CritterEffects { get; internal set; } = [];
    public int[] CritterEffectCause { get; internal set; } = [];
    public GameObjectGuid CritterFleeingFrom { get; internal set; }
    public int CritterPortrait { get; internal set; }
    public int CritterGold { get; internal set; }
    public int CritterArrows { get; internal set; }
    public int CritterBullets { get; internal set; }
    public int CritterPowerCells { get; internal set; }
    public int CritterFuel { get; internal set; }
    public GameObjectGuid[] CritterInventoryList { get; internal set; } = [];
    public int CritterInventorySource { get; internal set; }
    public int CritterDescriptionUnknown { get; internal set; }
    public GameObjectGuid[] CritterFollowers { get; internal set; } = [];
    public Location CritterTeleportDest { get; internal set; }
    public int CritterTeleportMap { get; internal set; }
    public int CritterDeathTime { get; internal set; }
    public int CritterAutoLevelScheme { get; internal set; }

    internal int InventoryCountReserved
    {
        get => _inventoryCountReserved;
        set => _inventoryCountReserved = value;
    }

    internal int CritterPadI1Reserved
    {
        get => _critterPadI1Reserved;
        set => _critterPadI1Reserved = value;
    }

    internal int CritterPadI2Reserved
    {
        get => _critterPadI2Reserved;
        set => _critterPadI2Reserved = value;
    }

    internal int CritterPadI3Reserved
    {
        get => _critterPadI3Reserved;
        set => _critterPadI3Reserved = value;
    }

    internal int CritterPadIas1Reserved
    {
        get => _critterPadIas1Reserved;
        set => _critterPadIas1Reserved = value;
    }

    internal long CritterPadI64As1Reserved
    {
        get => _critterPadI64As1Reserved;
        set => _critterPadI64As1Reserved = value;
    }

    internal static ObjectCritter Read(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectCritterCodec.Read(ref reader, bitmap, isPrototype);

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectCritterCodec.Write(this, ref writer, bitmap, isPrototype);

    protected void ReadCritterFields(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectCritterCodec.ReadFields(this, ref reader, bitmap, isPrototype);

    protected void WriteCritterFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectCritterCodec.WriteFields(this, ref writer, bitmap, isPrototype);
}
