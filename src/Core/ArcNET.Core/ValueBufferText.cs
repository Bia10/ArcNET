using Bia.ValueBuffers;

namespace ArcNET.Core;

/// <summary>
/// Shared text-formatting helpers built on <see cref="ValueStringBuilder"/> so callers can
/// compose compact diagnostic strings without intermediate arrays or per-item strings.
/// </summary>
internal static class ValueBufferText
{
    public static string JoinText(IEnumerable<string?> values, string separator)
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.AppendJoin(separator.AsSpan(), values);
        return sb.ToString();
    }

    public static string JoinFormatted<T, TFormatter>(IEnumerable<T> values, string separator, TFormatter formatter)
        where TFormatter : struct, IValueStringBuilderFormatter<T>
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.AppendJoin(separator.AsSpan(), values, formatter);
        return sb.ToString();
    }

    public static string JoinInt32(IEnumerable<int> values, string separator)
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        sb.AppendJoin(separator.AsSpan(), values);
        return sb.ToString();
    }

    public static string FormatHex(ReadOnlySpan<byte> bytes, bool includePrefix = false, string? suffix = null)
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        if (includePrefix)
            sb.AppendHex(bytes, "0x".AsSpan());
        else
            sb.AppendHex(bytes);
        if (!string.IsNullOrEmpty(suffix))
            sb.Append(suffix);
        return sb.ToString();
    }

    public static string FormatPrintableAscii(ReadOnlySpan<byte> bytes)
    {
        Span<char> buf = stackalloc char[256];
        var sb = new ValueStringBuilder(buf);
        foreach (var value in bytes)
        {
            if (value >= 32 && value < 127)
                sb.Append((char)value);
        }

        return sb.ToString();
    }

    public static string TruncateText(string text, int maxLen)
    {
        if (text.Length <= maxLen)
            return text;
        if (maxLen <= 3)
            return text[..maxLen];
        return string.Concat(text.AsSpan(0, maxLen - 3), "...");
    }
}
