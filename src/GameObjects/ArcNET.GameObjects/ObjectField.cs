namespace ArcNET.GameObjects;

/// <summary>
/// Maps every game-object field name to its bit index within the per-type bitmap stored in
/// <see cref="GameObjectHeader.Bitmap"/>.  Reading code checks this index before reading each
/// field: if the bit is clear (and the object is not a prototype) the field is absent.
/// </summary>
public enum ObjectField : byte
{
    // ── Common (bits 0–35) ───────────────────────────────────────────────────
    CurrentAid = 0,
    Location = 1,
    OffsetX = 2,
    OffsetY = 3,
    Shadow = 4,
    OverlayFore = 5,
    OverlayBack = 6,
    Underlay = 7,
    BlitFlags = 8,
    BlitColor = 9,
    BlitAlpha = 10,
    BlitScale = 11,
    LightFlags = 12,
    LightAid = 13,
    LightColor = 14,
    OverlayLightFlags = 15,
    OverlayLightAid = 16,
    OverlayLightColor = 17,
    ObjectFlags = 18,
    SpellFlags = 19,
    BlockingMask = 20,
    Name = 21,
    Description = 22,
    Aid = 23,
    DestroyedAid = 24,
    Ac = 25,
    HpPts = 26,
    HpAdj = 27,
    HpDamage = 28,
    Material = 29,
    ResistanceIdx = 30,
    ScriptsIdx = 31,
    SoundEffect = 32,
    Category = 33,

    // Bit 34: wire type is Float (Rotation). The name below is a legacy
    // misnomer — despite the "Ias1" suffix implying an int32 array, ObjectPropertyIo correctly
    // dispatches this bit as Float. Do not rename without updating ObjectCommon.cs.
    PadIas1 = 34,
    PadI64As1 = 35,

    // ── Common (bits 36–40) — movement / physics ─────────────────────────────
    // Wire types are Float throughout; cross-referenced from object_fields[].
    /// <summary>Run speed (float). Bit 36 in the common bitmap.</summary>
    SpeedRun = 36,

    /// <summary>Walk speed (float). Bit 37 in the common bitmap.</summary>
    SpeedWalk = 37,

    /// <summary>Reserved float field. Bit 38 in the common bitmap.</summary>
    PadFloat1 = 38,

    /// <summary>Collision radius (float). Bit 39 in the common bitmap.</summary>
    Radius = 39,

    /// <summary>Collision height (float). Bit 40 in the common bitmap.</summary>
    Height = 40,

    // ── Common extension (bits 41–63) — arcanum-CE / ToEE fields ─────────────
    /// <summary>Active condition handles SAR. Bit 41 (arcanum-CE/ToEE).</summary>
    Conditions = 41,

    /// <summary>Condition argument SAR. Bit 42 (arcanum-CE/ToEE).</summary>
    ConditionArg0 = 42,

    /// <summary>Permanent mod handles SAR. Bit 43 (arcanum-CE/ToEE).</summary>
    PermanentMods = 43,

    /// <summary>Combat initiative. Bit 44 (arcanum-CE/ToEE).</summary>
    Initiative = 44,

    /// <summary>Runtime dispatcher handle (0 in saves). Bit 45.</summary>
    Dispatcher = 45,

    /// <summary>Sub-initiative tiebreaker. Bit 46.</summary>
    Subinitiative = 46,

    /// <summary>Secret-door flags. Bit 47.</summary>
    SecretdoorFlags = 47,

    /// <summary>Secret-door animation effect name. Bit 48.</summary>
    SecretdoorEffectName = 48,

    /// <summary>Secret-door detection DC. Bit 49.</summary>
    SecretdoorDc = 49,

    /// <summary>Padding int32 fields. Bits 50–53.</summary>
    PadI7 = 50,
    PadI8 = 51,
    PadI9 = 52,
    PadI0 = 53,

    /// <summary>Z-axis offset (float). Bit 54.</summary>
    OffsetZ = 54,

    /// <summary>Pitch rotation (float). Bit 55.</summary>
    RotationPitch = 55,

    /// <summary>Padding float fields. Bits 56–63.</summary>
    PadF3 = 56,
    PadF4 = 57,
    PadF5 = 58,
    PadF6 = 59,
    PadF7 = 60,
    PadF8 = 61,
    PadF9 = 62,
    PadF0 = 63,

    // ── Wall (bits 64–68) ────────────────────────────────────────────────────
    WallFlags = 64,
    WallPadI1 = 65,
    WallPadI2 = 66,
    WallPadIas1 = 67,
    WallPadI64As1 = 68,

    // ── Portal (bits 64–71) ──────────────────────────────────────────────────
    PortalFlags = 64,
    PortalLockDifficulty = 65,
    PortalKeyId = 66,
    PortalNotifyNpc = 67,
    PortalPadI1 = 68,
    PortalPadI2 = 69,
    PortalPadIas1 = 70,
    PortalPadI64As1 = 71,

    // ── Container (bits 64–74) ───────────────────────────────────────────────
    ContainerFlags = 64,
    ContainerLockDifficulty = 65,
    ContainerKeyId = 66,
    ContainerInventoryNum = 67,
    ContainerInventoryListIdx = 68,
    ContainerInventorySource = 69,
    ContainerNotifyNpc = 70,
    ContainerPadI1 = 71,
    ContainerPadI2 = 72,
    ContainerPadIas1 = 73,
    ContainerPadI64As1 = 74,

    // ── Scenery (bits 64–69) ─────────────────────────────────────────────────
    SceneryFlags = 64,
    SceneryWhosInMe = 65,
    SceneryRespawnDelay = 66,
    SceneryPadI2 = 67,
    SceneryPadIas1 = 68,
    SceneryPadI64As1 = 69,

    // ── Projectile (bits 64–71) ──────────────────────────────────────────
    ProjectileFlagsCombat = 64,
    ProjectileFlagsCombatDamage = 65,
    ProjectileHitLoc = 66,
    ProjectileParentWeapon = 67,
    ProjectilePadI1 = 68,
    ProjectilePadI2 = 69,
    ProjectilePadIas1 = 70,
    ProjectilePadI64As1 = 71,

    // ── Trap (bits 64–68) ────────────────────────────────────────────────────
    TrapFlags = 64,
    TrapDifficulty = 65,
    TrapPadI2 = 66,
    TrapPadIas1 = 67,
    TrapPadI64As1 = 68,

    // ── Item (bits 64–86) ────────────────────────────────────────────────────
    ItemFlags = 64,
    ItemParent = 65,
    ItemWeight = 66,
    ItemMagicWeightAdj = 67,
    ItemWorth = 68,
    ItemManaStore = 69,
    ItemInvAid = 70,
    ItemInvLocation = 71,
    ItemUseAidFragment = 72,
    ItemMagicTechComplexity = 73,
    ItemDiscipline = 74,
    ItemDescriptionUnknown = 75,
    ItemDescriptionEffects = 76,
    ItemSpell1 = 77,
    ItemSpell2 = 78,
    ItemSpell3 = 79,
    ItemSpell4 = 80,
    ItemSpell5 = 81,
    ItemSpellManaStore = 82,
    ItemAiAction = 83,
    ItemPadI1 = 84,
    ItemPadIas1 = 85,
    ItemPadI64As1 = 86,

    // ── Weapon (bits 96–122) ─────────────────────────────────────────────────
    WeaponFlags = 96,
    WeaponPaperDollAid = 97,
    WeaponBonusToHit = 98,
    WeaponMagicHitAdj = 99,
    WeaponDamageLowerIdx = 100,
    WeaponDamageUpperIdx = 101,
    WeaponMagicDamageAdjIdx = 102,
    WeaponSpeedFactor = 103,
    WeaponMagicSpeedAdj = 104,
    WeaponRange = 105,
    WeaponMagicRangeAdj = 106,
    WeaponMinStrength = 107,
    WeaponMagicMinStrengthAdj = 108,
    WeaponAmmoType = 109,
    WeaponAmmoConsumption = 110,
    WeaponMissileAid = 111,
    WeaponVisualEffectAid = 112,
    WeaponCritHitChart = 113,
    WeaponMagicCritHitChance = 114,
    WeaponMagicCritHitEffect = 115,
    WeaponCritMissChart = 116,
    WeaponMagicCritMissChance = 117,
    WeaponMagicCritMissEffect = 118,
    WeaponPadI1 = 119,
    WeaponPadI2 = 120,
    WeaponPadIas1 = 121,
    WeaponPadI64As1 = 122,

    // ── Ammo (bits 96–102) ───────────────────────────────────────────────────
    AmmoFlags = 96,
    AmmoQuantity = 97,
    AmmoType = 98,
    AmmoPadI1 = 99,
    AmmoPadI2 = 100,
    AmmoPadIas1 = 101,
    AmmoPadI64As1 = 102,

    // ── Armor (bits 96–107) ──────────────────────────────────────────────────
    ArmorFlags = 96,
    ArmorPaperDollAid = 97,
    ArmorAcAdj = 98,
    ArmorMagicAcAdj = 99,
    ArmorResistanceAdjIdx = 100,
    ArmorMagicResistanceAdjIdx = 101,
    ArmorSilentMoveAdj = 102,
    ArmorMagicSilentMoveAdj = 103,
    ArmorUnarmedBonusDamage = 104,
    ArmorPadI2 = 105,
    ArmorPadIas1 = 106,
    ArmorPadI64As1 = 107,

    // ── Gold (bits 96–101) ───────────────────────────────────────────────────
    GoldFlags = 96,
    GoldQuantity = 97,
    GoldPadI1 = 98,
    GoldPadI2 = 99,
    GoldPadIas1 = 100,
    GoldPadI64As1 = 101,

    // ── Food (bits 96–100) ───────────────────────────────────────────────────
    FoodFlags = 96,
    FoodPadI1 = 97,
    FoodPadI2 = 98,
    FoodPadIas1 = 99,
    FoodPadI64As1 = 100,

    // ── Scroll (bits 96–100) ─────────────────────────────────────────────────
    ScrollFlags = 96,
    ScrollPadI1 = 97,
    ScrollPadI2 = 98,
    ScrollPadIas1 = 99,
    ScrollPadI64As1 = 100,

    // ── Key (bits 96–100) ────────────────────────────────────────────────────
    KeyKeyId = 96,
    KeyPadI1 = 97,
    KeyPadI2 = 98,
    KeyPadIas1 = 99,
    KeyPadI64As1 = 100,

    // ── KeyRing (bits 96–101) ────────────────────────────────────────────────
    KeyRingFlags = 96,
    KeyRingListIdx = 97,
    KeyRingPadI1 = 98,
    KeyRingPadI2 = 99,
    KeyRingPadIas1 = 100,
    KeyRingPadI64As1 = 101,

    // ── Written (bits 96–103) ────────────────────────────────────────────────
    WrittenFlags = 96,
    WrittenSubtype = 97,
    WrittenTextStartLine = 98,
    WrittenTextEndLine = 99,
    WrittenPadI1 = 100,
    WrittenPadI2 = 101,
    WrittenPadIas1 = 102,
    WrittenPadI64As1 = 103,

    // ── Generic (bits 96–100) ────────────────────────────────────────────────
    GenericFlags = 96,
    GenericUsageBonus = 97,
    GenericUsageCountRemaining = 98,
    GenericPadIas1 = 99,
    GenericPadI64As1 = 100,

    // ── Critter (bits 64–96) ─────────────────────────────────────────────────
    CritterFlags = 64,
    CritterFlags2 = 65,
    CritterStatBaseIdx = 66,
    CritterBasicSkillIdx = 67,
    CritterTechSkillIdx = 68,
    CritterSpellTechIdx = 69,
    CritterFatiguePts = 70,
    CritterFatigueAdj = 71,
    CritterFatigueDamage = 72,
    CritterCritHitChart = 73,
    CritterEffectsIdx = 74,
    CritterEffectCauseIdx = 75,
    CritterFleeingFrom = 76,
    CritterPortrait = 77,
    CritterGold = 78,
    CritterArrows = 79,
    CritterBullets = 80,
    CritterPowerCells = 81,
    CritterFuel = 82,
    CritterInventoryNum = 83,
    CritterInventoryListIdx = 84,
    CritterInventorySource = 85,
    CritterDescriptionUnknown = 86,
    CritterFollowerIdx = 87,
    CritterTeleportDest = 88,
    CritterTeleportMap = 89,
    CritterDeathTime = 90,
    CritterAutoLevelScheme = 91,
    CritterPadI1 = 92,
    CritterPadI2 = 93,
    CritterPadI3 = 94,
    CritterPadIas1 = 95,
    CritterPadI64As1 = 96,

    // ── PC (bits 128–152) ────────────────────────────────────────────────────
    PcFlags = 128,
    PcFlagsFate = 129,
    PcReputationIdx = 130,
    PcReputationTsIdx = 131,
    PcBackground = 132,
    PcBackgroundText = 133,
    PcQuestIdx = 134,
    PcBlessingIdx = 135,
    PcBlessingTsIdx = 136,
    PcCurseIdx = 137,
    PcCurseTsIdx = 138,
    PcPartyId = 139,
    PcRumorIdx = 140,
    PcPadIas2 = 141,
    PcSchematicsFoundIdx = 142,
    PcLogbookEgoIdx = 143,
    PcFogMask = 144,
    PcPlayerName = 145,
    PcBankMoney = 146,
    PcGlobalFlags = 147,
    PcGlobalVariables = 148,
    PcPadI1 = 149,
    PcPadI2 = 150,
    PcPadIas1 = 151,
    PcPadI64As1 = 152,

    // ── NPC (bits 128–152) ───────────────────────────────────────────────────
    NpcFlags = 128,
    NpcLeader = 129,
    NpcAiData = 130,
    NpcCombatFocus = 131,
    NpcWhoHitMeLast = 132,
    NpcExperienceWorth = 133,
    NpcExperiencePool = 134,
    NpcWaypointsIdx = 135,
    NpcWaypointCurrent = 136,
    NpcStandpointDay = 137,
    NpcStandpointNight = 138,
    NpcOrigin = 139,
    NpcFaction = 140,
    NpcRetailPriceMultiplier = 141,
    NpcSubstituteInventory = 142,
    NpcReactionBase = 143,
    NpcSocialClass = 144,
    NpcReactionPcIdx = 145,
    NpcReactionLevelIdx = 146,
    NpcReactionTimeIdx = 147,
    NpcWait = 148,
    NpcGeneratorData = 149,
    NpcPadI1 = 150,
    NpcDamageIdx = 151,
    NpcHostileListIdx = 152,
}
