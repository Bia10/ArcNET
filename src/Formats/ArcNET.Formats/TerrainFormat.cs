using System.Buffers;
using System.IO.Compression;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of an Arcanum terrain definition (.tdf) file.
/// </summary>
public sealed class TerrainData
{
    /// <summary>Format version; always <c>1.2f</c> for supported files.</summary>
    public required float Version { get; init; }

    /// <summary>Base terrain type for the entire terrain sheet.</summary>
    public required TerrainType BaseTerrainType { get; init; }

    /// <summary>Width in tiles.</summary>
    public required long Width { get; init; }

    /// <summary>Height in tiles.</summary>
    public required long Height { get; init; }

    /// <summary>
    /// When <see langword="true"/>, the writer emits row-by-row zlib compression
    /// (flag <c>0x1</c> in the header). Defaults to <see langword="true"/> because
    /// compressed is always valid; set to <see langword="false"/> for uncompressed output.
    /// </summary>
    public required bool Compressed { get; init; }

    /// <summary>
    /// Terrain type per tile. Indexed as <c>Tiles[y * Width + x]</c>.
    /// Values are <see cref="TerrainType"/> cast to <see cref="ushort"/>.
    /// </summary>
    public required ushort[] Tiles { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum terrain definition (.tdf) files.
/// Header: 32-byte <c>TerrainHeader</c>; body: <c>width × height × uint16</c> tile array.
/// When <c>flags &amp; 0x1</c> the body is row-by-row zlib-compressed.
/// </summary>
public sealed class TerrainFormat : IFormatFileReader<TerrainData>, IFormatFileWriter<TerrainData>
{
    private const float SupportedVersion = 1.2f;
    private const uint CompressedFlag = 0x1;
    private const int HeaderSize = 32;

    /// <inheritdoc/>
    public static TerrainData Parse(scoped ref SpanReader reader)
    {
        // TerrainHeader — 32 bytes
        var version = reader.ReadSingle(); // 0x00 float
        if (version != SupportedVersion)
            throw new InvalidDataException($"Unsupported TDF version {version}; expected {SupportedVersion}.");

        var flags = reader.ReadUInt32(); // 0x04
        var width = reader.ReadInt64(); // 0x08
        var height = reader.ReadInt64(); // 0x10
        var baseType = (TerrainType)reader.ReadInt32(); // 0x18
        reader.ReadInt32(); // 0x1C padding — discard

        var tileCount = checked((int)(width * height));
        var tiles = new ushort[tileCount];

        if ((flags & CompressedFlag) != 0)
            ReadCompressedRows(ref reader, tiles, width, height);
        else
            ReadRawTiles(ref reader, tiles, tileCount);

        return new TerrainData
        {
            Version = version,
            BaseTerrainType = baseType,
            Width = width,
            Height = height,
            Compressed = (flags & CompressedFlag) != 0,
            Tiles = tiles,
        };
    }

    private static void ReadRawTiles(ref SpanReader reader, ushort[] tiles, int count)
    {
        reader.ReadUInt16Array(tiles.AsSpan(0, count));
    }

    private static void ReadCompressedRows(ref SpanReader reader, ushort[] tiles, long width, long height)
    {
        var rowWidthBytes = checked((int)(width * 2));
        var rowBuf = ArrayPool<byte>.Shared.Rent(rowWidthBytes);
        try
        {
            for (var row = 0; row < height; row++)
            {
                var compressedSize = reader.ReadInt32();
                var compressedSpan = reader.ReadBytes(compressedSize);

                // Rent a pooled buffer instead of ToArray() to avoid per-row heap allocation.
                var rentedCompressed = ArrayPool<byte>.Shared.Rent(compressedSize);
                try
                {
                    compressedSpan.CopyTo(rentedCompressed);
                    using var compressedStream = new MemoryStream(rentedCompressed, 0, compressedSize, writable: false);
                    using var zlib = new ZLibStream(compressedStream, CompressionMode.Decompress);

                    var totalRead = 0;
                    while (totalRead < rowWidthBytes)
                    {
                        var n = zlib.Read(rowBuf, totalRead, rowWidthBytes - totalRead);
                        if (n == 0)
                            throw new InvalidDataException(
                                $"TDF compressed row {row} decompressed to fewer bytes than expected."
                            );
                        totalRead += n;
                    }

                    var tileOffset = (int)(row * width);
                    // MemoryMarshal reinterprets the byte pairs as LE uint16 — zero copy on LE hosts.
                    var rowSpan = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, ushort>(
                        rowBuf.AsSpan(0, rowWidthBytes)
                    );
                    rowSpan.CopyTo(tiles.AsSpan(tileOffset));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedCompressed);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuf);
        }
    }

    /// <inheritdoc/>
    public static TerrainData ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static TerrainData ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in TerrainData value, ref SpanWriter writer)
    {
        // Header — 32 bytes
        writer.WriteSingle(value.Version);
        writer.WriteUInt32(value.Compressed ? CompressedFlag : 0u);
        writer.WriteInt64(value.Width);
        writer.WriteInt64(value.Height);
        writer.WriteInt32((int)value.BaseTerrainType);
        writer.WriteInt32(0); // padding

        if (value.Compressed)
        {
            // Body — row-by-row zlib compression
            var rowWidth = (int)value.Width;
            var rowWidthBytes = rowWidth * 2;
            var rowBuf = ArrayPool<byte>.Shared.Rent(rowWidthBytes);
            try
            {
                for (var row = 0; row < value.Height; row++)
                {
                    var tileOffset = (int)(row * value.Width);
                    // MemoryMarshal cast writes tiles as LE uint16 pairs — zero copy on LE hosts.
                    System
                        .Runtime.InteropServices.MemoryMarshal.Cast<ushort, byte>(
                            value.Tiles.AsSpan(tileOffset, rowWidth)
                        )
                        .CopyTo(rowBuf.AsSpan(0, rowWidthBytes));

                    using var compressedStream = new MemoryStream();
                    using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                        zlib.Write(rowBuf, 0, rowWidthBytes);

                    var compressed = compressedStream.ToArray();
                    writer.WriteInt32(compressed.Length);
                    writer.WriteBytes(compressed);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rowBuf);
                // Note: rentedCompressed is returned inside the loop body below; declared per-iteration
            }
        }
        else
        {
            // Body — raw uint16 tiles; single bulk copy via MemoryMarshal (zero copy on LE hosts)
            writer.WriteUnmanaged<ushort>(value.Tiles);
        }
    }

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
