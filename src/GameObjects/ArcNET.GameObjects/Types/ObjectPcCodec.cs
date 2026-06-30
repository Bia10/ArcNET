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

        if (Bit(ObjectField.PcFlags))
            obj.PcFlags = reader.ReadInt32();
        if (Bit(ObjectField.PcFlagsFate))
            obj.FateFlags = reader.ReadInt32();
        if (Bit(ObjectField.PcReputationIdx))
            obj.Reputation = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcReputationTsIdx))
            obj.ReputationTs = ObjectCommon.ReadIndexedLongs(ref reader);
        if (Bit(ObjectField.PcBackground))
            obj.Background = reader.ReadInt32();
        if (Bit(ObjectField.PcBackgroundText))
            obj.BackgroundText = reader.ReadInt32();
        if (Bit(ObjectField.PcQuestIdx))
        {
            obj.QuestEntries = ObjectCommon.ReadIndexedPcQuestStates(ref reader);
            obj.Quest = [.. obj.QuestEntries.Select(static entry => entry.State)];
        }
        if (Bit(ObjectField.PcBlessingIdx))
            obj.Blessing = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcBlessingTsIdx))
            obj.BlessingTs = ObjectCommon.ReadIndexedLongs(ref reader);
        if (Bit(ObjectField.PcCurseIdx))
            obj.Curse = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcCurseTsIdx))
            obj.CurseTs = ObjectCommon.ReadIndexedLongs(ref reader);
        if (Bit(ObjectField.PcPartyId))
            obj.PartyId = reader.ReadInt32();
        if (Bit(ObjectField.PcRumorIdx))
            obj.Rumor = ObjectCommon.ReadIndexedLongs(ref reader);
        if (Bit(ObjectField.PcPadIas2))
            obj.PcPadIas2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.PcSchematicsFoundIdx))
            obj.SchematicsFound = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcLogbookEgoIdx))
            obj.LogbookEgo = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcFogMask))
            obj.FogMask = reader.ReadInt32();
        if (Bit(ObjectField.PcPlayerName))
            obj.PlayerName = ObjectSerializationHelpers.ReadRawString(ref reader);
        if (Bit(ObjectField.PcBankMoney))
            obj.BankMoney = reader.ReadInt32();
        if (Bit(ObjectField.PcGlobalFlags))
            obj.GlobalFlags = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcGlobalVariables))
            obj.GlobalVariables = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.PcPadI1))
            obj.PcPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.PcPadI2))
            obj.PcPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.PcPadIas1))
            obj.PcPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.PcPadI64As1))
            obj.PcPadI64As1Reserved = reader.ReadInt64();
    }

    public static void WriteFields(ObjectPc obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.PcFlags))
            writer.WriteInt32(obj.PcFlags);
        if (Bit(ObjectField.PcFlagsFate))
            writer.WriteInt32(obj.FateFlags);
        if (Bit(ObjectField.PcReputationIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Reputation);
        if (Bit(ObjectField.PcReputationTsIdx))
            ObjectCommon.WriteIndexedLongs(ref writer, obj.ReputationTs);
        if (Bit(ObjectField.PcBackground))
            writer.WriteInt32(obj.Background);
        if (Bit(ObjectField.PcBackgroundText))
            writer.WriteInt32(obj.BackgroundText);
        if (Bit(ObjectField.PcQuestIdx))
            ObjectCommon.WriteIndexedPcQuestStates(
                ref writer,
                obj.QuestEntries.Length != 0
                    ? obj.QuestEntries
                    : [.. obj.Quest.Select(static state => new PcQuestState(0, state))]
            );
        if (Bit(ObjectField.PcBlessingIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Blessing);
        if (Bit(ObjectField.PcBlessingTsIdx))
            ObjectCommon.WriteIndexedLongs(ref writer, obj.BlessingTs);
        if (Bit(ObjectField.PcCurseIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Curse);
        if (Bit(ObjectField.PcCurseTsIdx))
            ObjectCommon.WriteIndexedLongs(ref writer, obj.CurseTs);
        if (Bit(ObjectField.PcPartyId))
            writer.WriteInt32(obj.PartyId);
        if (Bit(ObjectField.PcRumorIdx))
            ObjectCommon.WriteIndexedLongs(ref writer, obj.Rumor);
        if (Bit(ObjectField.PcPadIas2))
            writer.WriteInt32(obj.PcPadIas2Reserved);
        if (Bit(ObjectField.PcSchematicsFoundIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.SchematicsFound);
        if (Bit(ObjectField.PcLogbookEgoIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.LogbookEgo);
        if (Bit(ObjectField.PcFogMask))
            writer.WriteInt32(obj.FogMask);
        if (Bit(ObjectField.PcPlayerName))
            ObjectSerializationHelpers.WriteRawString(ref writer, obj.PlayerName);
        if (Bit(ObjectField.PcBankMoney))
            writer.WriteInt32(obj.BankMoney);
        if (Bit(ObjectField.PcGlobalFlags))
            ObjectCommon.WriteIndexedInts(ref writer, obj.GlobalFlags);
        if (Bit(ObjectField.PcGlobalVariables))
            ObjectCommon.WriteIndexedInts(ref writer, obj.GlobalVariables);
        if (Bit(ObjectField.PcPadI1))
            writer.WriteInt32(obj.PcPadI1Reserved);
        if (Bit(ObjectField.PcPadI2))
            writer.WriteInt32(obj.PcPadI2Reserved);
        if (Bit(ObjectField.PcPadIas1))
            writer.WriteInt32(obj.PcPadIas1Reserved);
        if (Bit(ObjectField.PcPadI64As1))
            writer.WriteInt64(obj.PcPadI64As1Reserved);
    }
}
