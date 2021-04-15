﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace ArcNET.DataTypes.GameObjects.Flags
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ObjFFlags
    {
        OF_DESTROYED = 1,
        OF_OFF,
        OF_FLAT,
        OF_TEXT,
        OF_SEE_THROUGH,
        OF_SHOOT_THROUGH,
        OF_TRANSLUCENT,
        OF_SHRUNK,
        OF_DONTDRAW,
        OF_INVISIBLE,
        OF_NO_BLOCK,
        OF_CLICK_THROUGH,
        OF_INVENTORY,
        OF_DYNAMIC,
        OF_PROVIDES_COVER,
        OF_HAS_OVERLAYS,
        OF_HAS_UNDERLAYS,
        OF_WADING,
        OF_WATER_WALKING,
        OF_STONED,
        OF_DONTLIGHT,
        OF_TEXT_FLOATER,
        OF_INVULNERABLE,
        OF_EXTINCT,
        OF_TRAP_PC,
        OF_TRAP_SPOTTED,
        OF_DISALLOW_WADING,
        OF_MULTIPLAYER_LOCK,
        OF_FROZEN,
        OF_ANIMATED_DEAD,
        OF_TELEPORTED
    }
}