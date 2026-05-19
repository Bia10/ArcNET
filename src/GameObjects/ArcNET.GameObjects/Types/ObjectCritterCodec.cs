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

        if (Bit(ObjectField.CritterFlags))
            obj.CritterFlags = unchecked((CritterFlags)(uint)reader.ReadInt32());
        if (Bit(ObjectField.CritterFlags2))
            obj.CritterFlags2 = unchecked((CritterFlags2)(uint)reader.ReadInt32());
        if (Bit(ObjectField.CritterStatBaseIdx))
            obj.CritterStatBase = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.CritterBasicSkillIdx))
            obj.CritterBasicSkill = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.CritterTechSkillIdx))
            obj.CritterTechSkill = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.CritterSpellTechIdx))
            obj.CritterSpellTech = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.CritterFatiguePts))
            obj.CritterFatiguePts = reader.ReadInt32();
        if (Bit(ObjectField.CritterFatigueAdj))
            obj.CritterFatigueAdj = reader.ReadInt32();
        if (Bit(ObjectField.CritterFatigueDamage))
            obj.CritterFatigueDamage = reader.ReadInt32();
        if (Bit(ObjectField.CritterCritHitChart))
            obj.CritterCritHitChart = reader.ReadInt32();
        if (Bit(ObjectField.CritterEffectsIdx))
            obj.CritterEffects = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.CritterEffectCauseIdx))
            obj.CritterEffectCause = ObjectCommon.ReadIndexedInts(ref reader);
        if (Bit(ObjectField.CritterFleeingFrom))
            obj.CritterFleeingFrom = reader.ReadGameObjectGuid();
        if (Bit(ObjectField.CritterPortrait))
            obj.CritterPortrait = reader.ReadInt32();
        if (Bit(ObjectField.CritterGold))
            obj.CritterGold = reader.ReadInt32();
        if (Bit(ObjectField.CritterArrows))
            obj.CritterArrows = reader.ReadInt32();
        if (Bit(ObjectField.CritterBullets))
            obj.CritterBullets = reader.ReadInt32();
        if (Bit(ObjectField.CritterPowerCells))
            obj.CritterPowerCells = reader.ReadInt32();
        if (Bit(ObjectField.CritterFuel))
            obj.CritterFuel = reader.ReadInt32();

        var inventory = ObjectInventoryGuidListCodec.Read(
            ref reader,
            Bit(ObjectField.CritterInventoryNum),
            Bit(ObjectField.CritterInventoryListIdx),
            obj.InventoryCountReserved,
            obj.CritterInventoryList,
            "Critter"
        );
        obj.InventoryCountReserved = inventory.ReservedCount;
        obj.CritterInventoryList = inventory.Values;

        if (Bit(ObjectField.CritterInventorySource))
            obj.CritterInventorySource = reader.ReadInt32();
        if (Bit(ObjectField.CritterDescriptionUnknown))
            obj.CritterDescriptionUnknown = reader.ReadInt32();
        if (Bit(ObjectField.CritterFollowerIdx))
            obj.CritterFollowers = ObjectSerializationHelpers.ReadGuidArray(ref reader);
        if (Bit(ObjectField.CritterTeleportDest))
            obj.CritterTeleportDest = ObjectSerializationHelpers.ReadLocation(ref reader);
        if (Bit(ObjectField.CritterTeleportMap))
            obj.CritterTeleportMap = reader.ReadInt32();
        if (Bit(ObjectField.CritterDeathTime))
            obj.CritterDeathTime = ObjectSerializationHelpers.ReadPresencePrefixedInt64(ref reader);
        if (Bit(ObjectField.CritterAutoLevelScheme))
            obj.CritterAutoLevelScheme = reader.ReadInt32();
        if (Bit(ObjectField.CritterPadI1))
            obj.CritterPadI1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.CritterPadI2))
            obj.CritterPadI2Reserved = reader.ReadInt32();
        if (Bit(ObjectField.CritterPadI3))
            obj.CritterPadI3Reserved = reader.ReadInt32();
        if (Bit(ObjectField.CritterPadIas1))
            obj.CritterPadIas1Reserved = reader.ReadInt32();
        if (Bit(ObjectField.CritterPadI64As1))
            obj.CritterPadI64As1Reserved = reader.ReadInt64();
    }

    public static void WriteFields(ObjectCritter obj, ref SpanWriter writer, byte[] bitmap, bool isPrototype)
    {
        bool Bit(ObjectField f) => ObjectBitmap.IsFieldPresent(bitmap, f, isPrototype);
        var hasInventoryCount = Bit(ObjectField.CritterInventoryNum);
        var hasInventoryList = Bit(ObjectField.CritterInventoryListIdx);

        if (Bit(ObjectField.CritterFlags))
            writer.WriteInt32(unchecked((int)obj.CritterFlags));
        if (Bit(ObjectField.CritterFlags2))
            writer.WriteInt32(unchecked((int)obj.CritterFlags2));
        if (Bit(ObjectField.CritterStatBaseIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterStatBase);
        if (Bit(ObjectField.CritterBasicSkillIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterBasicSkill);
        if (Bit(ObjectField.CritterTechSkillIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterTechSkill);
        if (Bit(ObjectField.CritterSpellTechIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterSpellTech);
        if (Bit(ObjectField.CritterFatiguePts))
            writer.WriteInt32(obj.CritterFatiguePts);
        if (Bit(ObjectField.CritterFatigueAdj))
            writer.WriteInt32(obj.CritterFatigueAdj);
        if (Bit(ObjectField.CritterFatigueDamage))
            writer.WriteInt32(obj.CritterFatigueDamage);
        if (Bit(ObjectField.CritterCritHitChart))
            writer.WriteInt32(obj.CritterCritHitChart);
        if (Bit(ObjectField.CritterEffectsIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterEffects);
        if (Bit(ObjectField.CritterEffectCauseIdx))
            ObjectCommon.WriteIndexedInts(ref writer, obj.CritterEffectCause);
        if (Bit(ObjectField.CritterFleeingFrom))
            obj.CritterFleeingFrom.Write(ref writer);
        if (Bit(ObjectField.CritterPortrait))
            writer.WriteInt32(obj.CritterPortrait);
        if (Bit(ObjectField.CritterGold))
            writer.WriteInt32(obj.CritterGold);
        if (Bit(ObjectField.CritterArrows))
            writer.WriteInt32(obj.CritterArrows);
        if (Bit(ObjectField.CritterBullets))
            writer.WriteInt32(obj.CritterBullets);
        if (Bit(ObjectField.CritterPowerCells))
            writer.WriteInt32(obj.CritterPowerCells);
        if (Bit(ObjectField.CritterFuel))
            writer.WriteInt32(obj.CritterFuel);

        ObjectInventoryGuidListCodec.Write(
            ref writer,
            hasInventoryCount,
            hasInventoryList,
            obj.InventoryCountReserved,
            obj.CritterInventoryList,
            "Critter"
        );

        if (Bit(ObjectField.CritterInventorySource))
            writer.WriteInt32(obj.CritterInventorySource);
        if (Bit(ObjectField.CritterDescriptionUnknown))
            writer.WriteInt32(obj.CritterDescriptionUnknown);
        if (Bit(ObjectField.CritterFollowerIdx))
            ObjectSerializationHelpers.WriteGuidArray(ref writer, obj.CritterFollowers);
        if (Bit(ObjectField.CritterTeleportDest))
            ObjectSerializationHelpers.WriteLocation(ref writer, obj.CritterTeleportDest);
        if (Bit(ObjectField.CritterTeleportMap))
            writer.WriteInt32(obj.CritterTeleportMap);
        if (Bit(ObjectField.CritterDeathTime))
            ObjectSerializationHelpers.WritePresencePrefixedInt64(ref writer, obj.CritterDeathTime);
        if (Bit(ObjectField.CritterAutoLevelScheme))
            writer.WriteInt32(obj.CritterAutoLevelScheme);
        if (Bit(ObjectField.CritterPadI1))
            writer.WriteInt32(obj.CritterPadI1Reserved);
        if (Bit(ObjectField.CritterPadI2))
            writer.WriteInt32(obj.CritterPadI2Reserved);
        if (Bit(ObjectField.CritterPadI3))
            writer.WriteInt32(obj.CritterPadI3Reserved);
        if (Bit(ObjectField.CritterPadIas1))
            writer.WriteInt32(obj.CritterPadIas1Reserved);
        if (Bit(ObjectField.CritterPadI64As1))
            writer.WriteInt64(obj.CritterPadI64As1Reserved);
    }
}
