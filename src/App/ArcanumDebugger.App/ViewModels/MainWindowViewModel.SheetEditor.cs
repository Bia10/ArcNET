using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.GameObjects.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly SheetEditorService _sheetEditorService;
    private IReadOnlyList<DebuggerSheetEditableField> _sheetEditableFieldCache = [];
    private string? _sheetEditableFieldSourceTokenText;
    private int _sheetLiveInspectionVersion;

    [ObservableProperty]
    private string sheetEditableFieldSummaryText =
        "Load editable fields from one live sheet snapshot to browse stats, progression, identity, resistances, skills, spell mastery, colleges, and tech disciplines.";

    [ObservableProperty]
    private string sheetEditableFieldFilterText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerSheetEditableField> sheetEditableFields = [];

    [ObservableProperty]
    private DebuggerSheetEditableField? selectedSheetEditableField;

    [ObservableProperty]
    private string sheetMutationValueText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> sheetMutationValueOptions = [];

    [ObservableProperty]
    private DebuggerChoiceOption? selectedSheetMutationValueOption;

    [ObservableProperty]
    private bool sheetMutationShowValueSelector;

    [ObservableProperty]
    private bool sheetMutationShowValueInput = true;

    [ObservableProperty]
    private string sheetMutationValuePlaceholderText = "Value, rank, degree, or none";

    [ObservableProperty]
    private string sheetMutationTrainingText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> sheetMutationTrainingOptions = [];

    [ObservableProperty]
    private DebuggerChoiceOption? selectedSheetMutationTrainingOption;

    [ObservableProperty]
    private bool sheetMutationShowTrainingSelector;

    [ObservableProperty]
    private bool sheetMutationShowTrainingInput = true;

    [ObservableProperty]
    private string sheetMutationTrainingPlaceholderText = "Training optional for skills";

    [ObservableProperty]
    private string sheetMutationTimeoutText = "1000";

    [ObservableProperty]
    private string sheetMutationInputHintText =
        "Select one scanned field to prefill the editor, or type a supported field token manually. Training is only used for basic or tech skills, while Gender and Race expose named selectors.";

    [ObservableProperty]
    private bool sheetMutationShowLiveStateInspector = true;

    [ObservableProperty]
    private string sheetMutationLiveStateStatusText = "Current field state";

    [ObservableProperty]
    private string sheetMutationLiveStateSummaryText =
        "Pick or type one field token to inspect the current live value on the selected player or companion before applying a change.";

    [ObservableProperty]
    private string sheetMutationLiveStateValueText = string.Empty;

    [ObservableProperty]
    private string sheetMutationLiveStateTrainingText = string.Empty;

    [ObservableProperty]
    private bool sheetMutationLiveStateShowTrainingText;

    [ObservableProperty]
    private bool canUseSheetLiveStateValue;

    [ObservableProperty]
    private bool canLoadSheetEditableFields;

    [ObservableProperty]
    private bool canApplySheetMutation;

    [ObservableProperty]
    private string sheetMutationStatusText = "No live sheet mutation executed.";

    [ObservableProperty]
    private IReadOnlyList<string> sheetMutationResultLines =
    [
        "Load editable fields, pick one row, then write a new value without hunting internal ids or remembering every sheet token.",
    ];

    [ObservableProperty]
    private string sheetMutationDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string sheetMutationExecutionDetailText =
        "Target address and hook details will appear here after a live sheet mutation.";

    [ObservableProperty]
    private string sheetMutationResultText = "Mutation result values will appear here after a live sheet mutation.";

    partial void OnSheetHandleTokenTextChanged(string value)
    {
        InvalidateSheetEditableFieldSnapshotIfTargetChanged(value);
        QueueRefreshSheetLiveInspection();
        RefreshSheetEditorActions();
    }

    partial void OnSheetEditableFieldFilterTextChanged(string value) => ApplyFilteredSheetEditableFields();

    partial void OnSheetLabelTextChanged(string value)
    {
        ClearSelectedSheetEditableFieldIfTokenChanged(value);
        ApplySheetMutationEditorDescriptor();
        QueueRefreshSheetLiveInspection();
        RefreshSheetEditorActions();
    }

    partial void OnSheetMutationValueTextChanged(string value)
    {
        SyncSelectedSheetMutationValueOption();
        RefreshSheetEditorActions();
    }

    partial void OnSheetMutationTrainingTextChanged(string value)
    {
        SyncSelectedSheetMutationTrainingOption();
        RefreshSheetEditorActions();
    }

    partial void OnSheetMutationTimeoutTextChanged(string value) => RefreshSheetEditorActions();

    partial void OnSelectedSheetMutationValueOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is null)
        {
            RefreshSheetEditorActions();
            return;
        }

        if (!SheetMutationValueText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            SheetMutationValueText = value.Token;

        RefreshSheetEditorActions();
    }

    partial void OnSelectedSheetMutationTrainingOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is null)
        {
            RefreshSheetEditorActions();
            return;
        }

        if (!SheetMutationTrainingText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            SheetMutationTrainingText = value.Token;

        RefreshSheetEditorActions();
    }

    partial void OnSelectedSheetEditableFieldChanged(DebuggerSheetEditableField? value)
    {
        if (value is null)
        {
            SheetMutationInputHintText =
                "Select one scanned field to prefill the editor, or type a supported field token manually. Training is only used for basic or tech skills, while Gender and Race expose named selectors.";
            ApplySheetMutationEditorDescriptor();
            QueueRefreshSheetLiveInspection();
            RefreshSheetEditorActions();
            return;
        }

        if (!SheetLabelText.Equals(value.FieldToken, StringComparison.OrdinalIgnoreCase))
            SheetLabelText = value.FieldToken;

        if (!SheetMutationValueText.Equals(value.EditorValueText, StringComparison.Ordinal))
            SheetMutationValueText = value.EditorValueText;

        if (!SheetMutationTrainingText.Equals(value.EditorTrainingText, StringComparison.Ordinal))
            SheetMutationTrainingText = value.EditorTrainingText;

        SheetMutationInputHintText = value.InputHintText;
        ApplySheetMutationEditorDescriptor();
        QueueRefreshSheetLiveInspection();
        RefreshSheetEditorActions();
    }

    [RelayCommand]
    private void UseSelectedMobileForSheetTarget()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        SheetHandleTokenText = entry.HandleHex;
    }

    [RelayCommand]
    private void UseSheetLiveStateValue()
    {
        if (!CanUseSheetLiveStateValue)
            return;

        if (
            !string.IsNullOrWhiteSpace(SheetMutationLiveStateValueText)
            && !SheetMutationValueText.Equals(SheetMutationLiveStateValueText, StringComparison.Ordinal)
        )
        {
            SheetMutationValueText = SheetMutationLiveStateValueText;
        }

        if (
            (SheetMutationShowTrainingSelector || SheetMutationShowTrainingInput)
            && !SheetMutationTrainingText.Equals(SheetMutationLiveStateTrainingText, StringComparison.Ordinal)
        )
        {
            SheetMutationTrainingText = SheetMutationLiveStateTrainingText;
        }
    }

    [RelayCommand]
    private void UseSelectedMobileForLogbookTarget()
    {
        if (SelectedMobileRosterEntry is not { } entry)
            return;

        LogbookHandleTokenText = entry.HandleHex;
    }

    [RelayCommand]
    private async Task LoadSheetEditableFields()
    {
        if (ActiveSession is not { } session)
        {
            ResetSheetEditorState("Attach to a validated runtime before loading editable character-sheet fields.");
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ResetSheetEditorState(CreateSheetEditorAvailabilitySummary(session));
            return;
        }

        try
        {
            var snapshot = await LoadSheetEditableFieldSnapshot(session);
            SheetStatusText = snapshot.Status;
            SheetResultLines = CreateSheetScanLines(snapshot);
            ApplySheetEditableFieldSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            ResetSheetEditorState($"Unable to load editable sheet fields ({ex.Message}).");
        }
    }

    [RelayCommand]
    private async Task ApplySheetMutation()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSheetMutation(
                "No active session",
                ["Attach to a validated runtime before editing a live character sheet."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSheetMutation("Sheet editor unavailable", [CreateSheetEditorAvailabilitySummary(session)]);
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _sheetEditorService.Write(
                    new SheetWriteRequest(
                        session,
                        SheetHandleTokenText,
                        SheetLabelText,
                        SheetMutationValueText,
                        SheetMutationTrainingText,
                        SheetMutationTimeoutText
                    )
                )
            );
            ApplySheetMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                var refreshedScan = await LoadSheetEditableFieldSnapshot(session);
                SheetStatusText = refreshedScan.Status;
                SheetResultLines = CreateSheetScanLines(refreshedScan);
                ApplySheetEditableFieldSnapshot(refreshedScan);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSheetMutation("Sheet mutation failed", [ex.Message]);
        }
    }

    private async Task<SheetScanSnapshot> LoadSheetEditableFieldSnapshot(AttachedSessionSnapshot session) =>
        await Task.Run(() => _sheetService.Scan(new SheetScanRequest(session, SheetHandleTokenText)));

    private void ApplySheetEditableFieldSnapshot(SheetScanSnapshot snapshot)
    {
        if (!snapshot.IsAvailable)
        {
            ResetSheetEditorState(snapshot.Summary);
            return;
        }

        _sheetEditableFieldSourceTokenText = SheetHandleTokenText.Trim();
        _sheetEditableFieldCache = CreateSheetEditableFields(snapshot.Data);
        ApplyFilteredSheetEditableFields(snapshot.TargetText);
        QueueRefreshSheetLiveInspection();
        RefreshSheetEditorActions();
    }

    private void ResetSheetEditorState(string summary)
    {
        SheetEditableFieldSummaryText = summary;
        _sheetEditableFieldSourceTokenText = null;
        _sheetEditableFieldCache = [];
        SheetEditableFields = [];
        SelectedSheetEditableField = null;
        SheetMutationValueText = string.Empty;
        SheetMutationTrainingText = string.Empty;
        SheetMutationInputHintText =
            "Select one scanned field to prefill the editor, or type a supported field token manually. Training is only used for basic or tech skills, while Gender and Race expose named selectors.";
        ApplySheetMutationEditorDescriptor();
        ResetSheetLiveInspectionState();
        RefreshSheetEditorActions();
    }

    private void ApplyDormantSheetMutation(string status, IReadOnlyList<string> lines)
    {
        SheetMutationStatusText = status;
        SheetMutationDispatcherText = "Dispatcher result unavailable.";
        SheetMutationExecutionDetailText =
            "Target address and hook details will appear here after a live sheet mutation.";
        SheetMutationResultText = "Mutation result values will appear here after a live sheet mutation.";
        SheetMutationResultLines = lines;
        RefreshSheetEditorActions();
    }

    private void ApplySheetMutationSnapshot(SheetMutationSnapshot snapshot)
    {
        SheetMutationStatusText = snapshot.Status;
        SheetMutationDispatcherText = snapshot.DispatcherText;
        SheetMutationExecutionDetailText = snapshot.ExecutionDetailText;
        SheetMutationResultText = snapshot.ResultText;
        SheetMutationResultLines = CreateSheetMutationLines(snapshot);
        QueueRefreshSheetLiveInspection();
        RefreshSheetEditorActions();
    }

    private void RefreshSheetEditorActions()
    {
        var hasWritableSession = ActiveSession is { } session && CanInvokeFunctions(session);
        CanLoadSheetEditableFields = hasWritableSession && !string.IsNullOrWhiteSpace(SheetHandleTokenText);
        CanApplySheetMutation =
            hasWritableSession
            && !string.IsNullOrWhiteSpace(SheetHandleTokenText)
            && !string.IsNullOrWhiteSpace(SheetLabelText)
            && !string.IsNullOrWhiteSpace(SheetMutationValueText);
    }

    private void ApplySheetMutationEditorDescriptor()
    {
        var descriptor = TryResolveSheetMutationReference(SheetLabelText, out var reference)
            ? SheetMutationOptionCatalog.Describe(reference)
            : SheetMutationOptionCatalog.Fallback;
        SheetMutationShowValueSelector = descriptor.ShowsValueSelector;
        SheetMutationShowValueInput = descriptor.ShowsValueInput;
        SheetMutationValueOptions = descriptor.ValueOptions;
        SheetMutationValuePlaceholderText = descriptor.ValuePlaceholderText;
        SheetMutationShowTrainingSelector = descriptor.ShowsTrainingSelector;
        SheetMutationShowTrainingInput = descriptor.ShowsTrainingInput;
        SheetMutationTrainingOptions = descriptor.TrainingOptions;
        SheetMutationTrainingPlaceholderText = descriptor.TrainingPlaceholderText;
        SheetMutationLiveStateShowTrainingText = descriptor.ShowsTrainingSelector || descriptor.ShowsTrainingInput;
        SelectedSheetMutationValueOption = reference is { } resolvedReference
            ? SheetMutationOptionCatalog.ResolveValueOption(resolvedReference, SheetMutationValueText)
            : null;
        SelectedSheetMutationTrainingOption = SheetMutationOptionCatalog.ResolveTrainingOption(
            SheetMutationTrainingText
        );
    }

    private void SyncSelectedSheetMutationValueOption()
    {
        SelectedSheetMutationValueOption = TryResolveSheetMutationReference(SheetLabelText, out var reference)
            ? SheetMutationOptionCatalog.ResolveValueOption(reference, SheetMutationValueText)
            : null;
    }

    private void SyncSelectedSheetMutationTrainingOption() =>
        SelectedSheetMutationTrainingOption = SheetMutationOptionCatalog.ResolveTrainingOption(
            SheetMutationTrainingText
        );

    private void QueueRefreshSheetLiveInspection() => _ = RefreshSheetLiveInspectionAsync();

    private async Task RefreshSheetLiveInspectionAsync()
    {
        var requestVersion = ++_sheetLiveInspectionVersion;
        if (ActiveSession is not { } session)
        {
            if (requestVersion == _sheetLiveInspectionVersion)
            {
                ApplySheetLiveInspectionStatus(
                    "No active session",
                    "Attach to a live runtime before inspecting the current field state."
                );
            }

            return;
        }

        if (!CanInvokeFunctions(session))
        {
            if (requestVersion == _sheetLiveInspectionVersion)
                ApplySheetLiveInspectionStatus(
                    "Live field inspection unavailable",
                    CreateSheetEditorAvailabilitySummary(session)
                );

            return;
        }

        if (string.IsNullOrWhiteSpace(SheetHandleTokenText))
        {
            if (requestVersion == _sheetLiveInspectionVersion)
            {
                ApplySheetLiveInspectionStatus(
                    "Sheet target required",
                    "Provide a player or companion handle before inspecting the current field value."
                );
            }

            return;
        }

        if (!TryResolveSheetMutationReference(SheetLabelText, out var reference))
        {
            if (requestVersion == _sheetLiveInspectionVersion)
            {
                ApplySheetLiveInspectionStatus(
                    "Field selection required",
                    "Pick one scanned field or enter one supported field token to inspect the current live value."
                );
            }

            return;
        }

        SheetMutationShowLiveStateInspector = true;
        SheetMutationLiveStateStatusText = "Reading current field state...";
        SheetMutationLiveStateSummaryText =
            "ArcNET is reading the current live field value so you can compare the selected player or companion before applying a change.";
        CanUseSheetLiveStateValue = false;

        try
        {
            var snapshot = await Task.Run(() =>
                _sheetService.Read(new SheetRequest(session, SheetHandleTokenText, SheetLabelText))
            );
            if (requestVersion != _sheetLiveInspectionVersion)
                return;

            if (!snapshot.IsAvailable)
            {
                ApplySheetLiveInspectionStatus(snapshot.Status, snapshot.Summary);
                return;
            }

            ApplySheetLiveStatus(SheetLiveStatusCatalog.Describe(reference, snapshot));
        }
        catch (Exception ex)
        {
            if (requestVersion == _sheetLiveInspectionVersion)
                ApplySheetLiveInspectionStatus("Live field inspection failed", ex.Message);
        }
    }

    private void ApplySheetLiveStatus(DebuggerSheetLiveStatus status)
    {
        SheetMutationShowLiveStateInspector = true;
        SheetMutationLiveStateStatusText = status.StatusText;
        SheetMutationLiveStateSummaryText = status.SummaryText;
        SheetMutationLiveStateValueText = status.ValueTokenText;
        SheetMutationLiveStateTrainingText = status.TrainingTokenText;
        CanUseSheetLiveStateValue =
            !string.IsNullOrWhiteSpace(status.ValueTokenText) || !string.IsNullOrWhiteSpace(status.TrainingTokenText);
    }

    private void ApplySheetLiveInspectionStatus(string statusText, string summaryText)
    {
        SheetMutationShowLiveStateInspector = true;
        SheetMutationLiveStateStatusText = statusText;
        SheetMutationLiveStateSummaryText = summaryText;
        SheetMutationLiveStateValueText = string.Empty;
        SheetMutationLiveStateTrainingText = string.Empty;
        CanUseSheetLiveStateValue = false;
    }

    private void ResetSheetLiveInspectionState()
    {
        SheetMutationShowLiveStateInspector = true;
        SheetMutationLiveStateStatusText = "Current field state";
        SheetMutationLiveStateSummaryText =
            "Pick or type one field token to inspect the current live value on the selected player or companion before applying a change.";
        SheetMutationLiveStateValueText = string.Empty;
        SheetMutationLiveStateTrainingText = string.Empty;
        CanUseSheetLiveStateValue = false;
    }

    private void InvalidateSheetEditableFieldSnapshotIfTargetChanged(string? handleTokenText)
    {
        if (_sheetEditableFieldCache.Count == 0 || string.IsNullOrWhiteSpace(_sheetEditableFieldSourceTokenText))
            return;

        var normalizedToken = handleTokenText?.Trim() ?? string.Empty;
        if (normalizedToken.Equals(_sheetEditableFieldSourceTokenText, StringComparison.OrdinalIgnoreCase))
            return;

        _sheetEditableFieldSourceTokenText = null;
        _sheetEditableFieldCache = [];
        SheetEditableFields = [];
        SelectedSheetEditableField = null;
        SheetEditableFieldSummaryText =
            "The loaded field browser belonged to a different target. Reload editable fields for this player or companion, or use the live field inspector below to compare the current value first.";
    }

    private void ClearSelectedSheetEditableFieldIfTokenChanged(string? fieldToken)
    {
        if (
            SelectedSheetEditableField is { } selectedField
            && !selectedField.FieldToken.Equals(fieldToken?.Trim(), StringComparison.OrdinalIgnoreCase)
        )
        {
            SelectedSheetEditableField = null;
        }
    }

    private void ApplyFilteredSheetEditableFields(string? targetText = null)
    {
        if (_sheetEditableFieldCache.Count == 0)
        {
            SheetEditableFields = [];
            SelectedSheetEditableField = null;
            RefreshSheetEditorActions();
            return;
        }

        var selectedFieldToken = SelectedSheetEditableField?.FieldToken ?? SheetLabelText;
        var normalizedFilter = NormalizeSheetEditableFieldFilter(SheetEditableFieldFilterText);
        SheetEditableFields =
        [
            .. _sheetEditableFieldCache.Where(field => MatchesSheetEditableFieldFilter(field, normalizedFilter)),
        ];
        SelectedSheetEditableField =
            selectedFieldToken.Length == 0
                ? SheetEditableFields.FirstOrDefault()
                : SheetEditableFields.FirstOrDefault(field =>
                    field.FieldToken.Equals(selectedFieldToken, StringComparison.OrdinalIgnoreCase)
                ) ?? SheetEditableFields.FirstOrDefault();

        var summaryTargetText = string.IsNullOrWhiteSpace(targetText) ? "the selected target" : targetText;
        SheetEditableFieldSummaryText =
            normalizedFilter.Length == 0
                ? $"Loaded {_sheetEditableFieldCache.Count.ToString(CultureInfo.InvariantCulture)} editable fields for {summaryTargetText}. Selecting one row prefills the editor."
                : $"Showing {SheetEditableFields.Count.ToString(CultureInfo.InvariantCulture)}/{_sheetEditableFieldCache.Count.ToString(CultureInfo.InvariantCulture)} editable fields for {summaryTargetText}.";
        RefreshSheetEditorActions();
    }

    private static bool TryResolveSheetMutationReference(string? fieldToken, out SheetReference reference)
    {
        if (string.IsNullOrWhiteSpace(fieldToken))
        {
            reference = default;
            return false;
        }

        try
        {
            reference = SheetCatalog.ResolveReference(fieldToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            reference = default;
            return false;
        }
    }

    private static bool MatchesSheetEditableFieldFilter(DebuggerSheetEditableField field, string normalizedFilter) =>
        normalizedFilter.Length == 0
        || NormalizeSheetEditableFieldFilter(field.DisplayName).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeSheetEditableFieldFilter(field.Category).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeSheetEditableFieldFilter(field.FieldToken).Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeSheetEditableFieldFilter(field.CurrentValueText)
            .Contains(normalizedFilter, StringComparison.Ordinal)
        || NormalizeSheetEditableFieldFilter(field.DetailText).Contains(normalizedFilter, StringComparison.Ordinal);

    private static string NormalizeSheetEditableFieldFilter(string? value)
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

    private static IReadOnlyList<string> CreateSheetMutationLines(SheetMutationSnapshot snapshot)
    {
        List<string> lines = [snapshot.Summary];
        if (!string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            lines.Add($"Handle: {snapshot.TargetHandleText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TargetText))
            lines.Add($"Target: {snapshot.TargetText}");

        if (!string.IsNullOrWhiteSpace(snapshot.FieldDisplayName))
            lines.Add($"Field: {snapshot.FieldDisplayName}");

        if (!string.IsNullOrWhiteSpace(snapshot.ValueText))
            lines.Add($"Value: {snapshot.ValueText}");

        if (!string.IsNullOrWhiteSpace(snapshot.TrainingText))
            lines.Add($"Training: {snapshot.TrainingText}");

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<DebuggerSheetEditableField> CreateSheetEditableFields(SheetDataSnapshot data)
    {
        List<DebuggerSheetEditableField> fields = [];
        fields.AddRange(
            data.PrimaryStats.Select(static stat => CreateScalarField("Primary Stat", stat, SheetRoute.Stat))
        );
        fields.AddRange(data.Progression.Select(static stat => CreateProgressionField(stat)));
        fields.AddRange(
            data.DerivedStats.Select(static stat => CreateScalarField("Derived Stat", stat, SheetRoute.DerivedStat))
        );
        fields.AddRange(
            data.Resistances.Select(static stat => CreateScalarField("Resistance", stat, SheetRoute.Resistance))
        );
        fields.AddRange(
            data.BasicSkills.Select(static skill => CreateSkillField("Basic Skill", skill, SheetRoute.BasicSkill))
        );
        fields.AddRange(
            data.TechSkills.Select(static skill => CreateSkillField("Tech Skill", skill, SheetRoute.TechSkill))
        );
        fields.AddRange(
            data.SpellColleges.Select(static stat => CreateScalarField("Spell College", stat, SheetRoute.SpellCollege))
        );
        fields.Add(CreateSpellMasteryField(data.SpellMastery));
        fields.AddRange(
            data.TechDisciplines.Select(static stat =>
                CreateScalarField("Tech Discipline", stat, SheetRoute.TechDiscipline)
            )
        );
        return fields;
    }

    private static DebuggerSheetEditableField CreateScalarField(
        string category,
        SheetScalarSnapshot value,
        SheetRoute route
    )
    {
        var reference = new SheetReference(route, value.Id, value.Name);
        var fieldToken = CreateFieldToken(route, value.Name);
        var currentValueText =
            route == SheetRoute.SpellMastery
                ? FormatSpellMasteryValue(value.Value)
                : SheetValueCatalog.FormatDisplayValue(reference, value.Value);
        var editorValueText =
            route == SheetRoute.SpellMastery
                ? (
                    value.Value is >= 0 and < SpellCollegeCount
                        ? CharacterSheetMetadata.SpellCollegeName(value.Value)
                        : "none"
                )
                : SheetValueCatalog.FormatEditorValue(reference, value.Value);
        var detailText = route switch
        {
            SheetRoute.Stat when reference.Id is 26 or 27 =>
                $"Current {currentValueText} · raw {value.Value.ToString(CultureInfo.InvariantCulture)} · token: {fieldToken}",
            SheetRoute.Resistance => $"Token: {fieldToken}",
            SheetRoute.SpellCollege => $"Current rank · token: {fieldToken}",
            SheetRoute.TechDiscipline => $"Current degree · token: {fieldToken}",
            SheetRoute.DerivedStat => $"Derived stat · token: {fieldToken}",
            SheetRoute.SpellMastery => $"Token: {fieldToken}",
            _ => $"Token: {fieldToken}",
        };
        return new DebuggerSheetEditableField(
            fieldToken,
            value.Name,
            category,
            currentValueText,
            detailText,
            editorValueText,
            string.Empty,
            route switch
            {
                SheetRoute.SpellCollege => "Enter a spell-college rank between 0 and 5.",
                SheetRoute.TechDiscipline => "Enter a tech-discipline degree between 0 and 7.",
                SheetRoute.SpellMastery =>
                    "Use one college name such as fire or conveyance, or enter 'none' to clear mastery.",
                _ => SheetValueCatalog.CreateInputHint(reference),
            },
            route,
            SupportsTraining: false
        );
    }

    private static DebuggerSheetEditableField CreateProgressionField(SheetScalarSnapshot value) =>
        CreateScalarField(
            value.Id switch
            {
                24 => "Condition",
                25 or 26 or 27 => "Identity",
                _ => "Progression",
            },
            value,
            SheetRoute.Stat
        );

    private static DebuggerSheetEditableField CreateSkillField(
        string category,
        SheetSkillSnapshot value,
        SheetRoute route
    )
    {
        var fieldToken = CreateFieldToken(route, value.Name);
        return new DebuggerSheetEditableField(
            fieldToken,
            value.Name,
            category,
            value.Value.ToString(CultureInfo.InvariantCulture),
            $"{value.TrainingName} training · encoded {value.Encoded.ToString(CultureInfo.InvariantCulture)} · token: {fieldToken}",
            value.Value.ToString(CultureInfo.InvariantCulture),
            value.TrainingName.ToLowerInvariant(),
            "Enter skill points between 0 and 63. Training is optional: untrained, apprentice, expert, or master.",
            route,
            SupportsTraining: true
        );
    }

    private static DebuggerSheetEditableField CreateSpellMasteryField(SheetScalarSnapshot value) =>
        new(
            "spell-mastery",
            value.Name,
            "Spell Mastery",
            FormatSpellMasteryValue(value.Value),
            "Use a college name or 'none' to clear spell mastery.",
            value.Value is >= 0 and < SpellCollegeCount ? CharacterSheetMetadata.SpellCollegeName(value.Value) : "none",
            string.Empty,
            "Use one college name such as fire or temporal, or enter 'none' to clear mastery.",
            SheetRoute.SpellMastery,
            SupportsTraining: false
        );

    private static string CreateFieldToken(SheetRoute route, string name) =>
        route switch
        {
            SheetRoute.Resistance => $"{name.ToLowerInvariant()}-resistance",
            SheetRoute.SpellMastery => "spell-mastery",
            _ => name,
        };

    private static string FormatSpellMasteryValue(int value) =>
        value is >= 0 and < SpellCollegeCount ? CharacterSheetMetadata.SpellCollegeName(value) : "None";

    private static string CreateSheetEditorAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
        {
            return "The attached process has exited, so live sheet edits are unavailable until a new session is attached.";
        }

        return "Live sheet editing requires a validated runtime profile with live function invocation support.";
    }

    private const int SpellCollegeCount = 16;
}

public sealed record class DebuggerSheetEditableField(
    string FieldToken,
    string DisplayName,
    string Category,
    string CurrentValueText,
    string DetailText,
    string EditorValueText,
    string EditorTrainingText,
    string InputHintText,
    SheetRoute Route,
    bool SupportsTraining
);
