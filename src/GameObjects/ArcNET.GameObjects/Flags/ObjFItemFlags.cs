namespace ArcNET.GameObjects;

/// <summary>Item flags shared by all item-type game objects (OIF_ in arcanum-ce obj_flags.h).</summary>
[Flags]
public enum ObjFItemFlags : uint
{
    /// <summary>Item has been identified.</summary>
    Identified = 1 << 0,

    /// <summary>Item cannot be sold.</summary>
    WontSell = 1 << 1,

    /// <summary>Item is magical.</summary>
    IsMagical = 1 << 2,

    /// <summary>Item transfers its light to the owner.</summary>
    TransferLight = 1 << 3,

    /// <summary>Item is hidden from display.</summary>
    NoDisplay = 1 << 4,

    /// <summary>Item cannot be dropped.</summary>
    NoDrop = 1 << 5,

    /// <summary>Item is hexed and cannot be unwielded.</summary>
    Hexed = 1 << 6,

    /// <summary>Item can be activated via the action box.</summary>
    CanUseBox = 1 << 7,

    /// <summary>Item requires a target to use.</summary>
    NeedsTarget = 1 << 8,

    /// <summary>Item emits a small light.</summary>
    LightSmall = 1 << 9,

    /// <summary>Item emits a medium light.</summary>
    LightMedium = 1 << 10,

    /// <summary>Item emits a large light.</summary>
    LightLarge = 1 << 11,

    /// <summary>Item emits an extra-large light.</summary>
    LightXLarge = 1 << 12,

    /// <summary>Item persists after death (not dropped on critter death).</summary>
    Persistent = 1 << 13,

    /// <summary>Item triggers a magic/tech interaction.</summary>
    MtTriggered = 1 << 14,

    /// <summary>Item was stolen.</summary>
    Stolen = 1 << 15,

    /// <summary>Using the item throws it.</summary>
    UseIsThrow = 1 << 16,

    /// <summary>Item does not decay over time.</summary>
    NoDecay = 1 << 17,

    /// <summary>Item is uber-quality.</summary>
    Uber = 1 << 18,

    /// <summary>NPCs cannot pick up this item.</summary>
    NoNpcPickup = 1 << 19,

    /// <summary>Item cannot be used at range.</summary>
    NoRangedUse = 1 << 20,

    /// <summary>AI action for this item is valid.</summary>
    ValidAiAction = 1 << 21,

    /// <summary>Item was inserted via multiplayer.</summary>
    MpInserted = 1 << 22,
}
