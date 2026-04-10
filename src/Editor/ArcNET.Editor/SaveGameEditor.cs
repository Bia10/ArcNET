using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Stateful editor session for modifying player-character data in an Arcanum save game.
/// </summary>
/// <remarks>
/// Typical workflow:
/// <code>
/// var editor = new SaveGameEditor(save);
/// if (editor.TryFindPlayerCharacter(out var pc, out var mdyPath))
/// {
///     var updated = pc.ToBuilder()
///         .WithAlignment(75)
///         .WithSpellNecroBlack(5)
///         .Build();
///     editor.WithCharacter(mdyPath, c => c.Race == pc.Race &amp;&amp; c.Level == pc.Level, updated)
///           .Save(saveFolder, slotName);
/// }
/// </code>
/// </remarks>
public sealed class SaveGameEditor
{
    private readonly LoadedSave _save;

    // Pending typed MobileMdyFile replacements, keyed by virtual path.
    private readonly Dictionary<string, MobileMdyFile> _pendingMdyUpdates = [];

    public SaveGameEditor(LoadedSave save) => _save = save;

    // ── Character discovery ───────────────────────────────────────────────────

    /// <summary>
    /// Scans all <c>mobile.mdy</c> files in the save for a v2 character record
    /// that satisfies <paramref name="predicate"/>.
    /// Returns <see langword="false"/> when no matching record is found.
    /// </summary>
    public bool TryFindCharacter(
        Func<CharacterRecord, bool> predicate,
        out CharacterRecord character,
        out string mdyPath
    )
    {
        foreach (var (path, mdyFile) in _save.MobileMdys)
        {
            foreach (var record in mdyFile.Records)
            {
                if (!record.IsCharacter)
                    continue;
                var cr = CharacterRecord.From(record.Character);
                if (!predicate(cr))
                    continue;
                character = cr;
                mdyPath = path;
                return true;
            }
        }

        character = null!;
        mdyPath = string.Empty;
        return false;
    }

    /// <summary>
    /// Finds the player character in the save.
    /// A PC is identified as the first v2 character record that has all four SAR arrays
    /// present: stats, basic skills, tech skills, and spell / tech colleges.
    /// Use <see cref="TryFindCharacter"/> with a custom predicate for finer control.
    /// </summary>
    public bool TryFindPlayerCharacter(out CharacterRecord character, out string mdyPath) =>
        TryFindCharacter(c => c.HasCompleteData, out character, out mdyPath);

    // ── Applying updates ──────────────────────────────────────────────────────

    /// <summary>
    /// Queues a character update.
    /// The first v2 record in <paramref name="mdyPath"/> satisfying <paramref name="predicate"/>
    /// will be replaced with <paramref name="updated"/> when <see cref="Save(string,string)"/> is called.
    /// </summary>
    /// <returns><see langword="this"/> for fluent chaining.</returns>
    public SaveGameEditor WithCharacter(string mdyPath, Func<CharacterRecord, bool> predicate, CharacterRecord updated)
    {
        var source = _pendingMdyUpdates.TryGetValue(mdyPath, out var pending)
            ? pending
            : _save.MobileMdys.GetValueOrDefault(mdyPath);

        if (source is null)
            return this;

        var newRecords = source
            .Records.Select(r =>
            {
                if (!r.IsCharacter)
                    return r;
                var cr = CharacterRecord.From(r.Character);
                if (!predicate(cr))
                    return r;
                return MobileMdyRecord.FromCharacter(updated.ApplyTo(r.Character));
            })
            .ToList()
            .AsReadOnly();

        _pendingMdyUpdates[mdyPath] = new MobileMdyFile { Records = newRecords };
        return this;
    }

    // ── Read-back / inspection ────────────────────────────────────────────────

    /// <summary>
    /// Returns the queued <see cref="MobileMdyFile"/> for <paramref name="mdyPath"/>,
    /// or <see langword="null"/> if no update has been queued for that path.
    /// Useful for round-trip verification before committing to disk.
    /// </summary>
    public MobileMdyFile? GetPendingMobileMdy(string mdyPath) =>
        _pendingMdyUpdates.TryGetValue(mdyPath, out var f) ? f : null;

    // ── Saving ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes all queued updates to <c>{saveFolder}/{slotName}.gsi/.tfai/.tfaf</c>.
    /// </summary>
    public void Save(string saveFolder, string slotName) =>
        SaveGameWriter.Save(
            _save,
            saveFolder,
            slotName,
            _pendingMdyUpdates.Count > 0 ? new SaveGameUpdates { UpdatedMobileMdys = _pendingMdyUpdates } : null
        );

    /// <summary>
    /// Writes all queued updates to explicit file paths.
    /// </summary>
    public void Save(string gsiPath, string tfaiPath, string tfafPath) =>
        SaveGameWriter.Save(
            _save,
            gsiPath,
            tfaiPath,
            tfafPath,
            _pendingMdyUpdates.Count > 0 ? new SaveGameUpdates { UpdatedMobileMdys = _pendingMdyUpdates } : null
        );
}
