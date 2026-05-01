using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public sealed class ScriptEditorTests
{
    [Test]
    public async Task AddCondition_QueuesPendingScript_AndCurrentViewUsesPendingState()
    {
        var editor = new ScriptEditor(CreateScript()).AddCondition(
            ScriptConditionType.HasGold,
            ScriptActionType.FloatLine
        );

        var current = editor.GetCurrentScript();
        var pending = editor.GetPendingScript();

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(current.Entries.Count).IsEqualTo(1);
        await Assert.That(current.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.HasGold);
        await Assert.That(current.Entries[0].Action.ActionType).IsEqualTo(ScriptActionType.FloatLine);
        await Assert.That(pending).IsNotNull();
        await Assert.That(pending!.Entries.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Edit_UsesPendingStateAcrossChainedCalls()
    {
        var editor = new ScriptEditor(CreateScript())
            .AddCondition(ScriptConditionType.HasGold, ScriptActionType.FloatLine)
            .SetConditionOperands(0, [ScriptOperand.FromValueType(ScriptValueType.Number, 500)])
            .SetActionOperands(0, [ScriptOperand.FromFocusObject(ScriptFocusObject.Attachee, 3)])
            .WithDescription("Guard logic");

        var pending = editor.GetPendingScript();

        await Assert.That(pending).IsNotNull();
        await Assert.That(pending!.Description).IsEqualTo("Guard logic");
        await Assert.That(pending.Entries.Count).IsEqualTo(1);
        await Assert.That(pending.Entries[0].OpTypes[0]).IsEqualTo((byte)ScriptValueType.Number);
        await Assert.That(pending.Entries[0].OpValues[0]).IsEqualTo(500);
        await Assert.That(pending.Entries[0].Action.OpTypes[0]).IsEqualTo((byte)ScriptFocusObject.Attachee);
        await Assert.That(pending.Entries[0].Action.OpValues[0]).IsEqualTo(3);
    }

    [Test]
    public async Task UndoAndRedo_RewindPendingScriptHistory_AndClearRedoOnNewEdit()
    {
        var editor = new ScriptEditor(CreateScript(description: "Original"))
            .AddCondition(ScriptConditionType.HasGold, ScriptActionType.FloatLine)
            .WithDescription("Updated");

        await Assert.That(editor.CanUndo).IsTrue();
        await Assert.That(editor.CanRedo).IsFalse();
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Updated");
        await Assert.That(editor.GetCurrentScript().Entries.Count).IsEqualTo(1);

        editor.Undo();

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(editor.CanUndo).IsTrue();
        await Assert.That(editor.CanRedo).IsTrue();
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Original");
        await Assert.That(editor.GetCurrentScript().Entries.Count).IsEqualTo(1);

        editor.Undo();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.CanUndo).IsFalse();
        await Assert.That(editor.CanRedo).IsTrue();
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Original");
        await Assert.That(editor.GetCurrentScript().Entries.Count).IsEqualTo(0);

        editor.Redo();

        await Assert.That(editor.HasPendingChanges).IsTrue();
        await Assert.That(editor.CanUndo).IsTrue();
        await Assert.That(editor.CanRedo).IsTrue();
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Original");
        await Assert.That(editor.GetCurrentScript().Entries.Count).IsEqualTo(1);

        editor.WithDescription("Branch");

        await Assert.That(editor.CanRedo).IsFalse();
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Branch");
        await Assert.That(() => editor.Redo()).Throws<InvalidOperationException>();

        editor.DiscardPendingChanges();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.CanUndo).IsFalse();
        await Assert.That(editor.CanRedo).IsFalse();
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Original");
        await Assert.That(editor.GetCurrentScript().Entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CommitPendingChanges_PromotesPendingScript_AndClearsPendingState()
    {
        var editor = new ScriptEditor(CreateScript(description: "Original")).WithDescription("Updated");

        var committed = editor.CommitPendingChanges();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.GetPendingScript()).IsNull();
        await Assert.That(committed.Description).IsEqualTo("Updated");
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Updated");

        editor.AddCondition(ScriptConditionType.True);

        await Assert.That(editor.GetCurrentScript().Entries.Count).IsEqualTo(1);
        await Assert.That(editor.GetCurrentScript().Description).IsEqualTo("Updated");
    }

    [Test]
    public async Task DiscardPendingChanges_RestoresCommittedScript()
    {
        var editor = new ScriptEditor(CreateScript(description: "Original"))
            .WithDescription("Updated")
            .DiscardPendingChanges();

        var current = editor.GetCurrentScript();

        await Assert.That(editor.HasPendingChanges).IsFalse();
        await Assert.That(editor.GetPendingScript()).IsNull();
        await Assert.That(current.Description).IsEqualTo("Original");
        await Assert.That(current.Entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_UsesCurrentPendingScriptView()
    {
        var issues = new ScriptEditor(CreateScript()).WithDescription("Resume - caf\u00E9").Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(ScriptValidationSeverity.Warning);
        await Assert.That(issues[0].Message.Contains("non-ASCII", StringComparison.Ordinal)).IsTrue();
    }

    private static ScrFile CreateScript(string description = "") =>
        new ScriptBuilder().WithDescription(description).Build();
}
