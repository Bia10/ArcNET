using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Stateful editor session for modifying player-character data, save metadata,
/// and selected save-global assets in an Arcanum save game.
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
    private readonly Dictionary<string, MesFile> _pendingMessageUpdates = [];
    private readonly Dictionary<string, TownMapFog> _pendingTownMapFogUpdates = [];
    private readonly Dictionary<string, DataSavFile> _pendingDataSavUpdates = [];
    private readonly Dictionary<string, Data2SavFile> _pendingData2SavUpdates = [];
    private readonly Dictionary<string, byte[]> _pendingRawFileUpdates = [];
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
        if (
            _pendingMdyUpdates.Count == 0
            && _pendingMessageUpdates.Count == 0
            && _pendingTownMapFogUpdates.Count == 0
            && _pendingDataSavUpdates.Count == 0
            && _pendingData2SavUpdates.Count == 0
            && _pendingRawFileUpdates.Count == 0
            && updatedInfo is null
        )
            return null;

        return new SaveGameUpdates
        {
            UpdatedInfo = updatedInfo,
            UpdatedMobileMdys = _pendingMdyUpdates.Count > 0 ? _pendingMdyUpdates : null,
            UpdatedMessages = _pendingMessageUpdates.Count > 0 ? _pendingMessageUpdates : null,
            UpdatedTownMapFogs = _pendingTownMapFogUpdates.Count > 0 ? _pendingTownMapFogUpdates : null,
            UpdatedDataSavFiles = _pendingDataSavUpdates.Count > 0 ? _pendingDataSavUpdates : null,
            UpdatedData2SavFiles = _pendingData2SavUpdates.Count > 0 ? _pendingData2SavUpdates : null,
            RawFileUpdates = _pendingRawFileUpdates.Count > 0 ? _pendingRawFileUpdates : null,
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

    /// <summary>
    /// Returns the current typed message-file view for <paramref name="path"/> after queued
    /// edits have been applied, or <see langword="null"/> if the save does not contain that
    /// <c>.mes</c> file.
    /// </summary>
    public MesFile? GetCurrentMessageFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingMessageUpdates.TryGetValue(path, out var pending))
            return pending;

        if (_save.Messages.TryGetValue(path, out var current))
            return current;

        return null;
    }

    /// <summary>
    /// Returns the queued typed message-file replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no message update has been staged for that file.
    /// </summary>
    public MesFile? GetPendingMessageFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingMessageUpdates.TryGetValue(path, out var pending))
            return pending;

        return null;
    }

    /// <summary>
    /// Queues a typed message-file replacement for <paramref name="path"/>.
    /// Only existing <c>.mes</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithMessageFile(string path, MesFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        if (!_save.Messages.ContainsKey(path))
            return this;

        _pendingMessageUpdates[path] = updated;
        return this;
    }

    /// <summary>
    /// Queues a typed message-file replacement by transforming the current message view.
    /// The callback sees the current editor view, so chained message edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithMessageFile(string path, Func<MesFile, MesFile> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentMessageFile(path);
        if (current is null)
            return this;

        var updated = update(current);
        if (updated is null)
            throw new InvalidOperationException("Message-file update delegate must return a MesFile instance.");

        return WithMessageFile(path, updated);
    }

    /// <summary>
    /// Returns the current typed town-map fog view for <paramref name="path"/> after queued edits
    /// have been applied, or <see langword="null"/> if the save does not contain that <c>.tmf</c> file.
    /// </summary>
    public TownMapFog? GetCurrentTownMapFog(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingTownMapFogUpdates.TryGetValue(path, out var pending))
            return pending;

        if (_save.TownMapFogs.TryGetValue(path, out var current))
            return current;

        return null;
    }

    /// <summary>
    /// Returns the queued typed town-map fog replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no fog update has been staged for that file.
    /// </summary>
    public TownMapFog? GetPendingTownMapFog(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingTownMapFogUpdates.TryGetValue(path, out var pending))
            return pending;

        return null;
    }

    /// <summary>
    /// Queues a typed town-map fog replacement for <paramref name="path"/>.
    /// Only existing <c>.tmf</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithTownMapFog(string path, TownMapFog updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        if (!_save.TownMapFogs.ContainsKey(path))
            return this;

        _pendingTownMapFogUpdates[path] = updated;
        return this;
    }

    /// <summary>
    /// Queues a typed town-map fog replacement by transforming the current fog view.
    /// The callback sees the current editor view, so chained fog edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithTownMapFog(string path, Func<TownMapFog, TownMapFog> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentTownMapFog(path);
        if (current is null)
            return this;

        var updated = update(current);
        if (updated is null)
            throw new InvalidOperationException("Town-map fog update delegate must return a TownMapFog instance.");

        return WithTownMapFog(path, updated);
    }

    /// <summary>
    /// Returns the current typed <c>data.sav</c> view for <paramref name="path"/> after queued
    /// edits have been applied, or <see langword="null"/> if the save does not contain a
    /// successfully parsed <c>data.sav</c> file at that path.
    /// </summary>
    public DataSavFile? GetCurrentDataSav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingDataSavUpdates.TryGetValue(path, out var pending))
            return pending;

        if (_save.DataSavFiles.TryGetValue(path, out var current))
            return current;

        return null;
    }

    /// <summary>
    /// Returns the queued typed <c>data.sav</c> replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no update has been staged for that file.
    /// </summary>
    public DataSavFile? GetPendingDataSav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingDataSavUpdates.TryGetValue(path, out var pending))
            return pending;

        return null;
    }

    /// <summary>
    /// Queues a typed <c>data.sav</c> replacement for <paramref name="path"/>.
    /// Only existing successfully parsed <c>data.sav</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithDataSav(string path, DataSavFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        if (!_save.DataSavFiles.ContainsKey(path))
            return this;

        _pendingDataSavUpdates[path] = updated;
        return this;
    }

    /// <summary>
    /// Queues a typed <c>data.sav</c> replacement by transforming the current editor view.
    /// The callback sees the current typed state, so chained edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithDataSav(string path, Func<DataSavFile, DataSavFile> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentDataSav(path);
        if (current is null)
            return this;

        var updated = update(current);
        if (updated is null)
            throw new InvalidOperationException("data.sav update delegate must return a DataSavFile instance.");

        return WithDataSav(path, updated);
    }

    /// <summary>
    /// Queues a typed <c>data.sav</c> replacement by mutating a copy-on-write builder created from
    /// the current editor view. This lets callers batch multiple structural edits in one build step
    /// instead of cloning the full raw payload once per chained <c>With*</c> call.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithDataSav(string path, Action<DataSavFile.Builder> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentDataSav(path);
        if (current is null)
            return this;

        var builder = current.ToBuilder();
        update(builder);
        return WithDataSav(path, builder.Build());
    }

    /// <summary>
    /// Returns the current typed <c>data2.sav</c> view for <paramref name="path"/> after queued
    /// edits have been applied, or <see langword="null"/> if the save does not contain a
    /// successfully parsed <c>data2.sav</c> file at that path.
    /// </summary>
    public Data2SavFile? GetCurrentData2Sav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingData2SavUpdates.TryGetValue(path, out var pending))
            return pending;

        if (_save.Data2SavFiles.TryGetValue(path, out var current))
            return current;

        return null;
    }

    /// <summary>
    /// Returns the queued typed <c>data2.sav</c> replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no update has been staged for that file.
    /// </summary>
    public Data2SavFile? GetPendingData2Sav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingData2SavUpdates.TryGetValue(path, out var pending))
            return pending;

        return null;
    }

    /// <summary>
    /// Queues a typed <c>data2.sav</c> replacement for <paramref name="path"/>.
    /// Only existing successfully parsed <c>data2.sav</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithData2Sav(string path, Data2SavFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        if (!_save.Data2SavFiles.ContainsKey(path))
            return this;

        _pendingData2SavUpdates[path] = updated;
        return this;
    }

    /// <summary>
    /// Queues a typed <c>data2.sav</c> replacement by transforming the current editor view.
    /// The callback sees the current typed state, so chained edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithData2Sav(string path, Func<Data2SavFile, Data2SavFile> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentData2Sav(path);
        if (current is null)
            return this;

        var updated = update(current);
        if (updated is null)
            throw new InvalidOperationException("data2.sav update delegate must return a Data2SavFile instance.");

        return WithData2Sav(path, updated);
    }

    /// <summary>
    /// Queues a typed <c>data2.sav</c> replacement by mutating a copy-on-write builder created from
    /// the current editor view. This lets callers batch pair-table and unresolved-region edits in
    /// one build step instead of cloning the raw payload once per chained <c>With*</c> call.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithData2Sav(string path, Action<Data2SavFile.Builder> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentData2Sav(path);
        if (current is null)
            return this;

        var builder = current.ToBuilder();
        update(builder);
        return WithData2Sav(path, builder.Build());
    }

    /// <summary>
    /// Returns the current raw embedded-file bytes for <paramref name="path"/> after queued edits
    /// have been applied, or <see langword="null"/> if the save does not expose that path through
    /// <see cref="LoadedSave.RawFiles"/>.
    /// This is intended for embedded files that do not currently have a successful typed editor
    /// surface beyond the structural <c>data.sav</c> / partial typed <c>data2.sav</c> models.
    /// </summary>
    public ReadOnlyMemory<byte>? GetCurrentRawFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingRawFileUpdates.TryGetValue(path, out var pending))
            return pending;

        if (_save.RawFiles.TryGetValue(path, out var current))
            return current;

        return null;
    }

    /// <summary>
    /// Returns the queued raw replacement for <paramref name="path"/>, or <see langword="null"/>
    /// if no raw update has been staged for that file.
    /// </summary>
    public ReadOnlyMemory<byte>? GetPendingRawFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (_pendingRawFileUpdates.TryGetValue(path, out var pending))
            return pending;

        return null;
    }

    /// <summary>
    /// Queues a raw embedded-file replacement for <paramref name="path"/>.
    /// Only files exposed through <see cref="LoadedSave.RawFiles"/> can be updated through
    /// this API. Typed save surfaces such as <c>mobile.mdy</c>, <c>.tmf</c>, <c>.jmp</c>, and
    /// other parsed formats must go through their dedicated editor APIs.
    /// </summary>
    public SaveGameEditor WithRawFile(string path, byte[] updatedBytes)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updatedBytes);

        if (!_save.RawFiles.ContainsKey(path))
            return this;

        _pendingRawFileUpdates[path] = updatedBytes;
        return this;
    }

    /// <summary>
    /// Queues a raw embedded-file replacement by transforming the current file bytes.
    /// The callback sees the current editor view, so chained raw-file edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithRawFile(string path, Func<ReadOnlyMemory<byte>, byte[]> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentRawFile(path);
        if (current is null)
            return this;

        var updated = update(current.Value);
        if (updated is null)
            throw new InvalidOperationException("Raw file update delegate must return a byte array.");

        return WithRawFile(path, updated);
    }

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
