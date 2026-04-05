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
}
