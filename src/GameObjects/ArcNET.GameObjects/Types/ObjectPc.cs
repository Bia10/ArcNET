using System.Collections;
using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectPc : ObjectCritter
{
    public int PcFlags { get; set; }
    public int PcFlagsFate { get; set; }
    public int[] PcReputation { get; set; } = [];
    public int[] PcReputationTs { get; set; } = [];
    public int PcBackground { get; set; }
    public int PcBackgroundText { get; set; }
    public int[] PcQuest { get; set; } = [];
    public int[] PcBlessing { get; set; } = [];
    public int[] PcBlessingTs { get; set; } = [];
    public int[] PcCurse { get; set; } = [];
    public int[] PcCurseTs { get; set; } = [];
    public int PcPartyId { get; set; }
    public int[] PcRumor { get; set; } = [];
    public int PcPadIas2 { get; set; }
    public int[] PcSchematicsFound { get; set; } = [];
    public int[] PcLogbookEgo { get; set; } = [];
    public int PcFogMask { get; set; }
    public PrefixedString PcPlayerName { get; set; }
    public int PcBankMoney { get; set; }
    public int[] PcGlobalFlags { get; set; } = [];
    public int[] PcGlobalVariables { get; set; } = [];
    public int PcPadI1 { get; set; }
    public int PcPadI2 { get; set; }
    public int PcPadIas1 { get; set; }
    public long PcPadI64As1 { get; set; }

    internal static new ObjectPc Read(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        var obj = new ObjectPc();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadCritterFields(ref reader, bitmap, isPrototype);
        obj.ReadPcFields(ref reader, bitmap, isPrototype);
        return obj;
    }

    internal new void Write(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteCritterFields(ref writer, bitmap, isPrototype);
        WritePcFields(ref writer, bitmap, isPrototype);
    }

    private void WritePcFields(ref SpanWriter writer, BitArray bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;

        if (Bit(ObjectField.ObjFPcFlags))
            writer.WriteInt32(PcFlags);
        if (Bit(ObjectField.ObjFPcFlagsFate))
            writer.WriteInt32(PcFlagsFate);
        if (Bit(ObjectField.ObjFPcReputationIdx))
            WriteIndexedInts(ref writer, PcReputation);
        if (Bit(ObjectField.ObjFPcReputationTsIdx))
            WriteIndexedInts(ref writer, PcReputationTs);
        if (Bit(ObjectField.ObjFPcBackground))
            writer.WriteInt32(PcBackground);
        if (Bit(ObjectField.ObjFPcBackgroundText))
            writer.WriteInt32(PcBackgroundText);
        if (Bit(ObjectField.ObjFPcQuestIdx))
            WriteIndexedInts(ref writer, PcQuest);
        if (Bit(ObjectField.ObjFPcBlessingIdx))
            WriteIndexedInts(ref writer, PcBlessing);
        if (Bit(ObjectField.ObjFPcBlessingTsIdx))
            WriteIndexedInts(ref writer, PcBlessingTs);
        if (Bit(ObjectField.ObjFPcCurseIdx))
            WriteIndexedInts(ref writer, PcCurse);
        if (Bit(ObjectField.ObjFPcCurseTsIdx))
            WriteIndexedInts(ref writer, PcCurseTs);
        if (Bit(ObjectField.ObjFPcPartyId))
            writer.WriteInt32(PcPartyId);
        if (Bit(ObjectField.ObjFPcRumorIdx))
            WriteIndexedInts(ref writer, PcRumor);
        if (Bit(ObjectField.ObjFPcPadIas2))
            writer.WriteInt32(PcPadIas2);
        if (Bit(ObjectField.ObjFPcSchematicsFoundIdx))
            WriteIndexedInts(ref writer, PcSchematicsFound);
        if (Bit(ObjectField.ObjFPcLogbookEgoIdx))
            WriteIndexedInts(ref writer, PcLogbookEgo);
        if (Bit(ObjectField.ObjFPcFogMask))
            writer.WriteInt32(PcFogMask);
        if (Bit(ObjectField.ObjFPcPlayerName))
            PcPlayerName.Write(ref writer);
        if (Bit(ObjectField.ObjFPcBankMoney))
            writer.WriteInt32(PcBankMoney);
        if (Bit(ObjectField.ObjFPcGlobalFlags))
            WriteIndexedInts(ref writer, PcGlobalFlags);
        if (Bit(ObjectField.ObjFPcGlobalVariables))
            WriteIndexedInts(ref writer, PcGlobalVariables);
        if (Bit(ObjectField.ObjFPcPadI1))
            writer.WriteInt32(PcPadI1);
        if (Bit(ObjectField.ObjFPcPadI2))
            writer.WriteInt32(PcPadI2);
        if (Bit(ObjectField.ObjFPcPadIas1))
            writer.WriteInt32(PcPadIas1);
        if (Bit(ObjectField.ObjFPcPadI64As1))
            writer.WriteInt64(PcPadI64As1);
    }

    private void ReadPcFields(ref SpanReader reader, BitArray bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => bitmap[(int)f] || isPrototype;

        if (Bit(ObjectField.ObjFPcFlags))
            PcFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcFlagsFate))
            PcFlagsFate = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcReputationIdx))
            PcReputation = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcReputationTsIdx))
            PcReputationTs = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcBackground))
            PcBackground = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcBackgroundText))
            PcBackgroundText = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcQuestIdx))
            PcQuest = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcBlessingIdx))
            PcBlessing = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcBlessingTsIdx))
            PcBlessingTs = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcCurseIdx))
            PcCurse = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcCurseTsIdx))
            PcCurseTs = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcPartyId))
            PcPartyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcRumorIdx))
            PcRumor = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcPadIas2))
            PcPadIas2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcSchematicsFoundIdx))
            PcSchematicsFound = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcLogbookEgoIdx))
            PcLogbookEgo = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcFogMask))
            PcFogMask = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPlayerName))
            PcPlayerName = reader.ReadPrefixedString();
        if (Bit(ObjectField.ObjFPcBankMoney))
            PcBankMoney = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcGlobalFlags))
            PcGlobalFlags = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcGlobalVariables))
            PcGlobalVariables = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcPadI1))
            PcPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPadI2))
            PcPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPadIas1))
            PcPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPadI64As1))
            PcPadI64As1 = reader.ReadInt64();
    }
}
