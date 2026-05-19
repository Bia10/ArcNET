using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

internal static class ObjectNpcCodec
{
    public static ObjectNpc Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectNpc();
        ObjectCommonFieldsCodec.Read(obj, ref reader, bitmap, isPrototype);
        ObjectCritterCodec.ReadFields(obj, ref reader, bitmap, isPrototype);
        ReadFields(obj, ref reader, bitmap, isPrototype);
        return obj;
    }

    public static void Write(ObjectNpc obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        ObjectCommonFieldsCodec.Write(obj, ref writer, bitmap, isPrototype);
        ObjectCritterCodec.WriteFields(obj, ref writer, bitmap, isPrototype);
        WriteFields(obj, ref writer, bitmap, isPrototype);
    }

    public static void ReadFields(ObjectNpc obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.NpcFlags))
            obj.NpcFlags = unchecked((NpcFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.NpcLeader))
            obj.Leader = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.NpcAiData))
            obj.AiData = reader.ReadInt32();
        if (Bit(ObjectField.NpcCombatFocus))
            obj.CombatFocus = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.NpcWhoHitMeLast))
            obj.WhoHitMeLast = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.NpcExperienceWorth))
            obj.ExperienceWorth = reader.ReadInt32();
        if (Bit(ObjectField.NpcExperiencePool))
            obj.ExperiencePool = reader.ReadInt32();
        if (Bit(ObjectField.NpcWaypointsIdx))
            obj.Waypoints = ObjectSerializationHelpers.ReadLocationArray(ref reader);
        if (Bit(ObjectField.NpcWaypointCurrent))
            obj.WaypointCurrent = reader.ReadInt32();
        if (Bit(ObjectField.NpcStandpointDay))
            obj.StandpointDay = ObjectSerializationHelpers.ReadLocation(ref reader);
        if (Bit(ObjectField.NpcStandpointNight))
            obj.StandpointNight = ObjectSerializationHelpers.ReadLocation(ref reader);
        if (Bit(ObjectField.NpcOrigin))
            obj.Origin = reader.ReadInt32();
        if (Bit(ObjectField.NpcFaction))
            obj.Faction = reader.ReadInt32();
        if (Bit(ObjectField.NpcRetailPriceMultiplier))
            obj.RetailPriceMultiplier = reader.ReadInt32();
        if (Bit(ObjectField.NpcSubstituteInventory))
            obj.SubstituteInventory = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.NpcReactionBase))
            obj.ReactionBase = reader.ReadInt32();
        if (Bit(ObjectField.NpcSocialClass))
            obj.SocialClass = reader.ReadInt32();
        if (Bit(ObjectField.NpcReactionPcIdx))
            obj.ReactionPc = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.NpcReactionLevelIdx))
            obj.ReactionLevel = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.NpcReactionTimeIdx))
            obj.ReactionTime = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.NpcWait))
            obj.Wait = reader.ReadInt32();
        if (Bit(ObjectField.NpcGeneratorData))
            obj.GeneratorData = reader.ReadInt32();
        if (Bit(ObjectField.NpcPadI1))
            obj.NpcPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.NpcDamageIdx))
            obj.Damage = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.NpcHostileListIdx))
            obj.HostileList = ObjectCommon.ReadIndexedInts(ref reader);
    }

    public static void WriteFields(ObjectNpc obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.NpcFlags))
            writer.WriteInt32(unchecked((int)obj.NpcFlags));
        if (Bit(ObjectField.NpcLeader))
            obj.Leader.Write(ref writer);
        if (Bit(ObjectField.NpcAiData))
            writer.WriteInt32(obj.AiData);
        if (Bit(ObjectField.NpcCombatFocus))
            obj.CombatFocus.Write(ref writer);
        if (Bit(ObjectField.NpcWhoHitMeLast))
            obj.WhoHitMeLast.Write(ref writer);
        if (Bit(ObjectField.NpcExperienceWorth))
            writer.WriteInt32(obj.ExperienceWorth);
        if (Bit(ObjectField.NpcExperiencePool))
            writer.WriteInt32(obj.ExperiencePool);
        if (Bit(ObjectField.NpcWaypointsIdx))
            ObjectSerializationHelpers.WriteLocationArray(ref writer, obj.Waypoints);
        if (Bit(ObjectField.NpcWaypointCurrent))
            writer.WriteInt32(obj.WaypointCurrent);
        if (Bit(ObjectField.NpcStandpointDay))
            ObjectSerializationHelpers.WriteLocation(ref writer, obj.StandpointDay);
        if (Bit(ObjectField.NpcStandpointNight))
            ObjectSerializationHelpers.WriteLocation(ref writer, obj.StandpointNight);
        if (Bit(ObjectField.NpcOrigin))
            writer.WriteInt32(obj.Origin);
        if (Bit(ObjectField.NpcFaction))
            writer.WriteInt32(obj.Faction);
        if (Bit(ObjectField.NpcRetailPriceMultiplier))
            writer.WriteInt32(obj.RetailPriceMultiplier);
        if (Bit(ObjectField.NpcSubstituteInventory))
            obj.SubstituteInventory.Write(ref writer);
        if (Bit(ObjectField.NpcReactionBase))
            writer.WriteInt32(obj.ReactionBase);
        if (Bit(ObjectField.NpcSocialClass))
            writer.WriteInt32(obj.SocialClass);
        if (Bit(ObjectField.NpcReactionPcIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReactionPc);
        if (Bit(ObjectField.NpcReactionLevelIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReactionLevel);
        if (Bit(ObjectField.NpcReactionTimeIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReactionTime);
        if (Bit(ObjectField.NpcWait))
            writer.WriteInt32(obj.Wait);
        if (Bit(ObjectField.NpcGeneratorData))
            writer.WriteInt32(obj.GeneratorData);
        if (Bit(ObjectField.NpcPadI1))
            writer.WriteInt32(obj.NpcPadI1Reserved);
        if (Bit(ObjectField.NpcDamageIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Damage);
        if (Bit(ObjectField.NpcHostileListIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.HostileList);
    }
}
