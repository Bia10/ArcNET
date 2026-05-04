using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

internal static class ObjectPcCodec
{
    public static ObjectPc Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectPc();
        ObjectCommonFieldsCodec.Read(obj, ref reader, bitmap, isPrototype);
        ObjectCritterCodec.ReadFields(obj, ref reader, bitmap, isPrototype);
        ReadFields(obj, ref reader, bitmap, isPrototype);
        return obj;
    }

    public static void Write(ObjectPc obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        ObjectCommonFieldsCodec.Write(obj, ref writer, bitmap, isPrototype);
        ObjectCritterCodec.WriteFields(obj, ref writer, bitmap, isPrototype);
        WriteFields(obj, ref writer, bitmap, isPrototype);
    }

    public static void ReadFields(ObjectPc obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFPcFlags))
            obj.PcFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcFlagsFate))
            obj.FateFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcReputationIdx))
            obj.Reputation = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcReputationTsIdx))
            obj.ReputationTs = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcBackground))
            obj.Background = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcBackgroundText))
            obj.BackgroundText = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcQuestIdx))
            obj.Quest = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcBlessingIdx))
            obj.Blessing = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcBlessingTsIdx))
            obj.BlessingTs = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcCurseIdx))
            obj.Curse = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcCurseTsIdx))
            obj.CurseTs = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcPartyId))
            obj.PartyId = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcRumorIdx))
            obj.Rumor = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcPadIas2))
            obj.PcPadIas2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcSchematicsFoundIdx))
            obj.SchematicsFound = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcLogbookEgoIdx))
            obj.LogbookEgo = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcFogMask))
            obj.FogMask = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPlayerName))
            obj.PlayerName = ObjectSerializationHelpers.ReadRawString(ref reader);
        if (Bit(ObjectField.ObjFPcBankMoney))
            obj.BankMoney = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcGlobalFlags))
            obj.GlobalFlags = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcGlobalVariables))
            obj.GlobalVariables = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFPcPadI1))
            obj.PcPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPadI2))
            obj.PcPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPadIas1))
            obj.PcPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFPcPadI64As1))
            obj.PcPadI64As1Reserved = reader.ReadInt64();
    }

    public static void WriteFields(ObjectPc obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFPcFlags))
            writer.WriteInt32(obj.PcFlags);
        if (Bit(ObjectField.ObjFPcFlagsFate))
            writer.WriteInt32(obj.FateFlags);
        if (Bit(ObjectField.ObjFPcReputationIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Reputation);
        if (Bit(ObjectField.ObjFPcReputationTsIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReputationTs);
        if (Bit(ObjectField.ObjFPcBackground))
            writer.WriteInt32(obj.Background);
        if (Bit(ObjectField.ObjFPcBackgroundText))
            writer.WriteInt32(obj.BackgroundText);
        if (Bit(ObjectField.ObjFPcQuestIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Quest);
        if (Bit(ObjectField.ObjFPcBlessingIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Blessing);
        if (Bit(ObjectField.ObjFPcBlessingTsIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.BlessingTs);
        if (Bit(ObjectField.ObjFPcCurseIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Curse);
        if (Bit(ObjectField.ObjFPcCurseTsIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CurseTs);
        if (Bit(ObjectField.ObjFPcPartyId))
            writer.WriteInt32(obj.PartyId);
        if (Bit(ObjectField.ObjFPcRumorIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Rumor);
        if (Bit(ObjectField.ObjFPcPadIas2))
            writer.WriteInt32(obj.PcPadIas2Reserved);
        if (Bit(ObjectField.ObjFPcSchematicsFoundIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.SchematicsFound);
        if (Bit(ObjectField.ObjFPcLogbookEgoIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.LogbookEgo);
        if (Bit(ObjectField.ObjFPcFogMask))
            writer.WriteInt32(obj.FogMask);
        if (Bit(ObjectField.ObjFPcPlayerName))
            ObjectSerializationHelpers.WriteRawString(ref writer, obj.PlayerName);
        if (Bit(ObjectField.ObjFPcBankMoney))
            writer.WriteInt32(obj.BankMoney);
        if (Bit(ObjectField.ObjFPcGlobalFlags))
            ObjectCommon.WriteIndexedInts(ref writer, obj.GlobalFlags);
        if (Bit(ObjectField.ObjFPcGlobalVariables))
            ObjectCommon.WriteIndexedInts(ref writer, obj.GlobalVariables);
        if (Bit(ObjectField.ObjFPcPadI1))
            writer.WriteInt32(obj.PcPadI1Reserved);
        if (Bit(ObjectField.ObjFPcPadI2))
            writer.WriteInt32(obj.PcPadI2Reserved);
        if (Bit(ObjectField.ObjFPcPadIas1))
            writer.WriteInt32(obj.PcPadIas1Reserved);
        if (Bit(ObjectField.ObjFPcPadI64As1))
            writer.WriteInt64(obj.PcPadI64As1Reserved);
    }
}
