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
            .IsEqualTo(new EditorMapPaintableSceneSpriteDestinationRect(32d - (39d * 0.8d), 0d, 64d, 32d));

        var objectItem = paintableScene.Items.Single(static item => item.Kind == EditorMapRenderQueueItemKind.Object);
        await Assert.That(objectItem.Width).IsEqualTo(64d);
        await Assert.That(objectItem.Height).IsEqualTo(32d);
        // CE-correct: anchorX = tileCenterX + offsetX*scaleX = 2020 (was 1988 with wrong -40).
        // scene offsetX = -1984 (from tile minLeft=1984); anchorX_scene = 2020-1984 = 36.
        // left = anchorX_scene - centerX*sceneScaleX = 36 - 40*0.8 = 36-32 = 4.
        // anchorY = tileCenterY + offsetY*scaleY = 1024-2.4 = 1021.6.
        // minTop from tile = 1008; anchorY_scene = 1021.6-1008 = 13.6.
        // top = anchorY_scene - centerY*sceneScaleY = 13.6 - 20*0.8 = 13.6-16 = -2.4.
        await Assert.That(objectItem.Left).IsEqualTo(4d);
        await Assert.That(objectItem.Top).IsEqualTo(-2.3999999999999773d);
    }

    [Test]
    public async Task Build_IsometricFloorTileUsesCeTerrainLayoutInsteadOfSpriteHotspot()
    {
        var tile = new EditorMapFloorTileRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            MapTileX = 10,
            MapTileY = 20,
            Tile = new Location(10, 20),
            ArtId = new ArtId(0x00010203u),
            IsBlocked = false,
            HasLight = false,
            HasScript = false,
            DrawOrder = 0,
            CenterX = 100d,
            CenterY = 200d,
        };
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.Isometric,
            TileWidthPixels = 80d,
            TileHeightPixels = 40d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [tile],
            Objects = [],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.FloorTile,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Tile = tile,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(
                rotationIndex: 0,
                frameIndex: 0,
                width: 78,
                height: 40,
                centerX: 12,
                centerY: 34
            )
        );

        var floorTile = paintableScene.Items.Single();
        await Assert.That(floorTile.Left).IsEqualTo(61d);
        await Assert.That(floorTile.Top).IsEqualTo(180d);
        await Assert.That(floorTile.Width).IsEqualTo(78d);
        await Assert.That(floorTile.Height).IsEqualTo(40d);
        await Assert
            .That(floorTile.SpriteDestinationRect)
            .IsEqualTo(new EditorMapPaintableSceneSpriteDestinationRect(61d, 180d, 78d, 40d));
    }

    [Test]
    public async Task Build_IsometricFloorLightQuadrantsUseCeTerrainSubrectLayout()
    {
        var tile = new EditorMapFloorTileRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            MapTileX = 10,
            MapTileY = 20,
            Tile = new Location(10, 20),
            ArtId = new ArtId(0x00010203u),
            IsBlocked = false,
            HasLight = false,
            HasScript = false,
            DrawOrder = 0,
            CenterX = 100d,
            CenterY = 200d,
            LightDiagnostics = new EditorMapTileLightDiagnostics(
                TopLeft: 0xFF101010u,
                TopCenter: 0xFF202020u,
                TopRight: 0xFF303030u,
                MiddleLeft: 0xFF404040u,
                MiddleCenter: 0xFF505050u,
                MiddleRight: 0xFF606060u,
                BottomLeft: 0xFF707070u,
                BottomCenter: 0xFF808080u,
                BottomRight: 0xFF909090u
            ),
        };
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.Isometric,
            TileWidthPixels = 80d,
            TileHeightPixels = 40d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [tile],
            Objects = [],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.FloorTile,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Tile = tile,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(
                rotationIndex: 0,
                frameIndex: 0,
                width: 78,
                height: 40,
                centerX: 4,
                centerY: 6
            )
        );

        await Assert.That(paintableScene.Items.Count).IsEqualTo(4);

        var expected = new[]
        {
            (
                new EditorMapPaintableSceneSpriteSourceRect(0, 0, 39, 20),
                new EditorMapPaintableSceneSpriteDestinationRect(61d, 180d, 39d, 20d)
            ),
            (
                new EditorMapPaintableSceneSpriteSourceRect(39, 0, 39, 20),
                new EditorMapPaintableSceneSpriteDestinationRect(100d, 180d, 39d, 20d)
            ),
            (
                new EditorMapPaintableSceneSpriteSourceRect(0, 20, 39, 20),
                new EditorMapPaintableSceneSpriteDestinationRect(61d, 200d, 39d, 20d)
            ),
            (
                new EditorMapPaintableSceneSpriteSourceRect(39, 20, 39, 20),
                new EditorMapPaintableSceneSpriteDestinationRect(100d, 200d, 39d, 20d)
            ),
        };

        for (var index = 0; index < expected.Length; index++)
        {
            await Assert.That(paintableScene.Items[index].SpriteSourceRect).IsEqualTo(expected[index].Item1);
            await Assert.That(paintableScene.Items[index].SpriteDestinationRect).IsEqualTo(expected[index].Item2);
        }
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

    [Test]
    public async Task Build_SuppressesFallbackForDenseEmptyFloorTiles()
    {
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
                        ObjectDensityBand = EditorMapSectorDensityBand.None,
                        BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                        TileArtIds = new uint[64 * 64],
                        RoofArtIds = null,
                        BlockMask = new uint[128],
                        Lights = [],
                        TileScripts = [],
                        Objects = [],
                    },
                ],
            },
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.TopDown,
                TileWidthPixels = 32d,
                TileHeightPixels = 32d,
                IncludeEmptyTiles = true,
            }
        );

        var paintableScene = EditorMapPaintableSceneBuilder.Build(sceneRender);

        var floorTile = paintableScene.Items.First(static item => item.Kind == EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(floorTile.SpriteReference).IsNull();
        await Assert.That(floorTile.SuppressFallback).IsTrue();
    }

    [Test]
    public async Task Build_DoesNotSuppressFallbackForUnresolvedNonEmptyFloorTiles()
    {
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
                        ObjectDensityBand = EditorMapSectorDensityBand.None,
                        BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                        TileArtIds = tileArtIds,
                        RoofArtIds = null,
                        BlockMask = new uint[128],
                        Lights = [],
                        TileScripts = [],
                        Objects = [],
                    },
                ],
            },
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.TopDown,
                TileWidthPixels = 32d,
                TileHeightPixels = 32d,
            }
        );

        var paintableScene = EditorMapPaintableSceneBuilder.Build(sceneRender);

        var floorTile = paintableScene.Items.Single(static item => item.Kind == EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(floorTile.SpriteReference).IsNull();
        await Assert.That(floorTile.SuppressFallback).IsFalse();
    }

    [Test]
    public async Task Build_UsesResolvedSpriteCenterForObjectPlacementWhenSpriteMetricsDiffer()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var spriteBounds = new EditorMapObjectSpriteBounds
        {
            MaxFrameWidth = 100,
            MaxFrameHeight = 80,
            MaxFrameCenterX = 60,
            MaxFrameCenterY = 50,
        };
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x40003000u),
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            SpriteBounds = spriteBounds,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(
                rotationIndex: 0,
                frameIndex: 0,
                width: 90,
                height: 70,
                centerX: 40,
                centerY: 30
            )
        );

        var objectItem = paintableScene.Items.Single();
        await Assert.That(objectItem.Left).IsEqualTo(160d);
        await Assert.That(objectItem.Top).IsEqualTo(120d);
        await Assert.That(objectItem.Width).IsEqualTo(90d);
        await Assert.That(objectItem.Height).IsEqualTo(70d);
    }

    [Test]
    public async Task Build_UsesResolvedSpriteCenterAndDeltaOffsetsForObjectPlacement()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var spriteBounds = new EditorMapObjectSpriteBounds
        {
            MaxFrameWidth = 100,
            MaxFrameHeight = 80,
            MaxFrameCenterX = 60,
            MaxFrameCenterY = 50,
        };
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Scenery,
            CommittedRenderLayer = EditorMapCommittedRenderLayer.Scenery,
            CurrentArtId = new ArtId(0x40003000u),
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            SpriteBounds = spriteBounds,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(
                rotationIndex: 0,
                frameIndex: 0,
                width: 90,
                height: 70,
                centerX: 40,
                centerY: 30,
                deltaX: 5,
                deltaY: -10
            )
        );

        var objectItem = paintableScene.Items.Single();
        await Assert.That(objectItem.Left).IsEqualTo(160d); // 200 - 40 = 160
        await Assert.That(objectItem.Top).IsEqualTo(120d); // 150 - 30 = 120
    }

    [Test]
    public async Task Build_DoesNotForceHalfOpacityFromTranslucentFlagAlone()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 88, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Npc,
            CurrentArtId = new ArtId(0x20000000u),
            Flags = ObjectFlags.Translucent,
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );

        await Assert.That(paintableScene.Items.Single().SuggestedOpacity).IsEqualTo(1d);
    }

    [Test]
    public async Task Build_PreservesConstantAlphaColorAndBlendModeFromBlitFlags()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 89, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x40003000u),
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
            BlitFlags = unchecked((int)(BlitFlags.BlendAlphaConst | BlitFlags.BlendColorConst | BlitFlags.BlendAdd)),
            BlitAlpha = 64,
            BlitColor = 0xCC336699u,
        };
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );
        var item = paintableScene.Items.Single();

        await Assert.That(item.SuggestedOpacity).IsEqualTo(64d / 255d);
        await Assert.That(item.SuggestedTintColor).IsEqualTo(0xCC336699u);
        await Assert.That(item.BlendMode).IsEqualTo(EditorMapSpriteBlendMode.Add);
    }

    [Test]
    public async Task Build_OnlyAppliesEditorOffTintWhenRequested()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 90, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Npc,
            CurrentArtId = new ArtId(0x20002000u),
            Flags = ObjectFlags.Off,
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };

        var queue = new[]
        {
            new EditorMapRenderQueueItem
            {
                Kind = EditorMapRenderQueueItemKind.Object,
                DrawOrder = 0,
                SortKey = 0d,
                Object = renderItem,
            },
        };

        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue = queue,
        };
        var tintedSceneRender = new EditorMapFloorRenderPreview
        {
            MapName = sceneRender.MapName,
            ViewMode = sceneRender.ViewMode,
            TileWidthPixels = sceneRender.TileWidthPixels,
            TileHeightPixels = sceneRender.TileHeightPixels,
            WidthPixels = sceneRender.WidthPixels,
            HeightPixels = sceneRender.HeightPixels,
            Tiles = sceneRender.Tiles,
            Objects = sceneRender.Objects,
            Overlays = sceneRender.Overlays,
            Roofs = sceneRender.Roofs,
            RenderQueue = sceneRender.RenderQueue,
            IncludeEditorObjectStateTint = true,
        };

        var regularScene = EditorMapPaintableSceneBuilder.Build(sceneRender, spriteSource: new StubSpriteSource(0, 0));
        var tintedScene = EditorMapPaintableSceneBuilder.Build(
            tintedSceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );

        var regularItem = regularScene.Items.Single();
        var tintedItem = tintedScene.Items.Single();
        await Assert.That(regularItem.SuggestedTintColor).IsNull();
        await Assert.That(regularItem.BlendMode).IsEqualTo(EditorMapSpriteBlendMode.SourceOver);
        await Assert.That(tintedItem.SuggestedTintColor).IsEqualTo(0xFF00FF00u);
        await Assert.That(tintedItem.BlendMode).IsEqualTo(EditorMapSpriteBlendMode.Add);
    }

    [Test]
    public async Task Build_ObjectWithFrozenState_DoesNotGetLightMaskTintClassified()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 91, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x40003000u),
            Flags = ObjectFlags.Frozen,
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };

        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );

        var item = paintableScene.Items.Single();
        await Assert.That(item.UseLightMaskTint).IsFalse();
        await Assert.That(item.SuggestedTintColor).IsEqualTo(0xFF0080FFu);
        await Assert.That(item.BlendMode).IsEqualTo(EditorMapSpriteBlendMode.Add);
    }

    [Test]
    public async Task Build_ObjectWithLightAid_DoesNotClassifyPhysicalSceneryBodyAsLightMask()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 92, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x40003000u),
            BlitFlags = (int)BlitFlags.BlendAdd,
            LightAid = new ArtId(0x40004000u),
            LightColor = new Color(0xFF, 0xAA, 0x33),
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };

        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );

        var item = paintableScene.Items.Single();
        await Assert.That(item.BlendMode).IsEqualTo(EditorMapSpriteBlendMode.Add);
        await Assert.That(item.UseLightMaskTint).IsFalse();
    }

    [Test]
    public async Task Build_PrimaryLightArtObject_UsesPackedLightTintForVisibleMask()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 94, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var renderItem = new EditorMapObjectRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x90001000u),
            LightColor = new Color(0xEE, 0xCC, 0x66),
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            IsTileGridSnapped = false,
            Rotation = 0f,
            RotationPitch = 0f,
        };

        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [renderItem],
            Overlays = [],
            Roofs = [],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.Object,
                    DrawOrder = 0,
                    SortKey = 0d,
                    Object = renderItem,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );

        var item = paintableScene.Items.Single();
        await Assert.That(item.UseLightMaskTint).IsTrue();
        await Assert.That(item.SuggestedTintColor).IsEqualTo(0xFFEECC66u);
    }

    [Test]
    public async Task Build_AuxiliaryExplicitlyMarkedAsLightMask_PreservesTintClassification()
    {
        var auxiliary = new EditorMapObjectAuxiliaryRenderItem
        {
            SectorAssetPath = "maps/map01/sector_a.sec",
            ParentObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 93, Guid.NewGuid()),
            ParentObjectType = ObjectType.Scenery,
            CommittedRenderLayer = EditorMapCommittedRenderLayer.Scenery,
            ArtId = new ArtId(0x40005000u),
            Layer = EditorMapObjectAuxiliaryRenderLayer.OverlayFore,
            MapTileX = 0,
            MapTileY = 0,
            Tile = new Location(0, 0),
            DrawOrder = 0,
            AnchorX = 200d,
            AnchorY = 150d,
            UseLightMaskTint = true,
            SuggestedTintColor = 0xFFFFAA33u,
            RotationIndex = 0,
            ScalePercent = 100,
            IsShrunk = false,
            BlendMode = EditorMapSpriteBlendMode.Add,
            IsRoofCovered = false,
        };

        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 400d,
            HeightPixels = 300d,
            Tiles = [],
            Objects = [],
            Overlays = [],
            Roofs = [],
            ObjectAuxiliaryItems = [auxiliary],
            RenderQueue =
            [
                new EditorMapRenderQueueItem
                {
                    Kind = EditorMapRenderQueueItemKind.ObjectAuxiliary,
                    DrawOrder = 0,
                    SortKey = 0d,
                    ObjectAuxiliaryItem = auxiliary,
                },
            ],
        };

        var paintableScene = EditorMapPaintableSceneBuilder.Build(
            sceneRender,
            spriteSource: new StubSpriteSource(0, 0)
        );

        var item = paintableScene.Items.Single();
        await Assert.That(item.BlendMode).IsEqualTo(EditorMapSpriteBlendMode.Add);
        await Assert.That(item.UseLightMaskTint).IsTrue();
        await Assert.That(item.SuggestedTintColor).IsEqualTo(0xFFFFAA33u);
    }

    private sealed class StubSpriteSource(
        int rotationIndex,
        int frameIndex,
        int width = 32,
        int height = 48,
        int centerX = 16,
        int centerY = 40,
        int deltaX = 0,
        int deltaY = 0
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
                DeltaX = deltaX,
                DeltaY = deltaY,
            };
    }
}
