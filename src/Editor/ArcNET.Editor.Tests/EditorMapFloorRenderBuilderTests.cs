using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapFloorRenderBuilderTests
{
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
        await Assert.That(preview.WidthPixels).IsEqualTo(128d);
        await Assert.That(preview.HeightPixels).IsEqualTo(48d);
        await Assert.That(preview.Tiles.Count).IsEqualTo(3);

        await Assert.That(preview.Tiles[0].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Tiles[0].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Tiles[0].ArtId).IsEqualTo(new ArtId(100u));
        await Assert.That(preview.Tiles[0].CenterX).IsEqualTo(64d);
        await Assert.That(preview.Tiles[0].CenterY).IsEqualTo(16d);
        await Assert.That(preview.Tiles[0].IsBlocked).IsTrue();
        await Assert.That(preview.Tiles[0].HasLight).IsTrue();
        await Assert.That(preview.Tiles[0].HasScript).IsFalse();

        await Assert.That(preview.Tiles[1].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Tiles[1].MapTileY).IsEqualTo(62);
        await Assert.That(preview.Tiles[1].ArtId).IsEqualTo(new ArtId(200u));
        await Assert.That(preview.Tiles[1].CenterX).IsEqualTo(32d);
        await Assert.That(preview.Tiles[1].CenterY).IsEqualTo(32d);
        await Assert.That(preview.Tiles[1].RoofCell).IsEqualTo(new Location(0, 15));

        await Assert.That(preview.Tiles[2].MapTileX).IsEqualTo(1);
        await Assert.That(preview.Tiles[2].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Tiles[2].ArtId).IsEqualTo(new ArtId(300u));
        await Assert.That(preview.Tiles[2].CenterX).IsEqualTo(96d);
        await Assert.That(preview.Tiles[2].CenterY).IsEqualTo(32d);
        await Assert.That(preview.Tiles[2].HasLight).IsFalse();
        await Assert.That(preview.Tiles[2].HasScript).IsTrue();

        await Assert.That(preview.Overlays.Count).IsEqualTo(3);
        await Assert.That(preview.Overlays[0].Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
        await Assert.That(preview.Overlays[0].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Overlays[0].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Overlays[0].CenterX).IsEqualTo(64d);
        await Assert.That(preview.Overlays[0].CenterY).IsEqualTo(16d);
        await Assert.That(preview.Overlays[0].SuggestedTintColor).IsEqualTo(0x88CC6666u);

        await Assert.That(preview.Overlays[1].Kind).IsEqualTo(EditorMapTileOverlayKind.Light);
        await Assert.That(preview.Overlays[1].MapTileX).IsEqualTo(0);
        await Assert.That(preview.Overlays[1].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Overlays[1].SuggestedTintColor).IsEqualTo(0x88E0C85Au);

        await Assert.That(preview.Overlays[2].Kind).IsEqualTo(EditorMapTileOverlayKind.Script);
        await Assert.That(preview.Overlays[2].MapTileX).IsEqualTo(1);
        await Assert.That(preview.Overlays[2].MapTileY).IsEqualTo(63);
        await Assert.That(preview.Overlays[2].CenterX).IsEqualTo(96d);
        await Assert.That(preview.Overlays[2].CenterY).IsEqualTo(32d);
        await Assert.That(preview.Overlays[2].SuggestedTintColor).IsEqualTo(0x88996CCCu);

        await Assert.That(preview.RenderQueue.Count).IsEqualTo(6);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
        await Assert.That(preview.RenderQueue[1].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.BlockedTile);
        await Assert.That(preview.RenderQueue[2].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
        await Assert.That(preview.RenderQueue[2].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.Light);
        await Assert.That(preview.RenderQueue[5].Kind).IsEqualTo(EditorMapRenderQueueItemKind.TileOverlay);
        await Assert.That(preview.RenderQueue[5].TileOverlay?.Kind).IsEqualTo(EditorMapTileOverlayKind.Script);
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
        await Assert.That(densePreview.Tiles[0].MapTileY).IsEqualTo(63);
        await Assert.That(densePreview.Tiles[^1].MapTileY).IsEqualTo(0);
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
        await Assert.That(preview.HeightPixels).IsEqualTo(33d);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(2);
        await Assert.That(preview.RenderQueue[0].Kind).IsEqualTo(EditorMapRenderQueueItemKind.FloorTile);
        await Assert.That(preview.RenderQueue[1].Kind).IsEqualTo(EditorMapRenderQueueItemKind.Object);
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(objectId);
        await Assert.That(preview.Objects[0].Tile).IsEqualTo(new Location(0, 63));
        await Assert.That(preview.Objects[0].AnchorX).IsEqualTo(37d);
        await Assert.That(preview.Objects[0].AnchorY).IsEqualTo(12d);
        await Assert.That(preview.Objects[0].IsTileGridSnapped).IsFalse();
        await Assert.That(preview.Objects[0].SpriteBounds).IsNotNull();
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
    public async Task Build_Isometric_PrefersCollisionHeightBeforePreviewOrderForEqualDepthStackedObjects()
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
        await Assert.That(preview.Objects[0].ObjectId).IsEqualTo(shorterObjectId);
        await Assert.That(preview.Objects[1].ObjectId).IsEqualTo(tallerObjectId);
        await Assert.That(preview.RenderQueue.Count).IsEqualTo(3);
        await Assert.That(preview.RenderQueue[1].Object?.ObjectId).IsEqualTo(shorterObjectId);
        await Assert.That(preview.RenderQueue[2].Object?.ObjectId).IsEqualTo(tallerObjectId);
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
