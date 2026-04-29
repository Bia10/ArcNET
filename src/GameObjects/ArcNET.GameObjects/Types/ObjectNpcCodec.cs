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

        if (Bit(ObjectField.ObjFNpcFlags))
            obj.NpcFlags = unchecked((ObjFNpcFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFNpcLeader))
            obj.Leader = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcAiData))
            obj.AiData = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcCombatFocus))
            obj.CombatFocus = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcWhoHitMeLast))
            obj.WhoHitMeLast = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcExperienceWorth))
            obj.ExperienceWorth = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcExperiencePool))
            obj.ExperiencePool = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcWaypointsIdx))
            obj.Waypoints = ObjectSerializationHelpers.ReadLocationArray(ref reader);
        if (Bit(ObjectField.ObjFNpcWaypointCurrent))
            obj.WaypointCurrent = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcStandpointDay))
            obj.StandpointDay = reader.ReadLocation();
        if (Bit(ObjectField.ObjFNpcStandpointNight))
            obj.StandpointNight = reader.ReadLocation();
        if (Bit(ObjectField.ObjFNpcOrigin))
            obj.Origin = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcFaction))
            obj.Faction = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcRetailPriceMultiplier))
            obj.RetailPriceMultiplier = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcSubstituteInventory))
            obj.SubstituteInventory = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcReactionBase))
            obj.ReactionBase = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcSocialClass))
            obj.SocialClass = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcReactionPcIdx))
            obj.ReactionPc = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcReactionLevelIdx))
            obj.ReactionLevel = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcReactionTimeIdx))
            obj.ReactionTime = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcWait))
            obj.Wait = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcGeneratorData))
            obj.GeneratorData = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcPadI1))
            obj.NpcPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcDamageIdx))
            obj.Damage = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcHostileListIdx))
            obj.HostileList = ObjectCommon.ReadIndexedInts(ref reader);
    }

    public static void WriteFields(ObjectNpc obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFNpcFlags))
            writer.WriteInt32(unchecked((int)obj.NpcFlags));
        if (Bit(ObjectField.ObjFNpcLeader))
            obj.Leader.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcAiData))
            writer.WriteInt32(obj.AiData);
        if (Bit(ObjectField.ObjFNpcCombatFocus))
            obj.CombatFocus.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcWhoHitMeLast))
            obj.WhoHitMeLast.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcExperienceWorth))
            writer.WriteInt32(obj.ExperienceWorth);
        if (Bit(ObjectField.ObjFNpcExperiencePool))
            writer.WriteInt32(obj.ExperiencePool);
        if (Bit(ObjectField.ObjFNpcWaypointsIdx))
            ObjectSerializationHelpers.WriteLocationArray(ref writer, obj.Waypoints);
        if (Bit(ObjectField.ObjFNpcWaypointCurrent))
            writer.WriteInt32(obj.WaypointCurrent);
        if (Bit(ObjectField.ObjFNpcStandpointDay))
            obj.StandpointDay.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcStandpointNight))
            obj.StandpointNight.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcOrigin))
            writer.WriteInt32(obj.Origin);
        if (Bit(ObjectField.ObjFNpcFaction))
            writer.WriteInt32(obj.Faction);
        if (Bit(ObjectField.ObjFNpcRetailPriceMultiplier))
            writer.WriteInt32(obj.RetailPriceMultiplier);
        if (Bit(ObjectField.ObjFNpcSubstituteInventory))
            obj.SubstituteInventory.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcReactionBase))
            writer.WriteInt32(obj.ReactionBase);
        if (Bit(ObjectField.ObjFNpcSocialClass))
            writer.WriteInt32(obj.SocialClass);
        if (Bit(ObjectField.ObjFNpcReactionPcIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReactionPc);
        if (Bit(ObjectField.ObjFNpcReactionLevelIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReactionLevel);
        if (Bit(ObjectField.ObjFNpcReactionTimeIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.ReactionTime);
        if (Bit(ObjectField.ObjFNpcWait))
            writer.WriteInt32(obj.Wait);
        if (Bit(ObjectField.ObjFNpcGeneratorData))
            writer.WriteInt32(obj.GeneratorData);
        if (Bit(ObjectField.ObjFNpcPadI1))
            writer.WriteInt32(obj.NpcPadI1Reserved);
        if (Bit(ObjectField.ObjFNpcDamageIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.Damage);
        if (Bit(ObjectField.ObjFNpcHostileListIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.HostileList);
    }
}
