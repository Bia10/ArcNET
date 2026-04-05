namespace ArcNET.GameObjects;

/// <summary>
/// Maps every game-object field name to its bit index within the per-type bitmap stored in
/// <see cref="GameObjectHeader.Bitmap"/>.  Reading code checks this index before reading each
/// field: if the bit is clear (and the object is not a prototype) the field is absent.
/// </summary>
public enum ObjectField : byte
{
    // ── Common (bits 0–35) ───────────────────────────────────────────────────
    ObjFCurrentAid = 0,
    ObjFLocation = 1,
    ObjFOffsetX = 2,
    ObjFOffsetY = 3,
    ObjFShadow = 4,
    ObjFOverlayFore = 5,
    ObjFOverlayBack = 6,
    ObjFUnderlay = 7,
    ObjFBlitFlags = 8,
    ObjFBlitColor = 9,
    ObjFBlitAlpha = 10,
    ObjFBlitScale = 11,
    ObjFLightFlags = 12,
    ObjFLightAid = 13,
    ObjFLightColor = 14,
    ObjFOverlayLightFlags = 15,
    ObjFOverlayLightAid = 16,
    ObjFOverlayLightColor = 17,
    ObjFFlags = 18,
    ObjFSpellFlags = 19,
    ObjFBlockingMask = 20,
    ObjFName = 21,
    ObjFDescription = 22,
    ObjFAid = 23,
    ObjFDestroyedAid = 24,
    ObjFAc = 25,
    ObjFHpPts = 26,
    ObjFHpAdj = 27,
    ObjFHpDamage = 28,
    ObjFMaterial = 29,
    ObjFResistanceIdx = 30,
    ObjFScriptsIdx = 31,
    ObjFSoundEffect = 32,
    ObjFCategory = 33,

    // Bit 34: wire type is Float (ObjFRotation). The name below is a legacy
    // misnomer — despite the "Ias1" suffix implying an int32 array, ObjectPropertyIo correctly
    // dispatches this bit as Float. Do not rename without updating ObjectCommon.cs.
    ObjFPadIas1 = 34,
    ObjFPadI64As1 = 35,

    // ── Common (bits 36–40) — movement / physics ─────────────────────────────
    // Wire types are Float throughout; cross-referenced from object_fields[].
    /// <summary>Run speed (float). Bit 36 in the common bitmap.</summary>
    ObjFSpeedRun = 36,

    /// <summary>Walk speed (float). Bit 37 in the common bitmap.</summary>
    ObjFSpeedWalk = 37,

    /// <summary>Reserved float field. Bit 38 in the common bitmap.</summary>
    ObjFPadFloat1 = 38,

    /// <summary>Collision radius (float). Bit 39 in the common bitmap.</summary>
    ObjFRadius = 39,

    /// <summary>Collision height (float). Bit 40 in the common bitmap.</summary>
    ObjFHeight = 40,

    // ── Common extension (bits 41–63) — arcanum-CE / ToEE fields ─────────────
    /// <summary>Active condition handles SAR. Bit 41 (arcanum-CE/ToEE).</summary>
    ObjFConditions = 41,

    /// <summary>Condition argument SAR. Bit 42 (arcanum-CE/ToEE).</summary>
    ObjFConditionArg0 = 42,

    /// <summary>Permanent mod handles SAR. Bit 43 (arcanum-CE/ToEE).</summary>
    ObjFPermanentMods = 43,

    /// <summary>Combat initiative. Bit 44 (arcanum-CE/ToEE).</summary>
    ObjFInitiative = 44,

    /// <summary>Runtime dispatcher handle (0 in saves). Bit 45.</summary>
    ObjFDispatcher = 45,

    /// <summary>Sub-initiative tiebreaker. Bit 46.</summary>
    ObjFSubinitiative = 46,

    /// <summary>Secret-door flags. Bit 47.</summary>
    ObjFSecretdoorFlags = 47,

    /// <summary>Secret-door animation effect name. Bit 48.</summary>
    ObjFSecretdoorEffectName = 48,

    /// <summary>Secret-door detection DC. Bit 49.</summary>
    ObjFSecretdoorDc = 49,

    /// <summary>Padding int32 fields. Bits 50–53.</summary>
    ObjFPadI7 = 50,
    ObjFPadI8 = 51,
    ObjFPadI9 = 52,
    ObjFPadI0 = 53,

    /// <summary>Z-axis offset (float). Bit 54.</summary>
    ObjFOffsetZ = 54,

    /// <summary>Pitch rotation (float). Bit 55.</summary>
    ObjFRotationPitch = 55,

    /// <summary>Padding float fields. Bits 56–63.</summary>
    ObjFPadF3 = 56,
    ObjFPadF4 = 57,
    ObjFPadF5 = 58,
    ObjFPadF6 = 59,
    ObjFPadF7 = 60,
    ObjFPadF8 = 61,
    ObjFPadF9 = 62,
    ObjFPadF0 = 63,

    // ── Wall (bits 64–68) ────────────────────────────────────────────────────
    ObjFWallFlags = 64,
    ObjFWallPadI1 = 65,
    ObjFWallPadI2 = 66,
    ObjFWallPadIas1 = 67,
    ObjFWallPadI64As1 = 68,

    // ── Portal (bits 64–71) ──────────────────────────────────────────────────
    ObjFPortalFlags = 64,
    ObjFPortalLockDifficulty = 65,
    ObjFPortalKeyId = 66,
    ObjFPortalNotifyNpc = 67,
    ObjFPortalPadI1 = 68,
    ObjFPortalPadI2 = 69,
    ObjFPortalPadIas1 = 70,
    ObjFPortalPadI64As1 = 71,

    // ── Container (bits 64–74) ───────────────────────────────────────────────
    ObjFContainerFlags = 64,
    ObjFContainerLockDifficulty = 65,
    ObjFContainerKeyId = 66,
    ObjFContainerInventoryNum = 67,
    ObjFContainerInventoryListIdx = 68,
    ObjFContainerInventorySource = 69,
    ObjFContainerNotifyNpc = 70,
    ObjFContainerPadI1 = 71,
    ObjFContainerPadI2 = 72,
    ObjFContainerPadIas1 = 73,
    ObjFContainerPadI64As1 = 74,

    // ── Scenery (bits 64–69) ─────────────────────────────────────────────────
    ObjFSceneryFlags = 64,
    ObjFSceneryWhosInMe = 65,
    ObjFSceneryRespawnDelay = 66,
    ObjFSceneryPadI2 = 67,
    ObjFSceneryPadIas1 = 68,
    ObjFSceneryPadI64As1 = 69,

    // ── Projectile (bits 64–71) ──────────────────────────────────────────
    ObjFProjectileFlagsCombat = 64,
    ObjFProjectileFlagsCombatDamage = 65,
    ObjFProjectileHitLoc = 66,
    ObjFProjectileParentWeapon = 67,
    ObjFProjectilePadI1 = 68,
    ObjFProjectilePadI2 = 69,
    ObjFProjectilePadIas1 = 70,
    ObjFProjectilePadI64As1 = 71,

    // ── Trap (bits 64–68) ────────────────────────────────────────────────────
    ObjFTrapFlags = 64,
    ObjFTrapDifficulty = 65,
    ObjFTrapPadI2 = 66,
    ObjFTrapPadIas1 = 67,
    ObjFTrapPadI64As1 = 68,

    // ── Item (bits 64–86) ────────────────────────────────────────────────────
    ObjFItemFlags = 64,
    ObjFItemParent = 65,
    ObjFItemWeight = 66,
    ObjFItemMagicWeightAdj = 67,
    ObjFItemWorth = 68,
    ObjFItemManaStore = 69,
    ObjFItemInvAid = 70,
    ObjFItemInvLocation = 71,
    ObjFItemUseAidFragment = 72,
    ObjFItemMagicTechComplexity = 73,
    ObjFItemDiscipline = 74,
    ObjFItemDescriptionUnknown = 75,
    ObjFItemDescriptionEffects = 76,
    ObjFItemSpell1 = 77,
    ObjFItemSpell2 = 78,
    ObjFItemSpell3 = 79,
    ObjFItemSpell4 = 80,
    ObjFItemSpell5 = 81,
    ObjFItemSpellManaStore = 82,
    ObjFItemAiAction = 83,
    ObjFItemPadI1 = 84,
    ObjFItemPadIas1 = 85,
    ObjFItemPadI64As1 = 86,

    // ── Weapon (bits 96–122) ─────────────────────────────────────────────────
    ObjFWeaponFlags = 96,
    ObjFWeaponPaperDollAid = 97,
    ObjFWeaponBonusToHit = 98,
    ObjFWeaponMagicHitAdj = 99,
    ObjFWeaponDamageLowerIdx = 100,
    ObjFWeaponDamageUpperIdx = 101,
    ObjFWeaponMagicDamageAdjIdx = 102,
    ObjFWeaponSpeedFactor = 103,
    ObjFWeaponMagicSpeedAdj = 104,
    ObjFWeaponRange = 105,
    ObjFWeaponMagicRangeAdj = 106,
    ObjFWeaponMinStrength = 107,
    ObjFWeaponMagicMinStrengthAdj = 108,
    ObjFWeaponAmmoType = 109,
    ObjFWeaponAmmoConsumption = 110,
    ObjFWeaponMissileAid = 111,
    ObjFWeaponVisualEffectAid = 112,
    ObjFWeaponCritHitChart = 113,
    ObjFWeaponMagicCritHitChance = 114,
    ObjFWeaponMagicCritHitEffect = 115,
    ObjFWeaponCritMissChart = 116,
    ObjFWeaponMagicCritMissChance = 117,
    ObjFWeaponMagicCritMissEffect = 118,
    ObjFWeaponPadI1 = 119,
    ObjFWeaponPadI2 = 120,
    ObjFWeaponPadIas1 = 121,
    ObjFWeaponPadI64As1 = 122,

    // ── Ammo (bits 96–102) ───────────────────────────────────────────────────
    ObjFAmmoFlags = 96,
    ObjFAmmoQuantity = 97,
    ObjFAmmoType = 98,
    ObjFAmmoPadI1 = 99,
    ObjFAmmoPadI2 = 100,
    ObjFAmmoPadIas1 = 101,
    ObjFAmmoPadI64As1 = 102,

    // ── Armor (bits 96–107) ──────────────────────────────────────────────────
    ObjFArmorFlags = 96,
    ObjFArmorPaperDollAid = 97,
    ObjFArmorAcAdj = 98,
    ObjFArmorMagicAcAdj = 99,
    ObjFArmorResistanceAdjIdx = 100,
    ObjFArmorMagicResistanceAdjIdx = 101,
    ObjFArmorSilentMoveAdj = 102,
    ObjFArmorMagicSilentMoveAdj = 103,
    ObjFArmorUnarmedBonusDamage = 104,
    ObjFArmorPadI2 = 105,
    ObjFArmorPadIas1 = 106,
    ObjFArmorPadI64As1 = 107,

    // ── Gold (bits 96–101) ───────────────────────────────────────────────────
    ObjFGoldFlags = 96,
    ObjFGoldQuantity = 97,
    ObjFGoldPadI1 = 98,
    ObjFGoldPadI2 = 99,
    ObjFGoldPadIas1 = 100,
    ObjFGoldPadI64As1 = 101,

    // ── Food (bits 96–100) ───────────────────────────────────────────────────
    ObjFFoodFlags = 96,
    ObjFFoodPadI1 = 97,
    ObjFFoodPadI2 = 98,
    ObjFFoodPadIas1 = 99,
    ObjFFoodPadI64As1 = 100,

    // ── Scroll (bits 96–100) ─────────────────────────────────────────────────
    ObjFScrollFlags = 96,
    ObjFScrollPadI1 = 97,
    ObjFScrollPadI2 = 98,
    ObjFScrollPadIas1 = 99,
    ObjFScrollPadI64As1 = 100,

    // ── Key (bits 96–100) ────────────────────────────────────────────────────
    ObjFKeyKeyId = 96,
    ObjFKeyPadI1 = 97,
    ObjFKeyPadI2 = 98,
    ObjFKeyPadIas1 = 99,
    ObjFKeyPadI64As1 = 100,

    // ── KeyRing (bits 96–101) ────────────────────────────────────────────────
    ObjFKeyRingFlags = 96,
    ObjFKeyRingListIdx = 97,
    ObjFKeyRingPadI1 = 98,
    ObjFKeyRingPadI2 = 99,
    ObjFKeyRingPadIas1 = 100,
    ObjFKeyRingPadI64As1 = 101,

    // ── Written (bits 96–103) ────────────────────────────────────────────────
    ObjFWrittenFlags = 96,
    ObjFWrittenSubtype = 97,
    ObjFWrittenTextStartLine = 98,
    ObjFWrittenTextEndLine = 99,
    ObjFWrittenPadI1 = 100,
    ObjFWrittenPadI2 = 101,
    ObjFWrittenPadIas1 = 102,
    ObjFWrittenPadI64As1 = 103,

    // ── Generic (bits 96–100) ────────────────────────────────────────────────
    ObjFGenericFlags = 96,
    ObjFGenericUsageBonus = 97,
    ObjFGenericUsageCountRemaining = 98,
    ObjFGenericPadIas1 = 99,
    ObjFGenericPadI64As1 = 100,

    // ── Critter (bits 64–96) ─────────────────────────────────────────────────
    ObjFCritterFlags = 64,
    ObjFCritterFlags2 = 65,
    ObjFCritterStatBaseIdx = 66,
    ObjFCritterBasicSkillIdx = 67,
    ObjFCritterTechSkillIdx = 68,
    ObjFCritterSpellTechIdx = 69,
    ObjFCritterFatiguePts = 70,
    ObjFCritterFatigueAdj = 71,
    ObjFCritterFatigueDamage = 72,
    ObjFCritterCritHitChart = 73,
    ObjFCritterEffectsIdx = 74,
    ObjFCritterEffectCauseIdx = 75,
    ObjFCritterFleeingFrom = 76,
    ObjFCritterPortrait = 77,
    ObjFCritterGold = 78,
    ObjFCritterArrows = 79,
    ObjFCritterBullets = 80,
    ObjFCritterPowerCells = 81,
    ObjFCritterFuel = 82,
    ObjFCritterInventoryNum = 83,
    ObjFCritterInventoryListIdx = 84,
    ObjFCritterInventorySource = 85,
    ObjFCritterDescriptionUnknown = 86,
    ObjFCritterFollowerIdx = 87,
    ObjFCritterTeleportDest = 88,
    ObjFCritterTeleportMap = 89,
    ObjFCritterDeathTime = 90,
    ObjFCritterAutoLevelScheme = 91,
    ObjFCritterPadI1 = 92,
    ObjFCritterPadI2 = 93,
    ObjFCritterPadI3 = 94,
    ObjFCritterPadIas1 = 95,
    ObjFCritterPadI64As1 = 96,

    // ── PC (bits 128–152) ────────────────────────────────────────────────────
    ObjFPcFlags = 128,
    ObjFPcFlagsFate = 129,
    ObjFPcReputationIdx = 130,
    ObjFPcReputationTsIdx = 131,
    ObjFPcBackground = 132,
    ObjFPcBackgroundText = 133,
    ObjFPcQuestIdx = 134,
    ObjFPcBlessingIdx = 135,
    ObjFPcBlessingTsIdx = 136,
    ObjFPcCurseIdx = 137,
    ObjFPcCurseTsIdx = 138,
    ObjFPcPartyId = 139,
    ObjFPcRumorIdx = 140,
    ObjFPcPadIas2 = 141,
    ObjFPcSchematicsFoundIdx = 142,
    ObjFPcLogbookEgoIdx = 143,
    ObjFPcFogMask = 144,
    ObjFPcPlayerName = 145,
    ObjFPcBankMoney = 146,
    ObjFPcGlobalFlags = 147,
    ObjFPcGlobalVariables = 148,
    ObjFPcPadI1 = 149,
    ObjFPcPadI2 = 150,
    ObjFPcPadIas1 = 151,
    ObjFPcPadI64As1 = 152,

    // ── NPC (bits 128–152) ───────────────────────────────────────────────────
    ObjFNpcFlags = 128,
    ObjFNpcLeader = 129,
    ObjFNpcAiData = 130,
    ObjFNpcCombatFocus = 131,
    ObjFNpcWhoHitMeLast = 132,
    ObjFNpcExperienceWorth = 133,
    ObjFNpcExperiencePool = 134,
    ObjFNpcWaypointsIdx = 135,
    ObjFNpcWaypointCurrent = 136,
    ObjFNpcStandpointDay = 137,
    ObjFNpcStandpointNight = 138,
    ObjFNpcOrigin = 139,
    ObjFNpcFaction = 140,
    ObjFNpcRetailPriceMultiplier = 141,
    ObjFNpcSubstituteInventory = 142,
    ObjFNpcReactionBase = 143,
    ObjFNpcSocialClass = 144,
    ObjFNpcReactionPcIdx = 145,
    ObjFNpcReactionLevelIdx = 146,
    ObjFNpcReactionTimeIdx = 147,
    ObjFNpcWait = 148,
    ObjFNpcGeneratorData = 149,
    ObjFNpcPadI1 = 150,
    ObjFNpcDamageIdx = 151,
    ObjFNpcShitListIdx = 152,
}
