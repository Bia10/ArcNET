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
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(108);
        await Assert.That(centerY).IsEqualTo(60);
    }

    [Test]
    public async Task AdjustSpriteCenter_WallObjectWithoutFlipOnlyAppliesCeNorthSouthHotspotShift()
    {
        var artId = new ArtId(0x10000000u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Object,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(-10);
        await Assert.That(centerY).IsEqualTo(60);
    }

    [Test]
    public async Task AdjustSpriteCenter_WallObjectEastWestRotationDoesNotApplyNorthSouthShift()
    {
        var artId = new ArtId(0x10000001u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.Object,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 2,
            width: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(68);
        await Assert.That(centerY).IsEqualTo(40);
    }

    [Test]
    public async Task AdjustSpriteCenter_FloorTileSectorWallBindingAppliesCeWallHotspotShift()
    {
        var artId = new ArtId(0x00000101u);

        var (centerX, centerY) = EditorWorkspaceMapRenderSpriteSource.AdjustSpriteCenter(
            EditorMapRenderQueueItemKind.FloorTile,
            "art/wall/test.art",
            artId,
            effectiveRotationIndex: 0,
            width: 100,
            centerX: 30,
            centerY: 40
        );

        await Assert.That(centerX).IsEqualTo(-10);
        await Assert.That(centerY).IsEqualTo(60);
    }
}
