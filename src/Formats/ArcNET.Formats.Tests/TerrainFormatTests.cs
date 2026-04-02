using System.Buffers;
using System.IO.Compression;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="TerrainFormat"/>.</summary>
public sealed class TerrainFormatTests
{
    private static byte[] BuildBytes(Action<SpanWriter> fill)
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        fill(w);
        return buf.WrittenSpan.ToArray();
    }

    private static byte[] WriteHeader(SpanWriter w, float version, uint flags, long width, long height, int baseType)
    {
        // Not used directly; helper kept for future test variants.
        return [];
    }

    private static byte[] BuildUncompressedTdf(
        float version,
        uint flags,
        long width,
        long height,
        TerrainType baseType,
        ushort[] tiles
    )
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);
        w.WriteSingle(version);
        w.WriteUInt32(flags);
        w.WriteInt64(width);
        w.WriteInt64(height);
        w.WriteInt32((int)baseType);
        w.WriteInt32(0); // padding
        foreach (var t in tiles)
            w.WriteUInt16(t);
        return buf.WrittenSpan.ToArray();
    }

    [Test]
    public async Task Parse_UncompressedSingleTile_CorrectValues()
    {
        var bytes = BuildUncompressedTdf(1.2f, 0, 1, 1, TerrainType.Grasslands, [(ushort)TerrainType.Forest]);
        var result = TerrainFormat.ParseMemory(bytes);

        await Assert.That(result.Version).IsEqualTo(1.2f);
        await Assert.That(result.Width).IsEqualTo(1L);
        await Assert.That(result.Height).IsEqualTo(1L);
        await Assert.That(result.BaseTerrainType).IsEqualTo(TerrainType.Grasslands);
        await Assert.That(result.Tiles.Length).IsEqualTo(1);
        await Assert.That(result.Tiles[0]).IsEqualTo((ushort)TerrainType.Forest);
    }

    [Test]
    public async Task Parse_BadVersion_ThrowsInvalidDataException()
    {
        var bytes = BuildUncompressedTdf(9.9f, 0, 1, 1, TerrainType.Grasslands, [(ushort)0]);
        await Assert.That(() => TerrainFormat.ParseMemory(bytes)).Throws<InvalidDataException>();
    }

    [Test]
    public async Task RoundTrip_Compressed_TilesPreserved()
    {
        // 2×2 compressed terrain
        var src = new TerrainData
        {
            Version = 1.2f,
            BaseTerrainType = TerrainType.Desert,
            Width = 2,
            Height = 2,
            Compressed = true,
            Tiles =
            [
                (ushort)TerrainType.Desert,
                (ushort)TerrainType.Mountain,
                (ushort)TerrainType.Forest,
                (ushort)TerrainType.Swamp,
            ],
        };

        var bytes = TerrainFormat.WriteToArray(in src);
        var back = TerrainFormat.ParseMemory(bytes);

        await Assert.That(back.BaseTerrainType).IsEqualTo(src.BaseTerrainType);
        await Assert.That(back.Width).IsEqualTo(2L);
        await Assert.That(back.Height).IsEqualTo(2L);
        await Assert.That(back.Tiles.SequenceEqual(src.Tiles)).IsTrue();
    }

    [Test]
    public async Task Write_AlwaysProducesCompressedFlag()
    {
        var src = new TerrainData
        {
            Version = 1.2f,
            BaseTerrainType = TerrainType.Grasslands,
            Width = 1,
            Height = 1,
            Compressed = true,
            Tiles = [0],
        };
        var bytes = TerrainFormat.WriteToArray(in src);
        // Flags at offset 4
        var flags = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        await Assert.That((flags & 0x1u)).IsEqualTo(1u);
    }

    [Test]
    public async Task Write_Uncompressed_FlagIsZero()
    {
        var src = new TerrainData
        {
            Version = 1.2f,
            BaseTerrainType = TerrainType.Grasslands,
            Width = 1,
            Height = 1,
            Compressed = false,
            Tiles = [0],
        };
        var bytes = TerrainFormat.WriteToArray(in src);
        var flags = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        await Assert.That((flags & 0x1u)).IsEqualTo(0u);
    }

    [Test]
    public async Task UncompressedAndCompressed_SameTiles_ProduceIdenticalParsedData()
    {
        ushort[] tiles = [(ushort)TerrainType.Forest, (ushort)TerrainType.Swamp];

        var rawBytes = BuildUncompressedTdf(1.2f, 0, 2, 1, TerrainType.Grasslands, tiles);
        var rawResult = TerrainFormat.ParseMemory(rawBytes);

        var compSrc = new TerrainData
        {
            Version = 1.2f,
            BaseTerrainType = TerrainType.Grasslands,
            Width = 2,
            Height = 1,
            Compressed = true,
            Tiles = tiles,
        };
        var compResult = TerrainFormat.ParseMemory(TerrainFormat.WriteToArray(in compSrc));

        await Assert.That(rawResult.Tiles.SequenceEqual(compResult.Tiles)).IsTrue();
        await Assert.That(rawResult.BaseTerrainType).IsEqualTo(compResult.BaseTerrainType);
    }
}
