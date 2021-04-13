﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace ArcNET.DataTypes.GameObjects.Flags
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ObjFNpcFlags
    {
        ONF_FIGHTING = 1,
        ONF_WAYPOINTS_DAY,
        ONF_WAYPOINTS_NIGHT,
        ONF_AI_WAIT_HERE,
        ONF_AI_SPREAD_OUT,
        ONF_JILTED,
        ONF_CHECK_WIELD,
        ONF_CHECK_WEAPON,
        ONF_KOS,
        ONF_WAYPOINTS_BED,
        ONF_FORCED_FOLLOWER,
        ONF_KOS_OVERRIDE,
        ONF_WANDERS,
        ONF_WANDERS_IN_DARK,
        ONF_FENCE,
        ONF_FAMILIAR,
        ONF_CHECK_LEADER,
        ONF_ALOOF,
        ONF_CAST_HIGHEST,
        ONF_GENERATOR,
        ONF_GENERATED,
        ONF_GENERATOR_RATE1,
        ONF_GENERATOR_RATE2,
        ONF_GENERATOR_RATE3,
        ONF_DEMAINTAIN_SPELLS,
        ONF_LOOK_FOR_WEAPON,
        ONF_LOOK_FOR_ARMOR,
        ONF_LOOK_FOR_AMMO,
        ONF_BACKING_OFF,
        ONF_NO_ATTACK
    }
}