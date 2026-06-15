namespace ArcNET.GameObjects.Metadata;

/// <summary>
/// Stable labels for game-object runtime slots such as script attachment points and inventory locations.
/// </summary>
public static class GameObjectRuntimeMetadata
{
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
