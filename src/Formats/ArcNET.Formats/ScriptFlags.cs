namespace ArcNET.Formats;

/// <summary>
/// Behaviour flags on a compiled script file, matching the <c>SF_*</c> constants.
/// </summary>
[Flags]
public enum ScriptFlags : ushort
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Script defines a non-magical trap.</summary>
    NonmagicalTrap = 0x1,

    /// <summary>Script defines a magical trap.</summary>
    MagicalTrap = 0x2,

    /// <summary>Trap is auto-removed after triggering.</summary>
    AutoRemoving = 0x4,

    /// <summary>NPC has a death speech.</summary>
    DeathSpeech = 0x8,

    /// <summary>NPC has a surrender speech.</summary>
    SurrenderSpeech = 0x10,

    /// <summary>Trigger radius is 2 tiles.</summary>
    RadiusTwo = 0x20,

    /// <summary>Trigger radius is 3 tiles.</summary>
    RadiusThree = 0x40,

    /// <summary>Trigger radius is 5 tiles.</summary>
    RadiusFive = 0x80,

    /// <summary>Script is a teleport trigger.</summary>
    TeleportTrigger = 0x100,
}
