using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectPc : ObjectCritter
{
    private int _pcPadIas2Reserved;
    private int _pcPadI1Reserved;
    private int _pcPadI2Reserved;
    private int _pcPadIas1Reserved;
    private long _pcPadI64As1Reserved;

    public int PcFlags { get; internal set; }
    public int FateFlags { get; internal set; }
    public int[] Reputation { get; internal set; } = [];
    public int[] ReputationTs { get; internal set; } = [];
    public int Background { get; internal set; }
    public int BackgroundText { get; internal set; }
    public int[] Quest { get; internal set; } = [];
    public int[] Blessing { get; internal set; } = [];
    public int[] BlessingTs { get; internal set; } = [];
    public int[] Curse { get; internal set; } = [];
    public int[] CurseTs { get; internal set; } = [];
    public int PartyId { get; internal set; }
    public int[] Rumor { get; internal set; } = [];
    public int[] SchematicsFound { get; internal set; } = [];
    public int[] LogbookEgo { get; internal set; } = [];
    public int FogMask { get; internal set; }
    public PrefixedString PlayerName { get; internal set; } = new(string.Empty);
    public int BankMoney { get; internal set; }
    public int[] GlobalFlags { get; internal set; } = [];
    public int[] GlobalVariables { get; internal set; } = [];

    internal int PcPadIas2Reserved
    {
        get => _pcPadIas2Reserved;
        set => _pcPadIas2Reserved = value;
    }

    internal int PcPadI1Reserved
    {
        get => _pcPadI1Reserved;
        set => _pcPadI1Reserved = value;
    }

    internal int PcPadI2Reserved
    {
        get => _pcPadI2Reserved;
        set => _pcPadI2Reserved = value;
    }

    internal int PcPadIas1Reserved
    {
        get => _pcPadIas1Reserved;
        set => _pcPadIas1Reserved = value;
    }

    internal long PcPadI64As1Reserved
    {
        get => _pcPadI64As1Reserved;
        set => _pcPadI64As1Reserved = value;
    }

    internal static new ObjectPc Read(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectPcCodec.Read(ref reader, bitmap, isPrototype);

    internal override void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectPcCodec.Write(this, ref writer, bitmap, isPrototype);

    private void WritePcFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype) =>
        ObjectPcCodec.WriteFields(this, ref writer, bitmap, isPrototype);

    private void ReadPcFields(ref SpanReader reader, byte[] bitmap, bool isPrototype) =>
        ObjectPcCodec.ReadFields(this, ref reader, bitmap, isPrototype);
}
