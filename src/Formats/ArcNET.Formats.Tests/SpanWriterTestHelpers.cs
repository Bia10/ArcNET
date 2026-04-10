using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats.Tests;

internal static class SpanWriterTestHelpers
{
    internal static byte[] BuildBytes(Action<SpanWriter> fill)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        fill(w);
        return buf.WrittenSpan.ToArray();
    }
}
