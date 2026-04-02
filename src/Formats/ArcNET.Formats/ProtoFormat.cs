using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum prototype (.pro) data.</summary>
public sealed class ProtoData { }

/// <summary>Span-based parser and writer for Arcanum prototype (.pro) files.</summary>
public sealed class ProtoFormat : IFormatReader<ProtoData>, IFormatWriter<ProtoData>
{
    /// <inheritdoc/>
    public static ProtoData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("PRO format not yet reversed.");

    /// <inheritdoc/>
    public static ProtoData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static ProtoData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in ProtoData value, ref SpanWriter writer) =>
        throw new NotImplementedException("PRO format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in ProtoData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in ProtoData value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
