using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum art (.art) sprite animation data.</summary>
public sealed class ArtData { }

/// <summary>Span-based parser and writer for Arcanum art (.art) files.</summary>
public sealed class ArtFormat : IFormatReader<ArtData>, IFormatWriter<ArtData>
{
    /// <inheritdoc/>
    public static ArtData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("ART format not yet reversed.");

    /// <inheritdoc/>
    public static ArtData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static ArtData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in ArtData value, ref SpanWriter writer) =>
        throw new NotImplementedException("ART format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in ArtData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in ArtData value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
