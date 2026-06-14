using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.Diagnostics.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private readonly SpellTechEditorService _spellTechEditorService;

    [ObservableProperty]
    private string spellTechTargetHandleText = "player";

    [ObservableProperty]
    private string spellTechSpellTokenText = "teleportation";

    [ObservableProperty]
    private string spellTechSpellFilterText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<GameDataQuickPickEntry> spellTechSpellEntries = GameDataQuickPickCatalog.BuildSpellEntries(
        string.Empty,
        MaxSpellTechSpellCatalogEntries
    );

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedSpellTechSpellEntry;

    [ObservableProperty]
    private string spellTechSchematicIdText = string.Empty;

    [ObservableProperty]
    private string spellTechSchematicFilterText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<GameDataQuickPickEntry> spellTechSchematicCatalogEntries = [];

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedSpellTechSchematicCatalogEntry;

    [ObservableProperty]
    private bool hasSpellTechSchematicCatalogEntries;

    [ObservableProperty]
    private string spellTechCollegeTokenText = "fire";

    [ObservableProperty]
    private IReadOnlyList<GameDataQuickPickEntry> spellTechCollegeEntries =
        GameDataQuickPickCatalog.BuildSpellCollegeEntries(string.Empty, MaxSpellTechChoiceEntries);

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedSpellTechCollegeEntry;

    [ObservableProperty]
    private string spellTechCollegeLevelText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> spellTechCollegeLevelOptions = CreateSpellTechNumericOptions(
        0,
        SpellTechCatalog.SpellMaxLevel,
        "Rank"
    );

    [ObservableProperty]
    private DebuggerChoiceOption? selectedSpellTechCollegeLevelOption;

    [ObservableProperty]
    private string spellTechDisciplineTokenText = "mechanical";

    [ObservableProperty]
    private IReadOnlyList<GameDataQuickPickEntry> spellTechDisciplineEntries =
        GameDataQuickPickCatalog.BuildTechDisciplineEntries(string.Empty, MaxSpellTechChoiceEntries);

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedSpellTechDisciplineEntry;

    [ObservableProperty]
    private string spellTechDisciplineLevelText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> spellTechDisciplineLevelOptions = CreateSpellTechNumericOptions(
        0,
        7,
        "Degree"
    );

    [ObservableProperty]
    private DebuggerChoiceOption? selectedSpellTechDisciplineLevelOption;

    [ObservableProperty]
    private string spellTechSkillTokenText = "repair";

    [ObservableProperty]
    private IReadOnlyList<GameDataQuickPickEntry> spellTechSkillEntries =
        GameDataQuickPickCatalog.BuildTechSkillEntries(string.Empty, MaxSpellTechChoiceEntries);

    [ObservableProperty]
    private GameDataQuickPickEntry? selectedSpellTechSkillEntry;

    [ObservableProperty]
    private string spellTechSkillPointsText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DebuggerChoiceOption> spellTechSkillPointOptions = CreateSpellTechNumericOptions(
        0,
        SpellTechCatalog.TechSkillPointMask,
        "Points"
    );

    [ObservableProperty]
    private DebuggerChoiceOption? selectedSpellTechSkillPointOption;

    [ObservableProperty]
    private string spellTechTimeoutText = "1000";

    [ObservableProperty]
    private bool canAddSpell;

    [ObservableProperty]
    private bool canGrantSchematic;

    [ObservableProperty]
    private bool canRemoveSchematic;

    [ObservableProperty]
    private bool canSetSpellCollegeLevel;

    [ObservableProperty]
    private bool canSetTechDisciplineLevel;

    [ObservableProperty]
    private bool canSetTechSkillPoints;

    [ObservableProperty]
    private string spellTechMutationStatusText = "No spell or tech mutation executed.";

    [ObservableProperty]
    private IReadOnlyList<string> spellTechMutationResultLines =
    [
        "Force-learn spells, grant or remove schematic ids, or write spell-school, tech-discipline, and tech-skill progression directly against the live runtime.",
        "Use the built-in module and live selection lists instead of guessing spell, college, skill, discipline, or schematic tokens.",
    ];

    [ObservableProperty]
    private string spellTechMutationDispatcherText = "Dispatcher result unavailable.";

    [ObservableProperty]
    private string spellTechMutationExecutionDetailText =
        "Target address and hook details will appear here after a live spell or tech mutation.";

    [ObservableProperty]
    private string spellTechMutationResultText =
        "Mutation result values will appear here after a live spell or tech mutation.";

    partial void OnSpellTechTargetHandleTextChanged(string value)
    {
        InvalidateSpellTechLiveStateIfTargetChanged(value);
        RefreshSpellTechEditorActions();
    }

    partial void OnSpellTechSpellTokenTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSpellTechSpellFilterTextChanged(string value) => RefreshSpellTechSelectionLists();

    partial void OnSelectedSpellTechSpellEntryChanged(GameDataQuickPickEntry? value)
    {
        if (
            value is not null
            && !SpellTechSpellTokenText.Equals(value.ApplyValueText, StringComparison.OrdinalIgnoreCase)
        )
            SpellTechSpellTokenText = value.ApplyValueText;
    }

    partial void OnSpellTechSchematicIdTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSpellTechSchematicFilterTextChanged(string value) => RefreshSpellTechSelectionLists();

    partial void OnSelectedSpellTechSchematicCatalogEntryChanged(GameDataQuickPickEntry? value)
    {
        if (
            value is not null
            && !SpellTechSchematicIdText.Equals(value.ApplyValueText, StringComparison.OrdinalIgnoreCase)
        )
            SpellTechSchematicIdText = value.ApplyValueText;
    }

    partial void OnSpellTechCollegeTokenTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSelectedSpellTechCollegeEntryChanged(GameDataQuickPickEntry? value)
    {
        if (
            value is not null
            && !SpellTechCollegeTokenText.Equals(value.ApplyValueText, StringComparison.OrdinalIgnoreCase)
        )
            SpellTechCollegeTokenText = value.ApplyValueText;
    }

    partial void OnSpellTechCollegeLevelTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSelectedSpellTechCollegeLevelOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is not null && !SpellTechCollegeLevelText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            SpellTechCollegeLevelText = value.Token;
    }

    partial void OnSpellTechDisciplineTokenTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSelectedSpellTechDisciplineEntryChanged(GameDataQuickPickEntry? value)
    {
        if (
            value is not null
            && !SpellTechDisciplineTokenText.Equals(value.ApplyValueText, StringComparison.OrdinalIgnoreCase)
        )
            SpellTechDisciplineTokenText = value.ApplyValueText;
    }

    partial void OnSpellTechDisciplineLevelTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSelectedSpellTechDisciplineLevelOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is not null && !SpellTechDisciplineLevelText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            SpellTechDisciplineLevelText = value.Token;
    }

    partial void OnSpellTechSkillTokenTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSelectedSpellTechSkillEntryChanged(GameDataQuickPickEntry? value)
    {
        if (
            value is not null
            && !SpellTechSkillTokenText.Equals(value.ApplyValueText, StringComparison.OrdinalIgnoreCase)
        )
            SpellTechSkillTokenText = value.ApplyValueText;
    }

    partial void OnSpellTechSkillPointsTextChanged(string value)
    {
        SyncSpellTechSelections();
        RefreshSpellTechEditorActions();
    }

    partial void OnSelectedSpellTechSkillPointOptionChanged(DebuggerChoiceOption? value)
    {
        if (value is not null && !SpellTechSkillPointsText.Equals(value.Token, StringComparison.OrdinalIgnoreCase))
            SpellTechSkillPointsText = value.Token;
    }

    partial void OnSpellTechTimeoutTextChanged(string value) => RefreshSpellTechEditorActions();

    [RelayCommand]
    private async Task AddSpell()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSpellTechMutation(
                "No active session",
                ["Attach to a validated runtime before editing spell or tech progression."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSpellTechMutation(
                "Spell/tech editor unavailable",
                [CreateSpellTechEditorAvailabilitySummary(session)]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _spellTechEditorService.AddSpell(
                    new SpellAddRequest(
                        session,
                        SpellTechTargetHandleText,
                        SpellTechSpellTokenText,
                        SpellTechTimeoutText
                    )
                )
            );
            ApplySpellTechMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                await ReloadSpellTechLiveStateIfLoadedAsync(session);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSpellTechMutation("Spell add failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task GrantSchematic()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSpellTechMutation(
                "No active session",
                ["Attach to a validated runtime before editing spell or tech progression."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSpellTechMutation(
                "Spell/tech editor unavailable",
                [CreateSpellTechEditorAvailabilitySummary(session)]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _spellTechEditorService.GrantSchematic(
                    new SchematicGrantRequest(
                        session,
                        SpellTechTargetHandleText,
                        SpellTechSchematicIdText,
                        SpellTechTimeoutText
                    )
                )
            );
            ApplySpellTechMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                await ReloadSpellTechLiveStateIfLoadedAsync(session);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSpellTechMutation("Schematic grant failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task RemoveSchematic()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSpellTechMutation(
                "No active session",
                ["Attach to a validated runtime before editing spell or tech progression."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSpellTechMutation(
                "Spell/tech editor unavailable",
                [CreateSpellTechEditorAvailabilitySummary(session)]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _spellTechEditorService.RemoveSchematic(
                    new SchematicRemoveRequest(
                        session,
                        SpellTechTargetHandleText,
                        SpellTechSchematicIdText,
                        SpellTechTimeoutText
                    )
                )
            );
            ApplySpellTechMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                await ReloadSpellTechLiveStateIfLoadedAsync(session);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSpellTechMutation("Schematic removal failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task SetSpellCollegeLevel()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSpellTechMutation(
                "No active session",
                ["Attach to a validated runtime before editing spell or tech progression."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSpellTechMutation(
                "Spell/tech editor unavailable",
                [CreateSpellTechEditorAvailabilitySummary(session)]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _spellTechEditorService.SetSpellCollegeLevel(
                    new SpellCollegeWriteRequest(
                        session,
                        SpellTechTargetHandleText,
                        SpellTechCollegeTokenText,
                        SpellTechCollegeLevelText,
                        SpellTechTimeoutText
                    )
                )
            );
            ApplySpellTechMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                await ReloadSpellTechLiveStateIfLoadedAsync(session);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSpellTechMutation("Spell college update failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task SetTechDisciplineLevel()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSpellTechMutation(
                "No active session",
                ["Attach to a validated runtime before editing spell or tech progression."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSpellTechMutation(
                "Spell/tech editor unavailable",
                [CreateSpellTechEditorAvailabilitySummary(session)]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _spellTechEditorService.SetTechDisciplineLevel(
                    new TechDisciplineWriteRequest(
                        session,
                        SpellTechTargetHandleText,
                        SpellTechDisciplineTokenText,
                        SpellTechDisciplineLevelText,
                        SpellTechTimeoutText
                    )
                )
            );
            ApplySpellTechMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                await ReloadSpellTechLiveStateIfLoadedAsync(session);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSpellTechMutation("Tech discipline update failed", [ex.Message]);
        }
    }

    [RelayCommand]
    private async Task SetTechSkillPoints()
    {
        if (ActiveSession is not { } session)
        {
            ApplyDormantSpellTechMutation(
                "No active session",
                ["Attach to a validated runtime before editing spell or tech progression."]
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ApplyDormantSpellTechMutation(
                "Spell/tech editor unavailable",
                [CreateSpellTechEditorAvailabilitySummary(session)]
            );
            return;
        }

        try
        {
            var snapshot = await Task.Run(() =>
                _spellTechEditorService.SetTechSkillPoints(
                    new TechSkillWriteRequest(
                        session,
                        SpellTechTargetHandleText,
                        SpellTechSkillTokenText,
                        SpellTechSkillPointsText,
                        SpellTechTimeoutText
                    )
                )
            );
            ApplySpellTechMutationSnapshot(snapshot);
            if (snapshot.IsAvailable && !string.IsNullOrWhiteSpace(snapshot.TargetHandleText))
            {
                ObjectProbeHandleText = snapshot.TargetHandleText;
                await ReloadSpellTechLiveStateIfLoadedAsync(session);
            }
        }
        catch (Exception ex)
        {
            ApplyDormantSpellTechMutation("Tech skill update failed", [ex.Message]);
        }
    }

    private void ApplySpellTechMutationSnapshot(SpellTechMutationSnapshot snapshot)
    {
        SpellTechMutationStatusText = snapshot.Status;
        SpellTechMutationDispatcherText = snapshot.DispatcherText;
        SpellTechMutationExecutionDetailText = snapshot.ExecutionDetailText;
        SpellTechMutationResultText = snapshot.ResultText;
        SpellTechMutationResultLines = CreateSpellTechMutationLines(snapshot);
        RefreshSpellTechEditorActions();
    }

    private void ApplyDormantSpellTechMutation(string status, IReadOnlyList<string> lines)
    {
        SpellTechMutationStatusText = status;
        SpellTechMutationDispatcherText = "Dispatcher result unavailable.";
        SpellTechMutationExecutionDetailText =
            "Target address and hook details will appear here after a live spell or tech mutation.";
        SpellTechMutationResultText = "Mutation result values will appear here after a live spell or tech mutation.";
        SpellTechMutationResultLines = lines;
        RefreshSpellTechEditorActions();
    }

    private void RefreshSpellTechEditorActions()
    {
        SyncSpellTechSelections();
        var hasWritableSession = ActiveSession is { } session && CanInvokeFunctions(session);
        var hasTarget = !string.IsNullOrWhiteSpace(SpellTechTargetHandleText);
        CanAddSpell = hasWritableSession && hasTarget && !string.IsNullOrWhiteSpace(SpellTechSpellTokenText);
        CanGrantSchematic = hasWritableSession && hasTarget && !string.IsNullOrWhiteSpace(SpellTechSchematicIdText);
        CanRemoveSchematic = hasWritableSession && hasTarget && !string.IsNullOrWhiteSpace(SpellTechSchematicIdText);
        CanLoadSpellTechLiveState = hasWritableSession && hasTarget;
        CanSetSpellCollegeLevel =
            hasWritableSession
            && hasTarget
            && !string.IsNullOrWhiteSpace(SpellTechCollegeTokenText)
            && !string.IsNullOrWhiteSpace(SpellTechCollegeLevelText);
        CanSetTechDisciplineLevel =
            hasWritableSession
            && hasTarget
            && !string.IsNullOrWhiteSpace(SpellTechDisciplineTokenText)
            && !string.IsNullOrWhiteSpace(SpellTechDisciplineLevelText);
        CanSetTechSkillPoints =
            hasWritableSession
            && hasTarget
            && !string.IsNullOrWhiteSpace(SpellTechSkillTokenText)
            && !string.IsNullOrWhiteSpace(SpellTechSkillPointsText);
    }

    private void RefreshSpellTechSelectionLists()
    {
        var selectedSpellKey = SelectedSpellTechSpellEntry?.EntryKey;
        var selectedSchematicKey = SelectedSpellTechSchematicCatalogEntry?.EntryKey;
        SpellTechSpellEntries = GameDataQuickPickCatalog.BuildSpellEntries(
            SpellTechSpellFilterText,
            MaxSpellTechSpellCatalogEntries
        );
        SpellTechSchematicCatalogEntries =
            _gameDataCatalogPrototypeCache.Count == 0
                ? []
                : GameDataQuickPickCatalog.BuildSchematicEntries(
                    _gameDataCatalogPrototypeCache,
                    SpellTechSchematicFilterText,
                    MaxSpellTechSchematicCatalogEntries
                );
        HasSpellTechSchematicCatalogEntries = SpellTechSchematicCatalogEntries.Count != 0;
        SelectedSpellTechSpellEntry = ResolveSpellTechQuickPickSelection(
            SpellTechSpellEntries,
            SpellTechSpellTokenText,
            selectedSpellKey
        );
        SelectedSpellTechSchematicCatalogEntry = ResolveSpellTechQuickPickSelection(
            SpellTechSchematicCatalogEntries,
            SpellTechSchematicIdText,
            selectedSchematicKey
        );
        SyncSpellTechSelections();
    }

    private void SyncSpellTechSelections()
    {
        SelectedSpellTechSpellEntry = ResolveSpellTechQuickPickSelection(
            SpellTechSpellEntries,
            SpellTechSpellTokenText,
            SelectedSpellTechSpellEntry?.EntryKey
        );
        SelectedSpellTechSchematicCatalogEntry = ResolveSpellTechQuickPickSelection(
            SpellTechSchematicCatalogEntries,
            SpellTechSchematicIdText,
            SelectedSpellTechSchematicCatalogEntry?.EntryKey
        );
        SelectedSpellTechCollegeEntry = ResolveSpellTechQuickPickSelection(
            SpellTechCollegeEntries,
            SpellTechCollegeTokenText,
            SelectedSpellTechCollegeEntry?.EntryKey
        );
        SelectedSpellTechDisciplineEntry = ResolveSpellTechQuickPickSelection(
            SpellTechDisciplineEntries,
            SpellTechDisciplineTokenText,
            SelectedSpellTechDisciplineEntry?.EntryKey
        );
        SelectedSpellTechSkillEntry = ResolveSpellTechQuickPickSelection(
            SpellTechSkillEntries,
            SpellTechSkillTokenText,
            SelectedSpellTechSkillEntry?.EntryKey
        );
        SelectedSpellTechCollegeLevelOption = ResolveSpellTechChoiceSelection(
            SpellTechCollegeLevelOptions,
            SpellTechCollegeLevelText,
            SelectedSpellTechCollegeLevelOption?.Token
        );
        SelectedSpellTechDisciplineLevelOption = ResolveSpellTechChoiceSelection(
            SpellTechDisciplineLevelOptions,
            SpellTechDisciplineLevelText,
            SelectedSpellTechDisciplineLevelOption?.Token
        );
        SelectedSpellTechSkillPointOption = ResolveSpellTechChoiceSelection(
            SpellTechSkillPointOptions,
            SpellTechSkillPointsText,
            SelectedSpellTechSkillPointOption?.Token
        );
    }

    private static string CreateSpellTechEditorAvailabilitySummary(AttachedSessionSnapshot session)
    {
        if (session.HasExited)
            return "The attached process has exited, so live spell and tech edits are unavailable until a new session is attached.";

        return CanInvokeFunctions(session)
            ? "Force-learn spells, grant or remove schematic ids, or write spell and technology progression directly through native runtime hooks."
            : "This session does not currently expose live function-invocation capability, so the spell and tech editor stays disabled.";
    }

    private static IReadOnlyList<string> CreateSpellTechMutationLines(SpellTechMutationSnapshot snapshot)
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

        lines.AddRange(snapshot.Notes.Take(4).Select(static note => $"Note: {note}"));
        return lines;
    }

    private static IReadOnlyList<DebuggerChoiceOption> CreateSpellTechNumericOptions(
        int minimumInclusive,
        int maximumInclusive,
        string labelPrefix
    ) =>
        [
            .. Enumerable
                .Range(minimumInclusive, (maximumInclusive - minimumInclusive) + 1)
                .Select(value => new DebuggerChoiceOption(
                    value.ToString(CultureInfo.InvariantCulture),
                    value.ToString(CultureInfo.InvariantCulture),
                    $"{labelPrefix} {value.ToString(CultureInfo.InvariantCulture)}"
                )),
        ];

    private static GameDataQuickPickEntry? ResolveSpellTechQuickPickSelection(
        IReadOnlyList<GameDataQuickPickEntry> entries,
        string? tokenText,
        string? existingEntryKey
    )
    {
        if (!string.IsNullOrWhiteSpace(tokenText))
        {
            var exactMatch = entries.FirstOrDefault(entry =>
                entry.ApplyValueText.Equals(tokenText.Trim(), StringComparison.OrdinalIgnoreCase)
            );
            if (exactMatch is not null)
                return exactMatch;
        }

        if (string.IsNullOrWhiteSpace(existingEntryKey))
            return null;

        return entries.FirstOrDefault(entry =>
            entry.EntryKey.Equals(existingEntryKey, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static DebuggerChoiceOption? ResolveSpellTechChoiceSelection(
        IReadOnlyList<DebuggerChoiceOption> options,
        string? tokenText,
        string? existingToken
    )
    {
        if (!string.IsNullOrWhiteSpace(tokenText))
        {
            var exactMatch = options.FirstOrDefault(option =>
                option.Token.Equals(tokenText.Trim(), StringComparison.OrdinalIgnoreCase)
            );
            if (exactMatch is not null)
                return exactMatch;
        }

        if (string.IsNullOrWhiteSpace(existingToken))
            return null;

        return options.FirstOrDefault(option => option.Token.Equals(existingToken, StringComparison.OrdinalIgnoreCase));
    }

    private const int MaxSpellTechChoiceEntries = 32;
    private const int MaxSpellTechSpellCatalogEntries = 96;
    private const int MaxSpellTechSchematicCatalogEntries = 160;
}
