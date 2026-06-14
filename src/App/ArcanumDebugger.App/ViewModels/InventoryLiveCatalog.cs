using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;

namespace ArcanumDebugger.App.ViewModels;

public sealed record class InventoryOwnerEntry(
    string EntryKey,
    string DisplayText,
    string HandleTokenText,
    string SummaryText,
    bool IsPlayer
)
{
    public string RoleText => IsPlayer ? "Player" : "Companion";

    public override string ToString() => DisplayText;
}

public sealed record class InventoryLiveItemEntry(
    string EntryKey,
    string SlotLabelText,
    string DisplayText,
    string HandleText,
    string SummaryText,
    string PrototypeText
)
{
    public override string ToString() => DisplayText;
}

public static class InventoryLiveCatalog
{
    public static IReadOnlyList<InventoryOwnerEntry> CreateOwnerEntries(
        IReadOnlyList<MobileRosterEntrySnapshot> mobiles
    )
    {
        ArgumentNullException.ThrowIfNull(mobiles);

        List<InventoryOwnerEntry> entries = [new("player", "Player", "player", "Active player inventory.", true)];
        entries.AddRange(
            mobiles.Select(static mobile => new InventoryOwnerEntry(
                $"mobile:{mobile.HandleHex}",
                mobile.DisplayText,
                mobile.HandleHex,
                $"{mobile.ObjectTypeText} · {mobile.PrototypeText}",
                false
            ))
        );
        return entries;
    }

    public static IReadOnlyList<InventoryLiveItemEntry> CreateItemEntries(
        ObjectProbeObjectSnapshot owner,
        IReadOnlyList<ObjectProbeObjectSnapshot> items,
        int maxEntries,
        out int totalHandleCount
    )
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(items);

        var handleRows = ExtractInventoryHandleRows(owner);
        totalHandleCount = handleRows.Count;
        if (handleRows.Count == 0)
            return [];

        var limitedRows = handleRows.Take(maxEntries).ToArray();
        var itemsByHandle = items.ToDictionary(static item => item.HandleHex, StringComparer.OrdinalIgnoreCase);

        List<InventoryLiveItemEntry> entries = [];
        foreach (var row in limitedRows)
        {
            if (!itemsByHandle.TryGetValue(row.HandleText, out var item))
            {
                entries.Add(
                    new InventoryLiveItemEntry(
                        row.HandleText,
                        row.SlotLabelText,
                        row.HandleText,
                        row.HandleText,
                        "Item header unavailable.",
                        string.Empty
                    )
                );
                continue;
            }

            var displayText = item.ObjectIdText.StartsWith("No decoded", StringComparison.OrdinalIgnoreCase)
                ? item.ObjectTypeText
                : item.ObjectIdText;
            entries.Add(
                new InventoryLiveItemEntry(
                    item.HandleHex,
                    row.SlotLabelText,
                    displayText,
                    item.HandleHex,
                    item.StatusText,
                    item.PrototypeText
                )
            );
        }

        return entries;
    }

    public static IReadOnlyList<InventoryHandleRow> ExtractInventoryHandleRows(ObjectProbeObjectSnapshot owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var section = owner.Sections.FirstOrDefault(static probeSection =>
            string.Equals(probeSection.Key, "inventory_links", StringComparison.OrdinalIgnoreCase)
        );
        if (section is null)
            return [];

        List<InventoryHandleRow> rows = [];
        foreach (var detail in section.Details)
        {
            if (!TryParseHandleText(detail.Value, out var handle))
                continue;

            rows.Add(new InventoryHandleRow(detail.Label, RuntimeSemanticCatalog.FormatHandle(handle)));
        }

        return rows;
    }

    private static bool TryParseHandleText(string? value, out ulong handle)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            handle = 0;
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out handle);

        return ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out handle);
    }
}

public readonly record struct InventoryHandleRow(string SlotLabelText, string HandleText);
