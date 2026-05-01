using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Fluent mutable builder for <see cref="DlgFile"/> instances.
/// Construct from an existing file to edit it, or from the parameterless constructor
/// to start with an empty dialogue. Call <see cref="Build"/> to produce an immutable
/// <see cref="DlgFile"/> with entries sorted ascending by <see cref="DialogEntry.Num"/>.
/// </summary>
public sealed class DialogBuilder
{
    private readonly List<DialogEntry> _entries;

    /// <summary>Initialises an empty dialogue builder.</summary>
    public DialogBuilder()
    {
        _entries = [];
    }

    /// <summary>
    /// Initialises a builder pre-populated with all entries from <paramref name="existing"/>.
    /// </summary>
    public DialogBuilder(DlgFile existing)
    {
        _entries = new List<DialogEntry>(existing.Entries);
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends or replaces one NPC reply node.
    /// </summary>
    public DialogBuilder AddNpcReply(
        int num,
        string text,
        int responseTargetNumber = 0,
        string conditions = "",
        string actions = "",
        string genderField = ""
    ) => AddEntry(CreateEntry(num, text, genderField, 0, conditions, responseTargetNumber, actions));

    /// <summary>
    /// Appends or replaces one PC dialogue option node.
    /// </summary>
    public DialogBuilder AddPcOption(
        int num,
        string text,
        int intelligenceRequirement,
        int responseTargetNumber = 0,
        string conditions = "",
        string actions = "",
        string genderField = ""
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intelligenceRequirement);
        return AddEntry(
            CreateEntry(num, text, genderField, intelligenceRequirement, conditions, responseTargetNumber, actions)
        );
    }

    /// <summary>
    /// Appends or replaces one engine control entry such as <c>E:</c>, <c>R:</c>, or <c>T:</c>.
    /// </summary>
    public DialogBuilder AddControlEntry(
        int num,
        string text,
        int responseTargetNumber = 0,
        string conditions = "",
        string actions = ""
    )
    {
        if (!IsControlEntryText(text))
        {
            throw new ArgumentException(
                "Control entry text must be 'E:', 'F:', or start with 'R:', 'C:', or 'T:'.",
                nameof(text)
            );
        }

        return AddEntry(CreateEntry(num, text, string.Empty, 0, conditions, responseTargetNumber, actions));
    }

    /// <summary>
    /// Appends <paramref name="entry"/> to the builder.
    /// If an entry with the same <see cref="DialogEntry.Num"/> already exists it is replaced.
    /// </summary>
    public DialogBuilder AddEntry(DialogEntry entry)
    {
        var index = _entries.FindIndex(e => e.Num == entry.Num);
        if (index >= 0)
            _entries[index] = entry;
        else
            _entries.Add(entry);
        return this;
    }

    /// <summary>
    /// Removes the entry whose <see cref="DialogEntry.Num"/> equals <paramref name="num"/>.
    /// No-op when no such entry exists.
    /// </summary>
    public DialogBuilder RemoveEntry(int num)
    {
        _entries.RemoveAll(e => e.Num == num);
        return this;
    }

    /// <summary>
    /// Applies <paramref name="update"/> to the entry identified by <paramref name="num"/>
    /// and stores the result in its place.
    /// No-op when no entry with that number exists.
    /// </summary>
    public DialogBuilder UpdateEntry(int num, Func<DialogEntry, DialogEntry> update)
    {
        var index = _entries.FindIndex(e => e.Num == num);
        if (index >= 0)
            _entries[index] = update(_entries[index]);
        return this;
    }

    /// <summary>
    /// Rewires the response target for one entry while preserving the rest of the entry payload.
    /// No-op when no entry with that number exists.
    /// </summary>
    public DialogBuilder SetResponseTarget(int num, int responseTargetNumber)
    {
        return UpdateEntry(
            num,
            entry =>
                CreateEntry(
                    entry.Num,
                    entry.Text,
                    entry.GenderField,
                    entry.Iq,
                    entry.Conditions,
                    responseTargetNumber,
                    entry.Actions
                )
        );
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the current builder state using <see cref="DialogValidator"/>.
    /// </summary>
    public IReadOnlyList<DialogValidationIssue> Validate() => DialogValidator.Validate(Build());

    /// <summary>
    /// Produces an immutable <see cref="DlgFile"/> from the current builder state.
    /// Entries are sorted ascending by <see cref="DialogEntry.Num"/>.
    /// </summary>
    public DlgFile Build()
    {
        var sorted = new List<DialogEntry>(_entries);
        sorted.Sort(static (a, b) => a.Num.CompareTo(b.Num));
        return new DlgFile { Entries = sorted.AsReadOnly() };
    }

    private static DialogEntry CreateEntry(
        int num,
        string text,
        string genderField,
        int iq,
        string conditions,
        int responseVal,
        string actions
    ) =>
        new()
        {
            Num = num,
            Text = text,
            GenderField = genderField,
            Iq = iq,
            Conditions = conditions,
            ResponseVal = responseVal,
            Actions = actions,
        };

    private static bool IsControlEntryText(string text)
    {
        return text switch
        {
            "E:" => true,
            "F:" => true,
            _ when text.StartsWith("R:", StringComparison.Ordinal) => true,
            _ when text.StartsWith("C:", StringComparison.Ordinal) => true,
            _ when text.StartsWith("T:", StringComparison.Ordinal) => true,
            _ => false,
        };
    }
}
