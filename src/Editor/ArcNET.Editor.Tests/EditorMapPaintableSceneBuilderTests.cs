using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapPaintableSceneBuilderTests
{
    [Test]
    public async Task Build_PreservesEffectiveSpriteFrameAndRotationFromMetrics()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[63 * 64] = 100u;

        var sceneRender = EditorMapFloorRenderBuilder.Build(
            new EditorMapScenePreview
            {
                MapName = "map01",
                Width = 1,
                Height = 1,
                UnpositionedSectorCount = 0,
                Sectors =
                [
                    new EditorMapSectorScenePreview
                    {
                        AssetPath = "maps/map01/sector_a.sec",
                        SectorX = 0,
                        SectorY = 0,
                        LocalX = 0,
                        LocalY = 0,
                        PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                        ObjectDensityBand = EditorMapSectorDensityBand.Low,
                        BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                        TileArtIds = tileArtIds,
                        RoofArtIds = null,
                        BlockMask = new uint[128],
                        Lights = [],
                        TileScripts = [],
                        Objects =
                        [
                            new EditorMapObjectPreview
                            {
                                ObjectId = objectId,
                                ProtoId = protoId,
                                ObjectType = ObjectType.Scenery,
                                CurrentArtId = new ArtId(0x40003000u),
                                Location = new Location(0, 63),
                                RotationPitch = 0f,
                            },
                        ],
                    },
                ],
            },
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(rotationIndex: 6, frameIndex: 9)
        );

        var objectItem = paintableScene.Items.Single(static item => item.Kind == EditorMapRenderQueueItemKind.Object);
        await Assert.That(objectItem.SpriteReference).IsNotNull();
        await Assert.That(objectItem.SpriteReference!.RotationIndex).IsEqualTo(6);
        await Assert.That(objectItem.SpriteReference.FrameIndex).IsEqualTo(9);
    }

    [Test]
    public async Task Build_ScalesSpriteBackedItemsToCompressedIsometricGrid()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[63 * 64] = 100u;

        var sceneRender = EditorMapFloorRenderBuilder.Build(
            new EditorMapScenePreview
            {
                MapName = "map01",
                Width = 1,
                Height = 1,
                UnpositionedSectorCount = 0,
                Sectors =
                [
                    new EditorMapSectorScenePreview
                    {
                        AssetPath = "maps/map01/sector_a.sec",
                        SectorX = 0,
                        SectorY = 0,
                        LocalX = 0,
                        LocalY = 0,
                        PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                        ObjectDensityBand = EditorMapSectorDensityBand.Low,
                        BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                        TileArtIds = tileArtIds,
                        RoofArtIds = null,
                        BlockMask = new uint[128],
                        Lights = [],
                        TileScripts = [],
                        Objects =
                        [
                            new EditorMapObjectPreview
                            {
                                ObjectId = objectId,
                                ProtoId = protoId,
                                ObjectType = ObjectType.Scenery,
                                CurrentArtId = new ArtId(0x40003000u),
                                Location = new Location(0, 63),
                                OffsetX = 5,
                                OffsetY = -3,
                                RotationPitch = 0f,
                            },
                        ],
                    },
                ],
            },
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(
                rotationIndex: 6,
                frameIndex: 9,
                width: 80,
                height: 40,
                centerX: 40,
                centerY: 20
            )
        );

        var floorTile = paintableScene.Items.Single(static item => item.Kind == EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(floorTile.Width).IsEqualTo(64d);
        await Assert.That(floorTile.Height).IsEqualTo(32d);
        await Assert
            .That(floorTile.SpriteSourceRect)
            .IsEqualTo(new EditorMapPaintableSceneSpriteSourceRect(0, 0, 80, 40));
        await Assert
            .That(floorTile.SpriteDestinationRect)
            .IsEqualTo(new EditorMapPaintableSceneSpriteDestinationRect(0d, 2.3999999999999773d, 64d, 32d));

        var objectItem = paintableScene.Items.Single(static item => item.Kind == EditorMapRenderQueueItemKind.Object);
        await Assert.That(objectItem.Width).IsEqualTo(64d);
        await Assert.That(objectItem.Height).IsEqualTo(32d);
        await Assert.That(objectItem.Left).IsEqualTo(-28d);
        await Assert.That(objectItem.Top).IsEqualTo(-16d);
    }

    [Test]
    public async Task Build_AssignsCeRoofAlphaLerpForFadedRoofPieces()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[63 * 64] = 100u;
        var roofArtIds = new uint[16 * 16];
        roofArtIds[15 * 16] = 0xA0001000u;

        var sceneRender = EditorMapFloorRenderBuilder.Build(
            new EditorMapScenePreview
            {
                MapName = "map01",
                Width = 1,
                Height = 1,
                UnpositionedSectorCount = 0,
                Sectors =
                [
                    new EditorMapSectorScenePreview
                    {
                        AssetPath = "maps/map01/sector_a.sec",
                        SectorX = 0,
                        SectorY = 0,
                        LocalX = 0,
                        LocalY = 0,
                        PreviewFlags = EditorMapSectorPreviewFlags.HasRoofs | EditorMapSectorPreviewFlags.Occupied,
                        ObjectDensityBand = EditorMapSectorDensityBand.None,
                        BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                        TileArtIds = tileArtIds,
                        RoofArtIds = roofArtIds,
                        BlockMask = new uint[128],
                        Lights = [],
                        TileScripts = [],
                        Objects = [],
                    },
                ],
            },
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(
                rotationIndex: 0,
                frameIndex: 0,
                width: 80,
                height: 40,
                centerX: 0,
                centerY: 0
            )
        );

        var roofItem = paintableScene.Items.Single(static item => item.Kind == EditorMapRenderQueueItemKind.Roof);
        await Assert.That(roofItem.RoofAlphaLerp).IsEqualTo(new EditorMapRoofAlphaLerp(255, 128, 128, 255));
    }

    private sealed class StubSpriteSource(
        int rotationIndex,
        int frameIndex,
        int width = 32,
        int height = 48,
        int centerX = 16,
        int centerY = 40
    ) : IEditorMapRenderSpriteSource
    {
        public EditorMapRenderSprite? Resolve(ArtId artId, EditorMapRenderSpriteRequest? request = null) => null;

        public EditorMapRenderSpriteMetrics? GetSpriteMetrics(
            ArtId artId,
            EditorMapRenderSpriteRequest? request = null
        ) =>
            new()
            {
                RotationIndex = rotationIndex,
                FrameIndex = frameIndex,
                Width = width,
                Height = height,
                CenterX = centerX,
                CenterY = centerY,
            };
    }
}
