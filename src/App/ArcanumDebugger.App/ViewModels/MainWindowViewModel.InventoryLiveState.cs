using System.Linq;
using ArcNET.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private const int MaxInventoryProbeItems = 24;
    private int _inventoryInspectionVersion;
    private string? _pendingInventoryItemHandle;

    [ObservableProperty]
    private string inventoryOwnerStatusText =
        "Load the live roster to browse the player and companion inventories from one place.";

    [ObservableProperty]
    private IReadOnlyList<InventoryOwnerEntry> inventoryOwnerEntries =
    [
        new("player", "Player", "player", "Active player inventory.", true),
    ];

    [ObservableProperty]
    private InventoryOwnerEntry? selectedInventoryOwnerEntry = new(
        "player",
        "Player",
        "player",
        "Active player inventory.",
        true
    );

    [ObservableProperty]
    private string inventoryLiveStatusText = "Select a player or companion to inspect live items.";

    [ObservableProperty]
    private string inventoryLiveSummaryText = "Live inventory items will appear here.";

    [ObservableProperty]
    private IReadOnlyList<InventoryLiveItemEntry> inventoryLiveItemEntries = [];

    [ObservableProperty]
    private InventoryLiveItemEntry? selectedInventoryLiveItemEntry;

    [ObservableProperty]
    private bool canRefreshInventoryLiveState;

    [ObservableProperty]
    private bool canInspectSelectedInventoryLiveItem;

    [ObservableProperty]
    private bool canUseSelectedInventoryLiveItemHandle;

    partial void OnSelectedInventoryOwnerEntryChanged(InventoryOwnerEntry? value)
    {
        if (
            value is not null
            && !InventoryOwnerTokenText.Equals(value.HandleTokenText, StringComparison.OrdinalIgnoreCase)
        )
            InventoryOwnerTokenText = value.HandleTokenText;

        RefreshInventoryLiveActions();
        QueueRefreshInventoryInspection();
    }

    partial void OnSelectedInventoryLiveItemEntryChanged(InventoryLiveItemEntry? value)
    {
        if (value is not null && !InventoryItemHandleText.Equals(value.HandleText, StringComparison.OrdinalIgnoreCase))
            InventoryItemHandleText = value.HandleText;

        RefreshInventoryLiveActions();
    }

    [RelayCommand]
    private async Task RefreshInventoryLiveState()
    {
        await RefreshInventoryInspectionAsync(force: true);
    }

    [RelayCommand]
    private async Task InspectSelectedInventoryLiveItem()
    {
        if (SelectedInventoryLiveItemEntry is not { } item)
            return;

        ObjectProbeHandleText = item.HandleText;
        await InspectObjectHandle();
    }

    [RelayCommand]
    private void UseSelectedInventoryLiveItemHandle()
    {
        if (SelectedInventoryLiveItemEntry is not { } item)
            return;

        InventoryItemHandleText = item.HandleText;
    }

    private void RefreshInventoryOwnerEntries()
    {
        var selectedToken = SelectedInventoryOwnerEntry?.HandleTokenText ?? InventoryOwnerTokenText;
        InventoryOwnerEntries = InventoryLiveCatalog.CreateOwnerEntries(_mobileRosterCache);
        SyncSelectedInventoryOwnerEntry(selectedToken);
        InventoryOwnerStatusText =
            _mobileRosterCache.Count == 0
                ? "Player inventory is ready. Refresh the live roster to add companions and other mobiles."
                : $"{InventoryOwnerEntries.Count.ToString()} owner targets are available, including the player and live roster entries.";
        RefreshInventoryLiveActions();
    }

    private void SyncSelectedInventoryOwnerEntry(string? token)
    {
        var trimmedToken = token?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedToken))
        {
            if (SelectedInventoryOwnerEntry is not null)
                SelectedInventoryOwnerEntry = null;

            return;
        }

        var matchingEntry = InventoryOwnerEntries.FirstOrDefault(entry =>
            entry.HandleTokenText.Equals(trimmedToken, StringComparison.OrdinalIgnoreCase)
        );
        matchingEntry ??= InventoryOwnerEntries.FirstOrDefault(entry =>
            trimmedToken.Equals("player", StringComparison.OrdinalIgnoreCase) && entry.IsPlayer
        );
        if (EqualityComparer<InventoryOwnerEntry?>.Default.Equals(SelectedInventoryOwnerEntry, matchingEntry))
            return;

        SelectedInventoryOwnerEntry = matchingEntry;
    }

    private void QueueRefreshInventoryInspection() => _ = RefreshInventoryInspectionAsync();

    private async Task RefreshInventoryInspectionAsync(bool force = false)
    {
        var requestVersion = ++_inventoryInspectionVersion;
        if (ActiveSession is not { } session)
        {
            if (requestVersion == _inventoryInspectionVersion)
                ApplyDormantInventoryInspection(
                    "No active session",
                    "Attach to a live runtime before reading inventories."
                );

            return;
        }

        if (!CanProbeStructuredState(session))
        {
            if (requestVersion == _inventoryInspectionVersion)
                ApplyDormantInventoryInspection(
                    "Inventory view unavailable",
                    CreateObjectProbeAvailabilitySummary(session)
                );

            return;
        }

        var ownerToken = SelectedInventoryOwnerEntry?.HandleTokenText ?? InventoryOwnerTokenText.Trim();
        if (string.IsNullOrWhiteSpace(ownerToken))
        {
            if (requestVersion == _inventoryInspectionVersion)
                ApplyDormantInventoryInspection(
                    "Owner required",
                    "Select the player or one companion before reading live items."
                );

            return;
        }

        if (!force && requestVersion != _inventoryInspectionVersion)
            return;

        InventoryLiveStatusText = "Reading live inventory";
        InventoryLiveSummaryText = $"Loading items for {ownerToken}.";

        try
        {
            var ownerSnapshot = await Task.Run(() =>
                _objectProbeService.Inspect(new ObjectProbeRequest(session, [ownerToken], "inventory owner", 1))
            );
            if (requestVersion != _inventoryInspectionVersion)
                return;

            if (!ownerSnapshot.IsAvailable || ownerSnapshot.Objects.Count == 0)
            {
                ApplyDormantInventoryInspection(ownerSnapshot.Status, ownerSnapshot.Summary);
                return;
            }

            var ownerObject = ownerSnapshot.Objects[0];
            var inventoryHandleRows = InventoryLiveCatalog.ExtractInventoryHandleRows(ownerObject);
            if (inventoryHandleRows.Count == 0)
            {
                ApplyInventoryInspection(ownerObject, [], 0);
                return;
            }

            var handleTexts = inventoryHandleRows
                .Take(MaxInventoryProbeItems)
                .Select(static row => row.HandleText)
                .ToArray();
            var itemSnapshot = await Task.Run(() =>
                _objectProbeService.Inspect(
                    new ObjectProbeRequest(session, handleTexts, $"{ownerToken} inventory", handleTexts.Length)
                )
            );
            if (requestVersion != _inventoryInspectionVersion)
                return;

            var items = InventoryLiveCatalog.CreateItemEntries(
                ownerObject,
                itemSnapshot.Objects,
                MaxInventoryProbeItems,
                out var totalHandleCount
            );
            ApplyInventoryInspection(ownerObject, items, totalHandleCount);
        }
        catch (Exception ex)
        {
            if (requestVersion == _inventoryInspectionVersion)
                ApplyDormantInventoryInspection("Inventory read failed", ex.Message);
        }
    }

    private void ApplyInventoryInspection(
        ObjectProbeObjectSnapshot owner,
        IReadOnlyList<InventoryLiveItemEntry> items,
        int totalHandleCount
    )
    {
        InventoryLiveStatusText = totalHandleCount == 0 ? "Inventory empty" : "Inventory loaded";
        var shownCount = items.Count;
        InventoryLiveSummaryText = totalHandleCount switch
        {
            0 => $"{owner.ObjectTypeText} has no live inventory links right now.",
            > MaxInventoryProbeItems =>
                $"Showing {shownCount.ToString()} of {totalHandleCount.ToString()} live items for {owner.ObjectIdText}.",
            _ => $"Showing {shownCount.ToString()} live item(s) for {owner.ObjectIdText}.",
        };
        var selectedHandle = _pendingInventoryItemHandle ?? SelectedInventoryLiveItemEntry?.HandleText;
        InventoryLiveItemEntries = items;
        if (!string.IsNullOrWhiteSpace(selectedHandle))
        {
            var selectedItem = items.FirstOrDefault(entry =>
                entry.HandleText.Equals(selectedHandle, StringComparison.OrdinalIgnoreCase)
            );
            if (selectedItem is not null)
            {
                SelectedInventoryLiveItemEntry = selectedItem;
                _pendingInventoryItemHandle = null;
                RefreshInventoryLiveActions();
                return;
            }
        }

        SelectedInventoryLiveItemEntry = items.FirstOrDefault();
        _pendingInventoryItemHandle = null;
        RefreshInventoryLiveActions();
    }

    private void ApplyDormantInventoryInspection(string status, string summary)
    {
        InventoryLiveStatusText = status;
        InventoryLiveSummaryText = summary;
        InventoryLiveItemEntries = [];
        SelectedInventoryLiveItemEntry = null;
        RefreshInventoryLiveActions();
    }

    private void RefreshInventoryLiveActions()
    {
        var canReadInventory = ActiveSession is { } session && CanProbeStructuredState(session);
        CanRefreshInventoryLiveState = canReadInventory && !string.IsNullOrWhiteSpace(InventoryOwnerTokenText);
        CanInspectSelectedInventoryLiveItem = canReadInventory && SelectedInventoryLiveItemEntry is not null;
        CanUseSelectedInventoryLiveItemHandle = SelectedInventoryLiveItemEntry is not null;
    }
}
