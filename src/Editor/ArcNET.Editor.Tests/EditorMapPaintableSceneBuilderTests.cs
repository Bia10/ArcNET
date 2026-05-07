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

    private sealed class StubSpriteSource(int rotationIndex, int frameIndex) : IEditorMapRenderSpriteSource
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
                Width = 32,
                Height = 48,
                CenterX = 16,
                CenterY = 40,
            };
    }
}
