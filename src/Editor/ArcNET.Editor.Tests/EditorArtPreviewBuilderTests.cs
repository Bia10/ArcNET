using System.Linq;
using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public class EditorArtPreviewBuilderTests
{
    private static ArtPaletteEntry[] CreatePalette(params (byte Blue, byte Green, byte Red)[] colors)
    {
        var palette = new ArtPaletteEntry[256];
        for (var index = 0; index < colors.Length; index++)
            palette[index + 1] = new(colors[index].Blue, colors[index].Green, colors[index].Red);

        return palette;
    }

    private static ArtFile CreateArtFile(
        int width,
        int height,
        byte[] pixels,
        ArtPaletteEntry[]?[]? palettes = null,
        uint frameRate = 8,
        uint actionFrame = 0
    )
    {
        palettes ??= [CreatePalette((0, 0, 255), (0, 255, 0), (255, 0, 0)), null, null, null];

        var paletteIds = new int[4];
        for (var index = 0; index < palettes.Length; index++)
            paletteIds[index] = palettes[index] is null ? 0 : index + 1;

        return new ArtFile
        {
            Flags = ArtFlags.Static,
            FrameRate = frameRate,
            ActionFrame = actionFrame,
            FrameCount = 1,
            DataSizes = new uint[8],
            PaletteData1 = new uint[8],
            PaletteData2 = new uint[8],
            PaletteIds = paletteIds,
            Palettes = palettes,
            Frames =
            [
                [
                    new ArtFrame
                    {
                        Header = new ArtFrameHeader((uint)width, (uint)height, (uint)pixels.Length, 1, 2, 3, 4),
                        Pixels = pixels,
                    },
                ],
            ],
        };
    }

    [Test]
    public async Task Build_ConvertsPaletteIndexedPixelsToRgbaAndFlipsRows()
    {
        var art = CreateArtFile(2, 2, [1, 2, 3, 0], frameRate: 8, actionFrame: 1);

        var preview = EditorArtPreviewBuilder.Build(art);
        var frame = preview.Frames[0];

        await Assert.That(preview.RotationCount).IsEqualTo(1);
        await Assert.That(preview.FramesPerRotation).IsEqualTo(1);
        await Assert.That(preview.PaletteSlot).IsEqualTo(0);
        await Assert.That(preview.PixelFormat).IsEqualTo(EditorArtPreviewPixelFormat.Rgba32);
        await Assert.That(preview.ActionFrame).IsEqualTo(1u);
        await Assert.That(preview.FrameDuration).IsEqualTo(TimeSpan.FromMilliseconds(125));
        await Assert.That(frame.RotationIndex).IsEqualTo(0);
        await Assert.That(frame.FrameIndex).IsEqualTo(0);
        await Assert.That(frame.Width).IsEqualTo(2);
        await Assert.That(frame.Height).IsEqualTo(2);
        await Assert.That(frame.Stride).IsEqualTo(8);
        await Assert
            .That(
                frame.PixelData.SequenceEqual(new byte[] { 0, 0, 255, 255, 0, 0, 0, 0, 255, 0, 0, 255, 0, 255, 0, 255 })
            )
            .IsTrue();
    }

    [Test]
    public async Task BuildFrame_FlipVerticallyFalse_PreservesNativeRowOrder()
    {
        var art = CreateArtFile(2, 2, [1, 2, 3, 0]);

        var frame = EditorArtPreviewBuilder.BuildFrame(
            art,
            0,
            0,
            new EditorArtPreviewOptions { FlipVertically = false }
        );

        await Assert
            .That(
                frame.PixelData.SequenceEqual(new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 0, 0, 0, 0 })
            )
            .IsTrue();
    }

    [Test]
    public async Task BuildFrame_UsesRequestedPaletteSlotAndBgraOutput()
    {
        var palette0 = CreatePalette((1, 2, 3));
        var palette1 = CreatePalette((10, 20, 30));
        var art = CreateArtFile(1, 1, [1], [palette0, palette1, null, null]);

        var frame = EditorArtPreviewBuilder.BuildFrame(
            art,
            0,
            0,
            new EditorArtPreviewOptions { PaletteSlot = 1, PixelFormat = EditorArtPreviewPixelFormat.Bgra32 }
        );

        await Assert.That(frame.PixelData.SequenceEqual(new byte[] { 10, 20, 30, 255 })).IsTrue();
    }

    [Test]
    public async Task Build_MissingPaletteSlot_Throws()
    {
        var art = CreateArtFile(1, 1, [1], [null, null, null, null]);

        await Assert.That(() => EditorArtPreviewBuilder.Build(art)).Throws<InvalidOperationException>();
    }
}
