using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of an Arcanum map properties (.prp) file.
/// Binary layout: 24 bytes (int32 ArtId + int32 Unused + uint64 LimitX + uint64 LimitY).
/// </summary>
public sealed class MapProperties
{
    /// <summary>Base terrain art identifier; looked up in <c>art/ground/ground.mes</c>.</summary>
    public required int ArtId { get; init; }

    /// <summary>Unused field; must be preserved on round-trip (typically 0).</summary>
    public required int Unused { get; init; }

    /// <summary>Tile count along the X axis; always 960 in shipping maps.</summary>
    public required ulong LimitX { get; init; }

    /// <summary>Tile count along the Y axis; always 960 in shipping maps.</summary>
    public required ulong LimitY { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum map properties (.prp) files.
/// The format is a fixed 24-byte flat struct with no magic number or version field.
/// </summary>
public sealed class MapPropertiesFormat : IFormatReader<MapProperties>, IFormatWriter<MapProperties>
{
    private const int FileSize = 24;

    /// <inheritdoc/>
    public static MapProperties Parse(scoped ref SpanReader reader)
    {
        if (reader.Remaining < FileSize)
            throw new InvalidDataException($"PRP file too short: expected {FileSize} bytes, got {reader.Remaining}.");

        return new MapProperties
        {
            ArtId = reader.ReadInt32(),
            Unused = reader.ReadInt32(),
            LimitX = reader.ReadUInt64(),
            LimitY = reader.ReadUInt64(),
        };
    }

    /// <inheritdoc/>
    public static MapProperties ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static MapProperties ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in MapProperties value, ref SpanWriter writer)
    {
        writer.WriteInt32(value.ArtId);
        writer.WriteInt32(value.Unused);
        writer.WriteUInt64(value.LimitX);
        writer.WriteUInt64(value.LimitY);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in MapProperties value)
    {
        var buf = new ArrayBufferWriter<byte>(FileSize);
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in MapProperties value, string path) =>
        File.WriteAllBytes(path, WriteToArray(in value));
}
