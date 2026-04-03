using System.Collections.Frozen;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

// ─── Wire-type registry ────────────────────────────────────────────────────────

/// <summary>
/// Wire-type codes used when dispatching field reads in MOB / PRO files.
/// Corresponds to the <c>OD_TYPE_*</c> constants used for field dispatch.
/// </summary>
internal enum ObjectWireType
{
    Int32,
    Int64,
    Float,
    String, // int32 length prefix + ASCII bytes
    Int32Array, // SAR block — 4-byte elements
    UInt32Array, // SAR block — 4-byte elements
    Int64Array, // SAR block — 8-byte elements
    HandleArray, // SAR block — 8-byte int64 object handles
    ScriptArray, // SAR block — 12-byte Script elements
    QuestArray, // SAR block — quest elements (element size from SAR header)
}

/// <summary>
/// Stores a serialized object field: its bit-index identity and the raw bytes on disk.
/// Use the per-field typed accessor extensions once defined; for now all data is opaque.
/// </summary>
public sealed class ObjectProperty
{
    /// <summary>Field identity (bit index in the header bitmap).</summary>
    public required ObjectField Field { get; init; }

    /// <summary>
    /// Raw bytes as read from disk, in full wire representation
    /// (including SAR headers for array types).
    /// </summary>
    public required byte[] RawBytes { get; init; }
}

// ─── Dispatch tables ───────────────────────────────────────────────────────────

/// <summary>
/// Internal property I/O helpers shared by <see cref="MobFormat"/> and <see cref="ProtoFormat"/>.
/// Wire-type tables are cross-referenced from the engine's
/// <c>object_fields[]</c> and the guide's documented partial table in section 3.2.2.
/// </summary>
internal static class ObjectPropertyIo
{
    // ── Common fields (bit indices 0–63, same for all ObjectTypes) ────────
    // Source: object_fields[] + implementation guide §3.2.2.
    // Fields not present in this table throw NotSupportedException at read time.
    private static readonly FrozenDictionary<int, ObjectWireType> s_commonWireType = new Dictionary<int, ObjectWireType>
    {
        [0] = ObjectWireType.Int32, // ObjFCurrentAid
        [1] = ObjectWireType.Int64, // ObjFLocation — LOCATION_MAKE(x,y)
        [2] = ObjectWireType.Float, // ObjFOffsetX
        [3] = ObjectWireType.Float, // ObjFOffsetY
        [4] = ObjectWireType.Int32, // ObjFShadow
        [5] = ObjectWireType.Int32, // ObjFOverlayFore
        [6] = ObjectWireType.Int32, // ObjFOverlayBack
        [7] = ObjectWireType.Int32, // ObjFUnderlay
        [8] = ObjectWireType.Int32, // ObjFBlitFlags
        [9] = ObjectWireType.Int32, // ObjFBlitColor
        [10] = ObjectWireType.Int32, // ObjFBlitAlpha
        [11] = ObjectWireType.Int32, // ObjFBlitScale
        [12] = ObjectWireType.Int32, // ObjFLightFlags
        [13] = ObjectWireType.Int32, // ObjFLightAid
        [14] = ObjectWireType.Int32, // ObjFLightColor
        [15] = ObjectWireType.Int32, // ObjFOverlayLightFlags
        [16] = ObjectWireType.Int32, // ObjFOverlayLightAid
        [17] = ObjectWireType.Int32, // ObjFOverlayLightColor
        [18] = ObjectWireType.Int32, // ObjFFlags
        [19] = ObjectWireType.Int32, // ObjFSpellFlags
        [20] = ObjectWireType.Int32, // ObjFBlockingMask
        [21] = ObjectWireType.Int32, // ObjFName (MES string ID)
        [22] = ObjectWireType.Int32, // ObjFDescription (MES string ID)
        [23] = ObjectWireType.Int32, // ObjFAid
        [24] = ObjectWireType.Int32, // ObjFDestroyedAid
        [25] = ObjectWireType.Int32, // ObjFAc
        [26] = ObjectWireType.Int32, // ObjFHpPts
        [27] = ObjectWireType.Int32, // ObjFHpAdj
        [28] = ObjectWireType.Int32, // ObjFHpDamage
        [29] = ObjectWireType.Int32, // ObjFMaterial
        [30] = ObjectWireType.Int32, // ObjFResistanceIdx
        [31] = ObjectWireType.ScriptArray, // ObjFScriptsIdx
        [32] = ObjectWireType.Int32, // ObjFSoundEffect
        [33] = ObjectWireType.Int32, // ObjFCategory
        // Bits 34–40 from implementation guide §3.2.2 and object_fields[]:
        [34] = ObjectWireType.Float, // ObjFPadIas1 / ObjFRotation (radians)
        [35] = ObjectWireType.Int64, // ObjFPadI64As1
        [36] = ObjectWireType.Float, // ObjFSpeedRun
        [37] = ObjectWireType.Float, // ObjFSpeedWalk
        [38] = ObjectWireType.Float, // ObjFPadFloat1
        [39] = ObjectWireType.Float, // ObjFRadius
        [40] = ObjectWireType.Float, // ObjFHeight
    }.ToFrozenDictionary();

    // ── Type-specific fields (bit indices 64+, keyed on ObjectType) ───────
    // Naming convention in ObjectField enum:
    //   I = int32, I64 = int64, Ias = int32 SAR, I64as = int64 SAR
    private static ObjectWireType? TypeSpecificWireType(ObjectType objectType, int bit) =>
        objectType switch
        {
            ObjectType.Wall => bit switch
            {
                64 => ObjectWireType.Int32, // WallFlags
                65 => ObjectWireType.Int32, // WallPadI1
                66 => ObjectWireType.Int32, // WallPadI2
                67 => ObjectWireType.Int32Array, // WallPadIas1
                68 => ObjectWireType.Int64Array, // WallPadI64As1
                _ => null,
            },

            ObjectType.Portal => bit switch
            {
                64 => ObjectWireType.Int32, // PortalFlags
                65 => ObjectWireType.Int32, // PortalLockDifficulty
                66 => ObjectWireType.Int32, // PortalKeyId
                67 => ObjectWireType.Int32, // PortalNotifyNpc
                68 => ObjectWireType.Int32, // PortalPadI1
                69 => ObjectWireType.Int32, // PortalPadI2
                70 => ObjectWireType.Int32Array, // PortalPadIas1
                71 => ObjectWireType.Int64Array, // PortalPadI64As1
                _ => null,
            },

            ObjectType.Container => bit switch
            {
                64 => ObjectWireType.Int32, // ContainerFlags
                65 => ObjectWireType.Int32, // ContainerLockDifficulty
                66 => ObjectWireType.Int32, // ContainerKeyId
                67 => ObjectWireType.Int32, // ContainerInventoryNum           (OD_TYPE_INT32)
                68 => ObjectWireType.HandleArray, // ContainerInventoryListIdx (OD_TYPE_HANDLE_ARRAY — SAR of 24-byte ObjectIDs)
                69 => ObjectWireType.Int32, // ContainerInventorySource
                70 => ObjectWireType.Int32, // ContainerNotifyNpc
                71 => ObjectWireType.Int32, // ContainerPadI1
                72 => ObjectWireType.Int32, // ContainerPadI2
                73 => ObjectWireType.Int32Array, // ContainerPadIas1
                74 => ObjectWireType.Int64Array, // ContainerPadI64As1
                _ => null,
            },

            ObjectType.Scenery => bit switch
            {
                64 => ObjectWireType.Int32, // SceneryFlags
                65 => ObjectWireType.Int32, // SceneryWhosInMe
                66 => ObjectWireType.Int32, // SceneryRespawnDelay
                67 => ObjectWireType.Int32, // SceneryPadI2
                68 => ObjectWireType.Int32Array, // SceneryPadIas1
                69 => ObjectWireType.Int64Array, // SceneryPadI64As1
                _ => null,
            },

            ObjectType.Trap => bit switch
            {
                64 => ObjectWireType.Int32, // TrapFlags
                65 => ObjectWireType.Int32, // TrapDifficulty
                66 => ObjectWireType.Int32, // TrapPadI2
                67 => ObjectWireType.Int32Array, // TrapPadIas1
                68 => ObjectWireType.Int64Array, // TrapPadI64As1
                _ => null,
            },

            // Item subtype fields (bits 64–86).
            // All item subtypes (Weapon, Ammo, Armor, Gold, Food, Scroll, Key, KeyRing, Written, Generic)
            // share the item base block at 64–86, then add further type-specific fields at 96+.
            ObjectType.Weapon => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // WeaponFlags
                97 => ObjectWireType.Int32, // WeaponPaperDollAid
                98 => ObjectWireType.Int32, // WeaponBonusToHit
                99 => ObjectWireType.Int32, // WeaponMagicHitAdj
                100 => ObjectWireType.Int32, // WeaponDamageLowerIdx
                101 => ObjectWireType.Int32, // WeaponDamageUpperIdx
                102 => ObjectWireType.Int32, // WeaponMagicDamageAdjIdx
                103 => ObjectWireType.Int32, // WeaponSpeedFactor
                104 => ObjectWireType.Int32, // WeaponMagicSpeedAdj
                105 => ObjectWireType.Int32, // WeaponRange
                106 => ObjectWireType.Int32, // WeaponMagicRangeAdj
                107 => ObjectWireType.Int32, // WeaponMinStrength
                108 => ObjectWireType.Int32, // WeaponMagicMinStrengthAdj
                109 => ObjectWireType.Int32, // WeaponAmmoType
                110 => ObjectWireType.Int32, // WeaponAmmoConsumption
                111 => ObjectWireType.Int32, // WeaponMissileAid
                112 => ObjectWireType.Int32, // WeaponVisualEffectAid
                113 => ObjectWireType.Int32, // WeaponCritHitChart
                114 => ObjectWireType.Int32, // WeaponMagicCritHitChance
                115 => ObjectWireType.Int32, // WeaponMagicCritHitEffect
                116 => ObjectWireType.Int32, // WeaponCritMissChart
                117 => ObjectWireType.Int32, // WeaponMagicCritMissChance
                118 => ObjectWireType.Int32, // WeaponMagicCritMissEffect
                119 => ObjectWireType.Int32, // WeaponPadI1
                120 => ObjectWireType.Int32, // WeaponPadI2
                121 => ObjectWireType.Int32Array, // WeaponPadIas1
                122 => ObjectWireType.Int64Array, // WeaponPadI64As1
                _ => null,
            },

            ObjectType.Ammo => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // AmmoFlags
                97 => ObjectWireType.Int32, // AmmoQuantity
                98 => ObjectWireType.Int32, // AmmoType
                99 => ObjectWireType.Int32, // AmmoPadI1
                100 => ObjectWireType.Int32, // AmmoPadI2
                101 => ObjectWireType.Int32Array, // AmmoPadIas1
                102 => ObjectWireType.Int64Array, // AmmoPadI64As1
                _ => null,
            },

            ObjectType.Armor => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // ArmorFlags
                97 => ObjectWireType.Int32, // ArmorPaperDollAid
                98 => ObjectWireType.Int32, // ArmorAcAdj
                99 => ObjectWireType.Int32, // ArmorMagicAcAdj
                100 => ObjectWireType.Int32, // ArmorResistanceAdjIdx
                101 => ObjectWireType.Int32, // ArmorMagicResistanceAdjIdx
                102 => ObjectWireType.Int32, // ArmorSilentMoveAdj
                103 => ObjectWireType.Int32, // ArmorMagicSilentMoveAdj
                104 => ObjectWireType.Int32, // ArmorUnarmedBonusDamage
                105 => ObjectWireType.Int32, // ArmorPadI2
                106 => ObjectWireType.Int32Array, // ArmorPadIas1
                107 => ObjectWireType.Int64Array, // ArmorPadI64As1
                _ => null,
            },

            ObjectType.Gold => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // GoldFlags
                97 => ObjectWireType.Int32, // GoldQuantity
                98 => ObjectWireType.Int32, // GoldPadI1
                99 => ObjectWireType.Int32, // GoldPadI2
                100 => ObjectWireType.Int32Array, // GoldPadIas1
                101 => ObjectWireType.Int64Array, // GoldPadI64As1
                _ => null,
            },

            ObjectType.Food => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // FoodFlags
                97 => ObjectWireType.Int32, // FoodPadI1
                98 => ObjectWireType.Int32, // FoodPadI2
                99 => ObjectWireType.Int32Array, // FoodPadIas1
                100 => ObjectWireType.Int64Array, // FoodPadI64As1
                _ => null,
            },

            ObjectType.Scroll => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // ScrollFlags
                97 => ObjectWireType.Int32, // ScrollPadI1
                98 => ObjectWireType.Int32, // ScrollPadI2
                99 => ObjectWireType.Int32Array, // ScrollPadIas1
                100 => ObjectWireType.Int64Array, // ScrollPadI64As1
                _ => null,
            },

            ObjectType.Key => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // KeyKeyId
                97 => ObjectWireType.Int32, // KeyPadI1
                98 => ObjectWireType.Int32, // KeyPadI2
                99 => ObjectWireType.Int32Array, // KeyPadIas1
                100 => ObjectWireType.Int64Array, // KeyPadI64As1
                _ => null,
            },

            ObjectType.KeyRing => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // KeyRingFlags
                97 => ObjectWireType.Int32, // KeyRingListIdx
                98 => ObjectWireType.Int32, // KeyRingPadI1
                99 => ObjectWireType.Int32, // KeyRingPadI2
                100 => ObjectWireType.Int32Array, // KeyRingPadIas1
                101 => ObjectWireType.Int64Array, // KeyRingPadI64As1
                _ => null,
            },

            ObjectType.Written => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // WrittenFlags
                97 => ObjectWireType.Int32, // WrittenSubtype
                98 => ObjectWireType.Int32, // WrittenTextStartLine
                99 => ObjectWireType.Int32, // WrittenTextEndLine
                100 => ObjectWireType.Int32, // WrittenPadI1
                101 => ObjectWireType.Int32, // WrittenPadI2
                102 => ObjectWireType.Int32Array, // WrittenPadIas1
                103 => ObjectWireType.Int64Array, // WrittenPadI64As1
                _ => null,
            },

            ObjectType.Generic => bit switch
            {
                >= 64 and <= 86 => ItemBit(bit),
                96 => ObjectWireType.Int32, // GenericFlags
                97 => ObjectWireType.Int32, // GenericUsageBonus
                98 => ObjectWireType.Int32, // GenericUsageCountRemaining
                99 => ObjectWireType.Int32Array, // GenericPadIas1
                100 => ObjectWireType.Int64Array, // GenericPadI64As1
                _ => null,
            },

            // Critter base block (bits 64–96) shared by NPC and PC; type-specific at 128–152.
            ObjectType.Pc => CritterBit(bit) ?? PcBit(bit),

            ObjectType.Npc => CritterBit(bit) ?? NpcBit(bit),

            // Projectile fields at bits 64–71 (same as other type-specific blocks).
            ObjectType.Projectile => bit switch
            {
                64 => ObjectWireType.Int32, // ObjFProjectileFlagsCombat
                65 => ObjectWireType.Int32, // ObjFProjectileFlagsCombatDamage
                66 => ObjectWireType.Int32, // ObjFProjectileHitLoc
                67 => ObjectWireType.Int64, // ObjFProjectileParentWeapon (handle)
                68 => ObjectWireType.Int32, // ObjFProjectilePadI1
                69 => ObjectWireType.Int32, // ObjFProjectilePadI2
                70 => ObjectWireType.Int32Array, // ObjFProjectilePadIas1
                71 => ObjectWireType.Int64Array, // ObjFProjectilePadI64As1
                _ => null,
            },

            _ => null,
        };

    // Critter base block (bits 64–96) — shared by both NPC and PC objects.
    private static ObjectWireType? CritterBit(int bit) =>
        bit switch
        {
            64 => ObjectWireType.Int32, // CritterFlags
            65 => ObjectWireType.Int32, // CritterFlags2
            66 => ObjectWireType.Int32Array, // CritterStatBaseIdx
            67 => ObjectWireType.Int32Array, // CritterBasicSkillIdx
            68 => ObjectWireType.Int32Array, // CritterTechSkillIdx
            69 => ObjectWireType.Int32Array, // CritterSpellTechIdx
            70 => ObjectWireType.Int32, // CritterFatiguePts
            71 => ObjectWireType.Int32, // CritterFatigueAdj
            72 => ObjectWireType.Int32, // CritterFatigueDamage
            73 => ObjectWireType.Int32, // CritterCritHitChart
            74 => ObjectWireType.Int32Array, // CritterEffectsIdx
            75 => ObjectWireType.Int32Array, // CritterEffectCauseIdx
            76 => ObjectWireType.HandleArray, // CritterFleeingFrom (handle)
            77 => ObjectWireType.Int32, // CritterPortrait
            78 => ObjectWireType.Int32, // CritterGold
            79 => ObjectWireType.Int32, // CritterArrows
            80 => ObjectWireType.Int32, // CritterBullets
            81 => ObjectWireType.Int32, // CritterPowerCells
            82 => ObjectWireType.Int32, // CritterFuel
            83 => ObjectWireType.Int32, // CritterInventoryNum                   (OD_TYPE_INT32)
            84 => ObjectWireType.HandleArray, // CritterInventoryListIdx           (OD_TYPE_HANDLE_ARRAY — SAR of 24-byte ObjectIDs)
            85 => ObjectWireType.Int32, // CritterInventorySource
            86 => ObjectWireType.Int32, // CritterDescriptionUnknown
            87 => ObjectWireType.HandleArray, // CritterFollowerIdx               (OD_TYPE_HANDLE_ARRAY — SAR of 24-byte ObjectIDs)
            88 => ObjectWireType.Int64, // CritterTeleportDest (location int64)
            89 => ObjectWireType.Int32, // CritterTeleportMap
            90 => ObjectWireType.Int64, // CritterDeathTime
            91 => ObjectWireType.Int32, // CritterAutoLevelScheme
            92 => ObjectWireType.Int32, // CritterPadI1
            93 => ObjectWireType.Int32, // CritterPadI2
            94 => ObjectWireType.Int32, // CritterPadI3
            95 => ObjectWireType.Int32Array, // CritterPadIas1
            96 => ObjectWireType.Int64Array, // CritterPadI64As1
            _ => null,
        };

    // PC-specific fields (bits 128–152).
    private static ObjectWireType? PcBit(int bit) =>
        bit switch
        {
            128 => ObjectWireType.Int32, // PcFlags
            129 => ObjectWireType.Int32, // PcFlagsFate
            130 => ObjectWireType.Int32Array, // PcReputationIdx
            131 => ObjectWireType.Int32Array, // PcReputationTsIdx
            132 => ObjectWireType.Int32, // PcBackground
            133 => ObjectWireType.String, // PcBackgroundText
            134 => ObjectWireType.Int32Array, // PcQuestIdx
            135 => ObjectWireType.Int32Array, // PcBlessingIdx
            136 => ObjectWireType.Int32Array, // PcBlessingTsIdx
            137 => ObjectWireType.Int32Array, // PcCurseIdx
            138 => ObjectWireType.Int32Array, // PcCurseTsIdx
            139 => ObjectWireType.Int32, // PcPartyId
            140 => ObjectWireType.Int32Array, // PcRumorIdx
            141 => ObjectWireType.Int32Array, // PcPadIas2
            142 => ObjectWireType.Int32Array, // PcSchematicsFoundIdx
            143 => ObjectWireType.Int32Array, // PcLogbookEgoIdx
            144 => ObjectWireType.Int32, // PcFogMask
            145 => ObjectWireType.String, // PcPlayerName
            146 => ObjectWireType.Int32, // PcBankMoney
            147 => ObjectWireType.Int32Array, // PcGlobalFlags
            148 => ObjectWireType.Int32Array, // PcGlobalVariables
            149 => ObjectWireType.Int32, // PcPadI1
            150 => ObjectWireType.Int32, // PcPadI2
            151 => ObjectWireType.Int32Array, // PcPadIas1
            152 => ObjectWireType.Int64Array, // PcPadI64As1
            _ => null,
        };

    // NPC-specific fields (bits 128–152).
    private static ObjectWireType? NpcBit(int bit) =>
        bit switch
        {
            128 => ObjectWireType.Int32, // NpcFlags
            129 => ObjectWireType.HandleArray, // NpcLeader (handle)
            130 => ObjectWireType.Int32Array, // NpcAiData
            131 => ObjectWireType.HandleArray, // NpcCombatFocus (handle)
            132 => ObjectWireType.HandleArray, // NpcWhoHitMeLast (handle)
            133 => ObjectWireType.Int32, // NpcExperienceWorth
            134 => ObjectWireType.Int32, // NpcExperiencePool
            135 => ObjectWireType.Int64Array, // NpcWaypointsIdx (locations as int64)
            136 => ObjectWireType.Int32, // NpcWaypointCurrent
            137 => ObjectWireType.Int64, // NpcStandpointDay (location int64)
            138 => ObjectWireType.Int64, // NpcStandpointNight (location int64)
            139 => ObjectWireType.Int32, // NpcOrigin
            140 => ObjectWireType.Int32, // NpcFaction
            141 => ObjectWireType.Int32, // NpcRetailPriceMultiplier
            142 => ObjectWireType.Int32, // NpcSubstituteInventory
            143 => ObjectWireType.Int32, // NpcReactionBase
            144 => ObjectWireType.Int32, // NpcSocialClass
            145 => ObjectWireType.Int32Array, // NpcReactionPcIdx
            146 => ObjectWireType.Int32Array, // NpcReactionLevelIdx
            147 => ObjectWireType.Int32Array, // NpcReactionTimeIdx
            148 => ObjectWireType.Int32, // NpcWait
            149 => ObjectWireType.Int32Array, // NpcGeneratorData
            150 => ObjectWireType.Int32, // NpcPadI1
            151 => ObjectWireType.Int32Array, // NpcDamageIdx
            152 => ObjectWireType.Int32Array, // NpcShitListIdx
            _ => null,
        };

    private static ObjectWireType? ItemBit(int bit) =>
        bit switch
        {
            64 => ObjectWireType.Int32, // ItemFlags
            65 => ObjectWireType.Int64, // ItemParent (handle)
            66 => ObjectWireType.Int32, // ItemWeight
            67 => ObjectWireType.Int32, // ItemMagicWeightAdj
            68 => ObjectWireType.Int32, // ItemWorth
            69 => ObjectWireType.Int32, // ItemManaStore
            70 => ObjectWireType.Int32, // ItemInvAid
            71 => ObjectWireType.Int32, // ItemInvLocation
            72 => ObjectWireType.Int32, // ItemUseAidFragment
            73 => ObjectWireType.Int32, // ItemMagicTechComplexity
            74 => ObjectWireType.Int32, // ItemDiscipline
            75 => ObjectWireType.Int32, // ItemDescriptionUnknown
            76 => ObjectWireType.Int32, // ItemDescriptionEffects
            77 => ObjectWireType.Int32, // ItemSpell1
            78 => ObjectWireType.Int32, // ItemSpell2
            79 => ObjectWireType.Int32, // ItemSpell3
            80 => ObjectWireType.Int32, // ItemSpell4
            81 => ObjectWireType.Int32, // ItemSpell5
            82 => ObjectWireType.Int32, // ItemSpellManaStore
            83 => ObjectWireType.Int32, // ItemAiAction
            84 => ObjectWireType.Int32, // ItemPadI1
            85 => ObjectWireType.Int32Array, // ItemPadIas1
            86 => ObjectWireType.Int64Array, // ItemPadI64As1
            _ => null,
        };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Reads all object properties present in the bitmap and returns them as an ordered list.
    /// Fields are read in bitmap bit-order (bit 0 first, then bit 1, …).
    /// </summary>
    internal static IReadOnlyList<ObjectProperty> ReadProperties(ref SpanReader reader, GameObjectHeader header)
    {
        var bitmap = header.Bitmap;
        var objectType = header.GameObjectType;
        var list = new List<ObjectProperty>();

        for (var bit = 0; bit < bitmap.Length; bit++)
        {
            if (!bitmap[bit])
                continue;

            var wireType = ResolveWireType(objectType, bit);
            var raw = ReadField(ref reader, wireType);
            list.Add(new ObjectProperty { Field = (ObjectField)bit, RawBytes = raw });
        }

        return list;
    }

    /// <summary>
    /// Writes all object properties back in bitmap bit-order.
    /// </summary>
    internal static void WriteProperties(IReadOnlyList<ObjectProperty> properties, ref SpanWriter writer)
    {
        foreach (var prop in properties)
            writer.WriteBytes(prop.RawBytes);
    }

    // ── Wire-type resolution ──────────────────────────────────────────────

    private static ObjectWireType ResolveWireType(ObjectType objectType, int bit)
    {
        if (s_commonWireType.TryGetValue(bit, out var common))
            return common;

        var specific = TypeSpecificWireType(objectType, bit);
        if (specific.HasValue)
            return specific.Value;

        throw new NotSupportedException(
            $"Unknown wire type for ObjectType={objectType}, bit={bit}. "
                + "Cross-reference object_fields[] to determine the type "
                + "and add it to ObjectPropertyIo.s_commonWireType or TypeSpecificWireType."
        );
    }

    // ── Field readers ─────────────────────────────────────────────────────

    private static byte[] ReadField(ref SpanReader reader, ObjectWireType wireType) =>
        wireType switch
        {
            ObjectWireType.Int32 or ObjectWireType.Float => reader.ReadBytes(4).ToArray(),
            ObjectWireType.Int64 => ReadPresencePrefixedField(ref reader, 8),
            ObjectWireType.String => ReadStringField(ref reader),
            ObjectWireType.Int32Array
            or ObjectWireType.UInt32Array
            or ObjectWireType.Int64Array
            or ObjectWireType.HandleArray
            or ObjectWireType.ScriptArray
            or ObjectWireType.QuestArray => ReadSarField(ref reader),
            _ => throw new ArgumentOutOfRangeException(nameof(wireType), wireType, null),
        };

    /// <summary>
    /// Reads a presence-prefixed fixed-size field (used for OD_TYPE_INT64).
    /// Wire format: uint8 presence + <paramref name="dataSize"/> bytes if present.
    /// </summary>
    private static byte[] ReadPresencePrefixedField(ref SpanReader reader, int dataSize)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return [0];

        var data = reader.ReadBytes(dataSize).ToArray();
        var raw = new byte[1 + dataSize];
        raw[0] = presence;
        data.CopyTo(raw, 1);
        return raw;
    }

    /// <summary>
    /// Reads a presence-prefixed string field (OD_TYPE_STRING).
    /// Wire format: uint8 presence + int32 length + (length+1) bytes (including NUL terminator).
    /// </summary>
    private static byte[] ReadStringField(ref SpanReader reader)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return [0];

        var lengthBytes = reader.ReadBytes(4).ToArray();
        var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);

        // The game writes strlen() as the length, then writes strlen()+1 bytes (including NUL).
        var strDataSize = length + 1;
        var strBytes = reader.ReadBytes(strDataSize).ToArray();

        var total = 1 + 4 + strDataSize;
        var raw = new byte[total];
        raw[0] = presence;
        lengthBytes.CopyTo(raw, 1);
        strBytes.CopyTo(raw, 5);
        return raw;
    }

    /// <summary>
    /// Reads a presence-prefixed SizeableArray (SAR) field.
    /// Wire format:
    ///   uint8   presence        — 0 = absent, non-zero = SA data follows
    ///   int32   sa.size         — element size in bytes  (part of SizeableArray struct)
    ///   int32   sa.count        — number of elements     (part of SizeableArray struct)
    ///   int32   sa.bitset_id    — in-memory bitset ID    (part of SizeableArray struct, ignored on read)
    ///   byte[]  data            — sa.size × sa.count bytes of element data
    ///   int32   bitset_cnt      — number of 32-bit bitset storage words
    ///   int32[] bitset_data     — bitset_cnt × 4 bytes of bitmask data
    /// </summary>
    private static byte[] ReadSarField(ref SpanReader reader)
    {
        var presence = reader.ReadByte();
        if (presence == 0)
            return [0];

        // SizeableArray header: { int32 size, int32 count, int32 bitset_id } = 12 bytes
        var elementSize = reader.ReadUInt32();
        var elementCount = reader.ReadUInt32();
        var bitsetId = reader.ReadUInt32(); // in-memory reference, preserved for round-trip

        var dataLen = (int)(elementSize * elementCount);
        var data = reader.ReadBytes(dataLen).ToArray();

        // Bitset serialization: int32 cnt + cnt × int32 storage
        var bitsetCnt = reader.ReadUInt32();
        var bitset = reader.ReadBytes((int)(bitsetCnt * 4)).ToArray();

        // Assemble raw bytes preserving the on-disk layout exactly.
        // presence(1) + SA header(12) + data + bitsetCnt(4) + bitset
        var total = 1 + 12 + dataLen + 4 + (int)(bitsetCnt * 4);
        var raw = new byte[total];
        var p = 0;
        raw[p++] = presence;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), elementSize);
        p += 4;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), elementCount);
        p += 4;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), bitsetId);
        p += 4;
        data.CopyTo(raw, p);
        p += dataLen;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(p), bitsetCnt);
        p += 4;
        bitset.CopyTo(raw, p);

        return raw;
    }
}
