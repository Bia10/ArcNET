using System.Buffers;
using System.Text;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="ScriptFormat"/>.</summary>
public sealed class ScriptFormatTests
{
    private static byte[] BuildScrBytes(
        uint hdrFlags = 0,
        uint hdrCounters = 0,
        string description = "",
        ScriptFlags flags = ScriptFlags.None,
        IReadOnlyList<ScriptConditionData>? entries = null
    )
    {
        entries ??= Array.Empty<ScriptConditionData>();

        var src = new ScrFile
        {
            HeaderFlags = hdrFlags,
            HeaderCounters = hdrCounters,
            Description = description,
            Flags = flags,
            Entries = entries,
        };
        return ScriptFormat.WriteToArray(in src);
    }

    [Test]
    public async Task Parse_EmptyScript_ZeroEntries()
    {
        var bytes = BuildScrBytes();
        var result = ScriptFormat.ParseMemory(bytes);

        await Assert.That(result.Entries.Count).IsEqualTo(0);
        await Assert.That(result.Description).IsEqualTo(string.Empty);
        await Assert.That(result.Flags).IsEqualTo(ScriptFlags.None);
    }

    [Test]
    public async Task Parse_HeaderFields_Correct()
    {
        var bytes = BuildScrBytes(hdrFlags: 0xDEAD, hdrCounters: 0xBEEF, description: "TestScript");
        var result = ScriptFormat.ParseMemory(bytes);

        await Assert.That(result.HeaderFlags).IsEqualTo(0xDEADu);
        await Assert.That(result.HeaderCounters).IsEqualTo(0xBEEFu);
        await Assert.That(result.Description).IsEqualTo("TestScript");
    }

    [Test]
    public async Task RoundTrip_WithOneCondition_Preserved()
    {
        var action = new ScriptActionData(7, new byte[8], new int[8]);
        var cond = new ScriptConditionData(3, new byte[8], new int[8], action, action);

        var src = new ScrFile
        {
            HeaderFlags = 1,
            HeaderCounters = 2,
            Description = "MyScript",
            Flags = ScriptFlags.NonmagicalTrap | ScriptFlags.AutoRemoving,
            Entries = [cond],
        };

        var bytes = ScriptFormat.WriteToArray(in src);
        var back = ScriptFormat.ParseMemory(bytes);

        await Assert.That(back.HeaderFlags).IsEqualTo(src.HeaderFlags);
        await Assert.That(back.HeaderCounters).IsEqualTo(src.HeaderCounters);
        await Assert.That(back.Description).IsEqualTo(src.Description);
        await Assert.That(back.Flags).IsEqualTo(src.Flags);
        await Assert.That(back.Entries.Count).IsEqualTo(1);
        await Assert.That(back.Entries[0].Type).IsEqualTo(cond.Type);
        await Assert.That(back.Entries[0].Action.Type).IsEqualTo(action.Type);
    }

    [Test]
    public async Task Write_ConditionIs132Bytes()
    {
        // Each ScriptCondition must be exactly 0x84 = 132 bytes.
        var action = new ScriptActionData(0, new byte[8], new int[8]);
        var cond = new ScriptConditionData(0, new byte[8], new int[8], action, action);

        var src = new ScrFile
        {
            HeaderFlags = 0,
            HeaderCounters = 0,
            Description = "",
            Flags = ScriptFlags.None,
            Entries = [cond],
        };

        var bytes = ScriptFormat.WriteToArray(in src);
        // Header = 8, body metadata = 40+4+4+4+4 = 56, so first byte of entries is at 8+56=64
        // Condition = 132 bytes; total = 64 + 132 = 196
        await Assert.That(bytes.Length).IsEqualTo(8 + 40 + 4 + 4 + 4 + 4 + 132);
    }

    [Test]
    public async Task Parse_TruncatedInput_ThrowsException()
    {
        // Too short to even contain the ScriptHeader (8 bytes)
        var bytes = new byte[4];
        await Assert.That(() => ScriptFormat.ParseMemory(bytes)).ThrowsException();
    }

    [Test]
    public async Task Parse_AllScriptFlagCombinations_RoundTrip()
    {
        var allFlags =
            ScriptFlags.NonmagicalTrap
            | ScriptFlags.MagicalTrap
            | ScriptFlags.AutoRemoving
            | ScriptFlags.DeathSpeech
            | ScriptFlags.SurrenderSpeech
            | ScriptFlags.RadiusTwo
            | ScriptFlags.RadiusThree
            | ScriptFlags.RadiusFive
            | ScriptFlags.TeleportTrigger;

        var src = new ScrFile
        {
            HeaderFlags = 0,
            HeaderCounters = 0,
            Description = "",
            Flags = allFlags,
            Entries = [],
        };

        var bytes = ScriptFormat.WriteToArray(in src);
        var back = ScriptFormat.ParseMemory(bytes);

        await Assert.That(back.Flags).IsEqualTo(allFlags);
    }
}
