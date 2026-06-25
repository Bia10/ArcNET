using System.Runtime.InteropServices;
using ArcNET.Formats;

namespace ArcNET.Editor;

/// <summary>
/// Builds preview-ready ART projections for editor and tooling hosts.
/// </summary>
public static class EditorArtPreviewBuilder
{
    /// <summary>
    /// Builds a preview projection for every frame in <paramref name="art"/>.
    /// </summary>
    public static EditorArtPreview Build(ArtFile art, EditorArtPreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(art);

        options ??= new EditorArtPreviewOptions();
        var palette = GetPalette(art, options.PaletteSlot);
        var frames = new List<EditorArtPreviewFrame>(art.EffectiveRotationCount * checked((int)art.FrameCount));

        for (var rotationIndex = 0; rotationIndex < art.EffectiveRotationCount; rotationIndex++)
        {
            for (var frameIndex = 0; frameIndex < art.Frames[rotationIndex].Length; frameIndex++)
                frames.Add(
                    BuildFrameCore(art.Frames[rotationIndex][frameIndex], palette, options, rotationIndex, frameIndex)
                );
        }

        return new EditorArtPreview
        {
            Flags = art.Flags,
            FrameRate = art.FrameRate,
            ActionFrame = art.ActionFrame,
            RotationCount = art.EffectiveRotationCount,
            FramesPerRotation = checked((int)art.FrameCount),
            PaletteSlot = options.PaletteSlot,
            PixelFormat = options.PixelFormat,
            Frames = frames,
        };
    }

    /// <summary>
    /// Builds a preview projection for one frame from <paramref name="art"/>.
    /// </summary>
    public static EditorArtPreviewFrame BuildFrame(
        ArtFile art,
        int rotationIndex,
        int frameIndex,
        EditorArtPreviewOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(art);

        options ??= new EditorArtPreviewOptions();
        ValidateFrameIndices(art, rotationIndex, frameIndex);
        var palette = GetPalette(art, options.PaletteSlot);

        return BuildFrameCore(art.Frames[rotationIndex][frameIndex], palette, options, rotationIndex, frameIndex);
    }

    private static EditorArtPreviewFrame BuildFrameCore(
        ArtFrame frame,
        ArtPaletteEntry[] palette,
        EditorArtPreviewOptions options,
        int rotationIndex,
        int frameIndex
    )
    {
        var width = checked((int)frame.Header.Width);
        var height = checked((int)frame.Header.Height);
        var expectedPixels = checked(width * height);
        if (frame.Pixels.Length != expectedPixels)
        {
            throw new InvalidOperationException(
                $"ART frame {rotationIndex}:{frameIndex} pixel count {frame.Pixels.Length} does not match header dimensions {width}x{height}."
            );
        }

        var usePaletteAlpha = UsesExplicitPaletteAlpha(palette);
        var pixelData = new byte[checked(expectedPixels * 4)];
        if (BitConverter.IsLittleEndian && palette.Length >= byte.MaxValue + 1)
        {
            Span<uint> paletteLookup = stackalloc uint[byte.MaxValue + 1];
            PopulatePackedPaletteLookup(palette, options.PixelFormat, usePaletteAlpha, paletteLookup);
            var destinationPixels = MemoryMarshal.Cast<byte, uint>(pixelData.AsSpan());

            for (var destinationRow = 0; destinationRow < height; destinationRow++)
            {
                var sourceRow = options.FlipVertically ? height - 1 - destinationRow : destinationRow;
                var source = frame.Pixels.AsSpan(sourceRow * width, width);
                var destination = destinationPixels.Slice(destinationRow * width, width);
                ExpandPaletteIndexedRow(source, destination, paletteLookup);
            }
        }
        else
        {
            WritePixelDataPortable(frame.Pixels, pixelData, width, height, palette, options, usePaletteAlpha);
        }

        return new EditorArtPreviewFrame
        {
            RotationIndex = rotationIndex,
            FrameIndex = frameIndex,
            Header = frame.Header,
            PixelData = pixelData,
        };
    }

    private static void PopulatePackedPaletteLookup(
        ArtPaletteEntry[] palette,
        EditorArtPreviewPixelFormat pixelFormat,
        bool usePaletteAlpha,
        Span<uint> paletteLookup
    )
    {
        for (var paletteIndex = 1; paletteIndex < paletteLookup.Length; paletteIndex++)
        {
            var color = palette[paletteIndex];
            var alpha = usePaletteAlpha ? color.Alpha : byte.MaxValue;
            paletteLookup[paletteIndex] = pixelFormat switch
            {
                EditorArtPreviewPixelFormat.Rgba32 => PackLittleEndianPixel(color.Red, color.Green, color.Blue, alpha),
                EditorArtPreviewPixelFormat.Bgra32 => PackLittleEndianPixel(color.Blue, color.Green, color.Red, alpha),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(pixelFormat),
                    pixelFormat,
                    "Unsupported ART preview pixel format."
                ),
            };
        }
    }

    private static void ExpandPaletteIndexedRow(
        ReadOnlySpan<byte> source,
        Span<uint> destination,
        ReadOnlySpan<uint> paletteLookup
    )
    {
        for (var index = 0; index < source.Length; index++)
            destination[index] = paletteLookup[source[index]];
    }

    private static uint PackLittleEndianPixel(byte first, byte second, byte third, byte alpha) =>
        (uint)first | ((uint)second << 8) | ((uint)third << 16) | ((uint)alpha << 24);

    private static void WritePixelDataPortable(
        byte[] sourcePixels,
        byte[] pixelData,
        int width,
        int height,
        ArtPaletteEntry[] palette,
        EditorArtPreviewOptions options,
        bool usePaletteAlpha
    )
    {
        for (var destinationRow = 0; destinationRow < height; destinationRow++)
        {
            var sourceRow = options.FlipVertically ? height - 1 - destinationRow : destinationRow;
            for (var column = 0; column < width; column++)
            {
                var sourceIndex = sourceRow * width + column;
                var destinationIndex = checked((destinationRow * width + column) * 4);
                WritePixel(pixelData, destinationIndex, sourcePixels[sourceIndex], palette, options, usePaletteAlpha);
            }
        }
    }

    private static void WritePixel(
        byte[] pixelData,
        int destinationIndex,
        byte paletteIndex,
        ArtPaletteEntry[] palette,
        EditorArtPreviewOptions options,
        bool usePaletteAlpha
    )
    {
        if (paletteIndex == 0)
            return;

        var color = palette[paletteIndex];
        // CE light rendering treats ART pixels as palette-indexed colors plus a color-key at index 0;
        // it does not synthesize per-pixel alpha from the palette index. For preview projection we keep
        // visible pixels opaque unless the ART palette explicitly stores alpha.
        var alpha = usePaletteAlpha ? color.Alpha : byte.MaxValue;
        switch (options.PixelFormat)
        {
            case EditorArtPreviewPixelFormat.Rgba32:
                pixelData[destinationIndex] = color.Red;
                pixelData[destinationIndex + 1] = color.Green;
                pixelData[destinationIndex + 2] = color.Blue;
                pixelData[destinationIndex + 3] = alpha;
                break;
            case EditorArtPreviewPixelFormat.Bgra32:
                pixelData[destinationIndex] = color.Blue;
                pixelData[destinationIndex + 1] = color.Green;
                pixelData[destinationIndex + 2] = color.Red;
                pixelData[destinationIndex + 3] = alpha;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(options.PixelFormat),
                    options.PixelFormat,
                    "Unsupported ART preview pixel format."
                );
        }
    }

    private static bool UsesExplicitPaletteAlpha(ArtPaletteEntry[] palette)
    {
        for (var index = 1; index < palette.Length; index++)
        {
            if (palette[index].Alpha != 0)
                return true;
        }

        return false;
    }

    private static ArtPaletteEntry[] GetPalette(ArtFile art, int paletteSlot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(paletteSlot);
        if (paletteSlot >= art.Palettes.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(paletteSlot),
                paletteSlot,
                $"Palette slot must be between 0 and {art.Palettes.Length - 1}."
            );
        }

        // Many ART files only define palette slot 0. When the requested slot is encoded in
        // the ART ID but absent from the file, fall back to slot 0 — matching CE behaviour.
        // Only slot 0 being null indicates a corrupt file and warrants an exception.
        if (art.Palettes[paletteSlot] is { } requestedPalette)
            return requestedPalette;

        if (paletteSlot != 0)
            return art.Palettes[0]
                ?? throw new InvalidOperationException(
                    $"ART file does not define palette slot {paletteSlot} and the slot 0 fallback is also absent."
                );

        throw new InvalidOperationException($"ART file does not define palette slot 0.");
    }

    private static void ValidateFrameIndices(ArtFile art, int rotationIndex, int frameIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rotationIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);

        if (rotationIndex >= art.Frames.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationIndex),
                rotationIndex,
                $"Rotation index must be between 0 and {art.Frames.Length - 1}."
            );
        }

        if (frameIndex >= art.Frames[rotationIndex].Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameIndex),
                frameIndex,
                $"Frame index must be between 0 and {art.Frames[rotationIndex].Length - 1} for rotation {rotationIndex}."
            );
        }
    }
}
