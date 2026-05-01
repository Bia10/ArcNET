using ArcNET.Editor;
using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public class ScriptBuilderTests
{
    private static ScriptActionData MakeAction(ScriptActionType type = ScriptActionType.DoNothing) =>
        new((int)type, default, default);

    private static ScriptConditionData MakeCondition(ScriptConditionType condType = ScriptConditionType.True) =>
        new((int)condType, default, default, MakeAction(), MakeAction());

    // ── Empty build ───────────────────────────────────────────────────────────

    [Test]
    public async Task Build_EmptyBuilder_ReturnsEmptyScrFile()
    {
        var scr = new ScriptBuilder().Build();

        await Assert.That(scr.Entries.Count).IsEqualTo(0);
        await Assert.That(scr.Description).IsEqualTo(string.Empty);
        await Assert.That(scr.HeaderFlags).IsEqualTo(0u);
    }

    // ── AddCondition ──────────────────────────────────────────────────────────

    [Test]
    public async Task AddCondition_AppendsEntry()
    {
        var scr = new ScriptBuilder().AddCondition(MakeCondition()).Build();
        await Assert.That(scr.Entries.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddCondition_TypedOverload_ComposesConditionAndActions()
    {
        var scr = new ScriptBuilder()
            .AddCondition(ScriptConditionType.ObjIsDead, ScriptActionType.Attack, ScriptActionType.ReturnAndRunDefault)
            .Build();

        await Assert.That(scr.Entries.Count).IsEqualTo(1);
        await Assert.That(scr.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
        await Assert.That(scr.Entries[0].Action.ActionType).IsEqualTo(ScriptActionType.Attack);
        await Assert.That(scr.Entries[0].Else.ActionType).IsEqualTo(ScriptActionType.ReturnAndRunDefault);
    }

    [Test]
    public async Task SetOperands_TypedHelpers_ComposeOperandBuffers()
    {
        var scr = new ScriptBuilder()
            .AddCondition(ScriptConditionType.HasGold, ScriptActionType.FloatLine, ScriptActionType.Attack)
            .SetConditionOperands(0, [ScriptOperand.FromValueType(ScriptValueType.Number, 500)])
            .SetActionOperands(
                0,
                [
                    ScriptOperand.FromFocusObject(ScriptFocusObject.Attachee, 1),
                    ScriptOperand.FromValueType(ScriptValueType.Number, 1200),
                ]
            )
            .SetElseActionOperands(0, [ScriptOperand.FromFocusObject(ScriptFocusObject.Triggerer)])
            .Build();

        await Assert.That(scr.Entries[0].OpTypes[0]).IsEqualTo((byte)ScriptValueType.Number);
        await Assert.That(scr.Entries[0].OpValues[0]).IsEqualTo(500);
        await Assert.That(scr.Entries[0].Action.OpTypes[0]).IsEqualTo((byte)ScriptFocusObject.Attachee);
        await Assert.That(scr.Entries[0].Action.OpValues[0]).IsEqualTo(1);
        await Assert.That(scr.Entries[0].Action.OpTypes[1]).IsEqualTo((byte)ScriptValueType.Number);
        await Assert.That(scr.Entries[0].Action.OpValues[1]).IsEqualTo(1200);
        await Assert.That(scr.Entries[0].Else.OpTypes[0]).IsEqualTo((byte)ScriptFocusObject.Triggerer);
        await Assert.That(scr.Entries[0].Else.OpValues[0]).IsEqualTo(0);
    }

    [Test]
    public async Task ReplaceCondition_UpdatesAtIndex()
    {
        var scr = new ScriptBuilder()
            .AddCondition(MakeCondition(ScriptConditionType.True))
            .ReplaceCondition(0, MakeCondition(ScriptConditionType.ObjIsDead))
            .Build();

        await Assert.That(scr.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
    }

    [Test]
    public async Task ReplaceCondition_TypedOverload_UpdatesAtIndex()
    {
        var scr = new ScriptBuilder()
            .AddCondition(ScriptConditionType.True)
            .ReplaceCondition(0, ScriptConditionType.ObjIsDead, ScriptActionType.Attack)
            .Build();

        await Assert.That(scr.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
        await Assert.That(scr.Entries[0].Action.ActionType).IsEqualTo(ScriptActionType.Attack);
        await Assert.That(scr.Entries[0].Else.ActionType).IsEqualTo(ScriptActionType.DoNothing);
    }

    [Test]
    public async Task SetConditionOperands_MoreThanEight_Throws()
    {
        var builder = new ScriptBuilder().AddCondition(ScriptConditionType.True);
        var operands = Enumerable
            .Range(0, 9)
            .Select(value => ScriptOperand.FromValueType(ScriptValueType.Number, value));

        await Assert.That(() => builder.SetConditionOperands(0, [.. operands])).Throws<ArgumentOutOfRangeException>();
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    [Test]
    public async Task WithDescription_SetsDescription()
    {
        var scr = new ScriptBuilder().WithDescription("My Script").Build();
        await Assert.That(scr.Description).IsEqualTo("My Script");
    }

    [Test]
    public async Task WithFlags_SetsFlags()
    {
        var scr = new ScriptBuilder().WithFlags(ScriptFlags.NonmagicalTrap).Build();
        await Assert.That(scr.Flags).IsEqualTo(ScriptFlags.NonmagicalTrap);
    }

    [Test]
    public async Task WithHeaderFlags_SetsHeaderFlags()
    {
        var scr = new ScriptBuilder().WithHeaderFlags(0xDEADBEEF).Build();
        await Assert.That(scr.HeaderFlags).IsEqualTo(0xDEADBEEFu);
    }

    [Test]
    public async Task WithHeaderCounters_SetsHeaderCounters()
    {
        var scr = new ScriptBuilder().WithHeaderCounters(0x12345678).Build();
        await Assert.That(scr.HeaderCounters).IsEqualTo(0x12345678u);
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Validate_CleanScript_ReturnsNoIssues()
    {
        var issues = new ScriptBuilder().AddCondition(MakeCondition(ScriptConditionType.ObjIsDead)).Validate();

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_LongDescription_ReportsWarning()
    {
        var issues = new ScriptBuilder()
            .WithDescription(new string('A', 41))
            .AddCondition(MakeCondition(ScriptConditionType.ObjIsDead))
            .Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(ScriptValidationSeverity.Warning);
        await Assert.That(issues[0].Message.Contains("truncated", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Validate_NonAsciiDescription_ReportsWarning()
    {
        var issues = new ScriptBuilder()
            .WithDescription("Resume - caf\u00E9")
            .AddCondition(MakeCondition(ScriptConditionType.ObjIsDead))
            .Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(ScriptValidationSeverity.Warning);
        await Assert.That(issues[0].Message.Contains("non-ASCII", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Validate_UnknownAttachmentSlot_ReportsInfo()
    {
        var builder = new ScriptBuilder();
        for (var i = 0; i < 36; i++)
            builder.AddCondition(MakeCondition());

        var issues = builder.AddCondition(MakeCondition(ScriptConditionType.ObjIsDead)).Validate();

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Severity).IsEqualTo(ScriptValidationSeverity.Info);
        await Assert.That(issues[0].Message.Contains("36", StringComparison.Ordinal)).IsTrue();
    }

    // ── Construct from existing ScrFile ───────────────────────────────────────

    [Test]
    public async Task ConstructFromExisting_PreservesAllData()
    {
        var original = new ScriptBuilder()
            .AddCondition(MakeCondition(ScriptConditionType.True))
            .WithDescription("Existing")
            .WithFlags(ScriptFlags.NonmagicalTrap)
            .WithHeaderFlags(1u)
            .WithHeaderCounters(2u)
            .Build();

        var copy = new ScriptBuilder(original).Build();

        await Assert.That(copy.Entries.Count).IsEqualTo(1);
        await Assert.That(copy.Description).IsEqualTo("Existing");
        await Assert.That(copy.Flags).IsEqualTo(ScriptFlags.NonmagicalTrap);
        await Assert.That(copy.HeaderFlags).IsEqualTo(1u);
        await Assert.That(copy.HeaderCounters).IsEqualTo(2u);
    }

    // ── Round-trip through ScriptFormat ──────────────────────────────────────

    [Test]
    public async Task Build_RoundTripsThroughScriptFormat()
    {
        var scr = new ScriptBuilder()
            .AddCondition(ScriptConditionType.ObjIsDead, ScriptActionType.Attack)
            .SetConditionOperands(0, [ScriptOperand.FromValueType(ScriptValueType.GlobalVar, 12)])
            .SetActionOperands(0, [ScriptOperand.FromFocusObject(ScriptFocusObject.Attachee, 3)])
            .WithDescription("RoundTrip")
            .WithHeaderFlags(0xABu)
            .Build();

        var bytes = ScriptFormat.WriteToArray(in scr);
        var reparsed = ScriptFormat.ParseMemory(bytes);

        await Assert.That(reparsed.Entries.Count).IsEqualTo(1);
        await Assert.That(reparsed.Description).IsEqualTo("RoundTrip");
        await Assert.That(reparsed.HeaderFlags).IsEqualTo(0xABu);
        await Assert.That(reparsed.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
        await Assert.That(reparsed.Entries[0].OpTypes[0]).IsEqualTo((byte)ScriptValueType.GlobalVar);
        await Assert.That(reparsed.Entries[0].OpValues[0]).IsEqualTo(12);
        await Assert.That(reparsed.Entries[0].Action.ActionType).IsEqualTo(ScriptActionType.Attack);
        await Assert.That(reparsed.Entries[0].Action.OpTypes[0]).IsEqualTo((byte)ScriptFocusObject.Attachee);
        await Assert.That(reparsed.Entries[0].Action.OpValues[0]).IsEqualTo(3);
    }
}
