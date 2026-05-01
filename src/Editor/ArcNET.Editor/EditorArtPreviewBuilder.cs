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

        var pixelData = new byte[checked(expectedPixels * 4)];
        for (var destinationRow = 0; destinationRow < height; destinationRow++)
        {
            var sourceRow = options.FlipVertically ? height - 1 - destinationRow : destinationRow;
            for (var column = 0; column < width; column++)
            {
                var sourceIndex = sourceRow * width + column;
                var destinationIndex = checked((destinationRow * width + column) * 4);
                WritePixel(pixelData, destinationIndex, frame.Pixels[sourceIndex], palette, options.PixelFormat);
            }
        }

        return new EditorArtPreviewFrame
        {
            RotationIndex = rotationIndex,
            FrameIndex = frameIndex,
            Header = frame.Header,
            PixelData = pixelData,
        };
    }

    private static void WritePixel(
        byte[] pixelData,
        int destinationIndex,
        byte paletteIndex,
        ArtPaletteEntry[] palette,
        EditorArtPreviewPixelFormat pixelFormat
    )
    {
        if (paletteIndex == 0)
            return;

        var color = palette[paletteIndex];
        switch (pixelFormat)
        {
            case EditorArtPreviewPixelFormat.Rgba32:
                pixelData[destinationIndex] = color.Red;
                pixelData[destinationIndex + 1] = color.Green;
                pixelData[destinationIndex + 2] = color.Blue;
                pixelData[destinationIndex + 3] = byte.MaxValue;
                break;
            case EditorArtPreviewPixelFormat.Bgra32:
                pixelData[destinationIndex] = color.Blue;
                pixelData[destinationIndex + 1] = color.Green;
                pixelData[destinationIndex + 2] = color.Red;
                pixelData[destinationIndex + 3] = byte.MaxValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(pixelFormat),
                    pixelFormat,
                    "Unsupported ART preview pixel format."
                );
        }
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

        return art.Palettes[paletteSlot]
            ?? throw new InvalidOperationException($"ART file does not define palette slot {paletteSlot}.");
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
