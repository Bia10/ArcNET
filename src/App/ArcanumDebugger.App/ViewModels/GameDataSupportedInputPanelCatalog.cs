using System.Globalization;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;

namespace ArcanumDebugger.App.ViewModels;

public sealed record class GameDataSupportedInputPanelState(
    string TitleText,
    string SummaryText,
    string EmptyStateText,
    string BrowseButtonText,
    string FilterPlaceholderText,
    IReadOnlyList<GameDataQuickPickEntry> Entries
)
{
    public bool HasEntries => Entries.Count != 0;

    public bool ShowEmptyState => !HasEntries && !string.IsNullOrWhiteSpace(EmptyStateText);

    public bool ShowSummary => !string.IsNullOrWhiteSpace(SummaryText) && !ShowEmptyState;
}

public static class GameDataSupportedInputPanelCatalog
{
    public static GameDataSupportedInputPanelState CreateLookupState(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText
    )
    {
        var trimmedFilter = filterText?.Trim() ?? string.Empty;
        var hasCatalogData = prototypeEntries.Count != 0 || staticObjectEntries.Count != 0;
        var entries = GameDataQuickPickCatalog.BuildLookupTokenEntries(
            prototypeEntries,
            staticObjectEntries,
            trimmedFilter,
            DefaultMaxEntries
        );
        return CreateSearchState(
            "Supported Lookup Inputs",
            "Open Full Catalog",
            trimmedFilter,
            hasCatalogData,
            entries,
            "Load the local workspace catalog to search known prototype tokens, object ids, and GUIDs here.",
            "Type part of a prototype name, proto number, object id, or GUID to surface known-good lookup tokens here.",
            "No lookup token suggestions yet. Type part of a local object name or open the full browser.",
            "Search lookup names, proto numbers, GUIDs, or object ids",
            "supported lookup tokens",
            "supported lookup match(es)"
        );
    }

    public static GameDataSupportedInputPanelState CreateInventoryState(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText
    )
    {
        var trimmedFilter = filterText?.Trim() ?? string.Empty;
        var hasCatalogData = prototypeEntries.Count != 0 || staticObjectEntries.Count != 0;
        var entries = GameDataQuickPickCatalog.BuildInventoryTokenEntries(
            prototypeEntries,
            staticObjectEntries,
            trimmedFilter,
            DefaultMaxEntries
        );
        return CreateSearchState(
            "Supported Inventory Inputs",
            "Open Full Catalog",
            trimmedFilter,
            hasCatalogData,
            entries,
            "Load the local workspace catalog to search supported item prototypes and placed item sources here.",
            "Type part of an item name, proto number, object id, or GUID to surface supported inventory create inputs here.",
            "No inventory suggestions yet. Type part of a local item name or open the full browser.",
            "Search item names, proto numbers, GUIDs, or object ids",
            "supported inventory inputs",
            "supported inventory match(es)"
        );
    }

    public static GameDataSupportedInputPanelState CreateSpawnState(
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries,
        IReadOnlyList<StaticObjectCatalogEntry> staticObjectEntries,
        string? filterText
    )
    {
        var trimmedFilter = filterText?.Trim() ?? string.Empty;
        var hasCatalogData = prototypeEntries.Count != 0 || staticObjectEntries.Count != 0;
        var entries = GameDataQuickPickCatalog.BuildSpawnTokenEntries(
            prototypeEntries,
            staticObjectEntries,
            trimmedFilter,
            DefaultMaxEntries
        );
        return CreateSearchState(
            "Supported Spawn Inputs",
            "Open Full Catalog",
            trimmedFilter,
            hasCatalogData,
            entries,
            "Load the local workspace catalog to search supported mobiles, world objects, and placed spawn sources here.",
            "Type part of a mobile name, object name, proto number, object id, or GUID to surface supported spawn inputs here.",
            "No spawn suggestions yet. Type part of a local mobile or object name or open the full browser.",
            "Search mobile names, object names, proto numbers, GUIDs, or object ids",
            "supported spawn inputs",
            "supported spawn match(es)"
        );
    }

    public static GameDataSupportedInputPanelState CreateWorldLocationState(
        IReadOnlyList<WorldMapCatalogEntry> worldEntries,
        string? filterText = null
    )
    {
        var trimmedFilter = filterText?.Trim() ?? string.Empty;
        var hasCatalogData = worldEntries.Count != 0;
        var entries = GameDataQuickPickCatalog.BuildWorldLocationEntries(
            worldEntries,
            trimmedFilter,
            DefaultMaxEntries
        );
        return CreateSearchState(
            "Known World Locations",
            "Open Full Catalog",
            trimmedFilter,
            hasCatalogData,
            entries,
            "Load the local workspace catalog to browse known world-map destinations here.",
            "Pick one known world-map destination here or open the full browser to search the whole local area list.",
            "No world-map destinations are available yet. Open the full browser to reload the local area list.",
            "Search location names, map ids, area ids, or map names",
            "known world-map locations",
            "known world-map match(es)"
        );
    }

    private static GameDataSupportedInputPanelState CreateSearchState(
        string titleText,
        string browseButtonText,
        string trimmedFilter,
        bool hasCatalogData,
        IReadOnlyList<GameDataQuickPickEntry> entries,
        string unavailableSummary,
        string blankSummary,
        string blankEmptyState,
        string filterPlaceholderText,
        string noMatchLabel,
        string matchLabel
    )
    {
        if (!hasCatalogData)
            return new(titleText, unavailableSummary, unavailableSummary, browseButtonText, filterPlaceholderText, []);

        if (trimmedFilter.Length == 0)
        {
            if (entries.Count == 0)
                return new(titleText, blankSummary, blankEmptyState, browseButtonText, filterPlaceholderText, []);

            return new(titleText, string.Empty, string.Empty, browseButtonText, filterPlaceholderText, entries);
        }

        if (entries.Count == 0)
        {
            var noMatchText = $"No {noMatchLabel} matched '{trimmedFilter}' in the local workspace.";
            return new(titleText, noMatchText, noMatchText, browseButtonText, filterPlaceholderText, []);
        }

        var matchCountText = entries.Count.ToString(CultureInfo.InvariantCulture);
        return new(
            titleText,
            $"Showing {matchCountText} {matchLabel} for '{trimmedFilter}'.",
            string.Empty,
            browseButtonText,
            filterPlaceholderText,
            entries
        );
    }

    private const int DefaultMaxEntries = 6;
}
