using System.Globalization;
using ArcanumDebugger.App.Composition;
using ArcNET.Diagnostics;
using ArcNET.Formats;
using ArcNET.GameData.SaveGames;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly AuditService _auditService;
    private readonly PrototypeResolutionService _prototypeResolutionService;
    private readonly ReadService _readService;
    private readonly SheetService _sheetService;
    private readonly ScriptAttachmentService _scriptAttachmentService;
    private readonly LogbookService _logbookService;
    private readonly InterceptService _interceptService;
    private readonly InterceptTargetResolver _interceptTargetResolver;
    private readonly ModuleSymbolQueryService _moduleSymbolQueryService;
    private readonly RuntimeStatusService _runtimeStatusService;
    private readonly CrashDumpService _crashDumpService;
    private InterceptHandle? _activeInterceptHandle;

    [ObservableProperty]
    private IReadOnlyList<DebuggerFeatureLaneGroup> diagnosticsFeatureGroups = CreateDiagnosticsFeatureGroups();

    [ObservableProperty]
    private string diagnosticsFeatureSummaryText = CreateDiagnosticsFeatureSummary();

    [ObservableProperty]
    private string auditStatusText = "No advanced runtime audit run yet.";

    [ObservableProperty]
    private IReadOnlyList<string> auditResultLines = [];

    [ObservableProperty]
    private bool auditIncludeHooks;

    [ObservableProperty]
    private string auditHookSelectorsText = "session-core";

    [ObservableProperty]
    private string auditHookDurationMillisecondsText = "1500";

    [ObservableProperty]
    private bool auditIncludeWatchPass = true;

    [ObservableProperty]
    private bool auditIncludeInterceptPass;

    [ObservableProperty]
    private string auditStackCaptureDwordCountText = "4";

    [ObservableProperty]
    private bool auditStopOnFailure;

    [ObservableProperty]
    private string prototypeTokenText = string.Empty;

    [ObservableProperty]
    private string prototypeStatusText = "No prototype lookup yet.";

    [ObservableProperty]
    private IReadOnlyList<string> prototypeResultLines = [];

    partial void OnPrototypeTokenTextChanged(string value) => RefreshSupportedInputPanels();

    [ObservableProperty]
    private string readAdapterKeyText = "story-state";

    [ObservableProperty]
    private string readArgumentsText = string.Empty;

    [ObservableProperty]
    private string readStatusText = "No runtime detail read yet.";

    [ObservableProperty]
    private IReadOnlyList<string> readResultLines = [];

    [ObservableProperty]
    private string sheetHandleTokenText = "player";

    [ObservableProperty]
    private string sheetLabelText = "level";

    [ObservableProperty]
    private string sheetDiffDelayText = "500";

    [ObservableProperty]
    private string sheetStatusText = "No character sheet read yet.";

    [ObservableProperty]
    private IReadOnlyList<string> sheetResultLines = [];

    [ObservableProperty]
    private string scriptHandleTokenText = "player";

    [ObservableProperty]
    private string scriptAttachmentPointText = "dialog";

    [ObservableProperty]
    private string scriptStatusText = "No script attachment read yet.";

    [ObservableProperty]
    private IReadOnlyList<string> scriptResultLines = [];

    [ObservableProperty]
    private string logbookHandleTokenText = "player";

    [ObservableProperty]
    private string logbookPageTokenText = "quests";

    [ObservableProperty]
    private string logbookStatusText = "No journal page loaded yet.";

    [ObservableProperty]
    private IReadOnlyList<string> logbookResultLines = [];

    [ObservableProperty]
    private string logbookDisplaySummaryText =
        "Use the journal reader for quests, rumors, reputations, blessings, injuries, background, and keys.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> logbookPageOptions =
    [
        new("quests", "Quest journal", "Decoded quest entries with state text."),
        new("rumors", "Rumors and notes", "Decoded rumor text, note pages, and quelled state."),
        new("reputations", "Reputations", "Decoded reputation entries for the selected character."),
        new("blessings", "Blessings and curses", "Decoded blessing and curse entries with timestamps."),
        new("kills", "Kills and injuries", "Decoded combat history, summary counters, and active injuries."),
        new("background", "Background", "Decoded background name and description text."),
        new("keys", "Keyring", "Decoded keyring contents and ids."),
        new("all", "All pages", "Load the complete journal payload in one read."),
    ];

    [ObservableProperty]
    private DebuggerChoiceOption? selectedLogbookPageOption = new(
        "quests",
        "Quest journal",
        "Decoded quest entries with state text."
    );

    [ObservableProperty]
    private string logbookPageDescriptionText = "Decoded quest entries with state text.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerLogbookSummaryCard> logbookHighlights = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerLogbookSection> logbookSections = [];

    [ObservableProperty]
    private IReadOnlyList<string> logbookNotes = [];

    [ObservableProperty]
    private bool hasLogbookSections;

    [ObservableProperty]
    private bool showLogbookFallbackLines;

    [ObservableProperty]
    private string runtimeStatusText = "No runtime status read yet.";

    [ObservableProperty]
    private IReadOnlyList<string> runtimeStatusResultLines = [];

    [ObservableProperty]
    private string runtimeActionPointsText = string.Empty;

    [ObservableProperty]
    private string runtimeModuleFilterText = string.Empty;

    [ObservableProperty]
    private string runtimeModuleLimitText = "25";

    [ObservableProperty]
    private bool runtimeModuleDuplicatesOnly;

    [ObservableProperty]
    private string runtimeModuleStatusText = "No live symbol lookup yet.";

    [ObservableProperty]
    private IReadOnlyList<string> runtimeModuleResultLines = [];

    [ObservableProperty]
    private string runtimeDumpPathText = Path.Combine(
        global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.LocalApplicationData),
        "ArcanumDebugger",
        "Dumps",
        "Arcanum-runtime.dmp"
    );

    [ObservableProperty]
    private string runtimeDumpDirectoryText = Path.Combine(
        global::System.Environment.GetFolderPath(global::System.Environment.SpecialFolder.LocalApplicationData),
        "ArcanumDebugger",
        "Dumps"
    );

    [ObservableProperty]
    private string runtimeDumpProcessNameText = "Arcanum.exe";

    [ObservableProperty]
    private string runtimeDumpCountText = "5";

    [ObservableProperty]
    private string runtimeDumpKindText = "mini";

    [ObservableProperty]
    private string runtimeDumpStatusText = "No recovery or dump action yet.";

    [ObservableProperty]
    private IReadOnlyList<string> runtimeDumpResultLines = [];

    [ObservableProperty]
    private string interceptTargetText = "teleport_do";

    [ObservableProperty]
    private string interceptStackCaptureCountText = "8";

    [ObservableProperty]
    private bool interceptSkipOriginal;

    [ObservableProperty]
    private string interceptCleanupBytesText = "0";

    [ObservableProperty]
    private string interceptReturnEaxText = string.Empty;

    [ObservableProperty]
    private string interceptReturnEdxText = string.Empty;

    [ObservableProperty]
    private string interceptRegisterOverridesText = string.Empty;

    [ObservableProperty]
    private string interceptArgumentOverridesText = string.Empty;

    [ObservableProperty]
    private string interceptDereferencesText = "eax:16";

    [ObservableProperty]
    private string interceptStatusText = "No interception session yet.";

    [ObservableProperty]
    private IReadOnlyList<string> interceptResultLines = [];

    [ObservableProperty]
    private string offlineSaveDirectoryText = string.Empty;

    [ObservableProperty]
    private string offlineSlotText = "0001";

    [ObservableProperty]
    private string offlineSaveGsiPathText = string.Empty;

    [ObservableProperty]
    private string offlineSaveTfaiPathText = string.Empty;

    [ObservableProperty]
    private string offlineSaveTfafPathText = string.Empty;

    [ObservableProperty]
    private string offlineSaveStatusText = "No save analysis loaded yet.";

    [ObservableProperty]
    private IReadOnlyList<string> offlineSaveResultLines = [];

    [ObservableProperty]
    private string offlineRangeFirstSlotText = "0001";

    [ObservableProperty]
    private string offlineRangeLastSlotText = "0005";

    [ObservableProperty]
    private string offlineRangeStatusText = "No save comparison loaded yet.";

    [ObservableProperty]
    private IReadOnlyList<string> offlineRangeResultLines = [];

    [ObservableProperty]
    private string offlineRangeSemanticSummaryText =
        "Compare two or more saves to review progression, journal, and world-state deltas before inspecting raw file-time diffs.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightCard> offlineRangeHighlights = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightSection> offlineRangeSections = [];

    [ObservableProperty]
    private bool hasOfflineRangeSections;

    [ObservableProperty]
    private string offlineModulePathText = string.Empty;

    [ObservableProperty]
    private string offlineModuleFilterText = string.Empty;

    [ObservableProperty]
    private string offlineModuleLimitText = "25";

    [ObservableProperty]
    private bool offlineModuleDuplicatesOnly;

    [ObservableProperty]
    private string offlineModuleStatusText = "No module symbol lookup yet.";

    [ObservableProperty]
    private IReadOnlyList<string> offlineModuleResultLines = [];

    [ObservableProperty]
    private string offlineSourceRootText = string.Empty;

    [ObservableProperty]
    private string offlineSourceFilterText = string.Empty;

    [ObservableProperty]
    private string offlineSourceAreaText = string.Empty;

    [ObservableProperty]
    private string offlineSourceLimitText = "25";

    [ObservableProperty]
    private bool offlineSourceMissingOnly;

    [ObservableProperty]
    private bool offlineSourceCoveredOnly;

    [ObservableProperty]
    private string offlineSourceStatusText = "No source coverage audit yet.";

    [ObservableProperty]
    private IReadOnlyList<string> offlineSourceResultLines = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> offlineDumpTemplateOptions =
    [
        new("save-dir", "Save directory", "Dump a complete save directory with the combined file-time template set."),
        new("proto", "Prototype file", "Decode one .pro prototype into a readable object-oriented dump."),
        new("sector", "Sector file", "Decode one .sec sector file including embedded mobs and placements."),
        new("script", "Script file", "Decode one compiled .scr script into a readable listing."),
        new("dialog", "Dialog file", "Decode one .dlg dialog graph into readable branches."),
        new("message", "Message file", "Decode one .mes message table into readable key-value rows."),
        new("text-data", "Text data file", "Decode one text-data table into readable entries."),
        new("terrain", "Terrain file", "Decode one .tdf terrain file into readable tile metadata."),
        new("art", "Art file", "Decode one .art sprite file into readable frame metadata."),
        new("facwalk", "Facade walk file", "Decode one facade-walk file into readable walkability data."),
        new("jmp", "Jump-point file", "Decode one .jmp file into readable travel points."),
        new("map-props", "Map properties file", "Decode one .prp map-properties file into readable values."),
        new("save-info", "Save info file", "Decode one .gsi save-info file."),
        new("save-index", "Save index file", "Decode one .tfai save-index file."),
    ];

    [ObservableProperty]
    private DebuggerChoiceOption? selectedOfflineDumpTemplateOption = new(
        "save-dir",
        "Save directory",
        "Dump a complete save directory with the combined file-time template set."
    );

    [ObservableProperty]
    private string offlineDumpTemplateDescriptionText =
        "Dump a complete save directory with the combined file-time template set.";

    [ObservableProperty]
    private string offlineDumpPathText = string.Empty;

    [ObservableProperty]
    private string offlineDumpStatusText = "No offline report rendered yet.";

    [ObservableProperty]
    private IReadOnlyList<string> offlineDumpResultLines = [];

    [ObservableProperty]
    private string offlineDumpOutputText = string.Empty;

    [ObservableProperty]
    private string offlineDumpSemanticSummaryText =
        "Render a report template to review structured output before opening the full report text.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightCard> offlineDumpHighlights = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightSection> offlineDumpSections = [];

    [ObservableProperty]
    private bool hasOfflineDumpSections;

    [ObservableProperty]
    private string offlineSaveSemanticSummaryText =
        "Load a save slot to review player, journal, and world-state summaries before inspecting raw file-time details.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightCard> offlineSaveHighlights = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightSection> offlineSaveSections = [];

    [ObservableProperty]
    private bool hasOfflineSaveSections;

    [ObservableProperty]
    private string objectProbeSemanticSummaryText =
        "Select an inspected object to review decoded sections before inspecting raw runtime fields.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightCard> objectProbeHighlightCards = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerInsightSection> objectProbeSectionSummaries = [];

    [ObservableProperty]
    private bool hasObjectProbeSectionSummaries;

    partial void OnLogbookPageTokenTextChanged(string value)
    {
        InvalidateLoadedLogbookSnapshotIfRequestChanged(pageTokenText: value);
        RefreshSelectedLogbookPageOption();
    }

    partial void OnSelectedLogbookPageOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is null)
            return;

        LogbookPageDescriptionText = value.Description;
        if (!LogbookPageTokenText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            LogbookPageTokenText = value.Token;
    }

    partial void OnSelectedOfflineDumpTemplateOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is not null)
            OfflineDumpTemplateDescriptionText = value.Description;
    }

    partial void OnSelectedObjectProbeObjectChanged(ObjectProbeObjectSnapshot? value)
    {
        if (value is null)
        {
            ClearObjectProbePresentation();
            return;
        }

        ApplyObjectProbePresentation(value);
    }

    partial void OnActiveSessionChanged(AttachedSessionSnapshot? value)
    {
        if (value is null)
        {
            AuditStatusText = "No advanced runtime audit run yet.";
            AuditResultLines = [];
            PrototypeStatusText = "No prototype lookup yet.";
            PrototypeResultLines = [];
            ReadStatusText = "No runtime detail read yet.";
            ReadResultLines = [];
            SheetStatusText = "No character sheet read yet.";
            SheetResultLines = [];
            ScriptStatusText = "No script attachment read yet.";
            ScriptResultLines = [];
            LogbookStatusText = "No journal page loaded yet.";
            LogbookResultLines = [];
            ApplyLogbookFallback(
                "Use the journal reader for quests, rumors, reputations, blessings, injuries, background, and keys.",
                []
            );
            ResetSheetEditorState(
                "Load editable fields from one live sheet snapshot to browse stats, resistances, skills, spell mastery, colleges, and tech disciplines."
            );
            SheetMutationStatusText = "No live sheet mutation executed.";
            SheetMutationResultLines =
            [
                "Load editable fields, pick one row, then write a new value without hunting internal ids or remembering every sheet token.",
            ];
            SheetMutationDispatcherText = "Dispatcher result unavailable.";
            SheetMutationExecutionDetailText =
                "Target address and hook details will appear here after a live sheet mutation.";
            SheetMutationResultText = "Mutation result values will appear here after a live sheet mutation.";
            RuntimeStatusText = "No runtime status read yet.";
            RuntimeStatusResultLines = [];
            RuntimeModuleStatusText = "No live symbol lookup yet.";
            RuntimeModuleResultLines = [];
            RuntimeDumpStatusText = "No recovery or dump action yet.";
            RuntimeDumpResultLines = [];
            DisposeActiveIntercept();
            InterceptStatusText = "No interception session yet.";
            InterceptResultLines = [];
            return;
        }

        AuditStatusText = $"Ready to audit {value.DisplayName}.";
        PrototypeStatusText = "Enter a proto number, palette search term, or explicit prototype handle.";
        ReadStatusText = "Run one getter-backed read such as story-state, quest, stat, field, or script-local-flag.";
        SheetStatusText =
            "Read one sheet value, capture a full live sheet snapshot, or diff two snapshots for a handle token such as player.";
        ScriptStatusText = "Read one attachment point from a live handle token such as player.";
        LogbookStatusText = "Read one live logbook page from a handle token such as player.";
        ApplyLogbookFallback(
            "Read a live journal page to populate decoded sections such as quests, blessings, background, or keys.",
            []
        );
        ResetSheetEditorState(
            "Load editable fields from one live sheet snapshot to browse stats, resistances, skills, spell mastery, colleges, and tech disciplines."
        );
        SheetMutationStatusText = CanInvokeFunctions(value)
            ? "No live sheet mutation executed."
            : "Sheet editor unavailable";
        SheetMutationResultLines =
        [
            "Load editable fields, pick one row, then write a new value without hunting internal ids or remembering every sheet token.",
        ];
        SheetMutationDispatcherText = "Dispatcher result unavailable.";
        SheetMutationExecutionDetailText =
            "Target address and hook details will appear here after a live sheet mutation.";
        SheetMutationResultText = "Mutation result values will appear here after a live sheet mutation.";
        RuntimeStatusText =
            "Inspect runtime profile, fingerprint, action points, and current character-sheet identity.";
        RuntimeModuleStatusText =
            "Query live module symbols from the attached runtime to crosswalk names, RVAs, addresses, and duplicate matches.";
        RuntimeDumpStatusText =
            "Capture a manual dump or manage automatic WER LocalDumps configuration for the active runtime.";
        RuntimeDumpProcessNameText = $"{value.ProcessName}.exe";
        InterceptStatusText =
            "Start a live interception session to poll events, stack values, potential handles, and dereference captures.";
    }

    [RelayCommand]
    private async Task RunRuntimeAudit()
    {
        if (ActiveSession is not { } session)
        {
            AuditStatusText = "No active session";
            AuditResultLines = ["Attach to a live runtime before running the dispatcher/function audit."];
            return;
        }

        if (AuditIncludeHooks && !AuditIncludeWatchPass && !AuditIncludeInterceptPass)
        {
            AuditStatusText = "Hook audit mode incomplete";
            AuditResultLines = ["Enable at least one hook pass: watch and/or intercept."];
            return;
        }

        try
        {
            var request = CreateAuditRequest(session);
            var snapshot = await Task.Run(() => _auditService.Run(request));
            AuditStatusText = "Runtime audit completed";
            AuditResultLines = CreateAuditLines(snapshot);
        }
        catch (Exception ex)
        {
            AuditStatusText = "Runtime audit failed";
            AuditResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task ResolvePrototype()
    {
        if (ActiveSession is not { } session)
        {
            PrototypeStatusText = "No active session";
            PrototypeResultLines = ["Attach to a live runtime before resolving prototype handles."];
            return;
        }

        if (string.IsNullOrWhiteSpace(PrototypeTokenText))
        {
            PrototypeStatusText = "Prototype token required";
            PrototypeResultLines = ["Enter a proto number, palette term, or explicit prototype handle."];
            return;
        }

        try
        {
            var snapshot = await _prototypeResolutionService.ResolveAsync(
                new PrototypeResolutionRequest(session, PrototypeTokenText, ResolveWorkspacePathOverride())
            );
            PrototypeStatusText = snapshot.Status;
            PrototypeResultLines = CreatePrototypeLines(snapshot);
        }
        catch (Exception ex)
        {
            PrototypeStatusText = "Prototype resolution failed";
            PrototypeResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task RunStructuredRead()
    {
        if (ActiveSession is not { } session)
        {
            ReadStatusText = "No active session";
            ReadResultLines = ["Attach to a live runtime before invoking getter-backed structured reads."];
            return;
        }

        if (string.IsNullOrWhiteSpace(ReadAdapterKeyText))
        {
            ReadStatusText = "Read adapter required";
            ReadResultLines = ["Enter one adapter key such as story-state, quest, stat, field, or script-local-flag."];
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _readService.Read(new ReadRequest(session, ReadAdapterKeyText, ParseReadArguments(ReadArgumentsText)))
            );
            ReadStatusText = snapshot.Status;
            ReadResultLines = CreateReadLines(snapshot);
        }
        catch (Exception ex)
        {
            ReadStatusText = "Structured read failed";
            ReadResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task ReadSheetDiagnostics()
    {
        if (ActiveSession is not { } session)
        {
            SheetStatusText = "No active session";
            SheetResultLines = ["Attach to a live runtime before reading typed sheet values."];
            return;
        }

        if (string.IsNullOrWhiteSpace(SheetHandleTokenText) || string.IsNullOrWhiteSpace(SheetLabelText))
        {
            SheetStatusText = "Sheet target required";
            SheetResultLines =
            [
                "Provide both a handle token such as player and a sheet label such as level or strength.",
            ];
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _sheetService.Read(new SheetRequest(session, SheetHandleTokenText, SheetLabelText))
            );
            SheetStatusText = snapshot.Status;
            SheetResultLines = CreateSheetLines(snapshot);
        }
        catch (Exception ex)
        {
            SheetStatusText = "Sheet read failed";
            SheetResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task ScanSheetDiagnostics()
    {
        if (ActiveSession is not { } session)
        {
            SheetStatusText = "No active session";
            SheetResultLines = ["Attach to a live runtime before capturing a sheet snapshot."];
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _sheetService.Scan(new SheetScanRequest(session, SheetHandleTokenText))
            );
            SheetStatusText = snapshot.Status;
            SheetResultLines = CreateSheetScanLines(snapshot);
            ApplySheetEditableFieldSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            SheetStatusText = "Sheet scan failed";
            SheetResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task DiffSheetDiagnostics()
    {
        if (ActiveSession is not { } session)
        {
            SheetStatusText = "No active session";
            SheetResultLines = ["Attach to a live runtime before diffing sheet snapshots."];
            return;
        }

        try
        {
            var delayMilliseconds = ParseNonNegativeInt(SheetDiffDelayText, "sheet diff delay");
            var snapshot = await Task.Run(() =>
                _sheetService.Diff(new SheetDiffRequest(session, SheetHandleTokenText, delayMilliseconds))
            );
            SheetStatusText = snapshot.Status;
            SheetResultLines = CreateSheetDiffLines(snapshot);
        }
        catch (Exception ex)
        {
            SheetStatusText = "Sheet diff failed";
            SheetResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task ReadScriptAttachmentDiagnostics()
    {
        if (ActiveSession is not { } session)
        {
            ScriptStatusText = "No active session";
            ScriptResultLines = ["Attach to a live runtime before reading script attachments."];
            return;
        }

        if (string.IsNullOrWhiteSpace(ScriptHandleTokenText) || string.IsNullOrWhiteSpace(ScriptAttachmentPointText))
        {
            ScriptStatusText = "Script attachment target required";
            ScriptResultLines =
            [
                "Provide both a handle token such as player and an attachment point such as dialog or heartbeat.",
            ];
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _scriptAttachmentService.Read(
                    new ScriptAttachmentRequest(session, ScriptHandleTokenText, ScriptAttachmentPointText)
                )
            );
            ScriptStatusText = snapshot.Status;
            ScriptResultLines = CreateScriptAttachmentLines(snapshot);
        }
        catch (Exception ex)
        {
            ScriptStatusText = "Script attachment read failed";
            ScriptResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task ReadLogbookDiagnostics()
    {
        if (ActiveSession is not { } session)
        {
            LogbookStatusText = "No active session";
            LogbookResultLines = ["Attach to a live runtime before reading logbook pages."];
            ApplyLogbookFallback("Attach to a live runtime before reading the journal.", LogbookResultLines);
            return;
        }

        if (string.IsNullOrWhiteSpace(LogbookHandleTokenText) || string.IsNullOrWhiteSpace(LogbookPageTokenText))
        {
            LogbookStatusText = "Logbook target required";
            LogbookResultLines =
            [
                "Provide both a handle token such as player and a page token such as quests or background.",
            ];
            ApplyLogbookFallback(
                "Choose both a target and a journal page before reading the logbook.",
                LogbookResultLines
            );
            return;
        }

        try
        {
            var snapshot = await _logbookService.ReadAsync(
                new LogbookRequest(
                    session,
                    LogbookHandleTokenText,
                    LogbookPageTokenText,
                    ResolveWorkspacePathOverride()
                )
            );
            ApplyLogbookReadSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            LogbookStatusText = "Logbook read failed";
            LogbookResultLines = [ex.Message];
            ApplyLogbookFallback(
                "The journal view could not be composed from the current request.",
                LogbookResultLines
            );
        }
    }

    [RelayCommand]
    private async Task InspectRuntimeStatus()
    {
        if (ActiveSession is not { } session)
        {
            RuntimeStatusText = "No active session";
            RuntimeStatusResultLines = ["Attach to a live runtime before inspecting runtime status."];
            return;
        }

        try
        {
            var result = await Task.Run(() => _runtimeStatusService.Inspect(session.ProcessId));
            RuntimeStatusText = "Runtime status inspection completed";
            RuntimeStatusResultLines = CreateRuntimeStatusLines(result);
            if (result.ActionPoints.HasValue)
                RuntimeActionPointsText = result.ActionPoints.Value.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            RuntimeStatusText = "Runtime status inspection failed";
            RuntimeStatusResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task QueryRuntimeModuleSymbols()
    {
        if (ActiveSession is not { } session)
        {
            RuntimeModuleStatusText = "No active session";
            RuntimeModuleResultLines = ["Attach to a live runtime before querying live module symbols."];
            return;
        }

        try
        {
            var limit = ParsePositiveInt(RuntimeModuleLimitText, "live module-symbol limit");
            var snapshot = await Task.Run(() =>
                _moduleSymbolQueryService.QueryLive(
                    session.ProcessId,
                    new ModuleSymbolQueryRequest(
                        string.IsNullOrWhiteSpace(RuntimeModuleFilterText) ? null : RuntimeModuleFilterText,
                        limit,
                        RuntimeModuleDuplicatesOnly
                    )
                )
            );
            RuntimeModuleStatusText = "Live module-symbol query completed";
            RuntimeModuleResultLines = CreateModuleSymbolLines(snapshot);
        }
        catch (Exception ex)
        {
            RuntimeModuleStatusText = "Live module-symbol query failed";
            RuntimeModuleResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task WriteRuntimeActionPoints()
    {
        if (ActiveSession is not { } session)
        {
            RuntimeStatusText = "No active session";
            RuntimeStatusResultLines = ["Attach to a live runtime before writing action points."];
            return;
        }

        if (!int.TryParse(RuntimeActionPointsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            RuntimeStatusText = "Invalid action points value";
            RuntimeStatusResultLines = ["Enter one integer action-point value before writing to the runtime."];
            return;
        }

        try
        {
            var result = await Task.Run(() => _runtimeStatusService.WriteActionPoints(session.ProcessId, value));
            RuntimeStatusText = "Runtime action points updated";
            RuntimeStatusResultLines = CreateRuntimeMutationLines(result.Mutation, result.Status);
            if (result.Status.ActionPoints.HasValue)
                RuntimeActionPointsText = result.Status.ActionPoints.Value.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            RuntimeStatusText = "Runtime action-point write failed";
            RuntimeStatusResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task WriteRuntimeCrashDump()
    {
        if (ActiveSession is not { } session)
        {
            RuntimeDumpStatusText = "No active session";
            RuntimeDumpResultLines = ["Attach to a live runtime before writing a crash dump."];
            return;
        }

        if (string.IsNullOrWhiteSpace(RuntimeDumpPathText))
        {
            RuntimeDumpStatusText = "Dump path required";
            RuntimeDumpResultLines = ["Enter one output path for the dump file before capturing a dump."];
            return;
        }

        try
        {
            var dumpKind = ParseCrashDumpKind(RuntimeDumpKindText);
            var result = await _crashDumpService.WriteDumpAsync(session.ProcessId, RuntimeDumpPathText, dumpKind);
            RuntimeDumpStatusText = "Runtime dump captured";
            RuntimeDumpResultLines = CreateCrashDumpWriteLines(result.Dump, result.Analysis);
        }
        catch (Exception ex)
        {
            RuntimeDumpStatusText = "Runtime dump capture failed";
            RuntimeDumpResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task InspectAutomaticCrashDumps()
    {
        try
        {
            var modulePath = ActiveSession?.Fingerprint.ModulePath;
            var snapshot = await _crashDumpService.InspectAutomaticDumpsAsync(
                NormalizeProcessExecutableName(RuntimeDumpProcessNameText),
                modulePath
            );
            RuntimeDumpStatusText = snapshot.Status;
            RuntimeDumpResultLines = CreateCrashDumpAutoInspectionLines(snapshot);
        }
        catch (Exception ex)
        {
            RuntimeDumpStatusText = "Automatic dump status inspection failed";
            RuntimeDumpResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task EnableAutomaticCrashDumps()
    {
        if (string.IsNullOrWhiteSpace(RuntimeDumpDirectoryText))
        {
            RuntimeDumpStatusText = "Dump directory required";
            RuntimeDumpResultLines = ["Enter one dump directory before enabling automatic dumps."];
            return;
        }

        try
        {
            var dumpKind = ParseCrashDumpKind(RuntimeDumpKindText);
            var dumpCount = ParseDumpCount(RuntimeDumpCountText);
            var snapshot = await Task.Run(() =>
                _crashDumpService.EnableAutomaticDumps(
                    RuntimeDumpDirectoryText,
                    dumpKind,
                    dumpCount,
                    NormalizeProcessExecutableName(RuntimeDumpProcessNameText)
                )
            );
            RuntimeDumpStatusText = "Automatic dumps enabled";
            RuntimeDumpResultLines = CreateCrashDumpConfigurationLines(snapshot);
        }
        catch (Exception ex)
        {
            RuntimeDumpStatusText = "Automatic dump enable failed";
            RuntimeDumpResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task DisableAutomaticCrashDumps()
    {
        try
        {
            var snapshot = await Task.Run(() =>
                _crashDumpService.DisableAutomaticDumps(NormalizeProcessExecutableName(RuntimeDumpProcessNameText))
            );
            RuntimeDumpStatusText = "Automatic dumps disabled";
            RuntimeDumpResultLines = CreateCrashDumpConfigurationLines(snapshot);
        }
        catch (Exception ex)
        {
            RuntimeDumpStatusText = "Automatic dump disable failed";
            RuntimeDumpResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task StartRuntimeIntercept()
    {
        if (ActiveSession is not { } session)
        {
            InterceptStatusText = "No active session";
            InterceptResultLines = ["Attach to a live runtime before starting interception."];
            return;
        }

        try
        {
            var request = await Task.Run(() => CreateInterceptStartRequest(session));
            var handle = await Task.Run(() => _interceptService.Start(request));
            ReplaceActiveIntercept(handle);
            InterceptStatusText = "Interception session started";
            InterceptResultLines = CreateInterceptLines(handle.Snapshot);
        }
        catch (Exception ex)
        {
            DisposeActiveIntercept();
            InterceptStatusText = "Interception start failed";
            InterceptResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task PollRuntimeIntercept()
    {
        if (_activeInterceptHandle is null)
        {
            InterceptStatusText = "No interception session";
            InterceptResultLines = ["Start interception before polling live events."];
            return;
        }

        try
        {
            var snapshot = await Task.Run(() => _interceptService.Poll(_activeInterceptHandle));
            ApplyInterceptSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            DisposeActiveIntercept();
            InterceptStatusText = "Interception poll failed";
            InterceptResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private void StopRuntimeIntercept()
    {
        DisposeActiveIntercept();
        InterceptStatusText = "Interception session stopped";
        InterceptResultLines = ["The active interception handle has been disposed."];
    }

    [RelayCommand]
    private async Task RunOfflineSaveDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(OfflineSaveDirectoryText))
        {
            OfflineSaveStatusText = "Save directory required";
            OfflineSaveResultLines = ["Enter one save directory that contains Slot####.gsi/.tfai/.tfaf files."];
            ClearOfflineSavePresentation();
            return;
        }

        if (string.IsNullOrWhiteSpace(OfflineSlotText))
        {
            OfflineSaveStatusText = "Slot required";
            OfflineSaveResultLines = ["Enter one slot number such as 1 or 0001."];
            ClearOfflineSavePresentation();
            return;
        }

        try
        {
            var diagnostics = await Task.Run(() =>
                CreateOfflineSaveDiagnostics(SaveSlotLoadService.Load(OfflineSaveDirectoryText, OfflineSlotText))
            );
            OfflineSaveStatusText = "Offline save diagnostics completed";
            OfflineSaveResultLines = CreateOfflineSaveLines(diagnostics);
            ApplyOfflineSavePresentation(diagnostics);
        }
        catch (Exception ex)
        {
            OfflineSaveStatusText = "Offline save diagnostics failed";
            OfflineSaveResultLines = [ex.Message];
            ClearOfflineSavePresentation();
        }
    }

    [RelayCommand]
    private async Task RunOfflineSaveFileDiagnostics()
    {
        if (
            string.IsNullOrWhiteSpace(OfflineSaveGsiPathText)
            || string.IsNullOrWhiteSpace(OfflineSaveTfaiPathText)
            || string.IsNullOrWhiteSpace(OfflineSaveTfafPathText)
        )
        {
            OfflineSaveStatusText = "Explicit save files required";
            OfflineSaveResultLines =
            [
                "Provide .gsi, .tfai, and .tfaf paths before running file-based save diagnostics.",
            ];
            ClearOfflineSavePresentation();
            return;
        }

        try
        {
            var diagnostics = await Task.Run(() =>
            {
                var save = SaveSlotLoadService.LoadFiles(
                    OfflineSaveGsiPathText,
                    OfflineSaveTfaiPathText,
                    OfflineSaveTfafPathText
                );
                var slotStem = Path.GetFileNameWithoutExtension(OfflineSaveGsiPathText);
                var loaded = new SaveSlotLoadSnapshot(
                    0,
                    string.IsNullOrWhiteSpace(slotStem) ? "ExplicitFiles" : slotStem,
                    save
                );
                return CreateOfflineSaveDiagnostics(loaded);
            });
            OfflineSaveStatusText = "Offline save diagnostics completed";
            OfflineSaveResultLines = CreateOfflineSaveLines(diagnostics);
            ApplyOfflineSavePresentation(diagnostics);
        }
        catch (Exception ex)
        {
            OfflineSaveStatusText = "Offline save diagnostics failed";
            OfflineSaveResultLines = [ex.Message];
            ClearOfflineSavePresentation();
        }
    }

    [RelayCommand]
    private async Task RunOfflineRangeDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(OfflineSaveDirectoryText))
        {
            OfflineRangeStatusText = "Save directory required";
            OfflineRangeResultLines = ["Enter one save directory that contains Slot####.gsi/.tfai/.tfaf files."];
            ClearOfflineRangePresentation();
            return;
        }

        if (string.IsNullOrWhiteSpace(OfflineRangeFirstSlotText) || string.IsNullOrWhiteSpace(OfflineRangeLastSlotText))
        {
            OfflineRangeStatusText = "Slot range required";
            OfflineRangeResultLines = ["Provide both first and last slot numbers such as 0001 and 0005."];
            ClearOfflineRangePresentation();
            return;
        }

        if (!Directory.Exists(OfflineSaveDirectoryText))
        {
            OfflineRangeStatusText = "Save directory not found";
            OfflineRangeResultLines = [$"Directory does not exist: {OfflineSaveDirectoryText}"];
            ClearOfflineRangePresentation();
            return;
        }

        try
        {
            var firstSlot = ParseSlotNumber(OfflineRangeFirstSlotText);
            var lastSlot = ParseSlotNumber(OfflineRangeLastSlotText);
            if (lastSlot < firstSlot)
            {
                OfflineRangeStatusText = "Invalid slot range";
                OfflineRangeResultLines = ["The last slot must be greater than or equal to the first slot."];
                ClearOfflineRangePresentation();
                return;
            }

            var diagnostics = await Task.Run(() =>
                CreateOfflineRangeDiagnostics(OfflineSaveDirectoryText, firstSlot, lastSlot)
            );
            OfflineRangeStatusText = "Offline range diagnostics completed";
            OfflineRangeResultLines = CreateOfflineRangeLines(diagnostics);
            ApplyOfflineRangePresentation(diagnostics);
        }
        catch (Exception ex)
        {
            OfflineRangeStatusText = "Offline range diagnostics failed";
            OfflineRangeResultLines = [ex.Message];
            ClearOfflineRangePresentation();
        }
    }

    [RelayCommand]
    private async Task QueryOfflineModuleSymbols()
    {
        if (string.IsNullOrWhiteSpace(OfflineModulePathText))
        {
            OfflineModuleStatusText = "Module path required";
            OfflineModuleResultLines = ["Enter one executable or DLL path before querying offline symbols."];
            return;
        }

        try
        {
            var limit = ParsePositiveInt(OfflineModuleLimitText, "offline module-symbol limit");
            var snapshot = await Task.Run(() =>
                _moduleSymbolQueryService.QueryFile(
                    OfflineModulePathText,
                    new ModuleSymbolQueryRequest(
                        string.IsNullOrWhiteSpace(OfflineModuleFilterText) ? null : OfflineModuleFilterText,
                        limit,
                        OfflineModuleDuplicatesOnly
                    )
                )
            );
            OfflineModuleStatusText = "Offline module-symbol query completed";
            OfflineModuleResultLines = CreateModuleSymbolLines(snapshot);
        }
        catch (Exception ex)
        {
            OfflineModuleStatusText = "Offline module-symbol query failed";
            OfflineModuleResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task RunOfflineCeSourceAudit()
    {
        if (OfflineSourceMissingOnly && OfflineSourceCoveredOnly)
        {
            OfflineSourceStatusText = "CE source filter conflict";
            OfflineSourceResultLines = ["Choose missing-only or covered-only, but not both at the same time."];
            return;
        }

        try
        {
            var limit = ParsePositiveInt(OfflineSourceLimitText, "CE source audit limit");
            var snapshot = await Task.Run(() =>
                CeSourceAuditService.Create(
                    new CeSourceAuditRequest(
                        string.IsNullOrWhiteSpace(OfflineSourceRootText) ? null : OfflineSourceRootText,
                        string.IsNullOrWhiteSpace(OfflineSourceFilterText) ? null : OfflineSourceFilterText,
                        string.IsNullOrWhiteSpace(OfflineSourceAreaText) ? null : OfflineSourceAreaText,
                        limit,
                        OfflineSourceMissingOnly,
                        OfflineSourceCoveredOnly
                    )
                )
            );
            OfflineSourceStatusText = "CE source audit completed";
            OfflineSourceResultLines = CreateCeSourceLines(snapshot);
        }
        catch (Exception ex)
        {
            OfflineSourceStatusText = "CE source audit failed";
            OfflineSourceResultLines = [ex.Message];
        }
    }

    [RelayCommand]
    private async Task RunOfflineDumpTemplate()
    {
        if (SelectedOfflineDumpTemplateOption is not { } template)
        {
            OfflineDumpStatusText = "Report template required";
            OfflineDumpResultLines = ["Choose one file-time report template before rendering output."];
            OfflineDumpOutputText = string.Empty;
            ClearOfflineDumpTemplatePresentation();
            return;
        }

        if (string.IsNullOrWhiteSpace(OfflineDumpPathText))
        {
            OfflineDumpStatusText = "Path required";
            OfflineDumpResultLines = ["Enter one file or directory path for the selected report template."];
            OfflineDumpOutputText = string.Empty;
            ClearOfflineDumpTemplatePresentation();
            return;
        }

        try
        {
            var result = await Task.Run(() => CreateOfflineDumpRenderResult(template, OfflineDumpPathText));
            OfflineDumpStatusText = "Report rendered";
            OfflineDumpResultLines = CreateOfflineDumpLines(template, OfflineDumpPathText, result.Output);
            OfflineDumpOutputText = result.Output;
            ApplyOfflineDumpTemplatePresentation(result);
        }
        catch (Exception ex)
        {
            OfflineDumpStatusText = "Report rendering failed";
            OfflineDumpResultLines = [ex.Message];
            OfflineDumpOutputText = string.Empty;
            ClearOfflineDumpTemplatePresentation();
        }
    }

    private static string CreateDiagnosticsFeatureSummary()
    {
        var features = DiagnosticsFeatureCatalog.Features;
        var interactiveCount = features.Count(IsInteractiveFeature);
        return interactiveCount == features.Count
            ? $"All {features.Count} workflows are directly usable in the debugger today."
            : $"{interactiveCount} of {features.Count} workflows are directly usable in the debugger today; the rest remain visible here as reference or in-progress surfaces.";
    }

    private static IReadOnlyList<DebuggerFeatureLaneGroup> CreateDiagnosticsFeatureGroups() =>
        [
            CreateFeatureLaneGroup(
                "Connect & Choose",
                "Start here when you need a live session, capability posture, or workspace guidance before choosing one runtime or file-time lane.",
                static feature =>
                    feature.Key
                        is "workspace"
                            or "dashboard"
                            or "function-browser"
                            or "object-explorer"
                            or "timeline-guidance"
                            or "environment"
                            or "session"
                            or "runtime-profile"
            ),
            CreateFeatureLaneGroup(
                "Runtime Read",
                "Read the current runtime state through sheet, journal, status, script attachments, and targeted getter-backed lookups.",
                static feature =>
                    feature.Key
                        is "runtime-status"
                            or "prototype-resolution"
                            or "sheet"
                            or "script-attachment"
                            or "logbook"
                            or "read"
            ),
            CreateFeatureLaneGroup(
                "Runtime Observe",
                "Watch live change over time before you mutate or deep-inspect anything.",
                static feature => feature.Key is "watch"
            ),
            CreateFeatureLaneGroup(
                "Runtime Act",
                "Use guided actions first, then deeper raw calls, hook audits, interception, or recovery tooling when the safer surface is not enough.",
                static feature =>
                    feature.Key is "guided-action" or "function-call" or "audit" or "intercept" or "crash-dump"
            ),
            CreateFeatureLaneGroup(
                "Runtime Inspect",
                "Decode one chosen runtime object into grouped sections before falling back to raw structure details.",
                static feature => feature.Key is "object-probe"
            ),
            CreateFeatureLaneGroup(
                "File-Time",
                "Work offline with save analysis, symbol and source catalogs, comparisons, and report templates without attaching to a live process.",
                static feature =>
                    feature.Key is "module-symbol-query"
                    || feature.Area.StartsWith("File-time", StringComparison.Ordinal)
            ),
        ];

    private static DebuggerFeatureLaneGroup CreateFeatureLaneGroup(
        string title,
        string summary,
        Func<DebuggerFeatureCard, bool> includeFeature
    )
    {
        var features = DiagnosticsFeatureCatalog.Features.Where(includeFeature).ToArray();
        return new(title, summary, CreateFeatureLaneProgressText(features), features);
    }

    private static string CreateFeatureLaneProgressText(IReadOnlyList<DebuggerFeatureCard> features)
    {
        var interactiveCount = features.Count(IsInteractiveFeature);
        return features.Count switch
        {
            1 when interactiveCount == 1 => "This workflow is directly usable in the shell today.",
            1 => "This workflow remains visible mainly as catalog or reference guidance.",
            _ when interactiveCount == features.Count =>
                $"All {features.Count} workflows are directly usable in the shell today.",
            _ =>
                $"{interactiveCount} of {features.Count} workflows are directly usable in the shell today; the rest remain catalog or reference guidance.",
        };
    }

    private static bool IsInteractiveFeature(DebuggerFeatureCard feature) =>
        feature.ShellStatus.Equals("Interactive in shell", StringComparison.Ordinal);

    private static IReadOnlyList<string> CreateAuditLines(AuditSnapshot snapshot)
    {
        List<string> lines =
        [
            snapshot.RuntimeProfile.DisplayName,
            snapshot.Dispatcher is { Success: true } dispatcher ? $"Dispatcher: {dispatcher.Mode} @ {dispatcher.Site}"
            : snapshot.Dispatcher is { Error: { } error } ? $"Dispatcher failed: {error}"
            : "Dispatcher audit was skipped.",
        ];

        if (snapshot.Functions is { } functions)
        {
            lines.Add(
                $"Functions: {functions.ResolvedFunctions}/{functions.TotalFunctions} resolved, {functions.FailedFunctions} failed."
            );
            lines.AddRange(
                functions
                    .Results.Take(8)
                    .Select(static result =>
                        $"{(result.Success ? "ok" : "fail")} {result.Key} :: {result.Site ?? result.Error ?? result.Resolution ?? "no detail"}"
                    )
            );
        }

        if (snapshot.Hooks is { } hooks)
        {
            lines.Add(
                $"Hooks: {hooks.BoundHookCount}/{hooks.AuditedHookCount} bound, watch events {hooks.WatchObservedEventCount}, intercept events {hooks.InterceptObservedEventCount}."
            );
            lines.Add(
                $"Hook passes: watch {(hooks.IncludeWatch ? "on" : "off")} {hooks.WatchSuccessCount}/{hooks.AuditedHookCount}, intercept {(hooks.IncludeIntercept ? "on" : "off")} {hooks.InterceptSuccessCount}/{hooks.AuditedHookCount}, bind failures {hooks.BindFailureCount}."
            );
            lines.Add(
                $"Selection: {string.Join(", ", hooks.Selectors)} over {hooks.DurationMilliseconds.ToString(CultureInfo.InvariantCulture)} ms with stack {hooks.StackCaptureDwordCount.ToString(CultureInfo.InvariantCulture)} dwords."
            );
            lines.AddRange(
                hooks
                    .Hooks.Take(6)
                    .Select(result =>
                        $"{result.Key}: bind {(result.Bind.Success ? "ok" : "fail")}, watch {FormatHookPassResult(result.Watch)}, intercept {FormatHookPassResult(result.Intercept)}"
                    )
            );
        }
        else
        {
            lines.Add("Hook audit not included in this quick shell run.");
        }

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private AuditRequest CreateAuditRequest(AttachedSessionSnapshot session) =>
        AuditIncludeHooks
            ? new AuditRequest(
                session,
                IncludeDispatcher: true,
                IncludeFunctions: true,
                IncludeHooks: true,
                HookSelectors: ParseReadArguments(AuditHookSelectorsText),
                HookDuration: TimeSpan.FromMilliseconds(
                    ParsePositiveInt(AuditHookDurationMillisecondsText, "hook audit duration")
                ),
                IncludeWatchPass: AuditIncludeWatchPass,
                IncludeInterceptPass: AuditIncludeInterceptPass,
                StackCaptureDwordCount: ParsePositiveInt(
                    AuditStackCaptureDwordCountText,
                    "hook audit stack capture dword count"
                ),
                StopOnFailure: AuditStopOnFailure
            )
            : new AuditRequest(
                session,
                IncludeDispatcher: true,
                IncludeFunctions: true,
                IncludeHooks: false,
                HookSelectors: [],
                HookDuration: TimeSpan.Zero,
                IncludeWatchPass: false,
                IncludeInterceptPass: false,
                StackCaptureDwordCount: 1,
                StopOnFailure: false
            );

    private static string FormatHookPassResult(HookPassAuditSnapshot? pass) =>
        pass is null ? "off"
        : pass.Success ? $"ok/{pass.EventCount}"
        : $"fail/{pass.Error ?? "error"}";

    private static IReadOnlyList<string> CreatePrototypeLines(PrototypeResolutionSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (snapshot.ProtoNumber.HasValue)
            lines.Add($"Proto: {snapshot.ProtoNumber.Value.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(snapshot.DisplayName))
            lines.Add($"Display: {snapshot.DisplayName}");

        if (!string.IsNullOrWhiteSpace(snapshot.AssetPath))
            lines.Add($"Asset: {snapshot.AssetPath}");

        if (!string.IsNullOrWhiteSpace(snapshot.HandleText))
            lines.Add($"Handle: {snapshot.HandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.ResolutionSource))
            lines.Add($"Resolution: {snapshot.ResolutionSource}");

        if (snapshot.ResolvedObject is { } resolved)
        {
            lines.Add($"Object: {resolved.DisplayValue}");
            lines.Add($"Type: {resolved.ObjectType ?? "unknown"}");
            if (resolved.ProtoNumber.HasValue)
                lines.Add($"Resolved proto: {resolved.ProtoNumber.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateReadLines(ReadSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        lines.AddRange(snapshot.Values.Select(static value => $"{value.Label}: {value.ValueText}"));
        if (snapshot.NativeRead is { } nativeRead)
            lines.Add($"Getter: {nativeRead.FunctionKey} via {nativeRead.DispatcherMode}");

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateSheetLines(SheetSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        lines.AddRange(snapshot.Values.Select(static value => $"{value.Label}: {value.ValueText}"));
        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateSheetScanLines(SheetScanSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        if (!snapshot.IsAvailable)
        {
            lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
            return lines;
        }

        lines.Add(
            $"Primary: {string.Join(", ", snapshot.Data.PrimaryStats.Take(4).Select(static entry => $"{entry.Name}={entry.Value}"))}"
        );
        lines.Add(
            $"Progression: {string.Join(", ", snapshot.Data.Progression.Select(static entry => $"{entry.Name}={entry.Value}"))}"
        );
        lines.Add(
            $"Derived: {string.Join(", ", snapshot.Data.DerivedStats.Take(5).Select(static entry => $"{entry.Name}={entry.Value}"))}"
        );
        lines.Add(
            $"Best basic skills: {string.Join(", ", snapshot.Data.BasicSkills.OrderByDescending(static entry => entry.Value).ThenByDescending(static entry => entry.Training).Take(4).Select(static entry => $"{entry.Name}={entry.Value} ({entry.TrainingName})"))}"
        );
        lines.Add(
            $"Tech skills: {string.Join(", ", snapshot.Data.TechSkills.Select(static entry => $"{entry.Name}={entry.Value} ({entry.TrainingName})"))}"
        );
        lines.Add(
            $"Spell/tech: mastery {snapshot.Data.SpellMastery.Value}, colleges {string.Join(", ", snapshot.Data.SpellColleges.Where(static entry => entry.Value > 0).Take(4).Select(static entry => $"{entry.Name}={entry.Value}"))}"
        );
        lines.Add(
            $"Disciplines/resistances: {string.Join(", ", snapshot.Data.TechDisciplines.Where(static entry => entry.Value > 0).Take(4).Select(static entry => $"{entry.Name}={entry.Value}"))} :: {string.Join(", ", snapshot.Data.Resistances.Select(static entry => $"{entry.Name}={entry.Value}"))}"
        );
        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateSheetDiffLines(SheetDiffSnapshot snapshot)
    {
        List<string> lines =
        [
            snapshot.Summary,
            $"Window: {snapshot.DelayMilliseconds.ToString(CultureInfo.InvariantCulture)} ms",
            $"Changed: {(snapshot.Changed ? "yes" : "no")}",
        ];
        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        if (snapshot.Changes.Count == 0)
        {
            lines.Add("No sheet changes detected across the requested sampling window.");
        }
        else
        {
            lines.Add(
                $"Categories: {string.Join(", ", snapshot.Changes.GroupBy(static change => change.Category).OrderByDescending(static group => group.Count()).ThenBy(static group => group.Key).Select(group => $"{group.Key} x{group.Count()}"))}"
            );
            lines.AddRange(
                snapshot
                    .Changes.Take(10)
                    .Select(change =>
                        $"{change.Category} {change.Name}: {change.Before.ToString(CultureInfo.InvariantCulture)} -> {change.After.ToString(CultureInfo.InvariantCulture)}{(string.IsNullOrWhiteSpace(change.Detail) ? string.Empty : $" ({change.Detail})")}"
                    )
            );
        }

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateScriptAttachmentLines(ScriptAttachmentSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (snapshot.Script is { } script)
        {
            lines.Add($"Script: {script.ScriptNumber.ToString(CultureInfo.InvariantCulture)}");
            lines.Add($"Flags: {script.FlagsText}");
            lines.Add($"Counters: {string.Join(", ", script.Counters)}");
            lines.Add(script.IsEmpty ? "Attachment: empty" : "Attachment: populated");
        }

        if (snapshot.NativeRead is { } nativeRead)
            lines.Add($"Getter: {nativeRead.FunctionKey} via {nativeRead.DispatcherMode}");

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateLogbookLines(LogbookSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        var payload = snapshot.Data;
        if (payload.Quests is { } quests)
            lines.Add($"Quests: {quests.Entries.Count.ToString(CultureInfo.InvariantCulture)}");

        if (payload.RumorsAndNotes is { } rumors)
            lines.Add($"Rumors: {rumors.Entries.Count.ToString(CultureInfo.InvariantCulture)}");

        if (payload.Reputations is { } reputations)
            lines.Add($"Reputations: {reputations.Entries.Count.ToString(CultureInfo.InvariantCulture)}");

        if (payload.BlessingsAndCurses is { } blessings)
            lines.Add($"Blessings/Curses: {blessings.Entries.Count.ToString(CultureInfo.InvariantCulture)}");

        if (payload.KillsAndInjuries is { } injuries)
            lines.Add(
                $"Kills/Injuries: {injuries.Summary.Count.ToString(CultureInfo.InvariantCulture)} summary, {injuries.Injuries.Count.ToString(CultureInfo.InvariantCulture)} injuries"
            );

        if (payload.Background is { } background)
            lines.Add($"Background: {background.Name ?? background.CatalogName ?? "(unnamed)"}");

        if (payload.KeyringContents is { } keyring)
            lines.Add($"Keys: {keyring.Entries.Count.ToString(CultureInfo.InvariantCulture)}");

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private void ApplyLogbookSnapshot(LogbookSnapshot snapshot)
    {
        _loadedLogbookHandleTokenText = LogbookHandleTokenText.Trim();
        _loadedLogbookPageTokenText = snapshot.RequestedPageToken.Trim();
        _loadedLogbookWorkspacePath = NormalizeWorkspacePathKey(
            ActiveSession is { } activeSession
                ? ResolveEffectiveWorkspacePath(activeSession)
                : ResolveWorkspacePathOverride()
        );
        LogbookStatusText = snapshot.Status;
        LogbookDisplaySummaryText = snapshot.Summary;
        LogbookResultLines = CreateLogbookLines(snapshot);
        LogbookHighlights = CreateLogbookHighlights(snapshot);
        LogbookSections = CreateLogbookSections(snapshot);
        ApplyLogbookEditableEntries(snapshot);
        LogbookNotes = [.. snapshot.Notes.Take(4)];
        HasLogbookSections = LogbookSections.Count > 0;
        ShowLogbookFallbackLines = !HasLogbookSections && LogbookResultLines.Count > 0;
        RefreshSelectedLogbookPageOption();
        QueueRefreshLogbookLiveInspection();
    }

    private void ApplyLogbookReadSnapshot(LogbookSnapshot snapshot)
    {
        if (snapshot.IsAvailable)
        {
            ApplyLogbookSnapshot(snapshot);
            return;
        }

        var lines = CreateLogbookLines(snapshot);
        LogbookStatusText = snapshot.Status;
        if (IsExitedRuntimeMessage(snapshot.Summary))
        {
            ApplyDormantSession("Attached process exited", snapshot.Summary);
            LogbookStatusText = snapshot.Status;
            ApplyLogbookFallback(
                "The attached process is no longer running. Reattach before reading the journal again.",
                lines
            );
            return;
        }

        ApplyLogbookFallback(snapshot.Summary, lines);
    }

    private void ApplyLogbookFallback(string summary, IReadOnlyList<string> lines)
    {
        ResetLoadedLogbookSnapshotState();
        LogbookDisplaySummaryText = summary;
        LogbookResultLines = lines;
        LogbookHighlights = [];
        LogbookSections = [];
        ClearLogbookEditableEntries(
            "Load one journal page to turn the current live entries into editor-prefill shortcuts for player or a selected companion."
        );
        LogbookNotes = [];
        HasLogbookSections = false;
        ShowLogbookFallbackLines = lines.Count > 0;
        RefreshSelectedLogbookPageOption();
        QueueRefreshLogbookLiveInspection();
    }

    private static bool IsExitedRuntimeMessage(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && (
            message.Contains("is not running", StringComparison.OrdinalIgnoreCase)
            || message.Contains("has exited", StringComparison.OrdinalIgnoreCase)
        );

    private void RefreshSelectedLogbookPageOption()
    {
        var trimmed = LogbookPageTokenText.Trim();
        var matchingOption = LogbookPageOptions.FirstOrDefault(option =>
            option.Token.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
        );

        if (matchingOption is null)
        {
            SelectedLogbookPageOption = null;
            LogbookPageDescriptionText = string.IsNullOrWhiteSpace(trimmed)
                ? "Pick one journal page to read it as a grouped logbook surface."
                : $"Unknown journal page '{trimmed}'.";
            return;
        }

        LogbookPageDescriptionText = matchingOption.Description;
        if (
            SelectedLogbookPageOption is not null
            && SelectedLogbookPageOption.Token.Equals(matchingOption.Token, StringComparison.OrdinalIgnoreCase)
        )
            return;

        SelectedLogbookPageOption = matchingOption;
    }

    private static IReadOnlyList<DebuggerLogbookSummaryCard> CreateLogbookHighlights(LogbookSnapshot snapshot)
    {
        List<DebuggerLogbookSummaryCard> cards =
        [
            new(
                "Page",
                ResolveLogbookPageLabel(snapshot.RequestedPageToken),
                string.IsNullOrWhiteSpace(snapshot.TargetText) ? snapshot.Status : snapshot.TargetText
            ),
            new("Source", snapshot.IsAvailable ? "Live runtime" : "Unavailable", snapshot.Status),
        ];

        var payload = snapshot.Data;
        if (payload.Quests is { } quests)
        {
            cards.Add(
                new(
                    "Quests",
                    quests.Entries.Count.ToString(CultureInfo.InvariantCulture),
                    quests.UsesDumbText ? "Dumb-text phrasing" : "Normal phrasing"
                )
            );
        }

        if (payload.RumorsAndNotes is { } rumors)
        {
            cards.Add(
                new(
                    "Rumors",
                    rumors.Entries.Count.ToString(CultureInfo.InvariantCulture),
                    rumors.UsesDumbText ? "Dumb-text phrasing" : "Normal phrasing"
                )
            );
        }

        if (payload.Reputations is { } reputations)
            cards.Add(
                new("Reputations", reputations.Entries.Count.ToString(CultureInfo.InvariantCulture), "Named entries")
            );

        if (payload.BlessingsAndCurses is { } blessings)
        {
            cards.Add(
                new(
                    "Blessings",
                    blessings
                        .Entries.Count(entry => entry.Kind.Equals("blessing", StringComparison.OrdinalIgnoreCase))
                        .ToString(CultureInfo.InvariantCulture),
                    "Positive special effects"
                )
            );
            cards.Add(
                new(
                    "Curses",
                    blessings
                        .Entries.Count(entry => entry.Kind.Equals("curse", StringComparison.OrdinalIgnoreCase))
                        .ToString(CultureInfo.InvariantCulture),
                    "Negative special effects"
                )
            );
        }

        if (payload.KillsAndInjuries is { } injuries)
        {
            cards.Add(
                new(
                    "Kills",
                    injuries.Summary.Sum(static entry => entry.Value).ToString(CultureInfo.InvariantCulture),
                    $"{injuries.Summary.Count.ToString(CultureInfo.InvariantCulture)} summary rows"
                )
            );
            cards.Add(
                new(
                    "Injuries",
                    injuries.Injuries.Count(static entry => entry.Active).ToString(CultureInfo.InvariantCulture),
                    $"{injuries.Injuries.Count.ToString(CultureInfo.InvariantCulture)} injury rows"
                )
            );
        }

        if (payload.Background is { } background)
        {
            cards.Add(
                new(
                    "Background",
                    background.Name ?? background.CatalogName ?? "(unnamed)",
                    $"Id {background.BackgroundId.ToString(CultureInfo.InvariantCulture)}"
                )
            );
        }

        if (payload.KeyringContents is { } keyring)
            cards.Add(
                new("Keys", keyring.Entries.Count.ToString(CultureInfo.InvariantCulture), "Known key-ring entries")
            );

        return cards;
    }

    private static IReadOnlyList<DebuggerLogbookSection> CreateLogbookSections(LogbookSnapshot snapshot)
    {
        List<DebuggerLogbookSection> sections = [];
        var payload = snapshot.Data;

        if (payload.Quests is { } quests)
            sections.Add(CreateQuestSection(quests));

        if (payload.RumorsAndNotes is { } rumors)
            sections.Add(CreateRumorSection(rumors));

        if (payload.Reputations is { } reputations)
        {
            sections.Add(
                new(
                    "Reputations",
                    $"{reputations.Entries.Count.ToString(CultureInfo.InvariantCulture)} reputation entries",
                    [.. reputations.Entries.Select(FormatReputationEntry)]
                )
            );
        }

        if (payload.BlessingsAndCurses is { } blessings)
            sections.AddRange(CreateBlessingCurseSections(blessings));

        if (payload.KillsAndInjuries is { } injuries)
            sections.AddRange(CreateKillAndInjurySections(injuries));

        if (payload.Background is { } background)
        {
            sections.Add(
                new(
                    "Background",
                    $"Background {background.BackgroundId.ToString(CultureInfo.InvariantCulture)} · text {background.BackgroundTextId.ToString(CultureInfo.InvariantCulture)}",
                    CreateBackgroundEntries(background)
                )
            );
        }

        if (payload.KeyringContents is { } keyring)
        {
            sections.Add(
                new(
                    "Keyring",
                    $"{keyring.Entries.Count.ToString(CultureInfo.InvariantCulture)} known keys",
                    [.. keyring.Entries.Select(FormatKeyringEntry)]
                )
            );
        }

        return sections;
    }

    private static DebuggerLogbookSection CreateQuestSection(QuestLogbookPageSnapshot quests) =>
        new(
            "Quest journal",
            $"{quests.Entries.Count.ToString(CultureInfo.InvariantCulture)} quest entries · {(quests.UsesDumbText ? "dumb-text phrasing" : "normal phrasing")}",
            [.. quests.Entries.Select(FormatQuestEntry)]
        );

    private static DebuggerLogbookSection CreateRumorSection(RumorLogbookPageSnapshot rumors) =>
        new(
            "Rumors and notes",
            $"{rumors.Entries.Count.ToString(CultureInfo.InvariantCulture)} rumor entries · {(rumors.UsesDumbText ? "dumb-text phrasing" : "normal phrasing")}",
            [.. rumors.Entries.Select(FormatRumorEntry)]
        );

    private static IReadOnlyList<DebuggerLogbookSection> CreateBlessingCurseSections(
        BlessingCurseLogbookPageSnapshot blessings
    )
    {
        List<DebuggerLogbookSection> sections = [];
        var blessingEntries = blessings
            .Entries.Where(entry => entry.Kind.Equals("blessing", StringComparison.OrdinalIgnoreCase))
            .Select(FormatBlessingCurseEntry)
            .ToArray();
        if (blessingEntries.Length > 0)
        {
            sections.Add(
                new(
                    "Blessings",
                    $"{blessingEntries.Length.ToString(CultureInfo.InvariantCulture)} blessing entries",
                    blessingEntries
                )
            );
        }

        var curseEntries = blessings
            .Entries.Where(entry => entry.Kind.Equals("curse", StringComparison.OrdinalIgnoreCase))
            .Select(FormatBlessingCurseEntry)
            .ToArray();
        if (curseEntries.Length > 0)
        {
            sections.Add(
                new(
                    "Curses",
                    $"{curseEntries.Length.ToString(CultureInfo.InvariantCulture)} curse entries",
                    curseEntries
                )
            );
        }

        return sections;
    }

    private static IReadOnlyList<DebuggerLogbookSection> CreateKillAndInjurySections(
        KillsAndInjuriesLogbookPageSnapshot injuries
    )
    {
        List<DebuggerLogbookSection> sections = [];
        if (injuries.Summary.Count > 0)
        {
            sections.Add(
                new(
                    "Kill ledger",
                    $"{injuries.Summary.Sum(static entry => entry.Value).ToString(CultureInfo.InvariantCulture)} total tracked kills",
                    [.. injuries.Summary.Select(FormatKillEntry)]
                )
            );
        }

        if (injuries.Injuries.Count > 0)
        {
            sections.Add(
                new(
                    "Injury ledger",
                    $"{injuries.Injuries.Count(static entry => entry.Active).ToString(CultureInfo.InvariantCulture)} active injuries",
                    [.. injuries.Injuries.Select(FormatInjuryEntry)]
                )
            );
        }

        return sections;
    }

    private static IReadOnlyList<string> CreateBackgroundEntries(BackgroundLogbookPageSnapshot background)
    {
        List<string> entries = [$"Name :: {background.Name ?? background.CatalogName ?? "(unnamed)"}"];
        var body = FirstNonEmpty(background.Body, background.CatalogBody);
        if (!string.IsNullOrWhiteSpace(body))
            entries.Add(body);

        if (
            !string.IsNullOrWhiteSpace(background.CatalogName)
            && !string.Equals(background.CatalogName, background.Name, StringComparison.Ordinal)
        )
        {
            entries.Add($"Catalog name :: {background.CatalogName}");
        }

        return entries;
    }

    private static string FormatQuestEntry(QuestLogbookEntrySnapshot entry)
    {
        var title = string.IsNullOrWhiteSpace(entry.Label)
            ? $"Quest {entry.QuestId}"
            : $"{entry.Label} (#{entry.QuestId})";
        var description = FirstNonEmpty(entry.Description, entry.NormalDescription, entry.DumbDescription);
        return string.IsNullOrWhiteSpace(description)
            ? $"{title} :: {entry.StateName} :: {FormatLogbookDate(entry.DateTime)}"
            : $"{title} :: {entry.StateName} :: {FormatLogbookDate(entry.DateTime)} :: {description}";
    }

    private static string FormatRumorEntry(RumorLogbookEntrySnapshot entry)
    {
        var text = FirstNonEmpty(entry.Text, entry.NormalText, entry.DumbText) ?? "(no rumor text)";
        return $"{text} :: {(entry.Quelled ? "Quelled" : "Active")} :: {FormatLogbookDate(entry.DateTime)}";
    }

    private static string FormatReputationEntry(ReputationLogbookEntrySnapshot entry) =>
        $"{entry.Name} (#{entry.ReputationId.ToString(CultureInfo.InvariantCulture)}) :: {FormatLogbookDate(entry.DateTime)}";

    private static string FormatBlessingCurseEntry(BlessingCurseLogbookEntrySnapshot entry) =>
        $"{entry.Name} (#{entry.Id.ToString(CultureInfo.InvariantCulture)}) :: {FormatLogbookDate(entry.DateTime)}";

    private static string FormatKillEntry(KillLogbookSummaryEntrySnapshot entry)
    {
        var label = string.IsNullOrWhiteSpace(entry.Name) ? entry.Label : $"{entry.Label} ({entry.Name})";
        return $"{label} :: {entry.Value.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string FormatInjuryEntry(InjuryLogbookEntrySnapshot entry) =>
        $"{entry.SourceName} :: {entry.InjuryTypeName} :: {entry.StateText}";

    private static string FormatKeyringEntry(KeyringLogbookEntrySnapshot entry) =>
        $"{entry.Name} :: key {entry.KeyId.ToString(CultureInfo.InvariantCulture)} :: slot {entry.Index.ToString(CultureInfo.InvariantCulture)}";

    private static string FormatLogbookDate(GameDateTimeSnapshot dateTime) =>
        RuntimeWatchValueCatalog.FormatGameDateTime(((ulong)dateTime.Milliseconds << 32) | dateTime.Days);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string ResolveLogbookPageLabel(string token) =>
        token.Trim().ToLowerInvariant() switch
        {
            "all" => "All pages",
            "rumors" or "rumorsandnotes" or "notes" => "Rumors and notes",
            "quests" => "Quest journal",
            "reputations" or "reputation" => "Reputations",
            "blessings" or "blessingsandcurses" or "curses" => "Blessings and curses",
            "kills" or "killsandinjuries" or "injuries" => "Kills and injuries",
            "background" => "Background",
            "keys" or "keyring" or "keyringcontents" => "Keyring",
            _ => token,
        };

    private static IReadOnlyList<string> CreateRuntimeStatusLines(RuntimeStatusSnapshot status)
    {
        var runtimeProfile = status.RuntimeProfile;
        List<string> lines =
        [
            status.DisplayName,
            $"Profile: {runtimeProfile.DisplayName} · {runtimeProfile.RuntimeKind} · {runtimeProfile.SupportLevel}",
            $"Module: {status.ModulePath} @ {status.ModuleBase}",
            $"Fingerprint: {status.Fingerprint.ModuleFileName}, size {status.Fingerprint.ModuleSize.ToString(CultureInfo.InvariantCulture)} bytes, written {status.Fingerprint.ModuleLastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} UTC",
            $"Catalog RVAs: {(runtimeProfile.SupportsCatalogRvas ? "supported" : "unavailable")}",
            $"Action points: {status.ActionPoints?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}",
            $"Current character sheet: {status.CurrentCharacterSheetId?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}",
        ];

        if (!string.IsNullOrWhiteSpace(runtimeProfile.Id))
            lines.Add($"Runtime id: {runtimeProfile.Id}");

        if (!string.IsNullOrWhiteSpace(runtimeProfile.ModuleSha256))
            lines.Add($"SHA256: {runtimeProfile.ModuleSha256}");

        if (!string.IsNullOrWhiteSpace(runtimeProfile.HashError))
            lines.Add($"Hash error: {runtimeProfile.HashError}");

        lines.AddRange(status.Notes.Take(5).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateRuntimeMutationLines(
        ActionPointsMutationSnapshot mutation,
        RuntimeStatusSnapshot status
    )
    {
        List<string> lines =
        [
            $"Action points updated for {mutation.ProcessName}.exe PID {mutation.ProcessId}.",
            $"Address: {mutation.Address}",
            $"Action points: {mutation.Before.ToString(CultureInfo.InvariantCulture)} -> {mutation.After.ToString(CultureInfo.InvariantCulture)}",
            $"Current character sheet: {status.CurrentCharacterSheetId?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"}",
            $"Profile: {status.RuntimeProfile.DisplayName} · {status.RuntimeProfile.SupportLevel}",
        ];
        lines.AddRange(status.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateCrashDumpWriteLines(
        CrashDumpWriteSnapshot snapshot,
        CrashDumpAnalysisSnapshot analysis
    )
    {
        List<string> lines =
        [
            $"{snapshot.ProcessName}.exe PID {snapshot.ProcessId} dump captured.",
            $"Kind: {snapshot.DumpKind}",
            $"Output: {snapshot.OutputPath}",
            $"Module: {snapshot.ModulePath} @ {snapshot.ModuleBase}",
            $"Analysis: {analysis.Status}",
        ];

        if (!string.IsNullOrWhiteSpace(analysis.OutputPath))
            lines.Add($"Stack trace: {analysis.OutputPath}");

        if (!string.IsNullOrWhiteSpace(analysis.AnalyzerPath))
            lines.Add($"Analyzer: {analysis.AnalyzerPath}");

        if (!string.IsNullOrWhiteSpace(analysis.ProcessName))
            lines.Add($"Process: {analysis.ProcessName}");

        if (!string.IsNullOrWhiteSpace(analysis.ExceptionCode))
            lines.Add($"Exception: {analysis.ExceptionCode}");

        if (!string.IsNullOrWhiteSpace(analysis.FaultingInstruction))
            lines.Add($"Faulting IP: {analysis.FaultingInstruction}");

        if (analysis.StackPreview.Count > 0)
            lines.AddRange(analysis.StackPreview.Take(3).Select(static frame => $"Stack: {frame}"));
        else
            lines.AddRange(analysis.Highlights.Take(4));

        return lines;
    }

    private static IReadOnlyList<string> CreateCrashDumpConfigurationLines(CrashDumpAutoConfigurationSnapshot snapshot)
    {
        List<string> lines =
        [
            $"Process: {snapshot.ProcessExecutableName}",
            $"Enabled: {(snapshot.IsEnabled ? "yes" : "no")}",
            $"Registry: {snapshot.Scope}\\{snapshot.RegistryPath}",
        ];

        if (!string.IsNullOrWhiteSpace(snapshot.DumpFolder))
            lines.Add($"Dump folder: {snapshot.DumpFolder}");

        if (snapshot.DumpKind.HasValue)
            lines.Add($"Dump kind: {snapshot.DumpKind.Value}");

        if (snapshot.DumpCount.HasValue)
            lines.Add($"Dump count: {snapshot.DumpCount.Value.ToString(CultureInfo.InvariantCulture)}");

        return lines;
    }

    private static IReadOnlyList<string> CreateCrashDumpAutoInspectionLines(CrashDumpAutoInspectionSnapshot snapshot)
    {
        List<string> lines = [.. CreateCrashDumpConfigurationLines(snapshot.Configuration)];

        if (!string.IsNullOrWhiteSpace(snapshot.LatestDumpPath))
        {
            lines.Add($"Latest dump: {snapshot.LatestDumpPath}");

            if (snapshot.LatestDumpWrittenAtUtc.HasValue)
            {
                lines.Add(
                    $"Modified (UTC): {snapshot.LatestDumpWrittenAtUtc.Value.ToString("u", CultureInfo.InvariantCulture)}"
                );
            }

            if (snapshot.LatestDumpSizeBytes.HasValue)
                lines.Add($"Size: {snapshot.LatestDumpSizeBytes.Value.ToString(CultureInfo.InvariantCulture)} bytes");
        }

        if (snapshot.Analysis is { } analysis)
        {
            lines.Add($"Analysis: {analysis.Status}");

            if (!string.IsNullOrWhiteSpace(analysis.ExceptionCode))
                lines.Add($"Exception: {analysis.ExceptionCode}");

            if (!string.IsNullOrWhiteSpace(analysis.FaultingInstruction))
                lines.Add($"Faulting IP: {analysis.FaultingInstruction}");

            if (analysis.StackPreview.Count > 0)
                lines.AddRange(analysis.StackPreview.Take(3).Select(static frame => $"Stack: {frame}"));
            else
                lines.AddRange(analysis.Highlights.Take(4));
        }

        lines.AddRange(snapshot.Notes.Take(4));
        return lines;
    }

    private static IReadOnlyList<string> CreateInterceptLines(InterceptSnapshot snapshot)
    {
        List<string> lines =
        [
            snapshot.Status,
            snapshot.Summary,
            $"Target: {snapshot.TargetKey} @ {snapshot.TargetSite}",
            $"Resolution: {snapshot.TargetResolution}",
            $"Execution: {snapshot.ExecutionModeText}, stack {snapshot.StackCaptureDwordCount.ToString(CultureInfo.InvariantCulture)} dwords",
        ];

        if (snapshot.Events.Count == 0)
        {
            lines.Add("No intercept events captured yet. Poll after the target function executes.");
            return lines;
        }

        lines.AddRange(
            snapshot
                .Events.Take(3)
                .Select(eventSnapshot =>
                {
                    var firstStack = eventSnapshot.StackDwords.FirstOrDefault() ?? "(empty)";
                    return $"Event #{eventSnapshot.Sequence.ToString(CultureInfo.InvariantCulture)} {eventSnapshot.CallerSite} :: EAX {eventSnapshot.Registers.Eax}, stack0 {firstStack}";
                })
        );
        lines.AddRange(
            snapshot
                .Events.Take(2)
                .Where(static eventSnapshot => eventSnapshot.PotentialHandles.Count > 0)
                .Select(eventSnapshot =>
                    $"Handles #{eventSnapshot.Sequence.ToString(CultureInfo.InvariantCulture)}: {string.Join(", ", eventSnapshot.PotentialHandles.Select(static handle => $"stack{handle.StackIndex}={handle.HandleText}"))}"
                )
        );
        lines.AddRange(
            snapshot
                .Events.Take(2)
                .SelectMany(static eventSnapshot =>
                    eventSnapshot.Dereferences.Select(dereference =>
                        dereference.Error is { Length: > 0 }
                            ? $"Dereference {dereference.Source} @ {dereference.AddressText}: {dereference.Error}"
                            : $"Dereference {dereference.Source} @ {dereference.AddressText}: {dereference.Hex} :: {dereference.Ascii}"
                    )
                )
                .Take(4)
        );
        return lines;
    }

    private static IReadOnlyList<string> CreateOfflineSaveLines(OfflineSaveDiagnosticsBundle diagnostics)
    {
        var loaded = diagnostics.Loaded;
        var audit = diagnostics.Audit;
        var characterCatalog = diagnostics.CharacterCatalog;
        var characterSarDump = diagnostics.CharacterSarDump;
        var player = diagnostics.Player;
        var questBook = diagnostics.QuestBook;
        var typedOverview = diagnostics.TypedOverview;
        var structure = diagnostics.Structure;
        var globals = diagnostics.Globals;
        var embeddedFiles = diagnostics.EmbeddedFiles;
        var mobItemAnalysis = diagnostics.MobItemAnalysis;
        var gold = diagnostics.Gold;

        List<string> lines =
        [
            $"Loaded {loaded.SlotStem} for {audit.LeaderName} level {audit.LeaderLevel} on map {audit.MapId}.",
            $"Assets: {audit.Assets.TotalFileCount} files, {audit.Assets.MobileMdyCount} mobile MDY, {audit.Assets.ScriptCount} scripts, {audit.Assets.DialogCount} dialogs.",
            $"Validation: {audit.Validation.IssueCount} issues ({audit.Validation.ErrorCount} errors, {audit.Validation.WarningCount} warnings).",
            $"Objects: {audit.Objects.ObjectCount} objects, {audit.Objects.DistinctFieldCount} distinct fields, {audit.Objects.ParseNoteCount} parse notes.",
            $"Character catalog: {characterCatalog.Records.Count} records, {characterCatalog.Records.Count(static record => !string.IsNullOrWhiteSpace(record.Name))} named entries.",
            $"Character SAR dump: {characterSarDump.Records.Count} character records, {characterSarDump.Records.Sum(static record => record.Sars.Count)} SAR entries, {characterSarDump.Records.Sum(static record => record.Sars.Count(static sar => sar.IsFiller))} filler entries.",
            $"Typed context: player={(typedOverview.HasPlayer ? "yes" : "no")}, quests {typedOverview.QuestCount}, rumors {typedOverview.RumorsCount}, blessings {typedOverview.Blessings}, curses {typedOverview.Curses}, schematics {typedOverview.Schematics}, fog files {typedOverview.TownMapFogFileCount}.",
            $"Structure: module {structure.ModuleName}, map {structure.MapId}, tile {structure.LeaderTileX},{structure.LeaderTileY}, game day {structure.GameTime.DayNumber} {structure.GameTime.Hours:D2}:{structure.GameTime.Minutes:D2}:{structure.GameTime.Seconds:D2}.",
        ];

        if (player is { } summary)
        {
            lines.Add(
                $"Player: {summary.Progression.Name ?? audit.LeaderName} lvl {summary.Progression.Level}, gold {summary.Progression.Gold}, XP {summary.Progression.ExperiencePoints}, kills {summary.Progression.TotalKills}."
            );
            lines.Add(
                $"Quest log: {summary.QuestLog.Count} entries, rumors {summary.Rumors.Count}, reputations {summary.Reputation.Count}."
            );
            lines.Add(
                $"Primary: {string.Join(", ", summary.PrimaryAttributes.Take(4).Select(static stat => $"{stat.Label}={stat.Value}"))}"
            );
        }

        if (characterCatalog.Records.Count > 0)
        {
            lines.AddRange(
                characterCatalog
                    .Records.Take(3)
                    .Select(record =>
                        $"Character {record.Name ?? "(unnamed)"}: lvl {record.Level}, {record.RaceName}, gold {record.Gold}, MP {record.MagickPoints}, TP {record.TechPoints}."
                    )
            );
        }

        lines.Add(
            $"Quest book: {questBook.Quests.Count} quests, {questBook.Reputation.Count} reputations, {questBook.Blessings.Count} blessings, {questBook.Curses.Count} curses."
        );
        lines.AddRange(
            questBook
                .Quests.Take(5)
                .Select(static quest =>
                    $"Quest {quest.ProtoId}: {quest.Label ?? "(unlabeled)"} :: {quest.StateDescription}"
                )
        );

        if (structure.Extensions.Count > 0)
        {
            lines.Add(
                $"Top extensions: {string.Join(", ", structure.Extensions.Take(5).Select(static extension => $"{extension.DisplayExtension} x{extension.Count}"))}"
            );
        }

        if (globals.Files.Count > 0)
        {
            lines.AddRange(
                globals
                    .Files.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair =>
                    {
                        var detail = pair.Value;
                        var segments = new List<string>
                        {
                            $"{detail.TotalInts} ints",
                            $"{detail.NonZeroCount} non-zero",
                        };

                        if (detail.SaveIdPairs is { } saveIdPairs)
                            segments.Add($"{saveIdPairs.PairCount} id-pairs");

                        if (detail.QuadSummary is { } quadSummary)
                            segments.Add($"{quadSummary.SectionCount} quad runs");

                        return $"Global {pair.Key}: {string.Join(", ", segments)}.";
                    })
            );
        }

        if (embeddedFiles.Count > 0)
        {
            lines.Add(
                $"Embedded analysis: {embeddedFiles.Count} analyzable files :: {string.Join(", ", embeddedFiles.GroupBy(static detail => detail.Kind).OrderByDescending(static group => group.Count()).ThenBy(static group => group.Key).Select(group => $"{group.Key}={group.Count()}"))}"
            );
        }
        else
        {
            lines.Add("Embedded analysis: no recognized .mdy/.md/.des/.tmf/.dif/TimeEvent.dat files were detected.");
        }

        if (mobItemAnalysis is { } representativeItem)
        {
            var discipline =
                representativeItem.Analysis.DisciplineLabel
                ?? representativeItem.Analysis.Discipline?.ToString(CultureInfo.InvariantCulture)
                ?? "n/a";
            lines.Add(
                $"Representative item mob: {Path.GetFileName(representativeItem.Path)} :: {representativeItem.Analysis.ObjectType}, worth {representativeItem.Analysis.Worth?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}, weight {representativeItem.Analysis.Weight?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}, discipline {discipline}, effects {representativeItem.Analysis.SpellEffects.Count}."
            );
        }
        else
        {
            lines.Add("Representative item mob: no analyzable item mob was found in the loaded save.");
        }

        var totalGoldItems = gold.Files.Sum(static file => file.Items.Count);
        var playerLinkedGoldItems = gold.Files.Sum(file =>
            file.Items.Count(static item => item.FoundInPlayerCharacter)
        );
        lines.Add(
            totalGoldItems > 0
                ? $"Gold inspection: {totalGoldItems} gold items across {gold.Files.Count} files, {playerLinkedGoldItems} linked to a player character payload."
                : "Gold inspection: no gold item records detected."
        );

        lines.AddRange(audit.ValidationIssues.Take(4).Select(static issue => $"{issue.Severity}: {issue.Message}"));
        return lines;
    }

    private static IReadOnlyList<string> CreateModuleSymbolLines(ModuleSymbolQuerySnapshot snapshot)
    {
        List<string> lines =
        [
            $"{snapshot.ModuleFileName}: {snapshot.FunctionCount} symbols, {snapshot.UniqueNameCount} unique names, {snapshot.DuplicateNameCount} duplicate-name groups.",
        ];
        if (!string.IsNullOrWhiteSpace(snapshot.ModuleBase))
            lines.Add($"Module base: {snapshot.ModuleBase}");

        if (snapshot.Fingerprint is { } fingerprint)
            lines.Add($"Runtime: {fingerprint.ProcessName}.exe PID {fingerprint.ProcessId} @ {fingerprint.ModuleBase}");

        if (!string.IsNullOrWhiteSpace(snapshot.Filter))
            lines.Add($"Filter: {snapshot.Filter}");

        if (snapshot.DuplicatesOnly)
            lines.Add("Duplicates only: yes");

        lines.AddRange(snapshot.Symbols.Take(12).Select(static symbol => symbol.Site));
        return lines;
    }

    private static IReadOnlyList<string> CreateCeSourceLines(CeSourceAuditSnapshot snapshot)
    {
        List<string> lines =
        [
            $"Source root: {snapshot.SourceRoot}",
            $"Coverage: {snapshot.Summary.AnyCatalogCoverageCount}/{snapshot.Summary.SelectionCount} selected functions map to one debugger catalog surface.",
            $"Hooks: {snapshot.Summary.WatchHookCoverageCount}, debugger functions: {snapshot.Summary.DebuggerFunctionCoverageCount}, signatures: {snapshot.Summary.SignatureCoverageCount}.",
        ];
        lines.AddRange(
            snapshot
                .Areas.Take(6)
                .Select(static area =>
                    $"{area.Area}: {area.CoveredCount}/{area.FunctionCount} covered, {area.MissingCount} missing"
                )
        );
        lines.AddRange(
            snapshot
                .Functions.Where(static function => !function.Coverage.AnyCatalogCoverage)
                .Take(8)
                .Select(static function => $"Missing: {function.Name} ({function.RelativePath}:{function.LineNumber})")
        );
        return lines;
    }

    private static IReadOnlyList<string> CreateOfflineRangeLines(OfflineRangeDiagnosticsBundle diagnostics)
    {
        var firstLoaded = diagnostics.LoadedSlots[0];
        var lastLoaded = diagnostics.LoadedSlots[^1];
        List<string> lines =
        [
            $"Requested range {FormatSlotStem(diagnostics.RequestedFirstSlot)}-{FormatSlotStem(diagnostics.RequestedLastSlot)}; loaded {diagnostics.LoadedSlots.Count} slots from {firstLoaded.SlotStem} to {lastLoaded.SlotStem}.",
            $"Progression history: {diagnostics.ProgressionHistory.Slots.Count} player snapshots, {diagnostics.ProgressionHistory.Slots.Count(static slot => slot.IsBaseline)} baseline slots, {diagnostics.ProgressionHistory.Slots.Sum(static slot => slot.Changes.Count)} recorded changes.",
            $"Player SAR history: {diagnostics.SarHistory.Slots.Count} slots, {diagnostics.SarHistory.Tracks.Count} tracks, {diagnostics.SarLifecycle.Fingerprints.Count} fingerprints, {diagnostics.SarTransitionAnalysis.Transitions.Count} transitions.",
            $"Player SAR lifecycle report: {diagnostics.SarLifecycleReport.Fingerprints.Count} recurring fingerprints, {diagnostics.SarLifecycleReport.Tracks.Count} detailed tracks, {diagnostics.SarLifecycleReport.OmittedTrackRowCount} omitted track rows.",
            $"Player SAR transition report: +{diagnostics.SarTransitionReport.Transitions.Sum(static transition => transition.Summary.AddedCount)} / -{diagnostics.SarTransitionReport.Transitions.Sum(static transition => transition.Summary.RemovedCount)} / moved {diagnostics.SarTransitionReport.Transitions.Sum(static transition => transition.Summary.MovedCount)} / changed {diagnostics.SarTransitionReport.Transitions.Sum(static transition => transition.Summary.ChangedCount)}.",
            $"Typed context delta {firstLoaded.SlotStem} -> {lastLoaded.SlotStem}: player {diagnostics.TypedContextDelta.Player.Kind}, quests {FormatSignedInt(diagnostics.TypedContextDelta.Player.QuestDelta)}, rumors {FormatSignedInt(diagnostics.TypedContextDelta.Player.RumorsDelta)}, blessings {FormatSignedInt(diagnostics.TypedContextDelta.Player.BlessingsDelta)}, curses {FormatSignedInt(diagnostics.TypedContextDelta.Player.CursesDelta)}, schematics {FormatSignedInt(diagnostics.TypedContextDelta.Player.SchematicsDelta)}, fog files {diagnostics.TypedContextDelta.TownMapFogs.ChangedFiles}, revealed tiles {FormatSignedInt(diagnostics.TypedContextDelta.TownMapFogs.RevealedTileDelta)}.",
        ];

        if (diagnostics.TypedContextDelta.Player.Reputation.Kind is not SaveTypedReputationDeltaKind.Absent)
        {
            var reputation = diagnostics.TypedContextDelta.Player.Reputation;
            var changedSlots =
                reputation.ChangedSlots.Count == 0
                    ? string.Empty
                    : $" [{string.Join(", ", reputation.ChangedSlots.Take(6))}{(reputation.ChangedSlots.Count > 6 ? ", ..." : string.Empty)}]";
            lines.Add($"Reputation delta: {reputation.Kind}, {reputation.Count} affected slots{changedSlots}.");
        }

        lines.AddRange(
            diagnostics
                .ProgressionHistory.Slots.Where(static slot => slot.Changes.Count > 0)
                .OrderByDescending(static slot => slot.Changes.Count)
                .ThenBy(static slot => slot.Slot)
                .Take(3)
                .Select(slot =>
                    $"Progression {FormatSlotStem(slot.Slot)}: {slot.Changes.Count} changes :: {string.Join(", ", slot.Changes.Select(static change => change.Category).Distinct(StringComparer.OrdinalIgnoreCase).Take(4))}"
                )
        );

        if (diagnostics.CharacterSarDiff is { } characterSarDiff)
        {
            var kindSummary = string.Join(
                ", ",
                Enum.GetValues<CharacterSarDiffKind>()
                    .Select(kind => new
                    {
                        Kind = kind,
                        Count = characterSarDiff.Entries.Count(entry => entry.Kind == kind),
                    })
                    .Where(static pair => pair.Count > 0)
                    .Select(static pair => $"{pair.Kind}={pair.Count}")
            );
            lines.Add(
                $"Character SAR diff {FormatSlotStem(diagnostics.CharacterSarDiffBeforeSlot ?? firstLoaded.Slot)} -> {FormatSlotStem(diagnostics.CharacterSarDiffAfterSlot ?? lastLoaded.Slot)}: {characterSarDiff.Entries.Count} changes{(kindSummary.Length > 0 ? $" ({kindSummary})" : string.Empty)}."
            );
            lines.AddRange(
                characterSarDiff
                    .Entries.Take(3)
                    .Select(entry =>
                        $"SAR {entry.Kind}: {entry.Fingerprint} :: {entry.BeforeValueSummary ?? "(none)"} -> {entry.AfterValueSummary ?? "(none)"}"
                    )
            );
        }
        else
        {
            lines.Add(
                "Character SAR diff: no comparable player character records were available across the requested range."
            );
        }

        if (
            diagnostics.CharacterSarFullDump is { } characterSarFullDump
            && diagnostics.CharacterSarDumpSlot is { } dumpSlot
        )
        {
            lines.Add(
                $"Character SAR full dump {FormatSlotStem(dumpSlot)}: {characterSarFullDump.PrintedCount} entries, {characterSarFullDump.Entries.Count(static entry => entry.IsFiller)} filler, {characterSarFullDump.Entries.Count(static entry => entry.ElementSize == 4)} int32 entries."
            );
            lines.AddRange(
                characterSarFullDump
                    .Entries.Take(3)
                    .Select(entry =>
                        $"SAR dump: bs={entry.BitsetId} {entry.Annotation} [{entry.ElementSize}x{entry.ElementCount}]"
                    )
            );
        }

        if (diagnostics.GlobalDiffs.Count > 0)
        {
            lines.AddRange(
                diagnostics
                    .GlobalDiffs.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(pair =>
                    {
                        var diff = pair.Value;
                        var segments = new List<string>
                        {
                            $"{diff.ChangedInts} changed ints",
                            $"+{diff.AddedInts}/-{diff.RemovedInts} ints",
                            $"{diff.ChangedTailBytes} changed tail bytes",
                        };

                        if (diff.Window is { } window)
                            segments.Add($"window @{window.StartInt} (-{window.RemovedInts}/+{window.AddedInts})");

                        if (diff.SaveIdPairs is { } saveIdPairs)
                            segments.Add($"{saveIdPairs.TotalChangedPairs} changed id-pairs");

                        return $"Global diff {pair.Key}: {string.Join(", ", segments)}.";
                    })
            );
        }
        else
        {
            lines.Add("Global diff: no comparable save-global files were present in both boundary slots.");
        }

        lines.AddRange(
            diagnostics
                .GlobalDumps.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair =>
                {
                    var dump = pair.Value;
                    var segments = new List<string>
                    {
                        $"{dump.NonZeroSummary.Count} non-zero ints",
                        $"{dump.HexRows.Count} hex rows previewed",
                        $"{dump.AsciiCandidates.Count} ASCII candidates",
                    };

                    if (dump.SaveIdPairDetails is { } saveIdPairDetails)
                        segments.Add($"{saveIdPairDetails.NonZeroPairs} non-zero id-pairs");

                    if (dump.QuadPreviewRows.Count > 0)
                        segments.Add($"{dump.QuadPreviewRows.Count} quad rows");

                    return $"Global dump {pair.Key} @ {lastLoaded.SlotStem}: {string.Join(", ", segments)}.";
                })
        );

        foreach (var fileName in SaveGlobalAnalysisService.KnownFileNames)
        {
            diagnostics.GlobalRangeAnalysis.HotIndices.TryGetValue(fileName, out var hotIndices);
            diagnostics.GlobalRangeAnalysis.WindowPatterns.TryGetValue(fileName, out var patterns);
            diagnostics.GlobalRangeAnalysis.WindowTraces.TryGetValue(fileName, out var traces);

            var hottestSegment = hotIndices is { Count: > 0 }
                ? $", hottest int {hotIndices[0].Index} ({hotIndices[0].Hits} hits)"
                : string.Empty;
            lines.Add(
                $"Range analysis {fileName}: {(hotIndices?.Count ?? 0)} hot indices, {(patterns?.Count ?? 0)} window patterns, {(traces?.Count ?? 0)} traces{hottestSegment}."
            );
        }

        lines.Add(
            $"Range families: front matter {diagnostics.GlobalRangeAnalysis.FrontMatterFamilies.Count}, tail {diagnostics.GlobalRangeAnalysis.TailFamilies.Count}, data2 prefix {diagnostics.GlobalRangeAnalysis.PrefixFamilies.Count}, data2 suffix {diagnostics.GlobalRangeAnalysis.SuffixFamilies.Count}."
        );

        lines.Add(
            $"Binary diff {firstLoaded.SlotStem} -> {lastLoaded.SlotStem}: {diagnostics.BinaryDiff.ChangedFileCount}/{diagnostics.BinaryDiff.TotalFiles} files changed, {diagnostics.BinaryDiff.IdenticalFileCount} identical."
        );
        lines.AddRange(
            diagnostics
                .BinaryDiff.Files.Take(3)
                .Select(file =>
                    file.OnlyInA ? $"Binary file {file.Path}: only in {firstLoaded.SlotStem}."
                    : file.OnlyInB ? $"Binary file {file.Path}: only in {lastLoaded.SlotStem}."
                    : $"Binary file {file.Path}: {file.ChangedByteCount} changed bytes across {file.Regions.Count} diff regions."
                )
        );

        if (
            diagnostics.BinaryPreview is { Regions.Count: > 0 } preview
            && diagnostics.BinaryPreviewPath is { } previewPath
        )
        {
            var region = preview.Regions[0];
            lines.Add(
                $"Binary preview {previewPath}: region @ {region.Offset}, len {region.Length}, {region.ChangedByteCount} changed bytes, {region.Rows.Count} hex rows."
            );
        }

        return lines;
    }

    private void ApplyOfflineRangePresentation(OfflineRangeDiagnosticsBundle diagnostics)
    {
        OfflineRangeSemanticSummaryText = CreateOfflineRangeSemanticSummary(diagnostics);
        OfflineRangeHighlights = CreateOfflineRangeHighlights(diagnostics);
        OfflineRangeSections = CreateOfflineRangeSections(diagnostics);
        HasOfflineRangeSections = OfflineRangeSections.Count > 0;
    }

    private void ClearOfflineRangePresentation()
    {
        OfflineRangeSemanticSummaryText =
            "Compare two or more saves to review progression, journal, and world-state deltas before inspecting raw file-time diffs.";
        OfflineRangeHighlights = [];
        OfflineRangeSections = [];
        HasOfflineRangeSections = false;
    }

    private static string CreateOfflineRangeSemanticSummary(OfflineRangeDiagnosticsBundle diagnostics)
    {
        var firstLoaded = diagnostics.LoadedSlots[0];
        var lastLoaded = diagnostics.LoadedSlots[^1];
        return $"Comparing {firstLoaded.SlotStem} to {lastLoaded.SlotStem}: {diagnostics.ProgressionHistory.Slots.Count.ToString(CultureInfo.InvariantCulture)} progression snapshots, {diagnostics.BinaryDiff.ChangedFileCount.ToString(CultureInfo.InvariantCulture)} changed files, and {diagnostics.TypedContextDelta.Player.QuestDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)} quest-state delta across the selected range.";
    }

    private static IReadOnlyList<DebuggerInsightCard> CreateOfflineRangeHighlights(
        OfflineRangeDiagnosticsBundle diagnostics
    )
    {
        var firstLoaded = diagnostics.LoadedSlots[0];
        var lastLoaded = diagnostics.LoadedSlots[^1];
        return
        [
            new(
                "Range",
                $"{firstLoaded.SlotStem}-{lastLoaded.SlotStem}",
                $"{diagnostics.LoadedSlots.Count.ToString(CultureInfo.InvariantCulture)} loaded saves"
            ),
            new(
                "Progression",
                diagnostics
                    .ProgressionHistory.Slots.Sum(static slot => slot.Changes.Count)
                    .ToString(CultureInfo.InvariantCulture),
                "recorded progression changes"
            ),
            new(
                "Quests",
                diagnostics.TypedContextDelta.Player.QuestDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture),
                $"{diagnostics.TypedContextDelta.Player.RumorsDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)} rumors"
            ),
            new(
                "Binary diff",
                diagnostics.BinaryDiff.ChangedFileCount.ToString(CultureInfo.InvariantCulture),
                $"{diagnostics.BinaryDiff.TotalFiles.ToString(CultureInfo.InvariantCulture)} compared files"
            ),
        ];
    }

    private static IReadOnlyList<DebuggerInsightSection> CreateOfflineRangeSections(
        OfflineRangeDiagnosticsBundle diagnostics
    )
    {
        List<DebuggerInsightSection> sections = [];
        sections.Add(
            new(
                "Progression summary",
                $"{diagnostics.ProgressionHistory.Slots.Count.ToString(CultureInfo.InvariantCulture)} snapshots across the selected range",
                [
                    .. diagnostics
                        .ProgressionHistory.Slots.Where(static slot => slot.Changes.Count > 0)
                        .OrderByDescending(static slot => slot.Changes.Count)
                        .ThenBy(static slot => slot.Slot)
                        .Take(5)
                        .Select(slot =>
                            $"{FormatSlotStem(slot.Slot)} :: {slot.Changes.Count.ToString(CultureInfo.InvariantCulture)} changes"
                        ),
                ]
            )
        );

        sections.Add(
            new(
                "Journal and player delta",
                $"Quests {diagnostics.TypedContextDelta.Player.QuestDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)} · rumors {diagnostics.TypedContextDelta.Player.RumorsDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)} · blessings {diagnostics.TypedContextDelta.Player.BlessingsDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)} · curses {diagnostics.TypedContextDelta.Player.CursesDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)}",
                [
                    $"Schematics :: {diagnostics.TypedContextDelta.Player.SchematicsDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)}",
                    $"Revealed map tiles :: {diagnostics.TypedContextDelta.TownMapFogs.RevealedTileDelta.ToString("+#;-#;0", CultureInfo.InvariantCulture)}",
                    $"Changed fog files :: {diagnostics.TypedContextDelta.TownMapFogs.ChangedFiles.ToString(CultureInfo.InvariantCulture)}",
                ]
            )
        );

        sections.Add(
            new(
                "Binary and global diff",
                $"{diagnostics.BinaryDiff.ChangedFileCount.ToString(CultureInfo.InvariantCulture)} changed files across {diagnostics.BinaryDiff.TotalFiles.ToString(CultureInfo.InvariantCulture)} compared files",
                [
                    $"Global files with diff detail :: {diagnostics.GlobalDiffs.Count.ToString(CultureInfo.InvariantCulture)}",
                    $"Front-matter families :: {diagnostics.GlobalRangeAnalysis.FrontMatterFamilies.Count.ToString(CultureInfo.InvariantCulture)}",
                    $"Tail families :: {diagnostics.GlobalRangeAnalysis.TailFamilies.Count.ToString(CultureInfo.InvariantCulture)}",
                ]
            )
        );

        if (diagnostics.CharacterSarDiff is { } sarDiff)
        {
            sections.Add(
                new(
                    "Character SAR movement",
                    $"{sarDiff.Entries.Count.ToString(CultureInfo.InvariantCulture)} SAR changes between boundary saves",
                    [.. sarDiff.Entries.Take(5).Select(entry => $"{entry.Kind} :: {entry.Fingerprint}")]
                )
            );
        }

        return sections;
    }

    private void ApplyOfflineSavePresentation(OfflineSaveDiagnosticsBundle diagnostics)
    {
        OfflineSaveSemanticSummaryText = CreateOfflineSaveSemanticSummary(diagnostics);
        OfflineSaveHighlights = CreateOfflineSaveHighlights(diagnostics);
        OfflineSaveSections = CreateOfflineSaveSections(diagnostics);
        HasOfflineSaveSections = OfflineSaveSections.Count > 0;
    }

    private void ClearOfflineSavePresentation()
    {
        OfflineSaveSemanticSummaryText =
            "Load a save slot to review player, journal, and world-state summaries before inspecting raw file-time details.";
        OfflineSaveHighlights = [];
        OfflineSaveSections = [];
        HasOfflineSaveSections = false;
    }

    private void ApplyObjectProbePresentation(ObjectProbeObjectSnapshot snapshot)
    {
        ObjectProbeSemanticSummaryText =
            $"{snapshot.ObjectTypeText} resolved via {snapshot.ResolutionSource} with {snapshot.Sections.Count.ToString(CultureInfo.InvariantCulture)} decoded sections and {snapshot.Details.Count.ToString(CultureInfo.InvariantCulture)} detail rows.";
        ObjectProbeHighlightCards = CreateObjectProbeHighlights(snapshot);
        ObjectProbeSectionSummaries = CreateObjectProbeSections(snapshot);
        HasObjectProbeSectionSummaries = ObjectProbeSectionSummaries.Count > 0;
    }

    private void ClearObjectProbePresentation()
    {
        ObjectProbeSemanticSummaryText =
            "Select an inspected object to review decoded sections before inspecting raw runtime fields.";
        ObjectProbeHighlightCards = [];
        ObjectProbeSectionSummaries = [];
        HasObjectProbeSectionSummaries = false;
    }

    private static string CreateOfflineSaveSemanticSummary(OfflineSaveDiagnosticsBundle diagnostics)
    {
        var leader = diagnostics.Player?.Progression.Name ?? diagnostics.Audit.LeaderName;
        return $"{diagnostics.Loaded.SlotStem} tracks {leader} level {diagnostics.Audit.LeaderLevel} on map {diagnostics.Audit.MapId} with {diagnostics.QuestBook.Quests.Count.ToString(CultureInfo.InvariantCulture)} quests, {diagnostics.QuestBook.Reputation.Count.ToString(CultureInfo.InvariantCulture)} reputations, and {diagnostics.Structure.Extensions.Count.ToString(CultureInfo.InvariantCulture)} discovered asset extensions.";
    }

    private static IReadOnlyList<DebuggerInsightCard> CreateOfflineSaveHighlights(
        OfflineSaveDiagnosticsBundle diagnostics
    )
    {
        var player = diagnostics.Player;
        var questBook = diagnostics.QuestBook;
        return
        [
            new(
                "Leader",
                player?.Progression.Name ?? diagnostics.Audit.LeaderName,
                $"Level {diagnostics.Audit.LeaderLevel.ToString(CultureInfo.InvariantCulture)}"
            ),
            new(
                "Journal",
                questBook.Quests.Count.ToString(CultureInfo.InvariantCulture),
                $"{questBook.Reputation.Count.ToString(CultureInfo.InvariantCulture)} reputations"
            ),
            new(
                "Blessings",
                questBook.Blessings.Count.ToString(CultureInfo.InvariantCulture),
                $"{questBook.Curses.Count.ToString(CultureInfo.InvariantCulture)} curses"
            ),
            new(
                "Rumors",
                (player?.Rumors.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                $"{questBook.Schematics.Count.ToString(CultureInfo.InvariantCulture)} schematics"
            ),
            new(
                "World",
                $"Map {diagnostics.Structure.MapId.ToString(CultureInfo.InvariantCulture)}",
                $"Tile {diagnostics.Structure.LeaderTileX.ToString(CultureInfo.InvariantCulture)},{diagnostics.Structure.LeaderTileY.ToString(CultureInfo.InvariantCulture)}"
            ),
        ];
    }

    private static IReadOnlyList<DebuggerInsightSection> CreateOfflineSaveSections(
        OfflineSaveDiagnosticsBundle diagnostics
    )
    {
        List<DebuggerInsightSection> sections = [];
        if (diagnostics.Player is { } player)
        {
            sections.Add(
                new(
                    "Player snapshot",
                    $"{player.Progression.Name ?? diagnostics.Audit.LeaderName} · gold {player.Progression.Gold.ToString(CultureInfo.InvariantCulture)} · XP {player.Progression.ExperiencePoints.ToString(CultureInfo.InvariantCulture)}",
                    [
                        .. player
                            .PrimaryAttributes.Take(6)
                            .Select(static stat =>
                                $"{stat.Label}: {stat.Value.ToString(CultureInfo.InvariantCulture)}"
                            ),
                    ]
                )
            );
        }

        if (diagnostics.QuestBook.Quests.Count > 0)
        {
            sections.Add(
                new(
                    "Quest journal sample",
                    $"{diagnostics.QuestBook.Quests.Count.ToString(CultureInfo.InvariantCulture)} decoded quest entries",
                    [
                        .. diagnostics
                            .QuestBook.Quests.Take(6)
                            .Select(static quest => $"{quest.Label ?? "(unlabeled)"} :: {quest.StateDescription}"),
                    ]
                )
            );
        }

        if (diagnostics.QuestBook.Reputation.Count > 0)
        {
            sections.Add(
                new(
                    "Reputation ledger",
                    $"{diagnostics.QuestBook.Reputation.Count.ToString(CultureInfo.InvariantCulture)} tracked reputation slots",
                    [
                        .. diagnostics
                            .QuestBook.Reputation.Take(6)
                            .Select(static reputation =>
                                $"Reputation slot {reputation.Slot.ToString(CultureInfo.InvariantCulture)} :: {reputation.Value.ToString(CultureInfo.InvariantCulture)}"
                            ),
                    ]
                )
            );
        }

        if (diagnostics.QuestBook.Blessings.Count > 0 || diagnostics.QuestBook.Curses.Count > 0)
        {
            List<string> entries = [];
            entries.AddRange(
                diagnostics
                    .QuestBook.Blessings.Take(6)
                    .Select(static blessing => $"Blessing :: {blessing.ToString(CultureInfo.InvariantCulture)}")
            );
            entries.AddRange(
                diagnostics
                    .QuestBook.Curses.Take(6)
                    .Select(static curse => $"Curse :: {curse.ToString(CultureInfo.InvariantCulture)}")
            );

            sections.Add(
                new(
                    "Blessings and curses",
                    $"{diagnostics.QuestBook.Blessings.Count.ToString(CultureInfo.InvariantCulture)} blessings · {diagnostics.QuestBook.Curses.Count.ToString(CultureInfo.InvariantCulture)} curses",
                    entries
                )
            );
        }

        if ((diagnostics.Player?.Rumors.Count ?? 0) > 0 || diagnostics.QuestBook.Schematics.Count > 0)
        {
            List<string> entries =
            [
                $"Rumor count :: {(diagnostics.Player?.Rumors.Count ?? 0).ToString(CultureInfo.InvariantCulture)}",
            ];
            entries.AddRange(
                diagnostics
                    .QuestBook.Schematics.Take(6)
                    .Select(static schematic => $"Schematic :: {schematic.ToString(CultureInfo.InvariantCulture)}")
            );

            sections.Add(
                new(
                    "Rumors and schematics",
                    $"{(diagnostics.Player?.Rumors.Count ?? 0).ToString(CultureInfo.InvariantCulture)} rumors · {diagnostics.QuestBook.Schematics.Count.ToString(CultureInfo.InvariantCulture)} schematics",
                    entries
                )
            );
        }

        if (diagnostics.QuestBook.QuestCharacters.Count > 0)
        {
            sections.Add(
                new(
                    "Character roster",
                    $"{diagnostics.QuestBook.QuestCharacters.Count.ToString(CultureInfo.InvariantCulture)} character records with quest data",
                    [
                        .. diagnostics
                            .QuestBook.QuestCharacters.Take(5)
                            .Select(static character =>
                                $"{character.Name ?? "(unnamed)"} · level {character.Level.ToString(CultureInfo.InvariantCulture)} · rumors {character.RumorsCount.ToString(CultureInfo.InvariantCulture)}"
                            ),
                    ]
                )
            );
        }

        return sections;
    }

    private static IReadOnlyList<DebuggerInsightCard> CreateObjectProbeHighlights(ObjectProbeObjectSnapshot snapshot) =>
        [
            new("Type", snapshot.ObjectTypeText, snapshot.StatusText),
            new("Handle", snapshot.HandleHex, snapshot.ResolutionSource),
            new("Prototype", snapshot.PrototypeText, snapshot.PrototypeHandleText),
            new(
                "Decoded",
                snapshot.Sections.Count.ToString(CultureInfo.InvariantCulture),
                $"{snapshot.Details.Count.ToString(CultureInfo.InvariantCulture)} detail rows"
            ),
        ];

    private static IReadOnlyList<DebuggerInsightSection> CreateObjectProbeSections(
        ObjectProbeObjectSnapshot snapshot
    ) =>
        [
            .. snapshot.Sections.Select(section => new DebuggerInsightSection(
                section.Title,
                $"{section.Details.Count.ToString(CultureInfo.InvariantCulture)} decoded values · {section.SourceText}",
                [.. section.Details.Take(4).Select(static detail => $"{detail.Label}: {detail.Value}")]
            )),
        ];

    private static IReadOnlyList<string> CreateOfflineDumpLines(
        DebuggerChoiceOption template,
        string path,
        string output
    )
    {
        var preview = output.Split(["\r\n", "\n"], 2, StringSplitOptions.None).FirstOrDefault();

        return
        [
            $"Template: {template.Label}",
            $"Input: {path}",
            $"Output: {output.Length.ToString(CultureInfo.InvariantCulture)} characters.",
            string.IsNullOrWhiteSpace(preview) ? "Preview: (empty output)" : $"Preview: {preview}",
        ];
    }

    private void ApplyOfflineDumpTemplatePresentation(OfflineDumpRenderResult result)
    {
        OfflineDumpSemanticSummaryText = result.Summary;
        OfflineDumpHighlights = result.Highlights;
        OfflineDumpSections = result.Sections;
        HasOfflineDumpSections = result.Sections.Count > 0;
    }

    private void ClearOfflineDumpTemplatePresentation()
    {
        OfflineDumpSemanticSummaryText =
            "Render a report template to review structured output before opening the full report text.";
        OfflineDumpHighlights = [];
        OfflineDumpSections = [];
        HasOfflineDumpSections = false;
    }

    private static OfflineDumpRenderResult CreateOfflineDumpRenderResult(DebuggerChoiceOption template, string path) =>
        template.Token switch
        {
            "message" => CreateMessageDumpRenderResult(template, path),
            "text-data" => CreateTextDataDumpRenderResult(template, path),
            _ => CreateGenericDumpRenderResult(template, path, ExecuteOfflineDumpTemplate(template.Token, path)),
        };

    private static OfflineDumpRenderResult CreateMessageDumpRenderResult(DebuggerChoiceOption template, string path)
    {
        var file = MessageFormat.ParseFile(path);
        var output = MessageDumper.Dump(file);
        var withSound = file.Entries.Count(entry => entry.SoundId is not null);
        var textOnly = file.Entries.Count - withSound;
        var minId = file.Entries.Count == 0 ? 0 : file.Entries.Min(entry => entry.Index);
        var maxId = file.Entries.Count == 0 ? 0 : file.Entries.Max(entry => entry.Index);
        return new(
            output,
            $"{template.Label} exposes {file.Entries.Count.ToString(CultureInfo.InvariantCulture)} messages from {Path.GetFileName(path)}, with {(withSound > 0 ? withSound.ToString(CultureInfo.InvariantCulture) + " sound-linked" : "text-only")} entries.",
            [
                new("Template", template.Label, template.Description),
                new(
                    "Entries",
                    file.Entries.Count.ToString(CultureInfo.InvariantCulture),
                    $"{textOnly.ToString(CultureInfo.InvariantCulture)} text-only"
                ),
                new(
                    "Sound IDs",
                    withSound.ToString(CultureInfo.InvariantCulture),
                    withSound > 0 ? "Entries with linked sound playback" : "No sound-linked entries"
                ),
                new(
                    "ID range",
                    file.Entries.Count == 0
                        ? "Empty"
                        : $"{minId.ToString(CultureInfo.InvariantCulture)}-{maxId.ToString(CultureInfo.InvariantCulture)}",
                    Path.GetFileName(path)
                ),
            ],
            file.Entries.Count == 0
                ? []
                :
                [
                    new(
                        "Message sample",
                        $"{file.Entries.Count.ToString(CultureInfo.InvariantCulture)} decoded entries",
                        [
                            .. file
                                .Entries.Take(6)
                                .Select(static entry =>
                                    $"[{entry.Index.ToString(CultureInfo.InvariantCulture)}] {entry.Text}"
                                ),
                        ]
                    ),
                ]
        );
    }

    private static OfflineDumpRenderResult CreateTextDataDumpRenderResult(DebuggerChoiceOption template, string path)
    {
        var file = TextDataFormat.ParseFile(path);
        var output = TextDataDumper.Dump(file);
        var longestKeyLength = file.Entries.Count == 0 ? 0 : file.Entries.Max(entry => entry.Key.Length);
        return new(
            output,
            $"{template.Label} exposes {file.Entries.Count.ToString(CultureInfo.InvariantCulture)} keyed values from {Path.GetFileName(path)}.",
            [
                new("Template", template.Label, template.Description),
                new("Entries", file.Entries.Count.ToString(CultureInfo.InvariantCulture), "Decoded key-value pairs"),
                new(
                    "Longest key",
                    longestKeyLength.ToString(CultureInfo.InvariantCulture),
                    "Useful for spotting narrow vs wide lookup tables"
                ),
                new("Input", Path.GetFileName(path), Path.GetDirectoryName(path) ?? string.Empty),
            ],
            file.Entries.Count == 0
                ? []
                :
                [
                    new(
                        "Entry sample",
                        $"{file.Entries.Count.ToString(CultureInfo.InvariantCulture)} decoded values",
                        [.. file.Entries.Take(6).Select(static entry => $"{entry.Key} :: {entry.Value}")]
                    ),
                ]
        );
    }

    private static OfflineDumpRenderResult CreateGenericDumpRenderResult(
        DebuggerChoiceOption template,
        string path,
        string output
    )
    {
        var sections = CreateDumpOutputSections(output);
        var lines = output.Split(["\r\n", "\n"], StringSplitOptions.None);
        return new(
            output,
            sections.Count == 0
                ? $"{template.Label} rendered from {Path.GetFileName(path)} with raw output only."
                : $"{template.Label} rendered from {Path.GetFileName(path)} with {sections.Count.ToString(CultureInfo.InvariantCulture)} detected output sections.",
            [
                new("Template", template.Label, template.Description),
                new("Input", Path.GetFileName(path), Path.GetDirectoryName(path) ?? string.Empty),
                new(
                    "Output",
                    output.Length.ToString(CultureInfo.InvariantCulture),
                    $"{lines.Length.ToString(CultureInfo.InvariantCulture)} lines"
                ),
                new(
                    "Sections",
                    sections.Count.ToString(CultureInfo.InvariantCulture),
                    sections.Count == 0 ? "No named headings detected" : sections[0].Title
                ),
            ],
            sections
        );
    }

    private static IReadOnlyList<DebuggerInsightSection> CreateDumpOutputSections(string output)
    {
        var lines = output.Split(["\r\n", "\n"], StringSplitOptions.None);
        List<DebuggerInsightSection> sections = [];
        string? currentTitle = null;
        List<string> currentPreview = [];

        foreach (var line in lines)
        {
            var heading = TryExtractDumpHeading(line);
            if (!string.IsNullOrWhiteSpace(heading))
            {
                if (!string.IsNullOrWhiteSpace(currentTitle))
                    sections.Add(
                        new(
                            currentTitle,
                            $"{currentPreview.Count.ToString(CultureInfo.InvariantCulture)} preview lines",
                            [.. currentPreview]
                        )
                    );

                currentTitle = heading;
                currentPreview = [];
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentTitle))
                continue;

            var trimmed = line.Trim();
            if (trimmed.Length == 0 || currentPreview.Count >= 4)
                continue;

            currentPreview.Add(trimmed);
        }

        if (!string.IsNullOrWhiteSpace(currentTitle))
            sections.Add(
                new(
                    currentTitle,
                    $"{currentPreview.Count.ToString(CultureInfo.InvariantCulture)} preview lines",
                    [.. currentPreview]
                )
            );

        if (sections.Count > 0)
            return sections;

        return
        [
            new(
                "Preview",
                "First non-empty output lines",
                [.. lines.Select(static line => line.Trim()).Where(static line => line.Length > 0).Take(6)]
            ),
        ];
    }

    private static string? TryExtractDumpHeading(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
            return null;

        if (trimmed.StartsWith("===") && trimmed.EndsWith("===") && trimmed.Length > 6)
            return trimmed.Trim('=').Trim();

        if (trimmed.StartsWith("---") && trimmed.EndsWith("---") && trimmed.Length > 6)
            return trimmed.Trim('-').Trim();

        if (trimmed.Contains("───", StringComparison.Ordinal) || trimmed.Contains("═══", StringComparison.Ordinal))
        {
            var normalized = trimmed
                .Replace('─', ' ')
                .Replace('═', ' ')
                .Replace('│', ' ')
                .Replace('└', ' ')
                .Replace('┌', ' ')
                .Replace('┘', ' ')
                .Replace('┐', ' ')
                .Trim();
            return normalized.Length == 0 ? null : normalized;
        }

        return null;
    }

    private static string ExecuteOfflineDumpTemplate(string token, string path) =>
        token switch
        {
            "art" => DumpSingleFile(path, ArtFormat.ParseFile, ArtDumper.Dump),
            "dialog" => DumpSingleFile(path, DialogFormat.ParseFile, DialogDumper.Dump),
            "facwalk" => DumpSingleFile(path, FacWalkFormat.ParseFile, FacWalkDumper.Dump),
            "jmp" => DumpSingleFile(path, JmpFormat.ParseFile, JmpDumper.Dump),
            "map-props" => DumpSingleFile(path, MapPropertiesFormat.ParseFile, MapPropertiesDumper.Dump),
            "message" => DumpSingleFile(path, MessageFormat.ParseFile, MessageDumper.Dump),
            "proto" => DumpSingleFile(path, ProtoFormat.ParseFile, ProtoDumper.Dump),
            "save-index" => DumpSingleFile(path, SaveIndexFormat.ParseFile, SaveIndexDumper.Dump),
            "save-info" => DumpSingleFile(path, SaveInfoFormat.ParseFile, SaveInfoDumper.Dump),
            "save-dir" => DumpSaveDirectory(path),
            "script" => DumpSingleFile(path, ScriptFormat.ParseFile, ScriptDumper.Dump),
            "sector" => DumpSingleFile(path, SectorFormat.ParseFile, SectorDumper.Dump),
            "terrain" => DumpSingleFile(path, TerrainFormat.ParseFile, TerrainDumper.Dump),
            "text-data" => DumpSingleFile(path, TextDataFormat.ParseFile, TextDataDumper.Dump),
            _ => throw new InvalidOperationException($"Unknown dump template '{token}'."),
        };

    private static string DumpSaveDirectory(string path) =>
        Directory.Exists(path)
            ? SaveDumper.Dump(path)
            : throw new DirectoryNotFoundException($"Directory not found: {path}");

    private static string DumpSingleFile<T>(string path, Func<string, T> parse, Func<T, string> dump)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        return dump(parse(path));
    }

    private static IReadOnlyList<string> ParseReadArguments(string rawText) =>
        string.IsNullOrWhiteSpace(rawText)
            ? []
            : [.. rawText.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    private static int ParseSlotNumber(string slotText)
    {
        var normalized = slotText.Trim().PadLeft(4, '0');
        return int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out var slot)
            ? slot
            : throw new FormatException($"Invalid slot number: {slotText}");
    }

    private void ReplaceActiveIntercept(InterceptHandle handle)
    {
        DisposeActiveIntercept();
        _activeInterceptHandle = handle;
        ApplyInterceptSnapshot(handle.Snapshot);
    }

    private void DisposeActiveIntercept()
    {
        _activeInterceptHandle?.Dispose();
        _activeInterceptHandle = null;
    }

    private void ApplyInterceptSnapshot(InterceptSnapshot snapshot)
    {
        InterceptStatusText = snapshot.Status;
        InterceptResultLines = CreateInterceptLines(snapshot);
    }

    private InterceptStartRequest CreateInterceptStartRequest(AttachedSessionSnapshot session)
    {
        return new InterceptStartRequest(
            session,
            _interceptTargetResolver.Resolve(session.ProcessId, InterceptTargetText),
            ParsePositiveInt(InterceptStackCaptureCountText, "stack capture dword count"),
            new InterceptMutationRequest(
                InterceptSkipOriginal,
                ParseNonNegativeInt(InterceptCleanupBytesText, "cleanup bytes"),
                ParseOptionalUInt32(InterceptReturnEaxText),
                ParseOptionalUInt32(InterceptReturnEdxText),
                ParseInterceptRegisterOverrides(InterceptRegisterOverridesText),
                ParseInterceptArgumentOverrides(InterceptArgumentOverridesText)
            ),
            ParseInterceptDereferences(InterceptDereferencesText)
        );
    }

    private static CrashDumpKind ParseCrashDumpKind(string dumpKindText) =>
        dumpKindText.Trim().ToLowerInvariant() switch
        {
            "mini" => CrashDumpKind.Mini,
            "full" => CrashDumpKind.Full,
            _ => throw new FormatException($"Invalid dump kind: {dumpKindText}. Use 'mini' or 'full'."),
        };

    private static int ParseDumpCount(string dumpCountText) =>
        int.TryParse(dumpCountText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dumpCount)
            ? dumpCount
            : throw new FormatException($"Invalid dump count: {dumpCountText}");

    private static int ParsePositiveInt(string text, string label)
    {
        var value = ParseNonNegativeInt(text, label);
        return value > 0 ? value : throw new FormatException($"{label} must be greater than zero.");
    }

    private static int ParseNonNegativeInt(string text, string label) =>
        int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : throw new FormatException($"Invalid {label}: {text}");

    private static uint? ParseOptionalUInt32(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : ParseUInt32Token(text);

    private static uint ParseUInt32Token(string text)
    {
        var token = text.Trim();
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue)
                ? hexValue
                : throw new FormatException($"Invalid hex value: {text}");
        }

        return uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"Invalid unsigned value: {text}");
    }

    private static bool TryParseRvaValue(string text, out uint rva)
    {
        var token = text.Trim();
        if (token.StartsWith("rva:", StringComparison.OrdinalIgnoreCase))
            token = token[4..].Trim();

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return uint.TryParse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rva);

        return uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out rva);
    }

    private static InterceptRegisterOverrideRequest ParseInterceptRegisterOverrides(string rawText)
    {
        uint? edi = null;
        uint? esi = null;
        uint? ebp = null;
        uint? ebx = null;
        uint? edx = null;
        uint? ecx = null;
        uint? eax = null;

        foreach (var segment in SplitSpec(rawText))
        {
            var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                throw new FormatException($"Invalid register override: {segment}");

            var value = ParseUInt32Token(parts[1]);
            switch (parts[0].ToLowerInvariant())
            {
                case "edi":
                    edi = value;
                    break;
                case "esi":
                    esi = value;
                    break;
                case "ebp":
                    ebp = value;
                    break;
                case "ebx":
                    ebx = value;
                    break;
                case "edx":
                    edx = value;
                    break;
                case "ecx":
                    ecx = value;
                    break;
                case "eax":
                    eax = value;
                    break;
                default:
                    throw new FormatException($"Unknown register override '{parts[0]}'.");
            }
        }

        return new InterceptRegisterOverrideRequest(edi, esi, ebp, ebx, edx, ecx, eax);
    }

    private static IReadOnlyList<InterceptArgumentOverrideRequest> ParseInterceptArgumentOverrides(string rawText) =>
        [
            .. SplitSpec(rawText)
                .Select(segment =>
                {
                    var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length != 2)
                        throw new FormatException($"Invalid argument override: {segment}");

                    if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                        throw new FormatException($"Invalid argument index: {parts[0]}");

                    return new InterceptArgumentOverrideRequest(index, ParseUInt32Token(parts[1]));
                }),
        ];

    private static IReadOnlyList<InterceptDereferenceRequest> ParseInterceptDereferences(string rawText) =>
        [
            .. SplitSpec(rawText)
                .Select(segment =>
                {
                    var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length != 2)
                        throw new FormatException($"Invalid dereference spec: {segment}");

                    var (sourceKind, index) = ParseInterceptDereferenceSource(parts[0]);
                    return new InterceptDereferenceRequest(
                        parts[0],
                        sourceKind,
                        index,
                        ParsePositiveInt(parts[1], "dereference byte count")
                    );
                }),
        ];

    private static (InterceptDereferenceSourceKind SourceKind, int Index) ParseInterceptDereferenceSource(string token)
    {
        var normalized = token.Trim().ToLowerInvariant();
        if (normalized.StartsWith("stack", StringComparison.Ordinal))
        {
            var indexToken = normalized["stack".Length..];
            return int.TryParse(indexToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stackIndex)
                ? (InterceptDereferenceSourceKind.StackIndex, stackIndex)
                : throw new FormatException($"Invalid stack dereference source: {token}");
        }

        return normalized switch
        {
            "eax" => (InterceptDereferenceSourceKind.Eax, -1),
            "ecx" => (InterceptDereferenceSourceKind.Ecx, -1),
            "edx" => (InterceptDereferenceSourceKind.Edx, -1),
            "ebx" => (InterceptDereferenceSourceKind.Ebx, -1),
            "esi" => (InterceptDereferenceSourceKind.Esi, -1),
            "edi" => (InterceptDereferenceSourceKind.Edi, -1),
            "ebp" => (InterceptDereferenceSourceKind.Ebp, -1),
            "esp" or "originalesp" => (InterceptDereferenceSourceKind.OriginalEsp, -1),
            _ => throw new FormatException($"Unknown dereference source: {token}"),
        };
    }

    private static IReadOnlyList<string> SplitSpec(string rawText) =>
        string.IsNullOrWhiteSpace(rawText)
            ? []
            : [.. rawText.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    private static string NormalizeProcessExecutableName(string processName)
    {
        var trimmed = processName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("Process executable name is required.");

        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? trimmed : trimmed + ".exe";
    }

    private static OfflineSaveDiagnosticsBundle CreateOfflineSaveDiagnostics(SaveSlotLoadSnapshot loaded)
    {
        var audit = SaveFileAuditService.Create(new SaveFileAuditRequest(loaded.Save));
        var characterCatalog = SaveCharacterCatalogService.Create(loaded.Save);
        var characterSarDump = SaveCharacterSarDumpService.Create(loaded.Save);
        var player = SavePlayerCharacterSummaryService.Create(loaded.Save);
        var questBook = SavePlayerQuestBookService.Create(loaded.Save);
        var typedContext = SaveTypedContextService.Create(loaded.Save);
        var typedOverview = SaveTypedContextAnalysisService.CreateOverview(
            typedContext.Player,
            typedContext.TownMapFogs
        );
        var structure = SaveStructureAnalysisService.Create(loaded.Save);
        var globals = SaveGlobalAnalysisService.CreateSlotSnapshot(loaded.Slot, loaded.SlotStem, loaded.Save);
        var embeddedFiles = AnalyzeEmbeddedFileDetails(loaded);
        var mobItemAnalysis = AnalyzeRepresentativeMobItem(loaded.Save);
        var gold = SaveGoldItemInspectionService.Create(loaded.Save);

        return new OfflineSaveDiagnosticsBundle(
            loaded,
            audit,
            characterCatalog,
            characterSarDump,
            player,
            questBook,
            typedOverview,
            structure,
            globals,
            embeddedFiles,
            mobItemAnalysis,
            gold
        );
    }

    private static OfflineRangeDiagnosticsBundle CreateOfflineRangeDiagnostics(
        string saveDir,
        int firstSlot,
        int lastSlot
    )
    {
        var loadedSlots = LoadSaveSlotRange(saveDir, firstSlot, lastSlot);
        if (loadedSlots.Count < 2)
            throw new InvalidOperationException("At least two loadable save slots are required for range diagnostics.");

        var progressionHistory = SavePlayerProgressionHistoryService.Create(saveDir, firstSlot, lastSlot);
        var sarHistory = PlayerSarHistoryService.Create(saveDir, firstSlot, lastSlot);
        var sarLifecycle = PlayerSarAnalysisService.CreateLifecycleAnalysis(sarHistory);
        var sarTransitionAnalysis = PlayerSarAnalysisService.CreateTransitionAnalysis(sarHistory);
        var sarLifecycleReport = PlayerSarReportService.CreateLifecycleReport(sarHistory, sarLifecycle);
        var sarTransitionReport = PlayerSarReportService.CreateTransitionReport(sarTransitionAnalysis);
        var globalSlots = loadedSlots
            .Select(static loaded =>
                SaveGlobalAnalysisService.CreateSlotSnapshot(loaded.Slot, loaded.SlotStem, loaded.Save)
            )
            .ToArray();
        var typedContextDelta = SaveTypedContextAnalysisService.CreateDelta(
            globalSlots[0].Player,
            globalSlots[0].TownMapFogs,
            globalSlots[^1].Player,
            globalSlots[^1].TownMapFogs
        );
        var globalRangeAnalysis = SaveGlobalRangeAnalysisService.Analyze(globalSlots);
        var globalDiffs = CreateGlobalDiffs(globalSlots);
        var globalDumps = CreateGlobalDumps(globalSlots[^1]);
        var binaryDiff = SaveBinaryDiffService.CompareInnerFiles(loadedSlots[0].Save.Files, loadedSlots[^1].Save.Files);
        var (binaryPreviewPath, binaryPreview) = CreateBinaryPreview(binaryDiff);
        var (
            characterSarDiffBeforeSlot,
            characterSarDiffAfterSlot,
            characterSarDiff,
            characterSarDumpSlot,
            characterSarFullDump
        ) = CreateCharacterSarDiagnostics(loadedSlots);

        return new OfflineRangeDiagnosticsBundle(
            firstSlot,
            lastSlot,
            loadedSlots,
            progressionHistory,
            sarHistory,
            sarLifecycle,
            sarTransitionAnalysis,
            sarLifecycleReport,
            sarTransitionReport,
            typedContextDelta,
            globalSlots,
            globalRangeAnalysis,
            globalDiffs,
            globalDumps,
            binaryDiff,
            binaryPreviewPath,
            binaryPreview,
            characterSarDiffBeforeSlot,
            characterSarDiffAfterSlot,
            characterSarDiff,
            characterSarDumpSlot,
            characterSarFullDump
        );
    }

    private static IReadOnlyList<SaveSlotLoadSnapshot> LoadSaveSlotRange(string saveDir, int firstSlot, int lastSlot)
    {
        List<SaveSlotLoadSnapshot> loadedSlots = [];
        for (var slot = firstSlot; slot <= lastSlot; slot++)
        {
            try
            {
                loadedSlots.Add(SaveSlotLoadService.Load(saveDir, slot));
            }
            catch (FileNotFoundException) { }
            catch (Exception) { }
        }

        return loadedSlots;
    }

    private static IReadOnlyDictionary<string, SaveGlobalFileDiffSnapshot> CreateGlobalDiffs(
        IReadOnlyList<SaveGlobalSlotSnapshot> globalSlots
    )
    {
        var first = globalSlots[0];
        var last = globalSlots[^1];
        Dictionary<string, SaveGlobalFileDiffSnapshot> diffs = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            var fileName in first
                .Files.Keys.Intersect(last.Files.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase)
        )
        {
            var before = first.Files[fileName];
            var after = last.Files[fileName];
            diffs[fileName] = SaveGlobalDiffService.Compare(in before, in after);
        }

        return diffs;
    }

    private static IReadOnlyDictionary<string, SaveGlobalFileDumpSnapshot> CreateGlobalDumps(
        SaveGlobalSlotSnapshot latest
    )
    {
        Dictionary<string, SaveGlobalFileDumpSnapshot> dumps = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            var (fileName, file) in latest.Files.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        )
            dumps[fileName] = SaveGlobalDumpService.Create(in file);

        return dumps;
    }

    private static (string? Path, SaveBinaryDiffPreviewSnapshot? Preview) CreateBinaryPreview(
        SaveBinaryDiffSetSnapshot binaryDiff
    )
    {
        var firstChangedFile = binaryDiff.Files.FirstOrDefault(static file => file.Regions.Count > 0);
        return firstChangedFile is null
            ? (null, null)
            : (firstChangedFile.Path, SaveBinaryDiffService.CreatePreview(firstChangedFile.Regions, maxRegions: 1));
    }

    private static (
        int? BeforeSlot,
        int? AfterSlot,
        CharacterSarDiffSnapshot? Diff,
        int? DumpSlot,
        CharacterSarFullDumpSnapshot? Dump
    ) CreateCharacterSarDiagnostics(IReadOnlyList<SaveSlotLoadSnapshot> loadedSlots)
    {
        var resolutions = loadedSlots
            .Select(static loaded => (loaded.Slot, Resolution: SavePlayerCharacterResolver.Resolve(loaded.Save)))
            .Where(static pair => pair.Resolution is not null)
            .Select(static pair => (pair.Slot, Resolution: pair.Resolution!))
            .ToList();

        if (resolutions.Count == 0)
            return (null, null, null, null, null);

        var dumpSlot = resolutions[^1].Slot;
        var dump = CharacterSarFullDumpService.Create(resolutions[^1].Resolution.Record.RawBytes);
        if (resolutions.Count == 1)
            return (resolutions[0].Slot, resolutions[0].Slot, null, dumpSlot, dump);

        var before = resolutions[0];
        var after = resolutions[^1];
        return (
            before.Slot,
            after.Slot,
            CharacterSarDiffService.Compare(before.Resolution.Record.RawBytes, after.Resolution.Record.RawBytes),
            dumpSlot,
            dump
        );
    }

    private static IReadOnlyList<SaveEmbeddedFileDetailSnapshot> AnalyzeEmbeddedFileDetails(
        SaveSlotLoadSnapshot loaded
    ) =>
        [
            .. loaded
                .Save.Files.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static entry =>
                    SaveEmbeddedFileAnalysisService.TryAnalyze(Path.GetFileName(entry.Key), entry.Value)
                )
                .OfType<SaveEmbeddedFileDetailSnapshot>(),
        ];

    private static (string Path, MobItemAnalysisSnapshot Analysis)? AnalyzeRepresentativeMobItem(LoadedSave save)
    {
        foreach (
            var (path, file) in save.MobileMdys.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            var mob = file
                .Records.Where(static record =>
                    record.IsMob && IsAnalyzableItemMobType(record.Mob!.Header.GameObjectType)
                )
                .Select(static record => record.Mob!)
                .FirstOrDefault();
            if (mob is not null)
                return (path, MobItemAnalysisService.Analyze(mob));
        }

        return null;
    }

    private static bool IsAnalyzableItemMobType(ArcNET.GameObjects.ObjectType objectType) =>
        objectType
            is ArcNET.GameObjects.ObjectType.Weapon
                or ArcNET.GameObjects.ObjectType.Armor
                or ArcNET.GameObjects.ObjectType.Gold
                or ArcNET.GameObjects.ObjectType.Food
                or ArcNET.GameObjects.ObjectType.Scroll
                or ArcNET.GameObjects.ObjectType.Ammo
                or ArcNET.GameObjects.ObjectType.Key
                or ArcNET.GameObjects.ObjectType.Written
                or ArcNET.GameObjects.ObjectType.Generic;

    private static string FormatSignedInt(int value) => value.ToString("+#;-#;0", CultureInfo.InvariantCulture);

    private static string FormatSlotStem(int slot) => $"Slot{slot:D4}";

    private sealed record OfflineSaveDiagnosticsBundle(
        SaveSlotLoadSnapshot Loaded,
        SaveFileAuditSnapshot Audit,
        SaveCharacterCatalogSnapshot CharacterCatalog,
        SaveCharacterSarDumpSnapshot CharacterSarDump,
        SavePlayerCharacterSummarySnapshot? Player,
        PlayerQuestBookSnapshot QuestBook,
        SaveTypedContextOverviewSnapshot TypedOverview,
        SaveStructureAnalysisSnapshot Structure,
        SaveGlobalSlotSnapshot Globals,
        IReadOnlyList<SaveEmbeddedFileDetailSnapshot> EmbeddedFiles,
        (string Path, MobItemAnalysisSnapshot Analysis)? MobItemAnalysis,
        SaveGoldItemInspectionSnapshot Gold
    );

    private sealed record OfflineRangeDiagnosticsBundle(
        int RequestedFirstSlot,
        int RequestedLastSlot,
        IReadOnlyList<SaveSlotLoadSnapshot> LoadedSlots,
        PlayerProgressionHistorySnapshot ProgressionHistory,
        PlayerSarHistorySnapshot SarHistory,
        PlayerSarLifecycleAnalysisSnapshot SarLifecycle,
        PlayerSarTransitionAnalysisSnapshot SarTransitionAnalysis,
        PlayerSarLifecycleReportSnapshot SarLifecycleReport,
        PlayerSarTransitionReportSnapshot SarTransitionReport,
        SaveTypedContextDeltaSnapshot TypedContextDelta,
        IReadOnlyList<SaveGlobalSlotSnapshot> GlobalSlots,
        SaveGlobalRangeAnalysisSnapshot GlobalRangeAnalysis,
        IReadOnlyDictionary<string, SaveGlobalFileDiffSnapshot> GlobalDiffs,
        IReadOnlyDictionary<string, SaveGlobalFileDumpSnapshot> GlobalDumps,
        SaveBinaryDiffSetSnapshot BinaryDiff,
        string? BinaryPreviewPath,
        SaveBinaryDiffPreviewSnapshot? BinaryPreview,
        int? CharacterSarDiffBeforeSlot,
        int? CharacterSarDiffAfterSlot,
        CharacterSarDiffSnapshot? CharacterSarDiff,
        int? CharacterSarDumpSlot,
        CharacterSarFullDumpSnapshot? CharacterSarFullDump
    );
}

public sealed record class DebuggerFeatureLaneGroup(
    string Title,
    string Summary,
    string ProgressText,
    IReadOnlyList<DebuggerFeatureCard> Features
);

public sealed record class DebuggerChoiceOption(string Token, string Label, string Description);

public sealed record class DebuggerLogbookSummaryCard(string Label, string Value, string Detail);

public sealed record class DebuggerLogbookSection(string Title, string Summary, IReadOnlyList<string> Entries);

public sealed record class DebuggerInsightCard(string Label, string Value, string Detail);

public sealed record class DebuggerInsightSection(string Title, string Summary, IReadOnlyList<string> Entries);

public sealed record class OfflineDumpRenderResult(
    string Output,
    string Summary,
    IReadOnlyList<DebuggerInsightCard> Highlights,
    IReadOnlyList<DebuggerInsightSection> Sections
);
