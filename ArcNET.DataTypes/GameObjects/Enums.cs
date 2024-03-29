﻿namespace ArcNET.DataTypes.GameObjects;

public static class Enums
{
    public enum ObjectFieldBitmap
    {
        Wall = 12,
        Portal = 12,
        Container = 12,
        Scenery = 12,
        Projectile = 12,
        Trap = 12,
        Weapon = 16,
        Ammo = 16,
        Armor = 16,
        Gold = 16,
        Food = 16,
        Scroll = 16,
        Key = 16,
        KeyRing = 16,
        Written = 16,
        Generic = 16,
        Pc = 20,
        Npc = 20,
    }

    public enum ObjectType
    {
        Wall = 0,
        Portal,
        Container,
        Scenery,
        Projectile,
        Weapon,
        Ammo,
        Armor,
        Gold,
        Food,
        Scroll,
        Key,
        KeyRing,
        Written,
        Generic,
        Pc,
        Npc,
        Trap
    }

    public enum ObjectField
    {
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
        ObjFPadIas1 = 34,
        ObjFPadI64As1 = 35,
        ObjFWallFlags = 64,
        ObjFWallPadI1 = 65,
        ObjFWallPadI2 = 66,
        ObjFWallPadIas1 = 67,
        ObjFWallPadI64As1 = 68,

        ObjFPortalFlags = 64,
        ObjFPortalLockDifficulty = 65,
        ObjFPortalKeyId = 66,
        ObjFPortalNotifyNpc = 67,
        ObjFPortalPadI1 = 68,
        ObjFPortalPadI2 = 69,
        ObjFPortalPadIas1 = 70,
        ObjFPortalPadI64As1 = 71,

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

        ObjFSceneryFlags = 64,
        ObjFSceneryWhosInMe = 65,
        ObjFSceneryRespawnDelay = 66,
        ObjFSceneryPadI2 = 67,
        ObjFSceneryPadIas1 = 68,
        ObjFSceneryPadI64As1 = 69,

        ObjFProjectileFlagsCombat = 0,
        ObjFProjectileFlagsCombatDamage = 1,
        ObjFProjectileHitLoc = 2,
        ObjFProjectileParentWeapon = 3,
        ObjFProjectilePadI1 = 4,
        ObjFProjectilePadI2 = 5,
        ObjFProjectilePadIas1 = 6,
        ObjFProjectilePadI64As1 = 7,

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

        ObjFAmmoFlags = 96,
        ObjFAmmoQuantity = 97,
        ObjFAmmoType = 98,
        ObjFAmmoPadI1 = 99,
        ObjFAmmoPadI2 = 100,
        ObjFAmmoPadIas1 = 101,
        ObjFAmmoPadI64As1 = 102,

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

        ObjFGoldFlags = 96,
        ObjFGoldQuantity = 97,
        ObjFGoldPadI1 = 98,
        ObjFGoldPadI2 = 99,
        ObjFGoldPadIas1 = 100,
        ObjFGoldPadI64As1 = 101,

        ObjFFoodFlags = 96,
        ObjFFoodPadI1 = 97,
        ObjFFoodPadI2 = 98,
        ObjFFoodPadIas1 = 99,
        ObjFFoodPadI64As1 = 100,

        ObjFScrollFlags = 96,
        ObjFScrollPadI1 = 97,
        ObjFScrollPadI2 = 98,
        ObjFScrollPadIas1 = 99,
        ObjFScrollPadI64As1 = 100,

        ObjFKeyKeyId = 96,
        ObjFKeyPadI1 = 97,
        ObjFKeyPadI2 = 98,
        ObjFKeyPadIas1 = 99,
        ObjFKeyPadI64As1 = 100,
        ObjFKeyRingFlags = 96,
        ObjFKeyRingListIdx = 97,
        ObjFKeyRingPadI1 = 98,
        ObjFKeyRingPadI2 = 99,
        ObjFKeyRingPadIas1 = 100,
        ObjFKeyRingPadI64As1 = 101,

        ObjFWrittenFlags = 96,
        ObjFWrittenSubtype = 97,
        ObjFWrittenTextStartLine = 98,
        ObjFWrittenTextEndLine = 99,
        ObjFWrittenPadI1 = 100,
        ObjFWrittenPadI2 = 101,
        ObjFWrittenPadIas1 = 102,
        ObjFWrittenPadI64As1 = 103,

        ObjFGenericFlags = 96,
        ObjFGenericUsageBonus = 97,
        ObjFGenericUsageCountRemaining = 98,
        ObjFGenericPadIas1 = 99,
        ObjFGenericPadI64As1 = 100,

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

        ObjFPcFlags = 128,
        ObjFPcFlagsFate = 129,
        ObjFPcReputationIdx = 130,
        ObjFPcReputationTsIdx = 131,
        ObjFPcBackground = 132,
        ObjFPcBackgroundText = 133,
        ObjFPcQuestIdx = 134,
        ObjFPcBlessingIdx = 134,
        ObjFPcBlessingTsIdx = 135,
        ObjFPcCurseIdx = 136,
        ObjFPcCurseTsIdx = 137,
        ObjFPcPartyId = 138,
        ObjFPcRumorIdx = 139,
        ObjFPcPadIas2 = 140,
        ObjFPcSchematicsFoundIdx = 141,
        ObjFPcLogbookEgoIdx = 142,
        ObjFPcFogMask = 143,
        ObjFPcPlayerName = 144,
        ObjFPcBankMoney = 145,
        ObjFPcGlobalFlags = 146,
        ObjFPcGlobalVariables = 147,
        ObjFPcPadI1 = 148,
        ObjFPcPadI2 = 149,
        ObjFPcPadIas1 = 150,
        ObjFPcPadI64As1 = 151,
        ObjFPcPadI64As2 = 152,

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

        ObjFTrapFlags = 64,
        ObjFTrapDifficulty = 65,
        ObjFTrapPadI2 = 66,
        ObjFTrapPadIas1 = 67,
        ObjFTrapPadI64As1 = 68
    }
}