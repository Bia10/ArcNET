namespace ArcNET.GameObjects.Classes;

/// <summary>One item entry in an inventory loot table.</summary>
public sealed record InventorySourceEntry(int PrototypeId, double DropChance);

/// <summary>
/// A loot table mapping prototype IDs to drop chances.
/// Loaded from the TDF inventory-source database.
/// </summary>
public sealed class InventorySource
{
    /// <summary>Table identifier (0 is reserved/empty).</summary>
    public int Id { get; set; }

    /// <summary>Human-readable table name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Entries in this table.</summary>
    public List<InventorySourceEntry> Entries { get; set; } = [];
}
