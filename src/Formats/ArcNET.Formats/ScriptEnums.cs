namespace ArcNET.Formats;

/// <summary>
/// Script operand value-type tags (SVT_* enum).
/// Determines how to interpret an operand's <c>op_value</c> slot.
/// </summary>
public enum ScriptValueType : byte
{
    Counter,
    GlobalVar,
    LocalVar,
    Number,
    GlobalFlag,
    PcVar,
    PcFlag,
}

/// <summary>
/// Script operand focus-object tags (SFO_* enum).
/// Determines which game object(s) an operand refers to.
/// </summary>
public enum ScriptFocusObject : byte
{
    Triggerer,
    Attachee,
    EveryFollower,
    AnyFollower,
    EveryoneInParty,
    AnyoneInParty,
    EveryoneInTeam,
    AnyoneInTeam,
    EveryoneInVicinity,
    AnyoneInVicinity,
    CurrentLoopedObject,
    LocalObject,
    ExtraObject,
    EveryoneInGroup,
    AnyoneInGroup,
    EverySceneryInVicinity,
    AnySceneryInVicinity,
    EveryContainerInVicinity,
    AnyContainerInVicinity,
    EveryPortalInVicinity,
    AnyPortalInVicinity,
    Player,
    EveryItemInVicinity,
    AnyItemInVicinity,
}

/// <summary>
/// Script attachment points (SAP_* enum).
/// Determines when a script entry is evaluated (e.g. on examine, on use, on dialog).
/// </summary>
public enum ScriptAttachmentPoint
{
    Examine,
    Use,
    Destroy,
    Unlock,
    Get,
    Drop,
    Throw,
    Hit,
    Miss,
    Dialog,
    FirstHeartbeat,
    CatchingThiefPc,
    Dying,
    EnterCombat,
    ExitCombat,
    StartCombat,
    EndCombat,
    BuyObject,
    Resurrect,
    Heartbeat,
    LeaderKilling,
    InsertItem,
    WillKos,
    TakingDamage,
    WieldOn,
    WieldOff,
    CritterHits,
    NewSector,
    RemoveItem,
    LeaderSleeping,
    Bust,
    DialogOverride,
    Transfer,
    CaughtThief,
    CriticalHit,
    CriticalMiss,
}
