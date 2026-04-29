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
    private readonly ISaveGameWriter _saveGameWriter;
    private readonly PendingGameUpdates _pendingUpdates;
    private readonly CharacterLocator _characterLocator;
    private readonly SaveInfoEditor _saveInfoEditor;

    public SaveGameEditor(LoadedSave save)
        : this(save, DefaultSaveGameWriter.Instance) { }

    internal SaveGameEditor(LoadedSave save, ISaveGameWriter saveGameWriter)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(saveGameWriter);

        _save = save;
        _saveGameWriter = saveGameWriter;
        _pendingUpdates = new PendingGameUpdates(_save);
        _characterLocator = new CharacterLocator(_save, _pendingUpdates);
        _saveInfoEditor = new SaveInfoEditor(_save, _characterLocator);
    }

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
        if (
            !_pendingUpdates.MobileMdys.StageIfOriginalExists(
                mdyPath,
                new MobileMdyFile { Records = newRecords.AsReadOnly() }
            )
        )
        {
            throw new InvalidOperationException($"Unable to stage updated mobile.mdy '{mdyPath}'.");
        }

        if (_characterLocator.IsOriginalPlayerLocation(mdyPath, recordIndex))
            _saveInfoEditor.MarkPendingPlayerUpdate();

        return this;
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

        _saveInfoEditor.SetPendingSaveInfo(updated);
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

        _pendingUpdates.Messages.StageIfOriginalExists(path, updated);
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
        return WithMessageFile(path, updated);
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

        _pendingUpdates.TownMapFogs.StageIfOriginalExists(path, updated);
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

        _pendingUpdates.DataSavFiles.StageIfOriginalExists(path, updated);
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

        _pendingUpdates.Data2SavFiles.StageIfOriginalExists(path, updated);
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

        _pendingUpdates.RawFiles.StageIfOriginalExists(path, updatedBytes);
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
}
