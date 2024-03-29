﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace ArcNET.DataTypes.GameObjects.Flags;

[Flags]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ObjFCritterFlags
{
    OCF_IS_CONCEALED = 1,
    OCF_MOVING_SILENTLY,
    OCF_UNDEAD,
    OCF_ANIMAL,
    OCF_FLEEING,
    OCF_STUNNED,
    OCF_PARALYZED,
    OCF_BLINDED,
    OCF_CRIPPLED_ARMS_ONE,
    OCF_CRIPPLED_ARMS_BOTH,
    OCF_CRIPPLED_LEGS_BOTH,
    OCF_UNUSED,
    OCF_SLEEPING,
    OCF_MUTE,
    OCF_SURRENDERED,
    OCF_MONSTER,
    OCF_SPELL_FLEE,
    OCF_ENCOUNTER,
    OCF_COMBAT_MODE_ACTIVE,
    OCF_LIGHT_SMALL,
    OCF_LIGHT_MEDIUM,
    OCF_LIGHT_LARGE,
    OCF_LIGHT_XLARGE,
    OCF_UNREVIVIFIABLE,
    OCF_UNRESSURECTABLE,
    OCF_DEMON,
    OCF_FATIGUE_IMMUNE,
    OCF_NO_FLEE,
    OCF_NON_LETHAL_COMBAT,
    OCF_MECHANICAL,
    OCF_ANIMAL_ENSHROUD,
    OCF_FATIGUE_LIMITING
}