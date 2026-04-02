using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>Placeholder for parsed Arcanum terrain definition (.tdf) data.</summary>
public sealed class TerrainData { }

/// <summary>Span-based parser and writer for Arcanum terrain definition (.tdf) files.</summary>
public sealed class TerrainFormat : IFormatReader<TerrainData>, IFormatWriter<TerrainData>
{
    /// <inheritdoc/>
    public static TerrainData Parse(scoped ref SpanReader reader) =>
        throw new NotImplementedException("TDF format not yet reversed.");

    /// <inheritdoc/>
    public static TerrainData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static TerrainData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static void Write(in TerrainData value, ref SpanWriter writer) =>
        throw new NotImplementedException("TDF format not yet reversed.");

    /// <inheritdoc/>
    public static byte[] WriteToArray(in TerrainData value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in TerrainData value, string path) =>
        File.WriteAllBytes(path, WriteToArray(in value));
}
