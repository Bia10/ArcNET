using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using ArcanumDebugger.App.Composition;
using ArcNET.Diagnostics;
using ArcNET.Patch;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;

namespace ArcanumDebugger.App.ViewModels;

[SupportedOSPlatform("windows")]
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly Subject<ArcanumDebuggerPreviewScenario> _workspaceRequests = new();
    private readonly IDisposable _workspaceSubscription;
    private readonly IDisposable _clockSubscription;
    private readonly IDisposable _watchSubscription;
    private readonly EnvironmentService _environmentService;
    private readonly SessionService _sessionService;
    private readonly WatchService _watchService;
    private readonly ObjectProbeService _objectProbeService;
    private readonly FunctionCallService _functionCallService;
    private readonly GuidedActionService _guidedActionService;
    private readonly InventoryEditorService _inventoryEditorService;
    private readonly MobileEntityService _mobileEntityService;
    private WorkspaceSnapshot _previewWorkspace = WorkspaceService.Create(
        ArcanumDebuggerPreviewCatalog.Scenarios[0].WorkspaceRequest
    );
    private SessionHandle? _activeSessionHandle;
    private WatchHandle? _activeWatchHandle;
    private bool _watchPollInFlight;
    private bool _mobileRosterPollInFlight;
    private IReadOnlyList<MobileRosterEntrySnapshot> _mobileRosterCache = [];
    private string? _pendingMobileSelectionHandle;

    [ObservableProperty]
    private int selectedRootTabIndex;

    [ObservableProperty]
    private int selectedReadTabIndex;

    [ObservableProperty]
    private IReadOnlyList<ArcanumDebuggerPreviewScenario> scenarios = [.. ArcanumDebuggerPreviewCatalog.Scenarios];

    [ObservableProperty]
    private ArcanumDebuggerPreviewScenario? selectedScenario;

    [ObservableProperty]
    private WorkspaceSnapshot workspace = WorkspaceService.Create(
        ArcanumDebuggerPreviewCatalog.Scenarios[0].WorkspaceRequest
    );

    [ObservableProperty]
    private EnvironmentSnapshot environment = null!;

    [ObservableProperty]
    private IReadOnlyList<LaunchExecutableKindOption> launchExecutableOptions =
    [
        new(ArcanumExecutableKind.Auto, "Auto detect"),
        new(ArcanumExecutableKind.Classic, "Classic only"),
        new(ArcanumExecutableKind.CommunityEdition, "Community Edition only"),
    ];

    [ObservableProperty]
    private LaunchExecutableKindOption? selectedLaunchExecutableOption = new(ArcanumExecutableKind.Auto, "Auto detect");

    [ObservableProperty]
    private string installPath = string.Empty;

    [ObservableProperty]
    private string workspaceOverridePathText = string.Empty;

    [ObservableProperty]
    private bool launchWindowed;

    [ObservableProperty]
    private IReadOnlyList<string> capabilityChips = [];

    [ObservableProperty]
    private IReadOnlyList<WorkspacePanelWorkflowSnapshot> featuredPanels = [];

    [ObservableProperty]
    private IReadOnlyList<ArcNET.Diagnostics.Contracts.ProbeProfile> featuredProbeProfiles = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerTimelineCard> featuredTimelinePresets = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerAdvancedProfileCard> featuredAdvancedProfiles = [];

    [ObservableProperty]
    private IReadOnlyList<ArcNET.Diagnostics.DispatcherCandidateDefinition> dispatcherCandidates = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerFunctionCard> featuredFunctions = [];

    [ObservableProperty]
    private IReadOnlyList<FunctionDefinition> functionCallOptions = [];

    [ObservableProperty]
    private FunctionDefinition? selectedFunctionCallOption;

    [ObservableProperty]
    private string functionCallTargetText = string.Empty;

    [ObservableProperty]
    private string functionCallStackArgumentsText = string.Empty;

    [ObservableProperty]
    private string functionCallEcxText = string.Empty;

    [ObservableProperty]
    private string functionCallEdxText = string.Empty;

    [ObservableProperty]
    private string functionCallTimeoutText = "1000";

    [ObservableProperty]
    private bool functionCallUseSuggestedCleanup = true;

    [ObservableProperty]
    private IReadOnlyList<DebuggerCleanupModeOption> functionCallCleanupOptions =
    [
        new(StackCleanupMode.Cdecl, "Cdecl"),
        new(StackCleanupMode.StdCall, "StdCall"),
    ];

    [ObservableProperty]
    private DebuggerCleanupModeOption? selectedFunctionCallCleanupOption = new(StackCleanupMode.Cdecl, "Cdecl");

    [ObservableProperty]
    private bool canInvokeFunctionCall;

    [ObservableProperty]
    private string functionCallStatusText = "No live function call executed.";

    [ObservableProperty]
    private string functionCallSummaryText =
        "Low-level call path: choose a known function key or raw RVA only when no guided action fits the job.";

    [ObservableProperty]
    private string functionCallExampleText =
        "Tip: use handle(player) in stack arguments or handle_low(...) and handle_high(...) helpers for split object handles.";

    [ObservableProperty]
    private string functionCallTargetSummaryText = "Known-function metadata or raw RVA resolution will appear here.";

    [ObservableProperty]
    private string functionCallDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string functionCallExecutionDetailText =
        "Target address and cleanup details will appear here after a live call.";

    [ObservableProperty]
    private string functionCallResultText = "EAX and EDX values will appear here after a live call.";

    [ObservableProperty]
    private IReadOnlyList<FunctionCallArgumentSnapshot> functionCallArguments = [];

    [ObservableProperty]
    private IReadOnlyList<GuidedActionDescriptor> guidedActionOptions = [.. GuidedActionCatalog.Actions];

    [ObservableProperty]
    private GuidedActionDescriptor? selectedGuidedActionOption = GuidedActionCatalog.Actions.FirstOrDefault();

    [ObservableProperty]
    private string guidedActionTravelerText = "player";

    [ObservableProperty]
    private string guidedActionTileXText = string.Empty;

    [ObservableProperty]
    private string guidedActionTileYText = string.Empty;

    [ObservableProperty]
    private string guidedActionMapIdText = "-1";

    [ObservableProperty]
    private string guidedActionFlagsText = "0";

    [ObservableProperty]
    private string guidedActionTimeoutText = "1000";

    [ObservableProperty]
    private bool canInvokeGuidedAction;

    [ObservableProperty]
    private bool guidedActionShowTileInputs = true;

    [ObservableProperty]
    private bool guidedActionShowMapIdInput = true;

    [ObservableProperty]
    private bool guidedActionShowFlagsInput = true;

    [ObservableProperty]
    private string guidedActionTravelerPlaceholderText = "Traveler handle or player";

    [ObservableProperty]
    private string guidedActionTileXPlaceholderText = "Tile X";

    [ObservableProperty]
    private string guidedActionTileYPlaceholderText = "Tile Y";

    [ObservableProperty]
    private string guidedActionMapIdPlaceholderText = "Map id (-1 current map)";

    [ObservableProperty]
    private string guidedActionFlagsPlaceholderText = "Flags";

    [ObservableProperty]
    private string guidedActionStatusText = "No guided action executed.";

    [ObservableProperty]
    private string guidedActionSummaryText =
        "Choose a gameplay action and provide only the values that matter, such as who moves and where.";

    [ObservableProperty]
    private string guidedActionTargetSummaryText = "Guided action metadata will appear here.";

    [ObservableProperty]
    private string guidedActionHintText =
        "Traveler can be player or a raw object handle. Leave map id at -1 to stay on the current map.";

    [ObservableProperty]
    private string guidedActionDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string guidedActionExecutionDetailText =
        "Target address and cleanup details will appear here after a live action.";

    [ObservableProperty]
    private string guidedActionResultText = "EAX and EDX values will appear here after a live action.";

    [ObservableProperty]
    private string inventoryOwnerTokenText = "player";

    [ObservableProperty]
    private string inventoryPrototypeTokenText = string.Empty;

    [ObservableProperty]
    private string inventoryLocationText = "0";

    [ObservableProperty]
    private string inventoryItemHandleText = string.Empty;

    [ObservableProperty]
    private string inventoryTimeoutText = "1000";

    [ObservableProperty]
    private bool canCreateInventoryItem;

    [ObservableProperty]
    private bool canDestroyInventoryItem;

    [ObservableProperty]
    private string inventoryEditorStatusText = "No inventory mutation executed.";

    [ObservableProperty]
    private IReadOnlyList<string> inventoryEditorResultLines =
    [
        "Create items from proto tokens or remove live item handles through the native inventory hooks.",
    ];

    [ObservableProperty]
    private string inventoryEditorDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string inventoryEditorExecutionDetailText =
        "Target address and hook details will appear here after a live inventory mutation.";

    [ObservableProperty]
    private string inventoryEditorResultText =
        "Mutation result values will appear here after a live inventory mutation.";

    [ObservableProperty]
    private string mobileRosterFilterText = string.Empty;

    [ObservableProperty]
    private bool mobileRosterAutoRefresh = true;

    [ObservableProperty]
    private string mobileRosterStatusText = "No live mobile roster loaded.";

    [ObservableProperty]
    private string mobileRosterSummaryText =
        "Refresh to scan the live object pool for PC and NPC instances, then select one mobile to mutate or inspect.";

    [ObservableProperty]
    private IReadOnlyList<MobileRosterEntrySnapshot> mobileRosterEntries = [];

    [ObservableProperty]
    private MobileRosterEntrySnapshot? selectedMobileRosterEntry;

    [ObservableProperty]
    private bool canUseSelectedMobileRosterHandle;

    [ObservableProperty]
    private string mobileTargetHandleText = string.Empty;

    [ObservableProperty]
    private string mobileStatTokenText = "strength";

    [ObservableProperty]
    private string mobileStatValueText = string.Empty;

    [ObservableProperty]
    private string mobileSpawnPrototypeTokenText = string.Empty;

    [ObservableProperty]
    private string mobileSpawnAnchorTokenText = "player";

    [ObservableProperty]
    private string mobileMutationTimeoutText = "1000";

    [ObservableProperty]
    private bool canRefreshMobileRoster;

    [ObservableProperty]
    private bool canInspectSelectedMobile;

    [ObservableProperty]
    private bool canSetMobileStat;

    [ObservableProperty]
    private bool canKillMobile;

    [ObservableProperty]
    private bool canDespawnMobile;

    [ObservableProperty]
    private bool canSpawnMobile;

    [ObservableProperty]
    private string mobileMutationStatusText = "No mobile mutation executed.";

    [ObservableProperty]
    private IReadOnlyList<string> mobileMutationResultLines =
    [
        "Pick a live mobile from the roster, then edit one base stat, trigger critter_kill, destroy it, or create a mobile or world object from one prototype token.",
    ];

    [ObservableProperty]
    private string mobileMutationDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string mobileMutationExecutionDetailText =
        "Target address and hook details will appear here after a live mobile mutation.";

    [ObservableProperty]
    private string mobileMutationResultText = "Mutation result values will appear here after a live mobile mutation.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerObjectGroupCard> featuredObjectGroups = [];

    [ObservableProperty]
    private IReadOnlyList<string> featuredNotes = [];

    [ObservableProperty]
    private string runtimeTitle = string.Empty;

    [ObservableProperty]
    private string runtimeSubtitle = string.Empty;

    [ObservableProperty]
    private string runtimeKindText = string.Empty;

    [ObservableProperty]
    private string symbolStateText = string.Empty;

    [ObservableProperty]
    private string supportBadgeText = string.Empty;

    [ObservableProperty]
    private string supportSummaryText = string.Empty;

    [ObservableProperty]
    private string processTargetsText = string.Empty;

    [ObservableProperty]
    private string recommendedPanelCountText = string.Empty;

    [ObservableProperty]
    private string timelineHeadlineText = string.Empty;

    [ObservableProperty]
    private string timelineStatusText = string.Empty;

    [ObservableProperty]
    private string functionStatusText = string.Empty;

    [ObservableProperty]
    private string objectExplorerStatusText = string.Empty;

    [ObservableProperty]
    private string generatedAgoText = string.Empty;

    [ObservableProperty]
    private string selectedScenarioSummary = string.Empty;

    [ObservableProperty]
    private string workspaceSourceText = "Reference workspace";

    [ObservableProperty]
    private string attachSummaryText = string.Empty;

    [ObservableProperty]
    private string attachTargetText = "Select a detected live runtime to attach.";

    [ObservableProperty]
    private string launchPreviewSummaryText = "Enter an Arcanum install path to preview a launch plan.";

    [ObservableProperty]
    private string launchPreviewDetailText =
        "The debugger checks the install and shows how it will start the selected game variant.";

    [ObservableProperty]
    private IReadOnlyList<ProcessCandidateSnapshot> processCandidates = [];

    [ObservableProperty]
    private IReadOnlyList<LiveRuntimeSnapshot> liveRuntimes = [];

    [ObservableProperty]
    private LiveRuntimeSnapshot? selectedLiveRuntime;

    [ObservableProperty]
    private bool canAttachSelectedRuntime;

    [ObservableProperty]
    private AttachedSessionSnapshot? activeSession;

    [ObservableProperty]
    private string sessionHeadlineText = "No active session";

    [ObservableProperty]
    private string sessionOriginText = "Dormant";

    [ObservableProperty]
    private string sessionSummaryText =
        "Attach to a detected runtime or launch from an install path to create a live debugger session.";

    [ObservableProperty]
    private string sessionDetailText =
        "A live attachment lets you read state, observe changes, act deliberately, and inspect objects from one session.";

    [ObservableProperty]
    private IReadOnlyList<string> sessionNotes = [];

    [ObservableProperty]
    private string sessionSupportBadgeText = "Dormant";

    [ObservableProperty]
    private string sessionSupportSummaryText = "No process-backed session is attached.";

    [ObservableProperty]
    private IBrush sessionSupportAccentBrush = s_dormantAccentBrush;

    [ObservableProperty]
    private IReadOnlyList<TimelinePresetDescriptor> watchPresetOptions = [];

    [ObservableProperty]
    private TimelinePresetDescriptor? selectedWatchPresetOption;

    [ObservableProperty]
    private bool canStartTimelineWatch;

    [ObservableProperty]
    private WatchSnapshot? activeWatch;

    [ObservableProperty]
    private string liveTimelineStatusText = "No live watch running.";

    [ObservableProperty]
    private string liveTimelineSummaryText = "Start a watch from an active validated session to see live hook events.";

    [ObservableProperty]
    private IReadOnlyList<WatchEventSnapshot> liveTimelineEvents = [];

    [ObservableProperty]
    private WatchEventSnapshot? selectedLiveTimelineEvent;

    [ObservableProperty]
    private string selectedLiveTimelineEventSummaryText =
        "Select one live timeline event to reuse any detected object-handle candidates.";

    [ObservableProperty]
    private string selectedLiveTimelineEventHandleSummaryText = "No handle candidates detected yet.";

    [ObservableProperty]
    private string objectProbeHandleText = string.Empty;

    [ObservableProperty]
    private bool canUseSelectedEventHandle;

    [ObservableProperty]
    private bool canInspectObjectHandle;

    [ObservableProperty]
    private bool canInspectActivePlayer;

    [ObservableProperty]
    private string objectProbeStatusText = "No live object inspected.";

    [ObservableProperty]
    private string objectProbeSummaryText =
        "Select a live event, enter a handle, or inspect the active player to decode one object into grouped field sections.";

    [ObservableProperty]
    private IReadOnlyList<ObjectProbeObjectSnapshot> objectProbeObjects = [];

    [ObservableProperty]
    private ObjectProbeObjectSnapshot? selectedObjectProbeObject;

    [ObservableProperty]
    private IBrush supportAccentBrush = s_validatedAccentBrush;

    [ObservableProperty]
    private IBrush supportSurfaceBrush = s_validatedSurfaceBrush;

    public MainWindowViewModel(IDiagnosticsServices diagnosticsServices)
    {
        var services = diagnosticsServices ?? throw new ArgumentNullException(nameof(diagnosticsServices));
        _environmentService = services.EnvironmentService;
        _sessionService = services.SessionService;
        _watchService = services.WatchService;
        _objectProbeService = services.ObjectProbeService;
        _functionCallService = services.FunctionCallService;
        _guidedActionService = services.GuidedActionService;
        _inventoryEditorService = services.InventoryEditorService;
        _mobileEntityService = services.MobileEntityService;
        _auditService = services.AuditService;
        _prototypeResolutionService = services.PrototypeResolutionService;
        _readService = services.ReadService;
        _sheetService = services.SheetService;
        _scriptAttachmentService = services.ScriptAttachmentService;
        _logbookService = services.LogbookService;
        _interceptService = services.InterceptService;
        _interceptTargetResolver = services.InterceptTargetResolver;
        _moduleSymbolQueryService = services.ModuleSymbolQueryService;
        _runtimeStatusService = services.RuntimeStatusService;
        _crashDumpService = services.CrashDumpService;
        _gameDataCatalogService = services.GameDataCatalogService;
        _sheetEditorService = services.SheetEditorService;
        _spellTechEditorService = services.SpellTechEditorService;
        _logbookEditorService = services.LogbookEditorService;
        Environment = _environmentService.Create(new EnvironmentRequest([], null, ArcanumExecutableKind.Auto, false));
        _workspaceSubscription = _workspaceRequests
            .Select(static scenario => WorkspaceService.Create(scenario.WorkspaceRequest))
            .Subscribe(snapshot => Dispatcher.UIThread.Post(() => ApplyPreviewWorkspace(snapshot)));
        _clockSubscription = Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Subscribe(__ =>
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateGeneratedAgoText();
                    _ = RefreshMobileRosterAsync();
                })
            );
        _watchSubscription = Observable
            .Interval(TimeSpan.FromMilliseconds(250))
            .Subscribe(__ =>
            {
                _ = PollActiveWatchAsync();
            });

        RefreshEnvironment();
        RefreshGuidedActionMetadata();
        RefreshGuidedActionActions();
        ApplyLogbookMutationOption(SelectedLogbookMutationOption);
        RefreshInventoryOwnerEntries();
        RefreshInventoryEditorActions();
        RefreshInventoryLiveActions();
        RefreshLogbookEditorActions();
        RefreshSheetEditorActions();
        RefreshSpellTechEditorActions();
        RefreshMobileEditorActions();
        RefreshGameDataCatalogActions();
    }

    partial void OnSelectedScenarioChanged(ArcanumDebuggerPreviewScenario? value)
    {
        if (value is null)
            return;

        SelectedScenarioSummary = value.Summary;
        _workspaceRequests.OnNext(value);
    }

    partial void OnSelectedLiveRuntimeChanged(LiveRuntimeSnapshot? value)
    {
        CanAttachSelectedRuntime = value is not null;
        AttachTargetText = value is null
            ? "Select a detected live runtime to attach."
            : $"Ready to attach to {value.DisplayName} using the detected runtime profile.";

        if (value is not null)
            SelectScenario(value.ScenarioKey);
    }

    partial void OnSelectedLiveTimelineEventChanged(WatchEventSnapshot? value)
    {
        SelectedLiveTimelineEventSummaryText = value is null
            ? "Select one live timeline event to reuse any detected object-handle candidates."
            : $"{value.SemanticEvent} · {value.Signature}";
        SelectedLiveTimelineEventHandleSummaryText = value is { CandidateHandles.Count: > 0 }
            ? string.Join(", ", value.CandidateHandles)
            : "No handle candidates detected on the selected event.";
        CanUseSelectedEventHandle = value is { CandidateHandles.Count: > 0 } || value?.SuggestedHandleHex is not null;

        if (value is not null)
            ObjectProbeHandleText = value.SuggestedHandleHex ?? value.CandidateHandles.FirstOrDefault() ?? string.Empty;
        else
            RefreshObjectProbeActions();
    }

    partial void OnSelectedFunctionCallOptionChanged(FunctionDefinition? value)
    {
        if (value is not { } function || string.IsNullOrEmpty(function.Key))
        {
            RefreshFunctionCallMetadata();
            RefreshFunctionCallActions();
            return;
        }

        if (!FunctionCallTargetText.Equals(function.Key, StringComparison.OrdinalIgnoreCase))
            FunctionCallTargetText = function.Key;

        RefreshFunctionCallMetadata();
        RefreshFunctionCallActions();
    }

    partial void OnSelectedGuidedActionOptionChanged(GuidedActionDescriptor? value)
    {
        RefreshGuidedActionMetadata();
        if (!GuidedActionShowTileInputs && GuidedActionQuickPickVisible)
            CloseGameDataQuickPick();

        RefreshGuidedActionActions();
        RefreshSupportedInputPanels();
    }

    partial void OnSelectedMobileRosterEntryChanged(MobileRosterEntrySnapshot? value)
    {
        if (value is not null && !MobileTargetHandleText.Equals(value.HandleHex, StringComparison.OrdinalIgnoreCase))
            MobileTargetHandleText = value.HandleHex;

        RefreshSelectedMobileHandleActions();
        RefreshMobileEditorActions();
    }

    partial void OnObjectProbeHandleTextChanged(string value) => RefreshObjectProbeActions();

    partial void OnFunctionCallTargetTextChanged(string value)
    {
        SyncSelectedFunctionCallOption(value);
        RefreshFunctionCallMetadata();
        RefreshFunctionCallActions();
    }

    partial void OnInstallPathChanged(string value) => RefreshEnvironment();

    partial void OnSelectedLaunchExecutableOptionChanged(LaunchExecutableKindOption? value) => RefreshEnvironment();

    partial void OnLaunchWindowedChanged(bool value) => RefreshEnvironment();

    private void RefreshSelectedMobileHandleActions() =>
        CanUseSelectedMobileRosterHandle = SelectedMobileRosterEntry is not null;

    partial void OnFunctionCallUseSuggestedCleanupChanged(bool value) => RefreshFunctionCallActions();

    partial void OnFunctionCallTimeoutTextChanged(string value) => RefreshFunctionCallActions();

    partial void OnFunctionCallStackArgumentsTextChanged(string value) => RefreshFunctionCallActions();

    partial void OnFunctionCallEcxTextChanged(string value) => RefreshFunctionCallActions();

    partial void OnFunctionCallEdxTextChanged(string value) => RefreshFunctionCallActions();

    partial void OnGuidedActionTravelerTextChanged(string value) => RefreshGuidedActionActions();

    partial void OnGuidedActionTileXTextChanged(string value) => RefreshGuidedActionActions();

    partial void OnGuidedActionTileYTextChanged(string value) => RefreshGuidedActionActions();

    partial void OnGuidedActionMapIdTextChanged(string value) => RefreshGuidedActionActions();

    partial void OnGuidedActionFlagsTextChanged(string value) => RefreshGuidedActionActions();

    partial void OnGuidedActionTimeoutTextChanged(string value) => RefreshGuidedActionActions();

    partial void OnMobileRosterFilterTextChanged(string value) => ApplyFilteredMobileRoster();

    partial void OnMobileRosterAutoRefreshChanged(bool value)
    {
        if (value)
            _ = RefreshMobileRosterAsync(force: true);

        RefreshMobileEditorActions();
    }

    partial void OnMobileTargetHandleTextChanged(string value) => RefreshMobileEditorActions();

    partial void OnMobileStatTokenTextChanged(string value) => RefreshMobileEditorActions();

    partial void OnMobileStatValueTextChanged(string value) => RefreshMobileEditorActions();

    partial void OnMobileSpawnPrototypeTokenTextChanged(string value)
    {
        RefreshMobileEditorActions();
        RefreshSupportedInputPanels();
    }

    partial void OnMobileSpawnAnchorTokenTextChanged(string value) => RefreshMobileEditorActions();

    partial void OnMobileMutationTimeoutTextChanged(string value) => RefreshMobileEditorActions();

    partial void OnInventoryOwnerTokenTextChanged(string value)
    {
        SyncSelectedInventoryOwnerEntry(value);
        RefreshInventoryEditorActions();
        RefreshInventoryLiveActions();
    }

    partial void OnInventoryPrototypeTokenTextChanged(string value)
    {
        RefreshInventoryEditorActions();
        RefreshSupportedInputPanels();
    }

    partial void OnInventoryLocationTextChanged(string value) => RefreshInventoryEditorActions();

    partial void OnInventoryItemHandleTextChanged(string value) => RefreshInventoryEditorActions();

    partial void OnInventoryTimeoutTextChanged(string value) => RefreshInventoryEditorActions();

    [RelayCommand]
    private void RefreshWorkspace() => RefreshEnvironment(forceWorkspaceRefresh: true);

    [RelayCommand]
    private async Task AttachSelectedRuntime()
    {
        var runtime = ResolveAttachCandidate();
        if (runtime is null)
        {
            ApplyDormantSession(
                "No live runtime selected",
                "Select one detected runtime scenario or run exactly one supported executable locally before attaching."
            );
            return;
        }

        try
        {
            var handle = await Task.Run(() => _sessionService.Attach(runtime, ResolveWorkspacePathOverride()));
            ReplaceActiveSession(handle);
            RefreshEnvironment(forceWorkspaceRefresh: true);
        }
        catch (Exception ex)
        {
            if (ActiveSession is null)
                ApplyDormantSession("Attach failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task LaunchAndAttach()
    {
        if (string.IsNullOrWhiteSpace(InstallPath))
        {
            ApplyDormantSession("Launch path required", "Enter an Arcanum install directory before launching.");
            return;
        }

        try
        {
            var handle = await Task.Run(() =>
                _sessionService.LaunchAndAttach(
                    new LaunchSessionRequest(
                        InstallPath,
                        SelectedLaunchExecutableOption?.Kind ?? ArcanumExecutableKind.Auto,
                        LaunchWindowed,
                        WorkspacePathHint: ResolveWorkspacePathOverride()
                    )
                )
            );
            ReplaceActiveSession(handle);
            RefreshEnvironment(forceWorkspaceRefresh: true);
        }
        catch (Exception ex)
        {
            if (ActiveSession is null)
                ApplyDormantSession("Launch and attach failed", ex.Message);
        }
    }

    [RelayCommand]
    private void RefreshActiveSession()
    {
        if (_activeSessionHandle is null)
        {
            ApplyDormantSession("No active session", "Attach to a running runtime or use launch-and-attach first.");
            return;
        }

        ApplySessionSnapshot(_sessionService.Refresh(_activeSessionHandle));
    }

    [RelayCommand]
    private void DisconnectSession()
    {
        DisposeActiveIntercept();
        _activeSessionHandle?.Dispose();
        _activeSessionHandle = null;
        DisposeActiveWatch();
        ApplyDormantSession(
            "Session closed",
            "The process attachment has been released. The workspace cards remain available for another attach."
        );
    }

    [RelayCommand]
    private async Task StartTimelineWatch()
    {
        if (ActiveSession is null)
        {
            ApplyDormantWatch("No active session", "Attach to a validated runtime before starting a live watch.");
            return;
        }

        if (!CanStartTimelineWatch)
        {
            ApplyDormantWatch("Watch unavailable", CreateWatchAvailabilitySummary(ActiveSession));
            return;
        }

        var preset = SelectedWatchPresetOption ?? WatchPresetOptions.FirstOrDefault();
        if (preset is null)
        {
            ApplyDormantWatch("No watch preset", "No timeline watch preset is currently available for this workspace.");
            return;
        }

        try
        {
            var handle = await Task.Run(() => _watchService.Start(new WatchStartRequest(ActiveSession, preset)));
            ReplaceActiveWatch(handle);
        }
        catch (Exception ex)
        {
            ApplyDormantWatch("Watch start failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshTimelineWatch()
    {
        if (_activeWatchHandle is null)
        {
            ApplyDormantWatch("No live watch running", "Start a watch from the active session first.");
            return;
        }

        await PollActiveWatchAsync(force: true);
    }

    [RelayCommand]
    private void StopTimelineWatch()
    {
        DisposeActiveWatch();
        ApplyDormantWatch("Live watch stopped", "The hook session has been torn down.");
    }

    [RelayCommand]
    private void UseSelectedEventHandle()
    {
        if (SelectedLiveTimelineEvent is null)
            return;

        ObjectProbeHandleText =
            SelectedLiveTimelineEvent.SuggestedHandleHex
            ?? SelectedLiveTimelineEvent.CandidateHandles.FirstOrDefault()
            ?? string.Empty;
    }

    [RelayCommand]
    private async Task InspectObjectHandle()
    {
        if (ActiveSession is null)
        {
            ApplyDormantObjectProbe("No active session", "Attach to a live runtime before probing object handles.");
            return;
        }

        var sourceLabel = SelectedLiveTimelineEvent is null
            ? "manual handle entry"
            : $"timeline event {SelectedLiveTimelineEvent.Sequence}";

        try
        {
            var snapshot = await Task.Run(() =>
                _objectProbeService.Inspect(new ObjectProbeRequest(ActiveSession, [ObjectProbeHandleText], sourceLabel))
            );
            ApplyObjectProbeSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyDormantObjectProbe("Object probe failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task InspectActivePlayer()
    {
        if (ActiveSession is null)
        {
            ApplyDormantObjectProbe("No active session", "Attach to a live runtime before probing object handles.");
            return;
        }

        if (!CanProbeStructuredState(ActiveSession))
        {
            ApplyDormantObjectProbe("Object probe unavailable", CreateObjectProbeAvailabilitySummary(ActiveSession));
            return;
        }

        ObjectProbeHandleText = "player";

        try
        {
            var snapshot = await Task.Run(() =>
                _objectProbeService.Inspect(new ObjectProbeRequest(ActiveSession, ["player"], "active player token"))
            );
            ApplyObjectProbeSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyDormantObjectProbe("Object probe failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task InvokeFunctionCall()
    {
        if (ActiveSession is null)
        {
            ApplyDormantFunctionCall(
                "No active session",
                "Attach to a validated runtime before invoking native functions."
            );
            return;
        }

        if (!CanInvokeFunctions(ActiveSession))
        {
            ApplyDormantFunctionCall("Function call unavailable", CreateFunctionCallAvailabilitySummary(ActiveSession));
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _functionCallService.Invoke(
                    new FunctionCallRequest(
                        ActiveSession,
                        FunctionCallTargetText,
                        FunctionCallStackArgumentsText,
                        FunctionCallEcxText,
                        FunctionCallEdxText,
                        FunctionCallUseSuggestedCleanup,
                        SelectedFunctionCallCleanupOption?.Mode ?? StackCleanupMode.Cdecl,
                        FunctionCallTimeoutText
                    )
                )
            );
            ApplyFunctionCallSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyDormantFunctionCall("Function call failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task InvokeGuidedAction()
    {
        if (ActiveSession is null)
        {
            ApplyDormantGuidedAction(
                "No active session",
                "Attach to a validated runtime before executing guided actions."
            );
            return;
        }

        if (!CanInvokeFunctions(ActiveSession))
        {
            ApplyDormantGuidedAction("Guided action unavailable", CreateGuidedActionAvailabilitySummary(ActiveSession));
            return;
        }

        var action = SelectedGuidedActionOption ?? GuidedActionOptions.FirstOrDefault();
        if (action is null)
        {
            ApplyDormantGuidedAction("No guided action selected", "Choose one guided action before running it.");
            return;
        }

        try
        {
            var snapshot = await _guidedActionService.ExecuteAsync(
                new GuidedActionRequest(
                    ActiveSession,
                    action.Key,
                    GuidedActionTravelerText,
                    GuidedActionTileXText,
                    GuidedActionTileYText,
                    GuidedActionMapIdText,
                    GuidedActionFlagsText,
                    GuidedActionTimeoutText,
                    ResolveWorkspacePathOverride()
                )
            );
            ApplyGuidedActionSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyDormantGuidedAction("Guided action failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task CreateInventoryItem()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantInventoryEditor(
                "No active session",
                ["Attach to a validated runtime before editing inventory."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantInventoryEditor(
                "Inventory editor unavailable",
                ["Attach to a validated runtime before editing inventory."]
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(InventoryPrototypeTokenText))
        {
            ApplyDormantInventoryEditor(
                "Prototype token required",
                [
                    "Enter a proto number, palette search term, explicit prototype handle, or use the local catalog before creating an item.",
                ]
            );
            return;
        }

        try
        {
            var prototype = await _prototypeResolutionService.ResolveAsync(
                new PrototypeResolutionRequest(session, InventoryPrototypeTokenText, ResolveWorkspacePathOverride())
            );
            if (!prototype.IsAvailable || prototype.Handle is not ulong prototypeHandle)
            {
                ApplyDormantInventoryEditor(
                    prototype.Status,
                    [prototype.Summary, .. prototype.Notes.Take(4).Select(static note => $"Note: {note}")]
                );
                return;
            }

            var snapshot = await Task.Run(() =>
                _inventoryEditorService.CreateItem(
                    new InventoryCreateRequest(
                        session,
                        InventoryOwnerTokenText,
                        prototypeHandle,
                        InventoryLocationText,
                        InventoryTimeoutText
                    )
                )
            );
            ApplyInventoryEditorSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.ItemHandleText))
                ObjectProbeHandleText = snapshot.ItemHandleText;
        }
        catch (Exception ex)
        {
            ApplyDormantInventoryEditor("Inventory create failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task DestroyInventoryItem()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantInventoryEditor(
                "No active session",
                ["Attach to a validated runtime before editing inventory."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantInventoryEditor(
                "Inventory editor unavailable",
                ["Attach to a validated runtime before editing inventory."]
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(InventoryItemHandleText))
        {
            ApplyDormantInventoryEditor(
                "Item handle required",
                ["Enter one live item handle before removing it from inventory."]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _inventoryEditorService.DestroyItem(
                    new InventoryDestroyRequest(session, InventoryItemHandleText, InventoryTimeoutText)
                )
            );
            ApplyInventoryEditorSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyDormantInventoryEditor("Inventory destroy failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task RefreshMobileRoster()
    {
        await RefreshMobileRosterAsync(force: true);
    }

    [RelayCommand]
    private async Task InspectSelectedMobile()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        ObjectProbeHandleText = entry.HandleHex;
        await InspectObjectHandle();
    }

    [RelayCommand]
    private void UseSelectedMobileForGuidedAction()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        GuidedActionTravelerText = entry.HandleHex;
    }

    [RelayCommand]
    private void UseSelectedMobileForInventoryOwner()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        InventoryOwnerTokenText = entry.HandleHex;
    }

    [RelayCommand]
    private void UseSelectedMobileForSpawnAnchor()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        MobileSpawnAnchorTokenText = entry.HandleHex;
    }

    [RelayCommand]
    private void UseSelectedMobileForSpellTechTarget()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        SpellTechTargetHandleText = entry.HandleHex;
    }

    [RelayCommand]
    private async Task SetMobileStat()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantMobileMutation(
                "No active session",
                ["Attach to a validated runtime before editing live mobiles."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantMobileMutation("Mobile editor unavailable", [CreateMobileEditorAvailabilitySummary(session)]);
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _mobileEntityService.SetStat(
                    new MobileStatWriteRequest(
                        session,
                        MobileTargetHandleText,
                        MobileStatTokenText,
                        MobileStatValueText,
                        MobileMutationTimeoutText
                    )
                )
            );
            ApplyMobileMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
                ObjectProbeHandleText = snapshot.TargetHandleText;

            await RefreshMobileRosterAsync(force: true);
        }
        catch (Exception ex)
        {
            ApplyDormantMobileMutation("Mobile stat write failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task KillMobile()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantMobileMutation(
                "No active session",
                ["Attach to a validated runtime before editing live mobiles."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantMobileMutation("Mobile editor unavailable", [CreateMobileEditorAvailabilitySummary(session)]);
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _mobileEntityService.Kill(
                    new MobileActionRequest(session, MobileTargetHandleText, MobileMutationTimeoutText)
                )
            );
            ApplyMobileMutationSnapshot(snapshot);
            await RefreshMobileRosterAsync(force: true);
        }
        catch (Exception ex)
        {
            ApplyDormantMobileMutation("Mobile kill failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task DespawnMobile()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantMobileMutation(
                "No active session",
                ["Attach to a validated runtime before editing live mobiles."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantMobileMutation("Mobile editor unavailable", [CreateMobileEditorAvailabilitySummary(session)]);
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _mobileEntityService.Despawn(
                    new MobileActionRequest(session, MobileTargetHandleText, MobileMutationTimeoutText)
                )
            );
            ApplyMobileMutationSnapshot(snapshot);
            await RefreshMobileRosterAsync(force: true);
        }
        catch (Exception ex)
        {
            ApplyDormantMobileMutation("Mobile despawn failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task SpawnMobile()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantMobileMutation(
                "No active session",
                ["Attach to a validated runtime before editing live mobiles."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantMobileMutation("Mobile editor unavailable", [CreateMobileEditorAvailabilitySummary(session)]);
            return;
        }

        if (string.IsNullOrWhiteSpace(MobileSpawnPrototypeTokenText))
        {
            ApplyDormantMobileMutation(
                "Prototype token required",
                [
                    "Enter a proto number, palette search term, explicit prototype handle, or use the local catalog before creating a mobile or world object.",
                ]
            );
            return;
        }

        try
        {
            var prototype = await _prototypeResolutionService.ResolveAsync(
                new PrototypeResolutionRequest(session, MobileSpawnPrototypeTokenText, ResolveWorkspacePathOverride())
            );
            if (!prototype.IsAvailable || prototype.Handle is not ulong prototypeHandle)
            {
                ApplyDormantMobileMutation(
                    prototype.Status,
                    [prototype.Summary, .. prototype.Notes.Take(4).Select(static note => $"Note: {note}")]
                );
                return;
            }

            var snapshot = await Task.Run(() =>
                _mobileEntityService.Spawn(
                    new MobileSpawnRequest(
                        session,
                        MobileSpawnAnchorTokenText,
                        prototypeHandle,
                        MobileMutationTimeoutText
                    )
                )
            );
            ApplyMobileMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                MobileTargetHandleText = snapshot.TargetHandleText;
                _pendingMobileSelectionHandle = snapshot.TargetHandleText;
            }

            await RefreshMobileRosterAsync(force: true);
        }
        catch (Exception ex)
        {
            ApplyDormantMobileMutation("Mobile spawn failed", [ex.Message]);
        }
    }

    private void RefreshEnvironment(bool forceWorkspaceRefresh = false)
    {
        var environmentSnapshot = _environmentService.Create(
            new EnvironmentRequest(
                [],
                string.IsNullOrWhiteSpace(InstallPath) ? null : InstallPath,
                SelectedLaunchExecutableOption?.Kind ?? ArcanumExecutableKind.Auto,
                LaunchWindowed
            )
        );

        ApplyEnvironment(environmentSnapshot);

        if (forceWorkspaceRefresh && SelectedScenario is { } scenario)
            _workspaceRequests.OnNext(scenario);
    }

    private void ApplyEnvironment(EnvironmentSnapshot snapshot)
    {
        Environment = snapshot;
        ProcessCandidates = snapshot.ProcessCandidates;
        LiveRuntimes = snapshot.LiveRuntimes;
        AttachSummaryText = snapshot.AttachSummary;
        LaunchPreviewSummaryText =
            snapshot.LaunchPreview?.Summary ?? "Enter an Arcanum install path to preview a launch plan.";
        LaunchPreviewDetailText = snapshot.LaunchPreview switch
        {
            { CanLaunch: true, ExecutablePath: { } executablePath, Arguments.Count: > 0 } preview =>
                $"{executablePath} :: {string.Join(' ', preview.Arguments)}",
            { CanLaunch: true, ExecutablePath: { } executablePath } => executablePath,
            { Error: { } error } => error,
            _ => "The shell will validate executable resolution and Community Edition-only launch overrides.",
        };

        RebuildScenarios(snapshot);
        RefreshFeaturedNotes();
    }

    private void RebuildScenarios(EnvironmentSnapshot snapshot)
    {
        var currentKey = SelectedScenario?.Key;
        IReadOnlyList<ArcanumDebuggerPreviewScenario> runtimeScenarios =
        [
            .. snapshot.LiveRuntimes.Select(CreateLiveRuntimeScenario),
        ];
        IReadOnlyList<ArcanumDebuggerPreviewScenario> combined =
        [
            .. runtimeScenarios,
            .. ArcanumDebuggerPreviewCatalog.Scenarios,
        ];
        Scenarios = combined;
        RefreshSelectedLiveRuntime(snapshot.LiveRuntimes);

        var selected =
            combined.FirstOrDefault(scenario => scenario.Key == currentKey)
            ?? combined.FirstOrDefault(scenario => scenario.Key == SelectedLiveRuntime?.ScenarioKey)
            ?? combined.FirstOrDefault()
            ?? throw new InvalidOperationException("At least one debugger scenario must exist.");
        SelectedScenario = selected;
    }

    private static ArcanumDebuggerPreviewScenario CreateLiveRuntimeScenario(LiveRuntimeSnapshot runtime) =>
        new(
            runtime.ScenarioKey,
            $"Live: {runtime.DisplayName}",
            runtime.Summary,
            new WorkspaceRequest(runtime.RuntimeProfile, HasModuleSymbols: false, [runtime.ProcessName])
        );

    private void ApplyPreviewWorkspace(WorkspaceSnapshot snapshot)
    {
        _previewWorkspace = snapshot;

        if (ActiveSession is null)
            ApplyWorkspace(snapshot, CreateWorkspaceSourceText());
    }

    private void ApplyWorkspace(WorkspaceSnapshot snapshot, string sourceText)
    {
        Workspace = snapshot;
        WorkspaceSourceText = sourceText;
        RuntimeTitle = snapshot.RuntimeProfile.DisplayName;
        RuntimeSubtitle = snapshot.RuntimeProfile.Notes;
        RuntimeKindText = snapshot.RuntimeProfile.RuntimeKind switch
        {
            ArcNET.Diagnostics.Contracts.RuntimeKind.Classic => "Classic executable",
            ArcNET.Diagnostics.Contracts.RuntimeKind.CommunityEdition => "Community Edition executable",
            _ => "Unknown executable",
        };
        SymbolStateText = snapshot.HasModuleSymbols ? "Module symbols available" : "Module symbols unavailable";
        SupportBadgeText = FormatSupportLevel(snapshot.Dashboard.Capabilities.SupportLevel);
        SupportSummaryText = CreateSupportSummary(snapshot.Dashboard.Capabilities.SupportLevel);
        ProcessTargetsText = string.Join(
            " / ",
            snapshot.Dashboard.RequestedProcessNames.Select(static name => $"{name}.exe")
        );
        RecommendedPanelCountText =
            $"{snapshot.PanelWorkflows.Count} panel{(snapshot.PanelWorkflows.Count == 1 ? string.Empty : "s")}";
        TimelineHeadlineText =
            snapshot.Timeline.RecommendedPresets.Count > 0
                ? $"{snapshot.Timeline.RecommendedPresets.Count} preset(s) ready"
                : "Preset workflow unavailable";
        TimelineStatusText = snapshot.Timeline.Notes.FirstOrDefault() ?? "Timeline guidance is ready.";
        FunctionStatusText = snapshot.FunctionBrowser.Notes.FirstOrDefault() ?? "Function browser guidance is ready.";
        ObjectExplorerStatusText =
            snapshot.ObjectExplorer.Notes.FirstOrDefault() ?? "Object explorer guidance is ready.";
        CapabilityChips = [.. CreateCapabilityChips(snapshot.Dashboard.Capabilities.Capabilities)];
        FeaturedPanels = [.. snapshot.PanelWorkflows];
        FeaturedProbeProfiles = [.. snapshot.Dashboard.RecommendedProbeProfiles];
        FeaturedTimelinePresets = [.. snapshot.Timeline.RecommendedPresets.Take(6).Select(CreateTimelineCard)];
        FeaturedAdvancedProfiles = [.. snapshot.Timeline.AdvancedProfiles.Take(6).Select(CreateAdvancedProfileCard)];
        DispatcherCandidates = [.. snapshot.FunctionBrowser.DispatcherCandidates];
        FeaturedFunctions = [.. snapshot.FunctionBrowser.Functions.Take(10).Select(CreateFunctionCard)];
        FeaturedObjectGroups = [.. snapshot.ObjectExplorer.RecommendedGroups.Take(8).Select(CreateObjectGroupCard)];

        (SupportAccentBrush, SupportSurfaceBrush) = ResolveSupportBrushes(snapshot.Dashboard.Capabilities.SupportLevel);
        RefreshFunctionCallOptions(snapshot);
        RefreshWatchPresetOptions(snapshot);
        RefreshFeaturedNotes();
        UpdateGeneratedAgoText();
    }

    private void UpdateGeneratedAgoText()
    {
        var age = DateTimeOffset.UtcNow - Workspace.GeneratedAtUtc;
        GeneratedAgoText =
            age.TotalSeconds < 5 ? "Snapshot generated just now."
            : age.TotalMinutes < 1 ? $"Snapshot generated {Math.Max(1, (int)age.TotalSeconds)} second(s) ago."
            : $"Snapshot generated {Math.Max(1, (int)age.TotalMinutes)} minute(s) ago.";
    }

    private static string CreateSupportSummary(ArcNET.Diagnostics.Contracts.RuntimeSupportLevel supportLevel) =>
        supportLevel switch
        {
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated =>
                "Validated sessions can use timeline, object inspection, and controlled mutation workflows with the highest confidence.",
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.SymbolAssisted =>
                "Symbol-assisted sessions should prioritize symbol-backed function and diagnostic workflows while watch-heavy features remain capability-gated.",
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Exploratory =>
                "Exploratory sessions expose reduced guarantees and should stay limited to read-oriented or explicitly validated workflows.",
            _ =>
                "Unsupported builds default to read-mostly behavior, warning banners, and conservative attach guidance.",
        };

    private static string FormatSupportLevel(ArcNET.Diagnostics.Contracts.RuntimeSupportLevel supportLevel) =>
        supportLevel switch
        {
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated => "Validated",
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.SymbolAssisted => "Symbol-assisted",
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Exploratory => "Exploratory",
            _ => "Unsupported",
        };

    private static IEnumerable<string> CreateCapabilityChips(
        ArcNET.Diagnostics.Contracts.DiagnosticsCapability capabilities
    ) =>
        Enum.GetValues<ArcNET.Diagnostics.Contracts.DiagnosticsCapability>()
            .Where(static capability => capability != ArcNET.Diagnostics.Contracts.DiagnosticsCapability.None)
            .Where(capability => capabilities.HasFlag(capability))
            .Select(static capability => Humanize(capability.ToString()));

    private static (IBrush AccentBrush, IBrush SurfaceBrush) ResolveSupportBrushes(
        ArcNET.Diagnostics.Contracts.RuntimeSupportLevel supportLevel
    ) =>
        supportLevel switch
        {
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Validated => (
                s_validatedAccentBrush,
                s_validatedSurfaceBrush
            ),
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.SymbolAssisted => (
                s_symbolAssistedAccentBrush,
                s_symbolAssistedSurfaceBrush
            ),
            ArcNET.Diagnostics.Contracts.RuntimeSupportLevel.Exploratory => (
                s_exploratoryAccentBrush,
                s_exploratorySurfaceBrush
            ),
            _ => (s_unsupportedAccentBrush, s_unsupportedSurfaceBrush),
        };

    private static DebuggerTimelineCard CreateTimelineCard(TimelinePresetDescriptor preset) =>
        new(
            preset.DisplayName,
            preset.Description,
            $"Areas: {string.Join(", ", preset.Areas)}",
            $"Hooks: {string.Join(", ", preset.HookKeys.Take(4))}{(preset.HookKeys.Count > 4 ? "..." : string.Empty)}",
            preset.UsesHighVolumeHooks
        );

    private static DebuggerAdvancedProfileCard CreateAdvancedProfileCard(
        ArcNET.Diagnostics.RuntimeWatchProfileDescriptor profile
    ) =>
        new(
            profile.Key,
            profile.Description,
            $"{profile.Hooks.Count} hook(s) · {string.Join(", ", profile.Hooks.Take(4).Select(static hook => hook.Key))}{(profile.Hooks.Count > 4 ? "..." : string.Empty)}"
        );

    private static DebuggerFunctionCard CreateFunctionCard(ArcNET.Diagnostics.FunctionDefinition function) =>
        new(function.Key, function.Site, function.Summary);

    private static DebuggerObjectGroupCard CreateObjectGroupCard(ObjectFieldGroupDescriptor group) =>
        new(
            group.DisplayName,
            group.Description,
            $"{group.Fields.Count} field(s), {group.NoiseFieldCount} noisy",
            string.Join(", ", group.Fields.Take(4).Select(static field => field.DisplayName))
        );

    private static string Humanize(string value)
    {
        if (value.Length == 0)
            return string.Empty;

        List<char> buffer = [];
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && char.IsLower(value[index - 1]))
                buffer.Add(' ');

            buffer.Add(current);
        }

        return new string([.. buffer]);
    }

    public void Dispose()
    {
        DisposeActiveIntercept();
        DisposeActiveWatch();
        _activeSessionHandle?.Dispose();
        _watchSubscription.Dispose();
        _clockSubscription.Dispose();
        _workspaceSubscription.Dispose();
        _workspaceRequests.Dispose();
    }

    private LiveRuntimeSnapshot? ResolveAttachCandidate()
    {
        if (SelectedLiveRuntime is not null)
            return SelectedLiveRuntime;

        if (SelectedScenario is { Key: { } key })
        {
            var selectedRuntime = LiveRuntimes.FirstOrDefault(runtime =>
                runtime.ScenarioKey.Equals(key, StringComparison.Ordinal)
            );
            if (selectedRuntime is not null)
                return selectedRuntime;
        }

        return LiveRuntimes.Count == 1 ? LiveRuntimes[0] : null;
    }

    private void ReplaceActiveSession(SessionHandle handle)
    {
        DisposeActiveIntercept();
        DisposeActiveWatch();
        _activeSessionHandle?.Dispose();
        _activeSessionHandle = handle;
        ApplySessionSnapshot(handle.Snapshot);
    }

    private void ApplySessionSnapshot(AttachedSessionSnapshot snapshot)
    {
        ActiveSession = snapshot;
        SessionHeadlineText = snapshot.DisplayName;
        SessionOriginText = snapshot.Origin == SessionOrigin.Launch ? "Launch origin" : "Attach origin";
        SessionSummaryText = snapshot.Summary;
        SessionDetailText = snapshot.Detail;
        SessionNotes = snapshot.Notes;
        SessionSupportBadgeText = FormatSupportLevel(snapshot.Capabilities.SupportLevel);
        SessionSupportSummaryText = CreateSupportSummary(snapshot.Capabilities.SupportLevel);
        SessionSupportAccentBrush = ResolveSupportBrushes(snapshot.Capabilities.SupportLevel).AccentBrush;
        CanStartTimelineWatch =
            !snapshot.HasExited
            && snapshot.Capabilities.Capabilities.HasFlag(
                ArcNET.Diagnostics.Contracts.DiagnosticsCapability.WatchHooks
            );
        ApplyWorkspace(WorkspaceService.CreateForSession(snapshot), CreateWorkspaceSourceText(snapshot));
        ApplyDormantGuidedAction(
            CanInvokeFunctions(snapshot) ? "No guided action executed." : "Guided action unavailable",
            CreateGuidedActionAvailabilitySummary(snapshot)
        );
        ApplyDormantFunctionCall(
            CanInvokeFunctions(snapshot) ? "No live function call executed." : "Function call unavailable",
            CreateFunctionCallAvailabilitySummary(snapshot)
        );
        ApplyDormantInventoryEditor(
            CanInvokeFunctions(snapshot) ? "No inventory mutation executed." : "Inventory editor unavailable",
            [CreateInventoryEditorAvailabilitySummary(snapshot)]
        );
        ApplyDormantSpellTechMutation(
            CanInvokeFunctions(snapshot) ? "No spell or tech mutation executed." : "Spell/tech editor unavailable",
            [CreateSpellTechEditorAvailabilitySummary(snapshot)]
        );
        ResetSpellTechLiveState(
            CanInvokeFunctions(snapshot) ? "Live progression not loaded." : "Live progression unavailable",
            CanInvokeFunctions(snapshot)
                ? "Load current spell colleges, known spells, schematics, tech disciplines, and tech skills for player or a selected companion."
                : CreateSpellTechEditorAvailabilitySummary(snapshot)
        );
        ApplyDormantMobileRoster(
            CanProbeStructuredState(snapshot) ? "No live mobile roster loaded." : "Mobile roster unavailable",
            CreateMobileRosterAvailabilitySummary(snapshot)
        );
        ApplyDormantMobileMutation(
            CanInvokeFunctions(snapshot) ? "No mobile mutation executed." : "Mobile editor unavailable",
            [CreateMobileEditorAvailabilitySummary(snapshot)]
        );
        ApplyDormantObjectProbe(
            CanProbeStructuredState(snapshot) ? "No live object inspected." : "Object probe unavailable",
            CreateObjectProbeAvailabilitySummary(snapshot)
        );
        ApplySessionLogbookEditorState(snapshot);
        ApplySessionGameDataCatalogState(snapshot);

        if (_activeWatchHandle is null)
        {
            ApplyDormantWatch(
                CanStartTimelineWatch ? "No live watch running." : "Watch unavailable",
                CreateWatchAvailabilitySummary(snapshot)
            );
        }

        RefreshFeaturedNotes();
        _ = RefreshMobileRosterAsync(force: true);
        _ = EnsureGameDataCatalogLoadedAsync();
    }

    private void ApplyDormantSession(string headline, string detail)
    {
        DisposeActiveIntercept();
        ActiveSession = null;
        SessionHeadlineText = headline;
        SessionOriginText = "Dormant";
        SessionSummaryText =
            "Attach to a detected runtime or launch from an install path to create a live debugger session.";
        SessionDetailText = detail;
        SessionNotes = [];
        SessionSupportBadgeText = "Dormant";
        SessionSupportSummaryText = "No process-backed session is attached.";
        SessionSupportAccentBrush = s_dormantAccentBrush;
        CanStartTimelineWatch = false;
        ApplyWorkspace(_previewWorkspace, CreateWorkspaceSourceText());
        ApplyDormantWatch("No live watch running.", "Attach to a live session before starting a watch.");
        ApplyDormantGuidedAction(
            "No guided action executed.",
            "Attach to a validated session before running guided game actions."
        );
        ApplyDormantFunctionCall(
            "No live function call executed.",
            "Attach to a validated session before invoking native functions through the dispatcher surface."
        );
        ApplyDormantInventoryEditor(
            "No inventory mutation executed.",
            ["Attach to a validated session before creating or destroying inventory items."]
        );
        ApplyDormantSpellTechMutation(
            "No spell or tech mutation executed.",
            ["Attach to a validated session before editing spell or technology progression."]
        );
        ResetSpellTechLiveState(
            "Live progression not loaded.",
            "Attach to a validated session before reading spell or technology progression."
        );
        ApplyDormantMobileRoster(
            "No live mobile roster loaded.",
            "Attach to a session with structured-state support before scanning live mobiles."
        );
        ApplyDormantMobileMutation(
            "No mobile mutation executed.",
            ["Attach to a validated session before editing live mobiles."]
        );
        ApplyDormantObjectProbe(
            "No live object inspected.",
            "Attach to a session with structured-state support before probing runtime object handles."
        );
        ApplyDormantGameDataCatalog(
            "Game-data catalog not loaded.",
            "Attach to a live runtime before loading the local workspace catalog."
        );
        ApplyDormantLogbookEditorState(
            "Journal catalog not loaded.",
            "Attach to a live runtime before loading the local journal catalog."
        );
        ApplyDormantLogbookMutation(
            "No live journal mutation executed.",
            ["Attach to a validated session before editing journal entries."]
        );
        RefreshFeaturedNotes();
    }

    private void ReplaceActiveWatch(WatchHandle handle)
    {
        DisposeActiveWatch();
        _activeWatchHandle = handle;
        ApplyWatchSnapshot(handle.Snapshot);
    }

    private void DisposeActiveWatch()
    {
        _activeWatchHandle?.Dispose();
        _activeWatchHandle = null;
    }

    private void ApplyWatchSnapshot(WatchSnapshot snapshot)
    {
        var selectedSequence = SelectedLiveTimelineEvent?.Sequence;
        ActiveWatch = snapshot;
        LiveTimelineStatusText = snapshot.Status;
        LiveTimelineSummaryText = snapshot.Summary;
        LiveTimelineEvents = snapshot.Events;
        SelectedLiveTimelineEvent = selectedSequence is null
            ? snapshot.Events.FirstOrDefault()
            : snapshot.Events.FirstOrDefault(item => item.Sequence == selectedSequence.Value)
                ?? snapshot.Events.FirstOrDefault();
    }

    private void ApplyDormantWatch(string status, string summary)
    {
        ActiveWatch = null;
        LiveTimelineStatusText = status;
        LiveTimelineSummaryText = summary;
        LiveTimelineEvents = [];
        SelectedLiveTimelineEvent = null;
    }

    private async Task PollActiveWatchAsync(bool force = false)
    {
        if (_activeWatchHandle is null || (_watchPollInFlight && !force))
            return;

        _watchPollInFlight = true;
        try
        {
            var snapshot = await Task.Run(() => _watchService.Poll(_activeWatchHandle));
            await Dispatcher.UIThread.InvokeAsync(() => ApplyWatchSnapshot(snapshot));
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DisposeActiveWatch();
                ApplyDormantWatch("Live watch ended", ex.Message);
            });
        }
        finally
        {
            _watchPollInFlight = false;
        }
    }

    private async Task RefreshMobileRosterAsync(bool force = false)
    {
        if (ActiveSession is not { } session)
        {
            if (force)
            {
                ApplyDormantMobileRoster(
                    "No live mobile roster loaded.",
                    "Attach to a session with structured-state support before scanning live mobiles."
                );
            }

            return;
        }

        if (!CanProbeStructuredState(session))
        {
            if (force)
                ApplyDormantMobileRoster("Mobile roster unavailable", CreateMobileRosterAvailabilitySummary(session));

            return;
        }

        if (_mobileRosterPollInFlight && !force)
            return;

        if (!MobileRosterAutoRefresh && !force)
            return;

        _mobileRosterPollInFlight = true;
        try
        {
            var snapshot = await Task.Run(() =>
                _mobileEntityService.ListMobiles(new MobileRosterRequest(session, MobileRosterMaxEntries))
            );
            ApplyMobileRosterSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ApplyDormantMobileRoster("Mobile roster failed", ex.Message);
        }
        finally
        {
            _mobileRosterPollInFlight = false;
        }
    }

    private void RefreshWatchPresetOptions(WorkspaceSnapshot snapshot)
    {
        var currentKey = SelectedWatchPresetOption?.Key;
        WatchPresetOptions =
        [
            .. snapshot
                .Timeline.RecommendedPresets.Concat(snapshot.Timeline.AvailableProbePresets)
                .DistinctBy(static preset => preset.Key, StringComparer.Ordinal),
        ];
        SelectedWatchPresetOption =
            WatchPresetOptions.FirstOrDefault(option => option.Key == currentKey)
            ?? WatchPresetOptions.FirstOrDefault();
    }

    private void RefreshFunctionCallOptions(WorkspaceSnapshot snapshot)
    {
        var currentKey = SelectedFunctionCallOption?.Key ?? FunctionCallTargetText;
        FunctionCallOptions = [.. snapshot.FunctionBrowser.Functions];
        var matchingOption = FunctionCallOptions.FirstOrDefault(option =>
            option.Key.Equals(currentKey, StringComparison.OrdinalIgnoreCase)
        );
        SelectedFunctionCallOption = string.IsNullOrEmpty(matchingOption.Key) ? null : matchingOption;
        RefreshFunctionCallMetadata();
        RefreshFunctionCallActions();
    }

    private void RefreshSelectedLiveRuntime(IReadOnlyList<LiveRuntimeSnapshot> runtimes)
    {
        var currentKey = SelectedLiveRuntime?.ScenarioKey;
        var nextSelectedRuntime = runtimes.FirstOrDefault(runtime =>
            runtime.ScenarioKey.Equals(currentKey, StringComparison.Ordinal)
        );

        if (nextSelectedRuntime is null && SelectedLiveRuntime is null)
            nextSelectedRuntime = runtimes.FirstOrDefault();

        SelectedLiveRuntime = nextSelectedRuntime;
    }

    private void SelectScenario(string scenarioKey)
    {
        var nextScenario = Scenarios.FirstOrDefault(scenario =>
            scenario.Key.Equals(scenarioKey, StringComparison.Ordinal)
        );
        if (nextScenario is not null && !ReferenceEquals(SelectedScenario, nextScenario))
            SelectedScenario = nextScenario;
    }

    private string CreateWorkspaceSourceText() =>
        SelectedScenario is { Key: { } key }
        && LiveRuntimes.Any(runtime => runtime.ScenarioKey.Equals(key, StringComparison.Ordinal))
            ? "Live session workspace"
            : "Reference workspace";

    private static string CreateWatchAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live hook polling is unavailable until a new session is attached.";

        return session.Capabilities.Capabilities.HasFlag(ArcNET.Diagnostics.Contracts.DiagnosticsCapability.WatchHooks)
            ? "Start a watch from the active session when you are ready."
            : "This session does not currently expose watch-hook capability, so the timeline stays read-mostly.";
    }

    private void ApplyGuidedActionSnapshot(GuidedActionSnapshot snapshot)
    {
        GuidedActionStatusText = snapshot.Status;
        GuidedActionSummaryText = snapshot.Summary;
        GuidedActionTargetSummaryText = string.IsNullOrWhiteSpace(snapshot.FunctionSite)
            ? GuidedActionTargetSummaryText
            : $"{snapshot.ActionDisplayName} · {snapshot.FunctionSite}";
        GuidedActionDispatcherText = snapshot.DispatcherText;
        GuidedActionExecutionDetailText = snapshot.ExecutionDetailText;
        GuidedActionResultText = snapshot.ResultText;
        RefreshGuidedActionActions();
    }

    private void ApplyDormantGuidedAction(string status, string summary)
    {
        GuidedActionStatusText = status;
        GuidedActionSummaryText = summary;
        GuidedActionDispatcherText = "Dispatcher result unavailable.";
        GuidedActionExecutionDetailText = "Target address and cleanup details will appear here after a live action.";
        GuidedActionResultText = "EAX and EDX values will appear here after a live action.";
        RefreshGuidedActionMetadata();
        RefreshGuidedActionActions();
    }

    private void ApplyInventoryEditorSnapshot(InventoryEditorSnapshot snapshot)
    {
        InventoryEditorStatusText = snapshot.Status;
        InventoryEditorDispatcherText = snapshot.DispatcherText;
        InventoryEditorExecutionDetailText = snapshot.ExecutionDetailText;
        InventoryEditorResultText = snapshot.ResultText;
        InventoryEditorResultLines = CreateInventoryEditorLines(snapshot);
        if (!string.IsNullOrWhiteSpace(snapshot.ItemHandleText))
            _pendingInventoryItemHandle = snapshot.ItemHandleText;

        QueueRefreshInventoryInspection();
        RefreshInventoryEditorActions();
    }

    private void ApplyDormantInventoryEditor(string status, IReadOnlyList<string> lines)
    {
        InventoryEditorStatusText = status;
        InventoryEditorDispatcherText = "Dispatcher result unavailable.";
        InventoryEditorExecutionDetailText =
            "Target address and hook details will appear here after a live inventory mutation.";
        InventoryEditorResultText = "Mutation result values will appear here after a live inventory mutation.";
        InventoryEditorResultLines = lines;
        RefreshInventoryEditorActions();
    }

    private void ApplyMobileRosterSnapshot(MobileRosterSnapshot snapshot)
    {
        MobileRosterStatusText = snapshot.Status;
        MobileRosterSummaryText = snapshot.Summary;
        _mobileRosterCache = snapshot.Mobiles;
        ApplyFilteredMobileRoster();
        RefreshInventoryOwnerEntries();
        RefreshMobileEditorActions();
    }

    private void ApplyDormantMobileRoster(string status, string summary)
    {
        MobileRosterStatusText = status;
        MobileRosterSummaryText = summary;
        _mobileRosterCache = [];
        MobileRosterEntries = [];
        SelectedMobileRosterEntry = null;
        RefreshInventoryOwnerEntries();
        RefreshMobileEditorActions();
    }

    private void ApplyMobileMutationSnapshot(MobileMutationSnapshot snapshot)
    {
        MobileMutationStatusText = snapshot.Status;
        MobileMutationDispatcherText = snapshot.DispatcherText;
        MobileMutationExecutionDetailText = snapshot.ExecutionDetailText;
        MobileMutationResultText = snapshot.ResultText;
        MobileMutationResultLines = CreateMobileMutationLines(snapshot);
        RefreshMobileEditorActions();
    }

    private void ApplyDormantMobileMutation(string status, IReadOnlyList<string> lines)
    {
        MobileMutationStatusText = status;
        MobileMutationDispatcherText = "Dispatcher result unavailable.";
        MobileMutationExecutionDetailText =
            "Target address and hook details will appear here after a live mobile mutation.";
        MobileMutationResultText = "Mutation result values will appear here after a live mobile mutation.";
        MobileMutationResultLines = lines;
        RefreshMobileEditorActions();
    }

    private void ApplyFunctionCallSnapshot(FunctionCallSnapshot snapshot)
    {
        FunctionCallStatusText = snapshot.Status;
        FunctionCallSummaryText = snapshot.Summary;
        if (!string.IsNullOrWhiteSpace(snapshot.TargetSite))
            FunctionCallTargetSummaryText = snapshot.TargetSite;

        FunctionCallDispatcherText = string.IsNullOrWhiteSpace(snapshot.DispatcherText)
            ? "Dispatcher result unavailable."
            : snapshot.DispatcherText;
        FunctionCallExecutionDetailText =
            string.IsNullOrWhiteSpace(snapshot.TargetAddressText) && string.IsNullOrWhiteSpace(snapshot.CleanupModeText)
                ? "Target address and cleanup details will appear here after a live call."
                : $"{snapshot.TargetAddressText} · {snapshot.CleanupModeText}".Trim(' ', '·');
        FunctionCallResultText = string.IsNullOrWhiteSpace(snapshot.ResultEaxText)
            ? "EAX and EDX values will appear here after a live call."
            : $"EAX {snapshot.ResultEaxText} · EDX {snapshot.ResultEdxText}";
        FunctionCallArguments = snapshot.Arguments;
        RefreshFunctionCallActions();
    }

    private void ApplyDormantFunctionCall(string status, string summary)
    {
        FunctionCallStatusText = status;
        FunctionCallSummaryText = summary;
        FunctionCallDispatcherText = "Dispatcher result unavailable.";
        FunctionCallExecutionDetailText = "Target address and cleanup details will appear here after a live call.";
        FunctionCallResultText = "EAX and EDX values will appear here after a live call.";
        FunctionCallArguments = [];
        RefreshFunctionCallMetadata();
        RefreshFunctionCallActions();
    }

    private void ApplyObjectProbeSnapshot(ObjectProbeSnapshot snapshot)
    {
        ObjectProbeStatusText = snapshot.Status;
        ObjectProbeSummaryText = snapshot.Summary;
        ObjectProbeObjects = snapshot.Objects;
        SelectedObjectProbeObject = snapshot.Objects.FirstOrDefault();
        if (snapshot.Objects.Count > 0)
            SelectedRootTabIndex = InspectRootTabIndex;

        RefreshObjectProbeActions();
    }

    private void ApplyDormantObjectProbe(string status, string summary)
    {
        ObjectProbeStatusText = status;
        ObjectProbeSummaryText = summary;
        ObjectProbeObjects = [];
        SelectedObjectProbeObject = null;
        RefreshObjectProbeActions();
    }

    private void RefreshObjectProbeActions()
    {
        CanInspectObjectHandle =
            ActiveSession is { } session
            && CanProbeStructuredState(session)
            && !string.IsNullOrWhiteSpace(ObjectProbeHandleText);
        CanInspectActivePlayer = ActiveSession is { } activeSession && CanProbeStructuredState(activeSession);
        CanUseSelectedEventHandle =
            SelectedLiveTimelineEvent is { CandidateHandles.Count: > 0 }
            || SelectedLiveTimelineEvent?.SuggestedHandleHex is not null;
    }

    private void RefreshGuidedActionActions()
    {
        var requiresTileCoordinates =
            SelectedGuidedActionOption?.Key.Equals("teleport_traveler", StringComparison.OrdinalIgnoreCase) == true;
        CanInvokeGuidedAction =
            ActiveSession is { } session
            && CanInvokeFunctions(session)
            && SelectedGuidedActionOption is not null
            && (
                !requiresTileCoordinates
                || (
                    !string.IsNullOrWhiteSpace(GuidedActionTileXText)
                    && !string.IsNullOrWhiteSpace(GuidedActionTileYText)
                )
            );
    }

    private void RefreshFunctionCallActions() =>
        CanInvokeFunctionCall =
            ActiveSession is { } session
            && CanInvokeFunctions(session)
            && !string.IsNullOrWhiteSpace(FunctionCallTargetText);

    private void RefreshInventoryEditorActions()
    {
        var hasActiveSession = ActiveSession is { } session && CanInvokeFunctions(session);
        CanCreateInventoryItem =
            hasActiveSession
            && !string.IsNullOrWhiteSpace(InventoryOwnerTokenText)
            && !string.IsNullOrWhiteSpace(InventoryPrototypeTokenText)
            && !string.IsNullOrWhiteSpace(InventoryLocationText);
        CanDestroyInventoryItem = hasActiveSession && !string.IsNullOrWhiteSpace(InventoryItemHandleText);
    }

    private void RefreshMobileEditorActions()
    {
        var hasReadableSession = ActiveSession is { } readableSession && CanProbeStructuredState(readableSession);
        var hasWritableSession = ActiveSession is { } writableSession && CanInvokeFunctions(writableSession);
        CanRefreshMobileRoster = hasReadableSession;
        CanInspectSelectedMobile = hasReadableSession && SelectedMobileRosterEntry is not null;
        CanSetMobileStat =
            hasWritableSession
            && !string.IsNullOrWhiteSpace(MobileTargetHandleText)
            && !string.IsNullOrWhiteSpace(MobileStatTokenText)
            && !string.IsNullOrWhiteSpace(MobileStatValueText);
        CanKillMobile = hasWritableSession && !string.IsNullOrWhiteSpace(MobileTargetHandleText);
        CanDespawnMobile = hasWritableSession && !string.IsNullOrWhiteSpace(MobileTargetHandleText);
        CanSpawnMobile = hasWritableSession && !string.IsNullOrWhiteSpace(MobileSpawnPrototypeTokenText);
    }

    private static bool CanProbeStructuredState(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.Capabilities.Capabilities.HasFlag(
            ArcNET.Diagnostics.Contracts.DiagnosticsCapability.ReadStructuredState
        );

    private static bool CanInvokeFunctions(AttachedSessionSnapshot session) =>
        !session.HasExited
        && session.Capabilities.Capabilities.HasFlag(
            ArcNET.Diagnostics.Contracts.DiagnosticsCapability.InvokeFunctions
        );

    private static string CreateObjectProbeAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so object-pool inspection is unavailable until a new session is attached.";

        return session.Capabilities.Capabilities.HasFlag(
            ArcNET.Diagnostics.Contracts.DiagnosticsCapability.ReadStructuredState
        )
            ? session.Capabilities.Capabilities.HasFlag(
                ArcNET.Diagnostics.Contracts.DiagnosticsCapability.InvokeFunctions
            ) && session.RuntimeProfile.SupportsCatalogRvas
                ? "Select a live timeline event, enter a handle, or inspect the active player to inspect runtime object-pool identity and expand richer getter-backed detail cards."
                : "Select a live timeline event, enter a handle, or inspect the active player to inspect runtime object-pool identity."
            : "This session does not currently expose structured-state capability, so live object probing stays disabled.";
    }

    private static string CreateFunctionCallAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so native dispatcher calls are unavailable until a new session is attached.";

        return session.Capabilities.Capabilities.HasFlag(
            ArcNET.Diagnostics.Contracts.DiagnosticsCapability.InvokeFunctions
        )
            ? "Use guided game actions when possible; the raw native-call lane remains available for research workflows that still need direct function keys or RVAs."
            : "This session does not currently expose live function-invocation capability, so the function call surface stays disabled.";
    }

    private static string CreateGuidedActionAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so guided actions are unavailable until a new session is attached.";

        return session.Capabilities.Capabilities.HasFlag(
            ArcNET.Diagnostics.Contracts.DiagnosticsCapability.InvokeFunctions
        )
            ? "Choose one guided game action and describe the destination or target in gameplay terms."
            : "This session does not currently expose live function-invocation capability, so guided game actions stay disabled.";
    }

    private static string CreateInventoryEditorAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live inventory edits are unavailable until a new session is attached.";

        return session.Capabilities.Capabilities.HasFlag(
            ArcNET.Diagnostics.Contracts.DiagnosticsCapability.InvokeFunctions
        )
            ? "Create items from prototype tokens or destroy live item handles through native inventory hooks."
            : "This session does not currently expose live function-invocation capability, so the inventory editor stays disabled.";
    }

    private static string CreateMobileRosterAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so the live mobile roster is unavailable until a new session is attached.";

        return CanProbeStructuredState(session)
            ? "Scan the live object pool for PC and NPC instances, then select one mobile to mutate or inspect."
            : "This session does not currently expose structured-state capability, so the live mobile roster stays disabled.";
    }

    private static string CreateMobileEditorAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live mobile edits are unavailable until a new session is attached.";

        return CanInvokeFunctions(session)
            ? "Edit one mobile base stat, trigger critter_kill, destroy a live mobile, or spawn a new mobile from a prototype token."
            : "This session does not currently expose live function-invocation capability, so the mobile editor stays disabled.";
    }

    private void SyncSelectedFunctionCallOption(string targetText)
    {
        if (string.IsNullOrWhiteSpace(targetText))
        {
            SelectedFunctionCallOption = null;
            return;
        }

        var matchingOption = FunctionCallOptions.FirstOrDefault(option =>
            option.Key.Equals(targetText.Trim(), StringComparison.OrdinalIgnoreCase)
        );
        if (string.IsNullOrEmpty(matchingOption.Key))
        {
            SelectedFunctionCallOption = null;
            return;
        }

        if (
            SelectedFunctionCallOption is { } current
            && current.Key.Equals(matchingOption.Key, StringComparison.Ordinal)
        )
            return;

        SelectedFunctionCallOption = matchingOption;
    }

    private void RefreshFunctionCallMetadata()
    {
        if (SelectedFunctionCallOption is { } selectedOption)
        {
            FunctionCallTargetSummaryText = $"{selectedOption.Site} · {selectedOption.Summary}";
            FunctionCallExampleText = selectedOption.Example ?? "No catalog example is available for this target yet.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FunctionCallTargetText))
        {
            FunctionCallTargetSummaryText = "Known-function metadata or raw RVA resolution will appear here.";
            FunctionCallExampleText =
                "Tip: use handle(player) in stack arguments or handle_low(...) and handle_high(...) helpers for split object handles.";
            return;
        }

        var trimmed = FunctionCallTargetText.Trim();
        if (
            (
                trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("rva:", StringComparison.OrdinalIgnoreCase)
            ) && TryFormatRawTarget(trimmed, out var targetSummary)
        )
        {
            FunctionCallTargetSummaryText = targetSummary;
            FunctionCallExampleText =
                "Raw RVA targets use the selected cleanup mode unless you point at one known catalog function.";
            return;
        }

        FunctionCallTargetSummaryText = $"Unresolved target '{trimmed}'.";
        FunctionCallExampleText = "Use a known function key such as ui_start_dialog or a raw RVA like 0x000609E0.";
    }

    private void RefreshGuidedActionMetadata()
    {
        if (SelectedGuidedActionOption is not { } action)
        {
            GuidedActionTargetSummaryText = "Guided action metadata will appear here.";
            GuidedActionHintText =
                "Traveler can be player or a raw object handle. Leave map id at -1 to stay on the current map.";
            GuidedActionShowTileInputs = true;
            GuidedActionShowMapIdInput = true;
            GuidedActionShowFlagsInput = true;
            GuidedActionTravelerPlaceholderText = "Traveler handle or player";
            GuidedActionTileXPlaceholderText = "Tile X";
            GuidedActionTileYPlaceholderText = "Tile Y";
            GuidedActionMapIdPlaceholderText = "Map id (-1 current map)";
            GuidedActionFlagsPlaceholderText = "Flags";
            return;
        }

        if (FunctionCatalog.TryGetDefinition(action.FunctionKey, out var function))
        {
            GuidedActionTargetSummaryText = $"{action.DisplayName} · {function.Site}";
            if (action.Key.Equals("discover_world_map_locations", StringComparison.OrdinalIgnoreCase))
            {
                GuidedActionHintText =
                    "ArcNET loads the world-area catalog from the attached install, marks every area known, and only walks anchor teleports when the traveler is already standing on the world map.";
                GuidedActionShowTileInputs = false;
                GuidedActionShowMapIdInput = false;
                GuidedActionShowFlagsInput = false;
                GuidedActionTravelerPlaceholderText = "Traveler handle or player";
                GuidedActionTileXPlaceholderText = "Unused for discovery";
                GuidedActionTileYPlaceholderText = "Unused for discovery";
                GuidedActionMapIdPlaceholderText = "Unused for discovery";
                GuidedActionFlagsPlaceholderText = "Unused for discovery";
                return;
            }

            GuidedActionHintText =
                "Traveler can be player or a raw object handle. Leave map id at -1 to stay on the current map, and use flags only when you intentionally need a non-default teleport mode.";
            GuidedActionShowTileInputs = true;
            GuidedActionShowMapIdInput = true;
            GuidedActionShowFlagsInput = true;
            GuidedActionTravelerPlaceholderText = "Traveler handle or player";
            GuidedActionTileXPlaceholderText = "Tile X";
            GuidedActionTileYPlaceholderText = "Tile Y";
            GuidedActionMapIdPlaceholderText = "Map id (-1 current map)";
            GuidedActionFlagsPlaceholderText = "Flags";
            return;
        }

        GuidedActionTargetSummaryText = $"{action.DisplayName} · {action.FunctionKey}";
        GuidedActionHintText = action.Summary;
        GuidedActionShowTileInputs = true;
        GuidedActionShowMapIdInput = true;
        GuidedActionShowFlagsInput = true;
        GuidedActionTravelerPlaceholderText = "Traveler handle or player";
        GuidedActionTileXPlaceholderText = "Tile X";
        GuidedActionTileYPlaceholderText = "Tile Y";
        GuidedActionMapIdPlaceholderText = "Map id (-1 current map)";
        GuidedActionFlagsPlaceholderText = "Flags";
    }

    private static IReadOnlyList<string> CreateInventoryEditorLines(InventoryEditorSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.OwnerHandleText))
            lines.Add($"Owner handle: {snapshot.OwnerHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.OwnerTargetText))
            lines.Add($"Owner: {snapshot.OwnerTargetText}");

        if (!string.IsNullOrWhiteSpace(snapshot.ItemHandleText))
            lines.Add($"Item handle: {snapshot.ItemHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.PrototypeHandleText))
            lines.Add($"Prototype handle: {snapshot.PrototypeHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.InventoryLocationText))
            lines.Add($"Slot: {snapshot.InventoryLocationText}");

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private void ApplyFilteredMobileRoster()
    {
        var filter = NormalizeMobileFilter(MobileRosterFilterText);
        var filteredMobiles =
            filter.Length == 0
                ? _mobileRosterCache
                : [.. _mobileRosterCache.Where(entry => MatchesMobileFilter(entry, filter))];
        var selectionHandle = _pendingMobileSelectionHandle ?? SelectedMobileRosterEntry?.HandleHex;
        MobileRosterEntries = filteredMobiles;
        if (!string.IsNullOrWhiteSpace(selectionHandle))
        {
            var matchingEntry = filteredMobiles.FirstOrDefault(entry =>
                entry.HandleHex.Equals(selectionHandle, StringComparison.OrdinalIgnoreCase)
            );
            if (matchingEntry is not null)
            {
                if (
                    !EqualityComparer<MobileRosterEntrySnapshot?>.Default.Equals(
                        SelectedMobileRosterEntry,
                        matchingEntry
                    )
                )
                    SelectedMobileRosterEntry = matchingEntry;

                _pendingMobileSelectionHandle = null;
                return;
            }
        }

        if (SelectedMobileRosterEntry is not null)
            SelectedMobileRosterEntry = null;
    }

    private static IReadOnlyList<string> CreateMobileMutationLines(MobileMutationSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        if (!string.IsNullOrWhiteSpace(snapshot.PrototypeHandleText))
            lines.Add($"Prototype handle: {snapshot.PrototypeHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.StatNameText))
            lines.Add($"Stat: {snapshot.StatNameText}");

        if (!string.IsNullOrWhiteSpace(snapshot.StatValueText))
            lines.Add($"Value: {snapshot.StatValueText}");

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static bool MatchesMobileFilter(MobileRosterEntrySnapshot entry, string filter) =>
        NormalizeMobileFilter(entry.HandleHex).Contains(filter, StringComparison.Ordinal)
        || NormalizeMobileFilter(entry.DisplayText).Contains(filter, StringComparison.Ordinal)
        || NormalizeMobileFilter(entry.ObjectIdText).Contains(filter, StringComparison.Ordinal)
        || NormalizeMobileFilter(entry.PrototypeText).Contains(filter, StringComparison.Ordinal)
        || NormalizeMobileFilter(entry.ObjectTypeText).Contains(filter, StringComparison.Ordinal);

    private static string NormalizeMobileFilter(string? value)
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

    private static bool TryFormatRawTarget(string text, out string formatted)
    {
        var token = text.StartsWith("rva:", StringComparison.OrdinalIgnoreCase) ? text[4..] : text;
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rva))
            {
                formatted = CodeCatalog.FormatModuleAddress(rva);
                return true;
            }
        }
        else if (uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalRva))
        {
            formatted = CodeCatalog.FormatModuleAddress(decimalRva);
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    private void RefreshFeaturedNotes()
    {
        FeaturedNotes =
        [
            .. (ActiveSession?.Notes ?? [])
                .Concat(Environment.Notes)
                .Concat(Workspace.Dashboard.Notes)
                .Concat(Workspace.Timeline.Notes)
                .Concat(Workspace.ObjectExplorer.Notes)
                .Concat(Workspace.FunctionBrowser.Notes)
                .Distinct(StringComparer.Ordinal)
                .Take(12),
        ];
    }

    private static readonly IBrush s_validatedAccentBrush = CreateBrush("#FF4F7C45");
    private static readonly IBrush s_validatedSurfaceBrush = CreateBrush("#FFF3F8EC");
    private static readonly IBrush s_symbolAssistedAccentBrush = CreateBrush("#FF2C7180");
    private static readonly IBrush s_symbolAssistedSurfaceBrush = CreateBrush("#FFEDF7F8");
    private static readonly IBrush s_exploratoryAccentBrush = CreateBrush("#FFB56A1E");
    private static readonly IBrush s_exploratorySurfaceBrush = CreateBrush("#FFFCF3E7");
    private static readonly IBrush s_unsupportedAccentBrush = CreateBrush("#FF9A402B");
    private static readonly IBrush s_unsupportedSurfaceBrush = CreateBrush("#FFF9ECE8");
    private static readonly IBrush s_dormantAccentBrush = CreateBrush("#FF7C6A55");
    private const int MobileRosterMaxEntries = 128;

    private static IBrush CreateBrush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed record class DebuggerTimelineCard(
    string DisplayName,
    string Description,
    string AreaSummary,
    string HookSummary,
    bool UsesHighVolumeHooks
);

public sealed record class DebuggerAdvancedProfileCard(string Key, string Description, string HookSummary);

public sealed record class DebuggerFunctionCard(string Key, string Site, string Summary);

public sealed record class DebuggerObjectGroupCard(
    string DisplayName,
    string Description,
    string FieldSummary,
    string PreviewFields
);

public sealed record class LaunchExecutableKindOption(ArcanumExecutableKind Kind, string Label);

public sealed record class DebuggerCleanupModeOption(StackCleanupMode Mode, string Label);
