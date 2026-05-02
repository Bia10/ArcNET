using System.Buffers.Binary;
using ArcNET.Core.Primitives;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor;

/// <summary>
/// Live mutable session layered on top of an <see cref="EditorWorkspace"/>.
/// It reuses transactional editors for individual assets and can report dirty state across the workspace.
/// </summary>
public sealed class EditorWorkspaceSession
{
    private const int SectorTileAxisLength = 64;
    private const int SectorRoofAxisLength = 16;

    private readonly Dictionary<string, DialogEditor> _dialogEditors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ScriptEditor> _scriptEditors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProtoData> _pendingProtoAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MobData> _pendingMobAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Sector> _pendingSectorAssets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<EditorWorkspaceSessionHistoryFrame> _undoSnapshots = new();
    private readonly Stack<EditorWorkspaceSessionHistoryFrame> _redoSnapshots = new();
    private readonly Stack<EditorSessionStagedHistoryScopeKey> _undoStagedHistoryScopes = new();
    private readonly Stack<EditorSessionStagedHistoryScopeKey> _redoStagedHistoryScopes = new();
    private readonly Stack<EditorWorkspaceSessionDirectAssetSnapshot> _undoDirectAssetSnapshots = new();
    private readonly Stack<EditorWorkspaceSessionDirectAssetSnapshot> _redoDirectAssetSnapshots = new();
    private IReadOnlyList<EditorProjectOpenAsset> _projectOpenAssets = [];
    private IReadOnlyList<EditorProjectBookmark> _projectBookmarks = [];
    private IReadOnlyList<EditorProjectMapViewState> _projectMapViewStates = [];
    private IReadOnlyList<EditorProjectViewState> _projectViewStates = [];
    private IReadOnlyList<EditorProjectToolState> _projectToolStates = [];
    private string? _projectActiveAssetPath;
    private SaveGameEditor? _saveEditor;

    /// <summary>
    /// Initializes a live session for one loaded workspace snapshot.
    /// </summary>
    public EditorWorkspaceSession(EditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        Workspace = workspace;
    }

    /// <summary>
    /// Loaded workspace snapshot that owns this session.
    /// </summary>
    public EditorWorkspace Workspace { get; private set; }

    /// <summary>
    /// Returns <see langword="true"/> when the session has one or more applied change groups that can be undone.
    /// </summary>
    public bool CanUndo => _undoSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the session has one or more undone change groups that can be redone.
    /// </summary>
    public bool CanRedo => _redoSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more staged direct proto, mob, or sector edits
    /// can be undone before apply or save.
    /// </summary>
    public bool CanUndoDirectAssetChanges => _undoDirectAssetSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more undone staged direct proto, mob, or sector edits
    /// can be reapplied before apply or save.
    /// </summary>
    public bool CanRedoDirectAssetChanges => _redoDirectAssetSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when the session can undo one staged local edit across its merged dialog,
    /// script, save, and direct-asset history scopes.
    /// </summary>
    public bool CanUndoStagedChanges => TryPeekMergedStagedHistoryScope(canUndo: true, GetStagedHistoryScopes(), out _);

    /// <summary>
    /// Returns <see langword="true"/> when the session can redo one staged local edit across its merged dialog,
    /// script, save, and direct-asset history scopes.
    /// </summary>
    public bool CanRedoStagedChanges =>
        TryPeekMergedStagedHistoryScope(canUndo: false, GetStagedHistoryScopes(), out _);

    /// <summary>
    /// Returns <see langword="true"/> when one or more tracked editors currently have staged edits.
    /// </summary>
    public bool HasPendingChanges =>
        _dialogEditors.Values.Any(static editor => editor.HasPendingChanges)
        || _scriptEditors.Values.Any(static editor => editor.HasPendingChanges)
        || _pendingProtoAssets.Count > 0
        || _pendingMobAssets.Count > 0
        || _pendingSectorAssets.Count > 0
        || _saveEditor?.HasPendingChanges == true;

    /// <summary>
    /// Returns <see langword="true"/> when the current staged session head can be applied through the
    /// existing whole-session apply/save pathway.
    /// </summary>
    public bool CanApplyPendingChanges => HasPendingChanges && !GetPendingValidation().HasErrors;

    /// <summary>
    /// Returns <see langword="true"/> when the current staged session head can be discarded through the
    /// existing whole-session discard pathway.
    /// </summary>
    public bool CanDiscardPendingChanges => HasPendingChanges;

    /// <summary>
    /// Creates one explicit, optionally labeled session change group.
    /// Apply or save the staged changes through the returned group to record the label in history.
    /// </summary>
    public EditorSessionChangeGroup BeginChangeGroup(string? label = null) => new(this, label);

    /// <summary>
    /// Returns the undo history, ordered from the most recent change group to the oldest.
    /// </summary>
    public IReadOnlyList<EditorSessionHistoryEntry> GetUndoHistory() =>
        [.. _undoSnapshots.Select(static frame => frame.Entry)];

    /// <summary>
    /// Returns the redo history, ordered from the most recent undone change group to the oldest.
    /// </summary>
    public IReadOnlyList<EditorSessionHistoryEntry> GetRedoHistory() =>
        [.. _redoSnapshots.Select(static frame => frame.Entry)];

    /// <summary>
    /// Returns the session's current normalized project/session state summary.
    /// </summary>
    public EditorSessionProjectStateSummary GetProjectStateSummary() => CreateProjectStateSummary();

    /// <summary>
    /// Returns a unified host-facing bootstrap snapshot of the current session shell state.
    /// </summary>
    public EditorSessionBootstrapSummary GetBootstrapSummary() => CreateBootstrapSummary(restore: null);

    /// <summary>
    /// Returns a unified host-facing bootstrap snapshot of the current session shell state together
    /// with the supplied restore summary.
    /// </summary>
    public EditorSessionBootstrapSummary GetBootstrapSummary(EditorProjectRestoreResult restore)
    {
        ArgumentNullException.ThrowIfNull(restore);
        return CreateBootstrapSummary(restore);
    }

    /// <summary>
    /// Returns the default applied undo command that a host can bind directly.
    /// Returns <see langword="null"/> when no applied undo is currently available.
    /// </summary>
    public EditorSessionHistoryCommandSummary? GetDefaultUndoHistoryCommandSummary() =>
        CreateDefaultHistoryCommandSummary(EditorSessionHistoryCommandKind.Undo);

    /// <summary>
    /// Returns the default applied redo command that a host can bind directly.
    /// Returns <see langword="null"/> when no applied redo is currently available.
    /// </summary>
    public EditorSessionHistoryCommandSummary? GetDefaultRedoHistoryCommandSummary() =>
        CreateDefaultHistoryCommandSummary(EditorSessionHistoryCommandKind.Redo);

    /// <summary>
    /// Returns the currently executable applied history commands in stable order.
    /// Undo is listed before redo when both are available.
    /// </summary>
    public IReadOnlyList<EditorSessionHistoryCommandSummary> GetHistoryCommandSummaries()
    {
        var commands = new List<EditorSessionHistoryCommandSummary>(capacity: 2);

        var undo = GetDefaultUndoHistoryCommandSummary();
        if (undo is not null)
            commands.Add(undo);

        var redo = GetDefaultRedoHistoryCommandSummary();
        if (redo is not null)
            commands.Add(redo);

        return commands;
    }

    /// <summary>
    /// Returns the preferred staged-history scope for an undo command based on the active asset,
    /// tracked project state, and the merged local history order.
    /// Returns <see langword="null"/> when no local scope can currently undo.
    /// </summary>
    public EditorSessionStagedHistoryScope? GetPreferredUndoStagedHistoryScope() =>
        GetPreferredStagedHistoryScope(canUndo: true);

    /// <summary>
    /// Returns the preferred staged-history scope for a redo command based on the active asset,
    /// tracked project state, and the merged local history order.
    /// Returns <see langword="null"/> when no local scope can currently redo.
    /// </summary>
    public EditorSessionStagedHistoryScope? GetPreferredRedoStagedHistoryScope() =>
        GetPreferredStagedHistoryScope(canUndo: false);

    /// <summary>
    /// Returns the preferred staged transaction for an undo command based on the active asset,
    /// tracked project state, and the merged local history order.
    /// Returns <see langword="null"/> when no staged transaction can currently undo.
    /// </summary>
    public EditorSessionStagedTransactionSummary? GetPreferredUndoStagedTransactionSummary() =>
        GetPreferredStagedTransactionSummary(canUndo: true);

    /// <summary>
    /// Returns the preferred staged transaction for a redo command based on the active asset,
    /// tracked project state, and the merged local history order.
    /// Returns <see langword="null"/> when no staged transaction can currently redo.
    /// </summary>
    public EditorSessionStagedTransactionSummary? GetPreferredRedoStagedTransactionSummary() =>
        GetPreferredStagedTransactionSummary(canUndo: false);

    /// <summary>
    /// Returns the default staged undo command that a host can bind directly.
    /// Returns <see langword="null"/> when no staged undo is currently available.
    /// </summary>
    public EditorSessionStagedCommandSummary? GetDefaultUndoStagedCommandSummary() =>
        CreateDefaultStagedCommandSummary(EditorSessionStagedCommandKind.Undo);

    /// <summary>
    /// Returns the default staged redo command that a host can bind directly.
    /// Returns <see langword="null"/> when no staged redo is currently available.
    /// </summary>
    public EditorSessionStagedCommandSummary? GetDefaultRedoStagedCommandSummary() =>
        CreateDefaultStagedCommandSummary(EditorSessionStagedCommandKind.Redo);

    /// <summary>
    /// Returns the current default staged command inventory in stable order.
    /// Undo is listed before redo when both are available.
    /// </summary>
    public IReadOnlyList<EditorSessionStagedCommandSummary> GetStagedCommandSummaries()
    {
        var commands = new List<EditorSessionStagedCommandSummary>(capacity: 2);

        var undo = GetDefaultUndoStagedCommandSummary();
        if (undo is not null)
            commands.Add(undo);

        var redo = GetDefaultRedoStagedCommandSummary();
        if (redo is not null)
            commands.Add(redo);

        return commands;
    }

    /// <summary>
    /// Returns every currently executable staged undo command in stable host-facing order.
    /// The preferred default undo command, when any, is listed first.
    /// </summary>
    public IReadOnlyList<EditorSessionStagedCommandSummary> GetUndoStagedCommandSummaries() =>
        CreateAvailableStagedCommandSummaries(EditorSessionStagedCommandKind.Undo);

    /// <summary>
    /// Returns every currently executable staged redo command in stable host-facing order.
    /// The preferred default redo command, when any, is listed first.
    /// </summary>
    public IReadOnlyList<EditorSessionStagedCommandSummary> GetRedoStagedCommandSummaries() =>
        CreateAvailableStagedCommandSummaries(EditorSessionStagedCommandKind.Redo);

    /// <summary>
    /// Returns every currently executable staged command in stable host-facing order.
    /// Undo commands are listed before redo commands, and each group keeps its preferred default first.
    /// </summary>
    public IReadOnlyList<EditorSessionStagedCommandSummary> GetAvailableStagedCommandSummaries()
    {
        var commands = new List<EditorSessionStagedCommandSummary>();
        commands.AddRange(GetUndoStagedCommandSummaries());
        commands.AddRange(GetRedoStagedCommandSummaries());
        return commands;
    }

    /// <summary>
    /// Returns the currently tracked local staged-history scopes that hosts can inspect and drive through this session.
    /// Dialog and script scopes are reported per tracked editor, the save scope is reported when the save editor
    /// has been created, and the direct-asset scope is reported when it has pending state or local draft history.
    /// </summary>
    public IReadOnlyList<EditorSessionStagedHistoryScope> GetStagedHistoryScopes() => CollectStagedHistoryScopes();

    /// <summary>
    /// Returns the currently tracked staged transactions that hosts can inspect without branching between
    /// dialog, script, save, and direct-asset editor types before rendering a pending-work view.
    /// This surface is additive to <see cref="GetStagedHistoryScopes()"/> and preserves the existing
    /// scope-local routing APIs for incremental host adoption.
    /// </summary>
    public IReadOnlyList<EditorSessionStagedTransactionSummary> GetStagedTransactionSummaries()
    {
        var scopes = CollectStagedHistoryScopes();
        if (scopes.Count == 0)
            return [];

        var canApplyPendingChanges = CanApplyPendingChanges;
        var canDiscardPendingChanges = CanDiscardPendingChanges;
        var pendingChanges = CollectPendingChangesSnapshot();
        var summaries = new List<EditorSessionStagedTransactionSummary>(scopes.Count);

        foreach (var scope in scopes)
        {
            var selectedScopeKeys = CreateSelectedScopeKeys(scope.Kind, scope.Target);
            var scopePendingChanges = GetStagedTransactionPendingChanges(scope, pendingChanges);
            var blockingValidation = scope.HasPendingChanges
                ? CreateBlockingValidationReport(GetPendingValidation(selectedScopeKeys))
                : EditorWorkspaceValidationReport.Empty;
            var repairCandidates = scope.HasPendingChanges ? GetValidationRepairCandidates(selectedScopeKeys) : [];
            summaries.Add(
                new EditorSessionStagedTransactionSummary
                {
                    Kind = scope.Kind,
                    Target = scope.Target,
                    Label = GetStagedTransactionLabel(scope),
                    AffectedTargets = GetStagedTransactionAffectedTargets(scope, scopePendingChanges),
                    PendingChanges = scopePendingChanges,
                    HasPendingChanges = scope.HasPendingChanges,
                    CanUndo = scope.CanUndo,
                    CanRedo = scope.CanRedo,
                    CanApplyFromSession = canApplyPendingChanges && scope.HasPendingChanges,
                    CanDiscardFromSession = canDiscardPendingChanges && scope.HasPendingChanges,
                    CanApplyIndividually = scope.HasPendingChanges && !blockingValidation.HasErrors,
                    CanSaveIndividually = scope.HasPendingChanges && !blockingValidation.HasErrors,
                    BlockingValidation = blockingValidation,
                    RepairCandidates = repairCandidates,
                }
            );
        }

        return summaries;
    }

    private List<EditorSessionStagedHistoryScope> CollectStagedHistoryScopes()
    {
        var scopes = new List<EditorSessionStagedHistoryScope>();

        foreach (
            var (assetPath, editor) in _dialogEditors.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            scopes.Add(
                new EditorSessionStagedHistoryScope
                {
                    Kind = EditorSessionStagedHistoryScopeKind.Dialog,
                    Target = assetPath,
                    HasPendingChanges = editor.HasPendingChanges,
                    CanUndo = editor.CanUndo,
                    CanRedo = editor.CanRedo,
                }
            );
        }

        foreach (
            var (assetPath, editor) in _scriptEditors.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            scopes.Add(
                new EditorSessionStagedHistoryScope
                {
                    Kind = EditorSessionStagedHistoryScopeKind.Script,
                    Target = assetPath,
                    HasPendingChanges = editor.HasPendingChanges,
                    CanUndo = editor.CanUndo,
                    CanRedo = editor.CanRedo,
                }
            );
        }

        if (_saveEditor is not null)
        {
            scopes.Add(
                new EditorSessionStagedHistoryScope
                {
                    Kind = EditorSessionStagedHistoryScopeKind.Save,
                    Target = GetSaveHistoryScopeTarget(),
                    HasPendingChanges = _saveEditor.HasPendingChanges,
                    CanUndo = _saveEditor.CanUndo,
                    CanRedo = _saveEditor.CanRedo,
                }
            );
        }

        if (HasPendingDirectAssetChanges || CanUndoDirectAssetChanges || CanRedoDirectAssetChanges)
        {
            scopes.Add(
                new EditorSessionStagedHistoryScope
                {
                    Kind = EditorSessionStagedHistoryScopeKind.DirectAssets,
                    Target = null,
                    HasPendingChanges = HasPendingDirectAssetChanges,
                    CanUndo = CanUndoDirectAssetChanges,
                    CanRedo = CanRedoDirectAssetChanges,
                }
            );
        }

        return scopes;
    }

    /// <summary>
    /// Undoes one staged local change in the scope described by <paramref name="scope"/>.
    /// This does not affect applied session history.
    /// </summary>
    public EditorWorkspaceSession UndoStagedChanges(EditorSessionStagedHistoryScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        switch (scope.Kind)
        {
            case EditorSessionStagedHistoryScopeKind.Dialog:
                GetTrackedDialogEditor(scope.Target).Undo();
                return this;
            case EditorSessionStagedHistoryScopeKind.Script:
                GetTrackedScriptEditor(scope.Target).Undo();
                return this;
            case EditorSessionStagedHistoryScopeKind.Save:
                GetTrackedSaveEditor().Undo();
                return this;
            case EditorSessionStagedHistoryScopeKind.DirectAssets:
                return UndoDirectAssetChanges();
            default:
                throw new InvalidOperationException($"Unsupported staged history scope {scope.Kind}.");
        }
    }

    /// <summary>
    /// Undoes one staged local change in the transaction described by <paramref name="stagedTransaction"/>.
    /// This does not affect applied session history.
    /// </summary>
    public EditorWorkspaceSession UndoStagedChanges(EditorSessionStagedTransactionSummary stagedTransaction)
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);

        if (
            !TryGetMatchingStagedHistoryScope(
                GetStagedHistoryScopes(),
                new EditorSessionStagedHistoryScopeKey(stagedTransaction.Kind, stagedTransaction.Target),
                canUndo: true,
                out var scope
            )
        )
        {
            throw new InvalidOperationException(
                $"This session has no staged transaction that can currently undo for {stagedTransaction.Label}."
            );
        }

        return UndoStagedChanges(scope);
    }

    /// <summary>
    /// Executes one staged command routed through the current preferred transaction logic.
    /// </summary>
    public EditorWorkspaceSession ExecuteStagedCommand(EditorSessionStagedCommandSummary command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.Kind switch
        {
            EditorSessionStagedCommandKind.Undo => UndoStagedChanges(command.Transaction),
            EditorSessionStagedCommandKind.Redo => RedoStagedChanges(command.Transaction),
            _ => throw new InvalidOperationException($"Unsupported staged command kind {command.Kind}."),
        };
    }

    /// <summary>
    /// Executes one applied-history command routed through the current top undo/redo history entry.
    /// </summary>
    public EditorWorkspace ExecuteHistoryCommand(EditorSessionHistoryCommandSummary command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var current = command.Kind switch
        {
            EditorSessionHistoryCommandKind.Undo => GetDefaultUndoHistoryCommandSummary(),
            EditorSessionHistoryCommandKind.Redo => GetDefaultRedoHistoryCommandSummary(),
            _ => throw new InvalidOperationException($"Unsupported history command kind {command.Kind}."),
        };

        if (current is null || !ReferenceEquals(current.Entry, command.Entry))
        {
            throw new InvalidOperationException(
                $"This session can no longer execute the requested {command.Kind} history command for {command.Entry.Label}."
            );
        }

        return command.Kind switch
        {
            EditorSessionHistoryCommandKind.Undo => Undo(),
            EditorSessionHistoryCommandKind.Redo => Redo(),
            _ => throw new InvalidOperationException($"Unsupported history command kind {command.Kind}."),
        };
    }

    /// <summary>
    /// Undoes the most recent staged local edit across the session's merged dialog, script, save,
    /// and direct-asset history scopes.
    /// This does not affect applied session history.
    /// </summary>
    public EditorWorkspaceSession UndoStagedChanges()
    {
        if (!TryPeekMergedStagedHistoryScope(canUndo: true, GetStagedHistoryScopes(), out var scope))
            throw new InvalidOperationException("This session has no staged local edit to undo.");

        return UndoStagedChanges(scope);
    }

    /// <summary>
    /// Redoes one staged local change in the scope described by <paramref name="scope"/>.
    /// This does not affect applied session history.
    /// </summary>
    public EditorWorkspaceSession RedoStagedChanges(EditorSessionStagedHistoryScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        switch (scope.Kind)
        {
            case EditorSessionStagedHistoryScopeKind.Dialog:
                GetTrackedDialogEditor(scope.Target).Redo();
                return this;
            case EditorSessionStagedHistoryScopeKind.Script:
                GetTrackedScriptEditor(scope.Target).Redo();
                return this;
            case EditorSessionStagedHistoryScopeKind.Save:
                GetTrackedSaveEditor().Redo();
                return this;
            case EditorSessionStagedHistoryScopeKind.DirectAssets:
                return RedoDirectAssetChanges();
            default:
                throw new InvalidOperationException($"Unsupported staged history scope {scope.Kind}.");
        }
    }

    /// <summary>
    /// Redoes one staged local change in the transaction described by <paramref name="stagedTransaction"/>.
    /// This does not affect applied session history.
    /// </summary>
    public EditorWorkspaceSession RedoStagedChanges(EditorSessionStagedTransactionSummary stagedTransaction)
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);

        if (
            !TryGetMatchingStagedHistoryScope(
                GetStagedHistoryScopes(),
                new EditorSessionStagedHistoryScopeKey(stagedTransaction.Kind, stagedTransaction.Target),
                canUndo: false,
                out var scope
            )
        )
        {
            throw new InvalidOperationException(
                $"This session has no staged transaction that can currently redo for {stagedTransaction.Label}."
            );
        }

        return RedoStagedChanges(scope);
    }

    /// <summary>
    /// Redoes the most recently undone staged local edit across the session's merged dialog, script, save,
    /// and direct-asset history scopes.
    /// This does not affect applied session history.
    /// </summary>
    public EditorWorkspaceSession RedoStagedChanges()
    {
        if (!TryPeekMergedStagedHistoryScope(canUndo: false, GetStagedHistoryScopes(), out var scope))
            throw new InvalidOperationException("This session has no staged local edit to redo.");

        return RedoStagedChanges(scope);
    }

    /// <summary>
    /// Normalized currently active asset path tracked by the session project state, if any.
    /// </summary>
    public string? ActiveAssetPath => _projectActiveAssetPath;

    /// <summary>
    /// Returns the session's explicit open-asset set in its current normalized order.
    /// Supported dialog and script assets are also backed by tracked editors.
    /// </summary>
    public IReadOnlyList<EditorProjectOpenAsset> GetOpenAssets() => [.. _projectOpenAssets];

    /// <summary>
    /// Returns the session's typed map-view project state.
    /// </summary>
    public IReadOnlyList<EditorProjectMapViewState> GetMapViewStates() => [.. _projectMapViewStates];

    /// <summary>
    /// Returns the persisted world-edit workflow state for one tracked map view.
    /// </summary>
    public EditorProjectMapWorldEditState GetMapWorldEditState(string mapViewStateId) =>
        NormalizeProjectMapWorldEditState(ResolveTrackedMapViewState(mapViewStateId).WorldEdit);

    /// <summary>
    /// Returns one host-facing snapshot of the tracked terrain-paint tool for one map view.
    /// </summary>
    public EditorMapTerrainToolSummary GetTrackedTerrainToolSummary(string mapViewStateId)
    {
        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var toolState = NormalizeProjectMapTerrainToolState(mapViewState.WorldEdit.Terrain);
        return new EditorMapTerrainToolSummary
        {
            MapViewStateId = mapViewState.Id,
            MapName = mapViewState.MapName,
            ToolState = toolState,
            SelectedEntry = Workspace.FindTerrainPaletteEntry(toolState),
        };
    }

    /// <summary>
    /// Tracks one terrain palette entry as the active terrain-paint tool state for one map view.
    /// </summary>
    public EditorProjectMapTerrainToolState SetTrackedTerrainPaletteEntry(
        string mapViewStateId,
        EditorTerrainPaletteEntry entry,
        bool activateTool = true
    )
    {
        ArgumentNullException.ThrowIfNull(entry);

        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var normalizedTerrainState = new EditorProjectMapTerrainToolState
        {
            MapPropertiesAssetPath = entry.Asset.AssetPath,
            PaletteX = entry.PaletteX,
            PaletteY = entry.PaletteY,
        };
        var worldEditState = NormalizeProjectMapWorldEditState(mapViewState.WorldEdit);
        _ = SetMapWorldEditState(
            mapViewStateId,
            new EditorProjectMapWorldEditState
            {
                ActiveTool = activateTool
                    ? EditorProjectMapWorldEditActiveTool.TerrainPaint
                    : worldEditState.ActiveTool,
                Terrain = normalizedTerrainState,
                ObjectPlacement = worldEditState.ObjectPlacement,
            }
        );

        return normalizedTerrainState;
    }

    /// <summary>
    /// Returns one host-facing snapshot of the tracked object-placement tool for one map view.
    /// </summary>
    public EditorMapObjectPlacementToolSummary GetTrackedObjectPlacementToolSummary(string mapViewStateId)
    {
        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var toolState = NormalizeProjectMapObjectPlacementToolState(mapViewState.WorldEdit.ObjectPlacement);
        var selectedPreset =
            toolState.Mode == EditorProjectMapObjectPlacementMode.PlacementPreset
                ? toolState.FindSelectedPreset()
                : null;
        var effectivePlacementSet = toolState.Mode switch
        {
            EditorProjectMapObjectPlacementMode.SinglePlacement when toolState.PlacementRequest is { } request =>
                new EditorObjectPalettePlacementSet { Entries = [request] },
            EditorProjectMapObjectPlacementMode.PlacementSet => toolState.PlacementSet,
            EditorProjectMapObjectPlacementMode.PlacementPreset => selectedPreset?.CreatePlacementSet(),
            _ => null,
        };

        var resolvedEntries = new List<EditorObjectPaletteEntry>();
        var missingProtoNumbers = new List<int>();
        if (effectivePlacementSet is { } placementSet)
        {
            for (var i = 0; i < placementSet.Entries.Count; i++)
            {
                var request = placementSet.Entries[i];
                var paletteEntry = Workspace.FindObjectPaletteEntry(request.ProtoNumber);
                if (paletteEntry is null)
                    missingProtoNumbers.Add(request.ProtoNumber);
                else
                    resolvedEntries.Add(paletteEntry);
            }
        }

        return new EditorMapObjectPlacementToolSummary
        {
            MapViewStateId = mapViewState.Id,
            MapName = mapViewState.MapName,
            ToolState = toolState,
            EffectivePlacementSet = effectivePlacementSet,
            SelectedPreset = selectedPreset,
            ResolvedPaletteEntries = resolvedEntries,
            MissingProtoNumbers = [.. missingProtoNumbers.Distinct()],
        };
    }

    /// <summary>
    /// Tracks one single-placement request as the active object-placement tool state for one map view.
    /// </summary>
    public EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementRequest(
        string mapViewStateId,
        EditorObjectPalettePlacementRequest request,
        bool activateTool = true
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return SetTrackedObjectPlacementToolState(
            mapViewStateId,
            new EditorProjectMapObjectPlacementToolState
            {
                Mode = EditorProjectMapObjectPlacementMode.SinglePlacement,
                PlacementRequest = NormalizePlacementRequest(request),
            },
            activateTool
        );
    }

    /// <summary>
    /// Tracks one proto-backed palette entry as the active single-placement object tool for one map view.
    /// </summary>
    public EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementEntry(
        string mapViewStateId,
        EditorObjectPaletteEntry entry,
        int deltaTileX = 0,
        int deltaTileY = 0,
        float? rotation = null,
        float? rotationPitch = null,
        bool alignToTileGrid = true,
        bool activateTool = true
    )
    {
        ArgumentNullException.ThrowIfNull(entry);

        return SetTrackedObjectPlacementRequest(
            mapViewStateId,
            entry.CreatePlacementRequest(deltaTileX, deltaTileY, rotation, rotationPitch, alignToTileGrid),
            activateTool
        );
    }

    /// <summary>
    /// Tracks one placement set as the active object-placement tool state for one map view.
    /// </summary>
    public EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementSet(
        string mapViewStateId,
        EditorObjectPalettePlacementSet placementSet,
        bool activateTool = true
    )
    {
        ArgumentNullException.ThrowIfNull(placementSet);

        return SetTrackedObjectPlacementToolState(
            mapViewStateId,
            new EditorProjectMapObjectPlacementToolState
            {
                Mode = EditorProjectMapObjectPlacementMode.PlacementSet,
                PlacementSet = NormalizePlacementSet(placementSet),
            },
            activateTool
        );
    }

    /// <summary>
    /// Upserts one preset into the tracked preset library and selects it as the active object-placement tool.
    /// </summary>
    public EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementPreset(
        string mapViewStateId,
        EditorObjectPalettePlacementPreset preset,
        bool activateTool = true
    )
    {
        ArgumentNullException.ThrowIfNull(preset);

        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var worldEditState = NormalizeProjectMapWorldEditState(mapViewState.WorldEdit);
        var normalizedPreset = NormalizePlacementPreset(preset);
        var updatedPresetLibrary = UpsertPlacementPreset(
            worldEditState.ObjectPlacement.PresetLibrary,
            normalizedPreset
        );

        return SetTrackedObjectPlacementToolState(
            mapViewStateId,
            new EditorProjectMapObjectPlacementToolState
            {
                Mode = EditorProjectMapObjectPlacementMode.PlacementPreset,
                PresetLibrary = updatedPresetLibrary,
                SelectedPresetId = normalizedPreset.PresetId,
            },
            activateTool
        );
    }

    /// <summary>
    /// Selects one existing tracked placement preset by identifier for the active object-placement tool.
    /// </summary>
    public EditorProjectMapObjectPlacementToolState SelectTrackedObjectPlacementPreset(
        string mapViewStateId,
        string presetId,
        bool activateTool = true
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(presetId);

        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var worldEditState = NormalizeProjectMapWorldEditState(mapViewState.WorldEdit);

        return SetTrackedObjectPlacementToolState(
            mapViewStateId,
            new EditorProjectMapObjectPlacementToolState
            {
                Mode = EditorProjectMapObjectPlacementMode.PlacementPreset,
                PresetLibrary = worldEditState.ObjectPlacement.PresetLibrary,
                SelectedPresetId = presetId.Trim(),
            },
            activateTool
        );
    }

    /// <summary>
    /// Sets the normalized active asset path tracked by the session project state.
    /// </summary>
    public void SetActiveAsset(string? assetPath) => _projectActiveAssetPath = NormalizeOptionalAssetPath(assetPath);

    /// <summary>
    /// Adds one asset to the session's explicit open-document set.
    /// Dialog and script assets also reopen their tracked SDK editors.
    /// </summary>
    public EditorProjectOpenAsset OpenAsset(string assetPath) =>
        OpenAsset(new EditorProjectOpenAsset { AssetPath = assetPath });

    /// <summary>
    /// Adds or replaces one explicit open-document entry in the session project state.
    /// Dialog and script assets also reopen their tracked SDK editors.
    /// </summary>
    public EditorProjectOpenAsset OpenAsset(EditorProjectOpenAsset openAsset)
    {
        var normalizedOpenAsset = NormalizeProjectOpenAsset(openAsset);
        _ = TrackProjectOpenAsset(normalizedOpenAsset);
        return normalizedOpenAsset;
    }

    /// <summary>
    /// Closes one tracked open asset and removes any associated dialog or script editor.
    /// Pending editor changes must be discarded explicitly before the editor can be closed.
    /// </summary>
    public bool CloseAsset(string assetPath, bool discardPendingChanges = false)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        EnsureTrackedAssetsCanClose([normalizedPath], discardPendingChanges);
        return CloseAssetCore(normalizedPath, discardPendingChanges);
    }

    /// <summary>
    /// Closes every tracked open asset and removes any associated dialog or script editors.
    /// Pending editor changes must be discarded explicitly before their editors can be closed.
    /// </summary>
    public void CloseAllAssets(bool discardPendingChanges = false)
    {
        var assetPaths = CollectTrackedProjectAssetPaths();
        EnsureTrackedAssetsCanClose(assetPaths, discardPendingChanges);

        foreach (var assetPath in assetPaths)
            _ = CloseAssetCore(assetPath, discardPendingChanges);
    }

    /// <summary>
    /// Adds or replaces one typed map-view state entry in the live session project state.
    /// </summary>
    public EditorProjectMapViewState SetMapViewState(EditorProjectMapViewState mapViewState)
    {
        var normalizedMapViewState = NormalizeProjectMapViewState(mapViewState);
        var updatedMapViewStates = _projectMapViewStates.ToList();
        var existingIndex = updatedMapViewStates.FindIndex(existing =>
            string.Equals(existing.Id, normalizedMapViewState.Id, StringComparison.OrdinalIgnoreCase)
        );

        if (existingIndex >= 0)
            updatedMapViewStates[existingIndex] = normalizedMapViewState;
        else
            updatedMapViewStates.Add(normalizedMapViewState);

        _projectMapViewStates = [.. updatedMapViewStates];
        return normalizedMapViewState;
    }

    /// <summary>
    /// Adds or replaces the typed world-edit workflow state for one tracked map-view entry.
    /// </summary>
    public EditorProjectMapWorldEditState SetMapWorldEditState(
        string mapViewStateId,
        EditorProjectMapWorldEditState worldEditState
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapViewStateId);
        ArgumentNullException.ThrowIfNull(worldEditState);

        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var normalizedWorldEditState = NormalizeProjectMapWorldEditState(worldEditState);
        _ = SetMapViewState(
            new EditorProjectMapViewState
            {
                Id = mapViewState.Id,
                MapName = mapViewState.MapName,
                ViewId = mapViewState.ViewId,
                Camera = mapViewState.Camera,
                Selection = mapViewState.Selection,
                Preview = mapViewState.Preview,
                WorldEdit = normalizedWorldEditState,
            }
        );
        return normalizedWorldEditState;
    }

    /// <summary>
    /// Removes one typed map-view state entry by its stable identifier.
    /// </summary>
    public bool RemoveMapViewState(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var updatedMapViewStates = _projectMapViewStates
            .Where(existing => !string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (updatedMapViewStates.Length == _projectMapViewStates.Count)
            return false;

        _projectMapViewStates = updatedMapViewStates;
        return true;
    }

    /// <summary>
    /// Builds one render-ready committed scene preview from a typed map-view state.
    /// The returned render respects the map-view preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapFloorRenderPreview CreateMapFloorRenderPreview(
        EditorProjectMapViewState mapViewState,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        var normalizedMapViewState = NormalizeProjectMapViewState(mapViewState);
        var scenePreview = CreateEffectiveMapScenePreview(normalizedMapViewState.MapName);
        var effectiveRequest = ComposeMapViewRenderRequest(normalizedMapViewState.Preview, renderRequest);
        return EditorMapFloorRenderBuilder.Build(scenePreview, effectiveRequest);
    }

    /// <summary>
    /// Builds one render-ready committed scene preview from one tracked typed map-view state identifier.
    /// The returned render respects the persisted preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapFloorRenderPreview CreateMapFloorRenderPreview(
        string mapViewStateId,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        return CreateMapFloorRenderPreview(ResolveTrackedMapViewState(mapViewStateId), renderRequest);
    }

    /// <summary>
    /// Applies the tracked terrain-paint tool for one map view using its persisted selection state.
    /// </summary>
    public EditorMapLayerBrushResult ApplyTrackedTerrainTool(string mapViewStateId)
    {
        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var terrainEntry =
            Workspace.FindTerrainPaletteEntry(mapViewState.WorldEdit.Terrain)
            ?? throw new InvalidOperationException(
                $"The tracked terrain tool for map view '{mapViewStateId}' does not resolve to one loaded terrain palette entry."
            );
        var scenePreview = CreateEffectiveMapScenePreview(mapViewState.MapName);
        return ApplyTerrainPaletteEntry(scenePreview, mapViewState.Selection, terrainEntry);
    }

    /// <summary>
    /// Builds one live placement preview from the tracked object-placement tool for one map view.
    /// </summary>
    public EditorMapPlacementPreview PreviewTrackedObjectPlacementTool(
        string mapViewStateId,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var scenePreview = CreateEffectiveMapScenePreview(mapViewState.MapName);
        return PreviewTrackedObjectPlacementTool(scenePreview, mapViewState, renderRequest);
    }

    /// <summary>
    /// Applies the tracked object-placement tool for one map view using its persisted selection state.
    /// </summary>
    public IReadOnlyList<MobData> ApplyTrackedObjectPlacementTool(string mapViewStateId)
    {
        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var scenePreview = CreateEffectiveMapScenePreview(mapViewState.MapName);
        return ApplyTrackedObjectPlacementTool(scenePreview, mapViewState);
    }

    /// <summary>
    /// Creates one default typed map-view state seeded from the workspace default-map resolver.
    /// The returned camera is centered on the effective map bounds in tile space.
    /// </summary>
    public EditorProjectMapViewState CreateDefaultMapViewState(string id, string? viewId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var defaultMap =
            Workspace.ResolveDefaultMap()
            ?? throw new InvalidOperationException("This workspace has no indexed maps to resolve as a default map.");
        var scenePreview = CreateEffectiveMapScenePreview(defaultMap.MapName);
        var camera = CreateCenteredTileCamera(scenePreview);

        return NormalizeProjectMapViewState(
            new EditorProjectMapViewState
            {
                Id = id,
                MapName = defaultMap.MapName,
                ViewId = viewId,
                Camera = camera,
                Selection = new EditorProjectMapSelectionState(),
                Preview = new EditorProjectMapPreviewState(),
            }
        );
    }

    /// <summary>
    /// Builds one bundled host-facing world-edit scene from a typed map-view state.
    /// The returned bundle includes committed render output, render-space viewport metadata, one paintable scene model,
    /// and one optional live placement ghost.
    /// </summary>
    public EditorMapWorldEditScene CreateMapWorldEditScene(
        EditorProjectMapViewState mapViewState,
        EditorMapWorldEditSceneRequest? request = null
    )
    {
        var normalizedMapViewState = NormalizeProjectMapViewState(mapViewState);
        var sceneRender = CreateMapFloorRenderPreview(normalizedMapViewState, request?.RenderRequest);
        var placementPreview = request?.PlacementRequest is { } placementRequest
            ? PreviewSectorObjectPalettePlacement(normalizedMapViewState, placementRequest, request.RenderRequest)
            : null;
        var viewportState =
            request?.Viewport
            ?? EditorMapSceneRenderSpaceMath.CreateViewportState(sceneRender, normalizedMapViewState.Camera);
        var viewportLayout = EditorMapSceneRenderSpaceMath.CreateViewportLayout(
            sceneRender,
            request?.ViewportWidth ?? sceneRender.WidthPixels,
            request?.ViewportHeight ?? sceneRender.HeightPixels,
            viewportState
        );
        var effectiveArtResolver =
            request?.ArtResolver ?? Workspace.CreateArtResolver(EditorArtResolverBindingStrategy.Conservative);
        var spriteSource =
            request?.SpriteSource
            ?? (
                effectiveArtResolver.BindingCount > 0
                    ? Workspace.CreateMapRenderSpriteSource(effectiveArtResolver)
                    : null
            );
        var paintableScene = EditorMapPaintableSceneBuilder.Build(sceneRender, placementPreview, spriteSource);

        return new EditorMapWorldEditScene
        {
            MapViewState = normalizedMapViewState,
            SceneRender = sceneRender,
            PlacementPreview = placementPreview,
            ViewportLayout = viewportLayout,
            PaintableScene = paintableScene,
            SpriteCoverage = paintableScene.SpriteCoverage,
        };
    }

    /// <summary>
    /// Builds one bundled host-facing world-edit scene from one tracked typed map-view state identifier.
    /// </summary>
    public EditorMapWorldEditScene CreateMapWorldEditScene(
        string mapViewStateId,
        EditorMapWorldEditSceneRequest? request = null
    ) => CreateMapWorldEditScene(ResolveTrackedMapViewState(mapViewStateId), request);

    /// <summary>
    /// Builds one bundled host-facing world-edit scene for the workspace default map.
    /// The returned scene can be used by hosts as a first-load bootstrap surface.
    /// </summary>
    public EditorMapWorldEditScene CreateDefaultMapWorldEditScene(
        string id = "default-map",
        string? viewId = null,
        EditorMapWorldEditSceneRequest? request = null
    ) => CreateMapWorldEditScene(CreateDefaultMapViewState(id, viewId), request);

    /// <summary>
    /// Builds one live placement preview from a typed map-view state and one palette placement request.
    /// The returned preview respects the map-view selection and preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(
        EditorProjectMapViewState mapViewState,
        EditorObjectPalettePlacementRequest request,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        var normalizedMapViewState = NormalizeProjectMapViewState(mapViewState);
        var scenePreview = CreateEffectiveMapScenePreview(normalizedMapViewState.MapName);
        var effectiveRequest = ComposeMapViewRenderRequest(normalizedMapViewState.Preview, renderRequest);
        return PreviewSectorObjectPalettePlacement(
            scenePreview,
            normalizedMapViewState.Selection,
            request,
            effectiveRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview from one tracked typed map-view state identifier and one palette placement request.
    /// The returned preview respects the persisted map-view selection and preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(
        string mapViewStateId,
        EditorObjectPalettePlacementRequest request,
        EditorMapFloorRenderRequest? renderRequest = null
    ) => PreviewSectorObjectPalettePlacement(ResolveTrackedMapViewState(mapViewStateId), request, renderRequest);

    /// <summary>
    /// Builds one live placement preview from a typed map-view state and one reusable placement set.
    /// The returned preview respects the map-view selection and preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(
        EditorProjectMapViewState mapViewState,
        EditorObjectPalettePlacementSet placementSet,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        var normalizedMapViewState = NormalizeProjectMapViewState(mapViewState);
        var scenePreview = CreateEffectiveMapScenePreview(normalizedMapViewState.MapName);
        var effectiveRequest = ComposeMapViewRenderRequest(normalizedMapViewState.Preview, renderRequest);
        return PreviewSectorObjectPalettePlacementSet(
            scenePreview,
            normalizedMapViewState.Selection,
            placementSet,
            effectiveRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview from one tracked typed map-view state identifier and one reusable placement set.
    /// The returned preview respects the persisted map-view selection and preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(
        string mapViewStateId,
        EditorObjectPalettePlacementSet placementSet,
        EditorMapFloorRenderRequest? renderRequest = null
    ) =>
        PreviewSectorObjectPalettePlacementSet(ResolveTrackedMapViewState(mapViewStateId), placementSet, renderRequest);

    /// <summary>
    /// Builds one live placement preview from a typed map-view state and one named placement preset.
    /// The returned preview respects the map-view selection and preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(
        EditorProjectMapViewState mapViewState,
        EditorObjectPalettePlacementPreset preset,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        var normalizedMapViewState = NormalizeProjectMapViewState(mapViewState);
        var scenePreview = CreateEffectiveMapScenePreview(normalizedMapViewState.MapName);
        var effectiveRequest = ComposeMapViewRenderRequest(normalizedMapViewState.Preview, renderRequest);
        return PreviewSectorObjectPalettePlacementPreset(
            scenePreview,
            normalizedMapViewState.Selection,
            preset,
            effectiveRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview from one tracked typed map-view state identifier and one named placement preset.
    /// The returned preview respects the persisted map-view selection and preview visibility flags and includes staged session changes.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(
        string mapViewStateId,
        EditorObjectPalettePlacementPreset preset,
        EditorMapFloorRenderRequest? renderRequest = null
    ) => PreviewSectorObjectPalettePlacementPreset(ResolveTrackedMapViewState(mapViewStateId), preset, renderRequest);

    /// <summary>
    /// Gets a transactional dialog editor for one loaded dialog asset path.
    /// The same editor instance is reused across repeated calls for the same normalized path.
    /// </summary>
    public DialogEditor GetDialogEditor(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (!_dialogEditors.TryGetValue(normalizedPath, out var editor))
        {
            var dialog =
                Workspace.FindDialog(normalizedPath)
                ?? throw new InvalidOperationException($"No loaded dialog asset matched '{normalizedPath}'.");
            editor = new DialogEditor(
                dialog,
                CreateStagedHistoryObserver(EditorSessionStagedHistoryScopeKind.Dialog, normalizedPath)
            );
            _dialogEditors[normalizedPath] = editor;
        }

        EnsureBareProjectOpenAssetTracked(normalizedPath);

        return editor;
    }

    /// <summary>
    /// Gets a transactional script editor for one loaded script asset path.
    /// The same editor instance is reused across repeated calls for the same normalized path.
    /// </summary>
    public ScriptEditor GetScriptEditor(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (!_scriptEditors.TryGetValue(normalizedPath, out var editor))
        {
            var script =
                Workspace.FindScript(normalizedPath)
                ?? throw new InvalidOperationException($"No loaded script asset matched '{normalizedPath}'.");
            editor = new ScriptEditor(
                script,
                CreateStagedHistoryObserver(EditorSessionStagedHistoryScopeKind.Script, normalizedPath)
            );
            _scriptEditors[normalizedPath] = editor;
        }

        EnsureBareProjectOpenAssetTracked(normalizedPath);

        return editor;
    }

    /// <summary>
    /// Gets the session's persistent save editor.
    /// The editor is created lazily the first time it is requested.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workspace was loaded without a save slot.
    /// </exception>
    public SaveGameEditor GetSaveEditor()
    {
        if (_saveEditor is not null)
            return _saveEditor;

        var save =
            Workspace.Save
            ?? throw new InvalidOperationException(
                "This workspace was loaded without a save slot. Provide SaveFolder and SaveSlotName to EditorWorkspaceLoader first."
            );

        _saveEditor = new SaveGameEditor(
            save,
            DefaultSaveGameWriter.Instance,
            CreateStagedHistoryObserver(EditorSessionStagedHistoryScopeKind.Save, GetSaveHistoryScopeTarget())
        );
        return _saveEditor;
    }

    /// <summary>
    /// Creates a persisted project snapshot from the current workspace and the session's tracked open editors.
    /// Hosts can supply additional project metadata, or omit it after <see cref="RestoreProject(EditorProject)"/>
    /// so the session can round-trip the last restored project state together with any SDK-managed open assets.
    /// </summary>
    public EditorProject CreateProject(
        string? activeAssetPath = null,
        IReadOnlyList<EditorProjectOpenAsset>? openAssets = null,
        IReadOnlyList<EditorProjectBookmark>? bookmarks = null,
        IReadOnlyList<EditorProjectMapViewState>? mapViewStates = null,
        IReadOnlyList<EditorProjectViewState>? viewStates = null,
        IReadOnlyList<EditorProjectToolState>? toolStates = null
    )
    {
        var normalizedActiveAssetPath = activeAssetPath is null
            ? _projectActiveAssetPath
            : NormalizeOptionalAssetPath(activeAssetPath);
        var effectiveOpenAssets = openAssets ?? _projectOpenAssets;
        var effectiveBookmarks = bookmarks ?? _projectBookmarks;
        var effectiveMapViewStates = mapViewStates ?? _projectMapViewStates;
        var effectiveViewStates = viewStates ?? _projectViewStates;
        var effectiveToolStates = toolStates ?? _projectToolStates;

        return new EditorProject
        {
            Workspace = Workspace.CreateProject().Workspace,
            ActiveAssetPath = normalizedActiveAssetPath,
            OpenAssets = MergeProjectOpenAssets(normalizedActiveAssetPath, effectiveOpenAssets),
            Bookmarks = NormalizeProjectBookmarks(effectiveBookmarks),
            MapViewStates = NormalizeProjectMapViewStates(effectiveMapViewStates),
            ViewStates = NormalizeProjectViewStates(effectiveViewStates),
            ToolStates = NormalizeProjectToolStates(effectiveToolStates),
        };
    }

    /// <summary>
    /// Restores persisted project metadata onto this live session and reopens the SDK-managed asset editors that
    /// correspond to the project's current open or active assets.
    /// Unsupported or stale asset paths are preserved for project round-tripping but skipped during editor restore.
    /// </summary>
    public EditorProjectRestoreResult RestoreProject(EditorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var normalizedActiveAssetPath = NormalizeOptionalAssetPath(project.ActiveAssetPath);
        var normalizedOpenAssets = NormalizeProjectOpenAssets(project.OpenAssets);
        var normalizedBookmarks = NormalizeProjectBookmarks(project.Bookmarks);
        var normalizedMapViewStates = NormalizeProjectMapViewStates(project.MapViewStates);
        var normalizedViewStates = NormalizeProjectViewStates(project.ViewStates);
        var normalizedToolStates = NormalizeProjectToolStates(project.ToolStates);
        var targetAssetPaths = new HashSet<string>(
            EnumerateProjectRestoreAssetPaths(normalizedActiveAssetPath, normalizedOpenAssets),
            StringComparer.OrdinalIgnoreCase
        );
        var assetPathsToClose = CollectTrackedProjectAssetPaths()
            .Where(assetPath => !targetAssetPaths.Contains(assetPath))
            .ToArray();

        EnsureTrackedAssetsCanClose(assetPathsToClose, discardPendingChanges: false);

        foreach (var assetPath in assetPathsToClose)
            _ = CloseAssetCore(assetPath, discardPendingChanges: false);

        _projectActiveAssetPath = normalizedActiveAssetPath;
        _projectOpenAssets = [];
        _projectBookmarks = normalizedBookmarks;
        _projectMapViewStates = normalizedMapViewStates;
        _projectViewStates = normalizedViewStates;
        _projectToolStates = normalizedToolStates;

        var restoredAssetPaths = new List<string>();
        var skippedAssetPaths = new List<string>();
        var seenAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var openAsset in normalizedOpenAssets)
        {
            if (!seenAssetPaths.Add(openAsset.AssetPath))
                continue;

            if (TrackProjectOpenAsset(openAsset))
                restoredAssetPaths.Add(openAsset.AssetPath);
            else
                skippedAssetPaths.Add(openAsset.AssetPath);
        }

        if (_projectActiveAssetPath is not null && seenAssetPaths.Add(_projectActiveAssetPath))
        {
            if (TryRestoreTrackedProjectAsset(_projectActiveAssetPath))
                restoredAssetPaths.Add(_projectActiveAssetPath);
            else
                skippedAssetPaths.Add(_projectActiveAssetPath);
        }

        var restoredActiveAssetPath =
            _projectActiveAssetPath is not null
            && restoredAssetPaths.Any(path =>
                string.Equals(path, _projectActiveAssetPath, StringComparison.OrdinalIgnoreCase)
            )
                ? _projectActiveAssetPath
                : null;

        return new EditorProjectRestoreResult
        {
            RequestedActiveAssetPath = _projectActiveAssetPath,
            RestoredActiveAssetPath = restoredActiveAssetPath,
            RestoredAssetPaths = [.. restoredAssetPaths],
            SkippedAssetPaths = [.. skippedAssetPaths],
            RestoredProjectState = CreateProjectStateSummary(),
        };
    }

    /// <summary>
    /// Applies all staged editor changes into a fresh workspace snapshot, updates this session to that
    /// snapshot, and clears the pending state on the tracked editors.
    /// </summary>
    public EditorWorkspace ApplyPendingChanges() => ApplyPendingChanges(changeGroupLabel: null);

    /// <summary>
    /// Applies the currently staged work represented by <paramref name="stagedTransaction"/> into a fresh workspace snapshot,
    /// updates this session to that snapshot, and leaves other staged transactions pending.
    /// </summary>
    public EditorWorkspace ApplyPendingChanges(EditorSessionStagedTransactionSummary stagedTransaction)
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);
        return ApplyPendingChanges([stagedTransaction]);
    }

    /// <summary>
    /// Applies the currently staged work represented by <paramref name="stagedTransactions"/> into a fresh workspace snapshot,
    /// updates this session to that snapshot, and leaves other staged transactions pending.
    /// </summary>
    public EditorWorkspace ApplyPendingChanges(IReadOnlyList<EditorSessionStagedTransactionSummary> stagedTransactions)
    {
        var selectedScopeKeys = NormalizeSelectedStagedTransactionScopeKeys(stagedTransactions);
        return ApplyPendingChangesCore(changeGroupLabel: null, persistedToDisk: false, selectedScopeKeys);
    }

    /// <summary>
    /// Returns the workspace-level validation report for the current staged session state without applying or saving it.
    /// This reuses the same snapshot-building path as apply/save so hosts can inspect cross-asset validation findings
    /// before committing the pending change group.
    /// </summary>
    public EditorWorkspaceValidationReport GetPendingValidation() =>
        !HasPendingChanges ? Workspace.Validation : BuildPendingWorkspaceState().Workspace.Validation;

    /// <summary>
    /// Returns the workspace-level validation report for the staged state represented by
    /// <paramref name="stagedTransaction"/> without applying or saving it.
    /// </summary>
    public EditorWorkspaceValidationReport GetPendingValidation(EditorSessionStagedTransactionSummary stagedTransaction)
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);
        return GetPendingValidation([stagedTransaction]);
    }

    /// <summary>
    /// Returns the workspace-level validation report for the staged state represented by
    /// <paramref name="stagedTransactions"/> without applying or saving it.
    /// </summary>
    public EditorWorkspaceValidationReport GetPendingValidation(
        IReadOnlyList<EditorSessionStagedTransactionSummary> stagedTransactions
    )
    {
        var selectedScopeKeys = NormalizeSelectedStagedTransactionScopeKeys(stagedTransactions);
        if (selectedScopeKeys.Count == 0)
            return Workspace.Validation;

        return BuildPendingWorkspaceState(selectedScopeKeys).Workspace.Validation;
    }

    /// <summary>
    /// Returns a grouped summary of the current staged session changes plus the validation state that would apply to them.
    /// Hosts can use this to explain what a pending multi-asset apply/save would touch before committing it.
    /// </summary>
    public EditorSessionPendingChangeSummary GetPendingChangeSummary()
    {
        if (!HasPendingChanges)
            return CreatePendingChangeSummary([], Workspace, Workspace.Validation, []);

        var pendingState = BuildPendingWorkspaceState();
        return CreatePendingChangeSummary(
            pendingState.Changes,
            pendingState.Workspace,
            pendingState.Workspace.Validation,
            CollectPendingStateBlockingIssues(pendingState.Workspace.Validation)
        );
    }

    /// <summary>
    /// Returns staged repair candidates for the current session head.
    /// Today this surfaces actionable dialog-local fixes for validation findings that can be repaired through
    /// the existing dialog editor API without rebuilding the asset manually.
    /// </summary>
    public IReadOnlyList<EditorSessionValidationRepairCandidate> GetValidationRepairCandidates()
    {
        return GetValidationRepairCandidates(selectedScopeKeys: null);
    }

    /// <summary>
    /// Returns staged repair candidates for the transaction represented by <paramref name="stagedTransaction"/>.
    /// </summary>
    public IReadOnlyList<EditorSessionValidationRepairCandidate> GetValidationRepairCandidates(
        EditorSessionStagedTransactionSummary stagedTransaction
    )
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);
        return GetValidationRepairCandidates([stagedTransaction]);
    }

    /// <summary>
    /// Returns staged repair candidates for the transactions represented by <paramref name="stagedTransactions"/>.
    /// </summary>
    public IReadOnlyList<EditorSessionValidationRepairCandidate> GetValidationRepairCandidates(
        IReadOnlyList<EditorSessionStagedTransactionSummary> stagedTransactions
    )
    {
        var selectedScopeKeys = NormalizeSelectedStagedTransactionScopeKeys(stagedTransactions);
        return GetValidationRepairCandidates(selectedScopeKeys);
    }

    /// <summary>
    /// Applies one staged validation repair candidate through the current session editors.
    /// The resulting edit remains pending until the session is applied or saved.
    /// </summary>
    public EditorSessionChange ApplyValidationRepairCandidate(EditorSessionValidationRepairCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var assetPath = NormalizeAssetPath(candidate.AssetPath);
        var entry = GetCurrentDialogEntry(assetPath, candidate.DialogEntryNumber);

        switch (candidate.Kind)
        {
            case EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement:
                if (!candidate.SuggestedIntelligenceRequirement.HasValue)
                {
                    throw new ArgumentException(
                        "Dialog IQ repair candidates must provide SuggestedIntelligenceRequirement.",
                        nameof(candidate)
                    );
                }

                GetDialogEditor(assetPath)
                    .UpdateEntry(
                        candidate.DialogEntryNumber,
                        currentEntry => new DialogEntry
                        {
                            Num = currentEntry.Num,
                            Text = currentEntry.Text,
                            GenderField = currentEntry.GenderField,
                            Iq = candidate.SuggestedIntelligenceRequirement.Value,
                            Conditions = currentEntry.Conditions,
                            ResponseVal = currentEntry.ResponseVal,
                            Actions = currentEntry.Actions,
                        }
                    );
                break;
            case EditorSessionValidationRepairCandidateKind.SetDialogResponseTarget:
                if (!candidate.SuggestedResponseTargetNumber.HasValue)
                {
                    throw new ArgumentException(
                        "Dialog response-target repair candidates must provide SuggestedResponseTargetNumber.",
                        nameof(candidate)
                    );
                }

                GetDialogEditor(assetPath)
                    .SetResponseTarget(candidate.DialogEntryNumber, candidate.SuggestedResponseTargetNumber.Value);
                break;
            default:
                throw new InvalidOperationException($"Unsupported validation repair candidate kind {candidate.Kind}.");
        }

        _ = entry;
        return new EditorSessionChange { Kind = EditorSessionChangeKind.Dialog, Target = assetPath };
    }

    internal EditorWorkspace ApplyPendingChanges(string? changeGroupLabel) =>
        ApplyPendingChangesCore(changeGroupLabel, persistedToDisk: false, selectedScopeKeys: null);

    /// <summary>
    /// Applies all staged editor changes into a fresh workspace snapshot and persists the affected
    /// dialog/script assets plus the optional save slot to the workspace's configured paths.
    /// </summary>
    public EditorWorkspace SavePendingChanges() => SavePendingChanges(changeGroupLabel: null);

    /// <summary>
    /// Applies and persists the currently staged work represented by <paramref name="stagedTransaction"/>,
    /// updates this session to that snapshot, and leaves other staged transactions pending.
    /// </summary>
    public EditorWorkspace SavePendingChanges(EditorSessionStagedTransactionSummary stagedTransaction)
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);
        return SavePendingChanges([stagedTransaction]);
    }

    /// <summary>
    /// Applies and persists the currently staged work represented by <paramref name="stagedTransactions"/>,
    /// updates this session to that snapshot, and leaves other staged transactions pending.
    /// </summary>
    public EditorWorkspace SavePendingChanges(IReadOnlyList<EditorSessionStagedTransactionSummary> stagedTransactions)
    {
        var selectedScopeKeys = NormalizeSelectedStagedTransactionScopeKeys(stagedTransactions);
        return SavePendingChanges(changeGroupLabel: null, selectedScopeKeys);
    }

    internal EditorWorkspace SavePendingChanges(string? changeGroupLabel)
    {
        return SavePendingChanges(changeGroupLabel, selectedScopeKeys: null);
    }

    private EditorWorkspace SavePendingChanges(
        string? changeGroupLabel,
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys
    )
    {
        if (!HasPendingChanges)
            return Workspace;

        var pendingDialogPaths = CollectPendingDialogAssetPaths(selectedScopeKeys);
        var pendingScriptPaths = CollectPendingScriptAssetPaths(selectedScopeKeys);
        var pendingProtos = CollectProtoChanges(selectedScopeKeys);
        var pendingMobs = CollectMobChanges(selectedScopeKeys);
        var pendingSectors = CollectSectorChanges(selectedScopeKeys);
        var persistSave =
            _saveEditor?.HasPendingChanges == true
            && IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Save, GetSaveHistoryScopeTarget());

        var updatedWorkspace = ApplyPendingChangesCore(changeGroupLabel, persistedToDisk: false, selectedScopeKeys);

        if (
            pendingDialogPaths.Length == 0
            && pendingScriptPaths.Length == 0
            && pendingProtos.Count == 0
            && pendingMobs.Count == 0
            && pendingSectors.Count == 0
            && !persistSave
        )
        {
            return updatedWorkspace;
        }

        PersistDialogChanges(pendingDialogPaths);
        PersistScriptChanges(pendingScriptPaths);
        PersistProtoChanges(pendingProtos);
        PersistMobChanges(pendingMobs);
        PersistSectorChanges(pendingSectors);

        if (persistSave)
            PersistSaveChanges();

        PromoteLatestUndoHistoryToPersisted(changeGroupLabel);

        return updatedWorkspace;
    }

    /// <summary>
    /// Discards all staged changes across the tracked editors and restores their committed baselines.
    /// </summary>
    public EditorWorkspaceSession DiscardPendingChanges()
    {
        foreach (var editor in _dialogEditors.Values)
            editor.DiscardPendingChanges();

        foreach (var editor in _scriptEditors.Values)
            editor.DiscardPendingChanges();

        _pendingProtoAssets.Clear();
        _pendingMobAssets.Clear();
        _pendingSectorAssets.Clear();
        ClearDirectAssetDraftHistory();
        _saveEditor?.DiscardPendingChanges();
        return this;
    }

    /// <summary>
    /// Discards the currently staged work represented by <paramref name="stagedTransaction"/> and leaves
    /// other staged transactions pending.
    /// </summary>
    public EditorWorkspaceSession DiscardPendingChanges(EditorSessionStagedTransactionSummary stagedTransaction)
    {
        ArgumentNullException.ThrowIfNull(stagedTransaction);
        return DiscardPendingChanges([stagedTransaction]);
    }

    /// <summary>
    /// Discards the currently staged work represented by <paramref name="stagedTransactions"/> and leaves
    /// other staged transactions pending.
    /// </summary>
    public EditorWorkspaceSession DiscardPendingChanges(
        IReadOnlyList<EditorSessionStagedTransactionSummary> stagedTransactions
    )
    {
        var selectedScopeKeys = NormalizeSelectedStagedTransactionScopeKeys(stagedTransactions);
        if (selectedScopeKeys.Count == 0)
            return this;

        foreach (var scopeKey in selectedScopeKeys)
        {
            switch (scopeKey.Kind)
            {
                case EditorSessionStagedHistoryScopeKind.Dialog:
                    if (_dialogEditors.TryGetValue(scopeKey.Target!, out var dialogEditor))
                        dialogEditor.DiscardPendingChanges();
                    break;
                case EditorSessionStagedHistoryScopeKind.Script:
                    if (_scriptEditors.TryGetValue(scopeKey.Target!, out var scriptEditor))
                        scriptEditor.DiscardPendingChanges();
                    break;
                case EditorSessionStagedHistoryScopeKind.Save:
                    _saveEditor?.DiscardPendingChanges();
                    break;
                case EditorSessionStagedHistoryScopeKind.DirectAssets:
                    DiscardDirectAssetChanges();
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported staged transaction scope {scopeKey.Kind}.");
            }
        }

        return this;
    }

    /// <summary>
    /// Returns the current set of pending changes tracked by this session.
    /// Results are ordered by change kind group: dialogs, scripts, protos, mobs, sectors, then the optional save editor.
    /// </summary>
    public IReadOnlyList<EditorSessionChange> GetPendingChanges() => CollectPendingChangesSnapshot();

    /// <summary>
    /// Retargets one script identifier across all currently referenced proto, mob, and sector assets.
    /// The updates are staged in this session and can be applied or saved as one labeled change group.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the target script identifier does not resolve to any loaded script definition.
    /// </exception>
    public IReadOnlyList<EditorSessionChange> RetargetScriptReferences(int sourceScriptId, int targetScriptId)
    {
        if (sourceScriptId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(sourceScriptId),
                sourceScriptId,
                "Script IDs must be positive."
            );

        if (targetScriptId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(targetScriptId),
                targetScriptId,
                "Script IDs must be positive."
            );

        if (sourceScriptId == targetScriptId)
            return [];

        if (Workspace.Index.FindScriptDefinitions(targetScriptId).Count == 0)
        {
            throw new InvalidOperationException(
                $"Cannot retarget script references to ID {targetScriptId} because no loaded script definition matched that identifier."
            );
        }

        return TrackDirectAssetEdit(
            () =>
            {
                var targets = CollectScriptRetargetTargets(sourceScriptId);
                if (targets.Count == 0)
                    return Array.Empty<EditorSessionChange>();

                foreach (var target in targets)
                {
                    switch (target.Format)
                    {
                        case FileFormat.Proto:
                            if (
                                EditorScriptReferenceRetargeter.TryRetarget(
                                    GetCurrentProtoAsset(target.AssetPath),
                                    sourceScriptId,
                                    targetScriptId,
                                    out var updatedProto
                                )
                            )
                            {
                                _pendingProtoAssets[target.AssetPath] = updatedProto;
                            }

                            break;
                        case FileFormat.Mob:
                            if (
                                EditorScriptReferenceRetargeter.TryRetarget(
                                    GetCurrentMobAsset(target.AssetPath),
                                    sourceScriptId,
                                    targetScriptId,
                                    out var updatedMob
                                )
                            )
                            {
                                _pendingMobAssets[target.AssetPath] = updatedMob;
                            }

                            break;
                        case FileFormat.Sector:
                            if (
                                EditorScriptReferenceRetargeter.TryRetarget(
                                    GetCurrentSectorAsset(target.AssetPath),
                                    sourceScriptId,
                                    targetScriptId,
                                    out var updatedSector
                                )
                            )
                            {
                                _pendingSectorAssets[target.AssetPath] = updatedSector;
                            }

                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Script retargeting does not support referencing assets of format {target.Format}."
                            );
                    }
                }

                return [.. targets.Select(CreateDirectAssetChange)];
            },
            static changes => changes.Length > 0
        );
    }

    /// <summary>
    /// Replaces one art resource identifier across all currently referenced proto, mob, and sector assets.
    /// The updates are staged in this session and can be applied or saved as one labeled change group.
    /// </summary>
    public IReadOnlyList<EditorSessionChange> ReplaceArtReferences(uint sourceArtId, uint targetArtId)
    {
        if (sourceArtId == 0)
            throw new ArgumentOutOfRangeException(nameof(sourceArtId), sourceArtId, "Art IDs must be non-zero.");

        if (targetArtId == 0)
            throw new ArgumentOutOfRangeException(nameof(targetArtId), targetArtId, "Art IDs must be non-zero.");

        if (sourceArtId == targetArtId)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var targets = CollectArtReplacementTargets(sourceArtId);
                if (targets.Count == 0)
                    return Array.Empty<EditorSessionChange>();

                foreach (var target in targets)
                {
                    switch (target.Format)
                    {
                        case FileFormat.Proto:
                            if (
                                EditorArtReferenceReplacer.TryReplace(
                                    GetCurrentProtoAsset(target.AssetPath),
                                    sourceArtId,
                                    targetArtId,
                                    out var updatedProto
                                )
                            )
                            {
                                _pendingProtoAssets[target.AssetPath] = updatedProto;
                            }

                            break;
                        case FileFormat.Mob:
                            if (
                                EditorArtReferenceReplacer.TryReplace(
                                    GetCurrentMobAsset(target.AssetPath),
                                    sourceArtId,
                                    targetArtId,
                                    out var updatedMob
                                )
                            )
                            {
                                _pendingMobAssets[target.AssetPath] = updatedMob;
                            }

                            break;
                        case FileFormat.Sector:
                            if (
                                EditorArtReferenceReplacer.TryReplace(
                                    GetCurrentSectorAsset(target.AssetPath),
                                    sourceArtId,
                                    targetArtId,
                                    out var updatedSector
                                )
                            )
                            {
                                _pendingSectorAssets[target.AssetPath] = updatedSector;
                            }

                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Art replacement does not support referencing assets of format {target.Format}."
                            );
                    }
                }

                return [.. targets.Select(CreateDirectAssetChange)];
            },
            static changes => changes.Length > 0
        );
    }

    /// <summary>
    /// Stages one ground-tile art edit on a loaded sector asset.
    /// Returns <see langword="null"/> when the tile already uses <paramref name="artId"/>.
    /// </summary>
    public EditorSessionChange? SetSectorTileArt(string assetPath, int tileX, int tileY, uint artId)
    {
        ValidateTileCoordinate(nameof(tileX), tileX);
        ValidateTileCoordinate(nameof(tileY), tileY);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                var tileIndex = (tileY * SectorTileAxisLength) + tileX;
                if (sector.Tiles[tileIndex] == artId)
                    return null;

                return new SectorBuilder(sector).SetTile(tileX, tileY, artId).Build();
            }
        );
    }

    /// <summary>
    /// Stages one bulk ground-tile art edit from grouped scene-sector hits.
    /// Returns one change per sector asset that actually changed.
    /// </summary>
    public IReadOnlyList<EditorSessionChange> SetSectorTileArt(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        uint artId
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);

        if (sectorHitGroups.Count == 0)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var changes = new List<EditorSessionChange>(sectorHitGroups.Count);

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    SectorBuilder? builder = null;
                    var stagedTiles = new HashSet<Location>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        if (!stagedTiles.Add(hit.Tile))
                            continue;

                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.X);
                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.Y);

                        var tileIndex = (hit.Tile.Y * SectorTileAxisLength) + hit.Tile.X;
                        if (currentSector.Tiles[tileIndex] == artId)
                            continue;

                        builder ??= new SectorBuilder(currentSector);
                        builder.SetTile(hit.Tile.X, hit.Tile.Y, artId);
                    }

                    if (builder is null)
                        continue;

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                    changes.Add(CreateDirectAssetChange(normalizedPath, FileFormat.Sector));
                }

                return changes.ToArray();
            },
            static changes => changes.Length > 0
        );
    }

    /// <summary>
    /// Stages one bulk blocked-tile edit from grouped scene-sector hits.
    /// Returns one change per sector asset that actually changed.
    /// </summary>
    public IReadOnlyList<EditorSessionChange> SetSectorBlockedTile(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        bool blocked
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);

        if (sectorHitGroups.Count == 0)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var changes = new List<EditorSessionChange>(sectorHitGroups.Count);

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    SectorBuilder? builder = null;
                    var stagedTiles = new HashSet<Location>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        if (!stagedTiles.Add(hit.Tile))
                            continue;

                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.X);
                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.Y);

                        if (currentSector.BlockMask.IsBlocked(hit.Tile.X, hit.Tile.Y) == blocked)
                            continue;

                        builder ??= new SectorBuilder(currentSector);
                        builder.SetBlocked(hit.Tile.X, hit.Tile.Y, blocked);
                    }

                    if (builder is null)
                        continue;

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                    changes.Add(CreateDirectAssetChange(normalizedPath, FileFormat.Sector));
                }

                return changes.ToArray();
            },
            static changes => changes.Length > 0
        );
    }

    /// <summary>
    /// Stages one roof-cell art edit on a loaded sector asset.
    /// Returns <see langword="null"/> when the addressed roof cell already uses <paramref name="artId"/>,
    /// or when the sector currently has no roof layer and <paramref name="artId"/> is <c>0</c>.
    /// </summary>
    public EditorSessionChange? SetSectorRoofArt(string assetPath, int roofX, int roofY, uint artId)
    {
        ValidateRoofCoordinate(nameof(roofX), roofX);
        ValidateRoofCoordinate(nameof(roofY), roofY);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                var currentArtId = GetRoofArtId(sector, roofX, roofY);
                if (currentArtId == artId && (sector.HasRoofs || artId == 0))
                    return null;

                return new SectorBuilder(sector).SetRoof(roofX, roofY, artId).Build();
            }
        );
    }

    /// <summary>
    /// Stages one bulk roof-cell art edit from grouped scene-sector hits.
    /// Returns one change per sector asset that actually changed.
    /// </summary>
    public IReadOnlyList<EditorSessionChange> SetSectorRoofArt(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        uint artId
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);

        if (sectorHitGroups.Count == 0)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var changes = new List<EditorSessionChange>(sectorHitGroups.Count);

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    SectorBuilder? builder = null;
                    var stagedRoofCells = new HashSet<Location>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        if (!stagedRoofCells.Add(hit.RoofCell))
                            continue;

                        ValidateRoofCoordinate(nameof(sectorHitGroup), hit.RoofCell.X);
                        ValidateRoofCoordinate(nameof(sectorHitGroup), hit.RoofCell.Y);

                        var currentArtId = GetRoofArtId(currentSector, hit.RoofCell.X, hit.RoofCell.Y);
                        if (currentArtId == artId && (currentSector.HasRoofs || artId == 0))
                            continue;

                        builder ??= new SectorBuilder(currentSector);
                        builder.SetRoof(hit.RoofCell.X, hit.RoofCell.Y, artId);
                    }

                    if (builder is null)
                        continue;

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                    changes.Add(CreateDirectAssetChange(normalizedPath, FileFormat.Sector));
                }

                return changes.ToArray();
            },
            static changes => changes.Length > 0
        );
    }

    /// <summary>
    /// Stages one blocked-tile edit on a loaded sector asset.
    /// Returns <see langword="null"/> when the target tile already matches <paramref name="blocked"/>.
    /// </summary>
    public EditorSessionChange? SetSectorBlockedTile(string assetPath, int tileX, int tileY, bool blocked)
    {
        ValidateTileCoordinate(nameof(tileX), tileX);
        ValidateTileCoordinate(nameof(tileY), tileY);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                if (sector.BlockMask.IsBlocked(tileX, tileY) == blocked)
                    return null;

                return new SectorBuilder(sector).SetBlocked(tileX, tileY, blocked).Build();
            }
        );
    }

    /// <summary>
    /// Applies one typed layer-brush request to grouped scene-sector hits.
    /// </summary>
    public EditorMapLayerBrushResult ApplySectorLayerBrush(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorMapLayerBrushRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        return request.Mode switch
        {
            EditorMapLayerBrushMode.SetTileArt => new EditorMapLayerBrushResult
            {
                Changes = SetSectorTileArt(sectorHitGroups, request.ArtId),
            },
            EditorMapLayerBrushMode.SetRoofArt => new EditorMapLayerBrushResult
            {
                Changes = SetSectorRoofArt(sectorHitGroups, request.ArtId),
            },
            EditorMapLayerBrushMode.SetBlocked => new EditorMapLayerBrushResult
            {
                Changes = SetSectorBlockedTile(sectorHitGroups, request.Blocked),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Mode, "Unknown layer brush mode."),
        };
    }

    /// <summary>
    /// Applies one typed layer-brush request to a persisted rectangular area selection on a scene preview.
    /// </summary>
    public EditorMapLayerBrushResult ApplySectorLayerBrush(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorMapLayerBrushRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(request);

        var groupedHits = EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection);
        return ApplySectorLayerBrush(groupedHits, request);
    }

    /// <summary>
    /// Applies one typed layer-brush request to the current persisted map selection state.
    /// </summary>
    public EditorMapLayerBrushResult ApplySectorLayerBrush(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorMapLayerBrushRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(request);

        if (selection.Area is { } areaSelection)
            return ApplySectorLayerBrush(scenePreview, areaSelection, request);

        if (selection.SectorAssetPath is null || selection.Tile is null)
            return new EditorMapLayerBrushResult();

        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AssetPath, selection.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (sector is null)
            return new EditorMapLayerBrushResult();

        var tile = selection.Tile.Value;
        var mapTileX = (sector.LocalX * sector.TileWidth) + tile.X;
        var mapTileY = (sector.LocalY * sector.TileHeight) + tile.Y;

        return ApplySectorLayerBrush(
            [
                new EditorMapSceneSectorHitGroup
                {
                    SectorAssetPath = sector.AssetPath,
                    LocalX = sector.LocalX,
                    LocalY = sector.LocalY,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = mapTileX,
                            MapTileY = mapTileY,
                            SectorAssetPath = sector.AssetPath,
                            Tile = tile,
                            ObjectHits = [],
                        },
                    ],
                },
            ],
            request
        );
    }

    /// <summary>
    /// Applies one terrain palette entry to grouped scene-sector hits as a tile-paint request.
    /// </summary>
    public EditorMapLayerBrushResult ApplyTerrainPaletteEntry(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorTerrainPaletteEntry entry
    )
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ApplySectorLayerBrush(sectorHitGroups, entry.CreateTileArtBrushRequest());
    }

    /// <summary>
    /// Applies one terrain palette entry to a persisted rectangular area selection on a scene preview.
    /// </summary>
    public EditorMapLayerBrushResult ApplyTerrainPaletteEntry(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorTerrainPaletteEntry entry
    )
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ApplySectorLayerBrush(scenePreview, areaSelection, entry.CreateTileArtBrushRequest());
    }

    /// <summary>
    /// Applies one terrain palette entry to the current persisted map selection state.
    /// </summary>
    public EditorMapLayerBrushResult ApplyTerrainPaletteEntry(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorTerrainPaletteEntry entry
    )
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ApplySectorLayerBrush(scenePreview, selection, entry.CreateTileArtBrushRequest());
    }

    /// <summary>
    /// Stages one sector light addition on a loaded sector asset.
    /// </summary>
    public EditorSessionChange AddSectorLight(string assetPath, SectorLight light)
    {
        ValidateSectorLight(light);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(normalizedPath, sector => new SectorBuilder(sector).AddLight(light).Build())!;
    }

    /// <summary>
    /// Stages one sector light replacement on a loaded sector asset.
    /// Returns <see langword="null"/> when the addressed light already equals <paramref name="light"/>.
    /// </summary>
    public EditorSessionChange? ReplaceSectorLight(string assetPath, int index, SectorLight light)
    {
        ValidateSectorLight(light);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                ValidateSectorItemIndex(nameof(index), index, sector.Lights.Count, normalizedPath, "light");
                if (sector.Lights[index] == light)
                    return null;

                return new SectorBuilder(sector).ReplaceLight(index, light).Build();
            }
        );
    }

    /// <summary>
    /// Stages one sector light removal on a loaded sector asset.
    /// </summary>
    public EditorSessionChange RemoveSectorLight(string assetPath, int index)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                ValidateSectorItemIndex(nameof(index), index, sector.Lights.Count, normalizedPath, "light");
                return new SectorBuilder(sector).RemoveLight(index).Build();
            }
        )!;
    }

    /// <summary>
    /// Stages one tile-script addition on a loaded sector asset.
    /// </summary>
    public EditorSessionChange AddSectorTileScript(string assetPath, TileScript tileScript)
    {
        ValidateTileScript(tileScript);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector => new SectorBuilder(sector).AddTileScript(tileScript).Build()
        )!;
    }

    /// <summary>
    /// Stages one tile-script replacement on a loaded sector asset.
    /// Returns <see langword="null"/> when the addressed tile script already equals <paramref name="tileScript"/>.
    /// </summary>
    public EditorSessionChange? ReplaceSectorTileScript(string assetPath, int index, TileScript tileScript)
    {
        ValidateTileScript(tileScript);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                ValidateSectorItemIndex(nameof(index), index, sector.TileScripts.Count, normalizedPath, "tile script");
                if (sector.TileScripts[index] == tileScript)
                    return null;

                return new SectorBuilder(sector).ReplaceTileScript(index, tileScript).Build();
            }
        );
    }

    /// <summary>
    /// Stages one tile-script removal on a loaded sector asset.
    /// </summary>
    public EditorSessionChange RemoveSectorTileScript(string assetPath, int index)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                ValidateSectorItemIndex(nameof(index), index, sector.TileScripts.Count, normalizedPath, "tile script");
                return new SectorBuilder(sector).RemoveTileScript(index).Build();
            }
        )!;
    }

    /// <summary>
    /// Instantiates one placed object from a loaded proto definition and stages it on a sector asset.
    /// Returns the created object so hosts can capture the generated object ID for later move/remove operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no loaded proto definition matches <paramref name="protoNumber"/>.
    /// </exception>
    public MobData AddSectorObjectFromProto(string assetPath, int protoNumber, int tileX, int tileY)
    {
        var obj = CreateSectorObjectFromProto(protoNumber, tileX, tileY);

        _ = AddSectorObject(assetPath, obj);
        return obj;
    }

    /// <summary>
    /// Instantiates one placed object from a loaded proto definition for each unique grouped scene hit and stages
    /// those objects on the addressed sector assets.
    /// Returns the created objects in stable grouped-hit order so hosts can capture generated object IDs.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no loaded proto definition matches <paramref name="protoNumber"/>.
    /// </exception>
    public IReadOnlyList<MobData> AddSectorObjectsFromProto(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        int protoNumber
    ) => AddSectorObjectsFromPalettePlacement(sectorHitGroups, EditorObjectPalettePlacementRequest.Place(protoNumber));

    /// <summary>
    /// Instantiates one proto-backed palette placement request on grouped scene-sector hits and stages
    /// those objects on the addressed sector assets.
    /// Returns the created objects in stable grouped-hit order so hosts can capture generated object IDs.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no loaded proto definition matches <see cref="EditorObjectPalettePlacementRequest.ProtoNumber"/>.
    /// </exception>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacement(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        return AddSectorObjectsFromPalettePlacement(sectorHitGroups, request);
    }

    /// <summary>
    /// Instantiates one reusable palette placement set on grouped scene-sector hits and stages
    /// those objects on the addressed sector assets.
    /// Returns the created objects in stable grouped-hit order and per-set entry order.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacementSet(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementSet placementSet
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(placementSet);

        return AddSectorObjectsFromPalettePlacementSet(sectorHitGroups, placementSet);
    }

    /// <summary>
    /// Instantiates one named placement preset on grouped scene-sector hits and stages
    /// those objects on the addressed sector assets.
    /// Returns the created objects in stable grouped-hit order and per-preset entry order.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacementPreset(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementPreset preset
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(preset);

        return ApplySectorObjectPalettePlacementSet(sectorHitGroups, preset.CreatePlacementSet());
    }

    /// <summary>
    /// Instantiates one proto-backed palette placement request on a persisted rectangular area selection.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacement(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorObjectPalettePlacementRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(request);

        var groupedHits = EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection);
        return ApplySectorObjectPalettePlacement(groupedHits, request);
    }

    /// <summary>
    /// Instantiates one reusable palette placement set on a persisted rectangular area selection.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacementSet(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorObjectPalettePlacementSet placementSet
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(placementSet);

        var groupedHits = EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection);
        return ApplySectorObjectPalettePlacementSet(groupedHits, placementSet);
    }

    /// <summary>
    /// Instantiates one named placement preset on a persisted rectangular area selection.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacementPreset(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorObjectPalettePlacementPreset preset
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(preset);

        return ApplySectorObjectPalettePlacementSet(scenePreview, areaSelection, preset.CreatePlacementSet());
    }

    /// <summary>
    /// Instantiates one proto-backed palette placement request on the current persisted map selection state.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacement(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorObjectPalettePlacementRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(request);

        if (selection.Area is { } areaSelection)
            return ApplySectorObjectPalettePlacement(scenePreview, areaSelection, request);

        if (selection.SectorAssetPath is null || selection.Tile is null)
            return [];

        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AssetPath, selection.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (sector is null)
            return [];

        var tile = selection.Tile.Value;
        var mapTileX = (sector.LocalX * sector.TileWidth) + tile.X;
        var mapTileY = (sector.LocalY * sector.TileHeight) + tile.Y;

        return ApplySectorObjectPalettePlacement(
            [
                new EditorMapSceneSectorHitGroup
                {
                    SectorAssetPath = sector.AssetPath,
                    LocalX = sector.LocalX,
                    LocalY = sector.LocalY,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = mapTileX,
                            MapTileY = mapTileY,
                            SectorAssetPath = sector.AssetPath,
                            Tile = tile,
                            ObjectHits = [],
                        },
                    ],
                },
            ],
            request
        );
    }

    /// <summary>
    /// Instantiates one reusable palette placement set on the current persisted map selection state.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacementSet(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorObjectPalettePlacementSet placementSet
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(placementSet);

        if (selection.Area is { } areaSelection)
            return ApplySectorObjectPalettePlacementSet(scenePreview, areaSelection, placementSet);

        if (selection.SectorAssetPath is null || selection.Tile is null)
            return [];

        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AssetPath, selection.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (sector is null)
            return [];

        var tile = selection.Tile.Value;
        var mapTileX = (sector.LocalX * sector.TileWidth) + tile.X;
        var mapTileY = (sector.LocalY * sector.TileHeight) + tile.Y;

        return ApplySectorObjectPalettePlacementSet(
            [
                new EditorMapSceneSectorHitGroup
                {
                    SectorAssetPath = sector.AssetPath,
                    LocalX = sector.LocalX,
                    LocalY = sector.LocalY,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = mapTileX,
                            MapTileY = mapTileY,
                            SectorAssetPath = sector.AssetPath,
                            Tile = tile,
                            ObjectHits = [],
                        },
                    ],
                },
            ],
            placementSet
        );
    }

    /// <summary>
    /// Instantiates one named placement preset on the current persisted map selection state.
    /// </summary>
    public IReadOnlyList<MobData> ApplySectorObjectPalettePlacementPreset(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorObjectPalettePlacementPreset preset
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(preset);

        return ApplySectorObjectPalettePlacementSet(scenePreview, selection, preset.CreatePlacementSet());
    }

    /// <summary>
    /// Builds one live placement preview for a proto-backed palette placement request on grouped scene-sector hits.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(
        EditorMapScenePreview scenePreview,
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementRequest request,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        return BuildPlacementPreview(
            scenePreview,
            BuildPlacementPreviewObjects(scenePreview, sectorHitGroups, [request], renderRequest),
            renderRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview for a reusable palette placement set on grouped scene-sector hits.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(
        EditorMapScenePreview scenePreview,
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementSet placementSet,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(placementSet);

        return BuildPlacementPreview(
            scenePreview,
            BuildPlacementPreviewObjects(scenePreview, sectorHitGroups, placementSet.Entries, renderRequest),
            renderRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview for a named palette placement preset on grouped scene-sector hits.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(
        EditorMapScenePreview scenePreview,
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementPreset preset,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(preset);

        return PreviewSectorObjectPalettePlacementSet(
            scenePreview,
            sectorHitGroups,
            preset.CreatePlacementSet(),
            renderRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview for a proto-backed palette placement request on a persisted rectangular area selection.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorObjectPalettePlacementRequest request,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(request);

        return PreviewSectorObjectPalettePlacement(
            scenePreview,
            EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection),
            request,
            renderRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview for a reusable palette placement set on a persisted rectangular area selection.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorObjectPalettePlacementSet placementSet,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(placementSet);

        return PreviewSectorObjectPalettePlacementSet(
            scenePreview,
            EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection),
            placementSet,
            renderRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview for a named palette placement preset on a persisted rectangular area selection.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorObjectPalettePlacementPreset preset,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(preset);

        return PreviewSectorObjectPalettePlacementPreset(
            scenePreview,
            EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection),
            preset,
            renderRequest
        );
    }

    /// <summary>
    /// Builds one live placement preview for a proto-backed palette placement request on the current persisted map selection state.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacement(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorObjectPalettePlacementRequest request,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(request);

        return selection.Area is { } areaSelection
            ? PreviewSectorObjectPalettePlacement(scenePreview, areaSelection, request, renderRequest)
            : PreviewSectorObjectPalettePlacement(
                scenePreview,
                ResolveScenePointSelectionBySector(scenePreview, selection),
                request,
                renderRequest
            );
    }

    /// <summary>
    /// Builds one live placement preview for a reusable palette placement set on the current persisted map selection state.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementSet(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorObjectPalettePlacementSet placementSet,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(placementSet);

        return selection.Area is { } areaSelection
            ? PreviewSectorObjectPalettePlacementSet(scenePreview, areaSelection, placementSet, renderRequest)
            : PreviewSectorObjectPalettePlacementSet(
                scenePreview,
                ResolveScenePointSelectionBySector(scenePreview, selection),
                placementSet,
                renderRequest
            );
    }

    /// <summary>
    /// Builds one live placement preview for a named palette placement preset on the current persisted map selection state.
    /// </summary>
    public EditorMapPlacementPreview PreviewSectorObjectPalettePlacementPreset(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorObjectPalettePlacementPreset preset,
        EditorMapFloorRenderRequest? renderRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(preset);

        return PreviewSectorObjectPalettePlacementSet(
            scenePreview,
            selection,
            preset.CreatePlacementSet(),
            renderRequest
        );
    }

    private EditorMapPlacementPreview PreviewTrackedObjectPlacementTool(
        EditorMapScenePreview scenePreview,
        EditorProjectMapViewState mapViewState,
        EditorMapFloorRenderRequest? renderRequest
    )
    {
        var objectPlacementState = NormalizeProjectMapObjectPlacementToolState(mapViewState.WorldEdit.ObjectPlacement);
        return objectPlacementState.Mode switch
        {
            EditorProjectMapObjectPlacementMode.SinglePlacement
                when objectPlacementState.PlacementRequest is { } request => PreviewSectorObjectPalettePlacement(
                scenePreview,
                mapViewState.Selection,
                request,
                renderRequest
            ),
            EditorProjectMapObjectPlacementMode.PlacementSet
                when objectPlacementState.PlacementSet is { } placementSet => PreviewSectorObjectPalettePlacementSet(
                scenePreview,
                mapViewState.Selection,
                placementSet,
                renderRequest
            ),
            EditorProjectMapObjectPlacementMode.PlacementPreset
                when objectPlacementState.FindSelectedPreset() is { } preset =>
                PreviewSectorObjectPalettePlacementPreset(scenePreview, mapViewState.Selection, preset, renderRequest),
            EditorProjectMapObjectPlacementMode.SinglePlacement => throw new InvalidOperationException(
                $"The tracked object-placement tool for map view '{mapViewState.Id}' does not define one placement request."
            ),
            EditorProjectMapObjectPlacementMode.PlacementSet => throw new InvalidOperationException(
                $"The tracked object-placement tool for map view '{mapViewState.Id}' does not define one placement set."
            ),
            EditorProjectMapObjectPlacementMode.PlacementPreset => throw new InvalidOperationException(
                $"The tracked object-placement tool for map view '{mapViewState.Id}' does not resolve one selected placement preset."
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(objectPlacementState.Mode),
                objectPlacementState.Mode,
                "Unknown map object-placement mode."
            ),
        };
    }

    private IReadOnlyList<MobData> ApplyTrackedObjectPlacementTool(
        EditorMapScenePreview scenePreview,
        EditorProjectMapViewState mapViewState
    )
    {
        var objectPlacementState = NormalizeProjectMapObjectPlacementToolState(mapViewState.WorldEdit.ObjectPlacement);
        return objectPlacementState.Mode switch
        {
            EditorProjectMapObjectPlacementMode.SinglePlacement
                when objectPlacementState.PlacementRequest is { } request => ApplySectorObjectPalettePlacement(
                scenePreview,
                mapViewState.Selection,
                request
            ),
            EditorProjectMapObjectPlacementMode.PlacementSet
                when objectPlacementState.PlacementSet is { } placementSet => ApplySectorObjectPalettePlacementSet(
                scenePreview,
                mapViewState.Selection,
                placementSet
            ),
            EditorProjectMapObjectPlacementMode.PlacementPreset
                when objectPlacementState.FindSelectedPreset() is { } preset => ApplySectorObjectPalettePlacementPreset(
                scenePreview,
                mapViewState.Selection,
                preset
            ),
            EditorProjectMapObjectPlacementMode.SinglePlacement => throw new InvalidOperationException(
                $"The tracked object-placement tool for map view '{mapViewState.Id}' does not define one placement request."
            ),
            EditorProjectMapObjectPlacementMode.PlacementSet => throw new InvalidOperationException(
                $"The tracked object-placement tool for map view '{mapViewState.Id}' does not define one placement set."
            ),
            EditorProjectMapObjectPlacementMode.PlacementPreset => throw new InvalidOperationException(
                $"The tracked object-placement tool for map view '{mapViewState.Id}' does not resolve one selected placement preset."
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(objectPlacementState.Mode),
                objectPlacementState.Mode,
                "Unknown map object-placement mode."
            ),
        };
    }

    private EditorProjectMapObjectPlacementToolState SetTrackedObjectPlacementToolState(
        string mapViewStateId,
        EditorProjectMapObjectPlacementToolState objectPlacementToolState,
        bool activateTool
    )
    {
        var mapViewState = ResolveTrackedMapViewState(mapViewStateId);
        var worldEditState = NormalizeProjectMapWorldEditState(mapViewState.WorldEdit);
        var normalizedObjectPlacementState = NormalizeProjectMapObjectPlacementToolState(objectPlacementToolState);
        _ = SetMapWorldEditState(
            mapViewStateId,
            new EditorProjectMapWorldEditState
            {
                ActiveTool = activateTool
                    ? EditorProjectMapWorldEditActiveTool.ObjectPlacement
                    : worldEditState.ActiveTool,
                Terrain = worldEditState.Terrain,
                ObjectPlacement = normalizedObjectPlacementState,
            }
        );

        return normalizedObjectPlacementState;
    }

    private static IReadOnlyList<EditorObjectPalettePlacementPreset> UpsertPlacementPreset(
        IReadOnlyList<EditorObjectPalettePlacementPreset> presetLibrary,
        EditorObjectPalettePlacementPreset preset
    )
    {
        ArgumentNullException.ThrowIfNull(presetLibrary);
        ArgumentNullException.ThrowIfNull(preset);

        var updatedPresetLibrary = presetLibrary.ToList();
        var existingIndex = updatedPresetLibrary.FindIndex(existing =>
            string.Equals(existing.PresetId, preset.PresetId, StringComparison.OrdinalIgnoreCase)
        );
        if (existingIndex >= 0)
            updatedPresetLibrary[existingIndex] = preset;
        else
            updatedPresetLibrary.Add(preset);

        return updatedPresetLibrary;
    }

    private EditorProjectMapViewState ResolveTrackedMapViewState(string mapViewStateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapViewStateId);

        var mapViewState = _projectMapViewStates.FirstOrDefault(existing =>
            string.Equals(existing.Id, mapViewStateId, StringComparison.OrdinalIgnoreCase)
        );
        if (mapViewState is null)
            throw new InvalidOperationException($"No tracked map-view state matched '{mapViewStateId}'.");

        return mapViewState;
    }

    private EditorMapScenePreview CreateEffectiveMapScenePreview(string mapName)
    {
        var effectiveWorkspace = HasPendingChanges ? BuildPendingWorkspaceState().Workspace : Workspace;
        return effectiveWorkspace.CreateMapScenePreview(mapName, EditorArtResolverBindingStrategy.Conservative);
    }

    private static EditorProjectMapCameraState CreateCenteredTileCamera(EditorMapScenePreview scenePreview)
    {
        ArgumentNullException.ThrowIfNull(scenePreview);

        if (scenePreview.Sectors.Count == 0)
            return new EditorProjectMapCameraState();

        var sectorTileWidth = scenePreview.Sectors[0].TileWidth;
        var sectorTileHeight = scenePreview.Sectors[0].TileHeight;
        if (sectorTileWidth <= 0 || sectorTileHeight <= 0)
            return new EditorProjectMapCameraState();

        return new EditorProjectMapCameraState
        {
            CenterTileX = (scenePreview.Width * sectorTileWidth) / 2d,
            CenterTileY = (scenePreview.Height * sectorTileHeight) / 2d,
            Zoom = 1d,
        };
    }

    private static EditorMapFloorRenderRequest ComposeMapViewRenderRequest(
        EditorProjectMapPreviewState previewState,
        EditorMapFloorRenderRequest? renderRequest
    ) => (renderRequest ?? new EditorMapFloorRenderRequest()).WithPreviewState(previewState);

    private EditorMapPlacementPreview BuildPlacementPreview(
        EditorMapScenePreview scenePreview,
        IReadOnlyList<(EditorMapPlacementPreviewObject Object, double SortKey)> previewObjects,
        EditorMapFloorRenderRequest? renderRequest
    )
    {
        var sceneRender = EditorMapFloorRenderBuilder.Build(scenePreview, renderRequest);

        var minLeft = 0d;
        var minTop = 0d;
        var maxRight = sceneRender.WidthPixels;
        var maxBottom = sceneRender.HeightPixels;

        foreach (var previewObject in previewObjects)
            ExpandPlacementPreviewBounds(previewObject.Object, ref minLeft, ref minTop, ref maxRight, ref maxBottom);

        var shiftX = minLeft < 0d ? -minLeft : 0d;
        var shiftY = minTop < 0d ? -minTop : 0d;
        var widthPixels = maxRight - minLeft;
        var heightPixels = maxBottom - minTop;

        var shiftedPreviewObjects = previewObjects
            .OrderBy(static previewObject => previewObject.SortKey)
            .ThenBy(static previewObject => previewObject.Object.MapTileX)
            .ThenBy(static previewObject => previewObject.Object.MapTileY)
            .Select(
                (previewObject, drawOrder) =>
                    new EditorMapPlacementPreviewObject
                    {
                        SectorAssetPath = previewObject.Object.SectorAssetPath,
                        ProtoId = previewObject.Object.ProtoId,
                        ObjectType = previewObject.Object.ObjectType,
                        CurrentArtId = previewObject.Object.CurrentArtId,
                        MapTileX = previewObject.Object.MapTileX,
                        MapTileY = previewObject.Object.MapTileY,
                        Tile = previewObject.Object.Tile,
                        DrawOrder = drawOrder,
                        AnchorX = previewObject.Object.AnchorX + shiftX,
                        AnchorY = previewObject.Object.AnchorY + shiftY,
                        SpriteBounds = previewObject.Object.SpriteBounds,
                        IsTileGridSnapped = previewObject.Object.IsTileGridSnapped,
                        State = previewObject.Object.State,
                        ValidationMessage = previewObject.Object.ValidationMessage,
                        SuggestedOpacity = previewObject.Object.SuggestedOpacity,
                        SuggestedTintColor = previewObject.Object.SuggestedTintColor,
                        Rotation = previewObject.Object.Rotation,
                        RotationPitch = previewObject.Object.RotationPitch,
                    }
            )
            .ToArray();

        var renderQueue = BuildPlacementRenderQueue(sceneRender, previewObjects, shiftedPreviewObjects, shiftX, shiftY);
        return new EditorMapPlacementPreview
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            TileWidthPixels = sceneRender.TileWidthPixels,
            TileHeightPixels = sceneRender.TileHeightPixels,
            WidthPixels = widthPixels,
            HeightPixels = heightPixels,
            Objects = shiftedPreviewObjects,
            RenderQueue = renderQueue,
        };
    }

    private IReadOnlyList<(EditorMapPlacementPreviewObject Object, double SortKey)> BuildPlacementPreviewObjects(
        EditorMapScenePreview scenePreview,
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        IReadOnlyList<EditorObjectPalettePlacementRequest> requests,
        EditorMapFloorRenderRequest? renderRequest
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(requests);

        if (sectorHitGroups.Count == 0 || requests.Count == 0 || scenePreview.Sectors.Count == 0)
            return [];

        var artResolver = Workspace.CreateArtResolver(EditorArtResolverBindingStrategy.Conservative);
        var tileWidth = scenePreview.Sectors[0].TileWidth;
        var tileHeight = scenePreview.Sectors[0].TileHeight;
        var mapTileHeight = checked(scenePreview.Height * tileHeight);
        var renderProjectionRequest = renderRequest ?? new EditorMapFloorRenderRequest();
        var (normalizedOffsetX, normalizedOffsetY) = ResolveRenderNormalizationOffset(
            scenePreview,
            renderProjectionRequest
        );

        List<(EditorMapPlacementPreviewObject Object, double SortKey)> previewObjects = [];
        var sectorLookup = scenePreview.Sectors.ToDictionary(
            sector => sector.AssetPath,
            StringComparer.OrdinalIgnoreCase
        );
        for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
        {
            var sectorHitGroup = sectorHitGroups[groupIndex];
            ArgumentNullException.ThrowIfNull(sectorHitGroup);

            if (sectorHitGroup.Hits.Count == 0)
                continue;

            var stagedTiles = new HashSet<Location>();
            for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
            {
                var hit = sectorHitGroup.Hits[hitIndex];
                if (!stagedTiles.Add(hit.Tile))
                    continue;

                for (var requestIndex = 0; requestIndex < requests.Count; requestIndex++)
                {
                    var request = requests[requestIndex];
                    ArgumentNullException.ThrowIfNull(request);

                    var previewMob = CreateSectorObjectFromPalettePlacement(
                        GetCurrentProtoAssetForPlacement(request),
                        request,
                        hit.Tile.X,
                        hit.Tile.Y
                    );
                    var previewObject = EditorMapScenePreviewBuilder.BuildObjectPreview(
                        previewMob,
                        artResolver.FindArt
                    );
                    if (previewObject.Location is not { } location)
                        continue;

                    var mapTileX = checked((sectorHitGroup.LocalX * tileWidth) + location.X);
                    var mapTileY = checked((sectorHitGroup.LocalY * tileHeight) + location.Y);
                    var adjustedMapTileY = mapTileHeight - 1 - mapTileY;
                    var baseTileDrawOrder = EditorMapFloorRenderBuilder.GetDrawOrder(
                        renderProjectionRequest.ViewMode,
                        checked(scenePreview.Width * tileWidth),
                        mapTileX,
                        adjustedMapTileY
                    );
                    var (tileCenterX, tileCenterY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
                        renderProjectionRequest.ViewMode,
                        renderProjectionRequest.TileWidthPixels,
                        renderProjectionRequest.TileHeightPixels,
                        mapTileX,
                        adjustedMapTileY
                    );
                    var (anchorX, anchorY) = EditorMapFloorRenderBuilder.ProjectObjectAnchor(
                        tileCenterX,
                        tileCenterY,
                        previewObject
                    );
                    var sortKey = EditorMapFloorRenderBuilder.GetObjectSortKey(baseTileDrawOrder, previewObject);
                    var placementState = ResolvePlacementPreviewState(sectorLookup, hit.SectorAssetPath, location);

                    previewObjects.Add(
                        (
                            new EditorMapPlacementPreviewObject
                            {
                                SectorAssetPath = hit.SectorAssetPath,
                                ProtoId = previewObject.ProtoId,
                                ObjectType = previewObject.ObjectType,
                                CurrentArtId = previewObject.CurrentArtId,
                                MapTileX = mapTileX,
                                MapTileY = mapTileY,
                                Tile = location,
                                DrawOrder = 0,
                                AnchorX = anchorX + normalizedOffsetX,
                                AnchorY = anchorY + normalizedOffsetY,
                                SpriteBounds = previewObject.SpriteBounds,
                                IsTileGridSnapped = previewObject.IsTileGridSnapped,
                                State = placementState,
                                ValidationMessage = GetPlacementValidationMessage(placementState),
                                SuggestedOpacity = GetPlacementSuggestedOpacity(placementState),
                                SuggestedTintColor = GetPlacementSuggestedTintColor(placementState),
                                Rotation = previewObject.Rotation,
                                RotationPitch = previewObject.RotationPitch,
                            },
                            sortKey
                        )
                    );
                }
            }
        }

        return ApplyPreviewTileOccupancyHeuristic(previewObjects);
    }

    private static IReadOnlyList<EditorMapRenderQueueItem> BuildPlacementRenderQueue(
        EditorMapFloorRenderPreview sceneRender,
        IReadOnlyList<(EditorMapPlacementPreviewObject Object, double SortKey)> previewObjects,
        IReadOnlyList<EditorMapPlacementPreviewObject> shiftedPreviewObjects,
        double shiftX,
        double shiftY
    )
    {
        List<(double SortKey, EditorMapRenderQueueItemKind Kind, int Index)> queueEntries = [];

        for (var index = 0; index < sceneRender.RenderQueue.Count; index++)
            queueEntries.Add((sceneRender.RenderQueue[index].SortKey, sceneRender.RenderQueue[index].Kind, index));

        for (var index = 0; index < previewObjects.Count; index++)
            queueEntries.Add(
                (previewObjects[index].SortKey, EditorMapRenderQueueItemKind.PlacementPreviewObject, index)
            );

        return queueEntries
            .OrderBy(static entry => entry.SortKey)
            .ThenBy(static entry => entry.Kind)
            .ThenBy(static entry => entry.Index)
            .Select(
                (entry, drawOrder) =>
                {
                    if (entry.Kind is EditorMapRenderQueueItemKind.PlacementPreviewObject)
                    {
                        return new EditorMapRenderQueueItem
                        {
                            Kind = entry.Kind,
                            DrawOrder = drawOrder,
                            SortKey = entry.SortKey,
                            PlacementPreviewObject = shiftedPreviewObjects[entry.Index],
                        };
                    }

                    var sceneItem = sceneRender.RenderQueue[entry.Index];
                    return sceneItem.Kind switch
                    {
                        EditorMapRenderQueueItemKind.FloorTile => new EditorMapRenderQueueItem
                        {
                            Kind = sceneItem.Kind,
                            DrawOrder = drawOrder,
                            SortKey = sceneItem.SortKey,
                            Tile = ShiftTile(sceneItem.Tile!, shiftX, shiftY),
                        },
                        EditorMapRenderQueueItemKind.TileOverlay => new EditorMapRenderQueueItem
                        {
                            Kind = sceneItem.Kind,
                            DrawOrder = drawOrder,
                            SortKey = sceneItem.SortKey,
                            TileOverlay = ShiftTileOverlay(sceneItem.TileOverlay!, shiftX, shiftY),
                        },
                        EditorMapRenderQueueItemKind.Object => new EditorMapRenderQueueItem
                        {
                            Kind = sceneItem.Kind,
                            DrawOrder = drawOrder,
                            SortKey = sceneItem.SortKey,
                            Object = ShiftObject(sceneItem.Object!, shiftX, shiftY),
                        },
                        EditorMapRenderQueueItemKind.Roof => new EditorMapRenderQueueItem
                        {
                            Kind = sceneItem.Kind,
                            DrawOrder = drawOrder,
                            SortKey = sceneItem.SortKey,
                            Roof = ShiftRoof(sceneItem.Roof!, shiftX, shiftY),
                        },
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(sceneItem.Kind),
                            sceneItem.Kind,
                            "Unsupported render queue kind."
                        ),
                    };
                }
            )
            .ToArray();
    }

    private static EditorMapFloorTileRenderItem ShiftTile(
        EditorMapFloorTileRenderItem tile,
        double shiftX,
        double shiftY
    ) =>
        shiftX == 0d && shiftY == 0d
            ? tile
            : new EditorMapFloorTileRenderItem
            {
                SectorAssetPath = tile.SectorAssetPath,
                MapTileX = tile.MapTileX,
                MapTileY = tile.MapTileY,
                Tile = tile.Tile,
                ArtId = tile.ArtId,
                IsBlocked = tile.IsBlocked,
                HasLight = tile.HasLight,
                HasScript = tile.HasScript,
                DrawOrder = tile.DrawOrder,
                CenterX = tile.CenterX + shiftX,
                CenterY = tile.CenterY + shiftY,
            };

    private static EditorMapTileOverlayRenderItem ShiftTileOverlay(
        EditorMapTileOverlayRenderItem tileOverlay,
        double shiftX,
        double shiftY
    ) =>
        shiftX == 0d && shiftY == 0d
            ? tileOverlay
            : new EditorMapTileOverlayRenderItem
            {
                SectorAssetPath = tileOverlay.SectorAssetPath,
                MapTileX = tileOverlay.MapTileX,
                MapTileY = tileOverlay.MapTileY,
                Tile = tileOverlay.Tile,
                Kind = tileOverlay.Kind,
                DrawOrder = tileOverlay.DrawOrder,
                CenterX = tileOverlay.CenterX + shiftX,
                CenterY = tileOverlay.CenterY + shiftY,
                SuggestedOpacity = tileOverlay.SuggestedOpacity,
                SuggestedTintColor = tileOverlay.SuggestedTintColor,
            };

    private static EditorMapObjectRenderItem ShiftObject(EditorMapObjectRenderItem obj, double shiftX, double shiftY) =>
        shiftX == 0d && shiftY == 0d
            ? obj
            : new EditorMapObjectRenderItem
            {
                SectorAssetPath = obj.SectorAssetPath,
                ObjectId = obj.ObjectId,
                ProtoId = obj.ProtoId,
                ObjectType = obj.ObjectType,
                CurrentArtId = obj.CurrentArtId,
                MapTileX = obj.MapTileX,
                MapTileY = obj.MapTileY,
                Tile = obj.Tile,
                DrawOrder = obj.DrawOrder,
                AnchorX = obj.AnchorX + shiftX,
                AnchorY = obj.AnchorY + shiftY,
                SpriteBounds = obj.SpriteBounds,
                IsTileGridSnapped = obj.IsTileGridSnapped,
            };

    private static EditorMapRoofRenderItem ShiftRoof(EditorMapRoofRenderItem roof, double shiftX, double shiftY) =>
        shiftX == 0d && shiftY == 0d
            ? roof
            : new EditorMapRoofRenderItem
            {
                SectorAssetPath = roof.SectorAssetPath,
                RoofCell = roof.RoofCell,
                MapTileX = roof.MapTileX,
                MapTileY = roof.MapTileY,
                ArtId = roof.ArtId,
                DrawOrder = roof.DrawOrder,
                AnchorX = roof.AnchorX + shiftX,
                AnchorY = roof.AnchorY + shiftY,
            };

    private static void ExpandPlacementPreviewBounds(
        EditorMapPlacementPreviewObject previewObject,
        ref double minLeft,
        ref double minTop,
        ref double maxRight,
        ref double maxBottom
    ) =>
        EditorMapFloorRenderBuilder.ExpandObjectBounds(
            previewObject.SpriteBounds,
            previewObject.AnchorX,
            previewObject.AnchorY,
            ref minLeft,
            ref minTop,
            ref maxRight,
            ref maxBottom
        );

    private static IReadOnlyList<(
        EditorMapPlacementPreviewObject Object,
        double SortKey
    )> ApplyPreviewTileOccupancyHeuristic(
        IReadOnlyList<(EditorMapPlacementPreviewObject Object, double SortKey)> previewObjects
    )
    {
        var duplicateTiles = previewObjects
            .GroupBy(static preview => (preview.Object.SectorAssetPath, preview.Object.Tile))
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToHashSet();

        if (duplicateTiles.Count == 0)
            return previewObjects;

        return previewObjects
            .Select(previewTuple =>
            {
                var preview = previewTuple.Object;
                var state = duplicateTiles.Contains((preview.SectorAssetPath, preview.Tile))
                    ? (
                        preview.State is EditorMapPlacementPreviewState.BlockedTile
                            ? EditorMapPlacementPreviewState.BlockedTile
                            : EditorMapPlacementPreviewState.OccupiedTile
                    )
                    : preview.State;
                return (
                    new EditorMapPlacementPreviewObject
                    {
                        SectorAssetPath = preview.SectorAssetPath,
                        ProtoId = preview.ProtoId,
                        ObjectType = preview.ObjectType,
                        CurrentArtId = preview.CurrentArtId,
                        MapTileX = preview.MapTileX,
                        MapTileY = preview.MapTileY,
                        Tile = preview.Tile,
                        DrawOrder = preview.DrawOrder,
                        AnchorX = preview.AnchorX,
                        AnchorY = preview.AnchorY,
                        SpriteBounds = preview.SpriteBounds,
                        IsTileGridSnapped = preview.IsTileGridSnapped,
                        State = state,
                        ValidationMessage = GetPlacementValidationMessage(state),
                        SuggestedOpacity = GetPlacementSuggestedOpacity(state),
                        SuggestedTintColor = GetPlacementSuggestedTintColor(state),
                        Rotation = preview.Rotation,
                        RotationPitch = preview.RotationPitch,
                    },
                    previewTuple.SortKey
                );
            })
            .ToArray();
    }

    private static EditorMapPlacementPreviewState ResolvePlacementPreviewState(
        IReadOnlyDictionary<string, EditorMapSectorScenePreview> sectorLookup,
        string sectorAssetPath,
        Location tile
    )
    {
        if (!sectorLookup.TryGetValue(sectorAssetPath, out var sector))
            return EditorMapPlacementPreviewState.Valid;

        if (sector.IsTileBlocked(tile.X, tile.Y))
            return EditorMapPlacementPreviewState.BlockedTile;

        return sector.Objects.Any(obj => obj.Location == tile)
            ? EditorMapPlacementPreviewState.OccupiedTile
            : EditorMapPlacementPreviewState.Valid;
    }

    private static string? GetPlacementValidationMessage(EditorMapPlacementPreviewState state) =>
        state switch
        {
            EditorMapPlacementPreviewState.Valid => null,
            EditorMapPlacementPreviewState.BlockedTile => "Targets a blocked tile.",
            EditorMapPlacementPreviewState.OccupiedTile => "Targets an occupied tile.",
            _ => null,
        };

    private static double GetPlacementSuggestedOpacity(EditorMapPlacementPreviewState state) =>
        state switch
        {
            EditorMapPlacementPreviewState.Valid => 0.85d,
            EditorMapPlacementPreviewState.BlockedTile or EditorMapPlacementPreviewState.OccupiedTile => 0.55d,
            _ => 0.75d,
        };

    private static uint? GetPlacementSuggestedTintColor(EditorMapPlacementPreviewState state) =>
        state switch
        {
            EditorMapPlacementPreviewState.Valid => 0xAA66CC66u,
            EditorMapPlacementPreviewState.BlockedTile => 0xAACC6666u,
            EditorMapPlacementPreviewState.OccupiedTile => 0xAACCA066u,
            _ => null,
        };

    private (double OffsetX, double OffsetY) ResolveRenderNormalizationOffset(
        EditorMapScenePreview scenePreview,
        EditorMapFloorRenderRequest? renderRequest
    )
    {
        var sceneRender = EditorMapFloorRenderBuilder.Build(scenePreview, renderRequest);
        if (scenePreview.Sectors.Count == 0)
            return (0d, 0d);

        var request = renderRequest ?? new EditorMapFloorRenderRequest();
        var tileWidth = scenePreview.Sectors[0].TileWidth;
        var tileHeight = scenePreview.Sectors[0].TileHeight;
        var mapTileHeight = checked(scenePreview.Height * tileHeight);

        if (sceneRender.Tiles.Count > 0)
        {
            var firstTile = sceneRender.Tiles[0];
            var adjustedMapTileY = mapTileHeight - 1 - firstTile.MapTileY;
            var (rawCenterX, rawCenterY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
                request.ViewMode,
                request.TileWidthPixels,
                request.TileHeightPixels,
                firstTile.MapTileX,
                adjustedMapTileY
            );

            return (firstTile.CenterX - rawCenterX, firstTile.CenterY - rawCenterY);
        }

        if (sceneRender.Objects.Count > 0)
        {
            var firstObject = sceneRender.Objects[0];
            var sceneObject = FindSceneObjectPreview(scenePreview, firstObject.SectorAssetPath, firstObject.ObjectId);
            if (sceneObject?.Location is { } location)
            {
                var mapTileX = firstObject.MapTileX;
                var mapTileY = firstObject.MapTileY;
                var adjustedMapTileY = mapTileHeight - 1 - mapTileY;
                var (tileCenterX, tileCenterY) = EditorMapFloorRenderBuilder.ProjectTileCenter(
                    request.ViewMode,
                    request.TileWidthPixels,
                    request.TileHeightPixels,
                    mapTileX,
                    adjustedMapTileY
                );
                var (rawAnchorX, rawAnchorY) = EditorMapFloorRenderBuilder.ProjectObjectAnchor(
                    tileCenterX,
                    tileCenterY,
                    sceneObject
                );

                return (firstObject.AnchorX - rawAnchorX, firstObject.AnchorY - rawAnchorY);
            }
        }

        return (0d, 0d);
    }

    private static EditorMapObjectPreview? FindSceneObjectPreview(
        EditorMapScenePreview scenePreview,
        string sectorAssetPath,
        GameObjectGuid objectId
    ) =>
        scenePreview
            .Sectors.FirstOrDefault(sector =>
                string.Equals(sector.AssetPath, sectorAssetPath, StringComparison.OrdinalIgnoreCase)
            )
            ?.Objects.FirstOrDefault(obj => obj.ObjectId == objectId);

    private static IReadOnlyList<EditorMapSceneSectorHitGroup> ResolveScenePointSelectionBySector(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection
    )
    {
        if (selection.SectorAssetPath is null || selection.Tile is null)
            return [];

        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AssetPath, selection.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (sector is null)
            return [];

        var tile = selection.Tile.Value;
        var mapTileX = (sector.LocalX * sector.TileWidth) + tile.X;
        var mapTileY = (sector.LocalY * sector.TileHeight) + tile.Y;

        return
        [
            new EditorMapSceneSectorHitGroup
            {
                SectorAssetPath = sector.AssetPath,
                LocalX = sector.LocalX,
                LocalY = sector.LocalY,
                Hits =
                [
                    new EditorMapSceneHit
                    {
                        MapTileX = mapTileX,
                        MapTileY = mapTileY,
                        SectorAssetPath = sector.AssetPath,
                        Tile = tile,
                        ObjectHits = [],
                    },
                ],
            },
        ];
    }

    private IReadOnlyList<MobData> AddSectorObjectsFromPalettePlacement(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProtoNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.ProtoNumber,
                "Proto numbers must be positive."
            );
        }

        if (sectorHitGroups.Count == 0)
            return [];

        var protoAsset =
            Workspace.Index.FindProtoDefinition(request.ProtoNumber)
            ?? throw new InvalidOperationException(
                $"Cannot instantiate object from proto {request.ProtoNumber} because no loaded proto definition matched that identifier."
            );
        var proto = GetCurrentProtoAsset(protoAsset.AssetPath);

        return TrackDirectAssetEdit(
            () =>
            {
                var createdObjects = new List<MobData>();

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    SectorBuilder? builder = null;
                    var stagedTiles = new HashSet<Location>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        if (!stagedTiles.Add(hit.Tile))
                            continue;

                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.X);
                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.Y);

                        var createdObject = CreateSectorObjectFromPalettePlacement(
                            proto,
                            request,
                            hit.Tile.X,
                            hit.Tile.Y
                        );
                        builder ??= new SectorBuilder(currentSector);
                        builder.AddObject(createdObject);
                        createdObjects.Add(createdObject);
                    }

                    if (builder is null)
                        continue;

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                }

                return createdObjects.ToArray();
            },
            static createdObjects => createdObjects.Length > 0
        );
    }

    private IReadOnlyList<MobData> AddSectorObjectsFromPalettePlacementSet(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorObjectPalettePlacementSet placementSet
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(placementSet);

        if (!placementSet.HasEntries)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var createdObjects = new List<MobData>();

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    SectorBuilder? builder = null;
                    var stagedTiles = new HashSet<Location>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        if (!stagedTiles.Add(hit.Tile))
                            continue;

                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.X);
                        ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.Y);

                        for (var entryIndex = 0; entryIndex < placementSet.Entries.Count; entryIndex++)
                        {
                            var entry = placementSet.Entries[entryIndex];
                            ArgumentNullException.ThrowIfNull(entry);

                            var createdObject = CreateSectorObjectFromPalettePlacement(
                                GetCurrentProtoAssetForPlacement(entry),
                                entry,
                                hit.Tile.X,
                                hit.Tile.Y
                            );
                            builder ??= new SectorBuilder(currentSector);
                            builder.AddObject(createdObject);
                            createdObjects.Add(createdObject);
                        }
                    }

                    if (builder is null)
                        continue;

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                }

                return createdObjects.ToArray();
            },
            static createdObjects => createdObjects.Length > 0
        );
    }

    /// <summary>
    /// Stages one placed object addition on a loaded sector asset.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the sector already contains an object with the same object identifier.
    /// </exception>
    public EditorSessionChange AddSectorObject(string assetPath, MobData obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                if (sector.Objects.Any(existing => existing.Header.ObjectId == obj.Header.ObjectId))
                {
                    throw new InvalidOperationException(
                        $"Sector asset '{normalizedPath}' already contains object {obj.Header.ObjectId}."
                    );
                }

                return new SectorBuilder(sector).AddObject(obj).Build();
            }
        )!;
    }

    /// <summary>
    /// Stages one grouped placed-object erase pass from scene-sector hit groups.
    /// Returns removed object IDs in stable grouped-hit order so hosts can reconcile selection state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any referenced object no longer exists in the addressed sector asset.
    /// </exception>
    public IReadOnlyList<GameObjectGuid> RemoveSectorObjects(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);

        if (sectorHitGroups.Count == 0)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var removedObjectIds = new List<GameObjectGuid>();

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    var orderedObjectIds = new List<GameObjectGuid>();
                    var uniqueObjectIds = new HashSet<GameObjectGuid>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        for (var objectIndex = 0; objectIndex < hit.ObjectHits.Count; objectIndex++)
                        {
                            var objectId = hit.ObjectHits[objectIndex].ObjectId;
                            if (!uniqueObjectIds.Add(objectId))
                                continue;

                            orderedObjectIds.Add(objectId);
                        }
                    }

                    if (orderedObjectIds.Count == 0)
                        continue;

                    var removalIndices = new List<int>(orderedObjectIds.Count);
                    for (var objectIndex = 0; objectIndex < orderedObjectIds.Count; objectIndex++)
                        removalIndices.Add(
                            FindSectorObjectIndex(currentSector, normalizedPath, orderedObjectIds[objectIndex])
                        );

                    removalIndices.Sort();

                    var builder = new SectorBuilder(currentSector);
                    for (var removalIndex = removalIndices.Count - 1; removalIndex >= 0; removalIndex--)
                        builder.RemoveObject(removalIndices[removalIndex]);

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                    removedObjectIds.AddRange(orderedObjectIds);
                }

                return removedObjectIds.ToArray();
            },
            static removedObjectIds => removedObjectIds.Length > 0
        );
    }

    /// <summary>
    /// Applies one typed object-brush request to grouped scene-sector hits.
    /// </summary>
    public EditorMapObjectBrushResult ApplySectorObjectBrush(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorMapObjectBrushRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        return request.Mode switch
        {
            EditorMapObjectBrushMode.StampFromProto => new EditorMapObjectBrushResult
            {
                CreatedObjects = AddSectorObjectsFromProto(sectorHitGroups, request.ProtoNumber),
            },
            EditorMapObjectBrushMode.ReplaceWithProto => ReplaceSectorObjectsFromProto(
                sectorHitGroups,
                request.ProtoNumber
            ),
            EditorMapObjectBrushMode.Erase => new EditorMapObjectBrushResult
            {
                RemovedObjectIds = RemoveSectorObjects(sectorHitGroups),
            },
            EditorMapObjectBrushMode.Rotate => new EditorMapObjectBrushResult
            {
                UpdatedObjectIds = SetSectorObjectRotation(sectorHitGroups, request.Rotation),
            },
            EditorMapObjectBrushMode.RotatePitch => new EditorMapObjectBrushResult
            {
                UpdatedObjectIds = SetSectorObjectRotationPitch(sectorHitGroups, request.RotationPitch),
            },
            EditorMapObjectBrushMode.MoveByOffset => new EditorMapObjectBrushResult
            {
                UpdatedObjectIds = MoveSectorObjectsByOffset(sectorHitGroups, request.DeltaTileX, request.DeltaTileY),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Mode, "Unknown object brush mode."),
        };
    }

    /// <summary>
    /// Applies one higher-level object transform request to grouped scene-sector hits.
    /// </summary>
    public EditorMapObjectBrushResult ApplySectorObjectTransform(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorMapObjectTransformRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        return new EditorMapObjectBrushResult { UpdatedObjectIds = TransformSectorObjects(sectorHitGroups, request) };
    }

    /// <summary>
    /// Applies one typed object-brush request to a persisted rectangular area selection on a scene preview.
    /// </summary>
    public EditorMapObjectBrushResult ApplySectorObjectBrush(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorMapObjectBrushRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(request);

        var groupedHits = EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection);
        var filteredGroupedHits =
            areaSelection.ObjectIds.Count == 0
                ? groupedHits
                : FilterSectorHitGroupsBySelectedObjectIds(groupedHits, areaSelection.ObjectIds);

        return ApplySectorObjectBrush(filteredGroupedHits, request);
    }

    /// <summary>
    /// Applies one higher-level object transform request to a persisted rectangular area selection on a scene preview.
    /// </summary>
    public EditorMapObjectBrushResult ApplySectorObjectTransform(
        EditorMapScenePreview scenePreview,
        EditorProjectMapAreaSelectionState areaSelection,
        EditorMapObjectTransformRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(areaSelection);
        ArgumentNullException.ThrowIfNull(request);

        var groupedHits = EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, areaSelection);
        var filteredGroupedHits =
            areaSelection.ObjectIds.Count == 0
                ? groupedHits
                : FilterSectorHitGroupsBySelectedObjectIds(groupedHits, areaSelection.ObjectIds);

        return ApplySectorObjectTransform(filteredGroupedHits, request);
    }

    /// <summary>
    /// Applies one typed object-brush request to the current persisted map selection state.
    /// </summary>
    public EditorMapObjectBrushResult ApplySectorObjectBrush(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorMapObjectBrushRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(request);

        if (selection.Area is { } areaSelection)
            return ApplySectorObjectBrush(scenePreview, areaSelection, request);

        if (selection.SectorAssetPath is null || selection.Tile is null)
            return new EditorMapObjectBrushResult();

        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AssetPath, selection.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (sector is null)
            return new EditorMapObjectBrushResult();

        var tile = selection.Tile.Value;
        var mapTileX = (sector.LocalX * sector.TileWidth) + tile.X;
        var mapTileY = (sector.LocalY * sector.TileHeight) + tile.Y;
        var selectedObjectIds = selection.GetSelectedObjectIds();
        var objectHits =
            selectedObjectIds.Count == 0
                ? Array.Empty<EditorMapObjectPreview>()
                : sector
                    .Objects.Where(candidate =>
                        candidate.Location == tile && selectedObjectIds.Contains(candidate.ObjectId)
                    )
                    .ToArray();

        return ApplySectorObjectBrush(
            [
                new EditorMapSceneSectorHitGroup
                {
                    SectorAssetPath = sector.AssetPath,
                    LocalX = sector.LocalX,
                    LocalY = sector.LocalY,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = mapTileX,
                            MapTileY = mapTileY,
                            SectorAssetPath = sector.AssetPath,
                            Tile = tile,
                            ObjectHits = objectHits,
                        },
                    ],
                },
            ],
            request
        );
    }

    /// <summary>
    /// Applies one higher-level object transform request to the current persisted map selection state.
    /// </summary>
    public EditorMapObjectBrushResult ApplySectorObjectTransform(
        EditorMapScenePreview scenePreview,
        EditorProjectMapSelectionState selection,
        EditorMapObjectTransformRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(scenePreview);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(request);

        if (selection.Area is { } areaSelection)
            return ApplySectorObjectTransform(scenePreview, areaSelection, request);

        if (selection.SectorAssetPath is null || selection.Tile is null)
            return new EditorMapObjectBrushResult();

        var sector = scenePreview.Sectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AssetPath, selection.SectorAssetPath, StringComparison.OrdinalIgnoreCase)
        );
        if (sector is null)
            return new EditorMapObjectBrushResult();

        var tile = selection.Tile.Value;
        var mapTileX = (sector.LocalX * sector.TileWidth) + tile.X;
        var mapTileY = (sector.LocalY * sector.TileHeight) + tile.Y;
        var selectedObjectIds = selection.GetSelectedObjectIds();
        var objectHits =
            selectedObjectIds.Count == 0
                ? Array.Empty<EditorMapObjectPreview>()
                : sector
                    .Objects.Where(candidate =>
                        candidate.Location == tile && selectedObjectIds.Contains(candidate.ObjectId)
                    )
                    .ToArray();

        return ApplySectorObjectTransform(
            [
                new EditorMapSceneSectorHitGroup
                {
                    SectorAssetPath = sector.AssetPath,
                    LocalX = sector.LocalX,
                    LocalY = sector.LocalY,
                    Hits =
                    [
                        new EditorMapSceneHit
                        {
                            MapTileX = mapTileX,
                            MapTileY = mapTileY,
                            SectorAssetPath = sector.AssetPath,
                            Tile = tile,
                            ObjectHits = objectHits,
                        },
                    ],
                },
            ],
            request
        );
    }

    /// <summary>
    /// Stages one placed-object move on a loaded sector asset.
    /// Returns <see langword="null"/> when the object is already at the requested location.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no unique object with <paramref name="objectId"/> exists in the sector.
    /// </exception>
    public EditorSessionChange? MoveSectorObject(string assetPath, GameObjectGuid objectId, int tileX, int tileY)
    {
        ValidateTileCoordinate(nameof(tileX), tileX);
        ValidateTileCoordinate(nameof(tileY), tileY);

        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                var objectIndex = FindSectorObjectIndex(sector, normalizedPath, objectId);
                var currentObject = sector.Objects[objectIndex];

                if (
                    TryGetObjectLocation(currentObject) is { } currentLocation
                    && currentLocation.X == tileX
                    && currentLocation.Y == tileY
                )
                    return null;

                var updatedObject = new MobDataBuilder(currentObject).WithLocation(tileX, tileY).Build();
                return new SectorBuilder(sector).ReplaceObject(objectIndex, updatedObject).Build();
            }
        );
    }

    private EditorMapObjectBrushResult ReplaceSectorObjectsFromProto(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        int protoNumber
    )
    {
        if (protoNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(protoNumber), protoNumber, "Proto numbers must be positive.");
        }

        if (sectorHitGroups.Count == 0)
            return new EditorMapObjectBrushResult();

        var protoAsset =
            Workspace.Index.FindProtoDefinition(protoNumber)
            ?? throw new InvalidOperationException(
                $"Cannot instantiate object from proto {protoNumber} because no loaded proto definition matched that identifier."
            );
        var proto = GetCurrentProtoAsset(protoAsset.AssetPath);

        return TrackDirectAssetEdit(
            () =>
            {
                var createdObjects = new List<MobData>();
                var removedObjectIds = new List<GameObjectGuid>();

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    var orderedTiles = new List<Location>();
                    var uniqueTiles = new HashSet<Location>();
                    var orderedObjectIds = new List<GameObjectGuid>();
                    var uniqueObjectIds = new HashSet<GameObjectGuid>();

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        if (uniqueTiles.Add(hit.Tile))
                        {
                            ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.X);
                            ValidateTileCoordinate(nameof(sectorHitGroup), hit.Tile.Y);
                            orderedTiles.Add(hit.Tile);
                        }

                        for (var objectIndex = 0; objectIndex < hit.ObjectHits.Count; objectIndex++)
                        {
                            var objectId = hit.ObjectHits[objectIndex].ObjectId;
                            if (!uniqueObjectIds.Add(objectId))
                                continue;

                            orderedObjectIds.Add(objectId);
                        }
                    }

                    if (orderedTiles.Count == 0 && orderedObjectIds.Count == 0)
                        continue;

                    var builder = new SectorBuilder(currentSector);

                    if (orderedObjectIds.Count > 0)
                    {
                        var removalIndices = new List<int>(orderedObjectIds.Count);
                        for (var objectIndex = 0; objectIndex < orderedObjectIds.Count; objectIndex++)
                        {
                            removalIndices.Add(
                                FindSectorObjectIndex(currentSector, normalizedPath, orderedObjectIds[objectIndex])
                            );
                        }

                        removalIndices.Sort();

                        for (var removalIndex = removalIndices.Count - 1; removalIndex >= 0; removalIndex--)
                            builder.RemoveObject(removalIndices[removalIndex]);

                        removedObjectIds.AddRange(orderedObjectIds);
                    }

                    for (var tileIndex = 0; tileIndex < orderedTiles.Count; tileIndex++)
                    {
                        var tile = orderedTiles[tileIndex];
                        var createdObject = CreateSectorObjectFromProto(proto, protoNumber, tile.X, tile.Y);
                        builder.AddObject(createdObject);
                        createdObjects.Add(createdObject);
                    }

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                }

                return new EditorMapObjectBrushResult
                {
                    CreatedObjects = createdObjects.ToArray(),
                    RemovedObjectIds = removedObjectIds.ToArray(),
                };
            },
            static result => result.HasChanges
        );
    }

    private IReadOnlyList<GameObjectGuid> SetSectorObjectRotation(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        float rotation
    ) => TransformSectorObjects(sectorHitGroups, EditorMapObjectTransformRequest.Rotate(rotation));

    private IReadOnlyList<GameObjectGuid> SetSectorObjectRotationPitch(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        float rotationPitch
    ) => TransformSectorObjects(sectorHitGroups, EditorMapObjectTransformRequest.RotatePitch(rotationPitch));

    private IReadOnlyList<GameObjectGuid> TransformSectorObjects(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        EditorMapObjectTransformRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(request);

        if (sectorHitGroups.Count == 0 || !request.HasChanges)
            return [];

        return TrackDirectAssetEdit(
            () =>
            {
                var updatedObjectIds = new List<GameObjectGuid>();

                for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
                {
                    var sectorHitGroup = sectorHitGroups[groupIndex];
                    ArgumentNullException.ThrowIfNull(sectorHitGroup);

                    if (sectorHitGroup.Hits.Count == 0)
                        continue;

                    var normalizedPath = NormalizeAssetPath(sectorHitGroup.SectorAssetPath);
                    var currentSector = GetCurrentSectorAsset(normalizedPath);
                    var builder = new SectorBuilder(currentSector);
                    var uniqueObjectIds = new HashSet<GameObjectGuid>();
                    var changedAnyObjects = false;

                    for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
                    {
                        var hit = sectorHitGroup.Hits[hitIndex];
                        if (!string.Equals(hit.SectorAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Grouped sector hit path '{hit.SectorAssetPath}' did not match '{normalizedPath}'."
                            );
                        }

                        for (var objectHitIndex = 0; objectHitIndex < hit.ObjectHits.Count; objectHitIndex++)
                        {
                            var objectId = hit.ObjectHits[objectHitIndex].ObjectId;
                            if (!uniqueObjectIds.Add(objectId))
                                continue;

                            var objectIndex = FindSectorObjectIndex(currentSector, normalizedPath, objectId);
                            var currentObject = currentSector.Objects[objectIndex];
                            var updatedBuilder = new MobDataBuilder(currentObject);
                            var objectChanged = false;

                            if (request.HasMoveOffset)
                            {
                                var currentLocation =
                                    TryGetObjectLocation(currentObject)
                                    ?? throw new InvalidOperationException(
                                        $"Sector asset '{normalizedPath}' object {objectId} does not expose a usable location for object transforms."
                                    );

                                var nextTileX = currentLocation.X + request.DeltaTileX;
                                var nextTileY = currentLocation.Y + request.DeltaTileY;
                                ValidateTileCoordinate(nameof(request.DeltaTileX), nextTileX);
                                ValidateTileCoordinate(nameof(request.DeltaTileY), nextTileY);

                                if (nextTileX != currentLocation.X || nextTileY != currentLocation.Y)
                                {
                                    updatedBuilder.WithLocation(nextTileX, nextTileY);
                                    objectChanged = true;
                                }
                            }

                            if (
                                request.Rotation.HasValue
                                && ShouldApplyObjectFloatProperty(
                                    currentObject,
                                    ObjectField.ObjFPadIas1,
                                    request.Rotation.Value
                                )
                            )
                            {
                                updatedBuilder.WithRotation(request.Rotation.Value);
                                objectChanged = true;
                            }

                            if (
                                request.RotationPitch.HasValue
                                && ShouldApplyObjectFloatProperty(
                                    currentObject,
                                    ObjectField.ObjFRotationPitch,
                                    request.RotationPitch.Value
                                )
                            )
                            {
                                updatedBuilder.WithRotationPitch(request.RotationPitch.Value);
                                objectChanged = true;
                            }

                            if (request.AlignToTileGrid && TrySnapObjectToTileGrid(currentObject, updatedBuilder))
                                objectChanged = true;

                            if (!objectChanged)
                                continue;

                            var updatedObject = updatedBuilder.Build();
                            builder.ReplaceObject(objectIndex, updatedObject);
                            updatedObjectIds.Add(objectId);
                            changedAnyObjects = true;
                        }
                    }

                    if (!changedAnyObjects)
                        continue;

                    _pendingSectorAssets[normalizedPath] = builder.Build();
                }

                return updatedObjectIds.ToArray();
            },
            static updatedObjectIds => updatedObjectIds.Length > 0
        );
    }

    private IReadOnlyList<GameObjectGuid> MoveSectorObjectsByOffset(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        int deltaTileX,
        int deltaTileY
    ) => TransformSectorObjects(sectorHitGroups, EditorMapObjectTransformRequest.MoveByOffset(deltaTileX, deltaTileY));

    private static bool ShouldApplyObjectFloatProperty(MobData currentObject, ObjectField field, float value)
    {
        var currentProperty = currentObject.GetProperty(field);
        if (currentProperty is null)
            return value != 0f;

        return currentProperty.GetFloat() != value;
    }

    private static bool TrySnapObjectToTileGrid(MobData currentObject, MobDataBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(currentObject);
        ArgumentNullException.ThrowIfNull(builder);

        var changed = false;

        if (TryGetObjectIntProperty(currentObject, ObjectField.ObjFOffsetX) is { } offsetX && offsetX != 0)
        {
            builder.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetX, 0));
            changed = true;
        }

        if (TryGetObjectIntProperty(currentObject, ObjectField.ObjFOffsetY) is { } offsetY && offsetY != 0)
        {
            builder.WithProperty(ObjectPropertyFactory.ForInt32(ObjectField.ObjFOffsetY, 0));
            changed = true;
        }

        return changed;
    }

    private static IReadOnlyList<EditorMapSceneSectorHitGroup> FilterSectorHitGroupsBySelectedObjectIds(
        IReadOnlyList<EditorMapSceneSectorHitGroup> sectorHitGroups,
        IReadOnlyList<GameObjectGuid> selectedObjectIds
    )
    {
        ArgumentNullException.ThrowIfNull(sectorHitGroups);
        ArgumentNullException.ThrowIfNull(selectedObjectIds);

        if (sectorHitGroups.Count == 0 || selectedObjectIds.Count == 0)
            return sectorHitGroups;

        var selectedObjectIdSet = selectedObjectIds.ToHashSet();
        var filteredGroups = new EditorMapSceneSectorHitGroup[sectorHitGroups.Count];

        for (var groupIndex = 0; groupIndex < sectorHitGroups.Count; groupIndex++)
        {
            var sectorHitGroup = sectorHitGroups[groupIndex];
            ArgumentNullException.ThrowIfNull(sectorHitGroup);

            var filteredHits = new EditorMapSceneHit[sectorHitGroup.Hits.Count];
            for (var hitIndex = 0; hitIndex < sectorHitGroup.Hits.Count; hitIndex++)
            {
                var hit = sectorHitGroup.Hits[hitIndex];
                ArgumentNullException.ThrowIfNull(hit);

                filteredHits[hitIndex] = new EditorMapSceneHit
                {
                    MapTileX = hit.MapTileX,
                    MapTileY = hit.MapTileY,
                    SectorAssetPath = hit.SectorAssetPath,
                    Tile = hit.Tile,
                    ObjectHits =
                    [
                        .. hit.ObjectHits.Where(objectHit => selectedObjectIdSet.Contains(objectHit.ObjectId)),
                    ],
                };
            }

            filteredGroups[groupIndex] = new EditorMapSceneSectorHitGroup
            {
                SectorAssetPath = sectorHitGroup.SectorAssetPath,
                LocalX = sectorHitGroup.LocalX,
                LocalY = sectorHitGroup.LocalY,
                Hits = filteredHits,
            };
        }

        return filteredGroups;
    }

    /// <summary>
    /// Stages one placed-object primary rotation edit on a loaded sector asset.
    /// Returns <see langword="null"/> when the object already uses <paramref name="rotation"/>,
    /// or when the rotation property is absent and <paramref name="rotation"/> is <c>0</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no unique object with <paramref name="objectId"/> exists in the sector.
    /// </exception>
    public EditorSessionChange? SetSectorObjectRotation(string assetPath, GameObjectGuid objectId, float rotation)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                var objectIndex = FindSectorObjectIndex(sector, normalizedPath, objectId);
                var currentObject = sector.Objects[objectIndex];
                var currentRotationProperty = currentObject.GetProperty(ObjectField.ObjFPadIas1);

                if (currentRotationProperty is null && rotation == 0f)
                    return null;

                if (currentRotationProperty is not null && currentRotationProperty.GetFloat() == rotation)
                    return null;

                var updatedObject = new MobDataBuilder(currentObject).WithRotation(rotation).Build();
                return new SectorBuilder(sector).ReplaceObject(objectIndex, updatedObject).Build();
            }
        );
    }

    /// <summary>
    /// Stages one placed-object pitch rotation edit on a loaded sector asset.
    /// Returns <see langword="null"/> when the object already uses <paramref name="rotationPitch"/>,
    /// or when the rotation property is absent and <paramref name="rotationPitch"/> is <c>0</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no unique object with <paramref name="objectId"/> exists in the sector.
    /// </exception>
    public EditorSessionChange? SetSectorObjectRotationPitch(
        string assetPath,
        GameObjectGuid objectId,
        float rotationPitch
    )
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                var objectIndex = FindSectorObjectIndex(sector, normalizedPath, objectId);
                var currentObject = sector.Objects[objectIndex];
                var currentRotationProperty = currentObject.GetProperty(ObjectField.ObjFRotationPitch);

                if (currentRotationProperty is null && rotationPitch == 0f)
                    return null;

                if (currentRotationProperty is not null && currentRotationProperty.GetFloat() == rotationPitch)
                    return null;

                var updatedObject = new MobDataBuilder(currentObject).WithRotationPitch(rotationPitch).Build();
                return new SectorBuilder(sector).ReplaceObject(objectIndex, updatedObject).Build();
            }
        );
    }

    /// <summary>
    /// Stages one placed-object removal on a loaded sector asset.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no unique object with <paramref name="objectId"/> exists in the sector.
    /// </exception>
    public EditorSessionChange RemoveSectorObject(string assetPath, GameObjectGuid objectId)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        return StageSectorChange(
            normalizedPath,
            sector =>
            {
                var objectIndex = FindSectorObjectIndex(sector, normalizedPath, objectId);
                return new SectorBuilder(sector).RemoveObject(objectIndex).Build();
            }
        )!;
    }

    /// <summary>
    /// Restores the previous staged direct proto, mob, and sector snapshot.
    /// Dialog, script, and save-editor local histories are unaffected.
    /// </summary>
    public EditorWorkspaceSession UndoDirectAssetChanges()
    {
        if (!CanUndoDirectAssetChanges)
            throw new InvalidOperationException("This session has no staged direct asset edit to undo.");

        _redoDirectAssetSnapshots.Push(CaptureDirectAssetSnapshot());
        RestoreDirectAssetSnapshot(_undoDirectAssetSnapshots.Pop());
        RecordStagedHistoryMutation(DirectAssetHistoryScopeKey, EditorSessionStagedHistoryMutationKind.Undo);
        return this;
    }

    /// <summary>
    /// Reapplies the most recently undone staged direct proto, mob, and sector snapshot.
    /// Dialog, script, and save-editor local histories are unaffected.
    /// </summary>
    public EditorWorkspaceSession RedoDirectAssetChanges()
    {
        if (!CanRedoDirectAssetChanges)
            throw new InvalidOperationException("This session has no staged direct asset edit to redo.");

        _undoDirectAssetSnapshots.Push(CaptureDirectAssetSnapshot());
        RestoreDirectAssetSnapshot(_redoDirectAssetSnapshots.Pop());
        RecordStagedHistoryMutation(DirectAssetHistoryScopeKey, EditorSessionStagedHistoryMutationKind.Redo);
        return this;
    }

    public EditorWorkspace Undo()
    {
        EnsureHistoryOperationCanProceed("undo");
        if (!CanUndo)
            throw new InvalidOperationException("This session has no applied change group to undo.");

        var currentSnapshot = CaptureHistorySnapshot();
        var previousFrame = _undoSnapshots.Pop();
        _redoSnapshots.Push(new EditorWorkspaceSessionHistoryFrame(currentSnapshot, previousFrame.Entry));
        RestoreHistorySnapshot(previousFrame.Snapshot);
        return Workspace;
    }

    public EditorWorkspace Redo()
    {
        EnsureHistoryOperationCanProceed("redo");
        if (!CanRedo)
            throw new InvalidOperationException("This session has no undone change group to redo.");

        var currentSnapshot = CaptureHistorySnapshot();
        var nextFrame = _redoSnapshots.Pop();
        _undoSnapshots.Push(new EditorWorkspaceSessionHistoryFrame(currentSnapshot, nextFrame.Entry));
        RestoreHistorySnapshot(nextFrame.Snapshot);
        return Workspace;
    }

    private EditorWorkspace ApplyPendingChangesCore(
        string? changeGroupLabel,
        bool persistedToDisk,
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys
    )
    {
        if (!HasPendingChanges)
            return Workspace;

        var pendingState = BuildPendingWorkspaceState(selectedScopeKeys);
        if (pendingState.Changes.Count == 0)
            return Workspace;

        var previousSnapshot = CaptureHistorySnapshot();
        ThrowIfPendingStateIntroducesBlockingErrors(pendingState.Workspace.Validation);

        CommitDialogChanges(selectedScopeKeys);
        CommitScriptChanges(selectedScopeKeys);
        CommitDirectAssetChanges(selectedScopeKeys);
        CommitSaveChanges(selectedScopeKeys);
        Workspace = pendingState.Workspace;
        RecordAppliedSnapshot(
            previousSnapshot,
            CreateHistoryEntry(changeGroupLabel, pendingState.Changes, persistedToDisk)
        );
        return Workspace;
    }

    private PendingWorkspaceState BuildPendingWorkspaceState(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    )
    {
        var pendingChanges = CollectPendingChangesSnapshot(selectedScopeKeys);
        var pendingDialogs = CollectDialogChanges(selectedScopeKeys);
        var pendingScripts = CollectScriptChanges(selectedScopeKeys);
        var pendingProtos = CollectProtoChanges(selectedScopeKeys);
        var pendingMobs = CollectMobChanges(selectedScopeKeys);
        var pendingSectors = CollectSectorChanges(selectedScopeKeys);
        var pendingSave = CreatePendingSaveSnapshot(selectedScopeKeys);

        var updatedGameData =
            pendingDialogs.Count == 0
            && pendingScripts.Count == 0
            && pendingProtos.Count == 0
            && pendingMobs.Count == 0
            && pendingSectors.Count == 0
                ? Workspace.GameData
                : GameDataStoreSnapshotBuilder.CloneWithAssetReplacements(
                    Workspace.GameData,
                    updatedScripts: pendingScripts,
                    updatedDialogs: pendingDialogs,
                    updatedSectors: pendingSectors,
                    updatedProtos: pendingProtos,
                    updatedMobs: pendingMobs
                );

        return new PendingWorkspaceState(
            pendingChanges,
            EditorWorkspaceSnapshotBuilder.Build(Workspace, updatedGameData, pendingSave)
        );
    }

    private void ThrowIfPendingStateIntroducesBlockingErrors(EditorWorkspaceValidationReport pendingValidation)
    {
        ArgumentNullException.ThrowIfNull(pendingValidation);

        var blockingIssues = CollectPendingStateBlockingIssues(pendingValidation);
        if (blockingIssues.Length == 0)
            return;

        throw new EditorSessionValidationException(new EditorWorkspaceValidationReport { Issues = blockingIssues });
    }

    private EditorWorkspaceValidationIssue[] CollectPendingStateBlockingIssues(
        EditorWorkspaceValidationReport pendingValidation
    )
    {
        ArgumentNullException.ThrowIfNull(pendingValidation);

        return
        [
            .. pendingValidation
                .Issues.Where(static issue => issue.Severity == EditorWorkspaceValidationSeverity.Error)
                .Except(
                    Workspace.Validation.Issues.Where(static issue =>
                        issue.Severity == EditorWorkspaceValidationSeverity.Error
                    )
                ),
        ];
    }

    private static EditorSessionPendingChangeSummary CreatePendingChangeSummary(
        IReadOnlyList<EditorSessionChange> changes,
        EditorWorkspace workspace,
        EditorWorkspaceValidationReport validation,
        IReadOnlyList<EditorWorkspaceValidationIssue> blockingIssues
    )
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(blockingIssues);

        var targetSummaries = changes
            .Select(change => new EditorSessionPendingChangeTargetSummary
            {
                Kind = change.Kind,
                Target = change.Target,
                DependencySummary =
                    change.Kind == EditorSessionChangeKind.Save
                        ? null
                        : workspace.Index.FindAssetDependencySummary(change.Target),
            })
            .ToArray();

        var groups = changes
            .GroupBy(static change => change.Kind)
            .OrderBy(static group => (int)group.Key)
            .Select(static group => new EditorSessionChangeKindSummary
            {
                Kind = group.Key,
                Targets = [.. group.Select(static change => change.Target)],
            })
            .ToArray();

        return new EditorSessionPendingChangeSummary
        {
            TargetSummaries = targetSummaries,
            Changes = [.. changes],
            Groups = groups,
            Validation = validation,
            BlockingValidation =
                blockingIssues.Count == 0
                    ? EditorWorkspaceValidationReport.Empty
                    : new EditorWorkspaceValidationReport { Issues = [.. blockingIssues] },
        };
    }

    private List<EditorSessionChange> CollectPendingChangesSnapshot(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    )
    {
        var changes = new List<EditorSessionChange>();

        foreach (
            var (assetPath, editor) in _dialogEditors.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            if (
                !editor.HasPendingChanges
                || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Dialog, assetPath)
            )
                continue;

            changes.Add(new EditorSessionChange { Kind = EditorSessionChangeKind.Dialog, Target = assetPath });
        }

        foreach (
            var (assetPath, editor) in _scriptEditors.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            if (
                !editor.HasPendingChanges
                || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Script, assetPath)
            )
                continue;

            changes.Add(new EditorSessionChange { Kind = EditorSessionChangeKind.Script, Target = assetPath });
        }

        if (IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.DirectAssets, null))
        {
            foreach (
                var assetPath in _pendingProtoAssets.Keys.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            )
                changes.Add(new EditorSessionChange { Kind = EditorSessionChangeKind.Proto, Target = assetPath });

            foreach (
                var assetPath in _pendingMobAssets.Keys.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            )
                changes.Add(new EditorSessionChange { Kind = EditorSessionChangeKind.Mob, Target = assetPath });

            foreach (
                var assetPath in _pendingSectorAssets.Keys.OrderBy(
                    static path => path,
                    StringComparer.OrdinalIgnoreCase
                )
            )
                changes.Add(new EditorSessionChange { Kind = EditorSessionChangeKind.Sector, Target = assetPath });
        }

        if (
            _saveEditor?.HasPendingChanges == true
            && IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Save, GetSaveHistoryScopeTarget())
        )
        {
            changes.Add(
                new EditorSessionChange
                {
                    Kind = EditorSessionChangeKind.Save,
                    Target = string.IsNullOrWhiteSpace(Workspace.SaveSlotName) ? "save" : Workspace.SaveSlotName!,
                }
            );
        }

        return changes;
    }

    private static void AddDialogRepairCandidates(
        List<EditorSessionValidationRepairCandidate> candidates,
        string assetPath,
        DialogValidationIssue issue
    )
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(issue);

        if (!issue.EntryNumber.HasValue)
            return;

        switch (issue.Code)
        {
            case DialogValidationCode.NegativeIntelligenceRequirement:
                candidates.Add(
                    new EditorSessionValidationRepairCandidate
                    {
                        Kind = EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement,
                        AssetPath = assetPath,
                        DialogEntryNumber = issue.EntryNumber.Value,
                        Title = "Set IQ to 0",
                        Description =
                            $"Set dialog entry {issue.EntryNumber.Value} to IQ 0 so it becomes a valid NPC reply.",
                        SuggestedIntelligenceRequirement = 0,
                    }
                );
                candidates.Add(
                    new EditorSessionValidationRepairCandidate
                    {
                        Kind = EditorSessionValidationRepairCandidateKind.SetDialogEntryIntelligenceRequirement,
                        AssetPath = assetPath,
                        DialogEntryNumber = issue.EntryNumber.Value,
                        Title = "Set IQ to 1",
                        Description =
                            $"Set dialog entry {issue.EntryNumber.Value} to IQ 1 so it remains a valid PC option.",
                        SuggestedIntelligenceRequirement = 1,
                    }
                );
                break;
            case DialogValidationCode.MissingResponseTarget:
                candidates.Add(
                    new EditorSessionValidationRepairCandidate
                    {
                        Kind = EditorSessionValidationRepairCandidateKind.SetDialogResponseTarget,
                        AssetPath = assetPath,
                        DialogEntryNumber = issue.EntryNumber.Value,
                        Title = "End the conversation",
                        Description =
                            $"Set dialog entry {issue.EntryNumber.Value}'s response target to 0 because target {issue.ResponseTargetNumber} does not exist.",
                        SuggestedResponseTargetNumber = 0,
                    }
                );
                break;
        }
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        return assetPath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static IEnumerable<string> EnumerateProjectRestoreAssetPaths(
        string? activeAssetPath,
        IReadOnlyList<EditorProjectOpenAsset> openAssets
    )
    {
        foreach (var openAsset in openAssets)
            yield return openAsset.AssetPath;

        if (activeAssetPath is not null)
            yield return activeAssetPath;
    }

    private bool TryRestoreTrackedProjectAsset(string assetPath)
    {
        if (Workspace.FindDialog(assetPath) is not null)
        {
            _ = GetDialogEditor(assetPath);
            return true;
        }

        if (Workspace.FindScript(assetPath) is not null)
        {
            _ = GetScriptEditor(assetPath);
            return true;
        }

        return false;
    }

    private bool TrackProjectOpenAsset(EditorProjectOpenAsset normalizedOpenAsset)
    {
        ArgumentNullException.ThrowIfNull(normalizedOpenAsset);

        UpsertProjectOpenAsset(normalizedOpenAsset);
        return TryRestoreTrackedProjectAsset(normalizedOpenAsset.AssetPath);
    }

    private void EnsureBareProjectOpenAssetTracked(string normalizedPath)
    {
        if (
            _projectOpenAssets.Any(openAsset =>
                string.Equals(openAsset.AssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return;
        }

        _projectOpenAssets = [.. _projectOpenAssets, new EditorProjectOpenAsset { AssetPath = normalizedPath }];
    }

    private void UpsertProjectOpenAsset(EditorProjectOpenAsset normalizedOpenAsset)
    {
        var updatedOpenAssets = _projectOpenAssets.ToList();
        var existingIndex = updatedOpenAssets.FindIndex(existing =>
            string.Equals(existing.AssetPath, normalizedOpenAsset.AssetPath, StringComparison.OrdinalIgnoreCase)
        );

        if (existingIndex >= 0)
            updatedOpenAssets[existingIndex] = normalizedOpenAsset;
        else
            updatedOpenAssets.Add(normalizedOpenAsset);

        _projectOpenAssets = [.. updatedOpenAssets];
    }

    private bool CloseAssetCore(string normalizedPath, bool discardPendingChanges)
    {
        var removedOpenAsset = false;
        var removedEditor = false;
        var clearedActiveAsset = false;

        if (_dialogEditors.TryGetValue(normalizedPath, out var dialogEditor))
        {
            if (discardPendingChanges && dialogEditor.HasPendingChanges)
                dialogEditor.DiscardPendingChanges();

            RecordStagedHistoryMutation(
                new EditorSessionStagedHistoryScopeKey(EditorSessionStagedHistoryScopeKind.Dialog, normalizedPath),
                EditorSessionStagedHistoryMutationKind.Clear
            );
            removedEditor = _dialogEditors.Remove(normalizedPath) || removedEditor;
        }

        if (_scriptEditors.TryGetValue(normalizedPath, out var scriptEditor))
        {
            if (discardPendingChanges && scriptEditor.HasPendingChanges)
                scriptEditor.DiscardPendingChanges();

            RecordStagedHistoryMutation(
                new EditorSessionStagedHistoryScopeKey(EditorSessionStagedHistoryScopeKind.Script, normalizedPath),
                EditorSessionStagedHistoryMutationKind.Clear
            );
            removedEditor = _scriptEditors.Remove(normalizedPath) || removedEditor;
        }

        var updatedOpenAssets = _projectOpenAssets
            .Where(openAsset => !string.Equals(openAsset.AssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (updatedOpenAssets.Length != _projectOpenAssets.Count)
        {
            _projectOpenAssets = updatedOpenAssets;
            removedOpenAsset = true;
        }

        if (string.Equals(_projectActiveAssetPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            _projectActiveAssetPath = null;
            clearedActiveAsset = true;
        }

        return removedOpenAsset || removedEditor || clearedActiveAsset;
    }

    private string[] CollectTrackedProjectAssetPaths()
    {
        var assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var openAsset in _projectOpenAssets)
            _ = assetPaths.Add(openAsset.AssetPath);

        foreach (var assetPath in _dialogEditors.Keys)
            _ = assetPaths.Add(assetPath);

        foreach (var assetPath in _scriptEditors.Keys)
            _ = assetPaths.Add(assetPath);

        if (_projectActiveAssetPath is not null)
            _ = assetPaths.Add(_projectActiveAssetPath);

        return [.. assetPaths.OrderBy(static assetPath => assetPath, StringComparer.OrdinalIgnoreCase)];
    }

    private void EnsureTrackedAssetsCanClose(IReadOnlyList<string> assetPaths, bool discardPendingChanges)
    {
        ArgumentNullException.ThrowIfNull(assetPaths);

        if (discardPendingChanges)
            return;

        foreach (var assetPath in assetPaths)
        {
            if (_dialogEditors.TryGetValue(assetPath, out var dialogEditor) && dialogEditor.HasPendingChanges)
            {
                throw new InvalidOperationException(
                    $"Cannot close tracked asset '{assetPath}' because its dialog editor still has staged changes. Discard or apply them first."
                );
            }

            if (_scriptEditors.TryGetValue(assetPath, out var scriptEditor) && scriptEditor.HasPendingChanges)
            {
                throw new InvalidOperationException(
                    $"Cannot close tracked asset '{assetPath}' because its script editor still has staged changes. Discard or apply them first."
                );
            }
        }
    }

    private IEnumerable<string> EnumerateTrackedOpenAssetPaths()
    {
        foreach (var assetPath in _dialogEditors.Keys.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
            yield return assetPath;

        foreach (var assetPath in _scriptEditors.Keys.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
            yield return assetPath;
    }

    private IReadOnlyList<EditorProjectOpenAsset> MergeProjectOpenAssets(
        string? activeAssetPath,
        IReadOnlyList<EditorProjectOpenAsset>? openAssets
    )
    {
        var merged = new List<EditorProjectOpenAsset>();
        var seenAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (openAssets is not null)
        {
            foreach (var openAsset in openAssets)
            {
                var normalizedOpenAsset = NormalizeProjectOpenAsset(openAsset);
                if (!seenAssetPaths.Add(normalizedOpenAsset.AssetPath))
                    continue;

                merged.Add(normalizedOpenAsset);
            }
        }

        foreach (var assetPath in EnumerateTrackedOpenAssetPaths())
        {
            if (!seenAssetPaths.Add(assetPath))
                continue;

            merged.Add(new EditorProjectOpenAsset { AssetPath = assetPath });
        }

        if (activeAssetPath is not null && seenAssetPaths.Add(activeAssetPath))
            merged.Add(new EditorProjectOpenAsset { AssetPath = activeAssetPath });

        return [.. merged];
    }

    private static IReadOnlyList<EditorProjectOpenAsset> NormalizeProjectOpenAssets(
        IReadOnlyList<EditorProjectOpenAsset>? openAssets
    )
    {
        if (openAssets is null || openAssets.Count == 0)
            return [];

        var normalizedOpenAssets = new EditorProjectOpenAsset[openAssets.Count];
        for (var i = 0; i < openAssets.Count; i++)
            normalizedOpenAssets[i] = NormalizeProjectOpenAsset(openAssets[i]);

        return normalizedOpenAssets;
    }

    private static EditorProjectOpenAsset NormalizeProjectOpenAsset(EditorProjectOpenAsset openAsset)
    {
        ArgumentNullException.ThrowIfNull(openAsset);

        return new EditorProjectOpenAsset
        {
            AssetPath = NormalizeAssetPath(openAsset.AssetPath),
            ViewId = openAsset.ViewId,
            IsPinned = openAsset.IsPinned,
            Properties = openAsset.Properties,
        };
    }

    private static IReadOnlyList<EditorProjectBookmark> NormalizeProjectBookmarks(
        IReadOnlyList<EditorProjectBookmark>? bookmarks
    )
    {
        if (bookmarks is null || bookmarks.Count == 0)
            return [];

        var normalizedBookmarks = new EditorProjectBookmark[bookmarks.Count];
        for (var i = 0; i < bookmarks.Count; i++)
        {
            var bookmark = bookmarks[i];
            ArgumentNullException.ThrowIfNull(bookmark);

            normalizedBookmarks[i] = new EditorProjectBookmark
            {
                Id = bookmark.Id,
                AssetPath = NormalizeAssetPath(bookmark.AssetPath),
                Title = bookmark.Title,
                ViewId = bookmark.ViewId,
                LocationKey = bookmark.LocationKey,
                Properties = bookmark.Properties,
            };
        }

        return normalizedBookmarks;
    }

    private static IReadOnlyList<EditorProjectViewState> NormalizeProjectViewStates(
        IReadOnlyList<EditorProjectViewState>? viewStates
    )
    {
        if (viewStates is null || viewStates.Count == 0)
            return [];

        var normalizedViewStates = new EditorProjectViewState[viewStates.Count];
        for (var i = 0; i < viewStates.Count; i++)
        {
            var viewState = viewStates[i];
            ArgumentNullException.ThrowIfNull(viewState);

            normalizedViewStates[i] = new EditorProjectViewState
            {
                Id = viewState.Id,
                AssetPath = NormalizeOptionalAssetPath(viewState.AssetPath),
                ViewId = viewState.ViewId,
                Properties = viewState.Properties,
            };
        }

        return normalizedViewStates;
    }

    private static IReadOnlyList<EditorProjectMapViewState> NormalizeProjectMapViewStates(
        IReadOnlyList<EditorProjectMapViewState>? mapViewStates
    )
    {
        if (mapViewStates is null || mapViewStates.Count == 0)
            return [];

        var normalizedMapViewStates = new EditorProjectMapViewState[mapViewStates.Count];
        for (var i = 0; i < mapViewStates.Count; i++)
            normalizedMapViewStates[i] = NormalizeProjectMapViewState(mapViewStates[i]);

        return normalizedMapViewStates;
    }

    private static EditorProjectMapViewState NormalizeProjectMapViewState(EditorProjectMapViewState mapViewState)
    {
        ArgumentNullException.ThrowIfNull(mapViewState);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapViewState.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapViewState.MapName);

        var camera = mapViewState.Camera ?? new EditorProjectMapCameraState();
        var selection = mapViewState.Selection ?? new EditorProjectMapSelectionState();
        var preview = mapViewState.Preview ?? new EditorProjectMapPreviewState();
        var worldEdit = NormalizeProjectMapWorldEditState(mapViewState.WorldEdit);

        return new EditorProjectMapViewState
        {
            Id = mapViewState.Id,
            MapName = mapViewState.MapName,
            ViewId = mapViewState.ViewId,
            Camera = new EditorProjectMapCameraState
            {
                CenterTileX = camera.CenterTileX,
                CenterTileY = camera.CenterTileY,
                Zoom = camera.Zoom,
            },
            Selection = new EditorProjectMapSelectionState
            {
                SectorAssetPath = NormalizeOptionalAssetPath(selection.SectorAssetPath),
                Tile = selection.Tile,
                ObjectId = selection.ObjectId,
                Area = selection.Area is null
                    ? null
                    : new EditorProjectMapAreaSelectionState
                    {
                        MinMapTileX = selection.Area.MinMapTileX,
                        MinMapTileY = selection.Area.MinMapTileY,
                        MaxMapTileX = selection.Area.MaxMapTileX,
                        MaxMapTileY = selection.Area.MaxMapTileY,
                        ObjectIds = [.. selection.Area.ObjectIds],
                    },
            },
            Preview = new EditorProjectMapPreviewState
            {
                UseScenePreview = preview.UseScenePreview,
                OutlineMode = preview.OutlineMode,
                ShowObjects = preview.ShowObjects,
                ShowRoofs = preview.ShowRoofs,
                ShowLights = preview.ShowLights,
                ShowBlockedTiles = preview.ShowBlockedTiles,
                ShowScripts = preview.ShowScripts,
            },
            WorldEdit = worldEdit,
        };
    }

    private static EditorProjectMapWorldEditState NormalizeProjectMapWorldEditState(
        EditorProjectMapWorldEditState? worldEditState
    )
    {
        var terrain = NormalizeProjectMapTerrainToolState(worldEditState?.Terrain);
        var objectPlacement = NormalizeProjectMapObjectPlacementToolState(worldEditState?.ObjectPlacement);
        return new EditorProjectMapWorldEditState
        {
            ActiveTool = worldEditState?.ActiveTool ?? EditorProjectMapWorldEditActiveTool.None,
            Terrain = terrain,
            ObjectPlacement = objectPlacement,
        };
    }

    private static EditorProjectMapTerrainToolState NormalizeProjectMapTerrainToolState(
        EditorProjectMapTerrainToolState? terrainToolState
    ) =>
        new()
        {
            MapPropertiesAssetPath = NormalizeOptionalAssetPath(terrainToolState?.MapPropertiesAssetPath),
            PaletteX = terrainToolState?.PaletteX ?? 0UL,
            PaletteY = terrainToolState?.PaletteY ?? 0UL,
        };

    private static EditorProjectMapObjectPlacementToolState NormalizeProjectMapObjectPlacementToolState(
        EditorProjectMapObjectPlacementToolState? objectPlacementToolState
    ) =>
        new()
        {
            Mode = objectPlacementToolState?.Mode ?? EditorProjectMapObjectPlacementMode.SinglePlacement,
            PlacementRequest = NormalizePlacementRequest(objectPlacementToolState?.PlacementRequest),
            PlacementSet = NormalizePlacementSet(objectPlacementToolState?.PlacementSet),
            PresetLibrary = NormalizePlacementPresetLibrary(objectPlacementToolState?.PresetLibrary),
            SelectedPresetId = string.IsNullOrWhiteSpace(objectPlacementToolState?.SelectedPresetId)
                ? null
                : objectPlacementToolState!.SelectedPresetId.Trim(),
        };

    private static EditorObjectPalettePlacementRequest? NormalizePlacementRequest(
        EditorObjectPalettePlacementRequest? placementRequest
    ) =>
        placementRequest is null
            ? null
            : new EditorObjectPalettePlacementRequest
            {
                ProtoNumber = placementRequest.ProtoNumber,
                DeltaTileX = placementRequest.DeltaTileX,
                DeltaTileY = placementRequest.DeltaTileY,
                Rotation = placementRequest.Rotation,
                RotationPitch = placementRequest.RotationPitch,
                AlignToTileGrid = placementRequest.AlignToTileGrid,
            };

    private static EditorObjectPalettePlacementSet? NormalizePlacementSet(
        EditorObjectPalettePlacementSet? placementSet
    ) =>
        placementSet is null
            ? null
            : new EditorObjectPalettePlacementSet
            {
                Name = placementSet.Name,
                Entries = NormalizePlacementRequests(placementSet.Entries),
            };

    private static IReadOnlyList<EditorObjectPalettePlacementPreset> NormalizePlacementPresetLibrary(
        IReadOnlyList<EditorObjectPalettePlacementPreset>? presetLibrary
    )
    {
        if (presetLibrary is null || presetLibrary.Count == 0)
            return [];

        var normalizedPresets = new EditorObjectPalettePlacementPreset[presetLibrary.Count];
        for (var i = 0; i < presetLibrary.Count; i++)
            normalizedPresets[i] = NormalizePlacementPreset(presetLibrary[i]);

        return normalizedPresets;
    }

    private static EditorObjectPalettePlacementPreset NormalizePlacementPreset(
        EditorObjectPalettePlacementPreset preset
    )
    {
        ArgumentNullException.ThrowIfNull(preset);

        return new EditorObjectPalettePlacementPreset
        {
            PresetId = preset.PresetId,
            Name = preset.Name,
            Description = preset.Description,
            Entries = NormalizePlacementRequests(preset.Entries),
        };
    }

    private static IReadOnlyList<EditorObjectPalettePlacementRequest> NormalizePlacementRequests(
        IReadOnlyList<EditorObjectPalettePlacementRequest>? placementRequests
    )
    {
        if (placementRequests is null || placementRequests.Count == 0)
            return [];

        var normalizedRequests = new EditorObjectPalettePlacementRequest[placementRequests.Count];
        for (var i = 0; i < placementRequests.Count; i++)
        {
            var placementRequest = placementRequests[i];
            ArgumentNullException.ThrowIfNull(placementRequest);
            normalizedRequests[i] = NormalizePlacementRequest(placementRequest)!;
        }

        return normalizedRequests;
    }

    private static IReadOnlyList<EditorProjectToolState> NormalizeProjectToolStates(
        IReadOnlyList<EditorProjectToolState>? toolStates
    )
    {
        if (toolStates is null || toolStates.Count == 0)
            return [];

        var normalizedToolStates = new EditorProjectToolState[toolStates.Count];
        for (var i = 0; i < toolStates.Count; i++)
        {
            var toolState = toolStates[i];
            ArgumentNullException.ThrowIfNull(toolState);

            normalizedToolStates[i] = new EditorProjectToolState
            {
                ToolId = toolState.ToolId,
                ScopeId = toolState.ScopeId,
                Properties = toolState.Properties,
            };
        }

        return normalizedToolStates;
    }

    private static string? NormalizeOptionalAssetPath(string? assetPath) =>
        string.IsNullOrWhiteSpace(assetPath) ? null : NormalizeAssetPath(assetPath);

    private static IReadOnlyList<EditorSessionChange> GetStagedTransactionPendingChanges(
        EditorSessionStagedHistoryScope scope,
        IReadOnlyList<EditorSessionChange> pendingChanges
    )
    {
        return scope.Kind switch
        {
            EditorSessionStagedHistoryScopeKind.Dialog => pendingChanges
                .Where(change =>
                    change.Kind == EditorSessionChangeKind.Dialog
                    && string.Equals(change.Target, scope.Target, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray(),
            EditorSessionStagedHistoryScopeKind.Script => pendingChanges
                .Where(change =>
                    change.Kind == EditorSessionChangeKind.Script
                    && string.Equals(change.Target, scope.Target, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray(),
            EditorSessionStagedHistoryScopeKind.Save => pendingChanges
                .Where(change =>
                    change.Kind == EditorSessionChangeKind.Save
                    && string.Equals(change.Target, scope.Target, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray(),
            EditorSessionStagedHistoryScopeKind.DirectAssets => pendingChanges
                .Where(change =>
                    change.Kind
                        is EditorSessionChangeKind.Proto
                            or EditorSessionChangeKind.Mob
                            or EditorSessionChangeKind.Sector
                )
                .ToArray(),
            _ => throw new InvalidOperationException($"Unsupported staged transaction scope {scope.Kind}."),
        };
    }

    private static string GetStagedTransactionLabel(EditorSessionStagedHistoryScope scope) =>
        scope.Kind switch
        {
            EditorSessionStagedHistoryScopeKind.Dialog => scope.Target ?? "dialog",
            EditorSessionStagedHistoryScopeKind.Script => scope.Target ?? "script",
            EditorSessionStagedHistoryScopeKind.Save => scope.Target ?? "save",
            EditorSessionStagedHistoryScopeKind.DirectAssets => "direct-assets",
            _ => throw new InvalidOperationException($"Unsupported staged transaction scope {scope.Kind}."),
        };

    private static IReadOnlyList<string> GetStagedTransactionAffectedTargets(
        EditorSessionStagedHistoryScope scope,
        IReadOnlyList<EditorSessionChange> pendingChanges
    )
    {
        if (
            scope.Kind
                is EditorSessionStagedHistoryScopeKind.Dialog
                    or EditorSessionStagedHistoryScopeKind.Script
                    or EditorSessionStagedHistoryScopeKind.Save
            && scope.Target is not null
        )
        {
            return [scope.Target];
        }

        return [.. pendingChanges.Select(static change => change.Target).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private bool HasPendingDirectAssetChanges =>
        _pendingProtoAssets.Count > 0 || _pendingMobAssets.Count > 0 || _pendingSectorAssets.Count > 0;

    private string GetSaveHistoryScopeTarget() =>
        string.IsNullOrWhiteSpace(Workspace.SaveSlotName) ? "save" : Workspace.SaveSlotName!;

    private static EditorSessionStagedHistoryScopeKey DirectAssetHistoryScopeKey =>
        new(EditorSessionStagedHistoryScopeKind.DirectAssets, null);

    private Action<EditorSessionStagedHistoryMutationKind> CreateStagedHistoryObserver(
        EditorSessionStagedHistoryScopeKind kind,
        string? target
    )
    {
        var scopeKey = new EditorSessionStagedHistoryScopeKey(kind, target);
        return mutationKind => RecordStagedHistoryMutation(scopeKey, mutationKind);
    }

    private EditorSessionStagedHistoryScope? GetPreferredStagedHistoryScope(bool canUndo)
    {
        var scopes = GetStagedHistoryScopes();
        if (scopes.Count == 0)
            return null;

        if (TryGetActiveAssetHistoryScope(scopes, canUndo, out var activeScope))
            return activeScope;

        if (TryPeekMergedStagedHistoryScope(canUndo, scopes, out var mergedScope))
            return mergedScope;

        if (TryGetOpenAssetHistoryScope(scopes, canUndo, out var openAssetScope))
            return openAssetScope;

        return scopes.FirstOrDefault(scope => canUndo ? scope.CanUndo : scope.CanRedo);
    }

    private EditorSessionStagedTransactionSummary? GetPreferredStagedTransactionSummary(bool canUndo)
    {
        var summaries = GetStagedTransactionSummaries();
        if (summaries.Count == 0)
            return null;

        var preferredScope = GetPreferredStagedHistoryScope(canUndo);
        if (preferredScope is null)
            return null;

        return summaries.FirstOrDefault(summary =>
            summary.Kind == preferredScope.Kind
            && string.Equals(summary.Target, preferredScope.Target, StringComparison.OrdinalIgnoreCase)
        );
    }

    private EditorSessionStagedCommandSummary? CreateDefaultStagedCommandSummary(EditorSessionStagedCommandKind kind)
    {
        var transaction = kind switch
        {
            EditorSessionStagedCommandKind.Undo => GetPreferredUndoStagedTransactionSummary(),
            EditorSessionStagedCommandKind.Redo => GetPreferredRedoStagedTransactionSummary(),
            _ => throw new InvalidOperationException($"Unsupported staged command kind {kind}."),
        };

        if (transaction is null)
            return null;

        var verb = kind == EditorSessionStagedCommandKind.Undo ? "Undo" : "Redo";
        return new EditorSessionStagedCommandSummary
        {
            Kind = kind,
            Label = $"{verb} {transaction.Label}",
            Transaction = transaction,
            CanExecute = kind == EditorSessionStagedCommandKind.Undo ? transaction.CanUndo : transaction.CanRedo,
            IsDefault = true,
        };
    }

    private EditorSessionHistoryCommandSummary? CreateDefaultHistoryCommandSummary(EditorSessionHistoryCommandKind kind)
    {
        var entry = kind switch
        {
            EditorSessionHistoryCommandKind.Undo => _undoSnapshots.TryPeek(out var undoFrame) ? undoFrame.Entry : null,
            EditorSessionHistoryCommandKind.Redo => _redoSnapshots.TryPeek(out var redoFrame) ? redoFrame.Entry : null,
            _ => throw new InvalidOperationException($"Unsupported history command kind {kind}."),
        };

        if (entry is null)
            return null;

        var verb = kind == EditorSessionHistoryCommandKind.Undo ? "Undo" : "Redo";
        return new EditorSessionHistoryCommandSummary
        {
            Kind = kind,
            Label = $"{verb} {entry.Label}",
            Entry = entry,
            CanExecute = true,
        };
    }

    private IReadOnlyList<EditorSessionStagedCommandSummary> CreateAvailableStagedCommandSummaries(
        EditorSessionStagedCommandKind kind
    )
    {
        var preferred =
            kind == EditorSessionStagedCommandKind.Undo
                ? GetPreferredUndoStagedTransactionSummary()
                : GetPreferredRedoStagedTransactionSummary();
        var summaries = GetStagedTransactionSummaries();
        var commands = new List<EditorSessionStagedCommandSummary>(summaries.Count);
        var verb = kind == EditorSessionStagedCommandKind.Undo ? "Undo" : "Redo";

        foreach (var transaction in summaries)
        {
            var canExecute = kind == EditorSessionStagedCommandKind.Undo ? transaction.CanUndo : transaction.CanRedo;
            if (!canExecute)
                continue;

            var isDefault =
                preferred is not null
                && transaction.Kind == preferred.Kind
                && string.Equals(transaction.Target, preferred.Target, StringComparison.OrdinalIgnoreCase);

            commands.Add(
                new EditorSessionStagedCommandSummary
                {
                    Kind = kind,
                    Label = $"{verb} {transaction.Label}",
                    Transaction = transaction,
                    CanExecute = true,
                    IsDefault = isDefault,
                }
            );
        }

        commands.Sort(
            static (left, right) =>
            {
                var defaultOrder = right.IsDefault.CompareTo(left.IsDefault);
                if (defaultOrder != 0)
                    return defaultOrder;

                var kindOrder = left.Kind.CompareTo(right.Kind);
                if (kindOrder != 0)
                    return kindOrder;

                return StringComparer.OrdinalIgnoreCase.Compare(left.Label, right.Label);
            }
        );

        return commands;
    }

    private bool TryGetActiveAssetHistoryScope(
        IReadOnlyList<EditorSessionStagedHistoryScope> scopes,
        bool canUndo,
        out EditorSessionStagedHistoryScope scope
    )
    {
        if (_projectActiveAssetPath is not null)
        {
            if (TryGetAssetHistoryScope(scopes, _projectActiveAssetPath, canUndo, out scope))
                return true;

            if (IsTrackedDirectAssetPath(_projectActiveAssetPath) && TryGetDirectAssetScope(scopes, canUndo, out scope))
                return true;
        }

        scope = null!;
        return false;
    }

    private bool TryGetOpenAssetHistoryScope(
        IReadOnlyList<EditorSessionStagedHistoryScope> scopes,
        bool canUndo,
        out EditorSessionStagedHistoryScope scope
    )
    {
        for (var i = _projectOpenAssets.Count - 1; i >= 0; i--)
        {
            var assetPath = _projectOpenAssets[i].AssetPath;
            if (TryGetAssetHistoryScope(scopes, assetPath, canUndo, out scope))
                return true;

            if (IsTrackedDirectAssetPath(assetPath) && TryGetDirectAssetScope(scopes, canUndo, out scope))
                return true;
        }

        scope = null!;
        return false;
    }

    private static bool TryGetAssetHistoryScope(
        IReadOnlyList<EditorSessionStagedHistoryScope> scopes,
        string assetPath,
        bool canUndo,
        out EditorSessionStagedHistoryScope scope
    )
    {
        scope = scopes.FirstOrDefault(candidate =>
            (
                candidate.Kind == EditorSessionStagedHistoryScopeKind.Dialog
                || candidate.Kind == EditorSessionStagedHistoryScopeKind.Script
            )
            && string.Equals(candidate.Target, assetPath, StringComparison.OrdinalIgnoreCase)
            && (canUndo ? candidate.CanUndo : candidate.CanRedo)
        )!;

        return scope is not null;
    }

    private static bool TryGetDirectAssetScope(
        IReadOnlyList<EditorSessionStagedHistoryScope> scopes,
        bool canUndo,
        out EditorSessionStagedHistoryScope scope
    )
    {
        scope = scopes.FirstOrDefault(candidate =>
            candidate.Kind == EditorSessionStagedHistoryScopeKind.DirectAssets
            && (canUndo ? candidate.CanUndo : candidate.CanRedo)
        )!;

        return scope is not null;
    }

    private bool IsTrackedDirectAssetPath(string assetPath) =>
        _pendingProtoAssets.ContainsKey(assetPath)
        || _pendingMobAssets.ContainsKey(assetPath)
        || _pendingSectorAssets.ContainsKey(assetPath);

    private DialogEditor GetTrackedDialogEditor(string? target)
    {
        var normalizedPath = NormalizeAssetPath(
            target ?? throw new ArgumentException("Dialog history scopes require a target asset path.", nameof(target))
        );
        return _dialogEditors.TryGetValue(normalizedPath, out var editor)
            ? editor
            : throw new InvalidOperationException($"No tracked dialog editor matched '{normalizedPath}'.");
    }

    private ScriptEditor GetTrackedScriptEditor(string? target)
    {
        var normalizedPath = NormalizeAssetPath(
            target ?? throw new ArgumentException("Script history scopes require a target asset path.", nameof(target))
        );
        return _scriptEditors.TryGetValue(normalizedPath, out var editor)
            ? editor
            : throw new InvalidOperationException($"No tracked script editor matched '{normalizedPath}'.");
    }

    private SaveGameEditor GetTrackedSaveEditor() =>
        _saveEditor
        ?? throw new InvalidOperationException("This session does not currently track a save editor history scope.");

    private EditorWorkspaceSessionDirectAssetSnapshot CaptureDirectAssetSnapshot() =>
        new(
            new Dictionary<string, ProtoData>(_pendingProtoAssets, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, MobData>(_pendingMobAssets, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, Sector>(_pendingSectorAssets, StringComparer.OrdinalIgnoreCase)
        );

    private EditorWorkspaceSessionSnapshot CaptureHistorySnapshot() => new(Workspace, CreateProject());

    private void RecordDirectAssetSnapshot(EditorWorkspaceSessionDirectAssetSnapshot previousSnapshot)
    {
        _undoDirectAssetSnapshots.Push(previousSnapshot);
        _redoDirectAssetSnapshots.Clear();
        RecordStagedHistoryMutation(DirectAssetHistoryScopeKey, EditorSessionStagedHistoryMutationKind.Edit);
    }

    private void RecordStagedHistoryMutation(
        EditorSessionStagedHistoryScopeKey scopeKey,
        EditorSessionStagedHistoryMutationKind mutationKind
    )
    {
        switch (mutationKind)
        {
            case EditorSessionStagedHistoryMutationKind.Edit:
                _undoStagedHistoryScopes.Push(scopeKey);
                _redoStagedHistoryScopes.Clear();
                break;
            case EditorSessionStagedHistoryMutationKind.Undo:
                if (!MoveLatestMatchingStagedHistoryEntry(_undoStagedHistoryScopes, _redoStagedHistoryScopes, scopeKey))
                    _redoStagedHistoryScopes.Push(scopeKey);
                break;
            case EditorSessionStagedHistoryMutationKind.Redo:
                if (!MoveLatestMatchingStagedHistoryEntry(_redoStagedHistoryScopes, _undoStagedHistoryScopes, scopeKey))
                    _undoStagedHistoryScopes.Push(scopeKey);
                break;
            case EditorSessionStagedHistoryMutationKind.Clear:
                RemoveMatchingStagedHistoryEntries(_undoStagedHistoryScopes, scopeKey);
                RemoveMatchingStagedHistoryEntries(_redoStagedHistoryScopes, scopeKey);
                break;
            default:
                throw new InvalidOperationException($"Unsupported staged history mutation {mutationKind}.");
        }
    }

    private static bool MoveLatestMatchingStagedHistoryEntry(
        Stack<EditorSessionStagedHistoryScopeKey> source,
        Stack<EditorSessionStagedHistoryScopeKey> destination,
        EditorSessionStagedHistoryScopeKey scopeKey
    )
    {
        var bufferedEntries = new Stack<EditorSessionStagedHistoryScopeKey>();

        while (source.Count > 0)
        {
            var candidate = source.Pop();
            if (candidate == scopeKey)
            {
                destination.Push(candidate);

                while (bufferedEntries.Count > 0)
                    source.Push(bufferedEntries.Pop());

                return true;
            }

            bufferedEntries.Push(candidate);
        }

        while (bufferedEntries.Count > 0)
            source.Push(bufferedEntries.Pop());

        return false;
    }

    private static void RemoveMatchingStagedHistoryEntries(
        Stack<EditorSessionStagedHistoryScopeKey> stack,
        EditorSessionStagedHistoryScopeKey scopeKey
    )
    {
        var retainedEntries = new Stack<EditorSessionStagedHistoryScopeKey>();

        while (stack.Count > 0)
        {
            var candidate = stack.Pop();
            if (candidate != scopeKey)
                retainedEntries.Push(candidate);
        }

        while (retainedEntries.Count > 0)
            stack.Push(retainedEntries.Pop());
    }

    private bool TryPeekMergedStagedHistoryScope(
        bool canUndo,
        IReadOnlyList<EditorSessionStagedHistoryScope> scopes,
        out EditorSessionStagedHistoryScope scope
    )
    {
        var stack = canUndo ? _undoStagedHistoryScopes : _redoStagedHistoryScopes;

        while (stack.Count > 0)
        {
            var scopeKey = stack.Peek();
            if (TryGetMatchingStagedHistoryScope(scopes, scopeKey, canUndo, out scope))
                return true;

            _ = stack.Pop();
        }

        scope = null!;
        return false;
    }

    private static bool TryGetMatchingStagedHistoryScope(
        IReadOnlyList<EditorSessionStagedHistoryScope> scopes,
        EditorSessionStagedHistoryScopeKey scopeKey,
        bool canUndo,
        out EditorSessionStagedHistoryScope scope
    )
    {
        scope = scopes.FirstOrDefault(candidate =>
            candidate.Kind == scopeKey.Kind
            && string.Equals(candidate.Target, scopeKey.Target, StringComparison.OrdinalIgnoreCase)
            && (canUndo ? candidate.CanUndo : candidate.CanRedo)
        )!;

        return scope is not null;
    }

    private void RecordAppliedSnapshot(EditorWorkspaceSessionSnapshot previousSnapshot, EditorSessionHistoryEntry entry)
    {
        _undoSnapshots.Push(new EditorWorkspaceSessionHistoryFrame(previousSnapshot, entry));
        _redoSnapshots.Clear();
    }

    private void PromoteLatestUndoHistoryToPersisted(string? explicitLabel)
    {
        if (_undoSnapshots.Count == 0)
            return;

        var latestFrame = _undoSnapshots.Pop();
        var updatedLabel = latestFrame.Entry.Label;
        if (string.IsNullOrWhiteSpace(explicitLabel) && updatedLabel.StartsWith("Apply ", StringComparison.Ordinal))
            updatedLabel = "Save " + updatedLabel[6..];

        _undoSnapshots.Push(
            new EditorWorkspaceSessionHistoryFrame(
                latestFrame.Snapshot,
                new EditorSessionHistoryEntry
                {
                    Label = updatedLabel,
                    RecordedAtUtc = latestFrame.Entry.RecordedAtUtc,
                    PersistedToDisk = true,
                    Changes = latestFrame.Entry.Changes,
                    ProjectState = latestFrame.Entry.ProjectState,
                }
            )
        );
    }

    private void RestoreHistorySnapshot(EditorWorkspaceSessionSnapshot snapshot)
    {
        Workspace = snapshot.Workspace;
        _pendingProtoAssets.Clear();
        _pendingMobAssets.Clear();
        _pendingSectorAssets.Clear();
        ClearDirectAssetDraftHistory();
        _ = RestoreProject(snapshot.Project);
        RestoreDialogEditorBaselines();
        RestoreScriptEditorBaselines();
        RestoreSaveEditorBaseline();
    }

    private void RestoreDirectAssetSnapshot(EditorWorkspaceSessionDirectAssetSnapshot snapshot)
    {
        RestorePendingAssetDictionary(_pendingProtoAssets, snapshot.Protos);
        RestorePendingAssetDictionary(_pendingMobAssets, snapshot.Mobs);
        RestorePendingAssetDictionary(_pendingSectorAssets, snapshot.Sectors);
    }

    private static void RestorePendingAssetDictionary<T>(
        Dictionary<string, T> destination,
        IReadOnlyDictionary<string, T> source
    )
    {
        destination.Clear();

        foreach (var (assetPath, asset) in source)
            destination[assetPath] = asset;
    }

    private void ClearDirectAssetDraftHistory()
    {
        _undoDirectAssetSnapshots.Clear();
        _redoDirectAssetSnapshots.Clear();
        RecordStagedHistoryMutation(DirectAssetHistoryScopeKey, EditorSessionStagedHistoryMutationKind.Clear);
    }

    private T TrackDirectAssetEdit<T>(Func<T> applyEdit, Func<T, bool> hasChanged)
    {
        ArgumentNullException.ThrowIfNull(applyEdit);
        ArgumentNullException.ThrowIfNull(hasChanged);

        var previousSnapshot = CaptureDirectAssetSnapshot();
        var result = applyEdit();
        if (!hasChanged(result))
            return result;

        RecordDirectAssetSnapshot(previousSnapshot);
        return result;
    }

    private EditorSessionHistoryEntry CreateHistoryEntry(
        string? explicitLabel,
        IReadOnlyList<EditorSessionChange> changes,
        bool persistedToDisk
    ) =>
        new()
        {
            Label = CreateHistoryLabel(explicitLabel, changes, persistedToDisk),
            RecordedAtUtc = DateTimeOffset.UtcNow,
            PersistedToDisk = persistedToDisk,
            Changes = [.. changes],
            ProjectState = CreateProjectStateSummary(),
        };

    private static string CreateHistoryLabel(
        string? explicitLabel,
        IReadOnlyList<EditorSessionChange> changes,
        bool persistedToDisk
    )
    {
        if (!string.IsNullOrWhiteSpace(explicitLabel))
            return explicitLabel.Trim();

        var verb = persistedToDisk ? "Save" : "Apply";
        return changes.Count switch
        {
            0 => $"{verb} session changes",
            1 => $"{verb} {changes[0].Target}",
            _ => $"{verb} {changes.Count} session changes",
        };
    }

    private EditorSessionProjectStateSummary CreateProjectStateSummary() =>
        new()
        {
            ActiveAssetPath = _projectActiveAssetPath,
            OpenAssets = [.. _projectOpenAssets],
            Bookmarks = [.. _projectBookmarks],
            MapViewStates = [.. _projectMapViewStates],
            ViewStates = [.. _projectViewStates],
            ToolStates = [.. _projectToolStates],
        };

    private EditorSessionBootstrapSummary CreateBootstrapSummary(EditorProjectRestoreResult? restore) =>
        new()
        {
            ProjectState = CreateProjectStateSummary(),
            Restore = restore,
            StagedTransactions = GetStagedTransactionSummaries(),
            StagedCommands = GetAvailableStagedCommandSummaries(),
            HistoryCommands = GetHistoryCommandSummaries(),
        };

    private void RestoreDialogEditorBaselines()
    {
        foreach (var (assetPath, editor) in _dialogEditors)
        {
            var dialog =
                Workspace.FindDialog(assetPath)
                ?? throw new InvalidOperationException(
                    $"Cannot restore dialog editor '{assetPath}' because the restored workspace no longer contains that asset."
                );
            editor.ResetCommittedState(dialog);
        }
    }

    private void RestoreScriptEditorBaselines()
    {
        foreach (var (assetPath, editor) in _scriptEditors)
        {
            var script =
                Workspace.FindScript(assetPath)
                ?? throw new InvalidOperationException(
                    $"Cannot restore script editor '{assetPath}' because the restored workspace no longer contains that asset."
                );
            editor.ResetCommittedState(script);
        }
    }

    private void RestoreSaveEditorBaseline()
    {
        if (_saveEditor is null)
            return;

        var restoredSave =
            Workspace.Save
            ?? throw new InvalidOperationException(
                "Cannot restore the save editor because the restored workspace does not include a loaded save slot."
            );
        _saveEditor.ResetCommittedState(restoredSave);
    }

    private void EnsureHistoryOperationCanProceed(string operationName)
    {
        if (HasPendingChanges)
        {
            throw new InvalidOperationException(
                $"This session still has staged edits. Apply or discard them before attempting to {operationName}."
            );
        }
    }

    private Dictionary<string, DlgFile> CollectDialogChanges(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    )
    {
        var pendingDialogs = new Dictionary<string, DlgFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, editor) in _dialogEditors)
        {
            if (
                !editor.HasPendingChanges
                || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Dialog, assetPath)
            )
                continue;

            pendingDialogs[assetPath] = editor.GetCurrentDialog();
        }

        return pendingDialogs;
    }

    private DlgFile GetCurrentDialogAsset(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (_dialogEditors.TryGetValue(normalizedPath, out var editor))
            return editor.GetCurrentDialog();

        return Workspace.FindDialog(normalizedPath)
            ?? throw new InvalidOperationException($"No loaded dialog asset matched '{normalizedPath}'.");
    }

    private DialogEntry GetCurrentDialogEntry(string assetPath, int entryNumber)
    {
        var dialog = GetCurrentDialogAsset(assetPath);
        return dialog.Entries.FirstOrDefault(entry => entry.Num == entryNumber)
            ?? throw new InvalidOperationException(
                $"Dialog asset '{NormalizeAssetPath(assetPath)}' no longer contains entry {entryNumber}."
            );
    }

    private string[] CollectPendingDialogAssetPaths(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    ) =>
        _dialogEditors
            .Where(pair =>
                pair.Value.HasPendingChanges
                && IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Dialog, pair.Key)
            )
            .Select(static pair => pair.Key)
            .ToArray();

    private Dictionary<string, ScrFile> CollectScriptChanges(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    )
    {
        var pendingScripts = new Dictionary<string, ScrFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, editor) in _scriptEditors)
        {
            if (
                !editor.HasPendingChanges
                || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Script, assetPath)
            )
                continue;

            pendingScripts[assetPath] = editor.GetCurrentScript();
        }

        return pendingScripts;
    }

    private string[] CollectPendingScriptAssetPaths(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    ) =>
        _scriptEditors
            .Where(pair =>
                pair.Value.HasPendingChanges
                && IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Script, pair.Key)
            )
            .Select(static pair => pair.Key)
            .ToArray();

    private Dictionary<string, ProtoData> CollectProtoChanges(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    ) =>
        IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.DirectAssets, null)
            ? new(_pendingProtoAssets, StringComparer.OrdinalIgnoreCase)
            : new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, MobData> CollectMobChanges(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    ) =>
        IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.DirectAssets, null)
            ? new(_pendingMobAssets, StringComparer.OrdinalIgnoreCase)
            : new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, Sector> CollectSectorChanges(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    ) =>
        IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.DirectAssets, null)
            ? new(_pendingSectorAssets, StringComparer.OrdinalIgnoreCase)
            : new(StringComparer.OrdinalIgnoreCase);

    private EditorSessionChange? StageSectorChange(string normalizedPath, Func<Sector, Sector?> update) =>
        TrackDirectAssetEdit(
            () =>
            {
                var currentSector = GetCurrentSectorAsset(normalizedPath);
                var updatedSector = update(currentSector);
                if (updatedSector is null)
                    return null;

                _pendingSectorAssets[normalizedPath] = updatedSector;
                return CreateDirectAssetChange(normalizedPath, FileFormat.Sector);
            },
            static change => change is not null
        );

    private void PersistDialogChanges(IEnumerable<string> assetPaths)
    {
        foreach (var assetPath in assetPaths)
        {
            var outputPath = ResolveWorkspaceContentPath(assetPath);
            EnsureParentDirectory(outputPath);
            DialogFormat.WriteToFile(GetDialogEditor(assetPath).GetCurrentDialog(), outputPath);
        }
    }

    private void PersistScriptChanges(IEnumerable<string> assetPaths)
    {
        foreach (var assetPath in assetPaths)
        {
            var outputPath = ResolveWorkspaceContentPath(assetPath);
            EnsureParentDirectory(outputPath);
            ScriptFormat.WriteToFile(GetScriptEditor(assetPath).GetCurrentScript(), outputPath);
        }
    }

    private void PersistProtoChanges(IReadOnlyDictionary<string, ProtoData> protos)
    {
        foreach (var (assetPath, proto) in protos)
        {
            var outputPath = ResolveWorkspaceContentPath(assetPath);
            EnsureParentDirectory(outputPath);
            ProtoFormat.WriteToFile(in proto, outputPath);
        }
    }

    private void PersistMobChanges(IReadOnlyDictionary<string, MobData> mobs)
    {
        foreach (var (assetPath, mob) in mobs)
        {
            var outputPath = ResolveWorkspaceContentPath(assetPath);
            EnsureParentDirectory(outputPath);
            MobFormat.WriteToFile(in mob, outputPath);
        }
    }

    private void PersistSectorChanges(IReadOnlyDictionary<string, Sector> sectors)
    {
        foreach (var (assetPath, sector) in sectors)
        {
            var outputPath = ResolveWorkspaceContentPath(assetPath);
            EnsureParentDirectory(outputPath);
            SectorFormat.WriteToFile(in sector, outputPath);
        }
    }

    private void PersistSaveChanges()
    {
        if (_saveEditor is null)
            return;

        if (string.IsNullOrWhiteSpace(Workspace.SaveFolder) || string.IsNullOrWhiteSpace(Workspace.SaveSlotName))
        {
            throw new InvalidOperationException(
                "This workspace does not define SaveFolder and SaveSlotName, so the session cannot persist save changes."
            );
        }

        Directory.CreateDirectory(Workspace.SaveFolder!);
        _saveEditor.Save(Workspace.SaveFolder!, Workspace.SaveSlotName!);
    }

    private string ResolveWorkspaceContentPath(string assetPath) =>
        Path.Combine(Workspace.ContentDirectory, assetPath.Replace('/', Path.DirectorySeparatorChar));

    private static void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private void CommitDialogChanges(IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null)
    {
        foreach (var (assetPath, editor) in _dialogEditors)
        {
            if (
                !editor.HasPendingChanges
                || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Dialog, assetPath)
            )
                continue;

            editor.CommitPendingChanges();
        }
    }

    private void CommitScriptChanges(IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null)
    {
        foreach (var (assetPath, editor) in _scriptEditors)
        {
            if (
                !editor.HasPendingChanges
                || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Script, assetPath)
            )
                continue;

            editor.CommitPendingChanges();
        }
    }

    private void CommitDirectAssetChanges(IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null)
    {
        if (!IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.DirectAssets, null))
            return;

        _pendingProtoAssets.Clear();
        _pendingMobAssets.Clear();
        _pendingSectorAssets.Clear();
        ClearDirectAssetDraftHistory();
    }

    private LoadedSave? CreatePendingSaveSnapshot(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null
    )
    {
        if (
            _saveEditor?.HasPendingChanges != true
            || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Save, GetSaveHistoryScopeTarget())
        )
            return Workspace.Save;

        return _saveEditor.CreateCommittedSnapshot();
    }

    private void CommitSaveChanges(IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys = null)
    {
        if (
            _saveEditor?.HasPendingChanges != true
            || !IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Save, GetSaveHistoryScopeTarget())
        )
            return;

        _saveEditor.CommitPendingChanges();
    }

    private static bool IncludesScope(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys,
        EditorSessionStagedHistoryScopeKind kind,
        string? target
    ) => selectedScopeKeys is null || selectedScopeKeys.Contains(new EditorSessionStagedHistoryScopeKey(kind, target));

    private static HashSet<EditorSessionStagedHistoryScopeKey> NormalizeSelectedStagedTransactionScopeKeys(
        IReadOnlyList<EditorSessionStagedTransactionSummary> stagedTransactions
    )
    {
        ArgumentNullException.ThrowIfNull(stagedTransactions);

        var scopeKeys = new HashSet<EditorSessionStagedHistoryScopeKey>();
        foreach (var stagedTransaction in stagedTransactions)
        {
            ArgumentNullException.ThrowIfNull(stagedTransaction);
            scopeKeys.Add(new EditorSessionStagedHistoryScopeKey(stagedTransaction.Kind, stagedTransaction.Target));
        }

        return scopeKeys;
    }

    private static HashSet<EditorSessionStagedHistoryScopeKey> CreateSelectedScopeKeys(
        EditorSessionStagedHistoryScopeKind kind,
        string? target
    ) => [new EditorSessionStagedHistoryScopeKey(kind, target)];

    private IReadOnlyList<EditorSessionValidationRepairCandidate> GetValidationRepairCandidates(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys
    )
    {
        var candidates = new List<EditorSessionValidationRepairCandidate>();

        foreach (
            var assetPath in Workspace.GameData.DialogsBySource.Keys.OrderBy(
                static path => path,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            if (!IncludesScope(selectedScopeKeys, EditorSessionStagedHistoryScopeKind.Dialog, assetPath))
                continue;

            foreach (var issue in DialogValidator.Validate(GetCurrentDialogAsset(assetPath)))
                AddDialogRepairCandidates(candidates, assetPath, issue);
        }

        return candidates;
    }

    private EditorWorkspaceValidationReport GetPendingValidation(
        IReadOnlySet<EditorSessionStagedHistoryScopeKey>? selectedScopeKeys
    )
    {
        if (selectedScopeKeys is not null && selectedScopeKeys.Count == 0)
            return Workspace.Validation;

        return BuildPendingWorkspaceState(selectedScopeKeys).Workspace.Validation;
    }

    private EditorWorkspaceValidationReport CreateBlockingValidationReport(
        EditorWorkspaceValidationReport pendingValidation
    )
    {
        ArgumentNullException.ThrowIfNull(pendingValidation);

        var blockingIssues = CollectPendingStateBlockingIssues(pendingValidation);
        return blockingIssues.Length == 0
            ? EditorWorkspaceValidationReport.Empty
            : new EditorWorkspaceValidationReport { Issues = blockingIssues };
    }

    private void DiscardDirectAssetChanges()
    {
        _pendingProtoAssets.Clear();
        _pendingMobAssets.Clear();
        _pendingSectorAssets.Clear();
        ClearDirectAssetDraftHistory();
    }

    private List<ScriptRetargetTarget> CollectScriptRetargetTargets(int sourceScriptId)
    {
        var targets = new Dictionary<string, ScriptRetargetTarget>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in Workspace.Index.FindScriptReferences(sourceScriptId))
        {
            var assetPath = reference.Asset.AssetPath;
            if (!CurrentAssetStillContainsScriptReference(assetPath, reference.Format, sourceScriptId))
                continue;

            targets[assetPath] = new ScriptRetargetTarget(assetPath, reference.Format);
        }

        AddPendingScriptRetargetTarget(
            targets,
            _pendingProtoAssets,
            FileFormat.Proto,
            static (asset, scriptId) => EditorScriptReferenceRetargeter.ContainsScriptReference(asset, scriptId),
            sourceScriptId
        );
        AddPendingScriptRetargetTarget(
            targets,
            _pendingMobAssets,
            FileFormat.Mob,
            static (asset, scriptId) => EditorScriptReferenceRetargeter.ContainsScriptReference(asset, scriptId),
            sourceScriptId
        );
        AddPendingScriptRetargetTarget(
            targets,
            _pendingSectorAssets,
            FileFormat.Sector,
            static (asset, scriptId) => EditorScriptReferenceRetargeter.ContainsScriptReference(asset, scriptId),
            sourceScriptId
        );

        return
        [
            .. targets
                .Values.OrderBy(static target => (int)target.Format)
                .ThenBy(static target => target.AssetPath, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private List<ArtReplacementTarget> CollectArtReplacementTargets(uint sourceArtId)
    {
        var targets = new Dictionary<string, ArtReplacementTarget>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in Workspace.Index.FindArtReferences(sourceArtId))
        {
            var assetPath = reference.Asset.AssetPath;
            if (!CurrentAssetStillContainsArtReference(assetPath, reference.Format, sourceArtId))
                continue;

            targets[assetPath] = new ArtReplacementTarget(assetPath, reference.Format);
        }

        AddPendingArtReplacementTarget(
            targets,
            _pendingProtoAssets,
            FileFormat.Proto,
            static (asset, artId) => EditorArtReferenceReplacer.ContainsArtReference(asset, artId),
            sourceArtId
        );
        AddPendingArtReplacementTarget(
            targets,
            _pendingMobAssets,
            FileFormat.Mob,
            static (asset, artId) => EditorArtReferenceReplacer.ContainsArtReference(asset, artId),
            sourceArtId
        );
        AddPendingArtReplacementTarget(
            targets,
            _pendingSectorAssets,
            FileFormat.Sector,
            static (asset, artId) => EditorArtReferenceReplacer.ContainsArtReference(asset, artId),
            sourceArtId
        );

        return
        [
            .. targets
                .Values.OrderBy(static target => (int)target.Format)
                .ThenBy(static target => target.AssetPath, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private bool CurrentAssetStillContainsScriptReference(string assetPath, FileFormat format, int scriptId) =>
        format switch
        {
            FileFormat.Proto => EditorScriptReferenceRetargeter.ContainsScriptReference(
                GetCurrentProtoAsset(assetPath),
                scriptId
            ),
            FileFormat.Mob => EditorScriptReferenceRetargeter.ContainsScriptReference(
                GetCurrentMobAsset(assetPath),
                scriptId
            ),
            FileFormat.Sector => EditorScriptReferenceRetargeter.ContainsScriptReference(
                GetCurrentSectorAsset(assetPath),
                scriptId
            ),
            _ => false,
        };

    private bool CurrentAssetStillContainsArtReference(string assetPath, FileFormat format, uint artId) =>
        format switch
        {
            FileFormat.Proto => EditorArtReferenceReplacer.ContainsArtReference(GetCurrentProtoAsset(assetPath), artId),
            FileFormat.Mob => EditorArtReferenceReplacer.ContainsArtReference(GetCurrentMobAsset(assetPath), artId),
            FileFormat.Sector => EditorArtReferenceReplacer.ContainsArtReference(
                GetCurrentSectorAsset(assetPath),
                artId
            ),
            _ => false,
        };

    private static void AddPendingScriptRetargetTarget<T>(
        Dictionary<string, ScriptRetargetTarget> targets,
        IReadOnlyDictionary<string, T> pendingAssets,
        FileFormat format,
        Func<T, int, bool> containsScriptReference,
        int scriptId
    )
    {
        foreach (var (assetPath, asset) in pendingAssets)
        {
            if (!containsScriptReference(asset, scriptId))
                continue;

            targets[assetPath] = new ScriptRetargetTarget(assetPath, format);
        }
    }

    private static void AddPendingArtReplacementTarget<T>(
        Dictionary<string, ArtReplacementTarget> targets,
        IReadOnlyDictionary<string, T> pendingAssets,
        FileFormat format,
        Func<T, uint, bool> containsArtReference,
        uint artId
    )
    {
        foreach (var (assetPath, asset) in pendingAssets)
        {
            if (!containsArtReference(asset, artId))
                continue;

            targets[assetPath] = new ArtReplacementTarget(assetPath, format);
        }
    }

    private static void ValidateSectorLight(SectorLight light)
    {
        ValidateTileCoordinate("lightTileX", light.TileX);
        ValidateTileCoordinate("lightTileY", light.TileY);
    }

    private static void ValidateTileScript(TileScript tileScript)
    {
        const int maxTileScriptTileId = SectorTileAxisLength * SectorTileAxisLength;
        if (tileScript.TileId >= maxTileScriptTileId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileScript),
                tileScript.TileId,
                $"Tile script tile indices must be between 0 and {maxTileScriptTileId - 1}."
            );
        }
    }

    private static void ValidateSectorItemIndex(
        string paramName,
        int index,
        int count,
        string assetPath,
        string itemName
    )
    {
        if ((uint)index < (uint)count)
            return;

        throw new ArgumentOutOfRangeException(
            paramName,
            index,
            $"Sector asset '{assetPath}' has no {itemName} at index {index}."
        );
    }

    private static int FindSectorObjectIndex(Sector sector, string assetPath, GameObjectGuid objectId)
    {
        var foundIndex = -1;

        for (var i = 0; i < sector.Objects.Count; i++)
        {
            if (sector.Objects[i].Header.ObjectId != objectId)
                continue;

            if (foundIndex >= 0)
            {
                throw new InvalidOperationException(
                    $"Sector asset '{assetPath}' contains multiple objects with ID {objectId}."
                );
            }

            foundIndex = i;
        }

        if (foundIndex >= 0)
            return foundIndex;

        throw new InvalidOperationException($"Sector asset '{assetPath}' does not contain object {objectId}.");
    }

    private static (int X, int Y)? TryGetObjectLocation(MobData mob)
    {
        var locationProperty = mob.GetProperty(ObjectField.ObjFLocation);
        if (locationProperty is null)
            return null;

        try
        {
            return locationProperty.GetLocation();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static int? TryGetObjectIntProperty(MobData mob, ObjectField field)
    {
        var property = mob.GetProperty(field);
        if (property is null)
            return null;

        try
        {
            return property.GetInt32();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static uint GetRoofArtId(Sector sector, int roofX, int roofY)
    {
        if (sector.Roofs is null)
            return 0;

        return sector.Roofs[(roofY * SectorRoofAxisLength) + roofX];
    }

    private MobData CreateSectorObjectFromProto(int protoNumber, int tileX, int tileY)
    {
        if (protoNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(protoNumber), protoNumber, "Proto numbers must be positive.");
        }

        ValidateTileCoordinate(nameof(tileX), tileX);
        ValidateTileCoordinate(nameof(tileY), tileY);

        var protoAsset =
            Workspace.Index.FindProtoDefinition(protoNumber)
            ?? throw new InvalidOperationException(
                $"Cannot instantiate object from proto {protoNumber} because no loaded proto definition matched that identifier."
            );

        return CreateSectorObjectFromProto(GetCurrentProtoAsset(protoAsset.AssetPath), protoNumber, tileX, tileY);
    }

    private static MobData CreateSectorObjectFromPalettePlacement(
        ProtoData proto,
        EditorObjectPalettePlacementRequest request,
        int tileX,
        int tileY
    )
    {
        ArgumentNullException.ThrowIfNull(proto);
        ArgumentNullException.ThrowIfNull(request);

        var finalTileX = tileX + request.DeltaTileX;
        var finalTileY = tileY + request.DeltaTileY;
        ValidateTileCoordinate(nameof(tileX), finalTileX);
        ValidateTileCoordinate(nameof(tileY), finalTileY);

        var builder = new MobDataBuilder(
            proto,
            CreateObjectInstanceId(request.ProtoNumber),
            CreateProtoReferenceId(request.ProtoNumber)
        ).WithLocation(finalTileX, finalTileY);

        if (request.AlignToTileGrid)
            builder.WithOffset(0, 0);

        if (request.Rotation.HasValue)
            builder.WithRotation(request.Rotation.Value);

        if (request.RotationPitch.HasValue)
            builder.WithRotationPitch(request.RotationPitch.Value);

        return builder.Build();
    }

    private static MobData CreateSectorObjectFromProto(ProtoData proto, int protoNumber, int tileX, int tileY) =>
        CreateSectorObjectFromPalettePlacement(
            proto,
            EditorObjectPalettePlacementRequest.Place(protoNumber),
            tileX,
            tileY
        );

    private static GameObjectGuid CreateProtoReferenceId(int protoNumber)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, protoNumber);
        return new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, new Guid(bytes));
    }

    private static GameObjectGuid CreateObjectInstanceId(int protoNumber) =>
        new(GameObjectGuid.OidTypeGuid, 0, protoNumber, Guid.NewGuid());

    private static void ValidateTileCoordinate(string paramName, int value)
    {
        if ((uint)value >= SectorTileAxisLength)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Tile coordinates must be between 0 and {SectorTileAxisLength - 1}."
            );
        }
    }

    private static void ValidateRoofCoordinate(string paramName, int value)
    {
        if ((uint)value >= SectorRoofAxisLength)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Roof coordinates must be between 0 and {SectorRoofAxisLength - 1}."
            );
        }
    }

    private ProtoData GetCurrentProtoAssetForPlacement(EditorObjectPalettePlacementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProtoNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.ProtoNumber,
                "Proto numbers must be positive."
            );
        }

        var protoAsset =
            Workspace.Index.FindProtoDefinition(request.ProtoNumber)
            ?? throw new InvalidOperationException(
                $"Cannot instantiate object from proto {request.ProtoNumber} because no loaded proto definition matched that identifier."
            );

        return GetCurrentProtoAsset(protoAsset.AssetPath);
    }

    private ProtoData GetCurrentProtoAsset(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (_pendingProtoAssets.TryGetValue(normalizedPath, out var pendingProto))
            return pendingProto;

        return Workspace.GameData.ProtosBySource.TryGetValue(normalizedPath, out var protos)
            ? protos.First()
            : throw new InvalidOperationException($"No loaded prototype asset matched '{normalizedPath}'.");
    }

    private MobData GetCurrentMobAsset(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (_pendingMobAssets.TryGetValue(normalizedPath, out var pendingMob))
            return pendingMob;

        return Workspace.GameData.MobsBySource.TryGetValue(normalizedPath, out var mobs)
            ? mobs.First()
            : throw new InvalidOperationException($"No loaded mobile asset matched '{normalizedPath}'.");
    }

    private Sector GetCurrentSectorAsset(string assetPath)
    {
        var normalizedPath = NormalizeAssetPath(assetPath);
        if (_pendingSectorAssets.TryGetValue(normalizedPath, out var pendingSector))
            return pendingSector;

        return Workspace.FindSector(normalizedPath)
            ?? throw new InvalidOperationException($"No loaded sector asset matched '{normalizedPath}'.");
    }

    private static EditorSessionChange CreateDirectAssetChange(ScriptRetargetTarget target) =>
        CreateDirectAssetChange(target.AssetPath, target.Format);

    private static EditorSessionChange CreateDirectAssetChange(ArtReplacementTarget target) =>
        CreateDirectAssetChange(target.AssetPath, target.Format);

    private static EditorSessionChange CreateDirectAssetChange(string assetPath, FileFormat format) =>
        new()
        {
            Kind = format switch
            {
                FileFormat.Proto => EditorSessionChangeKind.Proto,
                FileFormat.Mob => EditorSessionChangeKind.Mob,
                FileFormat.Sector => EditorSessionChangeKind.Sector,
                _ => throw new InvalidOperationException(
                    $"Direct asset changes do not support assets of format {format}."
                ),
            },
            Target = assetPath,
        };

    private readonly record struct EditorWorkspaceSessionSnapshot(EditorWorkspace Workspace, EditorProject Project);

    private readonly record struct EditorWorkspaceSessionDirectAssetSnapshot(
        IReadOnlyDictionary<string, ProtoData> Protos,
        IReadOnlyDictionary<string, MobData> Mobs,
        IReadOnlyDictionary<string, Sector> Sectors
    );

    private readonly record struct EditorWorkspaceSessionHistoryFrame(
        EditorWorkspaceSessionSnapshot Snapshot,
        EditorSessionHistoryEntry Entry
    );

    private readonly record struct EditorSessionStagedHistoryScopeKey(
        EditorSessionStagedHistoryScopeKind Kind,
        string? Target
    );

    private readonly record struct ScriptRetargetTarget(string AssetPath, FileFormat Format);

    private readonly record struct ArtReplacementTarget(string AssetPath, FileFormat Format);

    private readonly record struct PendingWorkspaceState(
        IReadOnlyList<EditorSessionChange> Changes,
        EditorWorkspace Workspace
    );
}
