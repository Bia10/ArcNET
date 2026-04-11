using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Stateful editor session for modifying player-character data in an Arcanum save game.
/// </summary>
/// <remarks>
/// Typical workflow:
/// <code>
/// var editor = new SaveGameEditor(save);
/// editor
///     .WithSaveInfo(info => info.With(displayName: "Edited Slot"))
///     .WithPlayerCharacter(pc => pc.ToBuilder()
///         .WithAlignment(75)
///         .WithSpellNecroBlack(5)
///         .Build())
///     .Save(saveFolder, slotName);
///
/// // Or use the lower-level path + predicate API when you need exact control:
/// if (editor.TryFindPlayerCharacter(out var pc, out var mdyPath))
/// {
///     var updated = pc.ToBuilder()
///         .WithAlignment(75)
///         .Build();
///
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
    private SaveInfo? _pendingInfoUpdate;
    private bool _originalPlayerLocationInitialized;
    private (string Path, int Index)? _originalPlayerLocation;
    private bool _hasPendingPlayerUpdate;

    public SaveGameEditor(LoadedSave save) => _save = save;

    private IEnumerable<KeyValuePair<string, MobileMdyFile>> EnumerateMobileMdys(bool includePending)
    {
        foreach (var (path, original) in _save.MobileMdys)
        {
            if (includePending && _pendingMdyUpdates.TryGetValue(path, out var pending))
                yield return new KeyValuePair<string, MobileMdyFile>(path, pending);
            else
                yield return new KeyValuePair<string, MobileMdyFile>(path, original);
        }
    }

    private MobileMdyFile? GetCurrentMobileMdy(string mdyPath) =>
        _pendingMdyUpdates.TryGetValue(mdyPath, out var pending)
            ? pending
            : _save.MobileMdys.GetValueOrDefault(mdyPath);

    private bool TryFindCharacterCore(
        Func<CharacterRecord, bool> predicate,
        bool includePending,
        out CharacterRecord character,
        out string mdyPath,
        out int recordIndex
    )
    {
        foreach (var (path, mdyFile) in EnumerateMobileMdys(includePending))
        {
            for (var index = 0; index < mdyFile.Records.Count; index++)
            {
                var record = mdyFile.Records[index];
                if (!record.IsCharacter)
                    continue;

                var candidate = CharacterRecord.From(record.Character);
                if (!predicate(candidate))
                    continue;

                character = candidate;
                mdyPath = path;
                recordIndex = index;
                return true;
            }
        }

        character = null!;
        mdyPath = string.Empty;
        recordIndex = -1;
        return false;
    }

    private (string Path, int Index)? GetOriginalPlayerLocation()
    {
        if (_originalPlayerLocationInitialized)
            return _originalPlayerLocation;

        _originalPlayerLocationInitialized = true;
        if (
            TryFindCharacterCore(
                c => c.HasCompleteData && c.Name != null,
                includePending: false,
                out _,
                out var mdyPath,
                out var recordIndex
            )
            || TryFindCharacterCore(c => c.HasCompleteData, includePending: false, out _, out mdyPath, out recordIndex)
        )
            _originalPlayerLocation = (mdyPath, recordIndex);

        return _originalPlayerLocation;
    }

    private bool IsOriginalPlayerLocation(string mdyPath, int recordIndex)
    {
        var playerLocation = GetOriginalPlayerLocation();
        return playerLocation is { Path: var playerPath, Index: var playerIndex }
            && string.Equals(playerPath, mdyPath, StringComparison.OrdinalIgnoreCase)
            && playerIndex == recordIndex;
    }

    private SaveGameEditor WithCharacterAt(string mdyPath, int recordIndex, CharacterRecord updated)
    {
        var source = GetCurrentMobileMdy(mdyPath);
        if (source is null || recordIndex < 0 || recordIndex >= source.Records.Count)
            return this;

        var record = source.Records[recordIndex];
        if (!record.IsCharacter)
            return this;

        var newRecords = source.Records.ToList();
        newRecords[recordIndex] = MobileMdyRecord.FromCharacter(updated.ApplyTo(record.Character));
        _pendingMdyUpdates[mdyPath] = new MobileMdyFile { Records = newRecords.AsReadOnly() };

        if (IsOriginalPlayerLocation(mdyPath, recordIndex))
            _hasPendingPlayerUpdate = true;

        return this;
    }

    private static bool SaveInfoEquals(SaveInfo left, SaveInfo right) =>
        left.Version == right.Version
        && string.Equals(left.ModuleName, right.ModuleName, StringComparison.Ordinal)
        && string.Equals(left.LeaderName, right.LeaderName, StringComparison.Ordinal)
        && string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal)
        && left.MapId == right.MapId
        && left.GameTimeDays == right.GameTimeDays
        && left.GameTimeMs == right.GameTimeMs
        && left.LeaderPortraitId == right.LeaderPortraitId
        && left.LeaderLevel == right.LeaderLevel
        && left.LeaderTileX == right.LeaderTileX
        && left.LeaderTileY == right.LeaderTileY
        && left.StoryState == right.StoryState;

    private SaveInfo BuildCurrentInfo()
    {
        var info = _pendingInfoUpdate ?? _save.Info;

        if (!_hasPendingPlayerUpdate)
            return info;

        var playerLocation = GetOriginalPlayerLocation();
        if (playerLocation is not { Path: var mdyPath, Index: var recordIndex })
            return info;

        var currentMdy = GetCurrentMobileMdy(mdyPath);
        if (currentMdy is null || recordIndex < 0 || recordIndex >= currentMdy.Records.Count)
            return info;

        var record = currentMdy.Records[recordIndex];
        if (!record.IsCharacter)
            return info;

        var player = CharacterRecord.From(record.Character);
        var leaderName = player.Name ?? info.LeaderName;
        var leaderPortraitId = player.PortraitIndex >= 0 ? player.PortraitIndex : info.LeaderPortraitId;
        var leaderLevel = player.Level > 0 ? player.Level : info.LeaderLevel;

        if (
            string.Equals(leaderName, info.LeaderName, StringComparison.Ordinal)
            && leaderPortraitId == info.LeaderPortraitId
            && leaderLevel == info.LeaderLevel
        )
            return info;

        return info.With(leaderName: leaderName, leaderPortraitId: leaderPortraitId, leaderLevel: leaderLevel);
    }

    private SaveInfo? BuildUpdatedInfo()
    {
        var currentInfo = BuildCurrentInfo();
        return SaveInfoEquals(currentInfo, _save.Info) ? null : currentInfo;
    }

    private SaveGameUpdates? BuildUpdates()
    {
        var updatedInfo = BuildUpdatedInfo();
        if (_pendingMdyUpdates.Count == 0 && updatedInfo is null)
            return null;

        return new SaveGameUpdates
        {
            UpdatedInfo = updatedInfo,
            UpdatedMobileMdys = _pendingMdyUpdates.Count > 0 ? _pendingMdyUpdates : null,
        };
    }

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
    ) => TryFindCharacterCore(predicate, includePending: true, out character, out mdyPath, out _);

    /// <summary>
    /// Finds the player character in the save.
    /// A PC is identified as the first v2 character record that has all four SAR arrays
    /// present: stats, basic skills, tech skills, and spell / tech colleges.
    /// Use <see cref="TryFindCharacter(System.Func{ArcNET.Editor.CharacterRecord, bool}, out ArcNET.Editor.CharacterRecord, out string)"/>
    /// with a custom predicate for finer control.
    /// </summary>
    public bool TryFindPlayerCharacter(out CharacterRecord character, out string mdyPath) =>
        TryFindCharacterCore(
            c => c.HasCompleteData && c.Name != null,
            includePending: true,
            out character,
            out mdyPath,
            out _
        ) || TryFindCharacterCore(c => c.HasCompleteData, includePending: true, out character, out mdyPath, out _);

    /// <summary>
    /// Finds the player character in the current editor view.
    /// Queued edits are reflected in the returned record.
    /// </summary>
    public bool TryFindPlayerCharacter(out CharacterRecord character) => TryFindPlayerCharacter(out character, out _);

    private bool TryFindPlayerCharacter(out CharacterRecord character, out string mdyPath, out int recordIndex) =>
        TryFindCharacterCore(
            c => c.HasCompleteData && c.Name != null,
            includePending: true,
            out character,
            out mdyPath,
            out recordIndex
        )
        || TryFindCharacterCore(
            c => c.HasCompleteData,
            includePending: true,
            out character,
            out mdyPath,
            out recordIndex
        );

    // ── Applying updates ──────────────────────────────────────────────────────

    /// <summary>
    /// Queues a character update.
    /// The first v2 record in <paramref name="mdyPath"/> satisfying <paramref name="predicate"/>
    /// will be replaced with <paramref name="updated"/> when <see cref="Save(string,string)"/> is called.
    /// </summary>
    /// <returns><see langword="this"/> for fluent chaining.</returns>
    public SaveGameEditor WithCharacter(string mdyPath, Func<CharacterRecord, bool> predicate, CharacterRecord updated)
    {
        var source = GetCurrentMobileMdy(mdyPath);
        if (source is null)
            return this;

        for (var index = 0; index < source.Records.Count; index++)
        {
            var record = source.Records[index];
            if (!record.IsCharacter)
                continue;

            var candidate = CharacterRecord.From(record.Character);
            if (!predicate(candidate))
                continue;

            return WithCharacterAt(mdyPath, index, updated);
        }

        return this;
    }

    /// <summary>
    /// Queues an update for the player character using the current editor view.
    /// This avoids manual <c>mobile.mdy</c> path discovery and also keeps the save-slot
    /// leader metadata in the <c>.gsi</c> file aligned with the edited player record.
    /// </summary>
    public SaveGameEditor WithPlayerCharacter(Func<CharacterRecord, CharacterRecord> update)
    {
        if (!TryFindPlayerCharacter(out var player, out var mdyPath, out var recordIndex))
            return this;

        return WithCharacterAt(mdyPath, recordIndex, update(player));
    }

    /// <summary>
    /// Queues an update for the player character using the current editor view.
    /// </summary>
    public SaveGameEditor WithPlayerCharacter(CharacterRecord updated)
    {
        if (!TryFindPlayerCharacter(out _, out var mdyPath, out var recordIndex))
            return this;

        return WithCharacterAt(mdyPath, recordIndex, updated);
    }

    /// <summary>
    /// Returns the current player character view after queued edits have been applied.
    /// </summary>
    public bool TryFindPendingPlayerCharacter(out CharacterRecord character) => TryFindPlayerCharacter(out character);

    /// <summary>
    /// Queues a save-slot metadata update using the current editor view.
    /// If the player character is also edited in the same session, leader name / level /
    /// portrait fields are still synchronized from the pending player record before save.
    /// </summary>
    public SaveGameEditor WithSaveInfo(Func<SaveInfo, SaveInfo> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var updated = update(BuildCurrentInfo());
        if (updated is null)
            throw new InvalidOperationException("Save info update delegate must return a SaveInfo instance.");

        return WithSaveInfo(updated);
    }

    /// <summary>
    /// Queues a save-slot metadata update directly.
    /// If the player character is also edited in the same session, leader name / level /
    /// portrait fields are still synchronized from the pending player record before save.
    /// </summary>
    public SaveGameEditor WithSaveInfo(SaveInfo updated)
    {
        ArgumentNullException.ThrowIfNull(updated);

        _pendingInfoUpdate = updated;
        return this;
    }

    // ── Read-back / inspection ────────────────────────────────────────────────

    /// <summary>
    /// Returns the current save-slot metadata view after queued edits have been applied.
    /// This includes explicit <see cref="WithSaveInfo(SaveInfo)"/> updates and any pending
    /// player-driven leader metadata synchronization.
    /// </summary>
    public SaveInfo GetCurrentSaveInfo() => BuildCurrentInfo();

    /// <summary>
    /// Returns the queued <see cref="SaveInfo"/> that would be written by <see cref="Save(string,string)"/>,
    /// or <see langword="null"/> if the current metadata view still matches the loaded save.
    /// </summary>
    public SaveInfo? GetPendingSaveInfo() => BuildUpdatedInfo();

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
        SaveGameWriter.Save(_save, saveFolder, slotName, BuildUpdates());

    /// <summary>
    /// Writes all queued updates to explicit file paths.
    /// </summary>
    public void Save(string gsiPath, string tfaiPath, string tfafPath) =>
        SaveGameWriter.Save(_save, gsiPath, tfaiPath, tfafPath, BuildUpdates());

    /// <summary>
    /// Asynchronously writes all queued updates to <c>{saveFolder}/{slotName}.gsi/.tfai/.tfaf</c>.
    /// </summary>
    public Task SaveAsync(string saveFolder, string slotName, CancellationToken cancellationToken = default) =>
        SaveGameWriter.SaveAsync(_save, saveFolder, slotName, BuildUpdates(), cancellationToken);

    /// <summary>
    /// Asynchronously writes all queued updates to explicit file paths.
    /// </summary>
    public Task SaveAsync(
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        CancellationToken cancellationToken = default
    ) => SaveGameWriter.SaveAsync(_save, gsiPath, tfaiPath, tfafPath, BuildUpdates(), cancellationToken);
}
