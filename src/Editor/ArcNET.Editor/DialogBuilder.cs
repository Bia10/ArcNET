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

    // ── Build ─────────────────────────────────────────────────────────────────

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
}
