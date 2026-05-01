using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Transactional editor for one dialog file.
/// Edits are staged against the current dialog view until they are committed or discarded.
/// </summary>
public sealed class DialogEditor
{
    private DlgFile _dialog;
    private DlgFile? _pendingDialog;
    private readonly Action<EditorSessionStagedHistoryMutationKind>? _historyMutationObserver;
    private readonly Stack<DlgFile> _undoSnapshots = new();
    private readonly Stack<DlgFile> _redoSnapshots = new();

    /// <summary>
    /// Initializes a dialog editor from one existing dialog file snapshot.
    /// </summary>
    public DialogEditor(DlgFile dialog)
        : this(dialog, historyMutationObserver: null) { }

    internal DialogEditor(DlgFile dialog, Action<EditorSessionStagedHistoryMutationKind>? historyMutationObserver)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        _dialog = CloneDialog(dialog);
        _historyMutationObserver = historyMutationObserver;
    }

    /// <summary>
    /// Returns <see langword="true"/> when one or more edits are currently staged.
    /// </summary>
    public bool HasPendingChanges => _pendingDialog is not null;

    /// <summary>
    /// Returns <see langword="true"/> when one or more staged dialog edits can be undone.
    /// </summary>
    public bool CanUndo => _undoSnapshots.Count > 0;

    /// <summary>
    /// Returns <see langword="true"/> when one or more undone dialog edits can be redone.
    /// </summary>
    public bool CanRedo => _redoSnapshots.Count > 0;

    /// <summary>
    /// Returns the current dialog view after staged edits have been applied.
    /// </summary>
    public DlgFile GetCurrentDialog() => _pendingDialog ?? _dialog;

    /// <summary>
    /// Returns the staged dialog snapshot, or <see langword="null"/> when no edits have been queued.
    /// </summary>
    public DlgFile? GetPendingDialog() => _pendingDialog;

    /// <summary>
    /// Stages a full dialog replacement.
    /// </summary>
    public DialogEditor WithDialog(DlgFile dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (DialogsEqual(dialog, GetCurrentDialog()))
            return this;

        _undoSnapshots.Push(CloneDialog(GetCurrentDialog()));
        _redoSnapshots.Clear();
        RestorePendingDialog(dialog);
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Edit);
        return this;
    }

    /// <summary>
    /// Stages a full dialog replacement using the current editor view as input.
    /// Chained calls observe prior staged edits.
    /// </summary>
    public DialogEditor WithDialog(Func<DlgFile, DlgFile> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        return WithDialog(update(GetCurrentDialog()));
    }

    /// <summary>
    /// Stages dialog edits using <see cref="DialogBuilder"/> on top of the current editor view.
    /// Chained calls observe prior staged edits.
    /// </summary>
    public DialogEditor Edit(Action<DialogBuilder> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var builder = new DialogBuilder(GetCurrentDialog());
        update(builder);
        return WithDialog(builder.Build());
    }

    /// <summary>
    /// Appends or replaces one dialog entry in the staged view.
    /// </summary>
    public DialogEditor AddEntry(DialogEntry entry) => Edit(builder => builder.AddEntry(entry));

    /// <summary>
    /// Appends or replaces one NPC reply node in the staged view.
    /// </summary>
    public DialogEditor AddNpcReply(
        int num,
        string text,
        int responseTargetNumber = 0,
        string conditions = "",
        string actions = "",
        string genderField = ""
    ) => Edit(builder => builder.AddNpcReply(num, text, responseTargetNumber, conditions, actions, genderField));

    /// <summary>
    /// Appends or replaces one PC dialogue option node in the staged view.
    /// </summary>
    public DialogEditor AddPcOption(
        int num,
        string text,
        int intelligenceRequirement,
        int responseTargetNumber = 0,
        string conditions = "",
        string actions = "",
        string genderField = ""
    ) =>
        Edit(builder =>
            builder.AddPcOption(
                num,
                text,
                intelligenceRequirement,
                responseTargetNumber,
                conditions,
                actions,
                genderField
            )
        );

    /// <summary>
    /// Appends or replaces one engine control entry in the staged view.
    /// </summary>
    public DialogEditor AddControlEntry(
        int num,
        string text,
        int responseTargetNumber = 0,
        string conditions = "",
        string actions = ""
    ) => Edit(builder => builder.AddControlEntry(num, text, responseTargetNumber, conditions, actions));

    /// <summary>
    /// Inserts <paramref name="entry"/> after <paramref name="sourceEntryNumber"/> in the current dialog flow.
    /// The inserted entry inherits the source entry's current response target, and the source entry is rewired
    /// to point at the inserted entry.
    /// </summary>
    public DialogEditor InsertEntryAfter(int sourceEntryNumber, DialogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var currentDialog = GetCurrentDialog();
        var sourceEntry = currentDialog.Entries.FirstOrDefault(dialogEntry => dialogEntry.Num == sourceEntryNumber);
        if (sourceEntry is null)
        {
            throw new ArgumentException(
                $"No dialog entry with number {sourceEntryNumber} exists in the current dialog view.",
                nameof(sourceEntryNumber)
            );
        }

        if (currentDialog.Entries.Any(dialogEntry => dialogEntry.Num == entry.Num))
        {
            throw new ArgumentException(
                $"A dialog entry with number {entry.Num} already exists in the current dialog view.",
                nameof(entry)
            );
        }

        if (entry.Num == sourceEntry.ResponseVal)
        {
            throw new ArgumentException(
                $"Inserted entry number {entry.Num} matches the source entry's current response target and would not create a new intermediate hop.",
                nameof(entry)
            );
        }

        var insertedEntry = new DialogEntry
        {
            Num = entry.Num,
            Text = entry.Text,
            GenderField = entry.GenderField,
            Iq = entry.Iq,
            Conditions = entry.Conditions,
            ResponseVal = sourceEntry.ResponseVal,
            Actions = entry.Actions,
        };
        return Edit(builder =>
        {
            builder.AddEntry(insertedEntry);
            builder.SetResponseTarget(sourceEntryNumber, insertedEntry.Num);
        });
    }

    /// <summary>
    /// Inserts one NPC reply node after <paramref name="sourceEntryNumber"/> in the current dialog flow.
    /// </summary>
    public DialogEditor InsertNpcReplyAfter(
        int sourceEntryNumber,
        int num,
        string text,
        string conditions = "",
        string actions = "",
        string genderField = ""
    ) =>
        InsertEntryAfter(
            sourceEntryNumber,
            new DialogEntry
            {
                Num = num,
                Text = text,
                GenderField = genderField,
                Iq = 0,
                Conditions = conditions,
                ResponseVal = 0,
                Actions = actions,
            }
        );

    /// <summary>
    /// Inserts one PC dialogue option node after <paramref name="sourceEntryNumber"/> in the current dialog flow.
    /// </summary>
    public DialogEditor InsertPcOptionAfter(
        int sourceEntryNumber,
        int num,
        string text,
        int intelligenceRequirement,
        string conditions = "",
        string actions = "",
        string genderField = ""
    ) =>
        InsertEntryAfter(
            sourceEntryNumber,
            new DialogEntry
            {
                Num = num,
                Text = text,
                GenderField = genderField,
                Iq = intelligenceRequirement,
                Conditions = conditions,
                ResponseVal = 0,
                Actions = actions,
            }
        );

    /// <summary>
    /// Inserts one engine control entry after <paramref name="sourceEntryNumber"/> in the current dialog flow.
    /// </summary>
    public DialogEditor InsertControlEntryAfter(
        int sourceEntryNumber,
        int num,
        string text,
        string conditions = "",
        string actions = ""
    ) =>
        InsertEntryAfter(
            sourceEntryNumber,
            new DialogEntry
            {
                Num = num,
                Text = text,
                GenderField = string.Empty,
                Iq = 0,
                Conditions = conditions,
                ResponseVal = 0,
                Actions = actions,
            }
        );

    /// <summary>
    /// Removes one entry from the staged view.
    /// </summary>
    public DialogEditor RemoveEntry(int num) => Edit(builder => builder.RemoveEntry(num));

    /// <summary>
    /// Updates one entry in the staged view.
    /// </summary>
    public DialogEditor UpdateEntry(int num, Func<DialogEntry, DialogEntry> update) =>
        Edit(builder => builder.UpdateEntry(num, update));

    /// <summary>
    /// Rewires one response target in the staged view.
    /// </summary>
    public DialogEditor SetResponseTarget(int num, int responseTargetNumber) =>
        Edit(builder => builder.SetResponseTarget(num, responseTargetNumber));

    /// <summary>
    /// Validates the current dialog view after staged edits have been applied.
    /// </summary>
    public IReadOnlyList<DialogValidationIssue> Validate() => DialogValidator.Validate(GetCurrentDialog());

    /// <summary>
    /// Restores the previous staged dialog snapshot.
    /// </summary>
    public DialogEditor Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("This dialog editor has no staged edit to undo.");

        _redoSnapshots.Push(CloneDialog(GetCurrentDialog()));
        RestorePendingDialog(_undoSnapshots.Pop());
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Undo);
        return this;
    }

    /// <summary>
    /// Reapplies the most recently undone staged dialog snapshot.
    /// </summary>
    public DialogEditor Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("This dialog editor has no staged edit to redo.");

        _undoSnapshots.Push(CloneDialog(GetCurrentDialog()));
        RestorePendingDialog(_redoSnapshots.Pop());
        NotifyHistoryMutation(EditorSessionStagedHistoryMutationKind.Redo);
        return this;
    }

    /// <summary>
    /// Promotes the staged dialog snapshot to the new committed baseline and clears the pending state.
    /// Returns the committed dialog view.
    /// </summary>
    public DlgFile CommitPendingChanges()
    {
        if (_pendingDialog is not null)
            _dialog = CloneDialog(_pendingDialog);

        _pendingDialog = null;
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
        return _dialog;
    }

    /// <summary>
    /// Clears staged edits and restores the original committed dialog view.
    /// </summary>
    public DialogEditor DiscardPendingChanges()
    {
        _pendingDialog = null;
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
        return this;
    }

    internal void ResetCommittedState(DlgFile dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        _dialog = CloneDialog(dialog);
        _pendingDialog = null;
        ClearHistory(EditorSessionStagedHistoryMutationKind.Clear);
    }

    private void RestorePendingDialog(DlgFile dialog)
    {
        var restoredDialog = CloneDialog(dialog);
        _pendingDialog = DialogsEqual(_dialog, restoredDialog) ? null : restoredDialog;
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

    private static DlgFile CloneDialog(DlgFile dialog)
    {
        return new DlgFile { Entries = new List<DialogEntry>(dialog.Entries).AsReadOnly() };
    }

    private static bool DialogsEqual(DlgFile left, DlgFile right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left.Entries.Count != right.Entries.Count)
            return false;

        for (var i = 0; i < left.Entries.Count; i++)
        {
            if (!EntriesEqual(left.Entries[i], right.Entries[i]))
                return false;
        }

        return true;
    }

    private static bool EntriesEqual(DialogEntry left, DialogEntry right)
    {
        return left.Num == right.Num
            && string.Equals(left.Text, right.Text, StringComparison.Ordinal)
            && string.Equals(left.GenderField, right.GenderField, StringComparison.Ordinal)
            && left.Iq == right.Iq
            && string.Equals(left.Conditions, right.Conditions, StringComparison.Ordinal)
            && left.ResponseVal == right.ResponseVal
            && string.Equals(left.Actions, right.Actions, StringComparison.Ordinal);
    }
}
