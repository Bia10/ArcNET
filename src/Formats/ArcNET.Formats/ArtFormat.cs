using System.Buffers;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// One 4-byte BGR palette entry from an ART file.
/// The fourth byte (reserved) is always 0 and is written as 0.
/// Palette index 0 is always the transparency colour.
/// </summary>
public readonly record struct ArtPaletteEntry(byte Blue, byte Green, byte Red);

/// <summary>
/// Per-frame metadata block (28 bytes) from an ART file.
/// </summary>
public readonly record struct ArtFrameHeader(
    uint Width,
    uint Height,
    uint DataSize,
    int CenterX,
    int CenterY,
    int DeltaX,
    int DeltaY
);

/// <summary>
/// One decoded sprite frame: metadata plus raw palette-index pixels.
/// Pixels are stored <b>bottom-to-top</b> (row 0 = bottom row of the sprite),
/// matching the native ART storage order from <c>artconverter.cpp</c> <c>GetValueI()</c>.
/// </summary>
public sealed class ArtFrame
{
    /// <summary>Frame metadata — width, height, offsets, deltas.</summary>
    public required ArtFrameHeader Header { get; init; }

    /// <summary>
    /// Decoded palette indices, <c>Width × Height</c> bytes.
    /// Row 0 is the <b>bottom</b> row. Index 0 = transparent.
    /// </summary>
    public required byte[] Pixels { get; init; }
}

/// <summary>
/// Parsed contents of an Arcanum sprite animation (.art) file.
/// </summary>
public sealed class ArtFile
{
    /// <summary>
    /// Flags describing the art type and rendering behaviour.
    /// </summary>
    public required ArtFlags Flags { get; init; }

    /// <summary>Animation frames per second (typically 8 or 15).</summary>
    public required uint FrameRate { get; init; }

    /// <summary>Index of the key animation frame (e.g. the connect frame of an attack).</summary>
    public required uint ActionFrame { get; init; }

    /// <summary>Number of frames per rotation direction.</summary>
    public required uint FrameCount { get; init; }

    /// <summary>Per-direction sum of compressed frame data sizes (8 slots, always stored).</summary>
    public required uint[] DataSizes { get; init; }

    /// <summary>Service / metadata array from the header (8 × uint32); preserved verbatim.</summary>
    public required uint[] Unknown0 { get; init; }

    /// <summary>Second service array from the header (8 × uint32); preserved verbatim.</summary>
    public required uint[] Unknown2 { get; init; }

    /// <summary>
    /// Up to 4 palette slots; <see langword="null"/> slot = absent (<c>PaletteId[i] == 0</c>).
    /// Slot 0 entry 0 is the transparency colour.
    /// </summary>
    public required ArtPaletteEntry[]?[] Palettes { get; init; }

    /// <summary>
    /// Original palette ID values from the file header (4 slots).
    /// A slot value of <c>0</c> means that palette slot is absent.
    /// Preserved to enable byte-exact round-trips.
    /// </summary>
    public required int[] PaletteIds { get; init; }

    /// <summary>
    /// Frames indexed as <c>Frames[rotation][frame]</c>.
    /// Rotation count = <see cref="EffectiveRotationCount"/>.
    /// </summary>
    public required ArtFrame[][] Frames { get; init; }

    /// <summary>
    /// Number of rotation directions encoded in this file.
    /// 1 when <see cref="ArtFlags.Static"/> is set; 8 otherwise.
    /// </summary>
    public int EffectiveRotationCount => (Flags & ArtFlags.Static) != 0 ? 1 : 8;
}

/// <summary>
/// Span-based parser and writer for Arcanum sprite animation (.art) files.
/// Binary layout: 132-byte header → present palettes → all frame headers → all pixel data.
/// Pixel data uses a custom RLE codec defined in <c>artconverter.cpp</c>.
/// </summary>
public sealed class ArtFormat : IFormatReader<ArtFile>, IFormatWriter<ArtFile>
{
    private const int PaletteEntries = 256;
    private const int FrameHeaderSize = 28;
    private const int RawHeaderSize = 132;

    /// <inheritdoc/>
    public static ArtFile Parse(scoped ref SpanReader reader)
    {
        // ── ArtHeader (132 bytes) ──────────────────────────────────────────
        var flags = (ArtFlags)reader.ReadUInt32();
        var frameRate = reader.ReadUInt32();
        reader.ReadUInt32(); // RotationCount in file — always 8, not used; derive from flags
        var paletteIds = new int[4];
        for (var i = 0; i < 4; i++)
            paletteIds[i] = reader.ReadInt32();

        var actionFrame = reader.ReadUInt32();
        var frameCount = reader.ReadUInt32();

        var unknown0 = new uint[8];
        for (var i = 0; i < 8; i++)
            unknown0[i] = reader.ReadUInt32();

        var dataSizes = new uint[8];
        for (var i = 0; i < 8; i++)
            dataSizes[i] = reader.ReadUInt32();

        var unknown2 = new uint[8];
        for (var i = 0; i < 8; i++)
            unknown2[i] = reader.ReadUInt32();

        // ── Palettes ──────────────────────────────────────────────────────
        var palettes = new ArtPaletteEntry[]?[4];
        for (var slot = 0; slot < 4; slot++)
        {
            if (paletteIds[slot] == 0)
            {
                palettes[slot] = null;
                continue;
            }

            var entries = new ArtPaletteEntry[PaletteEntries];
            for (var e = 0; e < PaletteEntries; e++)
            {
                var b = reader.ReadByte();
                var g = reader.ReadByte();
                var r = reader.ReadByte();
                reader.ReadByte(); // reserved — always 0
                entries[e] = new ArtPaletteEntry(b, g, r);
            }

            palettes[slot] = entries;
        }

        // ── Frame headers (all before pixel data) ─────────────────────────
        var effectiveRotations = (flags & ArtFlags.Static) != 0 ? 1 : 8;
        var totalFrames = (int)(effectiveRotations * frameCount);
        var frameHeaders = new ArtFrameHeader[totalFrames];

        for (var i = 0; i < totalFrames; i++)
        {
            var w = reader.ReadUInt32();
            var h = reader.ReadUInt32();
            var ds = reader.ReadUInt32();
            var cx = reader.ReadInt32();
            var cy = reader.ReadInt32();
            var dx = reader.ReadInt32();
            var dy = reader.ReadInt32();
            frameHeaders[i] = new ArtFrameHeader(w, h, ds, cx, cy, dx, dy);
        }

        // ── Pixel data ────────────────────────────────────────────────────
        var frames = new ArtFrame[effectiveRotations][];
        for (var r = 0; r < effectiveRotations; r++)
        {
            frames[r] = new ArtFrame[(int)frameCount];
            for (var f = 0; f < (int)frameCount; f++)
            {
                var hdr = frameHeaders[r * (int)frameCount + f];
                var pixels = DecodePixels(ref reader, hdr);
                frames[r][f] = new ArtFrame { Header = hdr, Pixels = pixels };
            }
        }

        return new ArtFile
        {
            Flags = flags,
            FrameRate = frameRate,
            ActionFrame = actionFrame,
            FrameCount = frameCount,
            DataSizes = dataSizes,
            Unknown0 = unknown0,
            Unknown2 = unknown2,
            PaletteIds = paletteIds,
            Palettes = palettes,
            Frames = frames,
        };
    }

    private static byte[] DecodePixels(ref SpanReader reader, ArtFrameHeader hdr)
    {
        var rawSize = hdr.Width * hdr.Height;

        if (hdr.DataSize == rawSize)
        {
            // Uncompressed
            return reader.ReadBytes((int)hdr.DataSize).ToArray();
        }

        return DecodeRle(ref reader, hdr.Width, hdr.Height, hdr.DataSize);
    }

    private static byte[] DecodeRle(ref SpanReader reader, uint width, uint height, uint dataSize)
    {
        var output = new byte[width * height];
        var written = 0;
        var sub = reader.Slice((int)dataSize);

        while (written < output.Length)
        {
            var ctl = sub.ReadByte();
            var count = ctl & 0x7F;

            if ((ctl & 0x80) == 0)
            {
                // Run-length: repeat fill byte `count` times
                var fill = sub.ReadByte();
                for (var i = 0; i < count; i++)
                    output[written++] = fill;
            }
            else
            {
                // Literal: copy `count` bytes verbatim
                var raw = sub.ReadBytes(count);
                raw.CopyTo(output.AsSpan(written));
                written += count;
            }
        }

        return output;
    }

    /// <inheritdoc/>
    public static ArtFile ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static ArtFile ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in ArtFile value, ref SpanWriter writer)
    {
        // Pre-encode all frame pixel data so DataSizes are known before writing headers.
        var effectiveRotations = value.EffectiveRotationCount;
        var frameCount = (int)value.FrameCount;
        var encoded = new (byte[] data, uint dataSize)[effectiveRotations * frameCount];

        for (var r = 0; r < effectiveRotations; r++)
        {
            for (var f = 0; f < frameCount; f++)
            {
                var frame = value.Frames[r][f];
                encoded[r * frameCount + f] = EncodeFrame(frame.Pixels, frame.Header.Width, frame.Header.Height);
            }
        }

        // Recompute per-direction DataSizes
        var dataSizes = new uint[8];
        for (var r = 0; r < effectiveRotations && r < 8; r++)
        {
            uint dirTotal = 0;
            for (var f = 0; f < frameCount; f++)
                dirTotal += encoded[r * frameCount + f].dataSize;

            dataSizes[r] = dirTotal;
        }

        // ── ArtHeader (132 bytes) ──────────────────────────────────────────
        writer.WriteUInt32((uint)value.Flags);
        writer.WriteUInt32(value.FrameRate);
        writer.WriteUInt32(8); // RotationCount — always 8 in file
        for (var slot = 0; slot < 4; slot++)
            writer.WriteInt32(value.Palettes[slot] is null ? 0 : value.PaletteIds[slot]);

        writer.WriteUInt32(value.ActionFrame);
        writer.WriteUInt32(value.FrameCount);

        for (var i = 0; i < 8; i++)
            writer.WriteUInt32(value.Unknown0[i]);

        for (var i = 0; i < 8; i++)
            writer.WriteUInt32(dataSizes[i]);

        for (var i = 0; i < 8; i++)
            writer.WriteUInt32(value.Unknown2[i]);

        // ── Palettes ──────────────────────────────────────────────────────
        foreach (var palette in value.Palettes)
        {
            if (palette is null)
                continue;

            foreach (var entry in palette)
            {
                writer.WriteByte(entry.Blue);
                writer.WriteByte(entry.Green);
                writer.WriteByte(entry.Red);
                writer.WriteByte(0); // reserved
            }
        }

        // ── Frame headers (all before pixel data) ─────────────────────────
        for (var r = 0; r < effectiveRotations; r++)
        {
            for (var f = 0; f < frameCount; f++)
            {
                var src = value.Frames[r][f].Header;
                var (_, chosenSize) = encoded[r * frameCount + f];

                writer.WriteUInt32(src.Width);
                writer.WriteUInt32(src.Height);
                writer.WriteUInt32(chosenSize);
                writer.WriteInt32(src.CenterX);
                writer.WriteInt32(src.CenterY);
                writer.WriteInt32(src.DeltaX);
                writer.WriteInt32(src.DeltaY);
            }
        }

        // ── Pixel data ────────────────────────────────────────────────────
        for (var r = 0; r < effectiveRotations; r++)
        {
            for (var f = 0; f < frameCount; f++)
            {
                var (data, _) = encoded[r * frameCount + f];
                writer.WriteBytes(data);
            }
        }
    }

    private static (byte[] data, uint dataSize) EncodeFrame(byte[] pixels, uint width, uint height)
    {
        var rle = EncodeRle(pixels);
        var rawSize = width * height;

        if ((uint)rle.Length >= rawSize)
            return (pixels, rawSize);

        return (rle, (uint)rle.Length);
    }

    private static byte[] EncodeRle(byte[] pixels)
    {
        var buf = new List<byte>(pixels.Length);
        var i = 0;

        while (i < pixels.Length)
        {
            // Measure run of identical bytes
            var run = 1;
            while (i + run < pixels.Length && pixels[i + run] == pixels[i] && run < 127)
                run++;

            if (run >= 2)
            {
                buf.Add((byte)run); // bit7 clear = run-length
                buf.Add(pixels[i]);
                i += run;
            }
            else
            {
                // Collect a literal run
                var litLen = 0;
                while (litLen < 127 && i + litLen < pixels.Length)
                {
                    var ahead = 1;
                    while (
                        i + litLen + ahead < pixels.Length
                        && pixels[i + litLen + ahead] == pixels[i + litLen]
                        && ahead < 3
                    )
                        ahead++;

                    if (ahead >= 3)
                        break;

                    litLen++;
                }

                if (litLen == 0)
                    litLen = 1;

                buf.Add((byte)(0x80 | litLen)); // bit7 set = literal
                for (var j = 0; j < litLen; j++)
                    buf.Add(pixels[i + j]);

                i += litLen;
            }
        }

        return [.. buf];
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in ArtFile value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in ArtFile value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
