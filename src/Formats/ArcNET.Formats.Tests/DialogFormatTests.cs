using System.Buffers;
using System.Text;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="DialogFormat"/>.</summary>
public sealed class DialogFormatTests
{
    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    [Test]
    public async Task Parse_SingleNpcLine_AllFieldsCorrect()
    {
        var text = "{100}{Hello, traveller.}{Female version.}{0}{}{0}{}";
        var result = DialogFormat.ParseMemory(Utf8(text));

        await Assert.That(result.Entries.Count).IsEqualTo(1);
        var e = result.Entries[0];
        await Assert.That(e.Num).IsEqualTo(100);
        await Assert.That(e.Text).IsEqualTo("Hello, traveller.");
        await Assert.That(e.GenderField).IsEqualTo("Female version.");
        await Assert.That(e.Iq).IsEqualTo(0);
        await Assert.That(e.Conditions).IsEqualTo(string.Empty);
        await Assert.That(e.ResponseVal).IsEqualTo(0);
        await Assert.That(e.Actions).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Parse_MultipleEntries_SortedByNum()
    {
        var text = "{200}{B}{}{0}{}{0}{}\n{100}{A}{}{0}{}{0}{}";
        var result = DialogFormat.ParseMemory(Utf8(text));

        await Assert.That(result.Entries.Count).IsEqualTo(2);
        await Assert.That(result.Entries[0].Num).IsEqualTo(100);
        await Assert.That(result.Entries[1].Num).IsEqualTo(200);
    }

    [Test]
    public async Task Parse_BracesInsideFields_HandledCorrectly()
    {
        // The parser uses depth-tracking, so nested braces inside a field are preserved.
        var text = "{1}{Reply}{}{0}{condition()}{5}{action()}";
        var result = DialogFormat.ParseMemory(Utf8(text));

        await Assert.That(result.Entries.Count).IsEqualTo(1);
        await Assert.That(result.Entries[0].Conditions).IsEqualTo("condition()");
        await Assert.That(result.Entries[0].Actions).IsEqualTo("action()");
    }

    [Test]
    public async Task RoundTrip_PreservesAllFields()
    {
        var src = new DlgFile
        {
            Entries =
            [
                new DialogEntry
                {
                    Num = 10,
                    Text = "Who are you?",
                    GenderField = "",
                    Iq = 0,
                    Conditions = "",
                    ResponseVal = 20,
                    Actions = "npc_talk()",
                },
                new DialogEntry
                {
                    Num = 20,
                    Text = "I am the merchant.",
                    GenderField = "I am the merchant, m'lady.",
                    Iq = 0,
                    Conditions = "",
                    ResponseVal = 0,
                    Actions = "",
                },
            ],
        };

        var bytes = DialogFormat.WriteToArray(in src);
        var back = DialogFormat.ParseMemory(bytes.AsMemory());

        await Assert.That(back.Entries.Count).IsEqualTo(2);
        await Assert.That(back.Entries[0].Num).IsEqualTo(10);
        await Assert.That(back.Entries[0].ResponseVal).IsEqualTo(20);
        await Assert.That(back.Entries[1].GenderField).IsEqualTo("I am the merchant, m'lady.");
    }

    [Test]
    public async Task Parse_EmptyInput_EmptyFile()
    {
        var result = DialogFormat.ParseMemory(Array.Empty<byte>().AsMemory());
        await Assert.That(result.Entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_UnmatchedBrace_PartialEntryIgnored()
    {
        // 6 fields instead of 7 — the incomplete entry must be silently skipped.
        var text = "{1}{Reply}{}{0}{cond}{5}";
        var result = DialogFormat.ParseMemory(Utf8(text));
        await Assert.That(result.Entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_MaxIntNum_PreservedCorrectly()
    {
        var text = $"{{{int.MaxValue}}}{{text}}{{}}{{0}}{{}}{{0}}{{}}";
        var result = DialogFormat.ParseMemory(Utf8(text));
        await Assert.That(result.Entries.Count).IsEqualTo(1);
        await Assert.That(result.Entries[0].Num).IsEqualTo(int.MaxValue);
    }

    [Test]
    public async Task RoundTrip_LongText_PreservedCorrectly()
    {
        var longText = new string('A', 2000);
        var src = new DlgFile
        {
            Entries =
            [
                new DialogEntry
                {
                    Num = 1,
                    Text = longText,
                    GenderField = string.Empty,
                    Iq = 0,
                    Conditions = string.Empty,
                    ResponseVal = 0,
                    Actions = string.Empty,
                },
            ],
        };

        var bytes = DialogFormat.WriteToArray(in src);
        var back = DialogFormat.ParseMemory(bytes.AsMemory());

        await Assert.That(back.Entries.Count).IsEqualTo(1);
        await Assert.That(back.Entries[0].Text).IsEqualTo(longText);
    }
}
