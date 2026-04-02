using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum map properties (.prp) data.</summary>
public sealed class MapPropertiesData { }

/// <summary>Span-based parser and writer for Arcanum map properties (.prp) files.</summary>
public sealed class MapPropertiesFormat : IFormatReader<MapPropertiesData>, IFormatWriter<MapPropertiesData>
{
    /// <inheritdoc/>
    public static MapPropertiesData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("PRP format not yet reversed.");

    /// <inheritdoc/>
    public static MapPropertiesData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static MapPropertiesData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in MapPropertiesData value, ref SpanWriter writer) =>
        throw new NotImplementedException("PRP format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MapPropertiesData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in MapPropertiesData value, string path) =>
        File.WriteAllBytes(path, WriteToArray(in value));
}
