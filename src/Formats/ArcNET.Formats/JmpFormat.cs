using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// A single jump point entry: a source tile location, a destination map, and a destination tile location.
/// Wire size: 32 bytes.
/// </summary>
public sealed class JumpEntry
{
    /// <summary>Behaviour flags (typically 0).</summary>
    public required uint Flags { get; init; }

    /// <summary>
    /// Packed source tile: lower 32 bits = X, upper 32 bits = Y.
    /// Use <see cref="SourceX"/> / <see cref="SourceY"/> for unpacked access.
    /// </summary>
    public required long SourceLoc { get; init; }

    /// <summary>Destination map identifier.</summary>
    public required int DestinationMapId { get; init; }

    /// <summary>
    /// Packed destination tile: lower 32 bits = X, upper 32 bits = Y.
    /// Use <see cref="DestX"/> / <see cref="DestY"/> for unpacked access.
    /// </summary>
    public required long DestinationLoc { get; init; }

    /// <summary>Source tile X coordinate.</summary>
    public int SourceX => (int)(SourceLoc & 0xFFFFFFFF);

    /// <summary>Source tile Y coordinate.</summary>
    public int SourceY => (int)((SourceLoc >> 32) & 0xFFFFFFFF);

    /// <summary>Destination tile X coordinate.</summary>
    public int DestX => (int)(DestinationLoc & 0xFFFFFFFF);

    /// <summary>Destination tile Y coordinate.</summary>
    public int DestY => (int)((DestinationLoc >> 32) & 0xFFFFFFFF);
}

/// <summary>Parsed contents of an Arcanum jump-point (.jmp) file.</summary>
public sealed class JmpFile
{
    /// <summary>All jump point entries in file order.</summary>
    public required IReadOnlyList<JumpEntry> Jumps { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum jump-point (.jmp) files.
/// Format: <c>int32 count</c> followed by <c>count × 32-byte JumpPoint structs</c>.
/// </summary>
public sealed class JmpFormat : IFormatReader<JmpFile>, IFormatWriter<JmpFile>
{
    /// <inheritdoc/>
    public static JmpFile Parse(scoped ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var jumps = new JumpEntry[count];

        for (var i = 0; i < count; i++)
        {
            var flags = reader.ReadUInt32();
            reader.ReadInt32(); // padding_4 — always 0, not preserved
            var srcLoc = reader.ReadInt64();
            var dstMap = reader.ReadInt32();
            reader.ReadInt32(); // padding_14 — always 0, not preserved
            var dstLoc = reader.ReadInt64();

            jumps[i] = new JumpEntry
            {
                Flags = flags,
                SourceLoc = srcLoc,
                DestinationMapId = dstMap,
                DestinationLoc = dstLoc,
            };
        }

        return new JmpFile { Jumps = jumps };
    }

    /// <inheritdoc/>
    public static JmpFile ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static JmpFile ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in JmpFile value, ref SpanWriter writer)
    {
        writer.WriteInt32(value.Jumps.Count);

        foreach (var j in value.Jumps)
        {
            writer.WriteUInt32(j.Flags);
            writer.WriteInt32(0); // padding_4
            writer.WriteInt64(j.SourceLoc);
            writer.WriteInt32(j.DestinationMapId);
            writer.WriteInt32(0); // padding_14
            writer.WriteInt64(j.DestinationLoc);
        }
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in JmpFile value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in JmpFile value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
