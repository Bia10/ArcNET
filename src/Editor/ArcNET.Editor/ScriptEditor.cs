using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Transactional editor for one compiled script file.
/// Edits are staged against the current script view until they are committed or discarded.
/// </summary>
public sealed class ScriptEditor
{
    private ScrFile _script;
    private ScrFile? _pendingScript;
    private readonly Action<EditorSessionStagedHistoryMutationKind>? _historyMutationObserver;
    private readonly Stack<ScrFile> _undoSnapshots = new();
    private readonly Stack<ScrFile> _redoSnapshots = new();

    /// <summary>
    /// Initializes a script editor from one existing script file snapshot.
    /// </summary>
    public ScriptEditor(ScrFile script)
        : this(script, historyMutationObserver: null) { }

    internal ScriptEditor(ScrFile script, Action<EditorSessionStagedHistoryMutationKind>? historyMutationObserver)
    {
        ArgumentNullException.ThrowIfNull(script);
        _script = CloneScript(script);
        _historyMutationObserver = historyMutationObserver;
    }

    /// <summary>
    /// Returns <see langword="true"/> when one or more edits are currently staged.
    /// </summary>
    public bool HasPendingChanges => _pendingScript is not null;

    /// <summary>
    /// Returns <see langword="true"/> when one or more staged script edits can be undone.
    /// </summary>
    public bool CanUndo => _undoSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more undone script edits can be redone.
    /// </summary>
    public bool CanRedo => _redoSnapshots.Count > 0;

    /// <summary>
    /// Returns the current script view after staged edits have been applied.
    /// </summary>
    public ScrFile GetCurrentScript() => _pendingScript ?? _script;

    /// <summary>
    /// Returns the staged script snapshot, or <see langword="null"/> when no edits have been queued.
    /// </summary>
    public ScrFile? GetPendingScript() => _pendingScript;

    /// <summary>
    /// Stages a full script replacement.
    /// </summary>
    public ScriptEditor WithScript(ScrFile script)
    {
        ArgumentNullException.ThrowIfNull(script);

        if (ScriptsEqual(script, GetCurrentScript()))
            return this;

        _undoSnapshots.Push(CloneScript(GetCurrentScript()));
        _redoSnapshots.Clear();
        RestorePendingScript(script);
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Edit);
        return this;
    }

    /// <summary>
    /// Stages a full script replacement using the current editor view as input.
    /// Chained calls observe prior staged edits.
    /// </summary>
    public ScriptEditor WithScript(Func<ScrFile, ScrFile> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        return WithScript(update(GetCurrentScript()));
    }

    /// <summary>
    /// Stages script edits using <see cref="ScriptBuilder"/> on top of the current editor view.
    /// Chained calls observe prior staged edits.
    /// </summary>
    public ScriptEditor Edit(Action<ScriptBuilder> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var builder = new ScriptBuilder(GetCurrentScript());
        update(builder);
        return WithScript(builder.Build());
    }

    /// <summary>
    /// Appends one condition/action node in the staged view.
    /// </summary>
    public ScriptEditor AddCondition(ScriptConditionData condition) => Edit(builder => builder.AddCondition(condition));

    /// <summary>
    /// Appends one typed condition/action node in the staged view.
    /// </summary>
    public ScriptEditor AddCondition(
        ScriptConditionType conditionType,
        ScriptActionType actionType = ScriptActionType.DoNothing,
        ScriptActionType elseActionType = ScriptActionType.DoNothing
    ) => Edit(builder => builder.AddCondition(conditionType, actionType, elseActionType));

    /// <summary>
    /// Replaces the condition operands for one entry in the staged view.
    /// </summary>
    public ScriptEditor SetConditionOperands(int index, ReadOnlySpan<ScriptOperand> operands)
    {
        var copiedOperands = operands.ToArray();
        return Edit(builder => builder.SetConditionOperands(index, copiedOperands));
    }

    /// <summary>
    /// Replaces the action operands for one entry in the staged view.
    /// </summary>
    public ScriptEditor SetActionOperands(int index, ReadOnlySpan<ScriptOperand> operands)
    {
        var copiedOperands = operands.ToArray();
        return Edit(builder => builder.SetActionOperands(index, copiedOperands));
    }

    /// <summary>
    /// Replaces the else-action operands for one entry in the staged view.
    /// </summary>
    public ScriptEditor SetElseActionOperands(int index, ReadOnlySpan<ScriptOperand> operands)
    {
        var copiedOperands = operands.ToArray();
        return Edit(builder => builder.SetElseActionOperands(index, copiedOperands));
    }

    /// <summary>
    /// Removes one condition/action node from the staged view.
    /// </summary>
    public ScriptEditor RemoveCondition(int index) => Edit(builder => builder.RemoveCondition(index));

    /// <summary>
    /// Replaces one condition/action node in the staged view.
    /// </summary>
    public ScriptEditor ReplaceCondition(int index, ScriptConditionData condition) =>
        Edit(builder => builder.ReplaceCondition(index, condition));

    /// <summary>
    /// Replaces one typed condition/action node in the staged view.
    /// </summary>
    public ScriptEditor ReplaceCondition(
        int index,
        ScriptConditionType conditionType,
        ScriptActionType actionType = ScriptActionType.DoNothing,
        ScriptActionType elseActionType = ScriptActionType.DoNothing
    ) => Edit(builder => builder.ReplaceCondition(index, conditionType, actionType, elseActionType));

    /// <summary>
    /// Sets the script description in the staged view.
    /// </summary>
    public ScriptEditor WithDescription(string description) => Edit(builder => builder.WithDescription(description));

    /// <summary>
    /// Sets the script flags in the staged view.
    /// </summary>
    public ScriptEditor WithFlags(ScriptFlags flags) => Edit(builder => builder.WithFlags(flags));

    /// <summary>
    /// Sets the raw header flags in the staged view.
    /// </summary>
    public ScriptEditor WithHeaderFlags(uint flags) => Edit(builder => builder.WithHeaderFlags(flags));

    /// <summary>
    /// Sets the raw header counters in the staged view.
    /// </summary>
    public ScriptEditor WithHeaderCounters(uint counters) => Edit(builder => builder.WithHeaderCounters(counters));

    /// <summary>
    /// Validates the current script view after staged edits have been applied.
    /// </summary>
    public IReadOnlyList<ScriptValidationIssue> Validate() => ScriptValidator.Validate(GetCurrentScript());

    /// <summary>
    /// Restores the previous staged script snapshot.
    /// </summary>
    public ScriptEditor Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("This script editor has no staged edit to undo.");

        _redoSnapshots.Push(CloneScript(GetCurrentScript()));
        RestorePendingScript(_undoSnapshots.Pop());
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Undo);
        return this;
    }

    /// <summary>
    /// Reapplies the most recently undone staged script snapshot.
    /// </summary>
    public ScriptEditor Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("This script editor has no staged edit to redo.");

        _undoSnapshots.Push(CloneScript(GetCurrentScript()));
        RestorePendingScript(_redoSnapshots.Pop());
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Redo);
        return this;
    }

    /// <summary>
    /// Promotes the staged script snapshot to the new committed baseline and clears the pending state.
    /// Returns the committed script view.
    /// </summary>
    public ScrFile CommitPendingChanges()
    {
        if (_pendingScript is not null)
            _script = CloneScript(_pendingScript);

        _pendingScript = null;
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
        return _script;
    }

    /// <summary>
    /// Clears staged edits and restores the original committed script view.
    /// </summary>
    public ScriptEditor DiscardPendingChanges()
    {
        _pendingScript = null;
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
        return this;
    }

    internal void ResetCommittedState(ScrFile script)
    {
        ArgumentNullException.ThrowIfNull(script);

        _script = CloneScript(script);
        _pendingScript = null;
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
    }

    private void RestorePendingScript(ScrFile script)
    {
        var restoredScript = CloneScript(script);
        _pendingScript = ScriptsEqual(_script, restoredScript) ? null : restoredScript;
    }

    private void ClearHistory(EditorSessionStagedHistoryMutationKind? mutationKind = null)
    {
        _undoSnapshots.Clear();
        _redoSnapshots.Clear();

        if (mutationKind is not null)
            NotifyHistoryMutation(mutationKind.Value);
    }

    private void NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind mutationKind) =>
        _historyMutationObserver?.Invoke(mutationKind);

    private static ScrFile CloneScript(ScrFile script)
    {
        return new ScrFile
        {
            HeaderFlags = script.HeaderFlags,
            HeaderCounters = script.HeaderCounters,
            Description = script.Description,
            Flags = script.Flags,
            Entries = new List<ScriptConditionData>(script.Entries).AsReadOnly(),
        };
    }

    private static bool ScriptsEqual(ScrFile left, ScrFile right)
    {
        return left.HeaderFlags == right.HeaderFlags
            && left.HeaderCounters == right.HeaderCounters
            && string.Equals(left.Description, right.Description, StringComparison.Ordinal)
            && left.Flags == right.Flags
            && ConditionsEqual(left.Entries, right.Entries);
    }

    private static bool ConditionsEqual(
        IReadOnlyList<ScriptConditionData> left,
        IReadOnlyList<ScriptConditionData> right
    )
    {
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!ConditionEqual(left[i], right[i]))
                return false;
        }

        return true;
    }

    private static bool ConditionEqual(ScriptConditionData left, ScriptConditionData right)
    {
        return left.Type == right.Type
            && OpTypesEqual(left.OpTypes, right.OpTypes)
            && OpValuesEqual(left.OpValues, right.OpValues)
            && ActionEqual(left.Action, right.Action)
            && ActionEqual(left.Else, right.Else);
    }

    private static bool ActionEqual(ScriptActionData left, ScriptActionData right)
    {
        return left.Type == right.Type
            && OpTypesEqual(left.OpTypes, right.OpTypes)
            && OpValuesEqual(left.OpValues, right.OpValues);
    }

    private static bool OpTypesEqual(OpTypeBuffer left, OpTypeBuffer right)
    {
        for (var i = 0; i < 8; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private static bool OpValuesEqual(OpValueBuffer left, OpValueBuffer right)
    {
        for (var i = 0; i < 8; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }
}
