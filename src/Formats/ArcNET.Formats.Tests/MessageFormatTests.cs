using System.Text;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="MessageFormat"/>.</summary>
public sealed class MessageFormatTests
{
    private static byte[] ToUtf8(string s) => Encoding.UTF8.GetBytes(s);

    [Test]
    public async Task Parse_SingleEntry_IndexAndTextCorrect()
    {
        var bytes = ToUtf8("{100}{Hello world}\n");
        var file = MessageFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Index).IsEqualTo(100);
        await Assert.That(file.Entries[0].Text).IsEqualTo("Hello world");
    }

    [Test]
    public async Task Parse_ThreeField_SoundEntry_SoundIdAndTextPreserved()
    {
        // {index}{sound}{text} — middle token captured as SoundId, last token is Text
        var bytes = ToUtf8("{200}{snd_abc}{The actual text}\n");
        var file = MessageFormat.ParseMemory(bytes);

        await Assert.That(file.Entries[0].Index).IsEqualTo(200);
        await Assert.That(file.Entries[0].SoundId).IsEqualTo("snd_abc");
        await Assert.That(file.Entries[0].Text).IsEqualTo("The actual text");
    }

    [Test]
    public async Task Parse_EmptyFile_ZeroEntries()
    {
        var file = MessageFormat.ParseMemory(ReadOnlyMemory<byte>.Empty); // empty span
        await Assert.That(file.Entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_CommentLines_Skipped()
    {
        var bytes = ToUtf8("// This is a comment\n{1}{Text}\n");
        var file = MessageFormat.ParseMemory(bytes);
        await Assert.That(file.Entries.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Parse_MultipleEntries_AllParsed()
    {
        var text = string.Join("\n", "{1}{First}", "{2}{Second}", "{3}{Third}", "");
        var file = MessageFormat.ParseMemory(ToUtf8(text));

        await Assert.That(file.Entries.Count).IsEqualTo(3);
        await Assert.That(file.Entries[2].Index).IsEqualTo(3);
        await Assert.That(file.Entries[2].Text).IsEqualTo("Third");
    }

    [Test]
    public async Task Write_ThenParse_RoundTrips()
    {
        var src = new MesFile { Entries = [new MessageEntry(10, "Alpha"), new MessageEntry(20, "Beta")] };

        var bytes = MessageFormat.WriteToArray(in src);
        var back = MessageFormat.ParseMemory(bytes);

        await Assert.That(back.Entries.Count).IsEqualTo(2);
        await Assert.That(back.Entries[0].Index).IsEqualTo(10);
        await Assert.That(back.Entries[0].Text).IsEqualTo("Alpha");
        await Assert.That(back.Entries[1].Index).IsEqualTo(20);
        await Assert.That(back.Entries[1].Text).IsEqualTo("Beta");
    }

    [Test]
    public async Task Serialize_ProducesCorrectFormat()
    {
        var entries = new[] { new MessageEntry(5, "Test") };
        var output = MessageFormat.Serialize(entries);

        await Assert.That(output).Contains("{5}{Test}");
    }

    [Test]
    public async Task Parse_NonNumericIndex_LineSkipped()
    {
        // A line where the first token is not a number should be skipped gracefully.
        var bytes = ToUtf8("{notanumber}{text}\n{42}{valid}\n");
        var file = MessageFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Index).IsEqualTo(42);
    }

    [Test]
    public async Task Parse_MaxIndex_Preserved()
    {
        var bytes = ToUtf8($"{{{int.MaxValue}}}{{boundary text}}\n");
        var file = MessageFormat.ParseMemory(bytes);

        await Assert.That(file.Entries.Count).IsEqualTo(1);
        await Assert.That(file.Entries[0].Index).IsEqualTo(int.MaxValue);
        await Assert.That(file.Entries[0].Text).IsEqualTo("boundary text");
    }

    [Test]
    public async Task RoundTrip_EmptyTextEntry_Preserved()
    {
        var src = new MesFile { Entries = [new MessageEntry(1, string.Empty)] };
        var bytes = MessageFormat.WriteToArray(in src);
        var back = MessageFormat.ParseMemory(bytes);

        await Assert.That(back.Entries.Count).IsEqualTo(1);
        await Assert.That(back.Entries[0].Text).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RoundTrip_WithSoundId_Preserved()
    {
        var src = new MesFile { Entries = [new MessageEntry(42, "snd_hit", "You were hit!")] };
        var bytes = MessageFormat.WriteToArray(in src);
        var back = MessageFormat.ParseMemory(bytes);

        await Assert.That(back.Entries.Count).IsEqualTo(1);
        await Assert.That(back.Entries[0].Index).IsEqualTo(42);
        await Assert.That(back.Entries[0].SoundId).IsEqualTo("snd_hit");
        await Assert.That(back.Entries[0].Text).IsEqualTo("You were hit!");
    }

    [Test]
    public async Task Parse_TwoField_SoundIdIsNull()
    {
        var bytes = ToUtf8("{10}{Hello}\n");
        var file = MessageFormat.ParseMemory(bytes);

        await Assert.That(file.Entries[0].SoundId).IsNull();
    }
}
