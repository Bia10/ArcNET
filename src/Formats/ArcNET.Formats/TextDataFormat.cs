using System.Text;
using ArcNET.Core;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>A single key-value entry from a text data file.</summary>
/// <param name="Key">The parameter name (before the colon).</param>
/// <param name="Value">The parameter value (after the colon, trimmed).</param>
public readonly record struct TextDataEntry(string Key, string Value);

/// <summary>Parsed contents of an Arcanum text data file.</summary>
public sealed class TextDataFile
{
    /// <summary>All key-value pairs in document order.</summary>
    public required IReadOnlyList<TextDataEntry> Entries { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum text data files (entity definitions, etc.).
/// These files use <c>key:value</c> lines with optional inline <c>//</c> comments.
/// Implements both <see cref="IFormatFileReader{T}"/> and <see cref="IFormatFileWriter{T}"/> over
/// <see cref="TextDataFile"/>.
/// </summary>
/// <remarks>
/// Arcanum shipped as a Win32 title; its text files are encoded in Windows-1252 (code page 1252).
/// </remarks>
public sealed class TextDataFormat : IFormatFileReader<TextDataFile>, IFormatFileWriter<TextDataFile>
{
    // Arcanum text-data files use Windows-1252. Register the provider so the encoding is
    // available on non-Windows runtimes (no-op on Windows where it is built-in).
    private static readonly Encoding s_encoding;

    static TextDataFormat()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        s_encoding = Encoding.GetEncoding(1252);
    }

    /// <inheritdoc/>
    public static TextDataFile Parse(scoped ref SpanReader reader)
    {
        var text = s_encoding.GetString(reader.ReadBytes(reader.Remaining));
        return new TextDataFile { Entries = ParseLines(text.Split('\n')) };
    }

    /// <inheritdoc/>
    public static TextDataFile ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<TextDataFormat, TextDataFile>(memory);

    /// <inheritdoc/>
    public static TextDataFile ParseFile(string path) => FormatIo.ParseFile<TextDataFormat, TextDataFile>(path);

    /// <inheritdoc/>
    public static void Write(in TextDataFile value, ref SpanWriter writer)
    {
        Span<char> buf = stackalloc char[512];
        var sb = new ValueStringBuilder(buf);
        foreach (var (key, val) in value.Entries)
        {
            sb.Append(key);
            sb.Append(':');
            sb.Append(val);
            sb.Append('\n');
        }
        writer.WriteBytes(s_encoding.GetBytes(sb.ToString()));
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in TextDataFile value) =>
        FormatIo.WriteToArray<TextDataFormat, TextDataFile>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in TextDataFile value, string path) =>
        FormatIo.WriteToFile<TextDataFormat, TextDataFile>(in value, path);

    /// <summary>Parses all non-empty, non-comment key-value pairs from the given lines.</summary>
    public static IReadOnlyList<TextDataEntry> ParseLines(IEnumerable<string> lines)
    {
        var results = new List<TextDataEntry>();
        foreach (var raw in lines)
        {
            var line = raw.AsSpan();
            // Strip inline comments
            var commentIdx = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIdx >= 0)
                line = line[..commentIdx];

            line = line.Trim();
            if (line.IsEmpty)
                continue;

            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0)
                continue;

            var key = line[..colonIdx].Trim().ToString();
            var value = line[(colonIdx + 1)..].Trim().ToString();
            results.Add(new TextDataEntry(key, value));
        }

        return results;
    }
}
