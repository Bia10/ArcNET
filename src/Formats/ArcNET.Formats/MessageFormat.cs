namespace ArcNET.Formats;

/// <summary>A single entry from a .mes message file.</summary>
public readonly record struct MessageEntry(int Index, string Text);

/// <summary>Parser for Arcanum .mes (message) text files.</summary>
public static class MessageFormat
{
    /// <summary>Parses all valid message entries from the given lines.</summary>
    public static IReadOnlyList<MessageEntry> Parse(IEnumerable<string> lines)
    {
        var results = new List<MessageEntry>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('{'))
                continue;

            var entry = ParseLine(line);
            if (entry.HasValue)
                results.Add(entry.Value);
        }

        return results;
    }

    /// <summary>Parses all valid message entries from a file.</summary>
    public static IReadOnlyList<MessageEntry> ParseFile(string path) => Parse(File.ReadAllLines(path));

    private static MessageEntry? ParseLine(string line)
    {
        // Format: {index}{text}  — or with optional sound: {index}{sound}{text}
        // We read all braced tokens and use: first=index, last=text.
        var tokens = ReadAllBracedTokens(line.AsSpan());
        if (tokens.Count < 2)
            return null;

        if (!int.TryParse(tokens[0], out var index))
            return null;

        return new MessageEntry(index, tokens[^1]);
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
