using System.Buffers;
using System.Text;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Abstract node in a save-game archive index tree.</summary>
public abstract class TfaiEntry
{
    /// <summary>Entry name (file or directory name, no path separators).</summary>
    public required string Name { get; init; }
}

/// <summary>A leaf file entry in a TFAI index.</summary>
public sealed class TfaiFileEntry : TfaiEntry
{
    /// <summary>Number of bytes this file occupies in the companion TFAF data blob.</summary>
    public required int Size { get; init; }
}

/// <summary>A directory entry containing zero or more child entries.</summary>
public sealed class TfaiDirectoryEntry : TfaiEntry
{
    /// <summary>Child entries; may include nested directories.</summary>
    public required IReadOnlyList<TfaiEntry> Children { get; init; }
}

/// <summary>
/// Root of a parsed TFAI save-game archive index.
/// Companion TFAF data blob contains file payloads in depth-first traversal order.
/// </summary>
public sealed class SaveIndex
{
    /// <summary>Top-level entries in the index (may be files or directories).</summary>
    public required IReadOnlyList<TfaiEntry> Root { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum save-game index (.tfai) files.
/// The TFAI stream is a sequence of typed entries terminated by <see cref="TfaiEntryType.EndOfFile"/>.
/// The companion TFAF file is a raw concatenation of file payloads in depth-first order.
/// </summary>
public sealed class SaveIndexFormat : IFormatReader<SaveIndex>, IFormatWriter<SaveIndex>
{
    /// <inheritdoc/>
    public static SaveIndex Parse(scoped ref SpanReader reader)
    {
        var root = new List<TfaiEntry>();
        var stack = new Stack<List<TfaiEntry>>();
        stack.Push(root);

        while (true)
        {
            var type = (TfaiEntryType)reader.ReadUInt32();

            switch (type)
            {
                case TfaiEntryType.File:
                {
                    var name = ReadName(ref reader);
                    var size = reader.ReadInt32();
                    stack.Peek().Add(new TfaiFileEntry { Name = name, Size = size });
                    break;
                }

                case TfaiEntryType.Directory:
                {
                    var name = ReadName(ref reader);
                    var children = new List<TfaiEntry>();
                    stack.Peek().Add(new TfaiDirectoryEntry { Name = name, Children = children });
                    stack.Push(children);
                    break;
                }

                case TfaiEntryType.EndOfDirectory:
                    if (stack.Count <= 1)
                        throw new InvalidDataException("TFAI EndOfDirectory encountered with no open directory.");

                    stack.Pop();
                    break;

                case TfaiEntryType.EndOfFile:
                    return new SaveIndex { Root = root };

                default:
                    throw new InvalidDataException($"Unknown TFAI entry type: {(uint)type}");
            }
        }
    }

    private static string ReadName(ref SpanReader reader)
    {
        var length = reader.ReadInt32();
        if (length <= 0)
            return string.Empty;

        return Encoding.ASCII.GetString(reader.ReadBytes(length));
    }

    /// <inheritdoc/>
    public static SaveIndex ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static SaveIndex ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in SaveIndex value, ref SpanWriter writer)
    {
        WriteEntries(value.Root, ref writer);
        writer.WriteUInt32((uint)TfaiEntryType.EndOfFile);
    }

    private static void WriteEntries(IReadOnlyList<TfaiEntry> entries, ref SpanWriter writer)
    {
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                    writer.WriteUInt32((uint)TfaiEntryType.File);
                    WriteName(file.Name, ref writer);
                    writer.WriteInt32(file.Size);
                    break;

                case TfaiDirectoryEntry dir:
                    writer.WriteUInt32((uint)TfaiEntryType.Directory);
                    WriteName(dir.Name, ref writer);
                    WriteEntries(dir.Children, ref writer);
                    writer.WriteUInt32((uint)TfaiEntryType.EndOfDirectory);
                    break;
            }
        }
    }

    private static void WriteName(string name, ref SpanWriter writer)
    {
        var bytes = Encoding.ASCII.GetBytes(name);
        writer.WriteInt32(bytes.Length);
        writer.WriteBytes(bytes);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in SaveIndex value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in SaveIndex value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
