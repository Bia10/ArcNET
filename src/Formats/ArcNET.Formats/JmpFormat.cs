using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum world-map jump-point (.jmp) data.</summary>
public sealed class JmpData { }

/// <summary>Span-based parser and writer for Arcanum jump-point (.jmp) files.</summary>
public sealed class JmpFormat : IFormatReader<JmpData>, IFormatWriter<JmpData>
{
    /// <inheritdoc/>
    public static JmpData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("JMP format not yet reversed.");

    /// <inheritdoc/>
    public static JmpData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static JmpData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in JmpData value, ref SpanWriter writer) =>
        throw new NotImplementedException("JMP format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in JmpData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in JmpData value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
