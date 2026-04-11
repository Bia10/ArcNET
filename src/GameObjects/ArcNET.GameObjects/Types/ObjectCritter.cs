using ArcNET.Core;
using ArcNET.Core.Primitives;

namespace ArcNET.GameObjects.Types;

public class ObjectCritter : ObjectCommon
{
    public int CritterFlags { get; internal set; }
    public int CritterFlags2 { get; internal set; }
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
    public GameObjectGuid CritterGold { get; internal set; }
    public GameObjectGuid CritterArrows { get; internal set; }
    public GameObjectGuid CritterBullets { get; internal set; }
    public GameObjectGuid CritterPowerCells { get; internal set; }
    public GameObjectGuid CritterFuel { get; internal set; }
    public int CritterInventoryNum { get; internal set; }
    public GameObjectGuid[] CritterInventoryList { get; internal set; } = [];
    public int CritterInventorySource { get; internal set; }
    public int CritterDescriptionUnknown { get; internal set; }
    public GameObjectGuid[] CritterFollowers { get; internal set; } = [];
    public Location CritterTeleportDest { get; internal set; }
    public int CritterTeleportMap { get; internal set; }
    public int CritterDeathTime { get; internal set; }
    public int CritterAutoLevelScheme { get; internal set; }
    public int CritterPadI1 { get; internal set; }
    public int CritterPadI2 { get; internal set; }
    public int CritterPadI3 { get; internal set; }
    public int CritterPadIas1 { get; internal set; }
    public long CritterPadI64As1 { get; internal set; }

    internal static ObjectCritter Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectCritter();
        obj.ReadCommonFields(ref reader, bitmap, isPrototype);
        obj.ReadCritterFields(ref reader, bitmap, isPrototype);
        return obj;
    }

    internal void Write(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        WriteCommonFields(ref writer, bitmap, isPrototype);
        WriteCritterFields(ref writer, bitmap, isPrototype);
    }

    protected void ReadCritterFields(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFCritterFlags))
            CritterFlags = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFlags2))
            CritterFlags2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterStatBaseIdx))
            CritterStatBase = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterBasicSkillIdx))
            CritterBasicSkill = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterTechSkillIdx))
            CritterTechSkill = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterSpellTechIdx))
            CritterSpellTech = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterFatiguePts))
            CritterFatiguePts = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFatigueAdj))
            CritterFatigueAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFatigueDamage))
            CritterFatigueDamage = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterCritHitChart))
            CritterCritHitChart = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterEffectsIdx))
            CritterEffects = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterEffectCauseIdx))
            CritterEffectCause = ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterFleeingFrom))
            CritterFleeingFrom = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterPortrait))
            CritterPortrait = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterGold))
            CritterGold = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterArrows))
            CritterArrows = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterBullets))
            CritterBullets = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterPowerCells))
            CritterPowerCells = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterFuel))
            CritterFuel = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterInventoryNum))
            CritterInventoryNum = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterInventoryListIdx))
            CritterInventoryList = ReadGuidArray(ref reader, CritterInventoryNum);
        if (Bit(ObjectField.ObjFCritterInventorySource))
            CritterInventorySource = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterDescriptionUnknown))
            CritterDescriptionUnknown = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFollowerIdx))
            CritterFollowers = ReadGuidArray(ref reader, reader.ReadInt32());
        if (Bit(ObjectField.ObjFCritterTeleportDest))
            CritterTeleportDest = reader.ReadLocation();
        if (Bit(ObjectField.ObjFCritterTeleportMap))
            CritterTeleportMap = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterDeathTime))
            CritterDeathTime = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterAutoLevelScheme))
            CritterAutoLevelScheme = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI1))
            CritterPadI1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI2))
            CritterPadI2 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI3))
            CritterPadI3 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadIas1))
            CritterPadIas1 = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI64As1))
            CritterPadI64As1 = reader.ReadInt64();
    }

    private static GameObjectGuid[] ReadGuidArray(ref SpanReader reader, int count)
    {
        if (count == 0)
            return [];
        var result = new GameObjectGuid[count];
        for (var i = 0; i < count; i++)
            result[i] = reader.ReadGameObjectGuid();
        return result;
    }

    protected void WriteCritterFields(ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ((bitmap[(int)f >> 3] & (1 << ((int)f & 7))) != 0) || isPrototype;

        if (Bit(ObjectField.ObjFCritterFlags))
            writer.WriteInt32(CritterFlags);
        if (Bit(ObjectField.ObjFCritterFlags2))
            writer.WriteInt32(CritterFlags2);
        if (Bit(ObjectField.ObjFCritterStatBaseIdx))
            WriteIndexedInts(ref writer, CritterStatBase);
        if (Bit(ObjectField.ObjFCritterBasicSkillIdx))
            WriteIndexedInts(ref writer, CritterBasicSkill);
        if (Bit(ObjectField.ObjFCritterTechSkillIdx))
            WriteIndexedInts(ref writer, CritterTechSkill);
        if (Bit(ObjectField.ObjFCritterSpellTechIdx))
            WriteIndexedInts(ref writer, CritterSpellTech);
        if (Bit(ObjectField.ObjFCritterFatiguePts))
            writer.WriteInt32(CritterFatiguePts);
        if (Bit(ObjectField.ObjFCritterFatigueAdj))
            writer.WriteInt32(CritterFatigueAdj);
        if (Bit(ObjectField.ObjFCritterFatigueDamage))
            writer.WriteInt32(CritterFatigueDamage);
        if (Bit(ObjectField.ObjFCritterCritHitChart))
            writer.WriteInt32(CritterCritHitChart);
        if (Bit(ObjectField.ObjFCritterEffectsIdx))
            WriteIndexedInts(ref writer, CritterEffects);
        if (Bit(ObjectField.ObjFCritterEffectCauseIdx))
            WriteIndexedInts(ref writer, CritterEffectCause);
        if (Bit(ObjectField.ObjFCritterFleeingFrom))
            CritterFleeingFrom.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterPortrait))
            writer.WriteInt32(CritterPortrait);
        if (Bit(ObjectField.ObjFCritterGold))
            CritterGold.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterArrows))
            CritterArrows.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterBullets))
            CritterBullets.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterPowerCells))
            CritterPowerCells.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterFuel))
            CritterFuel.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterInventoryNum))
            writer.WriteInt32(CritterInventoryNum);
        if (Bit(ObjectField.ObjFCritterInventoryListIdx))
            WriteGuidArray(ref writer, CritterInventoryList);
        if (Bit(ObjectField.ObjFCritterInventorySource))
            writer.WriteInt32(CritterInventorySource);
        if (Bit(ObjectField.ObjFCritterDescriptionUnknown))
            writer.WriteInt32(CritterDescriptionUnknown);
        if (Bit(ObjectField.ObjFCritterFollowerIdx))
        {
            writer.WriteInt32(CritterFollowers.Length);
            WriteGuidArray(ref writer, CritterFollowers);
        }
        if (Bit(ObjectField.ObjFCritterTeleportDest))
            CritterTeleportDest.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterTeleportMap))
            writer.WriteInt32(CritterTeleportMap);
        if (Bit(ObjectField.ObjFCritterDeathTime))
            writer.WriteInt32(CritterDeathTime);
        if (Bit(ObjectField.ObjFCritterAutoLevelScheme))
            writer.WriteInt32(CritterAutoLevelScheme);
        if (Bit(ObjectField.ObjFCritterPadI1))
            writer.WriteInt32(CritterPadI1);
        if (Bit(ObjectField.ObjFCritterPadI2))
            writer.WriteInt32(CritterPadI2);
        if (Bit(ObjectField.ObjFCritterPadI3))
            writer.WriteInt32(CritterPadI3);
        if (Bit(ObjectField.ObjFCritterPadIas1))
            writer.WriteInt32(CritterPadIas1);
        if (Bit(ObjectField.ObjFCritterPadI64As1))
            writer.WriteInt64(CritterPadI64As1);
    }

    internal static void WriteGuidArray(ref SpanWriter writer, GameObjectGuid[] guids)
    {
        foreach (var g in guids)
            g.Write(ref writer);
    }
}
