using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public class DialogEditorTests
{
    private static DialogEntry MakeEntry(int num, string text = "Hello", int iq = 0, int responseTargetNumber = 0) =>
        new()
        {
            Num = num,
            Text = text,
            GenderField = string.Empty,
            Iq = iq,
            Conditions = string.Empty,
            ResponseVal = responseTargetNumber,
            Actions = string.Empty,
        };

    [Test]
    public async Task AddNpcReply_QueuesPendingDialog_AndCurrentViewUsesPendingState()
    {
        var editor = new DialogEditor(new DlgFile { Entries = [] }).AddNpcReply(10, "Hello", responseTargetNumber: 20);

        var current = editor.GetCurrentDialog();
        var pending = editor.GetPendingDialog();

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(current.Entries.Count).IsEqualTo(1);
        await Assert.That(current.Entries[0].Num).IsEqualTo(10);
        await Assert.That(current.Entries[0].ResponseVal).IsEqualTo(20);
        await Assert.That(pending).IsNotNull();
        await Assert.That(pending!.Entries.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Edit_UsesPendingStateAcrossChainedCalls()
    {
        var editor = new DialogEditor(new DlgFile { Entries = [] })
            .AddNpcReply(10, "Hello", responseTargetNumber: 20)
            .Edit(builder => builder.AddControlEntry(20, "E:"));

        var pending = editor.GetPendingDialog();

        await Assert.That(pending).IsNotNull();
        await Assert.That(pending!.Entries.Count).IsEqualTo(2);
        await Assert.That(pending.Entries[0].Num).IsEqualTo(10);
        await Assert.That(pending.Entries[1].Num).IsEqualTo(20);
    }

    [Test]
    public async Task UndoAndRedo_RewindPendingDialogHistory_AndClearRedoOnNewEdit()
    {
        var editor = new DialogEditor(new DlgFile { Entries = [MakeEntry(10, "Original")] })
            .AddControlEntry(20, "E:")
            .UpdateEntry(
                20,
                entry => new DialogEntry
                {
                    Num = entry.Num,
                    Text = "Exit",
                    GenderField = entry.GenderField,
                    Iq = entry.Iq,
                    Conditions = entry.Conditions,
                    ResponseVal = entry.ResponseVal,
                    Actions = entry.Actions,
                }
            );

        await Assert.That(editor.CanUndo).IsTrue();
        await Assert.That(editor.CanRedo).IsFalse();
        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
        await Assert.That(editor.GetCurrentDialog().Entries.Single(entry => entry.Num == 20).Text).IsEqualTo("Exit");

        editor.Undo();

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(editor.CanUndo).IsTrue();
        await Assert.That(editor.CanRedo).IsTrue();
        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
        await Assert.That(editor.GetCurrentDialog().Entries.Single(entry => entry.Num == 20).Text).IsEqualTo("E:");

        editor.Undo();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.CanUndo).IsFalse();
        await Assert.That(editor.CanRedo).IsTrue();
        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(1);

        editor.Redo();

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(editor.CanUndo).IsTrue();
        await Assert.That(editor.CanRedo).IsTrue();
        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(2);
        await Assert.That(editor.GetCurrentDialog().Entries.Single(entry => entry.Num == 20).Text).IsEqualTo("E:");

        editor.AddControlEntry(30, "F:");

        await Assert.That(editor.CanRedo).IsFalse();
        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(3);
        await Assert.That(() => editor.Redo()).Throws<InvalidOperationException>();

        editor.CommitPendingChanges();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.CanUndo).IsFalse();
        await Assert.That(editor.CanRedo).IsFalse();
    }

    [Test]
    public async Task CommitPendingChanges_PromotesPendingDialog_AndClearsPendingState()
    {
        var editor = new DialogEditor(new DlgFile { Entries = [MakeEntry(10, "Original")] }).AddControlEntry(20, "E:");

        var committed = editor.CommitPendingChanges();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.GetPendingDialog()).IsNull();
        await Assert.That(committed.Entries.Count).IsEqualTo(2);
        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(2);

        editor.RemoveEntry(20);

        await Assert.That(editor.GetCurrentDialog().Entries.Count).IsEqualTo(1);
        await Assert.That(editor.GetCurrentDialog().Entries[0].Num).IsEqualTo(10);
    }

    [Test]
    public async Task DiscardPendingChanges_RestoresCommittedDialog()
    {
        var editor = new DialogEditor(new DlgFile { Entries = [MakeEntry(10, "Original")] })
            .AddControlEntry(20, "E:")
            .DiscardPendingChanges();

        var current = editor.GetCurrentDialog();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.GetPendingDialog()).IsNull();
        await Assert.That(current.Entries.Count).IsEqualTo(1);
        await Assert.That(current.Entries[0].Num).IsEqualTo(10);
        await Assert.That(current.Entries[0].Text).IsEqualTo("Original");
    }

    [Test]
    public async Task Validate_UsesCurrentPendingDialogView()
    {
        var issues = new DialogEditor(new DlgFile { Entries = [] })
            .AddNpcReply(10, "Hello", responseTargetNumber: 99)
            .Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(DialogValidationSeverity.Warning);
        await Assert.That(issues[0].EntryNumber).IsEqualTo(10);
    }

    [Test]
    public async Task InsertNpcReplyAfter_RewiresSourceAndPreservesOriginalTarget()
    {
        var editor = new DialogEditor(
            new DlgFile { Entries = [MakeEntry(10, "Start", responseTargetNumber: 30), MakeEntry(30, "End")] }
        ).InsertNpcReplyAfter(10, 20, "Middle");

        var current = editor.GetCurrentDialog();
        var start = current.Entries.Single(entry => entry.Num == 10);
        var middle = current.Entries.Single(entry => entry.Num == 20);
        var end = current.Entries.Single(entry => entry.Num == 30);

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(current.Entries.Count).IsEqualTo(3);
        await Assert.That(start.ResponseVal).IsEqualTo(20);
        await Assert.That(middle.Text).IsEqualTo("Middle");
        await Assert.That(middle.ResponseVal).IsEqualTo(30);
        await Assert.That(end.ResponseVal).IsEqualTo(0);
    }

    [Test]
    public async Task InsertPcOptionAfter_UsesPendingStateAcrossChainedCalls()
    {
        var editor = new DialogEditor(
            new DlgFile { Entries = [MakeEntry(10, "Start", responseTargetNumber: 30), MakeEntry(30, "End")] }
        )
            .InsertPcOptionAfter(10, 20, "Ask", 8)
            .InsertNpcReplyAfter(20, 25, "Bridge");

        var current = editor.GetCurrentDialog();
        var first = current.Entries.Single(entry => entry.Num == 10);
        var second = current.Entries.Single(entry => entry.Num == 20);
        var third = current.Entries.Single(entry => entry.Num == 25);

        await Assert.That(current.Entries.Count).IsEqualTo(4);
        await Assert.That(first.ResponseVal).IsEqualTo(20);
        await Assert.That(second.Iq).IsEqualTo(8);
        await Assert.That(second.ResponseVal).IsEqualTo(25);
        await Assert.That(third.ResponseVal).IsEqualTo(30);
    }

    [Test]
    public async Task InsertEntryAfter_UnknownSource_Throws()
    {
        var editor = new DialogEditor(new DlgFile { Entries = [MakeEntry(10, "Start")] });

        await Assert
            .That(() =>
                editor.InsertEntryAfter(
                    99,
                    new DialogEntry
                    {
                        Num = 20,
                        Text = "Middle",
                        GenderField = string.Empty,
                        Iq = 0,
                        Conditions = string.Empty,
                        ResponseVal = 0,
                        Actions = string.Empty,
                    }
                )
            )
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task InsertEntryAfter_ExistingEntryNumber_Throws()
    {
        var editor = new DialogEditor(
            new DlgFile { Entries = [MakeEntry(10, "Start", responseTargetNumber: 30), MakeEntry(30, "End")] }
        );

        await Assert
            .That(() =>
                editor.InsertEntryAfter(
                    10,
                    new DialogEntry
                    {
                        Num = 30,
                        Text = "Collision",
                        GenderField = string.Empty,
                        Iq = 0,
                        Conditions = string.Empty,
                        ResponseVal = 0,
                        Actions = string.Empty,
                    }
                )
            )
            .Throws<ArgumentException>();
    }
}
