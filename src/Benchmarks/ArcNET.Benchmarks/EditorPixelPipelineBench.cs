using ArcNET.Core.Primitives;
using ArcNET.Editor;
using ArcNET.Formats;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace ArcNET.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class EditorArtPreviewBuilderBench
{
    private ArtFile _art = null!;
    private ArtPaletteEntry[] _palette = [];
    private EditorArtPreviewOptions _options = null!;

    [Params(64, 256)]
    public int Edge { get; set; }

    [Params(EditorArtPreviewPixelFormat.Rgba32, EditorArtPreviewPixelFormat.Bgra32)]
    public EditorArtPreviewPixelFormat PixelFormat { get; set; }

    [Params(false, true)]
    public bool FlipVertically { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var pixels = new byte[checked(Edge * Edge)];
        for (var index = 0; index < pixels.Length; index++)
            pixels[index] = index % 9 == 0 ? (byte)0 : (byte)((index % 255) + 1);

        _palette = CreatePalette();
        _art = CreateArtFile(Edge, Edge, pixels, _palette);
        _options = new EditorArtPreviewOptions { PixelFormat = PixelFormat, FlipVertically = FlipVertically };
    }

    [Benchmark(Baseline = true)]
    public byte[] BuildFrame_Legacy()
    {
        var frame = _art.Frames[0][0];
        var width = checked((int)frame.Header.Width);
        var height = checked((int)frame.Header.Height);
        var pixelData = new byte[checked(width * height * 4)];
        var usePaletteAlpha = UsesExplicitPaletteAlpha(_palette);

        for (var destinationRow = 0; destinationRow < height; destinationRow++)
        {
            var sourceRow = _options.FlipVertically ? height - 1 - destinationRow : destinationRow;
            for (var column = 0; column < width; column++)
            {
                var sourceIndex = sourceRow * width + column;
                var destinationIndex = checked((destinationRow * width + column) * 4);
                WritePixelLegacy(pixelData, destinationIndex, frame.Pixels[sourceIndex], usePaletteAlpha);
            }
        }

        return pixelData;
    }

    [Benchmark]
    public byte[] BuildFrame_PackedLookup() => EditorArtPreviewBuilder.BuildFrame(_art, 0, 0, _options).PixelData;

    private static ArtPaletteEntry[] CreatePalette()
    {
        var palette = new ArtPaletteEntry[byte.MaxValue + 1];
        for (var index = 1; index < palette.Length; index++)
        {
            palette[index] = new ArtPaletteEntry(
                Blue: (byte)((index * 17) & 0xFF),
                Green: (byte)((index * 29) & 0xFF),
                Red: (byte)((index * 43) & 0xFF),
                Alpha: (byte)(32 + (index % 224))
            );
        }

        return palette;
    }

    private static ArtFile CreateArtFile(int width, int height, byte[] pixels, ArtPaletteEntry[] palette) =>
        new()
        {
            Flags = ArtFlags.Static,
            FrameRate = 8,
            ActionFrame = 0,
            FrameCount = 1,
            DataSizes = new uint[8],
            PaletteData1 = new uint[8],
            PaletteData2 = new uint[8],
            PaletteIds = [1, 0, 0, 0],
            Palettes = [palette, null, null, null],
            Frames =
            [
                [
                    new ArtFrame
                    {
                        Header = new ArtFrameHeader((uint)width, (uint)height, (uint)pixels.Length, 0, 0, 0, 0),
                        Pixels = pixels,
                    },
                ],
            ],
        };

    private void WritePixelLegacy(byte[] pixelData, int destinationIndex, byte paletteIndex, bool usePaletteAlpha)
    {
        if (paletteIndex == 0)
            return;

        var color = _palette[paletteIndex];
        var alpha = usePaletteAlpha ? color.Alpha : byte.MaxValue;
        switch (_options.PixelFormat)
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
                throw new ArgumentOutOfRangeException(nameof(_options.PixelFormat), _options.PixelFormat, null);
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
}

[MemoryDiagnoser]
[ShortRunJob]
public class EditorSpriteMirrorBench
{
    private static readonly ArtId MirroredTileArtId = new(0x000121C1u);
    private byte[] _source = [];

    [Params(78, 256)]
    public int Width { get; set; }

    [Params(40, 256)]
    public int Height { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _source = new byte[checked(Width * Height * 4)];
        for (var index = 0; index < _source.Length; index++)
            _source[index] = (byte)((index * 31) & 0xFF);
    }

    [Benchmark(Baseline = true)]
    public byte[] Flip_Legacy()
    {
        var pixelData = (byte[])_source.Clone();
        FlipPixelDataHorizontallyLegacy(pixelData, Width, Height);
        return pixelData;
    }

    [Benchmark]
    public byte[] Flip_PackedPixels()
    {
        var pixelData = (byte[])_source.Clone();
        EditorWorkspaceMapRenderSpriteSource.ApplyCeHorizontalTileMirror(
            EditorMapRenderQueueItemKind.FloorTile,
            MirroredTileArtId,
            pixelData,
            Width,
            Height
        );
        return pixelData;
    }

    private static void FlipPixelDataHorizontallyLegacy(byte[] pixelData, int width, int height)
    {
        const int bytesPerPixel = 4;
        if (width <= 1 || height <= 0)
            return;

        for (var row = 0; row < height; row++)
        {
            var rowStart = checked(row * width * bytesPerPixel);
            for (int left = 0, right = width - 1; left < right; left++, right--)
            {
                var leftIndex = rowStart + (left * bytesPerPixel);
                var rightIndex = rowStart + (right * bytesPerPixel);
                for (var channel = 0; channel < bytesPerPixel; channel++)
                    (pixelData[leftIndex + channel], pixelData[rightIndex + channel]) = (
                        pixelData[rightIndex + channel],
                        pixelData[leftIndex + channel]
                    );
            }
        }
    }
}
