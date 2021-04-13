﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace ArcNET.DataTypes.GameObjects.Flags
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ObjFCritterFlags2
    {
        OCF2_ITEM_STOLEN = 1,
        OCF2_AUTO_ANIMATES,
        OCF2_USING_BOOMERANG,
        OCF2_FATIGUE_DRAINING,
        OCF2_SLOW_PARTY,
        OCF2_COMBAT_TOGGLE_FX,
        OCF2_NO_DECAY,
        OCF2_NO_PICKPOCKET,
        OCF2_NO_BLOOD_SPLOTCHES,
        OCF2_NIGH_INVULNERABLE,
        OCF2_ELEMENTAL,
        OCF2_DARK_SIGHT,
        OCF2_NO_SLIP,
        OCF2_NO_DISINTEGRATE,
        OCF2_REACTION_0,
        OCF2_REACTION_1,
        OCF2_REACTION_2,
        OCF2_REACTION_3,
        OCF2_REACTION_4,
        OCF2_REACTION_5,
        OCF2_REACTION_6,
        OCF2_TARGET_LOCK,
        OCF2_PERMA_POLYMORPH,
        OCF2_SAFE_OFF,
        OCF2_CHECK_REACTION_BAD,
        OCF2_CHECK_ALIGN_GOOD,
        OCF2_CHECK_ALIGN_BAD
    }
}