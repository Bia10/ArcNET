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
    private LoadedSave _save;
    private readonly ISaveGameWriter _saveGameWriter;
    private readonly Action<EditorSessionStagedHistoryMutationKind>? _historyMutationObserver;
    private PendingGameUpdates _pendingUpdates;
    private CharacterLocator _characterLocator;
    private SaveInfoEditor _saveInfoEditor;
    private readonly Stack<SaveGameEditorState> _undoSnapshots = new();
    private readonly Stack<SaveGameEditorState> _redoSnapshots = new();

    public SaveGameEditor(LoadedSave save)
        : this(save, DefaultSaveGameWriter.Instance) { }

    internal SaveGameEditor(
        LoadedSave save,
        ISaveGameWriter saveGameWriter,
        Action<EditorSessionStagedHistoryMutationKind>? historyMutationObserver = null
    )
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(saveGameWriter);

        _saveGameWriter = saveGameWriter;
        _historyMutationObserver = historyMutationObserver;
        _save = save;
        _pendingUpdates = new PendingGameUpdates(_save);
        _characterLocator = new CharacterLocator(_save, _pendingUpdates);
        _saveInfoEditor = new SaveInfoEditor(_save, _characterLocator);
    }

    /// <summary>
    /// Returns <see langword="true"/> when one or more save edits are currently staged.
    /// </summary>
    public bool HasPendingChanges => _pendingUpdates.HasPending || _saveInfoEditor.GetPendingSaveInfo() is not null;

    /// <summary>
    /// Returns <see langword="true"/> when one or more staged save edits can be undone.
    /// </summary>
    public bool CanUndo => _undoSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more undone save edits can be redone.
    /// </summary>
    public bool CanRedo => _redoSnapshots.Count > 0;

    private SaveGameEditor WithCharacterAt(string mdyPath, int recordIndex, CharacterRecord updated)
    {
        ArgumentNullException.ThrowIfNull(updated);

        var source =
            _characterLocator.GetCurrentMobileMdy(mdyPath)
            ?? throw new ArgumentException($"No mobile.mdy exists at '{mdyPath}'.", nameof(mdyPath));
        if (recordIndex < 0 || recordIndex >= source.Records.Count)
            throw new ArgumentOutOfRangeException(
                nameof(recordIndex),
                recordIndex,
                "Character record index is out of range."
            );

        var record = source.Records[recordIndex];
        if (!record.IsCharacter)
            throw new ArgumentException(
                $"Record {recordIndex} in '{mdyPath}' is not a character record.",
                nameof(recordIndex)
            );

        var newRecords = source.Records.ToList();
        newRecords[recordIndex] = MobileMdyRecord.FromCharacter(updated.ApplyTo(record.Character));
        var updatedFile = new MobileMdyFile { Records = newRecords.AsReadOnly() };

        return TrackEdit(() =>
        {
            if (MobileMdyFilesEqual(source, updatedFile))
                return false;

            if (!_pendingUpdates.MobileMdys.StageIfOriginalExists(mdyPath, CloneMobileMdyFile(updatedFile)))
                throw new InvalidOperationException($"Unable to stage updated mobile.mdy '{mdyPath}'.");

            if (_characterLocator.IsOriginalPlayerLocation(mdyPath, recordIndex))
                _saveInfoEditor.MarkPendingPlayerUpdate();

            return true;
        });
    }

    private SaveGameUpdates? CreateUpdateBundle()
    {
        var updatedInfo = _saveInfoEditor.GetPendingSaveInfo();
        return _pendingUpdates.ToSaveGameUpdates(updatedInfo);
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
    ) => _characterLocator.TryFindCharacter(predicate, includePending: true, out character, out mdyPath, out _);

    /// <summary>
    /// Finds the player character in the save.
    /// A PC is identified as the first v2 character record that has all four SAR arrays
    /// present: stats, basic skills, tech skills, and spell / tech colleges.
    /// Use <see cref="TryFindCharacter(System.Func{ArcNET.Editor.CharacterRecord, bool}, out ArcNET.Editor.CharacterRecord, out string)"/>
    /// with a custom predicate for finer control.
    /// </summary>
    public bool TryFindPlayerCharacter(out CharacterRecord character, out string mdyPath) =>
        _characterLocator.TryFindPlayerCharacter(includePending: true, out character, out mdyPath, out _);

    /// <summary>
    /// Finds the player character in the current editor view.
    /// Queued edits are reflected in the returned record.
    /// </summary>
    public bool TryFindPlayerCharacter(out CharacterRecord character) => TryFindPlayerCharacter(out character, out _);

    private bool TryFindPlayerCharacter(out CharacterRecord character, out string mdyPath, out int recordIndex) =>
        _characterLocator.TryFindPlayerCharacter(includePending: true, out character, out mdyPath, out recordIndex);

    // ── Applying updates ──────────────────────────────────────────────────────

    /// <summary>
    /// Queues a character update.
    /// The first v2 record in <paramref name="mdyPath"/> satisfying <paramref name="predicate"/>
    /// will be replaced with <paramref name="updated"/> when <see cref="Save(string,string)"/> is called.
    /// </summary>
    /// <returns><see langword="this"/> for fluent chaining.</returns>
    public SaveGameEditor WithCharacter(string mdyPath, Func<CharacterRecord, bool> predicate, CharacterRecord updated)
    {
        var source = _characterLocator.GetCurrentMobileMdy(mdyPath);
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

        var updated = update(_saveInfoEditor.GetCurrentSaveInfo());
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

        return TrackEdit(() =>
        {
            if (SaveInfoEditor.AreEqual(updated, _saveInfoEditor.GetCurrentSaveInfo()))
                return false;

            _saveInfoEditor.SetPendingSaveInfo(SaveInfoEditor.Clone(updated));
            return true;
        });
    }

    /// <summary>
    /// Restores the previous staged save snapshot.
    /// </summary>
    public SaveGameEditor Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("This save editor has no staged edit to undo.");

        _redoSnapshots.Push(CaptureState());
        RestoreState(_undoSnapshots.Pop());
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Undo);
        return this;
    }

    /// <summary>
    /// Reapplies the most recently undone staged save snapshot.
    /// </summary>
    public SaveGameEditor Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("This save editor has no staged edit to redo.");

        _undoSnapshots.Push(CaptureState());
        RestoreState(_redoSnapshots.Pop());
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Redo);
        return this;
    }

    /// <summary>
    /// Promotes the staged save snapshot to the new committed baseline and clears the pending state.
    /// Returns the committed loaded-save view.
    /// </summary>
    public LoadedSave CommitPendingChanges()
    {
        if (!HasPendingChanges)
        {
            ClearHistory();
            return _save;
        }

        var committedSave = CreateCommittedSnapshot();
        ResetCommittedState(committedSave);
        return _save;
    }

    /// <summary>
    /// Clears staged edits and restores the current committed save view.
    /// </summary>
    public SaveGameEditor DiscardPendingChanges()
    {
        ResetCommittedState(_save);
        return this;
    }

    // ── Read-back / inspection ────────────────────────────────────────────────

    /// <summary>
    /// Returns the current save-slot metadata view after queued edits have been applied.
    /// This includes explicit <see cref="WithSaveInfo(SaveInfo)"/> updates and any pending
    /// player-driven leader metadata synchronization.
    /// </summary>
    public SaveInfo GetCurrentSaveInfo() => _saveInfoEditor.GetCurrentSaveInfo();

    /// <summary>
    /// Returns the queued <see cref="SaveInfo"/> that would be written by <see cref="Save(string,string)"/>,
    /// or <see langword="null"/> if the current metadata view still matches the loaded save.
    /// </summary>
    public SaveInfo? GetPendingSaveInfo() => _saveInfoEditor.GetPendingSaveInfo();

    /// <summary>
    /// Returns the queued <see cref="MobileMdyFile"/> for <paramref name="mdyPath"/>,
    /// or <see langword="null"/> if no update has been queued for that path.
    /// Useful for round-trip verification before committing to disk.
    /// </summary>
    public MobileMdyFile? GetPendingMobileMdy(string mdyPath) => _pendingUpdates.MobileMdys.GetPending(mdyPath);

    /// <summary>
    /// Returns the current typed message-file view for <paramref name="path"/> after queued
    /// edits have been applied, or <see langword="null"/> if the save does not contain that
    /// <c>.mes</c> file.
    /// </summary>
    public MesFile? GetCurrentMessageFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.Messages.GetCurrent(path);
    }

    /// <summary>
    /// Returns the queued typed message-file replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no message update has been staged for that file.
    /// </summary>
    public MesFile? GetPendingMessageFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.Messages.GetPending(path);
    }

    /// <summary>
    /// Queues a typed message-file replacement for <paramref name="path"/>.
    /// Only existing <c>.mes</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithMessageFile(string path, MesFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        var current = GetCurrentMessageFile(path);
        if (current is null || MessageFilesEqual(current, updated))
            return this;

        return TrackEdit(() => _pendingUpdates.Messages.StageIfOriginalExists(path, CloneMessageFile(updated)));
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
        return WithMessageFile(path, updated);
    }

    /// <summary>
    /// Returns the current typed jump-file view for <paramref name="path"/> after queued edits
    /// have been applied, or <see langword="null"/> if the save does not contain that <c>.jmp</c> file.
    /// </summary>
    public JmpFile? GetCurrentJumpFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.JumpFiles.GetCurrent(path);
    }

    /// <summary>
    /// Returns the queued typed jump-file replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no jump-file update has been staged for that file.
    /// </summary>
    public JmpFile? GetPendingJumpFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.JumpFiles.GetPending(path);
    }

    /// <summary>
    /// Queues a typed jump-file replacement for <paramref name="path"/>.
    /// Only existing <c>.jmp</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithJumpFile(string path, JmpFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        var current = GetCurrentJumpFile(path);
        if (current is null || JumpFilesEqual(current, updated))
            return this;

        return TrackEdit(() => _pendingUpdates.JumpFiles.StageIfOriginalExists(path, CloneJumpFile(updated)));
    }

    /// <summary>
    /// Queues a typed jump-file replacement by transforming the current jump-file view.
    /// The callback sees the current editor view, so chained jump-file edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithJumpFile(string path, Func<JmpFile, JmpFile> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentJumpFile(path);
        if (current is null)
            return this;

        var updated = update(current);
        return WithJumpFile(path, updated);
    }

    /// <summary>
    /// Returns the current typed map-properties view for <paramref name="path"/> after queued edits
    /// have been applied, or <see langword="null"/> if the save does not contain that <c>.prp</c> file.
    /// </summary>
    public MapProperties? GetCurrentMapProperties(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.MapProperties.GetCurrent(path);
    }

    /// <summary>
    /// Returns the queued typed map-properties replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no map-properties update has been staged for that file.
    /// </summary>
    public MapProperties? GetPendingMapProperties(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.MapProperties.GetPending(path);
    }

    /// <summary>
    /// Queues a typed map-properties replacement for <paramref name="path"/>.
    /// Only existing <c>.prp</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithMapProperties(string path, MapProperties updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        var current = GetCurrentMapProperties(path);
        if (current is null || MapPropertiesFilesEqual(current, updated))
            return this;

        return TrackEdit(() => _pendingUpdates.MapProperties.StageIfOriginalExists(path, CloneMapProperties(updated)));
    }

    /// <summary>
    /// Queues a typed map-properties replacement by transforming the current map-properties view.
    /// The callback sees the current editor view, so chained map-properties edits compose.
    /// Returns <see langword="this"/> unchanged when the file path does not exist.
    /// </summary>
    public SaveGameEditor WithMapProperties(string path, Func<MapProperties, MapProperties> update)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(update);

        var current = GetCurrentMapProperties(path);
        if (current is null)
            return this;

        var updated = update(current);
        return WithMapProperties(path, updated);
    }

    /// <summary>
    /// Returns the current typed town-map fog view for <paramref name="path"/> after queued edits
    /// have been applied, or <see langword="null"/> if the save does not contain that <c>.tmf</c> file.
    /// </summary>
    public TownMapFog? GetCurrentTownMapFog(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.TownMapFogs.GetCurrent(path);
    }

    /// <summary>
    /// Returns the queued typed town-map fog replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no fog update has been staged for that file.
    /// </summary>
    public TownMapFog? GetPendingTownMapFog(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.TownMapFogs.GetPending(path);
    }

    /// <summary>
    /// Queues a typed town-map fog replacement for <paramref name="path"/>.
    /// Only existing <c>.tmf</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithTownMapFog(string path, TownMapFog updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        var current = GetCurrentTownMapFog(path);
        if (current is null || TownMapFogFilesEqual(current, updated))
            return this;

        return TrackEdit(() => _pendingUpdates.TownMapFogs.StageIfOriginalExists(path, CloneTownMapFog(updated)));
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

        return _pendingUpdates.DataSavFiles.GetCurrent(path);
    }

    /// <summary>
    /// Returns the queued typed <c>data.sav</c> replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no update has been staged for that file.
    /// </summary>
    public DataSavFile? GetPendingDataSav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.DataSavFiles.GetPending(path);
    }

    /// <summary>
    /// Queues a typed <c>data.sav</c> replacement for <paramref name="path"/>.
    /// Only existing successfully parsed <c>data.sav</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithDataSav(string path, DataSavFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        var current = GetCurrentDataSav(path);
        if (current is null || DataSavFilesEqual(current, updated))
            return this;

        return TrackEdit(() => _pendingUpdates.DataSavFiles.StageIfOriginalExists(path, CloneDataSavFile(updated)));
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

        return _pendingUpdates.Data2SavFiles.GetCurrent(path);
    }

    /// <summary>
    /// Returns the queued typed <c>data2.sav</c> replacement for <paramref name="path"/>, or
    /// <see langword="null"/> if no update has been staged for that file.
    /// </summary>
    public Data2SavFile? GetPendingData2Sav(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return _pendingUpdates.Data2SavFiles.GetPending(path);
    }

    /// <summary>
    /// Queues a typed <c>data2.sav</c> replacement for <paramref name="path"/>.
    /// Only existing successfully parsed <c>data2.sav</c> files can be updated through this API.
    /// </summary>
    public SaveGameEditor WithData2Sav(string path, Data2SavFile updated)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(updated);

        var current = GetCurrentData2Sav(path);
        if (current is null || Data2SavFilesEqual(current, updated))
            return this;

        return TrackEdit(() => _pendingUpdates.Data2SavFiles.StageIfOriginalExists(path, CloneData2SavFile(updated)));
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

        var current = _pendingUpdates.RawFiles.GetCurrent(path);
        if (current is null)
            return null;

        return new ReadOnlyMemory<byte>(current);
    }

    /// <summary>
    /// Returns the queued raw replacement for <paramref name="path"/>, or <see langword="null"/>
    /// if no raw update has been staged for that file.
    /// </summary>
    public ReadOnlyMemory<byte>? GetPendingRawFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var pending = _pendingUpdates.RawFiles.GetPending(path);
        if (pending is null)
            return null;

        return new ReadOnlyMemory<byte>(pending);
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

        var current = GetCurrentRawFile(path);
        if (current is null || current.Value.Span.SequenceEqual(updatedBytes))
            return this;

        return TrackEdit(() => _pendingUpdates.RawFiles.StageIfOriginalExists(path, [.. updatedBytes]));
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
        return WithRawFile(path, updated);
    }

    // ── Saving ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes all queued updates to <c>{saveFolder}/{slotName}.gsi/.tfai/.tfaf</c>.
    /// </summary>
    public void Save(string saveFolder, string slotName) =>
        _saveGameWriter.Save(_save, saveFolder, slotName, CreateUpdateBundle());

    /// <summary>
    /// Writes all queued updates to explicit file paths.
    /// </summary>
    public void Save(string gsiPath, string tfaiPath, string tfafPath) =>
        _saveGameWriter.Save(_save, gsiPath, tfaiPath, tfafPath, CreateUpdateBundle());

    /// <summary>
    /// Asynchronously writes all queued updates to <c>{saveFolder}/{slotName}.gsi/.tfai/.tfaf</c>.
    /// </summary>
    public Task SaveAsync(string saveFolder, string slotName, CancellationToken cancellationToken = default) =>
        _saveGameWriter.SaveAsync(_save, saveFolder, slotName, CreateUpdateBundle(), cancellationToken);

    /// <summary>
    /// Asynchronously writes all queued updates to explicit file paths.
    /// </summary>
    public Task SaveAsync(
        string gsiPath,
        string tfaiPath,
        string tfafPath,
        CancellationToken cancellationToken = default
    ) => _saveGameWriter.SaveAsync(_save, gsiPath, tfaiPath, tfafPath, CreateUpdateBundle(), cancellationToken);

    internal LoadedSave CreateCommittedSnapshot()
    {
        var files = SaveGamePayloadComposer.Compose(_save, CreateUpdateBundle());
        var index = SaveGameIndexRebuilder.Rebuild(_save.Index, files);
        return SaveGameLoader.LoadFromFiles(GetCurrentSaveInfo(), index, files);
    }

    internal void ResetCommittedState(LoadedSave save)
    {
        _save = save;
        _pendingUpdates = new PendingGameUpdates(_save);
        _characterLocator = new CharacterLocator(_save, _pendingUpdates);
        _saveInfoEditor = new SaveInfoEditor(_save, _characterLocator);
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
    }

    private SaveGameEditor TrackEdit(Func<bool> applyEdit)
    {
        ArgumentNullException.ThrowIfNull(applyEdit);

        var before = CaptureState();
        if (!applyEdit())
            return this;

        _undoSnapshots.Push(before);
        _redoSnapshots.Clear();
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Edit);
        return this;
    }

    private SaveGameEditorState CaptureState() =>
        new(
            CapturePendingAssets(
                _pendingUpdates.MobileMdys.PendingOrNull,
                static value => MobileMdyFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(
                _pendingUpdates.Messages.PendingOrNull,
                static value => MessageFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(
                _pendingUpdates.JumpFiles.PendingOrNull,
                static value => JmpFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(
                _pendingUpdates.MapProperties.PendingOrNull,
                static value => MapPropertiesFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(
                _pendingUpdates.TownMapFogs.PendingOrNull,
                static value => TownMapFogFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(
                _pendingUpdates.DataSavFiles.PendingOrNull,
                static value => DataSavFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(
                _pendingUpdates.Data2SavFiles.PendingOrNull,
                static value => Data2SavFormat.WriteToArray(in value)
            ),
            CapturePendingAssets(_pendingUpdates.RawFiles.PendingOrNull, static value => [.. value]),
            _saveInfoEditor.CaptureState()
        );

    private void RestoreState(SaveGameEditorState state)
    {
        _pendingUpdates = new PendingGameUpdates(_save);
        RestorePendingAssets(
            _pendingUpdates.MobileMdys,
            state.PendingMobileMdys,
            static bytes => MobileMdyFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(
            _pendingUpdates.Messages,
            state.PendingMessages,
            static bytes => MessageFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(
            _pendingUpdates.JumpFiles,
            state.PendingJumpFiles,
            static bytes => JmpFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(
            _pendingUpdates.MapProperties,
            state.PendingMapProperties,
            static bytes => MapPropertiesFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(
            _pendingUpdates.TownMapFogs,
            state.PendingTownMapFogs,
            static bytes => TownMapFogFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(
            _pendingUpdates.DataSavFiles,
            state.PendingDataSavFiles,
            static bytes => DataSavFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(
            _pendingUpdates.Data2SavFiles,
            state.PendingData2SavFiles,
            static bytes => Data2SavFormat.ParseMemory(bytes)
        );
        RestorePendingAssets(_pendingUpdates.RawFiles, state.PendingRawFiles, static bytes => bytes.ToArray());
        _characterLocator = new CharacterLocator(_save, _pendingUpdates);
        _saveInfoEditor = new SaveInfoEditor(_save, _characterLocator);
        _saveInfoEditor.RestoreState(state.SaveInfoState);
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

    private static Dictionary<string, byte[]> CapturePendingAssets<T>(
        IReadOnlyDictionary<string, T>? pending,
        Func<T, byte[]> serialize
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(serialize);

        var snapshot = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        if (pending is null)
            return snapshot;

        foreach (var (path, value) in pending)
            snapshot[path] = serialize(value);

        return snapshot;
    }

    private static void RestorePendingAssets<T>(
        PendingAssetUpdates<T> pending,
        IReadOnlyDictionary<string, byte[]> snapshot,
        Func<ReadOnlyMemory<byte>, T> parse
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(parse);

        foreach (var (path, bytes) in snapshot)
            _ = pending.StageIfOriginalExists(path, parse(bytes));
    }

    private static MobileMdyFile CloneMobileMdyFile(MobileMdyFile file) =>
        MobileMdyFormat.ParseMemory(MobileMdyFormat.WriteToArray(in file));

    private static MesFile CloneMessageFile(MesFile file) =>
        MessageFormat.ParseMemory(MessageFormat.WriteToArray(in file));

    private static JmpFile CloneJumpFile(JmpFile file) => JmpFormat.ParseMemory(JmpFormat.WriteToArray(in file));

    private static MapProperties CloneMapProperties(MapProperties value) =>
        MapPropertiesFormat.ParseMemory(MapPropertiesFormat.WriteToArray(in value));

    private static TownMapFog CloneTownMapFog(TownMapFog fog) =>
        TownMapFogFormat.ParseMemory(TownMapFogFormat.WriteToArray(in fog));

    private static DataSavFile CloneDataSavFile(DataSavFile file) =>
        DataSavFormat.ParseMemory(DataSavFormat.WriteToArray(in file));

    private static Data2SavFile CloneData2SavFile(Data2SavFile file) =>
        Data2SavFormat.ParseMemory(Data2SavFormat.WriteToArray(in file));

    private static bool MobileMdyFilesEqual(MobileMdyFile left, MobileMdyFile right) =>
        MobileMdyFormat.WriteToArray(in left).AsSpan().SequenceEqual(MobileMdyFormat.WriteToArray(in right));

    private static bool MessageFilesEqual(MesFile left, MesFile right) =>
        MessageFormat.WriteToArray(in left).AsSpan().SequenceEqual(MessageFormat.WriteToArray(in right));

    private static bool JumpFilesEqual(JmpFile left, JmpFile right) =>
        JmpFormat.WriteToArray(in left).AsSpan().SequenceEqual(JmpFormat.WriteToArray(in right));

    private static bool MapPropertiesFilesEqual(MapProperties left, MapProperties right) =>
        MapPropertiesFormat.WriteToArray(in left).AsSpan().SequenceEqual(MapPropertiesFormat.WriteToArray(in right));

    private static bool TownMapFogFilesEqual(TownMapFog left, TownMapFog right) =>
        TownMapFogFormat.WriteToArray(in left).AsSpan().SequenceEqual(TownMapFogFormat.WriteToArray(in right));

    private static bool DataSavFilesEqual(DataSavFile left, DataSavFile right) =>
        DataSavFormat.WriteToArray(in left).AsSpan().SequenceEqual(DataSavFormat.WriteToArray(in right));

    private static bool Data2SavFilesEqual(Data2SavFile left, Data2SavFile right) =>
        Data2SavFormat.WriteToArray(in left).AsSpan().SequenceEqual(Data2SavFormat.WriteToArray(in right));

    private readonly record struct SaveGameEditorState(
        IReadOnlyDictionary<string, byte[]> PendingMobileMdys,
        IReadOnlyDictionary<string, byte[]> PendingMessages,
        IReadOnlyDictionary<string, byte[]> PendingJumpFiles,
        IReadOnlyDictionary<string, byte[]> PendingMapProperties,
        IReadOnlyDictionary<string, byte[]> PendingTownMapFogs,
        IReadOnlyDictionary<string, byte[]> PendingDataSavFiles,
        IReadOnlyDictionary<string, byte[]> PendingData2SavFiles,
        IReadOnlyDictionary<string, byte[]> PendingRawFiles,
        SaveInfoEditor.StateSnapshot SaveInfoState
    );
}
