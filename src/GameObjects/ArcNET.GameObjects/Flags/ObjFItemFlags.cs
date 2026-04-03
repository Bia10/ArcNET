namespace ArcNET.GameObjects;

/// <summary>Item flags shared by all item-type game objects (OIF_).</summary>
[Flags]
public enum ObjFItemFlags : uint
{
    /// <summary>No flags set.</summary>
    None = 0,

    /// <summary>Item has been identified.</summary>
    Identified = 0x1,

    /// <summary>Item cannot be sold.</summary>
    WontSell = 0x2,

    /// <summary>Item is magical.</summary>
    IsMagical = 0x4,

    /// <summary>Item transfers its light to the owner.</summary>
    TransferLight = 0x8,

    /// <summary>Item is hidden from display.</summary>
    NoDisplay = 0x10,

    /// <summary>Item cannot be dropped.</summary>
    NoDrop = 0x20,

    /// <summary>Item is hexed and cannot be unwielded.</summary>
    Hexed = 0x40,

    /// <summary>Item can be activated via the action box.</summary>
    CanUseBox = 0x80,

    /// <summary>Item requires a target to use.</summary>
    NeedsTarget = 0x100,

    /// <summary>Item emits a small light.</summary>
    LightSmall = 0x200,

    /// <summary>Item emits a medium light.</summary>
    LightMedium = 0x400,

    /// <summary>Item emits a large light.</summary>
    LightLarge = 0x800,

    /// <summary>Item emits an extra-large light.</summary>
    LightXLarge = 0x1000,

    /// <summary>Item persists after death (not dropped on critter death).</summary>
    Persistent = 0x2000,

    /// <summary>Item triggers a magic/tech interaction.</summary>
    MtTriggered = 0x4000,

    /// <summary>Item was stolen.</summary>
    Stolen = 0x8000,

    /// <summary>Using the item throws it.</summary>
    UseIsThrow = 0x10000,

    /// <summary>Item does not decay over time.</summary>
    NoDecay = 0x20000,

    /// <summary>Item is uber-quality.</summary>
    Uber = 0x40000,

    /// <summary>NPCs cannot pick up this item.</summary>
    NoNpcPickup = 0x80000,

    /// <summary>Item cannot be used at range.</summary>
    NoRangedUse = 0x100000,

    /// <summary>AI action for this item is valid.</summary>
    ValidAiAction = 0x200000,

    /// <summary>Item was inserted via multiplayer.</summary>
    MpInserted = 0x400000,
}
