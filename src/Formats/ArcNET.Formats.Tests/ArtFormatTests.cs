using System.Buffers;
using ArcNET.Core;
using ArcNET.Formats;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="ArtFormat"/>.</summary>
public sealed class ArtFormatTests
{
    private static byte[] BuildMinimalArt(
        uint flags = 0x01, // static — 1 rotation
        uint frameRate = 8,
        uint frameCount = 1,
        bool includeFrame = true
    )
    {
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        // ArtHeader — 132 bytes
        w.WriteUInt32(flags);
        w.WriteUInt32(frameRate);
        w.WriteUInt32(8); // RotationCount always 8
        for (var i = 0; i < 4; i++)
            w.WriteInt32(0); // no palettes
        w.WriteUInt32(0); // ActionFrame
        w.WriteUInt32(frameCount); // FrameCount
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0); // Unknown0
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0); // DataSizes (placeholder)
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0); // Unknown2

        // No palettes (all PaletteIds == 0)

        // Frame header (28 bytes) — 1 rotation × 1 frame
        const uint w_px = 2;
        const uint h_px = 2;
        const uint dataSize = w_px * h_px; // uncompressed
        w.WriteUInt32(w_px);
        w.WriteUInt32(h_px);
        w.WriteUInt32(dataSize);
        w.WriteInt32(0); // CenterX
        w.WriteInt32(0); // CenterY
        w.WriteInt32(0); // DeltaX
        w.WriteInt32(0); // DeltaY

        // Pixel data — 4 bytes raw
        w.WriteByte(1);
        w.WriteByte(2);
        w.WriteByte(3);
        w.WriteByte(4);

        return buf.WrittenSpan.ToArray();
    }

    [Test]
    public async Task Parse_StaticArt_OneRotation()
    {
        var bytes = BuildMinimalArt(flags: 0x01);
        var result = ArtFormat.ParseMemory(bytes);

        await Assert.That(result.EffectiveRotationCount).IsEqualTo(1);
        await Assert.That(result.Frames.Length).IsEqualTo(1);
        await Assert.That(result.Frames[0].Length).IsEqualTo(1);
    }

    [Test]
    public async Task Parse_PixelsDecoded_MatchRaw()
    {
        var bytes = BuildMinimalArt();
        var result = ArtFormat.ParseMemory(bytes);

        var pixels = result.Frames[0][0].Pixels;
        await Assert.That(pixels.Length).IsEqualTo(4);
        await Assert.That(pixels[0]).IsEqualTo((byte)1);
        await Assert.That(pixels[3]).IsEqualTo((byte)4);
    }

    [Test]
    public async Task RoundTrip_StaticArt_PixelsUnchanged()
    {
        var bytes = BuildMinimalArt();
        var src = ArtFormat.ParseMemory(bytes);
        var rewritten = ArtFormat.WriteToArray(in src);
        var back = ArtFormat.ParseMemory(rewritten);

        var srcPixels = src.Frames[0][0].Pixels;
        var backPixels = back.Frames[0][0].Pixels;

        await Assert.That(backPixels.Length).IsEqualTo(srcPixels.Length);
        await Assert.That(backPixels.SequenceEqual(srcPixels)).IsTrue();
    }

    [Test]
    public async Task Parse_NoPalettes_AllNull()
    {
        var bytes = BuildMinimalArt();
        var result = ArtFormat.ParseMemory(bytes);

        for (var i = 0; i < 4; i++)
            await Assert.That(result.Palettes[i]).IsNull();
    }

    [Test]
    public async Task EncodeRle_RunLengthCompressesRuns()
    {
        // A frame of all the same pixel should RLE compress well.
        // We write it as raw, then use the round-trip to confirm RLE is inverted.
        const uint side = 4;
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        // Header
        w.WriteUInt32(0x01); // static
        w.WriteUInt32(8);
        w.WriteUInt32(8);
        for (var i = 0; i < 4; i++)
            w.WriteInt32(0);
        w.WriteUInt32(0);
        w.WriteUInt32(1);
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0);
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0);
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0);

        // Frame header — uncompressed (DataSize == w*h)
        w.WriteUInt32(side);
        w.WriteUInt32(side);
        w.WriteUInt32(side * side); // raw
        w.WriteInt32(0);
        w.WriteInt32(0);
        w.WriteInt32(0);
        w.WriteInt32(0);

        // Pixels — all 0x7F
        for (var i = 0; i < side * side; i++)
            w.WriteByte(0x7F);

        var bytes = buf.WrittenSpan.ToArray();
        var parsed = ArtFormat.ParseMemory(bytes);
        var pixels = parsed.Frames[0][0].Pixels;

        await Assert.That(pixels.Length).IsEqualTo((int)(side * side));
        await Assert.That(pixels[0]).IsEqualTo((byte)0x7F);
        await Assert.That(pixels[^1]).IsEqualTo((byte)0x7F);

        // Round-trip through the writer — it will RLE encode
        var back = ArtFormat.ParseMemory(ArtFormat.WriteToArray(in parsed));
        await Assert.That(back.Frames[0][0].Pixels.SequenceEqual(pixels)).IsTrue();
    }

    [Test]
    public async Task Parse_TruncatedHeader_ThrowsException()
    {
        // Only 10 bytes — far too short for the 132-byte ART header
        var bytes = new byte[10];
        await Assert.That(() => ArtFormat.ParseMemory(bytes)).ThrowsException();
    }

    [Test]
    public async Task Parse_ZeroFrameCount_EmptyFrameArray()
    {
        // A valid header with frameCount=0 should parse to empty frame collections.
        var bytes = BuildMinimalArt(frameCount: 0, includeFrame: false);
        var result = ArtFormat.ParseMemory(bytes);

        await Assert.That(result.FrameCount).IsEqualTo(0u);
        await Assert.That(result.Frames[0].Length).IsEqualTo(0);
    }

    [Test]
    public async Task RoundTrip_PaletteIdsPreserved()
    {
        // Build an ART with palette slot 0 using id 7 (non-canonical).
        var buf = new ArrayBufferWriter<byte>();
        var w = new SpanWriter(buf);

        w.WriteUInt32(0x01); // flags: static
        w.WriteUInt32(8);
        w.WriteUInt32(8);
        w.WriteInt32(7); // PaletteId[0] = 7
        for (var i = 1; i < 4; i++)
            w.WriteInt32(0);
        w.WriteUInt32(0); // ActionFrame
        w.WriteUInt32(1); // FrameCount
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0);
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0);
        for (var i = 0; i < 8; i++)
            w.WriteUInt32(0);

        // Palette slot 0: 256 × 4 bytes (all zero)
        for (var i = 0; i < 256 * 4; i++)
            w.WriteByte(0);

        // Frame header (28 bytes) — 2×2 raw
        w.WriteUInt32(2);
        w.WriteUInt32(2);
        w.WriteUInt32(4);
        w.WriteInt32(0);
        w.WriteInt32(0);
        w.WriteInt32(0);
        w.WriteInt32(0);

        // 4 pixel bytes
        w.WriteByte(1);
        w.WriteByte(2);
        w.WriteByte(3);
        w.WriteByte(4);

        var bytes = buf.WrittenSpan.ToArray();
        var art = ArtFormat.ParseMemory(bytes);

        await Assert.That(art.PaletteIds[0]).IsEqualTo(7);

        // Write and reparse — the id 7 must survive
        var roundTripped = ArtFormat.ParseMemory(ArtFormat.WriteToArray(in art));
        await Assert.That(roundTripped.PaletteIds[0]).IsEqualTo(7);
    }
}
