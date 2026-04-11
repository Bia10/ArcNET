using System.Text;
using ArcNET.Core;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

internal static class ValueStringBuilderEncodingBridge
{
    public static void WriteEncoded(scoped ReadOnlySpan<char> chars, Encoding encoding, ref SpanWriter writer)
    {
        if (chars.IsEmpty)
            return;

        var byteCount = encoding.GetByteCount(chars);
        using var bytes = new ValueByteBuffer(stackalloc byte[512]);
        bytes.EnsureCapacity(byteCount);
        var destination = bytes.GetWritableSpan(byteCount);
        encoding.GetBytes(chars, destination);
        bytes.AdvanceLength(byteCount);
        writer.WriteBytes(bytes.WrittenSpan);
    }
}
