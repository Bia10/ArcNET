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
    public static MesFile ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<MessageFormat, MesFile>(memory);

    /// <inheritdoc/>
    public static MesFile ParseFile(string path) => FormatIo.ParseFile<MessageFormat, MesFile>(path);

    /// <inheritdoc/>
    public static void Write(in MesFile value, ref SpanWriter writer)
    {
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
        foreach (var entry in value.Entries)
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

        try
        {
            ValueStringBuilderEncodingBridge.WriteEncoded(sb.WrittenSpan, Encoding.UTF8, ref writer);
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MesFile value) => FormatIo.WriteToArray<MessageFormat, MesFile>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in MesFile value, string path) =>
        FormatIo.WriteToFile<MessageFormat, MesFile>(in value, path);

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
        var tok = new ValueTokenizer(line);

        if (!tok.TryReadNext(out var t0))
            return null;

        if (!int.TryParse(t0, out var index))
            return null;

        if (!tok.TryReadNext(out var t1))
            return null;

        // Check for optional third token (sound id between index and text).
        if (tok.TryReadNext(out var t2))
            return new MessageEntry(index, t1.ToString(), t2.ToString());

        return new MessageEntry(index, null, t1.ToString());
    }
}
