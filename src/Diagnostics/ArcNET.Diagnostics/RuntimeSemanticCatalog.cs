namespace ArcNET.Diagnostics;

public static class RuntimeSemanticCatalog
{
    public static string StatName(int stat) =>
        stat >= 0 && stat < s_statNames.Length ? s_statNames[stat] : $"Stat[{stat}]";

    public static string AttachmentPointName(int attachmentPoint) =>
        attachmentPoint >= 0 && attachmentPoint < s_attachmentPointNames.Length
            ? s_attachmentPointNames[attachmentPoint]
            : $"AttachmentPoint[{attachmentPoint}]";

    public static string InventoryLocationName(int inventoryLocation) =>
        s_inventoryLocationNames.TryGetValue(inventoryLocation, out var name)
            ? name
            : $"InventoryLocation[{inventoryLocation}]";

    public static string InventoryLocationContext(int inventoryLocation) =>
        inventoryLocation is >= 1000 and <= 1008 ? "Equipment" : "ContainerOrGeneralInventory";

    public static string FormatHandle(ulong handle) => handle == 0 ? "null" : $"0x{handle:X16}";

    public static bool LooksLikeObjectHandle(ulong handle) =>
        (handle & RuntimeOffsets.ObjHandleMarkerMask) == RuntimeOffsets.ObjHandleMarkerValue;

    private static readonly string[] s_statNames =
    [
        "Strength",
        "Dexterity",
        "Constitution",
        "Beauty",
        "Intelligence",
        "Perception",
        "Willpower",
        "Charisma",
        "CarryWeight",
        "DamageBonus",
        "AcAdjustment",
        "Speed",
        "HealRate",
        "PoisonRecovery",
        "ReactionModifier",
        "MaxFollowers",
        "MagickTechAptitude",
        "Level",
        "ExperiencePoints",
        "Alignment",
        "FatePoints",
        "UnspentPoints",
        "MagickPoints",
        "TechPoints",
        "PoisonLevel",
        "Age",
        "Gender",
        "Race",
    ];

    private static readonly string[] s_attachmentPointNames =
    [
        "Examine",
        "Use",
        "Destroy",
        "Unlock",
        "Get",
        "Drop",
        "Throw",
        "Hit",
        "Miss",
        "Dialog",
        "FirstHeartbeat",
        "CatchingThiefPc",
        "Dying",
        "EnterCombat",
        "ExitCombat",
        "StartCombat",
        "EndCombat",
        "BuyObject",
        "Resurrect",
        "Heartbeat",
        "LeaderKilling",
        "InsertItem",
        "WillKos",
        "TakingDamage",
        "WieldOn",
        "WieldOff",
        "CritterHits",
        "NewSector",
        "RemoveItem",
        "LeaderSleeping",
        "Bust",
        "DialogOverride",
        "Transfer",
        "CaughtThief",
        "CriticalHit",
        "CriticalMiss",
    ];

    private static readonly Dictionary<int, string> s_inventoryLocationNames = new()
    {
        [1000] = "Helmet",
        [1001] = "RingLeft",
        [1002] = "RingRight",
        [1003] = "Medallion",
        [1004] = "Weapon",
        [1005] = "Shield",
        [1006] = "Armor",
        [1007] = "Gauntlet",
        [1008] = "Boots",
    };
}
