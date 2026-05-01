using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Fluent mutable builder for <see cref="ScrFile"/> instances.
/// Construct from an existing file to edit it, or from the parameterless constructor
/// to start with an empty script. Call <see cref="Build"/> to produce an immutable
/// <see cref="ScrFile"/>.
/// </summary>
public sealed class ScriptBuilder
{
    private readonly List<ScriptConditionData> _entries;
    private string _description;
    private ScriptFlags _flags;
    private uint _headerFlags;
    private uint _headerCounters;

    /// <summary>Initialises an empty script builder.</summary>
    public ScriptBuilder()
    {
        _entries = [];
        _description = string.Empty;
        _flags = ScriptFlags.None;
        _headerFlags = 0;
        _headerCounters = 0;
    }

    /// <summary>
    /// Initialises a builder pre-populated with all data from <paramref name="existing"/>.
    /// </summary>
    public ScriptBuilder(ScrFile existing)
    {
        _entries = new List<ScriptConditionData>(existing.Entries);
        _description = existing.Description;
        _flags = existing.Flags;
        _headerFlags = existing.HeaderFlags;
        _headerCounters = existing.HeaderCounters;
    }

    // ── Condition mutations ───────────────────────────────────────────────────

    /// <summary>Appends a condition/action node at the end of the condition list.</summary>
    public ScriptBuilder AddCondition(ScriptConditionData condition)
    {
        _entries.Add(condition);
        return this;
    }

    /// <summary>
    /// Appends a condition/action node using typed opcodes and empty operand buffers.
    /// Use the raw <see cref="AddCondition(ScriptConditionData)"/> overload when custom operands are required.
    /// </summary>
    public ScriptBuilder AddCondition(
        ScriptConditionType conditionType,
        ScriptActionType actionType = ScriptActionType.DoNothing,
        ScriptActionType elseActionType = ScriptActionType.DoNothing
    ) => AddCondition(CreateCondition(conditionType, actionType, elseActionType));

    /// <summary>
    /// Replaces the condition-operand buffer for one entry using typed operand descriptors.
    /// Use this after the typed add/replace overloads when the condition needs non-empty operands.
    /// </summary>
    public ScriptBuilder SetConditionOperands(int index, ReadOnlySpan<ScriptOperand> operands)
    {
        _entries[index] = WithConditionOperands(_entries[index], operands);
        return this;
    }

    /// <summary>
    /// Replaces the action-operand buffer for one entry using typed operand descriptors.
    /// </summary>
    public ScriptBuilder SetActionOperands(int index, ReadOnlySpan<ScriptOperand> operands)
    {
        var entry = _entries[index];
        _entries[index] = entry with { Action = WithOperands(entry.Action, operands) };
        return this;
    }

    /// <summary>
    /// Replaces the else-action operand buffer for one entry using typed operand descriptors.
    /// </summary>
    public ScriptBuilder SetElseActionOperands(int index, ReadOnlySpan<ScriptOperand> operands)
    {
        var entry = _entries[index];
        _entries[index] = entry with { Else = WithOperands(entry.Else, operands) };
        return this;
    }

    /// <summary>Removes the condition at <paramref name="index"/>.</summary>
    public ScriptBuilder RemoveCondition(int index)
    {
        _entries.RemoveAt(index);
        return this;
    }

    /// <summary>Replaces the condition at <paramref name="index"/> with <paramref name="condition"/>.</summary>
    public ScriptBuilder ReplaceCondition(int index, ScriptConditionData condition)
    {
        _entries[index] = condition;
        return this;
    }

    /// <summary>
    /// Replaces the condition at <paramref name="index"/> using typed opcodes and empty operand buffers.
    /// Use the raw <see cref="ReplaceCondition(int, ScriptConditionData)"/> overload when custom operands are required.
    /// </summary>
    public ScriptBuilder ReplaceCondition(
        int index,
        ScriptConditionType conditionType,
        ScriptActionType actionType = ScriptActionType.DoNothing,
        ScriptActionType elseActionType = ScriptActionType.DoNothing
    ) => ReplaceCondition(index, CreateCondition(conditionType, actionType, elseActionType));

    // ── Metadata ──────────────────────────────────────────────────────────────

    /// <summary>Sets the human-readable script description (truncated to 40 ASCII chars on disk).</summary>
    public ScriptBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the script behaviour flags (<c>SF_*</c> bitmask).</summary>
    public ScriptBuilder WithFlags(ScriptFlags flags)
    {
        _flags = flags;
        return this;
    }

    /// <summary>Sets the low-level header flags written to the 8-byte <c>ScriptHeader</c>.</summary>
    public ScriptBuilder WithHeaderFlags(uint flags)
    {
        _headerFlags = flags;
        return this;
    }

    /// <summary>Sets the header counter bitmask written to the 8-byte <c>ScriptHeader</c>.</summary>
    public ScriptBuilder WithHeaderCounters(uint counters)
    {
        _headerCounters = counters;
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the current builder state using <see cref="ScriptValidator"/>.
    /// </summary>
    public IReadOnlyList<ScriptValidationIssue> Validate() => ScriptValidator.Validate(Build());

    /// <summary>
    /// Produces an immutable <see cref="ScrFile"/> from the current builder state.
    /// </summary>
    public ScrFile Build() =>
        new()
        {
            HeaderFlags = _headerFlags,
            HeaderCounters = _headerCounters,
            Description = _description,
            Flags = _flags,
            Entries = _entries.AsReadOnly(),
        };

    private static ScriptConditionData CreateCondition(
        ScriptConditionType conditionType,
        ScriptActionType actionType,
        ScriptActionType elseActionType
    ) => new((int)conditionType, default, default, CreateAction((int)actionType), CreateAction((int)elseActionType));

    private static ScriptConditionData WithConditionOperands(
        ScriptConditionData condition,
        ReadOnlySpan<ScriptOperand> operands
    )
    {
        var (opTypes, opValues) = CreateOperandBuffers(operands, nameof(operands));
        return condition with { OpTypes = opTypes, OpValues = opValues };
    }

    private static ScriptActionData WithOperands(ScriptActionData action, ReadOnlySpan<ScriptOperand> operands)
    {
        var (opTypes, opValues) = CreateOperandBuffers(operands, nameof(operands));
        return action with { OpTypes = opTypes, OpValues = opValues };
    }

    private static (OpTypeBuffer OpTypes, OpValueBuffer OpValues) CreateOperandBuffers(
        ReadOnlySpan<ScriptOperand> operands,
        string paramName
    )
    {
        if (operands.Length > 8)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                operands.Length,
                "Script conditions and actions support at most 8 operands."
            );
        }

        OpTypeBuffer opTypes = default;
        OpValueBuffer opValues = default;
        for (var i = 0; i < operands.Length; i++)
        {
            opTypes[i] = operands[i].Type;
            opValues[i] = operands[i].Value;
        }

        return (opTypes, opValues);
    }

    private static ScriptActionData CreateAction(int actionType) => new(actionType, default, default);
}
