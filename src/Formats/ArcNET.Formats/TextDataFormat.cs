namespace ArcNET.Formats;

/// <summary>A single key-value entry from a text data file.</summary>
/// <param name="Key">The parameter name (before the colon).</param>
/// <param name="Value">The parameter value (after the colon, trimmed).</param>
public readonly record struct TextDataEntry(string Key, string Value);

/// <summary>
/// Span-based line iterator for Arcanum text data files (mob, entity definitions).
/// These files use <c>key:value</c> lines with optional inline comments.
/// </summary>
public static class TextDataFormat
{
    /// <summary>Parses all non-empty, non-comment key-value pairs from the given lines.</summary>
    public static IReadOnlyList<TextDataEntry> Parse(IEnumerable<string> lines)
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

    /// <summary>Parses all key-value pairs from a text file.</summary>
    public static IReadOnlyList<TextDataEntry> ParseFile(string path) => Parse(File.ReadAllLines(path));

    /// <summary>Parses all key-value pairs from a UTF-8 byte buffer.</summary>
    public static IReadOnlyList<TextDataEntry> ParseMemory(ReadOnlyMemory<byte> memory) =>
        Parse(System.Text.Encoding.UTF8.GetString(memory.Span).Split('\n'));
}
