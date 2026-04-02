using System.Text;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="TextDataFormat"/>.</summary>
public sealed class TextDataFormatTests
{
    private static byte[] ToUtf8(string s) => Encoding.UTF8.GetBytes(s);

    [Test]
    public async Task Parse_SingleEntry_KeyAndValueCorrect()
    {
        var bytes = ToUtf8("Name:Arcanum\n");
        var file = TextDataFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Key).IsEqualTo("Name");
        await Assert.That(file.Entries[0].Value).IsEqualTo("Arcanum");
    }

    [Test]
    public async Task Parse_InlineComment_Stripped()
    {
        var bytes = ToUtf8("Level:5 // maximum level\n");
        var file = TextDataFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Key).IsEqualTo("Level");
        await Assert.That(file.Entries[0].Value).IsEqualTo("5");
    }

    [Test]
    public async Task Parse_CommentOnlyLine_Skipped()
    {
        var bytes = ToUtf8("// this is a comment\nKey:Value\n");
        var file = TextDataFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Key).IsEqualTo("Key");
    }

    [Test]
    public async Task Parse_EmptyLines_Skipped()
    {
        var bytes = ToUtf8("\n\nA:1\n\nB:2\n\n");
        var file = TextDataFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Parse_EmptyInput_ZeroEntries()
    {
        var file = TextDataFormat.ParseMemory(ReadOnlyMemory<byte>.Empty);
        await Assert.That(file.Entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_LineWithoutColon_Skipped()
    {
        var bytes = ToUtf8("ThisHasNoColon\nValid:yes\n");
        var file = TextDataFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Key).IsEqualTo("Valid");
    }

    [Test]
    public async Task Parse_MultipleEntries_OrderPreserved()
    {
        var text = "First:1\nSecond:2\nThird:3\n";
        var file = TextDataFormat.ParseMemory(ToUtf8(text));

        await Assert.That(file.Entries.Count).IsEqualTo(3);
        await Assert.That(file.Entries[0].Key).IsEqualTo("First");
        await Assert.That(file.Entries[1].Key).IsEqualTo("Second");
        await Assert.That(file.Entries[2].Key).IsEqualTo("Third");
    }

    [Test]
    public async Task RoundTrip_MultipleEntries_Preserved()
    {
        var src = new TextDataFile
        {
            Entries = [new TextDataEntry("Alpha", "10"), new TextDataEntry("Beta", "hello world")],
        };

        var bytes = TextDataFormat.WriteToArray(in src);
        var back = TextDataFormat.ParseMemory(bytes);

        await Assert.That(back.Entries.Count).IsEqualTo(2);
        await Assert.That(back.Entries[0].Key).IsEqualTo("Alpha");
        await Assert.That(back.Entries[0].Value).IsEqualTo("10");
        await Assert.That(back.Entries[1].Key).IsEqualTo("Beta");
        await Assert.That(back.Entries[1].Value).IsEqualTo("hello world");
    }

    [Test]
    public async Task Write_EmptyFile_EmptyOutput()
    {
        var src = new TextDataFile { Entries = [] };
        var bytes = TextDataFormat.WriteToArray(in src);
        await Assert.That(bytes.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_WhiteSpaceAroundKeyValue_Trimmed()
    {
        var bytes = ToUtf8("  Key  :  Value  \n");
        var file = TextDataFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Key).IsEqualTo("Key");
        await Assert.That(file.Entries[0].Value).IsEqualTo("Value");
    }
}
