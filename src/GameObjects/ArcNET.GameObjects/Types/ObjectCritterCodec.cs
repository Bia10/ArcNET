using ArcNET.Core;

namespace ArcNET.GameObjects.Types;

internal static class ObjectCritterCodec
{
    public static ObjectCritter Read(ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        var obj = new ObjectCritter();
        ObjectCommonFieldsCodec.Read(obj, ref reader, bitmap, isPrototype);
        ReadFields(obj, ref reader, bitmap, isPrototype);
        return obj;
    }

    public static void Write(ObjectCritter obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        ObjectCommonFieldsCodec.Write(obj, ref writer, bitmap, isPrototype);
        WriteFields(obj, ref writer, bitmap, isPrototype);
    }

    public static void ReadFields(ObjectCritter obj, ref SpanReader reader, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);

        if (Bit(ObjectField.ObjFCritterFlags))
            obj.CritterFlags = unchecked((ObjFCritterFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFCritterFlags2))
            obj.CritterFlags2 = unchecked((ObjFCritterFlags2)(uint)reader.ReadInt32());
        if (Bit(ObjectField.ObjFCritterStatBaseIdx))
            obj.CritterStatBase = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterBasicSkillIdx))
            obj.CritterBasicSkill = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterTechSkillIdx))
            obj.CritterTechSkill = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterSpellTechIdx))
            obj.CritterSpellTech = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterFatiguePts))
            obj.CritterFatiguePts = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFatigueAdj))
            obj.CritterFatigueAdj = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFatigueDamage))
            obj.CritterFatigueDamage = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterCritHitChart))
            obj.CritterCritHitChart = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterEffectsIdx))
            obj.CritterEffects = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterEffectCauseIdx))
            obj.CritterEffectCause = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.ObjFCritterFleeingFrom))
            obj.CritterFleeingFrom = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.ObjFCritterPortrait))
            obj.CritterPortrait = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterGold))
            obj.CritterGold = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterArrows))
            obj.CritterArrows = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterBullets))
            obj.CritterBullets = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPowerCells))
            obj.CritterPowerCells = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFuel))
            obj.CritterFuel = reader.ReadInt32();

        var inventory = ObjectInventoryGuidListCodec.Read(
            ref reader,
            Bit(ObjectField.ObjFCritterInventoryNum),
            Bit(ObjectField.ObjFCritterInventoryListIdx),
            obj.InventoryCountReserved,
            obj.CritterInventoryList,
            "Critter"
        );
        obj.InventoryCountReserved = inventory.ReservedCount;
        obj.CritterInventoryList = inventory.Values;

        if (Bit(ObjectField.ObjFCritterInventorySource))
            obj.CritterInventorySource = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterDescriptionUnknown))
            obj.CritterDescriptionUnknown = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterFollowerIdx))
            obj.CritterFollowers = ObjectSerializationHelpers.ReadGuidArray(ref reader, reader.ReadInt32());
        if (Bit(ObjectField.ObjFCritterTeleportDest))
            obj.CritterTeleportDest = reader.ReadLocation();
        if (Bit(ObjectField.ObjFCritterTeleportMap))
            obj.CritterTeleportMap = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterDeathTime))
            obj.CritterDeathTime = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterAutoLevelScheme))
            obj.CritterAutoLevelScheme = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI1))
            obj.CritterPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI2))
            obj.CritterPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI3))
            obj.CritterPadI3Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadIas1))
            obj.CritterPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.ObjFCritterPadI64As1))
            obj.CritterPadI64As1Reserved = reader.ReadInt64();
    }

    public static void WriteFields(ObjectCritter obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        var hasInventoryCount = Bit(ObjectField.ObjFCritterInventoryNum);
        var hasInventoryList = Bit(ObjectField.ObjFCritterInventoryListIdx);

        if (Bit(ObjectField.ObjFCritterFlags))
            writer.WriteInt32(unchecked((int)obj.CritterFlags));
        if (Bit(ObjectField.ObjFCritterFlags2))
            writer.WriteInt32(unchecked((int)obj.CritterFlags2));
        if (Bit(ObjectField.ObjFCritterStatBaseIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterStatBase);
        if (Bit(ObjectField.ObjFCritterBasicSkillIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterBasicSkill);
        if (Bit(ObjectField.ObjFCritterTechSkillIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterTechSkill);
        if (Bit(ObjectField.ObjFCritterSpellTechIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterSpellTech);
        if (Bit(ObjectField.ObjFCritterFatiguePts))
            writer.WriteInt32(obj.CritterFatiguePts);
        if (Bit(ObjectField.ObjFCritterFatigueAdj))
            writer.WriteInt32(obj.CritterFatigueAdj);
        if (Bit(ObjectField.ObjFCritterFatigueDamage))
            writer.WriteInt32(obj.CritterFatigueDamage);
        if (Bit(ObjectField.ObjFCritterCritHitChart))
            writer.WriteInt32(obj.CritterCritHitChart);
        if (Bit(ObjectField.ObjFCritterEffectsIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterEffects);
        if (Bit(ObjectField.ObjFCritterEffectCauseIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterEffectCause);
        if (Bit(ObjectField.ObjFCritterFleeingFrom))
            obj.CritterFleeingFrom.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterPortrait))
            writer.WriteInt32(obj.CritterPortrait);
        if (Bit(ObjectField.ObjFCritterGold))
            writer.WriteInt32(obj.CritterGold);
        if (Bit(ObjectField.ObjFCritterArrows))
            writer.WriteInt32(obj.CritterArrows);
        if (Bit(ObjectField.ObjFCritterBullets))
            writer.WriteInt32(obj.CritterBullets);
        if (Bit(ObjectField.ObjFCritterPowerCells))
            writer.WriteInt32(obj.CritterPowerCells);
        if (Bit(ObjectField.ObjFCritterFuel))
            writer.WriteInt32(obj.CritterFuel);

        ObjectInventoryGuidListCodec.Write(
            ref writer,
            hasInventoryCount,
            hasInventoryList,
            obj.InventoryCountReserved,
            obj.CritterInventoryList,
            "Critter"
        );

        if (Bit(ObjectField.ObjFCritterInventorySource))
            writer.WriteInt32(obj.CritterInventorySource);
        if (Bit(ObjectField.ObjFCritterDescriptionUnknown))
            writer.WriteInt32(obj.CritterDescriptionUnknown);
        if (Bit(ObjectField.ObjFCritterFollowerIdx))
        {
            writer.WriteInt32(obj.CritterFollowers.Length);
            ObjectSerializationHelpers.WriteGuidArray(ref writer, obj.CritterFollowers);
        }
        if (Bit(ObjectField.ObjFCritterTeleportDest))
            obj.CritterTeleportDest.Write(ref writer);
        if (Bit(ObjectField.ObjFCritterTeleportMap))
            writer.WriteInt32(obj.CritterTeleportMap);
        if (Bit(ObjectField.ObjFCritterDeathTime))
            writer.WriteInt32(obj.CritterDeathTime);
        if (Bit(ObjectField.ObjFCritterAutoLevelScheme))
            writer.WriteInt32(obj.CritterAutoLevelScheme);
        if (Bit(ObjectField.ObjFCritterPadI1))
            writer.WriteInt32(obj.CritterPadI1Reserved);
        if (Bit(ObjectField.ObjFCritterPadI2))
            writer.WriteInt32(obj.CritterPadI2Reserved);
        if (Bit(ObjectField.ObjFCritterPadI3))
            writer.WriteInt32(obj.CritterPadI3Reserved);
        if (Bit(ObjectField.ObjFCritterPadIas1))
            writer.WriteInt32(obj.CritterPadIas1Reserved);
        if (Bit(ObjectField.ObjFCritterPadI64As1))
            writer.WriteInt64(obj.CritterPadI64As1Reserved);
    }
}
