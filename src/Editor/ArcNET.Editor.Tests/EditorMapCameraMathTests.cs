using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapCameraMathTests
{
    [Test]
    public async Task GetVisibleTileBounds_UsesCameraCenterAndZoom()
    {
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 10.5,
            CenterTileY = 20.25,
            Zoom = 2.0,
        };

        var bounds = EditorMapCameraMath.GetVisibleTileBounds(
            camera,
            viewportWidth: 320,
            viewportHeight: 160,
            pixelsPerTileAtZoom1: 16
        );

        await Assert.That(bounds.MinTileX).IsEqualTo(5.5);
        await Assert.That(bounds.MaxTileX).IsEqualTo(15.5);
        await Assert.That(bounds.MinTileY).IsEqualTo(17.75);
        await Assert.That(bounds.MaxTileY).IsEqualTo(22.75);
        await Assert.That(bounds.Width).IsEqualTo(10d);
        await Assert.That(bounds.Height).IsEqualTo(5d);
    }

    [Test]
    public async Task ProjectAndUnprojectTile_RoundTripViewportCoordinates()
    {
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 12.0,
            CenterTileY = 30.0,
            Zoom = 1.5,
        };

        var centerX = EditorMapCameraMath.ProjectTileX(
            camera,
            viewportWidth: 640,
            viewportHeight: 480,
            pixelsPerTileAtZoom1: 32,
            tileX: 12.0
        );
        var centerY = EditorMapCameraMath.ProjectTileY(
            camera,
            viewportWidth: 640,
            viewportHeight: 480,
            pixelsPerTileAtZoom1: 32,
            tileY: 30.0
        );
        var projectedX = EditorMapCameraMath.ProjectTileX(
            camera,
            viewportWidth: 640,
            viewportHeight: 480,
            pixelsPerTileAtZoom1: 32,
            tileX: 13.25
        );
        var projectedY = EditorMapCameraMath.ProjectTileY(
            camera,
            viewportWidth: 640,
            viewportHeight: 480,
            pixelsPerTileAtZoom1: 32,
            tileY: 28.5
        );
        var unprojectedX = EditorMapCameraMath.UnprojectTileX(
            camera,
            viewportWidth: 640,
            viewportHeight: 480,
            pixelsPerTileAtZoom1: 32,
            viewportX: projectedX
        );
        var unprojectedY = EditorMapCameraMath.UnprojectTileY(
            camera,
            viewportWidth: 640,
            viewportHeight: 480,
            pixelsPerTileAtZoom1: 32,
            viewportY: projectedY
        );

        await Assert.That(centerX).IsEqualTo(320d);
        await Assert.That(centerY).IsEqualTo(240d);
        await Assert.That(projectedX).IsEqualTo(380d);
        await Assert.That(projectedY).IsEqualTo(312d);
        await Assert.That(unprojectedX).IsEqualTo(13.25);
        await Assert.That(unprojectedY).IsEqualTo(28.5);
    }

    [Test]
    public async Task HitTestSceneSelection_MapsViewportPointBackToSectorLocalTileAndObject()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 77, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 2,
                [
                    new EditorMapObjectPreview
                    {
                        ObjectId = objectId,
                        ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                        ObjectType = ObjectType.Pc,
                        CurrentArtId = new ArtId(0x01020304u),
                        Location = new Location(10, 11),
                        RotationPitch = 0f,
                    },
                ]
            )
        );
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 96d,
            CenterTileY = 160d,
            Zoom = 1d,
        };

        var hit = EditorMapCameraMath.HitTestSceneSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.SectorAssetPath).IsEqualTo("maps/map01/sector_b.sec");
        await Assert.That(hit.Tile).IsEqualTo(new Location(10, 11));
        await Assert.That(hit.ObjectId).IsEqualTo(objectId);
    }

    [Test]
    public async Task HitTestScene_ExposesDerivedRoofCellForHitTile()
    {
        var scenePreview = CreateScenePreview(CreateSector(assetPath: "maps/map01/sector_b.sec", localX: 1, localY: 2));
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 96d,
            CenterTileY = 160d,
            Zoom = 1d,
        };

        var hit = EditorMapCameraMath.HitTestScene(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 143.25)
        );

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.Tile).IsEqualTo(new Location(10, 15));
        await Assert.That(hit.RoofCell).IsEqualTo(new Location(2, 3));
    }

    [Test]
    public async Task HitTestScene_ReturnsStackedObjectsInPreviewOrder()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 88, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 99, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 2,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(10, 11),
                    RotationPitch = 0f,
                },
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(10, 11),
                    RotationPitch = 0f,
                }
            )
        );
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 96d,
            CenterTileY = 160d,
            Zoom = 1d,
        };

        var hit = EditorMapCameraMath.HitTestScene(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );
        var selection = EditorMapCameraMath.HitTestSceneSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.MapTileX).IsEqualTo(74);
        await Assert.That(hit.MapTileY).IsEqualTo(139);
        await Assert.That(hit!.SectorAssetPath).IsEqualTo("maps/map01/sector_b.sec");
        await Assert.That(hit.Tile).IsEqualTo(new Location(10, 11));
        await Assert.That(hit.ObjectHits.Count).IsEqualTo(2);
        await Assert.That(hit.ObjectHits[0].ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(hit.ObjectHits[1].ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(selection).IsNotNull();
        await Assert.That(selection!.ObjectId).IsEqualTo(firstObjectId);
    }

    [Test]
    public async Task HitTestScene_PrefersOffsetBasedDepthBiasBeforePreviewOrder()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 188, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 199, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 2,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(10, 11),
                    OffsetY = 0,
                    OffsetZ = 0f,
                    RotationPitch = 0f,
                },
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(10, 11),
                    OffsetY = 24,
                    OffsetZ = 8f,
                    RotationPitch = 0f,
                }
            )
        );
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 96d,
            CenterTileY = 160d,
            Zoom = 1d,
        };

        var hit = EditorMapCameraMath.HitTestScene(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );
        var selection = EditorMapCameraMath.HitTestSceneSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.ObjectHits.Count).IsEqualTo(2);
        await Assert.That(hit.ObjectHits[0].ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(hit.ObjectHits[1].ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(selection).IsNotNull();
        await Assert.That(selection!.ObjectId).IsEqualTo(secondObjectId);
    }

    [Test]
    public async Task HitTestScene_PrefersResolvedSpriteBoundsWhenOffsetBiasMatches()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 288, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 299, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 2,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(10, 11),
                    CollisionHeight = 12f,
                    RotationPitch = 0f,
                },
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(10, 11),
                    SpriteBounds = new EditorMapObjectSpriteBounds
                    {
                        MaxFrameWidth = 18,
                        MaxFrameHeight = 36,
                        MaxFrameCenterX = 5,
                        MaxFrameCenterY = 20,
                    },
                    RotationPitch = 0f,
                }
            )
        );
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 96d,
            CenterTileY = 160d,
            Zoom = 1d,
        };

        var hit = EditorMapCameraMath.HitTestScene(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );
        var selection = EditorMapCameraMath.HitTestSceneSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 74.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 139.25)
        );

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.ObjectHits.Count).IsEqualTo(2);
        await Assert.That(hit.ObjectHits[0].ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(hit.ObjectHits[1].ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(selection).IsNotNull();
        await Assert.That(selection!.ObjectId).IsEqualTo(secondObjectId);
    }

    [Test]
    public async Task HitTestSceneArea_ReturnsHitsAcrossMultipleSectorsInScreenOrder()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 111, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 222, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_a.sec",
                localX: 0,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(63, 63),
                    RotationPitch = 0f,
                }
            ),
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(0, 63),
                    RotationPitch = 0f,
                }
            )
        );
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 64d,
            CenterTileY = 96d,
            Zoom = 1d,
        };

        var hits = EditorMapCameraMath.HitTestSceneArea(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportStartX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 63.1),
            viewportStartY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 127.9),
            viewportEndX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 64.9),
            viewportEndY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 126.1)
        );

        await Assert.That(hits.Count).IsEqualTo(4);
        await Assert.That(hits[0].MapTileX).IsEqualTo(63);
        await Assert.That(hits[0].MapTileY).IsEqualTo(127);
        await Assert.That(hits[0].SectorAssetPath).IsEqualTo("maps/map01/sector_a.sec");
        await Assert.That(hits[0].Tile).IsEqualTo(new Location(63, 63));
        await Assert.That(hits[0].ObjectHits.Single().ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(hits[1].MapTileX).IsEqualTo(64);
        await Assert.That(hits[1].MapTileY).IsEqualTo(127);
        await Assert.That(hits[1].SectorAssetPath).IsEqualTo("maps/map01/sector_b.sec");
        await Assert.That(hits[1].Tile).IsEqualTo(new Location(0, 63));
        await Assert.That(hits[1].ObjectHits.Single().ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(hits[2].MapTileX).IsEqualTo(63);
        await Assert.That(hits[2].MapTileY).IsEqualTo(126);
        await Assert.That(hits[3].MapTileX).IsEqualTo(64);
        await Assert.That(hits[3].MapTileY).IsEqualTo(126);
        await Assert.That(hits[2].ObjectHits.Count).IsEqualTo(0);
        await Assert.That(hits[3].ObjectHits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task HitTestSceneAreaSelection_ReturnsPrimarySelectionAndNormalizedAreaBounds()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 111, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 222, Guid.NewGuid());
        var thirdObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 333, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_a.sec",
                localX: 0,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(63, 63),
                    RotationPitch = 0f,
                },
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(63, 63),
                    RotationPitch = 0f,
                }
            ),
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = thirdObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x09101112u),
                    Location = new Location(0, 63),
                    RotationPitch = 0f,
                }
            )
        );
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 64d,
            CenterTileY = 96d,
            Zoom = 1d,
        };

        var selection = EditorMapCameraMath.HitTestSceneAreaSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportStartX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 63.1),
            viewportStartY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 127.9),
            viewportEndX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 64.9),
            viewportEndY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 126.1)
        );

        await Assert.That(selection).IsNotNull();
        await Assert.That(selection!.SectorAssetPath).IsEqualTo("maps/map01/sector_a.sec");
        await Assert.That(selection.Tile).IsEqualTo(new Location(63, 63));
        await Assert.That(selection.ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(selection.Area).IsNotNull();
        await Assert.That(selection.Area!.MinMapTileX).IsEqualTo(63);
        await Assert.That(selection.Area.MinMapTileY).IsEqualTo(126);
        await Assert.That(selection.Area.MaxMapTileX).IsEqualTo(64);
        await Assert.That(selection.Area.MaxMapTileY).IsEqualTo(127);
        await Assert.That(selection.Area.Width).IsEqualTo(2);
        await Assert.That(selection.Area.Height).IsEqualTo(2);
        await Assert.That(selection.Area.ObjectIds.Count).IsEqualTo(3);
        await Assert.That(selection.Area.ObjectIds[0]).IsEqualTo(firstObjectId);
        await Assert.That(selection.Area.ObjectIds[1]).IsEqualTo(secondObjectId);
        await Assert.That(selection.Area.ObjectIds[2]).IsEqualTo(thirdObjectId);
    }

    [Test]
    public async Task HitTestSceneArea_WhenRectangleHitsOnlyViewportGap_ReturnsEmpty()
    {
        var scenePreview = CreateScenePreview(CreateSector(assetPath: "maps/map01/sector_b.sec", localX: 1, localY: 0));
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 32d,
            CenterTileY = 32d,
            Zoom = 1d,
        };

        var hits = EditorMapCameraMath.HitTestSceneArea(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportStartX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 10.1),
            viewportStartY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 10.9),
            viewportEndX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 11.9),
            viewportEndY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 9.1)
        );
        var selection = EditorMapCameraMath.HitTestSceneAreaSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportStartX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 10.1),
            viewportStartY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 10.9),
            viewportEndX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 11.9),
            viewportEndY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 9.1)
        );

        await Assert.That(hits.Count).IsEqualTo(0);
        await Assert.That(selection).IsNull();
    }

    [Test]
    public async Task ResolveSceneAreaSelection_MapsPersistedAreaBackToSectorLocalTiles()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 411, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 422, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_a.sec",
                localX: 0,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(63, 63),
                    RotationPitch = 0f,
                }
            ),
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(0, 63),
                    RotationPitch = 0f,
                }
            )
        );
        var area = new EditorProjectMapAreaSelectionState
        {
            MinMapTileX = 63,
            MinMapTileY = 126,
            MaxMapTileX = 64,
            MaxMapTileY = 127,
        };

        var hits = EditorMapCameraMath.ResolveSceneAreaSelection(scenePreview, area);

        await Assert.That(hits.Count).IsEqualTo(4);
        await Assert.That(hits[0].SectorAssetPath).IsEqualTo("maps/map01/sector_a.sec");
        await Assert.That(hits[0].Tile).IsEqualTo(new Location(63, 63));
        await Assert.That(hits[0].ObjectHits.Single().ObjectId).IsEqualTo(firstObjectId);
        await Assert.That(hits[1].SectorAssetPath).IsEqualTo("maps/map01/sector_b.sec");
        await Assert.That(hits[1].Tile).IsEqualTo(new Location(0, 63));
        await Assert.That(hits[1].ObjectHits.Single().ObjectId).IsEqualTo(secondObjectId);
        await Assert.That(hits[2].Tile).IsEqualTo(new Location(63, 62));
        await Assert.That(hits[3].Tile).IsEqualTo(new Location(0, 62));
    }

    [Test]
    public async Task ResolveSceneAreaSelectionBySector_GroupsStableHitsPerPositionedSector()
    {
        var firstObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 511, Guid.NewGuid());
        var secondObjectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 522, Guid.NewGuid());
        var scenePreview = CreateScenePreview(
            CreateSector(
                assetPath: "maps/map01/sector_a.sec",
                localX: 0,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = firstObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Pc,
                    CurrentArtId = new ArtId(0x01020304u),
                    Location = new Location(63, 63),
                    RotationPitch = 0f,
                }
            ),
            CreateSector(
                assetPath: "maps/map01/sector_b.sec",
                localX: 1,
                localY: 1,
                new EditorMapObjectPreview
                {
                    ObjectId = secondObjectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 0, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(0x05060708u),
                    Location = new Location(0, 63),
                    RotationPitch = 0f,
                }
            )
        );
        var area = new EditorProjectMapAreaSelectionState
        {
            MinMapTileX = 63,
            MinMapTileY = 126,
            MaxMapTileX = 64,
            MaxMapTileY = 127,
        };

        var groupedHits = EditorMapCameraMath.ResolveSceneAreaSelectionBySector(scenePreview, area);

        await Assert.That(groupedHits.Count).IsEqualTo(2);
        await Assert.That(groupedHits[0].SectorAssetPath).IsEqualTo("maps/map01/sector_a.sec");
        await Assert.That(groupedHits[0].LocalX).IsEqualTo(0);
        await Assert.That(groupedHits[0].LocalY).IsEqualTo(1);
        await Assert.That(groupedHits[0].TileCount).IsEqualTo(2);
        await Assert.That(groupedHits[0].Hits[0].Tile).IsEqualTo(new Location(63, 63));
        await Assert.That(groupedHits[0].Hits[1].Tile).IsEqualTo(new Location(63, 62));
        await Assert.That(groupedHits[1].SectorAssetPath).IsEqualTo("maps/map01/sector_b.sec");
        await Assert.That(groupedHits[1].LocalX).IsEqualTo(1);
        await Assert.That(groupedHits[1].LocalY).IsEqualTo(1);
        await Assert.That(groupedHits[1].TileCount).IsEqualTo(2);
        await Assert.That(groupedHits[1].Hits[0].Tile).IsEqualTo(new Location(0, 63));
        await Assert.That(groupedHits[1].Hits[1].Tile).IsEqualTo(new Location(0, 62));
    }

    [Test]
    public async Task HitTestSceneSelection_PointInSectorGap_ReturnsNull()
    {
        var scenePreview = CreateScenePreview(CreateSector(assetPath: "maps/map01/sector_b.sec", localX: 1, localY: 0));
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 32d,
            CenterTileY = 32d,
            Zoom = 1d,
        };

        var hit = EditorMapCameraMath.HitTestSceneSelection(
            scenePreview,
            camera,
            viewportWidth: 640,
            viewportHeight: 640,
            pixelsPerTileAtZoom1: 8,
            viewportX: EditorMapCameraMath.ProjectTileX(camera, 640, 640, 8, 10.25),
            viewportY: EditorMapCameraMath.ProjectTileY(camera, 640, 640, 8, 10.25)
        );

        await Assert.That(hit).IsNull();
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

    private static EditorMapSectorScenePreview CreateSector(
        string assetPath,
        int localX,
        int localY,
        params EditorMapObjectPreview[] objects
    ) =>
        new()
        {
            AssetPath = assetPath,
            SectorX = localX,
            SectorY = localY,
            LocalX = localX,
            LocalY = localY,
            PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
            ObjectDensityBand = objects.Length == 0 ? EditorMapSectorDensityBand.None : EditorMapSectorDensityBand.Low,
            BlockedTileDensityBand = EditorMapSectorDensityBand.None,
            TileArtIds = new uint[64 * 64],
            RoofArtIds = null,
            BlockMask = new uint[128],
            Lights = [],
            TileScripts = [],
            Objects = objects,
        };
}
