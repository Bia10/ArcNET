using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly LogbookEditorService _logbookEditorService;
    private IReadOnlyList<LogbookCatalogEntrySnapshot> _logbookCatalogCache = [];
    private string? _logbookCatalogModulePath;
    private bool _logbookCatalogLoadInFlight;
    private int _logbookLiveInspectionVersion;
    private string? _loadedLogbookHandleTokenText;
    private string? _loadedLogbookPageTokenText;
    private string? _loadedLogbookWorkspacePath;

    [ObservableProperty]
    private IReadOnlyList<DebuggerLogbookMutationOption> logbookMutationOptions =
    [
        new(
            LogbookMutationKind.SetQuestState,
            "Quest State",
            "Rewrite one PC quest state. ArcNET uses the native quest path when possible, then falls back to a direct PC-quest record overwrite for completed, botched, or backward edits.",
            "quest",
            "quests",
            RequiresEntry: true,
            ShowsValueSelector: true,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            "Quest state",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetQuestGlobalState,
            "Quest Global State",
            "Write the shared global state for one quest.",
            "quest",
            "quests",
            RequiresEntry: true,
            ShowsValueSelector: true,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            "Quest state",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetRumorKnown,
            "Mark Rumor Known",
            "Adds one rumor or note entry to the selected character journal.",
            "rumor",
            "rumors",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.QuellRumor,
            "Quell Rumor",
            "Marks one rumor as quelled in the global rumor state.",
            "rumor",
            "rumors",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.AddReputation,
            "Add Reputation",
            "Appends one reputation entry and timestamp to the selected character.",
            "reputation",
            "reputations",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.RemoveReputation,
            "Remove Reputation",
            "Removes one reputation entry from the selected character.",
            "reputation",
            "reputations",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.AddBlessing,
            "Add Blessing",
            "Adds one blessing and its timestamp through the native blessing path.",
            "blessing",
            "blessings",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.RemoveBlessing,
            "Remove Blessing",
            "Removes one blessing entry from the selected character.",
            "blessing",
            "blessings",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.AddCurse,
            "Add Curse",
            "Adds one curse and its timestamp through the native curse path.",
            "curse",
            "blessings",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.RemoveCurse,
            "Remove Curse",
            "Removes one curse entry from the selected character.",
            "curse",
            "blessings",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.AddKey,
            "Add Key",
            "Adds one key id to the selected character keyring and refreshes the keyring page art.",
            "key",
            "keys",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.RemoveKey,
            "Remove Key",
            "Removes one key id from the selected character keyring and refreshes the keyring page art.",
            "key",
            "keys",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
        new(
            LogbookMutationKind.AddInjury,
            "Add Injury History",
            "Appends one injury-history entry using a local source description plus an injury type selector.",
            "injury",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: true,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            "Injury type",
            string.Empty
        ),
        new(
            LogbookMutationKind.RemoveInjury,
            "Remove Injury History",
            "Removes one healed injury-history row by exact slot. Use one live shortcut from the current injury ledger so ArcNET can target the right row without guessing hidden indices.",
            "injury",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: true,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: true,
            "Injury type",
            "History slot from live shortcut"
        ),
        new(
            LogbookMutationKind.AddKill,
            "Record Kill",
            "Records one kill on the selected character logbook from one live victim handle without killing or despawning that victim.",
            "kill",
            "kills",
            RequiresEntry: false,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: true,
            string.Empty,
            "Victim handle or Use Roster"
        ),
        new(
            LogbookMutationKind.SetTotalKills,
            "Set Total Kills",
            "Writes the raw total-kill counter on the selected character logbook.",
            string.Empty,
            "kills",
            RequiresEntry: false,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Total kills",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetMostPowerfulKill,
            "Set Most Powerful Kill",
            "Writes the most-powerful kill row using one source description plus one level value.",
            "description",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Level",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetLeastPowerfulKill,
            "Set Least Powerful Kill",
            "Writes the least-powerful kill row using one source description plus one level value.",
            "description",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Level",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetMostGoodKill,
            "Set Most Good Kill",
            "Writes the most-good kill row using one source description plus one good-rating value.",
            "description",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Good rating",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetMostEvilKill,
            "Set Most Evil Kill",
            "Writes the most-evil kill row using one source description plus one evil-rating value.",
            "description",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Evil rating",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetMostMagicalKill,
            "Set Most Magical Kill",
            "Writes the most-magical kill row using one source description plus one magick-aptitude value.",
            "description",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Magick aptitude",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetMostTechKill,
            "Set Most Tech Kill",
            "Writes the most-tech kill row using one source description plus one tech-aptitude value.",
            "description",
            "kills",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: true,
            ShowsAuxiliaryInput: false,
            "Tech aptitude",
            string.Empty
        ),
        new(
            LogbookMutationKind.SetBackground,
            "Set Background",
            "Applies one background id plus its text id to the selected character.",
            "background",
            "background",
            RequiresEntry: true,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: true,
            string.Empty,
            "Background text id"
        ),
        new(
            LogbookMutationKind.ClearBackground,
            "Clear Background",
            "Clears the active background and background effect from the selected character.",
            "background",
            "background",
            RequiresEntry: false,
            ShowsValueSelector: false,
            ShowsValueInput: false,
            ShowsAuxiliaryInput: false,
            string.Empty,
            string.Empty
        ),
    ];

    [ObservableProperty]
    private DebuggerLogbookMutationOption? selectedLogbookMutationOption = new(
        LogbookMutationKind.SetQuestState,
        "Quest State",
        "Rewrite one PC quest state. ArcNET uses the native quest path when possible, then falls back to a direct PC-quest record overwrite for completed, botched, or backward edits.",
        "quest",
        "quests",
        RequiresEntry: true,
        ShowsValueSelector: true,
        ShowsValueInput: false,
        ShowsAuxiliaryInput: false,
        "Quest state",
        string.Empty
    );

    [ObservableProperty]
    private string logbookCatalogStatusText = "Journal catalog not loaded.";

    [ObservableProperty]
    private string logbookCatalogSummaryText =
        "Load the local journal and source catalog to browse quests, rumors, reputations, blessings, curses, keys, injury sources, and backgrounds without typing raw ids, or use one live victim handle for kill history.";

    [ObservableProperty]
    private string logbookEditableEntrySummaryText =
        "Load one journal page to turn the current live entries into editor-prefill shortcuts for player or a selected companion.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerLogbookEditableEntry> logbookEditableEntries = [];

    [ObservableProperty]
    private DebuggerLogbookEditableEntry? selectedLogbookEditableEntry;

    [ObservableProperty]
    private string logbookCatalogFilterText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<LogbookCatalogEntrySnapshot> logbookCatalogEntries = [];

    [ObservableProperty]
    private LogbookCatalogEntrySnapshot? selectedLogbookCatalogEntry;

    [ObservableProperty]
    private string logbookMutationEntryText = string.Empty;

    [ObservableProperty]
    private string logbookMutationValueText = string.Empty;

    [ObservableProperty]
    private string logbookMutationAuxiliaryText = string.Empty;

    [ObservableProperty]
    private string logbookMutationTimeoutText = "1000";

    [ObservableProperty]
    private string logbookMutationInputHintText =
        "Pick one operation, browse the matching local journal or source catalog, then apply it to player or a selected companion. Record Kill uses one live victim handle, while kill-summary writes can reuse one description source plus a numeric value.";

    [ObservableProperty]
    private bool logbookMutationShowLiveStateInspector = true;

    [ObservableProperty]
    private string logbookMutationLiveStateStatusText = "Live entry status";

    [ObservableProperty]
    private string logbookMutationLiveStateSummaryText =
        "Supported journal operations can inspect the current live state on the selected player or companion before you apply a mutation.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> logbookMutationValueOptions = [];

    [ObservableProperty]
    private DebuggerChoiceOption? selectedLogbookMutationValueOption;

    [ObservableProperty]
    private bool logbookMutationRequiresEntry;

    [ObservableProperty]
    private bool logbookMutationShowValueSelector;

    [ObservableProperty]
    private bool logbookMutationShowValueInput;

    [ObservableProperty]
    private bool logbookMutationShowAuxiliaryInput;

    [ObservableProperty]
    private bool logbookMutationShowCatalogBrowser = true;

    [ObservableProperty]
    private bool logbookMutationShowAuxiliaryRosterButton;

    [ObservableProperty]
    private string logbookMutationValuePlaceholderText = string.Empty;

    [ObservableProperty]
    private string logbookMutationAuxiliaryPlaceholderText = string.Empty;

    [ObservableProperty]
    private bool canLoadLogbookCatalog;

    [ObservableProperty]
    private bool canApplyLogbookMutation;

    [ObservableProperty]
    private string logbookMutationStatusText = "No live journal mutation executed.";

    [ObservableProperty]
    private IReadOnlyList<string> logbookMutationResultLines =
    [
        "Use the local journal and source catalog to pick quests, rumors, reputations, blessings, curses, keys, injury sources, kill-summary descriptions, or backgrounds, or use one live victim handle to record kill history.",
    ];

    [ObservableProperty]
    private string logbookMutationDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string logbookMutationExecutionDetailText =
        "Target address and hook details will appear here after a live journal mutation.";

    [ObservableProperty]
    private string logbookMutationResultText = "Mutation result values will appear here after a live journal mutation.";

    partial void OnLogbookCatalogFilterTextChanged(string value) => ApplyFilteredLogbookCatalog();

    partial void OnLogbookHandleTokenTextChanged(string value)
    {
        InvalidateLoadedLogbookSnapshotIfRequestChanged(handleTokenText: value);
        QueueRefreshLogbookLiveInspection();
        RefreshLogbookEditorActions();
    }

    partial void OnLogbookMutationEntryTextChanged(string value)
    {
        QueueRefreshLogbookLiveInspection();
        RefreshLogbookEditorActions();
    }

    partial void OnLogbookMutationValueTextChanged(string value) => RefreshLogbookEditorActions();

    partial void OnLogbookMutationAuxiliaryTextChanged(string value) => RefreshLogbookEditorActions();

    partial void OnLogbookMutationTimeoutTextChanged(string value) => RefreshLogbookEditorActions();

    partial void OnSelectedLogbookCatalogEntryChanged(LogbookCatalogEntrySnapshot? value)
    {
        if (value is null)
        {
            RefreshLogbookEditorActions();
            return;
        }

        if (
            !LogbookMutationEntryText.Equals(
                value.EntryId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal
            )
        )
            LogbookMutationEntryText = value.EntryId.ToString(CultureInfo.InvariantCulture);

        if (SelectedLogbookMutationOption?.Kind == LogbookMutationKind.SetBackground)
        {
            var textId = value.AuxiliaryId.ToString(CultureInfo.InvariantCulture);
            if (!LogbookMutationAuxiliaryText.Equals(textId, StringComparison.Ordinal))
                LogbookMutationAuxiliaryText = textId;
        }

        LogbookMutationInputHintText =
            $"{value.DisplayName} [{value.EntryId.ToString(CultureInfo.InvariantCulture)}] selected from the local journal and source catalog.";
        QueueRefreshLogbookLiveInspection();
        RefreshLogbookEditorActions();
    }

    partial void OnSelectedLogbookEditableEntryChanged(DebuggerLogbookEditableEntry? value)
    {
        if (value is null)
        {
            RefreshLogbookEditorActions();
            return;
        }

        var option = LogbookMutationOptions.FirstOrDefault(option => option.Kind == value.MutationKind);
        if (option is not null && SelectedLogbookMutationOption?.Kind != option.Kind)
            SelectedLogbookMutationOption = option;

        if (!LogbookMutationEntryText.Equals(value.EntryText, StringComparison.Ordinal))
            LogbookMutationEntryText = value.EntryText;

        if (!LogbookMutationValueText.Equals(value.ValueText, StringComparison.OrdinalIgnoreCase))
            LogbookMutationValueText = value.ValueText;

        if (!LogbookMutationAuxiliaryText.Equals(value.AuxiliaryText, StringComparison.Ordinal))
            LogbookMutationAuxiliaryText = value.AuxiliaryText;

        TrySelectLogbookCatalogEntry(value.CatalogCategoryToken, value.EntryText);
        LogbookMutationInputHintText = value.SuggestedOperationText;
        QueueRefreshLogbookLiveInspection();
        RefreshLogbookEditorActions();
    }

    partial void OnSelectedLogbookMutationValueOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is null)
        {
            RefreshLogbookEditorActions();
            return;
        }

        if (!LogbookMutationValueText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            LogbookMutationValueText = value.Token;

        RefreshLogbookEditorActions();
    }

    partial void OnSelectedLogbookMutationOptionChanged(DebuggerLogbookMutationOption? value)
    {
        ApplyLogbookMutationOption(value);
        ApplyFilteredLogbookCatalog();
        QueueRefreshLogbookLiveInspection();
        RefreshLogbookEditorActions();
    }

    [RelayCommand]
    private async Task LoadLogbookCatalog()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantLogbookEditorState(
                "Journal catalog unavailable",
                "Attach to a live runtime before loading the local journal and source catalog."
            );
            return;
        }

        if (_logbookCatalogLoadInFlight)
            return;

        _logbookCatalogLoadInFlight = true;
        RefreshLogbookEditorActions();
        try
        {
            var workspacePathOverride = ResolveWorkspacePathOverride();
            var requestedWorkspacePath = ResolveEffectiveWorkspacePath(session);
            if (!string.IsNullOrWhiteSpace(requestedWorkspacePath))
                _ = await WorkspaceTextCatalog.LoadFromModulePathAsync(requestedWorkspacePath, forceReload: true);

            var snapshot = await _logbookEditorService.LoadCatalogAsync(
                new LogbookEditorCatalogRequest(session, workspacePathOverride)
            );
            if (!snapshot.IsAvailable)
            {
                ApplyDormantLogbookEditorState(snapshot.Status, snapshot.Summary);
                return;
            }

            _logbookCatalogModulePath = requestedWorkspacePath;
            _logbookCatalogCache = snapshot.Entries;
            LogbookCatalogStatusText = snapshot.Status;
            ApplyFilteredLogbookCatalog();
        }
        catch (Exception ex)
        {
            ApplyDormantLogbookEditorState(
                "Journal catalog failed",
                $"Unable to load the local journal and source catalog ({ex.GetType().Name}: {ex.Message})."
            );
        }
        finally
        {
            _logbookCatalogLoadInFlight = false;
            RefreshLogbookEditorActions();
        }
    }

    [RelayCommand]
    private void UseSelectedMobileForLogbookAuxiliary()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        LogbookMutationAuxiliaryText = entry.HandleHex;
    }

    [RelayCommand]
    private async Task ApplyLogbookMutation()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantLogbookMutation(
                "No active session",
                ["Attach to a validated runtime before editing the live journal."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantLogbookMutation(
                "Logbook editor unavailable",
                [CreateLogbookEditorAvailabilitySummary(session)]
            );
            return;
        }

        if (SelectedLogbookMutationOption is not { } operation)
        {
            ApplyDormantLogbookMutation(
                "Journal operation required",
                ["Pick one journal operation before applying a live mutation."]
            );
            return;
        }

        try
        {
            var snapshot = await _logbookEditorService.WriteAsync(
                new LogbookMutationRequest(
                    session,
                    LogbookHandleTokenText,
                    operation.Kind,
                    LogbookMutationEntryText,
                    LogbookMutationValueText,
                    LogbookMutationAuxiliaryText,
                    LogbookMutationTimeoutText,
                    ResolveWorkspacePathOverride()
                )
            );
            ApplyLogbookMutationSnapshot(snapshot);
            if (snapshot.IsAvailable)
            {
                if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
                {
                    ObjectProbeHandleText = snapshot.TargetHandleText;
                    await RefreshLogbookAfterMutation(session);
                }

                QueueRefreshLogbookLiveInspection();
            }
        }
        catch (Exception ex)
        {
            ApplyDormantLogbookMutation("Journal mutation failed", [ex.Message]);
        }
    }

    private async Task RefreshLogbookAfterMutation(AttachedSessionSnapshot session)
    {
        if (SelectedLogbookMutationOption is not { } option)
            return;

        var page = LogbookPageOptions.FirstOrDefault(page =>
            page.Token.Equals(option.PageToken, StringComparison.OrdinalIgnoreCase)
        );
        if (page is not null && SelectedLogbookPageOption?.Token != page.Token)
            SelectedLogbookPageOption = page;

        var snapshot = await _logbookService.ReadAsync(
            new LogbookRequest(session, LogbookHandleTokenText, LogbookPageTokenText, ResolveWorkspacePathOverride())
        );
        ApplyLogbookReadSnapshot(snapshot);
    }

    private void ApplyLogbookEditableEntries(LogbookSnapshot snapshot)
    {
        var selectedEntryKey = SelectedLogbookEditableEntry?.EntryKey;
        LogbookEditableEntries = LogbookLiveEntryCatalog.BuildEntries(snapshot);
        SelectedLogbookEditableEntry = selectedEntryKey is null
            ? null
            : LogbookEditableEntries.FirstOrDefault(entry =>
                entry.EntryKey.Equals(selectedEntryKey, StringComparison.OrdinalIgnoreCase)
            );
        LogbookEditableEntrySummaryText =
            LogbookEditableEntries.Count == 0
                ? $"No directly prefillable live editor shortcuts were found on this {ResolveLogbookPageLabel(snapshot.RequestedPageToken).ToLowerInvariant()} read. Use the local catalog below for add-style journal edits."
                : $"Showing {LogbookEditableEntries.Count.ToString(CultureInfo.InvariantCulture)} live editor shortcut(s) for {snapshot.TargetText}. Selecting one row prefills the editor below.";
    }

    private void InvalidateLoadedLogbookSnapshotIfRequestChanged(
        string? handleTokenText = null,
        string? pageTokenText = null
    )
    {
        var currentWorkspacePath = NormalizeWorkspacePathKey(
            ActiveSession is { } activeSession
                ? ResolveEffectiveWorkspacePath(activeSession)
                : ResolveWorkspacePathOverride()
        );
        var workspaceChanged =
            NormalizeWorkspacePathKey(_loadedLogbookWorkspacePath).Length != 0
            && !NormalizeWorkspacePathKey(_loadedLogbookWorkspacePath)
                .Equals(currentWorkspacePath, StringComparison.OrdinalIgnoreCase);
        if (
            !LogbookLoadedSnapshotStateCatalog.TryCreateInvalidation(
                _loadedLogbookHandleTokenText,
                _loadedLogbookPageTokenText,
                handleTokenText ?? LogbookHandleTokenText,
                pageTokenText ?? LogbookPageTokenText,
                out var invalidation
            ) && !workspaceChanged
        )
        {
            return;
        }

        if (workspaceChanged)
        {
            invalidation = invalidation is null
                ? new(
                    "The loaded journal view belonged to a different local workspace. Read the selected journal page again for the current workspace.",
                    "The loaded live editor shortcuts belonged to a different local workspace. Read the selected journal page again to rebuild shortcuts for the current workspace."
                )
                : invalidation with
                {
                    DisplaySummaryText = $"{invalidation.DisplaySummaryText} The local workspace path also changed.",
                    EditorSummaryText = $"{invalidation.EditorSummaryText} The local workspace path also changed.",
                };
        }

        ResetLoadedLogbookSnapshotState();
        LogbookStatusText = "Journal view needs reload";
        LogbookDisplaySummaryText = invalidation.DisplaySummaryText;
        LogbookResultLines = [];
        LogbookHighlights = [];
        LogbookSections = [];
        ClearLogbookEditableEntries(invalidation.EditorSummaryText);
        LogbookNotes = [];
        HasLogbookSections = false;
        ShowLogbookFallbackLines = false;
    }

    private void ClearLogbookEditableEntries(string summary)
    {
        LogbookEditableEntries = [];
        SelectedLogbookEditableEntry = null;
        LogbookEditableEntrySummaryText = summary;
    }

    private void ResetLoadedLogbookSnapshotState()
    {
        _loadedLogbookHandleTokenText = null;
        _loadedLogbookPageTokenText = null;
        _loadedLogbookWorkspacePath = null;
    }

    private void QueueRefreshLogbookLiveInspection() => _ = RefreshLogbookLiveInspectionAsync();

    private async Task RefreshLogbookLiveInspectionAsync()
    {
        var requestVersion = ++_logbookLiveInspectionVersion;
        var context = TryCreateLogbookLiveInspectionContext(out var statusText, out var summaryText);
        if (context is null)
        {
            if (requestVersion == _logbookLiveInspectionVersion)
                ApplyLogbookLiveInspectionStatus(statusText, summaryText);

            return;
        }

        LogbookMutationShowLiveStateInspector = true;
        LogbookMutationLiveStateStatusText = context.Value.PendingStatusText;
        LogbookMutationLiveStateSummaryText = context.Value.PendingSummaryText;

        try
        {
            DebuggerLogbookLiveStatus liveStatus;
            if (context.Value.ReadRequest is { } readRequest)
            {
                var snapshot = await Task.Run(() => _readService.Read(readRequest));
                if (requestVersion != _logbookLiveInspectionVersion)
                    return;

                if (!snapshot.IsAvailable)
                {
                    ApplyLogbookLiveInspectionStatus(snapshot.Status, snapshot.Summary);
                    return;
                }

                liveStatus = LogbookLiveStatusCatalog.DescribeRead(
                    context.Value.Kind,
                    context.Value.DisplayName,
                    snapshot
                );
            }
            else if (context.Value.LogbookRequest is { } logbookRequest)
            {
                var snapshot = await _logbookService.ReadAsync(logbookRequest);
                if (requestVersion != _logbookLiveInspectionVersion)
                    return;

                if (!snapshot.IsAvailable)
                {
                    ApplyLogbookLiveInspectionStatus(snapshot.Status, snapshot.Summary);
                    return;
                }

                liveStatus = LogbookLiveStatusCatalog.DescribeLogbook(
                    context.Value.Kind,
                    context.Value.DisplayName,
                    context.Value.EntryId,
                    context.Value.AuxiliaryId,
                    context.Value.ValueTokenText,
                    snapshot
                );
            }
            else
            {
                ApplyLogbookLiveInspectionStatus(
                    "Live inspection unavailable",
                    "No live inspection request was prepared."
                );
                return;
            }

            if (requestVersion != _logbookLiveInspectionVersion)
                return;

            ApplyLogbookLiveStatus(liveStatus);
        }
        catch (Exception ex)
        {
            if (requestVersion == _logbookLiveInspectionVersion)
                ApplyLogbookLiveInspectionStatus("Live inspection failed", ex.Message);
        }
    }

    private LogbookLiveInspectionContext? TryCreateLogbookLiveInspectionContext(
        out string statusText,
        out string summaryText
    )
    {
        var operation = SelectedLogbookMutationOption?.Kind;
        if (operation is null || !LogbookLiveStatusCatalog.Supports(operation.Value))
        {
            statusText = "Live entry status";
            summaryText =
                "Supported journal operations can inspect the current live state on the selected player or companion before you apply a mutation.";
            return null;
        }

        if (ActiveSession is not { } session)
        {
            statusText = "No active session";
            summaryText = "Attach to a live runtime before inspecting the current journal state.";
            return null;
        }

        if (!CanInvokeFunctions(session))
        {
            statusText = "Live inspection unavailable";
            summaryText = CreateLogbookEditorAvailabilitySummary(session);
            return null;
        }

        var displayName = ResolveLogbookLiveInspectionDisplayName(operation.Value);
        var auxiliaryId = ResolveLogbookLiveInspectionAuxiliaryId();

        switch (operation.Value)
        {
            case LogbookMutationKind.SetQuestState:
            case LogbookMutationKind.SetRumorKnown:
                if (string.IsNullOrWhiteSpace(LogbookHandleTokenText))
                {
                    statusText = "Journal target required";
                    summaryText =
                        "Provide a player or companion handle before inspecting the current state for this entry.";
                    return null;
                }

                goto case LogbookMutationKind.SetQuestGlobalState;

            case LogbookMutationKind.SetQuestGlobalState:
            case LogbookMutationKind.QuellRumor:
                if (
                    !int.TryParse(
                        LogbookMutationEntryText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var readEntryId
                    )
                )
                {
                    statusText = "Entry selection required";
                    summaryText =
                        "Pick one catalog row or enter one entry id to inspect the current live state before applying a mutation.";
                    return null;
                }

                string[] readArguments = operation.Value switch
                {
                    LogbookMutationKind.SetQuestState =>
                    [
                        LogbookHandleTokenText,
                        readEntryId.ToString(CultureInfo.InvariantCulture),
                    ],
                    LogbookMutationKind.SetQuestGlobalState => [readEntryId.ToString(CultureInfo.InvariantCulture)],
                    LogbookMutationKind.SetRumorKnown =>
                    [
                        LogbookHandleTokenText,
                        readEntryId.ToString(CultureInfo.InvariantCulture),
                    ],
                    LogbookMutationKind.QuellRumor => [readEntryId.ToString(CultureInfo.InvariantCulture)],
                    _ => [],
                };
                statusText = string.Empty;
                summaryText = string.Empty;
                return new LogbookLiveInspectionContext(
                    operation.Value,
                    displayName,
                    readEntryId,
                    auxiliaryId,
                    LogbookMutationValueText,
                    operation.Value switch
                    {
                        LogbookMutationKind.SetQuestState => "Reading current PC quest state...",
                        LogbookMutationKind.SetQuestGlobalState => "Reading current global quest state...",
                        LogbookMutationKind.SetRumorKnown => "Reading current rumor-known flag...",
                        LogbookMutationKind.QuellRumor => "Reading current rumor-quelled flag...",
                        _ => "Reading live state...",
                    },
                    "ArcNET is reading the current live journal state so the editor can explain what will change before you apply it.",
                    new ReadRequest(
                        session,
                        operation.Value switch
                        {
                            LogbookMutationKind.SetQuestState => "quest",
                            LogbookMutationKind.SetQuestGlobalState => "questglobal",
                            LogbookMutationKind.SetRumorKnown => "rumorknown",
                            LogbookMutationKind.QuellRumor => "rumorquelled",
                            _ => string.Empty,
                        },
                        readArguments
                    ),
                    null
                );

            default:
                if (string.IsNullOrWhiteSpace(LogbookHandleTokenText))
                {
                    statusText = "Journal target required";
                    summaryText =
                        "Provide a player or companion handle before inspecting the current journal state for this mutation.";
                    return null;
                }

                var requiresEntrySelection =
                    operation.Value
                    is not LogbookMutationKind.AddKill
                        and not LogbookMutationKind.SetTotalKills
                        and not LogbookMutationKind.ClearBackground
                        and not LogbookMutationKind.SetBackground;
                var pageEntryId = 0;
                if (
                    requiresEntrySelection
                    && !int.TryParse(
                        LogbookMutationEntryText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out pageEntryId
                    )
                )
                {
                    statusText = "Entry selection required";
                    summaryText =
                        "Pick one catalog row or enter one entry id to inspect the current journal state before applying a mutation.";
                    return null;
                }

                if (
                    operation.Value == LogbookMutationKind.SetBackground
                    && int.TryParse(
                        LogbookMutationEntryText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var selectedBackgroundId
                    )
                )
                {
                    pageEntryId = selectedBackgroundId;
                }

                if (
                    operation.Value is LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury
                    && string.IsNullOrWhiteSpace(LogbookMutationValueText)
                )
                {
                    statusText = "Injury type required";
                    summaryText =
                        operation.Value == LogbookMutationKind.AddInjury
                            ? "Choose an injury type before comparing this source description against the current active injury list."
                            : "Choose an injury type before comparing this history row against the current injury ledger.";
                    return null;
                }

                if (
                    operation.Value == LogbookMutationKind.AddKill
                    && string.IsNullOrWhiteSpace(LogbookMutationAuxiliaryText)
                )
                {
                    statusText = "Victim handle required";
                    summaryText =
                        "Use one live victim handle or Use Roster before inspecting how Record Kill will affect the current kill ledger.";
                    return null;
                }

                if (
                    operation.Value == LogbookMutationKind.RemoveInjury
                    && !int.TryParse(
                        LogbookMutationAuxiliaryText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out _
                    )
                )
                {
                    statusText = "History slot required";
                    summaryText = "Use one live injury shortcut so ArcNET can target the exact healed slot to remove.";
                    return null;
                }

                statusText = string.Empty;
                summaryText = string.Empty;
                return new LogbookLiveInspectionContext(
                    operation.Value,
                    displayName,
                    requiresEntrySelection ? pageEntryId : 0,
                    auxiliaryId,
                    LogbookMutationValueText,
                    operation.Value switch
                    {
                        LogbookMutationKind.AddKill => "Reading current kill ledger...",
                        LogbookMutationKind.SetTotalKills
                        or LogbookMutationKind.SetMostPowerfulKill
                        or LogbookMutationKind.SetLeastPowerfulKill
                        or LogbookMutationKind.SetMostGoodKill
                        or LogbookMutationKind.SetMostEvilKill
                        or LogbookMutationKind.SetMostMagicalKill
                        or LogbookMutationKind.SetMostTechKill => "Reading current kill ledger summary...",
                        LogbookMutationKind.AddReputation or LogbookMutationKind.RemoveReputation =>
                            "Reading current reputation page...",
                        LogbookMutationKind.AddBlessing
                        or LogbookMutationKind.RemoveBlessing
                        or LogbookMutationKind.AddCurse
                        or LogbookMutationKind.RemoveCurse => "Reading current blessing-and-curse page...",
                        LogbookMutationKind.AddKey or LogbookMutationKind.RemoveKey =>
                            "Reading current keyring page...",
                        LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury =>
                            "Reading current injury history...",
                        LogbookMutationKind.SetBackground or LogbookMutationKind.ClearBackground =>
                            "Reading current background page...",
                        _ => "Reading current journal page...",
                    },
                    "ArcNET is reading the current live journal page so the editor can explain what this mutation will change.",
                    null,
                    new LogbookRequest(
                        session,
                        LogbookHandleTokenText,
                        SelectedLogbookMutationOption!.PageToken,
                        ResolveWorkspacePathOverride()
                    )
                );
        }
    }

    private void ApplyLogbookLiveInspectionStatus(string statusText, string summaryText)
    {
        LogbookMutationShowLiveStateInspector =
            SelectedLogbookMutationOption is { } operation && LogbookLiveStatusCatalog.Supports(operation.Kind);
        LogbookMutationLiveStateStatusText = statusText;
        LogbookMutationLiveStateSummaryText = summaryText;
    }

    private void ApplyLogbookLiveStatus(DebuggerLogbookLiveStatus status)
    {
        LogbookMutationLiveStateStatusText = status.StatusText;
        LogbookMutationLiveStateSummaryText = status.SummaryText;
        ApplyQuestStateMutationValueToken(status.PrefillValueToken ?? string.Empty);
    }

    private void ApplyQuestStateMutationValueToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        var matchingOption = LogbookMutationValueOptions.FirstOrDefault(option =>
            option.Token.Equals(token, StringComparison.OrdinalIgnoreCase)
        );
        if (matchingOption is not null && SelectedLogbookMutationValueOption?.Token != matchingOption.Token)
            SelectedLogbookMutationValueOption = matchingOption;

        if (!LogbookMutationValueText.Equals(token, StringComparison.OrdinalIgnoreCase))
            LogbookMutationValueText = token;
    }

    private string ResolveLogbookLiveInspectionDisplayName(LogbookMutationKind kind)
    {
        if (kind == LogbookMutationKind.AddKill)
            return ResolveKillVictimDisplayName();

        var selectedDisplayName = SelectedLogbookCatalogEntry?.DisplayName ?? SelectedLogbookEditableEntry?.DisplayName;
        if (!string.IsNullOrWhiteSpace(selectedDisplayName))
            return selectedDisplayName;

        if (
            int.TryParse(LogbookMutationEntryText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryId)
            && entryId >= 0
        )
        {
            return kind switch
            {
                LogbookMutationKind.SetQuestState or LogbookMutationKind.SetQuestGlobalState =>
                    $"Quest {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.SetRumorKnown or LogbookMutationKind.QuellRumor =>
                    $"Rumor {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.AddReputation or LogbookMutationKind.RemoveReputation =>
                    $"Reputation {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.AddBlessing or LogbookMutationKind.RemoveBlessing =>
                    $"Blessing {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.AddCurse or LogbookMutationKind.RemoveCurse =>
                    $"Curse {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.AddKey or LogbookMutationKind.RemoveKey =>
                    $"Key {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury =>
                    $"Description {entryId.ToString(CultureInfo.InvariantCulture)}",
                LogbookMutationKind.SetMostPowerfulKill
                or LogbookMutationKind.SetLeastPowerfulKill
                or LogbookMutationKind.SetMostGoodKill
                or LogbookMutationKind.SetMostEvilKill
                or LogbookMutationKind.SetMostMagicalKill
                or LogbookMutationKind.SetMostTechKill => entryId > 0
                    ? $"Description {entryId.ToString(CultureInfo.InvariantCulture)}"
                    : "No description",
                LogbookMutationKind.SetTotalKills => "Total Kills",
                LogbookMutationKind.SetBackground => $"Background {entryId.ToString(CultureInfo.InvariantCulture)}",
                _ => $"{kind} {entryId.ToString(CultureInfo.InvariantCulture)}",
            };
        }

        return kind == LogbookMutationKind.ClearBackground ? "Current background"
            : kind == LogbookMutationKind.SetTotalKills ? "Total Kills"
            : SelectedLogbookMutationOption?.Label ?? "Selected entry";
    }

    private string ResolveKillVictimDisplayName()
    {
        var token = LogbookMutationAuxiliaryText.Trim();
        if (token.Length == 0)
            return "the selected victim";

        var rosterMatch =
            SelectedMobileRosterEntry is { } selectedEntry
            && selectedEntry.HandleHex.Equals(token, StringComparison.OrdinalIgnoreCase)
                ? selectedEntry
                : _mobileRosterCache.FirstOrDefault(entry =>
                    entry.HandleHex.Equals(token, StringComparison.OrdinalIgnoreCase)
                );
        if (rosterMatch is not null)
            return $"{rosterMatch.DisplayText} [{rosterMatch.HandleHex}]";

        return $"victim {token}";
    }

    private int ResolveLogbookLiveInspectionAuxiliaryId()
    {
        if (
            int.TryParse(
                LogbookMutationAuxiliaryText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var auxiliaryId
            )
            && auxiliaryId > 0
        )
        {
            return auxiliaryId;
        }

        return SelectedLogbookCatalogEntry?.AuxiliaryId ?? 0;
    }

    private void ApplyLogbookMutationOption(DebuggerLogbookMutationOption? value)
    {
        if (value is null)
            return;

        LogbookMutationRequiresEntry = value.RequiresEntry;
        LogbookMutationShowValueSelector = value.ShowsValueSelector;
        LogbookMutationShowValueInput = value.ShowsValueInput;
        LogbookMutationShowAuxiliaryInput = value.ShowsAuxiliaryInput;
        LogbookMutationShowCatalogBrowser = value.RequiresEntry && value.Kind != LogbookMutationKind.RemoveInjury;
        LogbookMutationShowAuxiliaryRosterButton = value.Kind == LogbookMutationKind.AddKill;
        LogbookMutationShowLiveStateInspector = LogbookLiveStatusCatalog.Supports(value.Kind);
        LogbookMutationValuePlaceholderText = value.ValuePlaceholderText;
        LogbookMutationAuxiliaryPlaceholderText = value.AuxiliaryPlaceholderText;
        LogbookMutationInputHintText = value.Description;
        if (value.Kind == LogbookMutationKind.AddKill)
            LogbookMutationAuxiliaryText = string.Empty;

        if (!LogbookMutationShowLiveStateInspector)
        {
            LogbookMutationLiveStateStatusText = "Live entry status";
            LogbookMutationLiveStateSummaryText =
                "Supported journal operations can inspect the current live state on the selected player or companion before you apply a mutation.";
        }

        LogbookMutationValueOptions = value.Kind switch
        {
            LogbookMutationKind.SetQuestState => s_questPcStateOptions,
            LogbookMutationKind.SetQuestGlobalState => s_questGlobalStateOptions,
            LogbookMutationKind.AddInjury or LogbookMutationKind.RemoveInjury => s_injuryTypeOptions,
            _ => [],
        };
        SelectedLogbookMutationValueOption = LogbookMutationValueOptions.FirstOrDefault(option =>
            option.Token.Equals(LogbookMutationValueText, StringComparison.OrdinalIgnoreCase)
        );
        if (LogbookMutationShowValueSelector && SelectedLogbookMutationValueOption is null)
        {
            SelectedLogbookMutationValueOption = LogbookMutationValueOptions.FirstOrDefault();
            if (SelectedLogbookMutationValueOption is not null)
                LogbookMutationValueText = SelectedLogbookMutationValueOption.Token;
        }

        var page = LogbookPageOptions.FirstOrDefault(optionPage =>
            optionPage.Token.Equals(value.PageToken, StringComparison.OrdinalIgnoreCase)
        );
        if (page is not null && SelectedLogbookPageOption?.Token != page.Token)
            SelectedLogbookPageOption = page;
    }

    private void ApplyFilteredLogbookCatalog()
    {
        if (!LogbookMutationShowCatalogBrowser)
        {
            LogbookCatalogEntries = [];
            SelectedLogbookCatalogEntry = null;
            LogbookCatalogSummaryText =
                SelectedLogbookMutationOption?.Kind == LogbookMutationKind.AddKill
                    ? "Record Kill uses one live victim handle instead of a local catalog id. Use Roster beside the victim field to copy any currently loaded mobile."
                : SelectedLogbookMutationOption?.Kind == LogbookMutationKind.SetTotalKills
                    ? "Set Total Kills uses the live kills page plus one numeric count instead of a local catalog row."
                : SelectedLogbookMutationOption?.Kind == LogbookMutationKind.RemoveInjury
                    ? "Remove Injury History uses one live injury shortcut instead of a local catalog row so ArcNET can target the exact healed slot safely."
                : $"{SelectedLogbookMutationOption?.Label ?? "This journal operation"} does not need a local catalog row.";
            RefreshLogbookEditorActions();
            return;
        }

        if (_logbookCatalogCache.Count == 0)
        {
            LogbookCatalogEntries = [];
            RefreshLogbookEditorActions();
            return;
        }

        var selectedCategory = SelectedLogbookMutationOption?.CategoryToken ?? string.Empty;
        var selectedEntry = SelectedLogbookCatalogEntry;
        var normalizedFilter = NormalizeLogbookCatalogFilter(LogbookCatalogFilterText);
        LogbookCatalogEntries =
        [
            .. _logbookCatalogCache.Where(entry =>
                (
                    selectedCategory.Length == 0
                    || entry.CategoryToken.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)
                ) && MatchesLogbookCatalogFilter(entry, normalizedFilter)
            ),
        ];
        SelectedLogbookCatalogEntry = selectedEntry is null
            ? LogbookCatalogEntries.FirstOrDefault()
            : LogbookCatalogEntries.FirstOrDefault(entry =>
                entry.CategoryToken.Equals(selectedEntry.CategoryToken, StringComparison.OrdinalIgnoreCase)
                && entry.EntryId == selectedEntry.EntryId
            ) ?? LogbookCatalogEntries.FirstOrDefault();

        var categoryLabel = SelectedLogbookMutationOption?.Label ?? "journal";
        LogbookCatalogSummaryText =
            $"Showing {LogbookCatalogEntries.Count.ToString(CultureInfo.InvariantCulture)} local catalog rows for {categoryLabel}. Selecting one row prefills the editor inputs.";
        RefreshLogbookEditorActions();
    }

    private void ApplySessionLogbookEditorState(AttachedSessionSnapshot snapshot)
    {
        var workspacePath = ResolveEffectiveWorkspacePath(snapshot);
        if (!string.Equals(_logbookCatalogModulePath, workspacePath, StringComparison.OrdinalIgnoreCase))
        {
            _logbookCatalogModulePath = null;
            _logbookCatalogCache = [];
            LogbookCatalogEntries = [];
            SelectedLogbookCatalogEntry = null;
            LogbookCatalogStatusText = "Journal catalog not loaded.";
            LogbookCatalogSummaryText =
                "Load the local journal and source catalog to browse quests, rumors, reputations, blessings, curses, keys, injury sources, and backgrounds without typing raw ids, or use one live victim handle for kill history.";
        }
        else if (_logbookCatalogCache.Count > 0)
        {
            LogbookCatalogStatusText = "Journal catalog loaded.";
            ApplyFilteredLogbookCatalog();
        }

        if (!CanInvokeFunctions(snapshot))
        {
            ApplyDormantLogbookMutation(
                "Logbook editor unavailable",
                [CreateLogbookEditorAvailabilitySummary(snapshot)]
            );
        }

        QueueRefreshLogbookLiveInspection();
        RefreshLogbookEditorActions();
    }

    private void ApplyDormantLogbookEditorState(string status, string summary)
    {
        ResetLoadedLogbookSnapshotState();
        _logbookCatalogModulePath = null;
        _logbookCatalogCache = [];
        LogbookCatalogEntries = [];
        SelectedLogbookCatalogEntry = null;
        LogbookCatalogStatusText = status;
        LogbookCatalogSummaryText = summary;
        LogbookMutationLiveStateStatusText = "No active session";
        LogbookMutationLiveStateSummaryText =
            "Attach to a live runtime before inspecting current journal state from the editor.";
        RefreshLogbookEditorActions();
    }

    private void ApplyDormantLogbookMutation(string status, IReadOnlyList<string> lines)
    {
        LogbookMutationStatusText = status;
        LogbookMutationDispatcherText = "Dispatcher result unavailable.";
        LogbookMutationExecutionDetailText =
            "Target address and hook details will appear here after a live journal mutation.";
        LogbookMutationResultText = "Mutation result values will appear here after a live journal mutation.";
        LogbookMutationResultLines = lines;
        RefreshLogbookEditorActions();
    }

    private void ApplyLogbookMutationSnapshot(LogbookMutationSnapshot snapshot)
    {
        LogbookMutationStatusText = snapshot.Status;
        LogbookMutationDispatcherText = snapshot.DispatcherText;
        LogbookMutationExecutionDetailText = snapshot.ExecutionDetailText;
        LogbookMutationResultText = snapshot.ResultText;
        LogbookMutationResultLines = CreateLogbookMutationLines(snapshot);
        RefreshLogbookEditorActions();
    }

    private void RefreshLogbookEditorActions()
    {
        CanLoadLogbookCatalog =
            ActiveSession is not null && !_logbookCatalogLoadInFlight && LogbookMutationShowCatalogBrowser;
        var hasWritableSession = ActiveSession is { } session && CanInvokeFunctions(session);
        var operation = SelectedLogbookMutationOption;
        CanApplyLogbookMutation =
            hasWritableSession
            && !string.IsNullOrWhiteSpace(LogbookHandleTokenText)
            && operation is not null
            && (!operation.RequiresEntry || !string.IsNullOrWhiteSpace(LogbookMutationEntryText))
            && (
                (!operation.ShowsValueSelector && !operation.ShowsValueInput)
                || !string.IsNullOrWhiteSpace(LogbookMutationValueText)
            )
            && (!operation.ShowsAuxiliaryInput || !string.IsNullOrWhiteSpace(LogbookMutationAuxiliaryText));
    }

    private static IReadOnlyList<string> CreateLogbookMutationLines(LogbookMutationSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.OperationText))
            lines.Add($"Operation: {snapshot.OperationText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        if (!string.IsNullOrWhiteSpace(snapshot.SubjectText))
            lines.Add($"Subject: {snapshot.SubjectText}");

        if (!string.IsNullOrWhiteSpace(snapshot.ValueText))
            lines.Add($"Value: {snapshot.ValueText}");

        if (!string.IsNullOrWhiteSpace(snapshot.AuxiliaryText))
        {
            var auxiliaryLabel = snapshot.Kind switch
            {
                LogbookMutationKind.AddKill => "Victim handle",
                LogbookMutationKind.RemoveInjury => "Slot",
                LogbookMutationKind.SetBackground => "Text",
                _ => "Extra",
            };
            lines.Add($"{auxiliaryLabel}: {snapshot.AuxiliaryText}");
        }

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static bool MatchesLogbookCatalogFilter(LogbookCatalogEntrySnapshot entry, string normalizedFilter) =>
        normalizedFilter.Length == 0
        || NormalizeLogbookCatalogFilter(entry.DisplayName).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeLogbookCatalogFilter(entry.DetailText).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeLogbookCatalogFilter(entry.EntryId.ToString(CultureInfo.InvariantCulture))
            .Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeLogbookCatalogFilter(entry.AuxiliaryId.ToString(CultureInfo.InvariantCulture))
            .Contains(normalizedFilter, StringComparison.Ordinal);

    private void TrySelectLogbookCatalogEntry(string categoryToken, string entryText)
    {
        if (
            categoryToken.Length == 0
            || !int.TryParse(entryText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryId)
        )
        {
            return;
        }

        SelectedLogbookCatalogEntry = _logbookCatalogCache.FirstOrDefault(entry =>
            entry.CategoryToken.Equals(categoryToken, StringComparison.OrdinalIgnoreCase) && entry.EntryId == entryId
        );
    }

    private static string NormalizeLogbookCatalogFilter(string? value)
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

    private static string CreateLogbookEditorAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live journal edits are unavailable until a new session is attached.";

        return "Live journal editing requires a validated runtime profile with live function invocation support.";
    }

    private static readonly DebuggerChoiceOption[] s_questPcStateOptions =
    [
        new("unknown", "Unknown", "Clears one quest back to the unknown state when the native quest path allows it."),
        new("mentioned", "Mentioned", "Marks one quest as mentioned."),
        new("accepted", "Accepted", "Marks one quest as accepted."),
        new("achieved", "Achieved", "Marks one quest as achieved."),
        new("completed", "Completed", "Marks one quest as completed."),
        new("other-completed", "Other Completed", "Marks one quest as completed elsewhere."),
        new(
            "botched",
            "Botched",
            "Marks one quest as botched while preserving the current base state when ArcNET has to rewrite the raw PC quest record directly."
        ),
        new(
            "mentioned-botched",
            "Mentioned [Botched]",
            "Writes one raw botched quest record whose recoverable base state is Mentioned."
        ),
        new(
            "accepted-botched",
            "Accepted [Botched]",
            "Writes one raw botched quest record whose recoverable base state is Accepted."
        ),
        new(
            "achieved-botched",
            "Achieved [Botched]",
            "Writes one raw botched quest record whose recoverable base state is Achieved."
        ),
        new(
            "completed-botched",
            "Completed [Botched]",
            "Writes one raw botched quest record whose recoverable base state is Completed."
        ),
        new(
            "other-completed-botched",
            "Other Completed [Botched]",
            "Writes one raw botched quest record whose recoverable base state is Other Completed."
        ),
    ];

    private static readonly DebuggerChoiceOption[] s_questGlobalStateOptions =
    [
        new(
            "unknown",
            "Unknown",
            "Clears one global quest state back to unknown when the native global quest path allows it."
        ),
        new("mentioned", "Mentioned", "Marks one shared quest state as mentioned."),
        new("accepted", "Accepted", "Marks one shared quest state as accepted."),
        new("achieved", "Achieved", "Marks one shared quest state as achieved."),
        new("completed", "Completed", "Marks one shared quest state as completed."),
        new("other-completed", "Other Completed", "Marks one shared quest state as completed elsewhere."),
        new("botched", "Botched", "Marks one shared quest state as botched."),
    ];

    private static readonly DebuggerChoiceOption[] s_injuryTypeOptions =
    [
        new("blinded", "Blinded", "Records blindness history for the selected source description."),
        new("crippled-arm", "Crippled Arm", "Records arm-crippling history for the selected source description."),
        new("crippled-leg", "Crippled Leg", "Records leg-crippling history for the selected source description."),
        new("scarred", "Scarred", "Records scarring history for the selected source description."),
    ];

    private readonly record struct LogbookLiveInspectionContext(
        LogbookMutationKind Kind,
        string DisplayName,
        int EntryId,
        int AuxiliaryId,
        string ValueTokenText,
        string PendingStatusText,
        string PendingSummaryText,
        ReadRequest? ReadRequest,
        LogbookRequest? LogbookRequest
    );
}

public sealed record class DebuggerLogbookMutationOption(
    LogbookMutationKind Kind,
    string Label,
    string Description,
    string CategoryToken,
    string PageToken,
    bool RequiresEntry,
    bool ShowsValueSelector,
    bool ShowsValueInput,
    bool ShowsAuxiliaryInput,
    string ValuePlaceholderText,
    string AuxiliaryPlaceholderText
);
