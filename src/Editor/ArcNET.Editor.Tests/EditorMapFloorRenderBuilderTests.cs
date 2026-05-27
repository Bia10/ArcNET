using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapFloorRenderBuilderTests
{
    [Test]
    public async Task Build_DoesNotOverflowDrawOrderForLargeSparseIsometricMap()
    {
        const int localSectorX = 19_999;
        const int localSectorY = 19_999;

        var scenePreview = new EditorMapScenePreview
        {
            MapName = "map01",
            Width = localSectorX + 1,
            Height = localSectorY + 1,
            UnpositionedSectorCount = 0,
            Sectors =
            [
                new EditorMapSectorScenePreview
                {
                    AssetPath = "maps/map01/999.sec",
                    SectorX = localSectorX,
                    SectorY = localSectorY,
                    LocalX = localSectorX,
                    LocalY = localSectorY,
                    PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                    ObjectDensityBand = EditorMapSectorDensityBand.None,
                    BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                    TileArtIds = CreateSparseMapTileArtIds(),
                    BlockMask = new uint[128],
                    Lights = [],
                    TileScripts = [],
                    Objects = [],
                },
            ],
        };

        var preview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            EditorMapFloorRenderRequest.CreateWorldEditPreset()
        );

        await Assert.That(preview.Tiles.Count).IsEqualTo(4096);
        await Assert.That(preview.Tiles[0].DrawOrder).IsEqualTo(0);
        await Assert.That(preview.Tiles[^1].DrawOrder).IsEqualTo(preview.Tiles.Count - 1);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(4096);
    }

    [Test]
    public async Task Build_KeepsNonFlatObjectsAfterFloorsOnLargeWorldMaps()
    {
        const int sectorCount = 37;
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 911, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        List<EditorMapSectorScenePreview> sectors = [];

        for (var localX = 0; localX < sectorCount; localX++)
        {
            sectors.Add(
                new EditorMapSectorScenePreview
                {
                    AssetPath = $"maps/map01/{localX}.sec",
                    SectorX = localX,
                    SectorY = 0,
                    LocalX = localX,
                    LocalY = 0,
                    PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                    ObjectDensityBand = EditorMapSectorDensityBand.Low,
                    BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                    TileArtIds = CreateFilledMapTileArtIds(),
                    RoofArtIds = null,
                    BlockMask = new uint[128],
                    Lights = [],
                    TileScripts = [],
                    Objects =
                    [
                        .. (
                            localX == 0
                                ? [CreateObjectPreview(objectId, protoId, ObjectType.Scenery, new ArtId(0x40000000u))]
                                : Array.Empty<EditorMapObjectPreview>()
                        ),
                    ],
                }
            );
        }

        var preview = EditorMapFloorRenderBuilder.Build(
            new EditorMapScenePreview
            {
                MapName = "map01",
                Width = sectorCount,
                Height = 1,
                UnpositionedSectorCount = 0,
                Sectors = sectors,
            },
            EditorMapFloorRenderRequest.CreateWorldEditPreset()
        );

        var lastFloorIndex = -1;
        var objectIndex = -1;
        for (var index = 0; index < preview.RenderQueue.Count; index++)
        {
            var item = preview.RenderQueue[index];
            if (item.Kind is EditorMapRenderQueueItemKind.FloorTile)
                lastFloorIndex = index;

            if (item.Kind is EditorMapRenderQueueItemKind.Object && item.Object?.ObjectId == objectId)
            {
                objectIndex = index;
            }
        }

        await Assert.That(preview.Tiles.Count).IsEqualTo(sectorCount * 64 * 64);
        await Assert.That(lastFloorIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(objectIndex).IsGreaterThan(lastFloorIndex);
    }

    [Test]
    public async Task Build_Isometric_ProjectsTilesInStableDrawOrderWithNormalizedBounds()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;
        tileArtIds[(62 * 64) + 0] = 200u;
        tileArtIds[(63 * 64) + 1] = 300u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                    BlockMask = CreateBlockMask((0, 63)),
                    Lights =
                    [
                        new EditorMapLightPreview
                        {
                            TileX = 0,
                            TileY = 63,
                            OffsetX = 0,
                            OffsetY = 0,
                            ArtId = new ArtId(0x01020304u),
                            Flags = 0,
                            Palette = 0,
                            Red = 0,
                            Green = 0,
                            Blue = 0,
                            TintColor = 0u,
                        },
                    ],
                    TileScripts =
                    [
                        new EditorMapTileScriptPreview
                        {
                            TileIndex = (63 * 64) + 1,
                            TileX = 1,
                            TileY = 63,
                            ScriptId = 77,
                            NodeFlags = 0u,
                            ScriptFlags = 0u,
                            ScriptCounters = 0u,
                        },
                    ],
                    Objects = [],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.ViewMode).IsEqualTo(EditorMapSceneViewMode.Isometric);
        await Assert.That(preview.WidthPixels).IsEqualTo(96d);
        await Assert.That(preview.HeightPixels).IsEqualTo(64d);
        await Assert.That(preview.Tiles.Count).IsEqualTo(3);

        await Assert.That(preview.Tiles[0].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Tiles[0].MapTileY).IsEqualTo(62);
        await Assert.That(preview.Tiles[0].ArtId).IsEqualTo(new ArtId(200u));
        await Assert.That(preview.Tiles[0].CenterX).IsEqualTo(32d);
        await Assert.That(preview.Tiles[0].CenterY).IsEqualTo(16d);
        await Assert.That(preview.Tiles[0].RoofCell).IsEqualTo(new Location(0, 15));
        await Assert.That(preview.Tiles[0].IsBlocked).IsFalse();
        await Assert.That(preview.Tiles[0].HasLight).IsFalse();
        await Assert.That(preview.Tiles[0].HasScript).IsFalse();

        await Assert.That(preview.Tiles[1].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Tiles[1].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Tiles[1].ArtId).IsEqualTo(new ArtId(100u));
        await Assert.That(preview.Tiles[1].CenterX).IsEqualTo(64d);
        await Assert.That(preview.Tiles[1].CenterY).IsEqualTo(32d);
        await Assert.That(preview.Tiles[1].IsBlocked).IsTrue();
        await Assert.That(preview.Tiles[1].HasLight).IsTrue();
        await Assert.That(preview.Tiles[1].HasScript).IsFalse();

        await Assert.That(preview.Tiles[2].MapTileX).IsEqualTo(1);
        await Assert.That(preview.Tiles[2].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Tiles[2].ArtId).IsEqualTo(new ArtId(300u));
        await Assert.That(preview.Tiles[2].CenterX).IsEqualTo(32d);
        await Assert.That(preview.Tiles[2].CenterY).IsEqualTo(48d);
        await Assert.That(preview.Tiles[2].HasLight).IsFalse();
        await Assert.That(preview.Tiles[2].HasScript).IsTrue();

        await Assert.That(preview.Overlays.Count).IsEqualTo(3);
        await Assert.That(preview.Overlays[0].Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
        await Assert.That(preview.Overlays[0].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Overlays[0].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Overlays[0].CenterX).IsEqualTo(64d);
        await Assert.That(preview.Overlays[0].CenterY).IsEqualTo(32d);
        await Assert.That(preview.Overlays[0].SuggestedTintColor).IsEqualTo(0x88CC6666u);

        await Assert.That(preview.Overlays[1].Kind).IsEqualTo(EditorMapTileOverlayKind.Light);
        await Assert.That(preview.Overlays[1].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Overlays[1].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Overlays[1].SuggestedTintColor).IsEqualTo(0x88E0C85Au);

        await Assert.That(preview.Overlays[2].Kind).IsEqualTo(EditorMapTileOverlayKind.Script);
        await Assert.That(preview.Overlays[2].MapTileX).IsEqualTo(1);
        await Assert.That(preview.Overlays[2].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Overlays[2].CenterX).IsEqualTo(32d);
        await Assert.That(preview.Overlays[2].CenterY).IsEqualTo(48d);
        await Assert.That(preview.Overlays[2].SuggestedTintColor).IsEqualTo(0x88996CCCu);

        await Assert.That(preview.Lights).HasSingleItem();
        await Assert.That(preview.Lights[0].ArtId).IsEqualTo(new ArtId(0x01020304u));
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(7);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[0].Tile?.ArtId).IsEqualTo(new ArtId(200u));
        await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[1].Tile?.ArtId).IsEqualTo(new ArtId(100u));
        await Assert.That(preview.RenderQueue[2].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[2].Tile?.ArtId).IsEqualTo(new ArtId(300u));
        await Assert.That(preview.RenderQueue[3].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
        await Assert.That(preview.RenderQueue[3].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
        await Assert.That(preview.RenderQueue[4].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
        await Assert.That(preview.RenderQueue[4].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.Light);
        await Assert.That(preview.RenderQueue[5].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
        await Assert.That(preview.RenderQueue[5].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.Script);
        await Assert.That(preview.RenderQueue[6].Kind).IsEqualTo(EditorMapRenderQueueItemKind.Light);
        await Assert.That(preview.RenderQueue[6].Light?.ArtId).IsEqualTo(new ArtId(0x01020304u));
    }

    [Test]
    public async Task Build_Isometric_MatchesCeLocationDeltasAcrossSectorBoundaries()
    {
        var originSectorTiles = new uint[64 * 64];
        originSectorTiles[(63 * 64) + 63] = 100u;
        var eastSectorTiles = new uint[64 * 64];
        eastSectorTiles[(63 * 64) + 0] = 200u;
        var southSectorTiles = new uint[64 * 64];
        southSectorTiles[63] = 300u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
                CreateSectorScenePreview("maps/map01/0_0.sec", localX: 0, localY: 0, tileArtIds: originSectorTiles),
                CreateSectorScenePreview("maps/map01/1_0.sec", localX: 1, localY: 0, tileArtIds: eastSectorTiles),
                CreateSectorScenePreview("maps/map01/0_1.sec", localX: 0, localY: 1, tileArtIds: southSectorTiles)
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var origin = preview.Tiles.Single(tile => tile.ArtId == new ArtId(100u));
        var plusX = preview.Tiles.Single(tile => tile.ArtId == new ArtId(200u));
        var plusY = preview.Tiles.Single(tile => tile.ArtId == new ArtId(300u));

        await Assert.That(plusX.MapTileX - origin.MapTileX).IsEqualTo(1);
        await Assert.That(plusX.MapTileY - origin.MapTileY).IsEqualTo(0);
        await Assert.That(plusX.CenterX - origin.CenterX).IsEqualTo(-32d);
        await Assert.That(plusX.CenterY - origin.CenterY).IsEqualTo(16d);
        await Assert.That(plusY.MapTileX - origin.MapTileX).IsEqualTo(0);
        await Assert.That(plusY.MapTileY - origin.MapTileY).IsEqualTo(1);
        await Assert.That(plusY.CenterX - origin.CenterX).IsEqualTo(32d);
        await Assert.That(plusY.CenterY - origin.CenterY).IsEqualTo(16d);
    }

    [Test]
    public async Task Build_Isometric_UsesAbsoluteSectorLightCoordinatesWithoutDoubleOffset()
    {
        const int localX = 10;
        const int localY = 20;
        const int tileX = 5;
        const int tileY = 7;
        var absoluteTileX = (localX * 64) + tileX;
        var absoluteTileY = (localY * 64) + tileY;
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(tileY * 64) + tileX] = 100u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
                new EditorMapSectorScenePreview
                {
                    AssetPath = "maps/map01/sector_abs_light.sec",
                    SectorX = localX,
                    SectorY = localY,
                    LocalX = localX,
                    LocalY = localY,
                    PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                    ObjectDensityBand = EditorMapSectorDensityBand.None,
                    BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                    TileArtIds = tileArtIds,
                    RoofArtIds = null,
                    BlockMask = new uint[128],
                    Lights =
                    [
                        new EditorMapLightPreview
                        {
                            TileX = absoluteTileX,
                            TileY = absoluteTileY,
                            OffsetX = 0,
                            OffsetY = 0,
                            ArtId = new ArtId(0x90100000u),
                            Flags = 0,
                            Palette = 0,
                            Red = 0xFF,
                            Green = 0xAA,
                            Blue = 0x55,
                            TintColor = 0x00FFAA55u,
                        },
                    ],
                    TileScripts = [],
                    Objects = [],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Lights).HasSingleItem();
        await Assert.That(preview.Lights[0].MapTileX).IsEqualTo(absoluteTileX);
        await Assert.That(preview.Lights[0].MapTileY).IsEqualTo(absoluteTileY);
        await Assert.That(preview.Lights[0].Tile).IsEqualTo(new Location(tileX, tileY));
        await Assert.That(preview.Lights[0].SuggestedTintColor).IsEqualTo(0xFFFFAA55u);
        await Assert.That(preview.RenderQueue.Any(item => item.Kind is EditorMapRenderQueueItemKind.Light)).IsTrue();
    }

    [Test]
    public async Task ProjectObjectAnchor_Isometric_ScalesCeOffsetsToCompressedRenderGrid()
    {
        var preview = new EditorMapObjectPreview
        {
            ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 33, Guid.NewGuid()),
            ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x40000000u),
            OffsetX = 40,
            OffsetY = 20,
            OffsetZ = 10f,
            RotationPitch = 0f,
        };

        var (anchorX, anchorY) = EditorMapFloorRenderBuilder.ProjectObjectAnchor(
            EditorMapSceneViewMode.Isometric,
            tileWidthPixels: 64d,
            tileHeightPixels: 32d,
            tileCenterX: 32d,
            tileCenterY: 16d,
            preview
        );

        // CE anchor: base_x = loc_x + offset_x + 40; tileCenterX already equals loc_x + 40.
        // With scaleX=0.8: anchorX = tileCenterX(32) + offsetX(40)*0.8 = 32 + 32 = 64.
        // With scaleY=0.8: anchorY = tileCenterY(16) + offsetY(20)*0.8 - offsetZ(10)*0.8 = 16+16-8 = 24.
        await Assert.That(anchorX).IsEqualTo(64d);
        await Assert.That(anchorY).IsEqualTo(24d);
    }

    [Test]
    public async Task Build_Isometric_IgnoresOffsetZForSameTileObjectOrdering()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 909, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 910, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateSingleTileObjectScene(
                CreateObjectPreview(
                    firstObjectId,
                    protoId,
                    ObjectType.Scenery,
                    new ArtId(0x40000000u),
                    offsetX: 12,
                    offsetY: 8,
                    offsetZ: 48f
                ),
                CreateObjectPreview(
                    secondObjectId,
                    protoId,
                    ObjectType.Scenery,
                    new ArtId(0x40000000u),
                    offsetX: 12,
                    offsetY: 8,
                    offsetZ: 0f
                )
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(2);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(preview.Objects[1].ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(preview.RenderQueue[1].Object?.ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(preview.RenderQueue[2].Object?.ObjectId).IsEqualTo(secondObjectId);
    }

    [Test]
    public async Task Build_TopDown_SkipsEmptyTilesUnlessExplicitlyRequested()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 111u;

        var scenePreview = CreateScenePreview(
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
            }
        );

        var compactPreview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.TopDown,
                TileWidthPixels = 32d,
                TileHeightPixels = 32d,
            }
        );
        var densePreview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.TopDown,
                TileWidthPixels = 32d,
                TileHeightPixels = 32d,
                IncludeEmptyTiles = true,
            }
        );

        await Assert.That(compactPreview.Tiles.Count).IsEqualTo(1);
        await Assert.That(compactPreview.WidthPixels).IsEqualTo(32d);
        await Assert.That(compactPreview.HeightPixels).IsEqualTo(32d);
        await Assert.That(compactPreview.Tiles[0].CenterX).IsEqualTo(16d);
        await Assert.That(compactPreview.Tiles[0].CenterY).IsEqualTo(16d);

        await Assert.That(densePreview.Tiles.Count).IsEqualTo(64 * 64);
        await Assert.That(densePreview.WidthPixels).IsEqualTo(2048d);
        await Assert.That(densePreview.HeightPixels).IsEqualTo(2048d);
        await Assert.That(densePreview.Tiles[0].MapTileY).IsEqualTo(0);
        await Assert.That(densePreview.Tiles[^1].MapTileY).IsEqualTo(63);
    }

    [Test]
    public async Task Build_Isometric_ProjectsObjectAnchorsIntoNormalizedFloorRenderSpace()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                            ObjectType = ObjectType.Pc,
                            CurrentArtId = new ArtId(0x01020304u),
                            Location = new Location(0, 63),
                            OffsetX = 5,
                            OffsetY = -3,
                            OffsetZ = 2f,
                            SpriteBounds = new EditorMapObjectSpriteBounds
                            {
                                MaxFrameWidth = 20,
                                MaxFrameHeight = 30,
                                MaxFrameCenterX = 8,
                                MaxFrameCenterY = 12,
                            },
                            RotationPitch = 0f,
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Tiles.Count).IsEqualTo(1);
        await Assert.That(preview.Objects.Count).IsEqualTo(1);
        await Assert.That(preview.WidthPixels).IsEqualTo(64d);
        await Assert.That(preview.HeightPixels).IsEqualTo(32d);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(2);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.Object);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(objectId);
        await Assert.That(preview.Objects[0].Tile).IsEqualTo(new Location(0, 63));
        await Assert.That(preview.Objects[0].AnchorX).IsEqualTo(36d);
        await Assert.That(preview.Objects[0].AnchorY).IsEqualTo(12d);
        await Assert.That(preview.Objects[0].IsTileGridSnapped).IsFalse();
        await Assert.That(preview.Objects[0].SpriteBounds).IsNotNull();
    }

    private static uint[] CreateSparseMapTileArtIds()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[0] = 1u;
        return tileArtIds;
    }

    [Test]
    public async Task Build_Isometric_PreservesPreviewOrderForEqualDepthStackedObjects()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 99, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                            ObjectId = firstObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Pc,
                            CurrentArtId = new ArtId(0x01020304u),
                            Location = new Location(0, 63),
                            RotationPitch = 0f,
                        },
                        new EditorMapObjectPreview
                        {
                            ObjectId = secondObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Npc,
                            CurrentArtId = new ArtId(0x05060708u),
                            Location = new Location(0, 63),
                            RotationPitch = 0f,
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(2);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(preview.Objects[1].ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(3);
        await Assert.That(preview.RenderQueue[1].Object?.ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(preview.RenderQueue[2].Object?.ObjectId).IsEqualTo(secondObjectId);
    }

    [Test]
    public async Task Build_Isometric_PreservesPreviewOrderForEqualDepthNonFlatObjects()
    {
        var tallerObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 199, Guid.NewGuid());
        var shorterObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 299, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                            ObjectId = tallerObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Pc,
                            CurrentArtId = new ArtId(0x01020304u),
                            Location = new Location(0, 63),
                            CollisionHeight = 32f,
                            RotationPitch = 0f,
                        },
                        new EditorMapObjectPreview
                        {
                            ObjectId = shorterObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Npc,
                            CurrentArtId = new ArtId(0x05060708u),
                            Location = new Location(0, 63),
                            CollisionHeight = 8f,
                            RotationPitch = 0f,
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(2);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(tallerObjectId);
        await Assert.That(preview.Objects[1].ObjectId).IsEqualTo(shorterObjectId);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(3);
        await Assert.That(preview.RenderQueue[1].Object?.ObjectId).IsEqualTo(tallerObjectId);
        await Assert.That(preview.RenderQueue[2].Object?.ObjectId).IsEqualTo(shorterObjectId);
    }

    [Test]
    public async Task Build_Isometric_UsesCeWallRotationOrderForSameTileWalls()
    {
        var earlierWallId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 399, Guid.NewGuid());
        var laterWallId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 499, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateSingleTileObjectScene(
                CreateObjectPreview(laterWallId, protoId, ObjectType.Wall, CreateWallArtId(rotation: 2)),
                CreateObjectPreview(earlierWallId, protoId, ObjectType.Wall, CreateWallArtId(rotation: 0))
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(2);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(earlierWallId);
        await Assert.That(preview.Objects[1].ObjectId).IsEqualTo(laterWallId);
        await Assert.That(preview.RenderQueue[1].Object?.ObjectId).IsEqualTo(earlierWallId);
        await Assert.That(preview.RenderQueue[2].Object?.ObjectId).IsEqualTo(laterWallId);
    }

    [Test]
    public async Task Build_Isometric_UsesCeWallOrderingAgainstSameTileScenery()
    {
        var wallId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 599, Guid.NewGuid());
        var sceneryId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 699, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateSingleTileObjectScene(
                CreateObjectPreview(wallId, protoId, ObjectType.Wall, CreateWallArtId(rotation: 2)),
                CreateObjectPreview(
                    sceneryId,
                    protoId,
                    ObjectType.Scenery,
                    new ArtId(0x406003C0u),
                    offsetX: -15,
                    offsetY: 3
                )
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(2);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(sceneryId);
        await Assert.That(preview.Objects[1].ObjectId).IsEqualTo(wallId);
        await Assert.That(preview.RenderQueue[1].Object?.ObjectId).IsEqualTo(sceneryId);
        await Assert.That(preview.RenderQueue[2].Object?.ObjectId).IsEqualTo(wallId);
    }

    [Test]
    public async Task Build_Isometric_UsesCeGlobalBandsForUnderlaysFlatObjectsAndOverlays()
    {
        var flatObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 801, Guid.NewGuid());
        var nonFlatObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 802, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(10 * 64) + 10] = 100u;
        tileArtIds[(10 * 64) + 11] = 101u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                            ObjectId = nonFlatObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Npc,
                            CurrentArtId = new ArtId(0x20000000u),
                            Location = new Location(10, 10),
                            RotationPitch = 0f,
                        },
                        new EditorMapObjectPreview
                        {
                            ObjectId = flatObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Scenery,
                            CurrentArtId = new ArtId(0x40000000u),
                            Flags = ObjectFlags.Flat,
                            Location = new Location(11, 10),
                            RotationPitch = 0f,
                            UnderlayArtIds = [901],
                            OverlayForeArtIds = [902],
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var objectStageItems = preview
            .RenderQueue.Where(item =>
                item.Kind is EditorMapRenderQueueItemKind.Object or EditorMapRenderQueueItemKind.ObjectAuxiliary
            )
            .ToArray();

        await Assert.That(objectStageItems.Length).IsEqualTo(4);
        await Assert
            .That(objectStageItems[0].ObjectAuxiliaryItem?.Layer)
            .IsEqualTo(EditorMapObjectAuxiliaryRenderLayer.Underlay);
        await Assert.That(objectStageItems[0].ObjectAuxiliaryItem?.ParentObjectId).IsEqualTo(flatObjectId);
        await Assert.That(objectStageItems[1].Object?.ObjectId).IsEqualTo(flatObjectId);
        await Assert
            .That(objectStageItems[1].Object?.CommittedRenderLayer)
            .IsEqualTo(EditorMapCommittedRenderLayer.GroundDecal);
        await Assert.That(objectStageItems[2].Object?.ObjectId).IsEqualTo(nonFlatObjectId);
        await Assert
            .That(objectStageItems[3].ObjectAuxiliaryItem?.Layer)
            .IsEqualTo(EditorMapObjectAuxiliaryRenderLayer.OverlayFore);
        await Assert.That(objectStageItems[3].ObjectAuxiliaryItem?.ParentObjectId).IsEqualTo(flatObjectId);
    }

    [Test]
    public async Task Build_Isometric_UsesCeGlobalShadowBandBeforeAllNonFlatMainSprites()
    {
        var shadowedObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 803, Guid.NewGuid());
        var otherObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 804, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(10 * 64) + 10] = 100u;
        tileArtIds[(10 * 64) + 11] = 101u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                            ObjectId = otherObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Npc,
                            CurrentArtId = new ArtId(0x20000000u),
                            Location = new Location(10, 10),
                            RotationPitch = 0f,
                        },
                        new EditorMapObjectPreview
                        {
                            ObjectId = shadowedObjectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Npc,
                            CurrentArtId = new ArtId(0x20000000u),
                            Location = new Location(11, 10),
                            RotationPitch = 0f,
                            ShadowArtId = new ArtId(903u),
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var objectStageItems = preview
            .RenderQueue.Where(item =>
                item.Kind is EditorMapRenderQueueItemKind.Object or EditorMapRenderQueueItemKind.ObjectAuxiliary
            )
            .ToArray();

        await Assert.That(objectStageItems.Length).IsEqualTo(3);
        await Assert
            .That(objectStageItems[0].ObjectAuxiliaryItem?.Layer)
            .IsEqualTo(EditorMapObjectAuxiliaryRenderLayer.Shadow);
        await Assert.That(objectStageItems[0].ObjectAuxiliaryItem?.ParentObjectId).IsEqualTo(shadowedObjectId);
        await Assert.That(objectStageItems[1].Object?.ObjectId).IsEqualTo(otherObjectId);
        await Assert.That(objectStageItems[2].Object?.ObjectId).IsEqualTo(shadowedObjectId);
    }

    [Test]
    public async Task Build_Isometric_UsesCeOverlaySlotOrder()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 805, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var overlayBackArtIds = new int[7];
        var overlayForeArtIds = new int[7];
        overlayForeArtIds[6] = 9606;
        overlayBackArtIds[6] = 9706;
        overlayForeArtIds[0] = 9600;
        overlayBackArtIds[0] = 9700;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateSingleTileObjectScene(
                new EditorMapObjectPreview
                {
                    ObjectId = objectId,
                    ProtoId = protoId,
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x20000000u),
                    Location = new Location(0, 63),
                    RotationPitch = 0f,
                    OverlayBackArtIds = overlayBackArtIds,
                    OverlayForeArtIds = overlayForeArtIds,
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var overlayArtIds = preview
            .ObjectAuxiliaryItems.Where(item =>
                item.Layer
                    is EditorMapObjectAuxiliaryRenderLayer.OverlayBack
                        or EditorMapObjectAuxiliaryRenderLayer.OverlayFore
            )
            .Select(item => item.ArtId.Value)
            .ToArray();

        await Assert.That(overlayArtIds.Length).IsEqualTo(4);
        await Assert.That(overlayArtIds[0]).IsEqualTo(9606u);
        await Assert.That(overlayArtIds[1]).IsEqualTo(9706u);
        await Assert.That(overlayArtIds[2]).IsEqualTo(9600u);
        await Assert.That(overlayArtIds[3]).IsEqualTo(9700u);
    }

    [Test]
    public async Task Build_Isometric_InterleavesGhostAndArmorOverlaysWithNonFlatObjects()
    {
        var parentNpcId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 999, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var ghostArtId = new ArtId((uint)((243 << 17) | 0x60000000u));

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateSingleTileObjectScene(
                new EditorMapObjectPreview
                {
                    ObjectId = parentNpcId,
                    ProtoId = protoId,
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x20000000u),
                    Location = new Location(0, 63),
                    RotationPitch = 0f,
                    IsDead = true,
                    OverlayForeArtIds = [(int)ghostArtId.Value],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        var relevantQueue = preview
            .RenderQueue.Where(item =>
                (item.Kind == EditorMapRenderQueueItemKind.Object && item.Object?.ObjectId == parentNpcId)
                || (
                    item.Kind == EditorMapRenderQueueItemKind.ObjectAuxiliary
                    && item.ObjectAuxiliaryItem?.ParentObjectId == parentNpcId
                )
            )
            .ToArray();

        await Assert.That(relevantQueue.Length).IsEqualTo(2);
        await Assert.That(relevantQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.Object);
        await Assert.That(relevantQueue[0].Object?.ObjectId).IsEqualTo(parentNpcId);
        await Assert.That(relevantQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.ObjectAuxiliary);
        await Assert
            .That(relevantQueue[1].ObjectAuxiliaryItem?.Layer)
            .IsEqualTo(EditorMapObjectAuxiliaryRenderLayer.OverlayFore);
    }

    [Test]
    public async Task Build_Isometric_NormalizesWallBoundsUsingRawCeHotspot()
    {
        var wallId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 799, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateSingleTileObjectScene(
                new EditorMapObjectPreview
                {
                    ObjectId = wallId,
                    ProtoId = protoId,
                    ObjectType = ObjectType.Wall,
                    CurrentArtId = CreateWallArtId(rotation: 0),
                    Location = new Location(0, 63),
                    OffsetX = 0,
                    OffsetY = 0,
                    OffsetZ = 0f,
                    CollisionHeight = 0f,
                    SpriteBounds = new EditorMapObjectSpriteBounds
                    {
                        MaxFrameWidth = 40,
                        MaxFrameHeight = 152,
                        MaxFrameCenterX = 39,
                        MaxFrameCenterY = 131,
                    },
                    RotationPitch = 0f,
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.HeightPixels).IsCloseTo(136.8d, 0.001d);
        await Assert.That(preview.Objects).HasSingleItem();
        await Assert.That(preview.Objects[0].AnchorY).IsCloseTo(120.8d, 0.001d);
    }

    [Test]
    public async Task GetLayoutSpriteCenter_WallRotationZeroAppliesCeNorthSouthHotspotShift()
    {
        var spriteBounds = new EditorMapObjectSpriteBounds
        {
            MaxFrameWidth = 100,
            MaxFrameHeight = 80,
            MaxFrameCenterX = 30,
            MaxFrameCenterY = 40,
        };

        var (centerX, centerY) = EditorMapFloorRenderBuilder.GetLayoutSpriteCenter(
            ObjectType.Wall,
            CreateWallArtId(rotation: 0),
            spriteBounds
        );

        await Assert.That(centerX).IsEqualTo(-10);
        await Assert.That(centerY).IsEqualTo(60);
    }

    [Test]
    public async Task Build_Isometric_HidesObjectsCoveredByRoofCellsBeforeQueueing()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 806, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(10 * 64) + 10] = 100u;
        var roofArtIds = new uint[16 * 16];
        roofArtIds[(3 * 16) + 3] = CreateRoofArtId(piece: 8);

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
                new EditorMapSectorScenePreview
                {
                    AssetPath = "maps/map01/sector_a.sec",
                    SectorX = 0,
                    SectorY = 0,
                    LocalX = 0,
                    LocalY = 0,
                    PreviewFlags = EditorMapSectorPreviewFlags.HasRoofs | EditorMapSectorPreviewFlags.Occupied,
                    ObjectDensityBand = EditorMapSectorDensityBand.Low,
                    BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                    TileArtIds = tileArtIds,
                    RoofArtIds = roofArtIds,
                    BlockMask = new uint[128],
                    Lights = [],
                    TileScripts = [],
                    Objects =
                    [
                        new EditorMapObjectPreview
                        {
                            ObjectId = objectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Npc,
                            CurrentArtId = new ArtId(0x20000000u),
                            Location = new Location(10, 10),
                            RotationPitch = 0f,
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        // Objects under roofs are retained in the queue with IsRoofCovered=true for dynamic UI-layer hiding.
        await Assert.That(preview.Objects.Count).IsEqualTo(1);
        await Assert.That(preview.Objects[0].IsRoofCovered).IsTrue();
        await Assert
            .That(preview.RenderQueue.Count(item => item.Kind == EditorMapRenderQueueItemKind.Object))
            .IsEqualTo(1);
        await Assert.That(preview.Roofs.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Build_Isometric_HidesTransparentCardinalWallsUnderFadedRoofs()
    {
        var wallId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 807, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[0] = 100u;
        var roofArtIds = new uint[16 * 16];
        roofArtIds[0] = CreateRoofArtId(piece: 0, faded: true);

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
                new EditorMapSectorScenePreview
                {
                    AssetPath = "maps/map01/sector_a.sec",
                    SectorX = 0,
                    SectorY = 0,
                    LocalX = 0,
                    LocalY = 0,
                    PreviewFlags = EditorMapSectorPreviewFlags.HasRoofs | EditorMapSectorPreviewFlags.Occupied,
                    ObjectDensityBand = EditorMapSectorDensityBand.Low,
                    BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                    TileArtIds = tileArtIds,
                    RoofArtIds = roofArtIds,
                    BlockMask = new uint[128],
                    Lights = [],
                    TileScripts = [],
                    Objects =
                    [
                        new EditorMapObjectPreview
                        {
                            ObjectId = wallId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Wall,
                            CurrentArtId = CreateWallArtId(rotation: 0),
                            Location = new Location(0, 0),
                            WallFlags = 0x2,
                            RotationPitch = 0f,
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Objects).IsEmpty();
        await Assert
            .That(preview.RenderQueue.Count(item => item.Kind == EditorMapRenderQueueItemKind.Object))
            .IsEqualTo(0);
        await Assert.That(preview.Roofs.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Build_Isometric_ProjectsRoofCellsIntoUnifiedRenderQueue()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var roofArtIds = new uint[16 * 16];
        roofArtIds[(15 * 16) + 0] = 999u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Roofs.Count).IsEqualTo(1);
        await Assert.That(preview.Roofs[0].RoofCell).IsEqualTo(new Location(0, 15));
        await Assert.That(preview.Roofs[0].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Roofs[0].MapTileY).IsEqualTo(60);
        await Assert.That(preview.Roofs[0].ArtId).IsEqualTo(new ArtId(999u));
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(2);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.Roof);
    }

    [Test]
    public async Task ProjectRoofAnchor_Isometric_MatchesCeNormalizedRoofCellOffsets()
    {
        var (anchorX, anchorY) = EditorMapFloorRenderBuilder.ProjectRoofAnchor(
            EditorMapSceneViewMode.Isometric,
            tileWidthPixels: 64d,
            tileHeightPixels: 32d,
            mapTileX: 0,
            topMapTileY: 60
        );

        await Assert.That(anchorX).IsEqualTo(1792d);
        await Assert.That(anchorY).IsEqualTo(864d);
    }

    [Test]
    public async Task Build_IgnoresInvalidRoofSentinelArtIds()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var roofArtIds = new uint[16 * 16];
        roofArtIds[(15 * 16) + 0] = uint.MaxValue;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Roofs.Count).IsEqualTo(0);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(1);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
    }

    [Test]
    public async Task Build_IgnoresRoofFillPiecesLikeCeRoofDraw()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var roofArtIds = new uint[16 * 16];
        roofArtIds[(15 * 16) + 0] = 0xA0002000u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
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
                }
            ),
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );

        await Assert.That(preview.Roofs).IsEmpty();
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(1);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
    }

    [Test]
    public async Task Build_WithPreviewState_ComposesObjectRoofAndOverlayVisibility()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var tileArtIds = new uint[64 * 64];
        tileArtIds[(63 * 64) + 0] = 100u;

        var roofArtIds = new uint[16 * 16];
        roofArtIds[(15 * 16) + 0] = 999u;

        var preview = EditorMapFloorRenderBuilder.Build(
            CreateScenePreview(
                new EditorMapSectorScenePreview
                {
                    AssetPath = "maps/map01/sector_a.sec",
                    SectorX = 0,
                    SectorY = 0,
                    LocalX = 0,
                    LocalY = 0,
                    PreviewFlags = EditorMapSectorPreviewFlags.HasRoofs | EditorMapSectorPreviewFlags.Occupied,
                    ObjectDensityBand = EditorMapSectorDensityBand.Low,
                    BlockedTileDensityBand = EditorMapSectorDensityBand.Low,
                    TileArtIds = tileArtIds,
                    RoofArtIds = roofArtIds,
                    BlockMask = CreateBlockMask((0, 63)),
                    Lights =
                    [
                        new EditorMapLightPreview
                        {
                            TileX = 0,
                            TileY = 63,
                            OffsetX = 0,
                            OffsetY = 0,
                            ArtId = new ArtId(0x01020304u),
                            Flags = 0,
                            Palette = 0,
                            Red = 0,
                            Green = 0,
                            Blue = 0,
                            TintColor = 0u,
                        },
                    ],
                    TileScripts =
                    [
                        new EditorMapTileScriptPreview
                        {
                            TileIndex = 63 * 64,
                            TileX = 0,
                            TileY = 63,
                            ScriptId = 77,
                            NodeFlags = 0u,
                            ScriptFlags = 0u,
                            ScriptCounters = 0u,
                        },
                    ],
                    Objects =
                    [
                        new EditorMapObjectPreview
                        {
                            ObjectId = objectId,
                            ProtoId = protoId,
                            ObjectType = ObjectType.Pc,
                            CurrentArtId = new ArtId(0x01020304u),
                            Location = new Location(0, 63),
                            OffsetX = 0,
                            OffsetY = 0,
                            OffsetZ = 0f,
                            SpriteBounds = null,
                            RotationPitch = 0f,
                        },
                    ],
                }
            ),
            new EditorMapFloorRenderRequest().WithPreviewState(
                new EditorProjectMapPreviewState
                {
                    ShowObjects = false,
                    ShowRoofs = false,
                    ShowLights = false,
                    ShowBlockedTiles = true,
                    ShowScripts = false,
                }
            )
        );

        await Assert.That(preview.Tiles.Count).IsEqualTo(1);
        await Assert.That(preview.Objects).IsEmpty();
        await Assert.That(preview.Roofs).IsEmpty();
        await Assert.That(preview.Overlays.Count).IsEqualTo(1);
        await Assert.That(preview.Overlays[0].Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(2);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
    }

    private static EditorMapScenePreview CreateScenePreview(params EditorMapSectorScenePreview[] sectors) =>
        new()
        {
            MapName = "map01",
            Width = sectors.Length == 0 ? 0 : sectors.Max(static sector => sector.LocalX) + 1,
            Height = sectors.Length == 0 ? 0 : sectors.Max(static sector => sector.LocalY) + 1,
            UnpositionedSectorCount = 0,
            Sectors = sectors,
        };

    private static EditorMapSectorScenePreview CreateSectorScenePreview(
        string assetPath,
        int localX,
        int localY,
        uint[] tileArtIds
    ) =>
        new()
        {
            AssetPath = assetPath,
            SectorX = localX,
            SectorY = localY,
            LocalX = localX,
            LocalY = localY,
            PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
            ObjectDensityBand = EditorMapSectorDensityBand.None,
            BlockedTileDensityBand = EditorMapSectorDensityBand.None,
            TileArtIds = tileArtIds,
            RoofArtIds = null,
            BlockMask = new uint[128],
            Lights = [],
            TileScripts = [],
            Objects = [],
        };

    private static EditorMapScenePreview CreateSingleTileObjectScene(params EditorMapObjectPreview[] objects) =>
        CreateScenePreview(
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
                TileArtIds = CreateSingleTileMapTileArtIds(),
                RoofArtIds = null,
                BlockMask = new uint[128],
                Lights = [],
                TileScripts = [],
                Objects = objects,
            }
        );

    private static EditorMapObjectPreview CreateObjectPreview(
        GameObjectGuid objectId,
        GameObjectGuid protoId,
        ObjectType objectType,
        ArtId currentArtId,
        int offsetX = 0,
        int offsetY = 0,
        float offsetZ = 0f,
        float collisionHeight = 0f
    ) =>
        new()
        {
            ObjectId = objectId,
            ProtoId = protoId,
            ObjectType = objectType,
            CurrentArtId = currentArtId,
            Location = new Location(0, 63),
            OffsetX = offsetX,
            OffsetY = offsetY,
            OffsetZ = offsetZ,
            CollisionHeight = collisionHeight,
            RotationPitch = 0f,
        };

    private static uint[] CreateSingleTileMapTileArtIds()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[63 * 64] = 100u;
        return tileArtIds;
    }

    private static uint[] CreateFilledMapTileArtIds(uint artId = 100u)
    {
        var tileArtIds = new uint[64 * 64];
        Array.Fill(tileArtIds, artId);
        return tileArtIds;
    }

    private static ArtId CreateWallArtId(int rotation) => new(0x10000000u | ((uint)(rotation & 0x7) << 11));

    private static uint CreateRoofArtId(int piece, bool faded = false, bool fill = false)
    {
        var artId = 0xA0000000u | ((uint)piece << 14);
        if (faded)
            artId |= 0x1000u;
        if (fill)
            artId |= 0x2000u;

        return artId;
    }

    [Test]
    public async Task Build_ParityGaps_Reconciliation_Verified()
    {
        var guidId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1000, Guid.NewGuid());
        var guidId2 = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1001, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);

        // 1. Verify item types and traps are mapped to Scenery instead of Ground
        var weaponObj = CreateObjectPreview(
            objectId: guidId,
            protoId: protoId,
            objectType: ObjectType.Weapon,
            currentArtId: new ArtId(0x02000000u)
        );
        var trapObj = CreateObjectPreview(
            objectId: guidId2,
            protoId: protoId,
            objectType: ObjectType.Trap,
            currentArtId: new ArtId(0x02000001u)
        );

        var scenePreview = CreateScenePreview(
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
                TileArtIds = CreateSingleTileMapTileArtIds(),
                RoofArtIds = null,
                BlockMask = new uint[128],
                Lights = [],
                TileScripts = [],
                Objects = [weaponObj, trapObj],
            }
        );

        var preview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 80d,
                TileHeightPixels = 40d,
            }
        );

        await Assert.That(preview.Objects.Count).IsEqualTo(2);
        await Assert.That(preview.Objects[0].CommittedRenderLayer).IsEqualTo(EditorMapCommittedRenderLayer.Scenery);
        await Assert.That(preview.Objects[1].CommittedRenderLayer).IsEqualTo(EditorMapCommittedRenderLayer.Scenery);

        // 2. Verify that floor tiles with TileType != 0 (e.g. 1) are correctly evaluated under roofs
        var tileArtIds = new uint[64 * 64];
        tileArtIds[0] = 0x01000005u; // ArtType = Tile (0), TileType = 1 (bits 24-27), ArtNum = 5

        var roofArtIds = new uint[16 * 16];
        roofArtIds[0] = CreateRoofArtId(2, faded: false, fill: false); // FrameIndex = 2, row 3, col 3 will map to roof matrix index 2 (which is true)

        var objectUnderRoof = new EditorMapObjectPreview
        {
            ObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1002, Guid.NewGuid()),
            ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
            ObjectType = ObjectType.Scenery,
            CurrentArtId = new ArtId(0x02000002u),
            Location = new Location(0, 0),
            RotationPitch = 0f,
        };

        var scenePreviewWithRoof = CreateScenePreview(
            new EditorMapSectorScenePreview
            {
                AssetPath = "maps/map01/sector_roof.sec",
                SectorX = 0,
                SectorY = 0,
                LocalX = 0,
                LocalY = 0,
                PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                ObjectDensityBand = EditorMapSectorDensityBand.Low,
                BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                TileArtIds = tileArtIds,
                RoofArtIds = roofArtIds,
                BlockMask = new uint[128],
                Lights = [],
                TileScripts = [],
                Objects = [objectUnderRoof],
            }
        );

        var previewWithRoof = EditorMapFloorRenderBuilder.Build(
            scenePreviewWithRoof,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 80d,
                TileHeightPixels = 40d,
            }
        );

        await Assert.That(previewWithRoof.Objects.Count).IsEqualTo(1);
        await Assert.That(previewWithRoof.Objects[0].IsRoofCovered).IsTrue();
    }

    [Test]
    public async Task Build_FloorLightShadingPass_PopulatesBilinearLightGridDiagnostics()
    {
        var tileArtIds = new uint[64 * 64];
        tileArtIds[0] = 0x00000001u; // ArtType = Tile (0), TileType = 0 (bits 24-27 => indoor)

        var scenePreview = CreateScenePreview(
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
                Lights =
                [
                    new EditorMapLightPreview
                    {
                        TileX = 0,
                        TileY = 0,
                        OffsetX = 0,
                        OffsetY = 0,
                        ArtId = new ArtId(0x01020304u),
                        Flags = 0,
                        Palette = 0,
                        Red = 100,
                        Green = 50,
                        Blue = 20,
                        TintColor = 0xFF000000u | (100u << 16) | (50u << 8) | 20u,
                    },
                ],
                TileScripts = [],
                Objects = [],
            }
        );

        var preview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 80d,
                TileHeightPixels = 40d,
                IncludeFloorLightTint = true,
                IncludeEmptyTiles = true,
            }
        );

        var targetTile = preview.Tiles.Single(tile => tile.MapTileX == 0 && tile.MapTileY == 0);
        await Assert.That(targetTile.SuggestedTintColor).IsNotNull();
        await Assert.That(targetTile.LightDiagnostics).IsNotNull();
        await Assert.That(targetTile.LightDiagnostics.Value.MiddleCenter).IsNotNull();
        // Since CenterX and CenterY are equal to lx and ly, middleCenter should have maximum light intensity added to ambient 128:
        // Math.Clamp(128 + 100, 0, 255) = 228 (E4)
        // Math.Clamp(128 + 50, 0, 255) = 178 (B2)
        // Math.Clamp(128 + 20, 0, 255) = 148 (94)
        // pointColor: 0xFFE4B294
        await Assert.That(targetTile.LightDiagnostics.Value.MiddleCenter!.Value).IsEqualTo(0xFFE4B294u);
    }

    [Test]
    public async Task Build_ObjectOverlayLights_ContributeFloorTintAndVisibleLightQueueItems()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1003, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var overlayLightArtId = new ArtId(0x90002000u);
        var scenePreview = CreateSingleTileObjectScene(
            new EditorMapObjectPreview
            {
                ObjectId = objectId,
                ProtoId = protoId,
                ObjectType = ObjectType.Scenery,
                CurrentArtId = new ArtId(0x40000000u),
                Location = new Location(0, 63),
                RotationPitch = 0f,
                OverlayLights =
                [
                    new EditorMapObjectOverlayLightPreview
                    {
                        Flags = 7,
                        ArtId = overlayLightArtId,
                        Color = new Color(0xAA, 0x55, 0x22),
                    },
                ],
            }
        );

        var preview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 80d,
                TileHeightPixels = 40d,
                IncludeFloorLightTint = true,
            }
        );

        await Assert.That(preview.ObjectAuxiliaryItems.Any(item => item.ArtId == overlayLightArtId)).IsFalse();
        await Assert
            .That(preview.RenderQueue.Any(item => item.Kind is EditorMapRenderQueueItemKind.ObjectAuxiliary))
            .IsFalse();
        await Assert.That(preview.Lights.Count).IsEqualTo(1);
        await Assert.That(preview.Lights[0].ArtId).IsEqualTo(overlayLightArtId);
        await Assert
            .That(preview.RenderQueue.Count(item => item.Kind is EditorMapRenderQueueItemKind.Light))
            .IsEqualTo(1);
        var litTile = preview.Tiles.Single(tile => tile.MapTileX == 0 && tile.MapTileY == 63);
        await Assert.That(litTile.SuggestedTintColor).IsNotNull();
        await Assert.That(litTile.LightDiagnostics).IsNotNull();
    }

    [Test]
    public async Task Build_ObjectLightAid_ContributesFloorTintAndVisibleLightQueueItems()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 1004, Guid.NewGuid());
        var protoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty);
        var lightAidArtId = new ArtId(0x90001000u);
        var scenePreview = CreateSingleTileObjectScene(
            new EditorMapObjectPreview
            {
                ObjectId = objectId,
                ProtoId = protoId,
                ObjectType = ObjectType.Scenery,
                CurrentArtId = new ArtId(0x40000000u),
                LightAid = lightAidArtId,
                LightColor = new Color(0xD0, 0x90, 0x40),
                Location = new Location(0, 63),
                RotationPitch = 0f,
            }
        );

        var preview = EditorMapFloorRenderBuilder.Build(
            scenePreview,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 80d,
                TileHeightPixels = 40d,
                IncludeFloorLightTint = true,
            }
        );

        await Assert.That(preview.ObjectAuxiliaryItems.Any(item => item.ArtId == lightAidArtId)).IsFalse();
        await Assert
            .That(preview.RenderQueue.Any(item => item.Kind is EditorMapRenderQueueItemKind.ObjectAuxiliary))
            .IsFalse();
        await Assert.That(preview.Lights.Count).IsEqualTo(1);
        await Assert.That(preview.Lights[0].ArtId).IsEqualTo(lightAidArtId);
        await Assert
            .That(preview.RenderQueue.Count(item => item.Kind is EditorMapRenderQueueItemKind.Light))
            .IsEqualTo(1);
        var litTile = preview.Tiles.Single(tile => tile.MapTileX == 0 && tile.MapTileY == 63);
        await Assert.That(litTile.SuggestedTintColor).IsNotNull();
        await Assert.That(litTile.LightDiagnostics).IsNotNull();
    }

    private static uint[] CreateBlockMask(params (int TileX, int TileY)[] blockedTiles)
    {
        var blockMask = new uint[128];
        foreach (var (tileX, tileY) in blockedTiles)
        {
            var tileIndex = (tileY * 64) + tileX;
            blockMask[tileIndex / 32] |= 1u << (tileIndex % 32);
        }

        return blockMask;
    }
}
