namespace ArcNET.GameObjects.Classes;

/// <summary>One item entry in an NPC merchant buy list.</summary>
public sealed record InventorySourceBuyEntry(int PrototypeId);

/// <summary>
/// A buy list that describes which prototype items an NPC merchant will purchase.
/// Loaded from the TDF inventory-source-buy database.
/// </summary>
public sealed class InventorySourceBuy
{
    /// <summary>Buy-list identifier (0 is reserved/empty).</summary>
    public int Id { get; set; }

    /// <summary>Human-readable buy-list name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Entries in this buy list.</summary>
    public List<InventorySourceBuyEntry> Entries { get; set; } = [];
}
