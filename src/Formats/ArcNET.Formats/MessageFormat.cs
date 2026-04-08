using System.Buffers;
using System.Text;
using ArcNET.Core;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>A single entry from a .mes message file.</summary>
/// <param name="Index">Integer entry key.</param>
/// <param name="SoundId">
/// Optional sound-effect identifier token from 3-token lines (<c>{index}{sound}{text}</c>).
/// <see langword="null"/> when the entry has no sound field.
/// </param>
/// <param name="Text">Displayed text (always the last brace-delimited token).</param>
public readonly record struct MessageEntry(int Index, string? SoundId, string Text)
{
    /// <summary>Creates an entry with no sound identifier.</summary>
    public MessageEntry(int index, string text)
        : this(index, null, text) { }
}

/// <summary>Parsed contents of an Arcanum message (.mes) file.</summary>
public sealed class MesFile
{
    /// <summary>All entries, in the order they appear in the file.</summary>
    public required IReadOnlyList<MessageEntry> Entries { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum message (.mes) text files.
/// The format is plain text: each line with at least two brace-delimited tokens is an entry.
/// Format: <c>{index}{text}</c> — or with optional sound field: <c>{index}{sound}{text}</c>.
/// Implements <see cref="IFormatFileReader{T}"/> and <see cref="IFormatFileWriter{T}"/> using
/// UTF-8 encoding for <see cref="SpanReader"/> / <see cref="SpanWriter"/> integration.
/// </summary>
public sealed class MessageFormat : IFormatFileReader<MesFile>, IFormatFileWriter<MesFile>
{
    /// <inheritdoc/>
    public static MesFile Parse(scoped ref SpanReader reader)
    {
        var text = Encoding.UTF8.GetString(reader.ReadBytes(reader.Remaining));
        return new MesFile { Entries = ParseText(text.AsSpan()) };
    }

    /// <inheritdoc/>
    public static MesFile ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static MesFile ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in MesFile value, ref SpanWriter writer)
    {
        var bytes = Encoding.UTF8.GetBytes(Serialize(value.Entries));
        writer.WriteBytes(bytes);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MesFile value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in MesFile value, string path) => File.WriteAllBytes(path, WriteToArray(in value));

    // ── Legacy overloads kept for backward compatibility ──────────────────────

    /// <summary>Parses all valid message entries from the given lines.</summary>
    public static IReadOnlyList<MessageEntry> ParseLines(IEnumerable<string> lines)
    {
        var results = new List<MessageEntry>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('{'))
                continue;

            var entry = ParseLine(line.AsSpan());
            if (entry.HasValue)
                results.Add(entry.Value);
        }

        return results;
    }

    // Span-based parse: uses EnumerateLines to avoid string[] allocation from Split.
    private static IReadOnlyList<MessageEntry> ParseText(ReadOnlySpan<char> text)
    {
        var results = new List<MessageEntry>();
        foreach (var line in text.EnumerateLines())
        {
            if (line.IsEmpty || line.IsWhiteSpace() || line[0] != '{')
                continue;

            var entry = ParseLine(line);
            if (entry.HasValue)
                results.Add(entry.Value);
        }

        return results;
    }

    /// <summary>Serializes <paramref name="entries"/> to .mes text format.</summary>
    public static string Serialize(IEnumerable<MessageEntry> entries)
    {
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
        foreach (var entry in entries)
        {
            sb.Append('{');
            sb.Append(entry.Index);
            sb.Append('}');
            if (entry.SoundId != null)
            {
                sb.Append('{');
                sb.Append(entry.SoundId);
                sb.Append('}');
            }
            sb.Append('{');
            sb.Append(entry.Text);
            sb.Append('}');
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static MessageEntry? ParseLine(ReadOnlySpan<char> line)
    {
        var tokens = ReadAllBracedTokens(line);
        if (tokens.Count < 2)
            return null;

        if (!int.TryParse(tokens[0], out var index))
            return null;

        // 3-token line: {index}{sound}{text} — middle token is the sound identifier
        if (tokens.Count >= 3)
            return new MessageEntry(index, tokens[1], tokens[^1]);

        return new MessageEntry(index, null, tokens[^1]);
    }

    private static List<string> ReadAllBracedTokens(ReadOnlySpan<char> span)
    {
        var tokens = new List<string>(3);
        var pos = 0;

        while (pos < span.Length)
        {
            if (span[pos] != '{')
            {
                pos++;
                continue;
            }

            var closeIdx = span[pos..].IndexOf('}');
            if (closeIdx < 0)
                break;

            tokens.Add(span[(pos + 1)..(pos + closeIdx)].ToString());
            pos += closeIdx + 1;
        }

        return tokens;
    }
}
