using System.Globalization;

namespace ArcNET.Diagnostics;

public sealed class BytePattern
{
    private readonly int _anchorIndex = -1;
    private readonly byte _anchorValue;

    private BytePattern(byte?[] bytes, string normalizedText)
    {
        Bytes = bytes;
        NormalizedText = normalizedText;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] is not { } value)
                continue;

            _anchorIndex = index;
            _anchorValue = value;
            break;
        }
    }

    public byte?[] Bytes { get; }

    public string NormalizedText { get; }

    public int Length => Bytes.Length;

    public static BytePattern Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var tokens = text.Split([' ', '\t', '\r', '\n', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            throw new InvalidOperationException("A byte pattern must contain at least one token.");

        List<byte?> bytes = [];
        foreach (var token in tokens)
        {
            if (token is "?" or "??")
            {
                bytes.Add(null);
                continue;
            }

            if (token.Length != 2)
            {
                throw new InvalidOperationException(
                    $"Invalid pattern token '{token}'. Expected one byte like '8B' or wildcard '??'."
                );
            }

            bytes.Add(byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        return new BytePattern([.. bytes], string.Join(' ', tokens.Select(static token => token.ToUpperInvariant())));
    }

    public int[] FindMatches(ReadOnlySpan<byte> haystack)
    {
        if (haystack.Length < Bytes.Length)
            return [];

        List<int> matches = [];
        if (_anchorIndex < 0)
        {
            for (var start = 0; start <= haystack.Length - Bytes.Length; start++)
            {
                if (MatchesAt(haystack, start))
                    matches.Add(start);
            }

            return [.. matches];
        }

        for (var start = 0; start <= haystack.Length - Bytes.Length; start++)
        {
            var anchorOffset = start + _anchorIndex;
            var maxAnchorOffset = haystack.Length - Bytes.Length + _anchorIndex;
            var nextAnchorOffset = haystack
                .Slice(anchorOffset, maxAnchorOffset - anchorOffset + 1)
                .IndexOf(_anchorValue);
            if (nextAnchorOffset < 0)
                break;

            start += nextAnchorOffset;
            if (MatchesAt(haystack, start))
                matches.Add(start);
        }

        return [.. matches];
    }

    private bool MatchesAt(ReadOnlySpan<byte> haystack, int start)
    {
        for (var index = 0; index < Bytes.Length; index++)
        {
            var expected = Bytes[index];
            if (expected.HasValue && haystack[start + index] != expected.Value)
                return false;
        }

        return true;
    }
}
