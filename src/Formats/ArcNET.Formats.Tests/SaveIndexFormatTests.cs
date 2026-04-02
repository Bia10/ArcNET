using System.Buffers;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="SaveIndexFormat"/>.</summary>
public sealed class SaveIndexFormatTests
{
    [Test]
    public async Task Parse_EmptyRoot_ReturnsEmptyList()
    {
        // Only an EndOfFile tag
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        w.WriteUInt32(3); // EndOfFile
        var result = SaveIndexFormat.ParseMemory(buf.WrittenSpan.ToArray().AsMemory());
        await Assert.That(result.Root.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_SingleFile_CorrectFields()
    {
        var src = new SaveIndex { Root = [new TfaiFileEntry { Name = "save.dat", Size = 1024 }] };
        var bytes = SaveIndexFormat.WriteToArray(in src);
        var back = SaveIndexFormat.ParseMemory(bytes.AsMemory());

        await Assert.That(back.Root.Count).IsEqualTo(1);
        await Assert.That(back.Root[0]).IsTypeOf<TfaiFileEntry>();
        var file = (TfaiFileEntry)back.Root[0];
        await Assert.That(file.Name).IsEqualTo("save.dat");
        await Assert.That(file.Size).IsEqualTo(1024);
    }

    [Test]
    public async Task RoundTrip_NestedDirectories_Preserved()
    {
        var src = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "maps",
                    Children =
                    [
                        new TfaiFileEntry { Name = "map1.sec", Size = 512 },
                        new TfaiFileEntry { Name = "map2.sec", Size = 256 },
                    ],
                },
                new TfaiFileEntry { Name = "info.gsi", Size = 64 },
            ],
        };

        var bytes = SaveIndexFormat.WriteToArray(in src);
        var back = SaveIndexFormat.ParseMemory(bytes.AsMemory());

        await Assert.That(back.Root.Count).IsEqualTo(2);
        var dir = (TfaiDirectoryEntry)back.Root[0];
        await Assert.That(dir.Name).IsEqualTo("maps");
        await Assert.That(dir.Children.Count).IsEqualTo(2);
        await Assert.That(((TfaiFileEntry)dir.Children[0]).Name).IsEqualTo("map1.sec");
        await Assert.That(((TfaiFileEntry)back.Root[1]).Name).IsEqualTo("info.gsi");
    }

    [Test]
    public void Parse_UnknownEntryType_ThrowsInvalidDataException()
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        w.WriteUInt32(99); // unknown type
        Assert.Throws<InvalidDataException>(() => SaveIndexFormat.ParseMemory(buf.WrittenSpan.ToArray().AsMemory()));
    }

    [Test]
    public async Task Parse_HardcodedBytes_KnownFields()
    {
        // Construct a minimal TFAI stream manually:
        // File("notes.txt", size=256), EndOfFile
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        // FILE entry
        w.WriteUInt32(0); // TfaiEntryType.File
        var nameBytes = System.Text.Encoding.ASCII.GetBytes("notes.txt");
        w.WriteInt32(nameBytes.Length);
        w.WriteBytes(nameBytes);
        w.WriteInt32(256); // size

        // EOF
        w.WriteUInt32(3); // TfaiEntryType.EndOfFile

        var result = SaveIndexFormat.ParseMemory(buf.WrittenSpan.ToArray().AsMemory());

        await Assert.That(result.Root.Count).IsEqualTo(1);
        var entry = (TfaiFileEntry)result.Root[0];
        await Assert.That(entry.Name).IsEqualTo("notes.txt");
        await Assert.That(entry.Size).IsEqualTo(256);
    }

    [Test]
    public async Task RoundTrip_DeeplyNested3Levels_Preserved()
    {
        var src = new SaveIndex
        {
            Root =
            [
                new TfaiDirectoryEntry
                {
                    Name = "a",
                    Children =
                    [
                        new TfaiDirectoryEntry
                        {
                            Name = "b",
                            Children = [new TfaiFileEntry { Name = "leaf.bin", Size = 1 }],
                        },
                    ],
                },
            ],
        };

        var bytes = SaveIndexFormat.WriteToArray(in src);
        var back = SaveIndexFormat.ParseMemory(bytes.AsMemory());

        var lvl1 = (TfaiDirectoryEntry)back.Root[0];
        var lvl2 = (TfaiDirectoryEntry)lvl1.Children[0];
        var leaf = (TfaiFileEntry)lvl2.Children[0];

        await Assert.That(lvl1.Name).IsEqualTo("a");
        await Assert.That(lvl2.Name).IsEqualTo("b");
        await Assert.That(leaf.Name).IsEqualTo("leaf.bin");
        await Assert.That(leaf.Size).IsEqualTo(1);
    }
}
