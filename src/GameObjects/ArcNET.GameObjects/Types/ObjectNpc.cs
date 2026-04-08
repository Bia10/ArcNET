using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public sealed class ObjectNpc : ObjectCritter
{
    public int NpcFlags { get; internal set; }
    public GameObjectGuid NpcLeader { get; internal set; }
    public int NpcAiData { get; internal set; }
    public GameObjectGuid NpcCombatFocus { get; internal set; }
    public GameObjectGuid NpcWhoHitMeLast { get; internal set; }
    public int NpcExperienceWorth { get; internal set; }
    public int NpcExperiencePool { get; internal set; }
    public Location[] NpcWaypoints { get; internal set; } = [];
    public int NpcWaypointCurrent { get; internal set; }
    public Location NpcStandpointDay { get; internal set; }
    public Location NpcStandpointNight { get; internal set; }
    public int NpcOrigin { get; internal set; }
    public int NpcFaction { get; internal set; }
    public int NpcRetailPriceMultiplier { get; internal set; }
    public GameObjectGuid NpcSubstituteInventory { get; internal set; }
    public int NpcReactionBase { get; internal set; }
    public int NpcSocialClass { get; internal set; }
    public int[] NpcReactionPc { get; internal set; } = [];
    public int[] NpcReactionLevel { get; internal set; } = [];
    public int[] NpcReactionTime { get; internal set; } = [];
    public int NpcWait { get; internal set; }
    public int NpcGeneratorData { get; internal set; }
    public int NpcPadI1 { get; internal set; }
    public int[] NpcDamage { get; internal set; } = [];
    public int[] NpcShitList { get; internal set; } = [];

    internal static new ObjectNpc Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectNpc();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadCritterFields(ref reader, bitmap, isPrototype);
        obj.ReadNpcFields(ref reader, bitmap, isPrototype);
        return obj;
    }

    internal new void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteCritterFields(ref writer, bitmap, isPrototype);
        WriteNpcFields(ref writer, bitmap, isPrototype);
    }

    private void WriteNpcFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFNpcFlags))
            writer.WriteInt32(NpcFlags);
        if (Bit(ObjectField.ObjFNpcLeader))
            NpcLeader.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcAiData))
            writer.WriteInt32(NpcAiData);
        if (Bit(ObjectField.ObjFNpcCombatFocus))
            NpcCombatFocus.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcWhoHitMeLast))
            NpcWhoHitMeLast.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcExperienceWorth))
            writer.WriteInt32(NpcExperienceWorth);
        if (Bit(ObjectField.ObjFNpcExperiencePool))
            writer.WriteInt32(NpcExperiencePool);
        if (Bit(ObjectField.ObjFNpcWaypointsIdx))
            WriteLocationArray(ref writer, NpcWaypoints);
        if (Bit(ObjectField.ObjFNpcWaypointCurrent))
            writer.WriteInt32(NpcWaypointCurrent);
        if (Bit(ObjectField.ObjFNpcStandpointDay))
            NpcStandpointDay.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcStandpointNight))
            NpcStandpointNight.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcOrigin))
            writer.WriteInt32(NpcOrigin);
        if (Bit(ObjectField.ObjFNpcFaction))
            writer.WriteInt32(NpcFaction);
        if (Bit(ObjectField.ObjFNpcRetailPriceMultiplier))
            writer.WriteInt32(NpcRetailPriceMultiplier);
        if (Bit(ObjectField.ObjFNpcSubstituteInventory))
            NpcSubstituteInventory.Write(ref writer);
        if (Bit(ObjectField.ObjFNpcReactionBase))
            writer.WriteInt32(NpcReactionBase);
        if (Bit(ObjectField.ObjFNpcSocialClass))
            writer.WriteInt32(NpcSocialClass);
        if (Bit(ObjectField.ObjFNpcReactionPcIdx))
            WriteIndexedInts(ref writer, NpcReactionPc);
        if (Bit(ObjectField.ObjFNpcReactionLevelIdx))
            WriteIndexedInts(ref writer, NpcReactionLevel);
        if (Bit(ObjectField.ObjFNpcReactionTimeIdx))
            WriteIndexedInts(ref writer, NpcReactionTime);
        if (Bit(ObjectField.ObjFNpcWait))
            writer.WriteInt32(NpcWait);
        if (Bit(ObjectField.ObjFNpcGeneratorData))
            writer.WriteInt32(NpcGeneratorData);
        if (Bit(ObjectField.ObjFNpcPadI1))
            writer.WriteInt32(NpcPadI1);
        if (Bit(ObjectField.ObjFNpcDamageIdx))
            WriteIndexedInts(ref writer, NpcDamage);
        if (Bit(ObjectField.ObjFNpcShitListIdx))
            WriteIndexedInts(ref writer, NpcShitList);
    }

    private void ReadNpcFields(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFNpcFlags))
            NpcFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcLeader))
            NpcLeader = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcAiData))
            NpcAiData = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcCombatFocus))
            NpcCombatFocus = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcWhoHitMeLast))
            NpcWhoHitMeLast = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcExperienceWorth))
            NpcExperienceWorth = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcExperiencePool))
            NpcExperiencePool = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcWaypointsIdx))
            NpcWaypoints = ReadLocationArray(ref reader);
        if (Bit(ObjectField.ObjFNpcWaypointCurrent))
            NpcWaypointCurrent = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcStandpointDay))
            NpcStandpointDay = reader.ReadLocation();
        if (Bit(ObjectField.ObjFNpcStandpointNight))
            NpcStandpointNight = reader.ReadLocation();
        if (Bit(ObjectField.ObjFNpcOrigin))
            NpcOrigin = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcFaction))
            NpcFaction = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcRetailPriceMultiplier))
            NpcRetailPriceMultiplier = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcSubstituteInventory))
            NpcSubstituteInventory = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFNpcReactionBase))
            NpcReactionBase = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcSocialClass))
            NpcSocialClass = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcReactionPcIdx))
            NpcReactionPc = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcReactionLevelIdx))
            NpcReactionLevel = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcReactionTimeIdx))
            NpcReactionTime = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcWait))
            NpcWait = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcGeneratorData))
            NpcGeneratorData = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcPadI1))
            NpcPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFNpcDamageIdx))
            NpcDamage = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFNpcShitListIdx))
            NpcShitList = ReadIndexedInts(ref reader);
    }

    private static Location[] ReadLocationArray(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        if (count == 0)
            return [];
        var result = new Location[count];
        for (var i = 0; i < count; i++)
            result[i] = reader.ReadLocation();
        return result;
    }

    private static void WriteLocationArray(ref SpanWriter writer, Location[] locs)
    {
        writer.WriteInt32(locs.Length);
        foreach (var l in locs)
            l.Write(ref writer);
    }
}
