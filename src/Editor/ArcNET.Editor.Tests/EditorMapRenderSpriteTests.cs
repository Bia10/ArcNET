using ArcNET.Core.Primitives;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapRenderSpriteTests
{
    [Test]
    public async Task AdjustSpriteCenter_WallObjectFlagFlipMatchesCeFrameDataOrder()
    {
        var artId = new ArtId(0x10000001u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Object,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 100,
            height: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(108);
        await Assert.That(centerY).IsEqualTo(60);
    }

    [Test]
    public async Task AdjustSpriteCenter_WallObjectWithoutFlipAppliesCeNorthSouthHotspotShift()
    {
        var artId = new ArtId(0x10000000u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Object,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 100,
            height: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(-10);
        await Assert.That(centerY).IsEqualTo(60);
    }

    [Test]
    public async Task AdjustSpriteCenter_WallObjectEastWestRotationDoesNotApplyNorthSouthShift()
    {
        var artId = new ArtId(0x10001001u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Object,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 2,
            width: 100,
            height: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(68);
        await Assert.That(centerY).IsEqualTo(40);
    }

    [Test]
    public async Task AdjustSpriteCenter_PortalObjectAppliesCeNorthSouthHotspotShiftBeforeMirror()
    {
        var artId = new ArtId(0x30000001u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Object,
            "art/portal/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 100,
            height: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(108);
        await Assert.That(centerY).IsEqualTo(60);
    }

    [Test]
    public async Task AdjustSpriteCenter_FloorTileSectorWallBindingUsesRawHotspot()
    {
        var artId = new ArtId(0x00000101u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.FloorTile,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 100,
            height: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(30);
        await Assert.That(centerY).IsEqualTo(40);
    }

    [Test]
    public async Task AdjustSpriteCenter_FloorTileAssetUsesRawArtHotspotLikeTigFrameData()
    {
        var artId = new ArtId(0x00000100u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.FloorTile,
            "art/tile/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 78,
            height: 40,
            centerX: 12,
            centerY: 39
        );

        await Assert.That(centerX).IsEqualTo(12);
        await Assert.That(centerY).IsEqualTo(39);
    }

    [Test]
    public async Task AdjustSpriteCenter_MirroredFlippableFloorTileUsesCeMirroredHotspot()
    {
        var artId = new ArtId(0x000121C1u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.FloorTile,
            "art/tile/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 78,
            height: 40,
            centerX: 12,
            centerY: 39
        );

        await Assert.That(centerX).IsEqualTo(64);
        await Assert.That(centerY).IsEqualTo(39);
    }

    [Test]
    public async Task AdjustSpriteCenter_MirroredNonFlippableFloorTileStillUsesCeMirroredHotspot()
    {
        var artId = new ArtId(0x00010001u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.FloorTile,
            "art/tile/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 78,
            height: 40,
            centerX: 12,
            centerY: 39
        );

        await Assert.That(centerX).IsEqualTo(64);
        await Assert.That(centerY).IsEqualTo(39);
    }

    [Test]
    public async Task AdjustSpriteCenter_FacadeWalkableBitUsesRawHotspot()
    {
        var artId = new ArtId(0xB5C2080Bu);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.FloorTile,
            "art/facade/Rug4-Flip.art",
            artId,
            effectiveRotationIndex: 0,
            width: 78,
            height: 40,
            centerX: 12,
            centerY: 39
        );

        await Assert.That(centerX).IsEqualTo(12);
        await Assert.That(centerY).IsEqualTo(39);
    }

    [Test]
    public async Task AdjustSpriteCenter_MirroredRoofUsesCeZeroHotspot()
    {
        var artId = new ArtId(0xA0000001u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Roof,
            "art/roof/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 78,
            height: 40,
            centerX: 12,
            centerY: 39
        );

        await Assert.That(centerX).IsEqualTo(0);
        await Assert.That(centerY).IsEqualTo(39);
    }

    [Test]
    public async Task ApplyArtIdPalette_UsesDecodedPaletteWithoutChangingOtherPreviewOptions()
    {
        var options = new EditorArtPreviewOptions
        {
            PaletteSlot = 0,
            PixelFormat = EditorArtPreviewPixelFormat.Bgra32,
            FlipVertically = true,
        };

        var effective = EditorWorkspaceMapRenderSpriteSource.ApplyArtIdPalette(options, new ArtId(0x40000020u));

        await Assert.That(effective.PaletteSlot).IsEqualTo(2);
        await Assert.That(effective.PixelFormat).IsEqualTo(EditorArtPreviewPixelFormat.Bgra32);
        await Assert.That(effective.FlipVertically).IsTrue();
    }

    [Test]
    public async Task ApplyArtIdPalette_DetectsLightTypeCodeAndKeepsCePaletteZero()
    {
        var options = new EditorArtPreviewOptions
        {
            PaletteSlot = 0,
            PixelFormat = EditorArtPreviewPixelFormat.Bgra32,
            FlipVertically = true,
        };

        // ArtId with type = Light (9)
        var artId = new ArtId(9u << 28);
        var effective = EditorWorkspaceMapRenderSpriteSource.ApplyArtIdPalette(options, artId);

        await Assert.That(effective.IsLightMask).IsTrue();
        await Assert.That(effective.PaletteSlot).IsEqualTo(0);
    }

    [Test]
    public async Task ApplyCeHorizontalTileMirror_MirroredFlippableFloorTileMatchesCePixelFlip()
    {
        var pixelData = new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 };

        EditorWorkspaceMapRenderSpriteSource.ApplyCeHorizontalTileMirror(
            EditorMapRenderQueueItemKind.FloorTile,
            new ArtId(0x000121C1u),
            pixelData,
            width: 2,
            height: 1
        );

        await Assert.That(pixelData.SequenceEqual(new byte[] { 0, 0, 255, 255, 255, 0, 0, 255 })).IsTrue();
    }

    [Test]
    public async Task ApplyCeHorizontalTileMirror_MirroredNonFlippableFloorTile_StillFlipsPixelsLikeCe()
    {
        var pixelData = new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 };

        // CE still mirrors non-flippable indoor tiles during source traversal when the mirror bit is set.
        var artId = new ArtId(0x00010001u);

        EditorWorkspaceMapRenderSpriteSource.ApplyCeHorizontalTileMirror(
            EditorMapRenderQueueItemKind.FloorTile,
            artId,
            pixelData,
            width: 2,
            height: 1
        );

        await Assert.That(pixelData.SequenceEqual(new byte[] { 0, 0, 255, 255, 255, 0, 0, 255 })).IsTrue();
    }

    [Test]
    public async Task ApplyCeHorizontalTileMirror_FacadeWalkableBit_DoesNotFlipPixels()
    {
        var pixelData = new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 };

        EditorWorkspaceMapRenderSpriteSource.ApplyCeHorizontalTileMirror(
            EditorMapRenderQueueItemKind.FloorTile,
            new ArtId(0xB5C2080Bu),
            pixelData,
            width: 2,
            height: 1
        );

        await Assert.That(pixelData.SequenceEqual(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 })).IsTrue();
    }
}
