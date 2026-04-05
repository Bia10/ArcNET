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
    public async Task AddCondition_PreservesConditionType()
    {
        var scr = new ScriptBuilder().AddCondition(MakeCondition(ScriptConditionType.ObjIsDead)).Build();

        await Assert.That(scr.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
    }

    // ── RemoveCondition ───────────────────────────────────────────────────────

    [Test]
    public async Task RemoveCondition_ByIndex_Removed()
    {
        var scr = new ScriptBuilder()
            .AddCondition(MakeCondition(ScriptConditionType.True))
            .AddCondition(MakeCondition(ScriptConditionType.ObjIsDead))
            .RemoveCondition(0)
            .Build();

        await Assert.That(scr.Entries.Count).IsEqualTo(1);
        await Assert.That(scr.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
    }

    // ── ReplaceCondition ──────────────────────────────────────────────────────

    [Test]
    public async Task ReplaceCondition_UpdatesAtIndex()
    {
        var scr = new ScriptBuilder()
            .AddCondition(MakeCondition(ScriptConditionType.True))
            .ReplaceCondition(0, MakeCondition(ScriptConditionType.ObjIsDead))
            .Build();

        await Assert.That(scr.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
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
        var action = new ScriptActionData((int)ScriptActionType.Attack, new byte[8], new int[8]);
        var condition = new ScriptConditionData(
            (int)ScriptConditionType.ObjIsDead,
            new byte[8],
            new int[8],
            action,
            MakeAction()
        );

        var scr = new ScriptBuilder()
            .AddCondition(condition)
            .WithDescription("RoundTrip")
            .WithHeaderFlags(0xABu)
            .Build();

        var bytes = ScriptFormat.WriteToArray(in scr);
        var reparsed = ScriptFormat.ParseMemory(bytes);

        await Assert.That(reparsed.Entries.Count).IsEqualTo(1);
        await Assert.That(reparsed.Description).IsEqualTo("RoundTrip");
        await Assert.That(reparsed.HeaderFlags).IsEqualTo(0xABu);
        await Assert.That(reparsed.Entries[0].ConditionType).IsEqualTo(ScriptConditionType.ObjIsDead);
        await Assert.That(reparsed.Entries[0].Action.ActionType).IsEqualTo(ScriptActionType.Attack);
    }
}
