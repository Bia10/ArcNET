using System.Globalization;
using System.Linq;
using ArcNET.Diagnostics;
using ArcNET.GameData.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcanumDebugger.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private string? _spellTechLiveStateSourceTokenText;

    [ObservableProperty]
    private string spellTechLiveStateStatusText = "Live progression not loaded.";

    [ObservableProperty]
    private string spellTechLiveStateSummaryText =
        "Load current spell colleges, known spells, schematics, tech disciplines, and tech skills for player or a selected companion.";

    [ObservableProperty]
    private IReadOnlyList<DebuggerSpellTechLiveEntry> spellTechProgressEntries = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerSpellTechKnownSpellEntry> spellTechKnownSpellEntries = [];

    [ObservableProperty]
    private IReadOnlyList<DebuggerSpellTechSchematicEntry> spellTechSchematicEntries = [];

    [ObservableProperty]
    private bool hasSpellTechProgressEntries;

    [ObservableProperty]
    private bool hasSpellTechKnownSpellEntries;

    [ObservableProperty]
    private bool hasSpellTechSchematicEntries;

    [ObservableProperty]
    private bool canLoadSpellTechLiveState;

    [RelayCommand]
    private async Task LoadSpellTechLiveState()
    {
        if (ActiveSession is not { } session)
        {
            ResetSpellTechLiveState(
                "No active session",
                "Attach to a validated runtime before reading live spell or technology progression."
            );
            return;
        }

        if (!CanInvokeFunctions(session))
        {
            ResetSpellTechLiveState("Live progression unavailable", CreateSpellTechEditorAvailabilitySummary(session));
            return;
        }

        if (string.IsNullOrWhiteSpace(SpellTechTargetHandleText))
        {
            ResetSpellTechLiveState(
                "Target required",
                "Provide a player or companion handle before loading live spell or technology progression."
            );
            return;
        }

        try
        {
            var snapshot = await LoadSpellTechLiveStateSnapshotAsync(
                session,
                SpellTechTargetHandleText.Trim(),
                _gameDataCatalogPrototypeCache
            );
            ApplySpellTechLiveState(snapshot);
        }
        catch (Exception ex)
        {
            ResetSpellTechLiveState("Live progression load failed", ex.Message);
        }
    }

    [RelayCommand]
    private void UseSpellTechLiveEntry(DebuggerSpellTechLiveEntry? entry)
    {
        if (entry is null)
            return;

        switch (entry.Kind)
        {
            case SpellTechLiveEntryKind.SpellCollege:
                SpellTechCollegeTokenText = entry.TokenText;
                SpellTechCollegeLevelText = entry.NumericValueText;
                SpellTechLiveStateSummaryText =
                    $"{entry.TitleText} now fills the spell-college editor with the current live rank from the selected target.";
                break;

            case SpellTechLiveEntryKind.TechDiscipline:
                SpellTechDisciplineTokenText = entry.TokenText;
                SpellTechDisciplineLevelText = entry.NumericValueText;
                SpellTechLiveStateSummaryText =
                    $"{entry.TitleText} now fills the tech-discipline editor with the current live degree from the selected target.";
                break;

            case SpellTechLiveEntryKind.TechSkill:
                SpellTechSkillTokenText = entry.TokenText;
                SpellTechSkillPointsText = entry.NumericValueText;
                SpellTechLiveStateSummaryText =
                    $"{entry.TitleText} now fills the tech-skill editor with the current live point total from the selected target.";
                break;
        }
    }

    [RelayCommand]
    private void UseSpellTechKnownSpell(DebuggerSpellTechKnownSpellEntry? entry)
    {
        if (entry is null)
            return;

        SpellTechSpellTokenText = entry.TokenText;
        SpellTechLiveStateSummaryText =
            $"{entry.TitleText} now fills the spell editor from the selected target's known live spellbook.";
    }

    [RelayCommand]
    private void UseSpellTechSchematic(DebuggerSpellTechSchematicEntry? entry)
    {
        if (entry is null)
            return;

        SpellTechSchematicIdText = entry.ValueText;
        SpellTechLiveStateSummaryText =
            $"{entry.TitleText} now fills the schematic editor with one known live schematic id from slot {entry.SlotIndex.ToString(CultureInfo.InvariantCulture)}.";
    }

    private void InvalidateSpellTechLiveStateIfTargetChanged(string? targetHandleToken)
    {
        if (string.IsNullOrWhiteSpace(_spellTechLiveStateSourceTokenText))
            return;

        var normalizedToken = targetHandleToken?.Trim() ?? string.Empty;
        if (normalizedToken.Equals(_spellTechLiveStateSourceTokenText, StringComparison.OrdinalIgnoreCase))
            return;

        ResetSpellTechLiveState(
            "Live progression not loaded.",
            "The loaded spell-tech browser belonged to a different target. Load current progression for this player or companion to refresh it."
        );
    }

    private async Task ReloadSpellTechLiveStateIfLoadedAsync(AttachedSessionSnapshot session)
    {
        if (string.IsNullOrWhiteSpace(_spellTechLiveStateSourceTokenText))
            return;

        try
        {
            var snapshot = await LoadSpellTechLiveStateSnapshotAsync(
                session,
                SpellTechTargetHandleText.Trim(),
                _gameDataCatalogPrototypeCache
            );
            ApplySpellTechLiveState(snapshot);
        }
        catch (Exception ex)
        {
            ResetSpellTechLiveState("Live progression refresh failed", ex.Message);
        }
    }

    private async Task<DebuggerSpellTechLiveSnapshot> LoadSpellTechLiveStateSnapshotAsync(
        AttachedSessionSnapshot session,
        string targetTokenText,
        IReadOnlyList<PrototypePaletteEntry> prototypeEntries
    ) =>
        await Task.Run(() =>
        {
            var sheetSnapshot = _sheetService.Scan(new SheetScanRequest(session, targetTokenText));
            if (!sheetSnapshot.IsAvailable)
            {
                return new DebuggerSpellTechLiveSnapshot(
                    IsAvailable: false,
                    sheetSnapshot.Status,
                    sheetSnapshot.Summary,
                    targetTokenText,
                    [],
                    [],
                    []
                );
            }

            var fieldLengthSnapshot = _readService.Read(
                new ReadRequest(session, "field-length", [targetTokenText, "OBJ_F_PC_SCHEMATICS_FOUND_IDX"])
            );
            if (!fieldLengthSnapshot.IsAvailable)
            {
                return new DebuggerSpellTechLiveSnapshot(
                    IsAvailable: false,
                    fieldLengthSnapshot.Status,
                    fieldLengthSnapshot.Summary,
                    targetTokenText,
                    [],
                    [],
                    []
                );
            }

            var schematicCount = Math.Max(0, TryReadInt32(fieldLengthSnapshot, "length"));
            List<int> schematicIds = [];
            var scanCount = Math.Min(schematicCount, MaxSpellTechSchematicScanCount);
            for (var slotIndex = 0; slotIndex < scanCount; slotIndex++)
            {
                var schematicSnapshot = _readService.Read(
                    new ReadRequest(
                        session,
                        "schematic",
                        [targetTokenText, slotIndex.ToString(CultureInfo.InvariantCulture)]
                    )
                );
                if (!schematicSnapshot.IsAvailable)
                {
                    return new DebuggerSpellTechLiveSnapshot(
                        IsAvailable: false,
                        schematicSnapshot.Status,
                        schematicSnapshot.Summary,
                        targetTokenText,
                        [],
                        [],
                        []
                    );
                }

                var schematicId = TryReadInt32(schematicSnapshot, "schematic_id");
                if (schematicId > 0)
                    schematicIds.Add(schematicId);
            }

            var progressEntries = SpellTechLiveCatalog.CreateProgressionEntries(sheetSnapshot.Data);
            var knownSpellEntries = SpellTechLiveCatalog.CreateKnownSpellEntries(sheetSnapshot.Data);
            var schematicEntries = SpellTechLiveCatalog.CreateSchematicEntries(schematicIds, prototypeEntries);
            var targetText = string.IsNullOrWhiteSpace(sheetSnapshot.TargetText)
                ? "the selected target"
                : sheetSnapshot.TargetText;
            var summary =
                $"Loaded {knownSpellEntries.Count.ToString(CultureInfo.InvariantCulture)} known spells, {progressEntries.Count.ToString(CultureInfo.InvariantCulture)} live progression rows, and {schematicEntries.Count.ToString(CultureInfo.InvariantCulture)} known schematic ids for {targetText}."
                + (
                    schematicCount > scanCount
                        ? $" The schematic scan stopped at {scanCount.ToString(CultureInfo.InvariantCulture)} slots."
                        : string.Empty
                );
            return new DebuggerSpellTechLiveSnapshot(
                IsAvailable: true,
                "Live progression loaded",
                summary,
                targetTokenText,
                progressEntries,
                knownSpellEntries,
                schematicEntries
            );
        });

    private void ApplySpellTechLiveState(DebuggerSpellTechLiveSnapshot snapshot)
    {
        SpellTechLiveStateStatusText = snapshot.StatusText;
        SpellTechLiveStateSummaryText = snapshot.SummaryText;
        SpellTechProgressEntries = snapshot.ProgressEntries;
        SpellTechKnownSpellEntries = snapshot.KnownSpellEntries;
        SpellTechSchematicEntries = snapshot.SchematicEntries;
        HasSpellTechProgressEntries = snapshot.ProgressEntries.Count != 0;
        HasSpellTechKnownSpellEntries = snapshot.KnownSpellEntries.Count != 0;
        HasSpellTechSchematicEntries = snapshot.SchematicEntries.Count != 0;
        _spellTechLiveStateSourceTokenText = snapshot.IsAvailable ? snapshot.SourceTokenText : null;
        RefreshSpellTechEditorActions();
    }

    private void ResetSpellTechLiveState(string statusText, string summaryText)
    {
        SpellTechLiveStateStatusText = statusText;
        SpellTechLiveStateSummaryText = summaryText;
        SpellTechProgressEntries = [];
        SpellTechKnownSpellEntries = [];
        SpellTechSchematicEntries = [];
        HasSpellTechProgressEntries = false;
        HasSpellTechKnownSpellEntries = false;
        HasSpellTechSchematicEntries = false;
        _spellTechLiveStateSourceTokenText = null;
        RefreshSpellTechEditorActions();
    }

    private static int TryReadInt32(ReadSnapshot snapshot, string key)
    {
        var valueText = TryReadValueText(snapshot, key);
        return int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static string? TryReadValueText(ReadSnapshot snapshot, string key)
    {
        var value = snapshot.Values.FirstOrDefault(candidate => candidate.Key == key);
        return string.IsNullOrWhiteSpace(value.Key) ? null : value.ValueText;
    }

    private const int MaxSpellTechSchematicScanCount = 256;

    private readonly record struct DebuggerSpellTechLiveSnapshot(
        bool IsAvailable,
        string StatusText,
        string SummaryText,
        string SourceTokenText,
        IReadOnlyList<DebuggerSpellTechLiveEntry> ProgressEntries,
        IReadOnlyList<DebuggerSpellTechKnownSpellEntry> KnownSpellEntries,
        IReadOnlyList<DebuggerSpellTechSchematicEntry> SchematicEntries
    );
}
