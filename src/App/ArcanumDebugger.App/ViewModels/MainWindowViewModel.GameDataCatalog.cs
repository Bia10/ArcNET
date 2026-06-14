using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using ArcNET.GameData.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly GameDataCatalogService _gameDataCatalogService;
    private IReadOnlyList<PrototypePaletteEntry> _gameDataCatalogPrototypeCache = [];
    private IReadOnlyList<WorldMapCatalogEntry> _gameDataCatalogWorldCache = [];
    private IReadOnlyList<TileArtCatalogEntry> _gameDataCatalogTileArtCache = [];
    private IReadOnlyList<StaticObjectCatalogEntry> _gameDataCatalogStaticObjectCache = [];
    private GameDataQuickPickMode _gameDataQuickPickMode;
    private string? _gameDataCatalogModulePath;
    private bool _gameDataCatalogLoadInFlight;
    private const int ReadRootTabIndex = 1;
    private const int ActRootTabIndex = 4;
    private const int InspectRootTabIndex = 5;
    private const int RuntimeReadTabIndex = 1;
    private const int GuidedActTabIndex = 0;
    private const int InventoryActTabIndex = 1;
    private const int MobilesActTabIndex = 2;
    private const int MaxTileArtCatalogResults = 512;
    private const int MaxStaticObjectCatalogResults = 512;

    [ObservableProperty]
    private int selectedActTabIndex;

    [ObservableProperty]
    private int selectedGameDataCatalogTabIndex;

    [ObservableProperty]
    private string gameDataCatalogStatusText = "Game-data catalog not loaded.";

    [ObservableProperty]
    private string gameDataCatalogSummaryText =
        "Load the local ArcNET workspace catalog to browse prototypes, world-map destinations, tile-art ids, and placed object ids from the installed game data, then feed those supported values straight into teleport, inventory, lookup, and spawn flows.";

    [ObservableProperty]
    private string gameDataCatalogPresetSourceText = "Local workspace source inactive.";

    [ObservableProperty]
    private string gameDataCatalogPresetSummaryText =
        "Attach to a live runtime to load supported prototype, item, schematic, and world selections from the active local workspace.";

    [ObservableProperty]
    private string gameDataCatalogStagedInputText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogStagedInputSummaryText =
        "Stage one supported input from the local workspace here when you want a known-good token, coordinates pair, or art id ready to reuse elsewhere.";

    [ObservableProperty]
    private string gameDataCatalogPrototypeFilterText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogWorldFilterText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogTileArtFilterText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogStaticObjectFilterText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> gameDataCatalogPrototypeScopeOptions =
    [
        new("all", "All prototypes", "Browse every prototype from the loaded local workspace."),
        new("items", "Inventory items", "Show item-like prototypes that fit the inventory create flow."),
        new("mobiles", "Mobiles", "Show PC and NPC prototypes that fit the live critter spawn flow."),
        new("objects", "World objects", "Show scenery, containers, portals, walls, traps, and similar placed objects."),
    ];

    [ObservableProperty]
    private DebuggerChoiceOption? selectedGameDataCatalogPrototypeScopeOption = new(
        "all",
        "All prototypes",
        "Browse every prototype from the loaded local workspace."
    );

    [ObservableProperty]
    private string gameDataCatalogPrototypeScopeDescriptionText =
        "Browse every prototype from the loaded local workspace.";

    [ObservableProperty]
    private IReadOnlyList<PrototypePaletteEntry> gameDataCatalogPrototypeEntries = [];

    [ObservableProperty]
    private PrototypePaletteEntry? selectedGameDataCatalogPrototypeEntry;

    [ObservableProperty]
    private IReadOnlyList<WorldMapCatalogEntry> gameDataCatalogWorldEntries = [];

    [ObservableProperty]
    private WorldMapCatalogEntry? selectedGameDataCatalogWorldEntry;

    [ObservableProperty]
    private IReadOnlyList<TileArtCatalogEntry> gameDataCatalogTileArtEntries = [];

    [ObservableProperty]
    private TileArtCatalogEntry? selectedGameDataCatalogTileArtEntry;

    [ObservableProperty]
    private string gameDataCatalogSelectedTileArtValueText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogSelectedTileArtSummaryText =
        "Select one tile-art row to inspect its canonical art id.";

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedGameDataCatalogTileArtValueEntry;

    [ObservableProperty]
    private IReadOnlyList<StaticObjectCatalogEntry> gameDataCatalogStaticObjectEntries = [];

    [ObservableProperty]
    private StaticObjectCatalogEntry? selectedGameDataCatalogStaticObjectEntry;

    [ObservableProperty]
    private string gameDataCatalogSelectedStaticObjectGuidText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogSelectedStaticObjectIdText = string.Empty;

    [ObservableProperty]
    private string gameDataCatalogSelectedStaticObjectSummaryText =
        "Select one static object row to inspect its source path, object id, GUID, and prototype context.";

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedGameDataCatalogStaticObjectValueEntry;

    [ObservableProperty]
    private string gameDataQuickPickTitleText = "Local game-data picker";

    [ObservableProperty]
    private string gameDataQuickPickSummaryText =
        "Open one picker to browse workspace-backed prototypes, world-map destinations, inventory items, placed objects, or spawnable entries from the local ArcNET workspace.";

    [ObservableProperty]
    private string gameDataQuickPickFilterText = string.Empty;

    [ObservableProperty]
    private string gameDataQuickPickFilterPlaceholderText = "Filter local game data";

    [ObservableProperty]
    private string gameDataQuickPickApplyButtonText = "Use Selection";

    [ObservableProperty]
    private IReadOnlyList<GameDataQuickPickEntry> gameDataQuickPickEntries = [];

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedGameDataQuickPickEntry;

    [ObservableProperty]
    private GameDataSupportedInputPanelState lookupSupportedInputPanelState =
        GameDataSupportedInputPanelCatalog.CreateLookupState([], [], string.Empty);

    [ObservableProperty]
    private string lookupSupportedInputFilterText = string.Empty;

    [ObservableProperty]
    private GameDataSupportedInputPanelState guidedActionWorldSupportedInputPanelState =
        GameDataSupportedInputPanelCatalog.CreateWorldLocationState([]);

    [ObservableProperty]
    private string guidedActionWorldSupportedInputFilterText = string.Empty;

    [ObservableProperty]
    private GameDataSupportedInputPanelState inventorySupportedInputPanelState =
        GameDataSupportedInputPanelCatalog.CreateInventoryState([], [], string.Empty);

    [ObservableProperty]
    private string inventorySupportedInputFilterText = string.Empty;

    [ObservableProperty]
    private GameDataSupportedInputPanelState spawnSupportedInputPanelState =
        GameDataSupportedInputPanelCatalog.CreateSpawnState([], [], string.Empty);

    [ObservableProperty]
    private string spawnSupportedInputFilterText = string.Empty;

    [ObservableProperty]
    private bool guidedActionWorldSupportedInputPanelVisible = true;

    [ObservableProperty]
    private bool lookupQuickPickVisible;

    [ObservableProperty]
    private bool guidedActionQuickPickVisible;

    [ObservableProperty]
    private bool inventoryQuickPickVisible;

    [ObservableProperty]
    private bool spawnQuickPickVisible;

    [ObservableProperty]
    private bool spellTechQuickPickVisible;

    [ObservableProperty]
    private bool canApplySelectedGameDataQuickPick;

    [ObservableProperty]
    private bool canLoadGameDataCatalog;

    [ObservableProperty]
    private bool canUseSelectedCatalogPrototypeForLookup;

    [ObservableProperty]
    private bool canUseSelectedCatalogPrototypeForInventory;

    [ObservableProperty]
    private bool canUseSelectedCatalogPrototypeForSpawn;

    [ObservableProperty]
    private bool canUseSelectedCatalogWorldLocationForGuidedAction;

    [ObservableProperty]
    private bool canUseSelectedCatalogStaticObjectPrototypeForLookup;

    [ObservableProperty]
    private bool canUseSelectedCatalogStaticObjectPrototypeForInventory;

    [ObservableProperty]
    private bool canUseSelectedCatalogStaticObjectPrototypeForSpawn;

    partial void OnGameDataCatalogPrototypeFilterTextChanged(string value) => ApplyFilteredGameDataCatalog();

    partial void OnGameDataCatalogWorldFilterTextChanged(string value) => ApplyFilteredGameDataCatalog();

    partial void OnGameDataCatalogTileArtFilterTextChanged(string value) => ApplyFilteredGameDataCatalog();

    partial void OnGameDataCatalogStaticObjectFilterTextChanged(string value) => ApplyFilteredGameDataCatalog();

    partial void OnGameDataQuickPickFilterTextChanged(string value) => ApplyGameDataQuickPickFilter();

    partial void OnLookupSupportedInputFilterTextChanged(string value) => RefreshSupportedInputPanels();

    partial void OnGuidedActionWorldSupportedInputFilterTextChanged(string value) => RefreshSupportedInputPanels();

    partial void OnInventorySupportedInputFilterTextChanged(string value) => RefreshSupportedInputPanels();

    partial void OnSpawnSupportedInputFilterTextChanged(string value) => RefreshSupportedInputPanels();

    partial void OnSelectedGameDataCatalogPrototypeScopeOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is not null)
            GameDataCatalogPrototypeScopeDescriptionText = value.Description;

        ApplyFilteredGameDataCatalog();
    }

    partial void OnSelectedGameDataCatalogPrototypeEntryChanged(PrototypePaletteEntry? value) =>
        RefreshGameDataCatalogActions();

    partial void OnSelectedGameDataCatalogWorldEntryChanged(WorldMapCatalogEntry? value) =>
        RefreshGameDataCatalogActions();

    partial void OnSelectedGameDataCatalogTileArtEntryChanged(TileArtCatalogEntry? value)
    {
        GameDataCatalogSelectedTileArtValueText = value?.ArtIdText ?? string.Empty;
        GameDataCatalogSelectedTileArtSummaryText = value is null
            ? "Select one tile-art row to inspect its canonical art id."
            : value.SummaryText;
        SelectedGameDataCatalogTileArtValueEntry = value is null
            ? null
            : GameDataQuickPickCatalog.CreateCatalogEntry(value);
        RefreshGameDataCatalogActions();
    }

    partial void OnSelectedGameDataCatalogStaticObjectEntryChanged(StaticObjectCatalogEntry? value)
    {
        GameDataCatalogSelectedStaticObjectGuidText = value?.ObjectGuidText ?? string.Empty;
        GameDataCatalogSelectedStaticObjectIdText = value?.ObjectIdText ?? string.Empty;
        GameDataCatalogSelectedStaticObjectSummaryText = value is null
            ? "Select one static object row to inspect its source path, object id, GUID, and prototype context."
            : $"{value.SummaryText} - {value.LocationText}";
        SelectedGameDataCatalogStaticObjectValueEntry = value is null
            ? null
            : GameDataQuickPickCatalog.CreateCatalogEntry(value);
        RefreshGameDataCatalogActions();
    }

    partial void OnSelectedGameDataQuickPickEntryChanged(GameDataQuickPickEntry? value) =>
        RefreshGameDataQuickPickActions();

    [RelayCommand]
    private async Task LoadGameDataCatalog()
    {
        await EnsureGameDataCatalogLoadedAsync(forceReload: true);
    }

    [RelayCommand]
    private async Task BrowseInventoryPrototypeCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.InventoryPrototype,
            CreatePrototypeCatalogFilterSeed(InventoryPrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseInventoryStaticObjectCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.InventoryStaticObject,
            CreatePrototypeCatalogFilterSeed(InventoryPrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseInventorySupportedCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.InventoryToken,
            CreatePrototypeCatalogFilterSeed(InventoryPrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseLookupPrototypeCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.LookupPrototype,
            CreatePrototypeCatalogFilterSeed(PrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseLookupStaticObjectCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.LookupStaticObject,
            CreatePrototypeCatalogFilterSeed(PrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseLookupSupportedCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.LookupToken,
            CreatePrototypeCatalogFilterSeed(PrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseSpawnPrototypeCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.SpawnPrototype,
            CreatePrototypeCatalogFilterSeed(MobileSpawnPrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseSpawnStaticObjectCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.SpawnStaticObject,
            CreatePrototypeCatalogFilterSeed(MobileSpawnPrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseSpawnSupportedCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.SpawnToken,
            CreatePrototypeCatalogFilterSeed(MobileSpawnPrototypeTokenText)
        );
    }

    [RelayCommand]
    private async Task BrowseGuidedActionWorldCatalog()
    {
        if (!string.Equals(SelectedGuidedActionOption?.Key, "teleport_traveler", StringComparison.OrdinalIgnoreCase))
            SelectedGuidedActionOption = GuidedActionCatalog.GetDescriptor("teleport_traveler");

        await OpenGameDataQuickPickAsync(GameDataQuickPickMode.GuidedWorldLocation, GameDataQuickPickFilterText);
    }

    [RelayCommand]
    private async Task BrowseSpellCatalog()
    {
        await OpenGameDataQuickPickAsync(GameDataQuickPickMode.Spell, SpellTechSpellTokenText);
    }

    [RelayCommand]
    private async Task BrowseSchematicCatalog()
    {
        await OpenGameDataQuickPickAsync(
            GameDataQuickPickMode.Schematic,
            CreatePrototypeCatalogFilterSeed(SpellTechSchematicIdText)
        );
    }

    [RelayCommand]
    private async Task BrowseSpellCollegeCatalog()
    {
        await OpenGameDataQuickPickAsync(GameDataQuickPickMode.SpellCollege, SpellTechCollegeTokenText);
    }

    [RelayCommand]
    private async Task BrowseTechDisciplineCatalog()
    {
        await OpenGameDataQuickPickAsync(GameDataQuickPickMode.TechDiscipline, SpellTechDisciplineTokenText);
    }

    [RelayCommand]
    private async Task BrowseTechSkillCatalog()
    {
        await OpenGameDataQuickPickAsync(GameDataQuickPickMode.TechSkill, SpellTechSkillTokenText);
    }

    [RelayCommand]
    private void ApplySelectedGameDataQuickPick()
    {
        if (SelectedGameDataQuickPickEntry is not { } entry)
            return;

        ApplyGameDataQuickPickValueCore(entry, null);
    }

    [RelayCommand]
    private void ApplyGameDataQuickPickValue(GameDataQuickPickValueOption? option)
    {
        if (SelectedGameDataQuickPickEntry is not { } entry)
            return;

        ApplyGameDataQuickPickValueCore(entry, option?.ValueText);
    }

    private void ApplyGameDataQuickPickValueCore(GameDataQuickPickEntry entry, string? overrideValueText)
    {
        var applyValueText = string.IsNullOrWhiteSpace(overrideValueText) ? entry.ApplyValueText : overrideValueText;
        switch (_gameDataQuickPickMode)
        {
            case GameDataQuickPickMode.LookupToken:
            case GameDataQuickPickMode.LookupPrototype:
            case GameDataQuickPickMode.LookupStaticObject:
                PrototypeTokenText = applyValueText;
                break;

            case GameDataQuickPickMode.InventoryToken:
            case GameDataQuickPickMode.InventoryPrototype:
            case GameDataQuickPickMode.InventoryStaticObject:
                InventoryPrototypeTokenText = applyValueText;
                break;

            case GameDataQuickPickMode.SpawnToken:
            case GameDataQuickPickMode.SpawnPrototype:
            case GameDataQuickPickMode.SpawnStaticObject:
                MobileSpawnPrototypeTokenText = applyValueText;
                break;

            case GameDataQuickPickMode.GuidedWorldLocation
                when entry.WorldX is int worldX && entry.WorldY is int worldY:
                SelectedGuidedActionOption = GuidedActionCatalog.GetDescriptor("teleport_traveler");
                GuidedActionTileXText = worldX.ToString(CultureInfo.InvariantCulture);
                GuidedActionTileYText = worldY.ToString(CultureInfo.InvariantCulture);
                GuidedActionMapIdText = "-1";
                GuidedActionFlagsText = "0";
                break;

            case GameDataQuickPickMode.Spell:
                SpellTechSpellTokenText = applyValueText;
                break;

            case GameDataQuickPickMode.Schematic:
                SpellTechSchematicIdText = applyValueText;
                break;

            case GameDataQuickPickMode.SpellCollege:
                SpellTechCollegeTokenText = applyValueText;
                break;

            case GameDataQuickPickMode.TechDiscipline:
                SpellTechDisciplineTokenText = applyValueText;
                break;

            case GameDataQuickPickMode.TechSkill:
                SpellTechSkillTokenText = applyValueText;
                break;
        }

        CloseGameDataQuickPick();
    }

    [RelayCommand]
    private void ApplyLookupSupportedInput(GameDataQuickPickEntry? entry)
    {
        if (entry is null)
            return;

        PrototypeTokenText = entry.ApplyValueText;
    }

    [RelayCommand]
    private void ApplyLookupSupportedInputValue(GameDataSupportedInputValueChoice? choice)
    {
        if (choice is null)
            return;

        PrototypeTokenText = choice.ValueText;
    }

    [RelayCommand]
    private void ApplyInventorySupportedInput(GameDataQuickPickEntry? entry)
    {
        if (entry is null)
            return;

        InventoryPrototypeTokenText = entry.ApplyValueText;
    }

    [RelayCommand]
    private void ApplyInventorySupportedInputValue(GameDataSupportedInputValueChoice? choice)
    {
        if (choice is null)
            return;

        InventoryPrototypeTokenText = choice.ValueText;
    }

    [RelayCommand]
    private void ApplySpawnSupportedInput(GameDataQuickPickEntry? entry)
    {
        if (entry is null)
            return;

        MobileSpawnPrototypeTokenText = entry.ApplyValueText;
    }

    [RelayCommand]
    private void ApplySpawnSupportedInputValue(GameDataSupportedInputValueChoice? choice)
    {
        if (choice is null)
            return;

        MobileSpawnPrototypeTokenText = choice.ValueText;
    }

    [RelayCommand]
    private void ApplyGuidedActionWorldSupportedInput(GameDataQuickPickEntry? entry)
    {
        if (entry is null || entry.WorldX is not int worldX || entry.WorldY is not int worldY)
            return;

        SelectedGuidedActionOption = GuidedActionCatalog.GetDescriptor("teleport_traveler");
        GuidedActionTileXText = worldX.ToString(CultureInfo.InvariantCulture);
        GuidedActionTileYText = worldY.ToString(CultureInfo.InvariantCulture);
        GuidedActionMapIdText = "-1";
        GuidedActionFlagsText = "0";
    }

    [RelayCommand]
    private void ApplyGuidedActionWorldSupportedInputValue(GameDataSupportedInputValueChoice? choice)
    {
        if (choice is null || choice.Entry.WorldX is not int worldX || choice.Entry.WorldY is not int worldY)
            return;

        SelectedGuidedActionOption = GuidedActionCatalog.GetDescriptor("teleport_traveler");
        GuidedActionTileXText = worldX.ToString(CultureInfo.InvariantCulture);
        GuidedActionTileYText = worldY.ToString(CultureInfo.InvariantCulture);
        GuidedActionMapIdText = "-1";
        GuidedActionFlagsText = "0";
    }

    [RelayCommand]
    private void StageSelectedGameDataCatalogInput(GameDataQuickPickEntry? entry)
    {
        if (entry is null)
            return;

        StageGameDataCatalogInputCore(entry, entry.RecommendedValueOption);
    }

    [RelayCommand]
    private void StageSelectedGameDataCatalogInputValue(GameDataSupportedInputValueChoice? choice)
    {
        if (choice is null)
            return;

        StageGameDataCatalogInputCore(choice.Entry, choice.Option);
    }

    [RelayCommand]
    private void CloseGameDataQuickPick()
    {
        _gameDataQuickPickMode = GameDataQuickPickMode.None;
        LookupQuickPickVisible = false;
        GuidedActionQuickPickVisible = false;
        InventoryQuickPickVisible = false;
        SpawnQuickPickVisible = false;
        SpellTechQuickPickVisible = false;
        GameDataQuickPickTitleText = "Local game-data picker";
        GameDataQuickPickSummaryText =
            "Open one picker to browse workspace-backed prototypes, world-map destinations, inventory items, placed objects, or spawnable entries from the local ArcNET workspace.";
        GameDataQuickPickFilterPlaceholderText = "Filter local game data";
        GameDataQuickPickApplyButtonText = "Use Selection";
        GameDataQuickPickEntries = [];
        SelectedGameDataQuickPickEntry = null;
        GameDataQuickPickFilterText = string.Empty;
        RefreshGameDataQuickPickActions();
    }

    [RelayCommand]
    private void UseSelectedCatalogPrototypeForLookup()
    {
        if (SelectedGameDataCatalogPrototypeEntry is not { } entry)
            return;

        PrototypeTokenText = FormatCatalogPrototypeToken(entry);
        SelectedRootTabIndex = ReadRootTabIndex;
        SelectedReadTabIndex = RuntimeReadTabIndex;
        GameDataCatalogStatusText = "Prototype applied";
        GameDataCatalogSummaryText = $"{DescribeCatalogPrototype(entry)} now fills the runtime prototype lookup box.";
    }

    [RelayCommand]
    private void UseSelectedCatalogPrototypeForInventory()
    {
        if (SelectedGameDataCatalogPrototypeEntry is not { } entry || !IsInventoryPrototype(entry))
            return;

        InventoryPrototypeTokenText = FormatCatalogPrototypeToken(entry);
        SelectedRootTabIndex = ActRootTabIndex;
        SelectedActTabIndex = InventoryActTabIndex;
        GameDataCatalogStatusText = "Inventory prototype applied";
        GameDataCatalogSummaryText =
            $"{DescribeCatalogPrototype(entry)} now fills the inventory create prototype field.";
    }

    [RelayCommand]
    private void UseSelectedCatalogPrototypeForSpawn()
    {
        if (SelectedGameDataCatalogPrototypeEntry is not { } entry)
            return;

        MobileSpawnPrototypeTokenText = FormatCatalogPrototypeToken(entry);
        SelectedRootTabIndex = ActRootTabIndex;
        SelectedActTabIndex = MobilesActTabIndex;
        GameDataCatalogStatusText = "Spawn prototype applied";
        GameDataCatalogSummaryText =
            $"{DescribeCatalogPrototype(entry)} now fills the live anchor-create prototype field. NPC and PC prototypes will show up in the mobile roster after creation; other objects can be inspected through the object probe.";
    }

    [RelayCommand]
    private void UseSelectedCatalogWorldLocationForGuidedAction()
    {
        if (SelectedGameDataCatalogWorldEntry is not { HasWorldCoordinates: true } entry)
            return;

        SelectedGuidedActionOption = GuidedActionCatalog.GetDescriptor("teleport_traveler");
        GuidedActionTileXText = entry.WorldX.ToString(CultureInfo.InvariantCulture);
        GuidedActionTileYText = entry.WorldY.ToString(CultureInfo.InvariantCulture);
        GuidedActionMapIdText = "-1";
        GuidedActionFlagsText = "0";
        SelectedRootTabIndex = ActRootTabIndex;
        SelectedActTabIndex = GuidedActTabIndex;
        GameDataCatalogStatusText = "World location applied";
        GameDataCatalogSummaryText =
            $"{entry.DisplayName} now fills the guided teleport fields with world-scene coordinates. Use that teleport when the traveler is already standing on the world map.";
    }

    [RelayCommand]
    private void UseSelectedCatalogStaticObjectPrototypeForLookup()
    {
        if (SelectedGameDataCatalogStaticObjectEntry is not { HasPrototype: true } entry)
            return;

        PrototypeTokenText = CreatePreferredStaticObjectToken(entry);
        SelectedRootTabIndex = ReadRootTabIndex;
        SelectedReadTabIndex = RuntimeReadTabIndex;
        GameDataCatalogStatusText = "Placed object token applied";
        GameDataCatalogSummaryText =
            $"{entry.DisplayName} from {entry.SourceAssetPath} now fills the runtime lookup box with one exact workspace token, so ArcNET can resolve the matching prototype without manual proto hunting.";
    }

    [RelayCommand]
    private void UseSelectedCatalogStaticObjectPrototypeForInventory()
    {
        if (
            SelectedGameDataCatalogStaticObjectEntry is not { HasPrototype: true } entry
            || !IsInventoryObjectType(entry.ObjectType)
        )
        {
            return;
        }

        InventoryPrototypeTokenText = CreatePreferredStaticObjectToken(entry);
        SelectedRootTabIndex = ActRootTabIndex;
        SelectedActTabIndex = InventoryActTabIndex;
        GameDataCatalogStatusText = "Placed object token applied";
        GameDataCatalogSummaryText =
            $"{entry.DisplayName} from {entry.SourceAssetPath} now fills the inventory create field with one exact workspace token, so ArcNET can resolve the right item prototype for you.";
    }

    [RelayCommand]
    private void UseSelectedCatalogStaticObjectPrototypeForSpawn()
    {
        if (SelectedGameDataCatalogStaticObjectEntry is not { HasPrototype: true } entry)
            return;

        MobileSpawnPrototypeTokenText = CreatePreferredStaticObjectToken(entry);
        SelectedRootTabIndex = ActRootTabIndex;
        SelectedActTabIndex = MobilesActTabIndex;
        GameDataCatalogStatusText = "Placed object token applied";
        GameDataCatalogSummaryText =
            $"{entry.DisplayName} from {entry.SourceAssetPath} now fills the live create field with one exact workspace token, so ArcNET can recreate the matching mobile or world object without a manual proto id.";
    }

    private async Task OpenGameDataQuickPickAsync(GameDataQuickPickMode mode, string? filterSeed)
    {
        ApplyGameDataQuickPickMode(mode);
        var effectiveFilterSeed =
            mode == GameDataQuickPickMode.GuidedWorldLocation ? string.Empty : filterSeed ?? string.Empty;
        if (!string.Equals(GameDataQuickPickFilterText, effectiveFilterSeed, StringComparison.Ordinal))
            GameDataQuickPickFilterText = effectiveFilterSeed;
        else
            ApplyGameDataQuickPickFilter();

        if (RequiresWorkspaceCatalog(mode) && !await EnsureGameDataCatalogLoadedAsync())
        {
            GameDataQuickPickEntries = [];
            SelectedGameDataQuickPickEntry = null;
            GameDataQuickPickSummaryText = RequiresWorkspaceCatalog(mode)
                ? GameDataCatalogSummaryText
                : CreateGameDataQuickPickSummary(mode, 0, GameDataQuickPickFilterText);
            RefreshGameDataQuickPickActions();
            return;
        }

        ApplyGameDataQuickPickFilter();
        if (SelectedGameDataQuickPickEntry is null)
            SelectedGameDataQuickPickEntry = GameDataQuickPickEntries.FirstOrDefault();
    }

    private void ApplyGameDataQuickPickMode(GameDataQuickPickMode mode)
    {
        _gameDataQuickPickMode = mode;
        LookupQuickPickVisible =
            mode
                is GameDataQuickPickMode.LookupToken
                    or GameDataQuickPickMode.LookupPrototype
                    or GameDataQuickPickMode.LookupStaticObject;
        GuidedActionQuickPickVisible = mode == GameDataQuickPickMode.GuidedWorldLocation;
        InventoryQuickPickVisible =
            mode
                is GameDataQuickPickMode.InventoryToken
                    or GameDataQuickPickMode.InventoryPrototype
                    or GameDataQuickPickMode.InventoryStaticObject;
        SpawnQuickPickVisible =
            mode
                is GameDataQuickPickMode.SpawnToken
                    or GameDataQuickPickMode.SpawnPrototype
                    or GameDataQuickPickMode.SpawnStaticObject;
        SpellTechQuickPickVisible =
            mode
                is GameDataQuickPickMode.Spell
                    or GameDataQuickPickMode.Schematic
                    or GameDataQuickPickMode.SpellCollege
                    or GameDataQuickPickMode.TechDiscipline
                    or GameDataQuickPickMode.TechSkill;
        GameDataQuickPickEntries = [];
        SelectedGameDataQuickPickEntry = null;
        (
            GameDataQuickPickTitleText,
            GameDataQuickPickSummaryText,
            GameDataQuickPickFilterPlaceholderText,
            GameDataQuickPickApplyButtonText
        ) = mode switch
        {
            GameDataQuickPickMode.LookupToken => (
                "Lookup Token Picker",
                "Search the local ArcNET workspace across prototype palette entries and placed objects, then apply one supported token directly into the runtime lookup box.",
                "Filter by name, proto, object id, guid, asset, or tile",
                "Use for Lookup"
            ),
            GameDataQuickPickMode.LookupPrototype => (
                "Lookup Prototype Picker",
                "Search the local ArcNET prototype palette and apply one known proto token directly into the runtime lookup box.",
                "Filter by proto, type, name, or asset path",
                "Use for Lookup"
            ),
            GameDataQuickPickMode.LookupStaticObject => (
                "Placed Object Picker",
                "Search loaded mob and sector assets by object name, object id, GUID, location, or prototype, then apply that object's exact workspace token for runtime lookup.",
                "Filter by object name, object id, guid, proto, asset, or tile",
                "Use Object Token"
            ),
            GameDataQuickPickMode.InventoryPrototype => (
                "Inventory Item Picker",
                "Search the local ArcNET workspace for supported inventory items, then apply one known-good proto token directly into the create field.",
                "Filter by item name, type, proto, or asset path",
                "Use for Inventory"
            ),
            GameDataQuickPickMode.InventoryStaticObject => (
                "Inventory Source Picker",
                "Search placed mob and sector objects by name, object id, GUID, tile, or prototype, then apply that object's exact workspace token for inventory creation.",
                "Filter by object name, object id, guid, proto, asset, or tile",
                "Use Object Token"
            ),
            GameDataQuickPickMode.InventoryToken => (
                "Inventory Source Picker",
                "Search supported inventory prototypes and placed item-like objects from the local workspace, then apply one good token directly into the create field.",
                "Filter by item, proto, object id, guid, asset, or tile",
                "Use for Inventory"
            ),
            GameDataQuickPickMode.SpawnPrototype => (
                "Spawn Prototype Picker",
                "Search the local ArcNET workspace for mobiles and world objects, then apply one supported proto token into the live create field.",
                "Filter by mobile, object, proto, or asset path",
                "Use for Spawn"
            ),
            GameDataQuickPickMode.SpawnStaticObject => (
                "Placed Spawn Source Picker",
                "Search placed world objects and mob assets by name, object id, GUID, location, or prototype, then apply that object's exact workspace token for live creation.",
                "Filter by object name, object id, guid, proto, asset, or tile",
                "Use Object Token"
            ),
            GameDataQuickPickMode.SpawnToken => (
                "Spawn Source Picker",
                "Search the local ArcNET workspace across spawnable prototypes and placed world objects, then apply one supported token directly into the live create field.",
                "Filter by mobile, object, proto, object id, guid, asset, or tile",
                "Use for Spawn"
            ),
            GameDataQuickPickMode.GuidedWorldLocation => (
                "World Location Picker",
                "Search the local ArcNET world-area catalog, then apply one known world-map destination into the guided teleport fields.",
                "Filter by location, area id, map, or description",
                "Use for Teleport"
            ),
            GameDataQuickPickMode.Spell => (
                "Spell Picker",
                "Browse the supported spell list and apply one canonical spell token into the learn-spell field.",
                "Filter by spell, school, rank, or id",
                "Use for Spell"
            ),
            GameDataQuickPickMode.Schematic => (
                "Schematic Picker",
                "Browse local item prototypes and apply one prototype number directly into the schematic field as a discovered-schematic id.",
                "Filter by item, proto, type, or asset path",
                "Use for Schematic"
            ),
            GameDataQuickPickMode.SpellCollege => (
                "Spell College Picker",
                "Browse the supported spell schools and apply one canonical college token into the live rank editor.",
                "Filter by college name or id",
                "Use for College"
            ),
            GameDataQuickPickMode.TechDiscipline => (
                "Tech Discipline Picker",
                "Browse the supported technology disciplines and apply one canonical discipline token into the live degree editor.",
                "Filter by discipline name or id",
                "Use for Discipline"
            ),
            GameDataQuickPickMode.TechSkill => (
                "Tech Skill Picker",
                "Browse the supported technology skills and apply one canonical skill token into the live points editor.",
                "Filter by skill name or id",
                "Use for Skill"
            ),
            _ => (
                "Local game-data picker",
                "Open one picker to browse workspace-backed prototypes, world-map destinations, inventory items, placed objects, or spawnable entries from the local ArcNET workspace.",
                "Filter local game data",
                "Use Selection"
            ),
        };
    }

    private void ApplyGameDataQuickPickFilter()
    {
        if (_gameDataQuickPickMode == GameDataQuickPickMode.None)
        {
            GameDataQuickPickEntries = [];
            SelectedGameDataQuickPickEntry = null;
            RefreshGameDataQuickPickActions();
            return;
        }

        var hasCatalogData =
            _gameDataCatalogPrototypeCache.Count != 0
            || _gameDataCatalogWorldCache.Count != 0
            || _gameDataCatalogTileArtCache.Count != 0
            || _gameDataCatalogStaticObjectCache.Count != 0;
        if (RequiresWorkspaceCatalog(_gameDataQuickPickMode) && !hasCatalogData)
        {
            GameDataQuickPickEntries = [];
            SelectedGameDataQuickPickEntry = null;
            RefreshGameDataQuickPickActions();
            return;
        }

        var selectedEntryKey = SelectedGameDataQuickPickEntry?.EntryKey;
        GameDataQuickPickEntries = _gameDataQuickPickMode switch
        {
            GameDataQuickPickMode.LookupToken => GameDataQuickPickCatalog.BuildLookupTokenEntries(
                _gameDataCatalogPrototypeCache,
                _gameDataCatalogStaticObjectCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.InventoryPrototype => GameDataQuickPickCatalog.BuildInventoryPrototypeEntries(
                _gameDataCatalogPrototypeCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.InventoryToken => GameDataQuickPickCatalog.BuildInventoryTokenEntries(
                _gameDataCatalogPrototypeCache,
                _gameDataCatalogStaticObjectCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.LookupPrototype => GameDataQuickPickCatalog.BuildLookupPrototypeEntries(
                _gameDataCatalogPrototypeCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.LookupStaticObject =>
                GameDataQuickPickCatalog.BuildLookupStaticObjectPrototypeEntries(
                    _gameDataCatalogStaticObjectCache,
                    GameDataQuickPickFilterText
                ),
            GameDataQuickPickMode.InventoryStaticObject =>
                GameDataQuickPickCatalog.BuildInventoryStaticObjectPrototypeEntries(
                    _gameDataCatalogStaticObjectCache,
                    GameDataQuickPickFilterText
                ),
            GameDataQuickPickMode.SpawnPrototype => GameDataQuickPickCatalog.BuildSpawnPrototypeEntries(
                _gameDataCatalogPrototypeCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.SpawnToken => GameDataQuickPickCatalog.BuildSpawnTokenEntries(
                _gameDataCatalogPrototypeCache,
                _gameDataCatalogStaticObjectCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.SpawnStaticObject => GameDataQuickPickCatalog.BuildSpawnStaticObjectPrototypeEntries(
                _gameDataCatalogStaticObjectCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.GuidedWorldLocation => GameDataQuickPickCatalog.BuildWorldLocationEntries(
                _gameDataCatalogWorldCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.Spell => GameDataQuickPickCatalog.BuildSpellEntries(GameDataQuickPickFilterText),
            GameDataQuickPickMode.Schematic => GameDataQuickPickCatalog.BuildSchematicEntries(
                _gameDataCatalogPrototypeCache,
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.SpellCollege => GameDataQuickPickCatalog.BuildSpellCollegeEntries(
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.TechDiscipline => GameDataQuickPickCatalog.BuildTechDisciplineEntries(
                GameDataQuickPickFilterText
            ),
            GameDataQuickPickMode.TechSkill => GameDataQuickPickCatalog.BuildTechSkillEntries(
                GameDataQuickPickFilterText
            ),
            _ => [],
        };
        SelectedGameDataQuickPickEntry = selectedEntryKey is not null
            ? GameDataQuickPickEntries.FirstOrDefault(entry =>
                string.Equals(entry.EntryKey, selectedEntryKey, StringComparison.OrdinalIgnoreCase)
            )
            : GameDataQuickPickEntries.FirstOrDefault();
        GameDataQuickPickSummaryText = CreateGameDataQuickPickSummary(
            _gameDataQuickPickMode,
            GameDataQuickPickEntries.Count,
            GameDataQuickPickFilterText
        );
        RefreshGameDataQuickPickActions();
    }

    private void RefreshGameDataQuickPickActions() =>
        CanApplySelectedGameDataQuickPick =
            SelectedGameDataQuickPickEntry is not null && _gameDataQuickPickMode != GameDataQuickPickMode.None;

    private static string CreateGameDataQuickPickSummary(GameDataQuickPickMode mode, int matchCount, string? filterText)
    {
        var trimmedFilter = filterText?.Trim() ?? string.Empty;
        return mode switch
        {
            GameDataQuickPickMode.LookupToken when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No supported lookup tokens matched '{trimmedFilter}'. Try a broader name, proto number, object id, GUID, asset path, or tile.",
            GameDataQuickPickMode.LookupToken =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} supported lookup token candidate(s) from the local prototype palette and placed objects.",
            GameDataQuickPickMode.LookupPrototype when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No local prototypes matched '{trimmedFilter}'. Try a broader proto number, name, type, or asset path.",
            GameDataQuickPickMode.LookupPrototype =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} runtime lookup prototype candidate(s) from the local workspace.",
            GameDataQuickPickMode.LookupStaticObject when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No placed objects matched '{trimmedFilter}'. Try an object name, object id, GUID, proto number, or source map path.",
            GameDataQuickPickMode.LookupStaticObject =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} placed object candidate(s) that can seed runtime prototype lookup.",
            GameDataQuickPickMode.InventoryToken when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No supported inventory source tokens matched '{trimmedFilter}'. Try an item name, proto number, object id, GUID, asset path, or tile.",
            GameDataQuickPickMode.InventoryToken =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} inventory source candidate(s) from local prototypes and placed objects.",
            GameDataQuickPickMode.InventoryPrototype when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No local inventory item prototypes matched '{trimmedFilter}'. Try a broader item name, type, or proto number.",
            GameDataQuickPickMode.InventoryPrototype =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} inventory item candidate(s) from the local workspace.",
            GameDataQuickPickMode.InventoryStaticObject when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No placed inventory-compatible objects matched '{trimmedFilter}'. Try an item name, object id, GUID, proto number, or map path.",
            GameDataQuickPickMode.InventoryStaticObject =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} placed object candidate(s) whose prototypes can create inventory items.",
            GameDataQuickPickMode.SpawnToken when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No supported spawn source tokens matched '{trimmedFilter}'. Try a creature name, object name, proto number, object id, GUID, asset path, or tile.",
            GameDataQuickPickMode.SpawnToken =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} spawn source candidate(s) from local prototypes and placed objects.",
            GameDataQuickPickMode.SpawnPrototype when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No local spawnable prototypes matched '{trimmedFilter}'. Try a broader creature, object, or proto filter.",
            GameDataQuickPickMode.SpawnPrototype =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} mobile or object prototype candidate(s) from the local workspace.",
            GameDataQuickPickMode.SpawnStaticObject when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No placed objects matched '{trimmedFilter}' for live creation. Try an object name, object id, GUID, proto number, or source map path.",
            GameDataQuickPickMode.SpawnStaticObject =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} placed object candidate(s) whose prototypes can be recreated live.",
            GameDataQuickPickMode.GuidedWorldLocation when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No world-map destinations matched '{trimmedFilter}'. Try a broader town name, area id, or linked map name.",
            GameDataQuickPickMode.GuidedWorldLocation =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} world-map destination candidate(s) from the local workspace.",
            GameDataQuickPickMode.Spell when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No supported spells matched '{trimmedFilter}'. Try a broader spell name, school, or id.",
            GameDataQuickPickMode.Spell =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} supported spell candidate(s).",
            GameDataQuickPickMode.Schematic when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No local prototype ids matched '{trimmedFilter}' for schematic selection. Try a broader item name or proto number.",
            GameDataQuickPickMode.Schematic =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} local prototype candidate(s) for schematic ids.",
            GameDataQuickPickMode.SpellCollege when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No spell colleges matched '{trimmedFilter}'. Try a broader school name or numeric id.",
            GameDataQuickPickMode.SpellCollege =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} spell-college candidate(s).",
            GameDataQuickPickMode.TechDiscipline when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No tech disciplines matched '{trimmedFilter}'. Try a broader discipline name or numeric id.",
            GameDataQuickPickMode.TechDiscipline =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} tech-discipline candidate(s).",
            GameDataQuickPickMode.TechSkill when matchCount == 0 && trimmedFilter.Length != 0 =>
                $"No tech skills matched '{trimmedFilter}'. Try a broader skill name or numeric id.",
            GameDataQuickPickMode.TechSkill =>
                $"Showing {matchCount.ToString(CultureInfo.InvariantCulture)} tech-skill candidate(s).",
            _ =>
                "Open one picker to browse workspace-backed prototypes, world-map destinations, inventory items, placed objects, or spawnable entries from the local ArcNET workspace.",
        };
    }

    private static bool RequiresWorkspaceCatalog(GameDataQuickPickMode mode) =>
        mode
            is GameDataQuickPickMode.LookupToken
                or GameDataQuickPickMode.LookupPrototype
                or GameDataQuickPickMode.LookupStaticObject
                or GameDataQuickPickMode.InventoryToken
                or GameDataQuickPickMode.InventoryPrototype
                or GameDataQuickPickMode.InventoryStaticObject
                or GameDataQuickPickMode.SpawnToken
                or GameDataQuickPickMode.SpawnPrototype
                or GameDataQuickPickMode.SpawnStaticObject
                or GameDataQuickPickMode.GuidedWorldLocation
                or GameDataQuickPickMode.Schematic;

    private void StageGameDataCatalogInputCore(GameDataQuickPickEntry entry, GameDataQuickPickValueOption option)
    {
        GameDataCatalogStagedInputText = option.ValueText;
        GameDataCatalogStagedInputSummaryText =
            $"{option.LabelText} from {entry.TitleText} is now staged from the local workspace. You can copy that known-good value into any raw debugger field, or keep using the direct action buttons when one flow already knows where the value belongs.";
    }

    private void ApplySessionGameDataCatalogState(AttachedSessionSnapshot snapshot)
    {
        var workspacePath = ResolveEffectiveWorkspacePath(snapshot);
        var moduleDisplayName = CreateWorkspaceDisplayName(snapshot);
        GameDataCatalogPresetSourceText = CreateWorkspaceSourceText(snapshot);

        if (!string.Equals(_gameDataCatalogModulePath, workspacePath, StringComparison.OrdinalIgnoreCase))
        {
            _gameDataCatalogModulePath = null;
            _gameDataCatalogPrototypeCache = [];
            _gameDataCatalogWorldCache = [];
            _gameDataCatalogTileArtCache = [];
            _gameDataCatalogStaticObjectCache = [];
            GameDataCatalogPrototypeEntries = [];
            GameDataCatalogWorldEntries = [];
            GameDataCatalogTileArtEntries = [];
            GameDataCatalogStaticObjectEntries = [];
            SelectedGameDataCatalogPrototypeEntry = null;
            SelectedGameDataCatalogWorldEntry = null;
            SelectedGameDataCatalogTileArtEntry = null;
            SelectedGameDataCatalogStaticObjectEntry = null;
            SelectedGameDataCatalogTileArtValueEntry = null;
            SelectedGameDataCatalogStaticObjectValueEntry = null;
            GameDataCatalogStagedInputText = string.Empty;
            GameDataCatalogStagedInputSummaryText =
                "Stage one supported input from the local workspace here when you want a known-good token, coordinates pair, or art id ready to reuse elsewhere.";
            CloseGameDataQuickPick();
            GameDataCatalogStatusText = $"Local workspace data ready to load for {moduleDisplayName}.";
            GameDataCatalogSummaryText =
                "ArcNET will load the local workspace catalog for the attached runtime so the selectors below stay aligned with the active install context instead of relying on raw token guesses.";
            GameDataCatalogPresetSummaryText =
                "Supported prototype, item, schematic, and world selections will follow the attached runtime after the local workspace catalog finishes loading.";
        }
        else
        {
            GameDataCatalogStatusText = "Game-data catalog loaded.";
            GameDataCatalogPresetSummaryText =
                $"Supported prototype, item, schematic, and world selections are currently aligned with {moduleDisplayName}. Use the built-in selectors when you are unsure which token format a field expects.";
            ApplyFilteredGameDataCatalog();
        }

        RefreshGameDataCatalogActions();
    }

    private void ApplyDormantGameDataCatalog(string status, string summary)
    {
        _gameDataCatalogModulePath = null;
        _gameDataCatalogPrototypeCache = [];
        _gameDataCatalogWorldCache = [];
        _gameDataCatalogTileArtCache = [];
        _gameDataCatalogStaticObjectCache = [];
        GameDataCatalogPrototypeEntries = [];
        GameDataCatalogWorldEntries = [];
        GameDataCatalogTileArtEntries = [];
        GameDataCatalogStaticObjectEntries = [];
        SelectedGameDataCatalogPrototypeEntry = null;
        SelectedGameDataCatalogWorldEntry = null;
        SelectedGameDataCatalogTileArtEntry = null;
        SelectedGameDataCatalogStaticObjectEntry = null;
        SelectedGameDataCatalogTileArtValueEntry = null;
        SelectedGameDataCatalogStaticObjectValueEntry = null;
        GameDataCatalogStagedInputText = string.Empty;
        GameDataCatalogStagedInputSummaryText =
            "Stage one supported input from the local workspace here when you want a known-good token, coordinates pair, or art id ready to reuse elsewhere.";
        GameDataCatalogStatusText = status;
        GameDataCatalogSummaryText = summary;
        GameDataCatalogPresetSourceText = "Local workspace source inactive.";
        GameDataCatalogPresetSummaryText =
            "Attach to a live runtime to load supported prototype, item, schematic, and world selections from the active local workspace.";
        RefreshGameDataCatalogActions();
    }

    private void ApplyFilteredGameDataCatalog()
    {
        if (
            _gameDataCatalogPrototypeCache.Count == 0
            && _gameDataCatalogWorldCache.Count == 0
            && _gameDataCatalogTileArtCache.Count == 0
            && _gameDataCatalogStaticObjectCache.Count == 0
        )
        {
            RefreshGameDataCatalogActions();
            return;
        }

        var selectedPrototypeNumber = SelectedGameDataCatalogPrototypeEntry?.ProtoNumber;
        var selectedWorldAreaId = SelectedGameDataCatalogWorldEntry?.AreaId;
        var selectedTileArtId = SelectedGameDataCatalogTileArtEntry?.ArtIdValue;
        var selectedStaticObjectKey = SelectedGameDataCatalogStaticObjectEntry is { } selectedStaticObject
            ? $"{selectedStaticObject.SourceAssetPath}|{selectedStaticObject.ObjectIdText}"
            : null;
        var normalizedPrototypeFilter = NormalizeGameDataCatalogFilter(GameDataCatalogPrototypeFilterText);
        var normalizedWorldFilter = NormalizeGameDataCatalogFilter(GameDataCatalogWorldFilterText);
        var normalizedTileArtFilter = NormalizeGameDataCatalogFilter(GameDataCatalogTileArtFilterText);
        var normalizedStaticObjectFilter = NormalizeGameDataCatalogFilter(GameDataCatalogStaticObjectFilterText);
        var selectedScope = SelectedGameDataCatalogPrototypeScopeOption?.Token ?? "all";
        GameDataCatalogPrototypeEntries =
        [
            .. _gameDataCatalogPrototypeCache.Where(entry =>
                MatchesPrototypeScope(entry, selectedScope) && MatchesPrototypeFilter(entry, normalizedPrototypeFilter)
            ),
        ];
        GameDataCatalogWorldEntries =
        [
            .. _gameDataCatalogWorldCache.Where(entry => MatchesWorldFilter(entry, normalizedWorldFilter)),
        ];
        GameDataCatalogTileArtEntries =
        [
            .. _gameDataCatalogTileArtCache
                .Where(entry => MatchesTileArtFilter(entry, normalizedTileArtFilter))
                .Take(MaxTileArtCatalogResults),
        ];
        GameDataCatalogStaticObjectEntries =
        [
            .. _gameDataCatalogStaticObjectCache
                .Where(entry => MatchesStaticObjectFilter(entry, normalizedStaticObjectFilter))
                .Take(MaxStaticObjectCatalogResults),
        ];

        SelectedGameDataCatalogPrototypeEntry = selectedPrototypeNumber.HasValue
            ? GameDataCatalogPrototypeEntries.FirstOrDefault(entry =>
                entry.ProtoNumber == selectedPrototypeNumber.Value
            )
            : null;
        SelectedGameDataCatalogWorldEntry = selectedWorldAreaId.HasValue
            ? GameDataCatalogWorldEntries.FirstOrDefault(entry => entry.AreaId == selectedWorldAreaId.Value)
            : null;
        SelectedGameDataCatalogTileArtEntry = selectedTileArtId.HasValue
            ? GameDataCatalogTileArtEntries.FirstOrDefault(entry => entry.ArtIdValue == selectedTileArtId.Value)
            : null;
        SelectedGameDataCatalogStaticObjectEntry = selectedStaticObjectKey is not null
            ? GameDataCatalogStaticObjectEntries.FirstOrDefault(entry =>
                string.Equals(
                    $"{entry.SourceAssetPath}|{entry.ObjectIdText}",
                    selectedStaticObjectKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            : null;

        GameDataCatalogSummaryText =
            $"Showing {GameDataCatalogPrototypeEntries.Count.ToString(CultureInfo.InvariantCulture)}/{_gameDataCatalogPrototypeCache.Count.ToString(CultureInfo.InvariantCulture)} prototypes, {GameDataCatalogWorldEntries.Count.ToString(CultureInfo.InvariantCulture)}/{_gameDataCatalogWorldCache.Count.ToString(CultureInfo.InvariantCulture)} world-map locations, {GameDataCatalogTileArtEntries.Count.ToString(CultureInfo.InvariantCulture)}/{_gameDataCatalogTileArtCache.Count.ToString(CultureInfo.InvariantCulture)} tile-art ids, and {GameDataCatalogStaticObjectEntries.Count.ToString(CultureInfo.InvariantCulture)}/{_gameDataCatalogStaticObjectCache.Count.ToString(CultureInfo.InvariantCulture)} static objects from the cached local workspace.";
        ApplyGameDataQuickPickFilter();
        RefreshGameDataCatalogActions();
    }

    private void RefreshGameDataCatalogActions()
    {
        CanLoadGameDataCatalog = ActiveSession is not null && !_gameDataCatalogLoadInFlight;
        CanUseSelectedCatalogPrototypeForLookup = SelectedGameDataCatalogPrototypeEntry is not null;
        CanUseSelectedCatalogPrototypeForInventory =
            SelectedGameDataCatalogPrototypeEntry is { } inventoryEntry && IsInventoryPrototype(inventoryEntry);
        CanUseSelectedCatalogPrototypeForSpawn = SelectedGameDataCatalogPrototypeEntry is not null;
        CanUseSelectedCatalogWorldLocationForGuidedAction =
            SelectedGameDataCatalogWorldEntry?.HasWorldCoordinates == true;
        CanUseSelectedCatalogStaticObjectPrototypeForLookup =
            SelectedGameDataCatalogStaticObjectEntry?.HasPrototype == true;
        CanUseSelectedCatalogStaticObjectPrototypeForInventory =
            SelectedGameDataCatalogStaticObjectEntry is { HasPrototype: true } staticObjectEntry
            && IsInventoryObjectType(staticObjectEntry.ObjectType);
        CanUseSelectedCatalogStaticObjectPrototypeForSpawn =
            SelectedGameDataCatalogStaticObjectEntry?.HasPrototype == true;
        RefreshGameDataQuickPickActions();
        RefreshSupportedInputPanels();
    }

    private void RefreshSupportedInputPanels()
    {
        LookupSupportedInputPanelState = GameDataSupportedInputPanelCatalog.CreateLookupState(
            _gameDataCatalogPrototypeCache,
            _gameDataCatalogStaticObjectCache,
            LookupSupportedInputFilterText
        );
        InventorySupportedInputPanelState = GameDataSupportedInputPanelCatalog.CreateInventoryState(
            _gameDataCatalogPrototypeCache,
            _gameDataCatalogStaticObjectCache,
            InventorySupportedInputFilterText
        );
        SpawnSupportedInputPanelState = GameDataSupportedInputPanelCatalog.CreateSpawnState(
            _gameDataCatalogPrototypeCache,
            _gameDataCatalogStaticObjectCache,
            SpawnSupportedInputFilterText
        );
        GuidedActionWorldSupportedInputPanelVisible = string.Equals(
            SelectedGuidedActionOption?.Key,
            "teleport_traveler",
            StringComparison.OrdinalIgnoreCase
        );
        GuidedActionWorldSupportedInputPanelState = GuidedActionWorldSupportedInputPanelVisible
            ? GameDataSupportedInputPanelCatalog.CreateWorldLocationState(
                _gameDataCatalogWorldCache,
                GuidedActionWorldSupportedInputFilterText
            )
            : new(
                "Known World Locations",
                "World-location picks are available when guided teleport is selected.",
                "Switch the guided action back to teleport to reuse one known world destination here.",
                "Browse All World Locations",
                "Search location names, map ids, area ids, or map names",
                []
            );
        RefreshSpellTechSelectionLists();
    }

    private static string FormatCatalogPrototypeToken(PrototypePaletteEntry entry) =>
        $"proto:{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}";

    private static string CreatePreferredStaticObjectToken(StaticObjectCatalogEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.ObjectGuidText) ? entry.ObjectGuidText : entry.ObjectIdText;

    private static string DescribeCatalogPrototype(PrototypePaletteEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.DisplayName)
            ? $"{entry.DisplayName} [{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}]"
            : $"{entry.AssetPath} [{entry.ProtoNumber.ToString(CultureInfo.InvariantCulture)}]";

    private static bool MatchesPrototypeScope(PrototypePaletteEntry entry, string scopeToken) =>
        scopeToken switch
        {
            "items" => IsInventoryPrototype(entry),
            "mobiles" => IsMobilePrototype(entry),
            "objects" => !IsInventoryPrototype(entry) && !IsMobilePrototype(entry),
            _ => true,
        };

    private static bool IsInventoryPrototype(PrototypePaletteEntry entry) => IsInventoryObjectType(entry.ObjectType);

    private static bool IsInventoryObjectType(string objectType) =>
        objectType
            is "Weapon"
                or "Ammo"
                or "Armor"
                or "Gold"
                or "Food"
                or "Scroll"
                or "Key"
                or "KeyRing"
                or "Written"
                or "Generic";

    private static bool IsMobilePrototype(PrototypePaletteEntry entry) => entry.ObjectType is "Pc" or "Npc";

    private static bool MatchesPrototypeFilter(PrototypePaletteEntry entry, string normalizedFilter) =>
        normalizedFilter.Length == 0
        || NormalizeGameDataCatalogFilter(entry.ProtoNumber.ToString(CultureInfo.InvariantCulture))
            .Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.ObjectType).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.AssetPath).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.DisplayName).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.Description).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.PaletteGroup).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.ArtAssetPath).Contains(normalizedFilter, StringComparison.Ordinal);

    private static bool MatchesWorldFilter(WorldMapCatalogEntry entry, string normalizedFilter) =>
        normalizedFilter.Length == 0
        || NormalizeGameDataCatalogFilter(entry.AreaId.ToString(CultureInfo.InvariantCulture))
            .Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.DisplayName).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.Description).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.MapSummaryText).Contains(normalizedFilter, StringComparison.Ordinal)
        || entry.MapNames.Any(name =>
            NormalizeGameDataCatalogFilter(name).Contains(normalizedFilter, StringComparison.Ordinal)
        );

    private static bool MatchesTileArtFilter(TileArtCatalogEntry entry, string normalizedFilter) =>
        normalizedFilter.Length == 0
        || NormalizeGameDataCatalogFilter(entry.ArtIdText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.DisplayName).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.ArtTypeText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.AssetPath).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.SummaryText).Contains(normalizedFilter, StringComparison.Ordinal);

    private static bool MatchesStaticObjectFilter(StaticObjectCatalogEntry entry, string normalizedFilter) =>
        normalizedFilter.Length == 0
        || NormalizeGameDataCatalogFilter(entry.DisplayName).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.ObjectType).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.ObjectIdText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.ObjectGuidText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.PrototypeText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.SourceAssetPath).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.LocationText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeGameDataCatalogFilter(entry.SourceKindText).Contains(normalizedFilter, StringComparison.Ordinal);

    private static string NormalizeGameDataCatalogFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        Span<char> buffer = stackalloc char[value.Length];
        var count = 0;
        foreach (var ch in value)
        {
            if (!char.IsLetterOrDigit(ch))
                continue;

            buffer[count++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..count]);
    }

    private async Task<bool> EnsureGameDataCatalogLoadedAsync(bool forceReload = false)
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantGameDataCatalog(
                "No active session",
                "Attach to a live runtime before loading the local workspace catalog."
            );
            return false;
        }

        if (
            !forceReload
            && !string.IsNullOrWhiteSpace(_gameDataCatalogModulePath)
            && string.Equals(
                _gameDataCatalogModulePath,
                ResolveEffectiveWorkspacePath(session),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            ApplyFilteredGameDataCatalog();
            RefreshGameDataCatalogActions();
            ApplyGameDataQuickPickFilter();
            return true;
        }

        if (_gameDataCatalogLoadInFlight)
            return false;

        _gameDataCatalogLoadInFlight = true;
        var workspacePathOverride = ResolveWorkspacePathOverride();
        var requestedWorkspacePath = ResolveEffectiveWorkspacePath(session);
        var requestedModuleDisplayName = CreateWorkspaceDisplayName(session);
        GameDataCatalogStatusText = $"Loading local workspace data for {requestedModuleDisplayName}...";
        GameDataCatalogSummaryText =
            "ArcNET is reading the attached local workspace so the in-app selectors can show supported inputs instead of raw guesses.";
        RefreshGameDataCatalogActions();
        try
        {
            if (forceReload && !string.IsNullOrWhiteSpace(requestedWorkspacePath))
                _ = await WorkspaceGameDataCatalogLoader.LoadFromModulePathAsync(
                    requestedWorkspacePath,
                    forceReload: true
                );

            var snapshot = await _gameDataCatalogService.LoadAsync(
                new GameDataCatalogRequest(session, workspacePathOverride)
            );
            if (!snapshot.IsAvailable)
            {
                ApplyDormantGameDataCatalog(snapshot.Status, snapshot.Summary);
                return false;
            }

            if (!MatchesCurrentCatalogSession(session, requestedWorkspacePath))
                return false;

            _gameDataCatalogModulePath = requestedWorkspacePath;
            _gameDataCatalogPrototypeCache = snapshot.PrototypeEntries;
            _gameDataCatalogWorldCache = snapshot.WorldMapEntries;
            _gameDataCatalogTileArtCache = snapshot.TileArtEntries;
            _gameDataCatalogStaticObjectCache = snapshot.StaticObjectEntries;
            GameDataCatalogPresetSourceText = CreateWorkspaceSourceText(session);
            GameDataCatalogPresetSummaryText =
                $"Loaded workspace-backed prototype, item, schematic, and world selections for {requestedModuleDisplayName}. Use the built-in selectors first when you are unsure which value format a field accepts.";
            GameDataCatalogStatusText = snapshot.Status;
            ApplyFilteredGameDataCatalog();
            ApplyGameDataQuickPickFilter();
            return true;
        }
        catch (Exception ex)
        {
            ApplyDormantGameDataCatalog(
                "Game-data catalog failed",
                $"Unable to load the local workspace catalog ({ex.GetType().Name}: {ex.Message})."
            );
            return false;
        }
        finally
        {
            _gameDataCatalogLoadInFlight = false;
            RefreshGameDataCatalogActions();
        }
    }

    private static string CreatePrototypeCatalogFilterSeed(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        var trimmed = token.Trim();
        if (trimmed.StartsWith("proto:", StringComparison.OrdinalIgnoreCase))
            return trimmed["proto:".Length..];

        if (trimmed.StartsWith("prototype:", StringComparison.OrdinalIgnoreCase))
            return trimmed["prototype:".Length..];

        if (TryParseUInt64(trimmed, out var handle) && RuntimeSemanticCatalog.LooksLikeObjectHandle(handle))
            return string.Empty;

        return trimmed;
    }

    private bool MatchesCurrentCatalogSession(AttachedSessionSnapshot requestSession, string requestWorkspacePath) =>
        ActiveSession is { } activeSession
        && activeSession.ProcessId == requestSession.ProcessId
        && string.Equals(
            ResolveEffectiveWorkspacePath(activeSession),
            requestWorkspacePath,
            StringComparison.OrdinalIgnoreCase
        );

    private static string CreateModuleCatalogDisplayName(RuntimeFingerprint fingerprint)
    {
        var moduleFileName = global::System.IO.Path.GetFileName(fingerprint.ModulePath);
        return string.IsNullOrWhiteSpace(moduleFileName) ? fingerprint.ProcessName : moduleFileName;
    }

    private static bool TryParseUInt64(string value, out ulong result)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private enum GameDataQuickPickMode
    {
        None,
        LookupToken,
        LookupPrototype,
        LookupStaticObject,
        InventoryToken,
        InventoryPrototype,
        InventoryStaticObject,
        SpawnToken,
        SpawnPrototype,
        SpawnStaticObject,
        GuidedWorldLocation,
        Spell,
        Schematic,
        SpellCollege,
        TechDiscipline,
        TechSkill,
    }
}
