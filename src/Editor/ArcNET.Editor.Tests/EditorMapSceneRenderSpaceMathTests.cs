using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapSceneRenderSpaceMathTests
{
    [Test]
    public async Task CreateViewportState_ProjectsTileCameraIntoIsometricRenderSpace()
    {
        var sceneRender = CreateIsometricSceneRender();
        var camera = new EditorProjectMapCameraState
        {
            CenterTileX = 1d,
            CenterTileY = 0d,
            Zoom = 2d,
        };

        var viewportState = EditorMapSceneRenderSpaceMath.CreateViewportState(sceneRender, camera);
        var tileCenter = EditorMapSceneRenderSpaceMath.ProjectMapTileCenter(sceneRender, 1d, 0d);

        await Assert.That(viewportState.CenterRenderX).IsEqualTo(tileCenter.X);
        await Assert.That(viewportState.CenterRenderY).IsEqualTo(tileCenter.Y);
        await Assert.That(viewportState.Zoom).IsEqualTo(2d);
    }

    [Test]
    public async Task HitTestSceneSelection_RoundTripsViewportRenderAndMapTile()
    {
        var sceneRender = CreateIsometricSceneRender();
        var viewportState = EditorMapSceneRenderSpaceMath.CreateViewportState(
            sceneRender,
            new EditorProjectMapCameraState
            {
                CenterTileX = 0d,
                CenterTileY = 0d,
                Zoom = 2d,
            }
        );
        var layout = EditorMapSceneRenderSpaceMath.CreateViewportLayout(
            sceneRender,
            viewportWidth: 320d,
            viewportHeight: 200d,
            viewportState
        );
        var renderPoint = EditorMapSceneRenderSpaceMath.ProjectMapTileCenter(sceneRender, 1d, 0d);
        var viewportX = EditorMapSceneRenderSpaceMath.RenderToViewportX(layout, renderPoint.X);
        var viewportY = EditorMapSceneRenderSpaceMath.RenderToViewportY(layout, renderPoint.Y);

        var tilePoint = EditorMapSceneRenderSpaceMath.UnprojectMapTile(sceneRender, renderPoint.X, renderPoint.Y);
        var hit = EditorMapSceneRenderSpaceMath.HitTestScene(sceneRender, layout, viewportX, viewportY);
        var selection = EditorMapSceneRenderSpaceMath.HitTestSceneSelection(sceneRender, layout, viewportX, viewportY);

        await Assert.That(tilePoint.MapTileX).IsEqualTo(1d);
        await Assert.That(tilePoint.MapTileY).IsEqualTo(0d);
        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.MapTileX).IsEqualTo(1);
        await Assert.That(hit.MapTileY).IsEqualTo(0);
        await Assert.That(hit.Tile).IsEqualTo(new Location(1, 0));
        await Assert.That(hit.HasObjectHits).IsTrue();
        await Assert.That(hit.ObjectHits[0].MapTileX).IsEqualTo(1);
        await Assert.That(selection).IsNotNull();
        await Assert.That(selection!.SectorAssetPath).IsEqualTo("maps/map01/sector_1.sec");
        await Assert.That(selection.Tile).IsEqualTo(new Location(1, 0));
        await Assert.That(selection.ObjectId).IsEqualTo(hit.ObjectHits[0].ObjectId);
    }

    [Test]
    public async Task TopDownProjection_RoundTripsCeMapY()
    {
        var sceneRender = CreateTopDownSceneRender();
        var renderPoint = EditorMapSceneRenderSpaceMath.ProjectMapTileCenter(sceneRender, 0d, 1d);
        var tilePoint = EditorMapSceneRenderSpaceMath.UnprojectMapTile(sceneRender, renderPoint.X, renderPoint.Y);
        var viewportState = EditorMapSceneRenderSpaceMath.CreateViewportState(
            sceneRender,
            new EditorProjectMapCameraState
            {
                CenterTileX = 0d,
                CenterTileY = 1d,
                Zoom = 1d,
            }
        );
        var layout = EditorMapSceneRenderSpaceMath.CreateViewportLayout(
            sceneRender,
            viewportWidth: 128d,
            viewportHeight: 96d,
            viewportState
        );
        var hit = EditorMapSceneRenderSpaceMath.HitTestScene(sceneRender, layout, viewportX: 64d, viewportY: 48d);

        await Assert.That(renderPoint.X).IsEqualTo(16d);
        await Assert.That(renderPoint.Y).IsEqualTo(48d);
        await Assert.That(tilePoint.MapTileX).IsEqualTo(0d);
        await Assert.That(tilePoint.MapTileY).IsEqualTo(1d);
        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.MapTileX).IsEqualTo(0);
        await Assert.That(hit.MapTileY).IsEqualTo(1);
        await Assert.That(hit.Tile).IsEqualTo(new Location(0, 1));
    }

    [Test]
    public async Task HitTestScene_UsesCeWallLayoutCenterForObjectHits()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 42, Guid.NewGuid());
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 512d,
            TileHeightPixels = 512d,
            WidthPixels = 512d,
            HeightPixels = 512d,
            Tiles =
            [
                new EditorMapFloorTileRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    MapTileX = 0,
                    MapTileY = 0,
                    Tile = new Location(0, 0),
                    ArtId = new ArtId(100u),
                    IsBlocked = false,
                    HasLight = false,
                    HasScript = false,
                    DrawOrder = 0,
                    CenterX = 256d,
                    CenterY = 256d,
                },
            ],
            Objects =
            [
                new EditorMapObjectRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    ObjectId = objectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 2001, Guid.Empty),
                    ObjectType = ObjectType.Wall,
                    CurrentArtId = new ArtId(0x10000000u),
                    MapTileX = 0,
                    MapTileY = 0,
                    Tile = new Location(0, 0),
                    DrawOrder = 1,
                    AnchorX = 100d,
                    AnchorY = 100d,
                    SpriteBounds = new EditorMapObjectSpriteBounds
                    {
                        MaxFrameWidth = 100,
                        MaxFrameHeight = 80,
                        MaxFrameCenterX = 30,
                        MaxFrameCenterY = 40,
                    },
                    IsTileGridSnapped = true,
                    Rotation = 0f,
                    RotationPitch = 0f,
                },
            ],
            Overlays = [],
            Roofs = [],
            RenderQueue = [],
        };
        var viewportState = EditorMapSceneRenderSpaceMath.CreateViewportState(
            sceneRender,
            new EditorProjectMapCameraState
            {
                CenterTileX = 0d,
                CenterTileY = 0d,
                Zoom = 1d,
            }
        );
        var layout = EditorMapSceneRenderSpaceMath.CreateViewportLayout(
            sceneRender,
            viewportWidth: 512d,
            viewportHeight: 512d,
            viewportState
        );
        var viewportX = EditorMapSceneRenderSpaceMath.RenderToViewportX(layout, 190d);
        var viewportY = EditorMapSceneRenderSpaceMath.RenderToViewportY(layout, 100d);

        var hit = EditorMapSceneRenderSpaceMath.HitTestScene(sceneRender, layout, viewportX, viewportY);

        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.ObjectHits.Count).IsEqualTo(1);
        await Assert.That(hit.ObjectHits[0].ObjectId).IsEqualTo(objectId);
    }

    private static EditorMapFloorRenderPreview CreateIsometricSceneRender()
    {
        var objectId = new GameObjectGuid(GameObjectGuid.OidTypeGuid, 0, 41, Guid.NewGuid());

        return new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.Isometric,
            TileWidthPixels = 64d,
            TileHeightPixels = 32d,
            WidthPixels = 160d,
            HeightPixels = 96d,
            Tiles =
            [
                new EditorMapFloorTileRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    MapTileX = 0,
                    MapTileY = 0,
                    Tile = new Location(0, 0),
                    ArtId = new ArtId(100u),
                    IsBlocked = false,
                    HasLight = false,
                    HasScript = false,
                    DrawOrder = 0,
                    CenterX = 64d,
                    CenterY = 16d,
                },
                new EditorMapFloorTileRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    MapTileX = 1,
                    MapTileY = 0,
                    Tile = new Location(1, 0),
                    ArtId = new ArtId(101u),
                    IsBlocked = false,
                    HasLight = false,
                    HasScript = false,
                    DrawOrder = 1,
                    CenterX = 32d,
                    CenterY = 32d,
                },
            ],
            Objects =
            [
                new EditorMapObjectRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    ObjectId = objectId,
                    ProtoId = new GameObjectGuid(GameObjectGuid.OidTypeA, 0, 1001, Guid.Empty),
                    ObjectType = ObjectType.Npc,
                    CurrentArtId = new ArtId(200u),
                    MapTileX = 1,
                    MapTileY = 0,
                    Tile = new Location(1, 0),
                    DrawOrder = 2,
                    AnchorX = 32d,
                    AnchorY = 32d,
                    IsTileGridSnapped = true,
                    Rotation = 0f,
                    RotationPitch = 0f,
                },
            ],
            Overlays = [],
            Roofs = [],
            RenderQueue = [],
        };
    }

    private static EditorMapFloorRenderPreview CreateTopDownSceneRender() =>
        new()
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.TopDown,
            TileWidthPixels = 32d,
            TileHeightPixels = 32d,
            WidthPixels = 32d,
            HeightPixels = 64d,
            Tiles =
            [
                new EditorMapFloorTileRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    MapTileX = 0,
                    MapTileY = 0,
                    Tile = new Location(0, 0),
                    ArtId = new ArtId(100u),
                    IsBlocked = false,
                    HasLight = false,
                    HasScript = false,
                    DrawOrder = 1,
                    CenterX = 16d,
                    CenterY = 16d,
                },
                new EditorMapFloorTileRenderItem
                {
                    SectorAssetPath = "maps/map01/sector_1.sec",
                    MapTileX = 0,
                    MapTileY = 1,
                    Tile = new Location(0, 1),
                    ArtId = new ArtId(101u),
                    IsBlocked = false,
                    HasLight = false,
                    HasScript = false,
                    DrawOrder = 0,
                    CenterX = 16d,
                    CenterY = 48d,
                },
            ],
            Objects = [],
            Overlays = [],
            Roofs = [],
            RenderQueue = [],
        };
}
