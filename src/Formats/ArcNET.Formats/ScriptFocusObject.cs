namespace ArcNET.Formats;

/// <summary>
/// Script operand focus-object tags (SFO_* enum).
/// Determines which game object(s) an operand refers to.
/// </summary>
public enum ScriptFocusObject : byte
{
    /// <summary>The script triggerer object.</summary>
    Triggerer = 0,

    /// <summary>The object the script is attached to.</summary>
    Attachee = 1,

    /// <summary>Every follower of the triggerer.</summary>
    EveryFollower = 2,

    /// <summary>Any single follower of the triggerer.</summary>
    AnyFollower = 3,

    /// <summary>Everyone in the player's party.</summary>
    EveryoneInParty = 4,

    /// <summary>Any member of the player's party.</summary>
    AnyoneInParty = 5,

    /// <summary>Every member of the triggerer's team.</summary>
    EveryoneInTeam = 6,

    /// <summary>Any member of the triggerer's team.</summary>
    AnyoneInTeam = 7,

    /// <summary>Every object within vicinity.</summary>
    EveryoneInVicinity = 8,

    /// <summary>Any object within vicinity.</summary>
    AnyoneInVicinity = 9,

    /// <summary>The object currently being iterated in a loop.</summary>
    CurrentLoopedObject = 10,

    /// <summary>A locally-stored object reference.</summary>
    LocalObject = 11,

    /// <summary>An extra object reference slot.</summary>
    ExtraObject = 12,

    /// <summary>Every member of a defined group.</summary>
    EveryoneInGroup = 13,

    /// <summary>Any member of a defined group.</summary>
    AnyoneInGroup = 14,

    /// <summary>Every scenery object within vicinity.</summary>
    EverySceneryInVicinity = 15,

    /// <summary>Any scenery object within vicinity.</summary>
    AnySceneryInVicinity = 16,

    /// <summary>Every container within vicinity.</summary>
    EveryContainerInVicinity = 17,

    /// <summary>Any container within vicinity.</summary>
    AnyContainerInVicinity = 18,

    /// <summary>Every portal within vicinity.</summary>
    EveryPortalInVicinity = 19,

    /// <summary>Any portal within vicinity.</summary>
    AnyPortalInVicinity = 20,

    /// <summary>The player character.</summary>
    Player = 21,

    /// <summary>Every item within vicinity.</summary>
    EveryItemInVicinity = 22,

    /// <summary>Any item within vicinity.</summary>
    AnyItemInVicinity = 23,
}
