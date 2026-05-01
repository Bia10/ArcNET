using ArcNET.Editor;
using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public class DialogBuilderTests
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

    // ── Empty build ───────────────────────────────────────────────────────────

    [Test]
    public async Task Build_EmptyBuilder_ReturnsEmptyFile()
    {
        var dlg = new DialogBuilder().Build();
        await Assert.That(dlg.Entries.Count).IsEqualTo(0);
    }

    // ── AddEntry ──────────────────────────────────────────────────────────────

    [Test]
    public async Task AddEntry_SingleEntry_AppearsInOutput()
    {
        var dlg = new DialogBuilder().AddEntry(MakeEntry(1, "Hi")).Build();
        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("Hi");
    }

    [Test]
    public async Task AddEntry_DuplicateNum_ReplacesExisting()
    {
        var dlg = new DialogBuilder().AddEntry(MakeEntry(1, "Old")).AddEntry(MakeEntry(1, "New")).Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("New");
    }

    [Test]
    public async Task AddNpcReply_ComposesNpcReplyEntry()
    {
        var dlg = new DialogBuilder()
            .AddNpcReply(10, "Hello", responseTargetNumber: 20, conditions: "can_talk", actions: "reply()")
            .Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Num).IsEqualTo(10);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("Hello");
        await Assert.That(dlg.Entries[0].Iq).IsEqualTo(0);
        await Assert.That(dlg.Entries[0].Conditions).IsEqualTo("can_talk");
        await Assert.That(dlg.Entries[0].Actions).IsEqualTo("reply()");
        await Assert.That(dlg.Entries[0].ResponseVal).IsEqualTo(20);
    }

    [Test]
    public async Task AddPcOption_ComposesPcOptionEntry()
    {
        var dlg = new DialogBuilder().AddPcOption(20, "Ask", 8, responseTargetNumber: 30).Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Num).IsEqualTo(20);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("Ask");
        await Assert.That(dlg.Entries[0].Iq).IsEqualTo(8);
        await Assert.That(dlg.Entries[0].ResponseVal).IsEqualTo(30);
    }

    [Test]
    public async Task AddControlEntry_ComposesControlEntry()
    {
        var dlg = new DialogBuilder().AddControlEntry(30, "E:").Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Num).IsEqualTo(30);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("E:");
        await Assert.That(dlg.Entries[0].Iq).IsEqualTo(0);
        await Assert.That(dlg.Entries[0].ResponseVal).IsEqualTo(0);
    }

    // ── RemoveEntry ───────────────────────────────────────────────────────────

    [Test]
    public async Task RemoveEntry_ExistingNum_Removed()
    {
        var dlg = new DialogBuilder().AddEntry(MakeEntry(1)).AddEntry(MakeEntry(2)).RemoveEntry(1).Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Num).IsEqualTo(2);
    }

    [Test]
    public async Task RemoveEntry_AbsentNum_NoOp()
    {
        var dlg = new DialogBuilder().AddEntry(MakeEntry(1)).RemoveEntry(99).Build();
        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
    }

    // ── UpdateEntry ───────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateEntry_ExistingNum_AppliesTransform()
    {
        var dlg = new DialogBuilder()
            .AddEntry(MakeEntry(5, "Original"))
            .UpdateEntry(
                5,
                e => new DialogEntry
                {
                    Num = e.Num,
                    Text = "Updated",
                    GenderField = e.GenderField,
                    Iq = e.Iq,
                    Conditions = e.Conditions,
                    ResponseVal = e.ResponseVal,
                    Actions = e.Actions,
                }
            )
            .Build();

        await Assert.That(dlg.Entries[0].Text).IsEqualTo("Updated");
    }

    [Test]
    public async Task UpdateEntry_AbsentNum_NoOp()
    {
        var dlg = new DialogBuilder()
            .AddEntry(MakeEntry(1, "Keep"))
            .UpdateEntry(99, e => MakeEntry(99, "Ghost"))
            .Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("Keep");
    }

    [Test]
    public async Task SetResponseTarget_ExistingNum_RewiresTransition()
    {
        var dlg = new DialogBuilder()
            .AddNpcReply(10, "Hello", responseTargetNumber: 20)
            .SetResponseTarget(10, 40)
            .Build();

        await Assert.That(dlg.Entries.Count).IsEqualTo(1);
        await Assert.That(dlg.Entries[0].Text).IsEqualTo("Hello");
        await Assert.That(dlg.Entries[0].ResponseVal).IsEqualTo(40);
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_CleanDialog_ReturnsNoIssues()
    {
        var issues = new DialogBuilder()
            .AddNpcReply(10, "Hello", responseTargetNumber: 20)
            .AddPcOption(20, "Ask", 8, responseTargetNumber: 30)
            .AddControlEntry(30, "E:")
            .Validate();

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_DuplicateEntryNumbers_ReportsError()
    {
        var issues = new DialogBuilder(
            new DlgFile { Entries = [MakeEntry(10, "First"), MakeEntry(10, "Second")] }
        ).Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(DialogValidationSeverity.Error);
        await Assert.That(issues[0].EntryNumber).IsEqualTo(10);
    }

    [Test]
    public async Task Validate_NegativeIq_ReportsError()
    {
        var issues = new DialogBuilder().AddEntry(MakeEntry(20, "Broken", iq: -1)).Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(DialogValidationSeverity.Error);
        await Assert.That(issues[0].EntryNumber).IsEqualTo(20);
    }

    [Test]
    public async Task Validate_MissingResponseTarget_ReportsWarning()
    {
        var issues = new DialogBuilder().AddNpcReply(10, "Hello", responseTargetNumber: 99).Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(DialogValidationSeverity.Warning);
        await Assert.That(issues[0].EntryNumber).IsEqualTo(10);
    }

    // ── Ordering ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Build_EntriesSortedByNumAscending()
    {
        var dlg = new DialogBuilder().AddEntry(MakeEntry(30)).AddEntry(MakeEntry(10)).AddEntry(MakeEntry(20)).Build();

        await Assert.That(dlg.Entries[0].Num).IsEqualTo(10);
        await Assert.That(dlg.Entries[1].Num).IsEqualTo(20);
        await Assert.That(dlg.Entries[2].Num).IsEqualTo(30);
    }

    // ── Construct from existing DlgFile ───────────────────────────────────────

    [Test]
    public async Task ConstructFromExisting_PreservesEntries()
    {
        var original = new DialogBuilder().AddEntry(MakeEntry(1, "A")).AddEntry(MakeEntry(2, "B")).Build();

        var modified = new DialogBuilder(original).AddEntry(MakeEntry(3, "C")).Build();

        await Assert.That(modified.Entries.Count).IsEqualTo(3);
    }

    // ── Round-trip through DialogFormat ───────────────────────────────────────

    [Test]
    public async Task Build_RoundTripsThroughDialogFormat()
    {
        var dlg = new DialogBuilder().AddEntry(MakeEntry(1, "Line one")).AddEntry(MakeEntry(2, "Line two")).Build();

        var bytes = DialogFormat.WriteToArray(in dlg);
        var reparsed = DialogFormat.ParseMemory(bytes);

        await Assert.That(reparsed.Entries.Count).IsEqualTo(2);
        await Assert.That(reparsed.Entries[0].Text).IsEqualTo("Line one");
        await Assert.That(reparsed.Entries[1].Text).IsEqualTo("Line two");
    }
}
