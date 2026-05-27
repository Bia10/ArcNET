using ArcNET.Core.Primitives;
using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapFacadePaintableSceneBuilderTests
{
    [Test]
    public async Task Build_CentersFacadeWalkEntriesAroundSelectedTile()
    {
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.Isometric,
            TileWidthPixels = 80d,
            TileHeightPixels = 40d,
            WidthPixels = 1280d,
            HeightPixels = 720d,
            Tiles = [],
            Objects = [],
            ObjectAuxiliaryItems = [],
            Overlays = [],
            Lights = [],
            Roofs = [],
            RenderQueue = [],
        };
        var selection = new EditorProjectMapSelectionState { Tile = new Location(10, 20) };
        var terrainEntry = new EditorTerrainPaletteEntry
        {
            Asset = new EditorAssetEntry
            {
                AssetPath = "rules/map.prp",
                Format = FileFormat.MapProperties,
                ItemCount = 1,
                SourceKind = EditorAssetSourceKind.LooseFile,
                SourcePath = "C:/content/rules/map.prp",
            },
            BaseArtId = 0,
            LimitX = 1,
            LimitY = 1,
            PaletteX = 0,
            PaletteY = 0,
            PaletteIndex = 0,
            ArtId = new ArtId(0xB0040000u),
            ArtAssetPath = "art/facade/test.art",
        };
        var facadeWalk = new FacadeWalk
        {
            Header = new FacWalkHeader(Terrain: 0, Outdoor: 0, Flippable: 0, Width: 4, Height: 2),
            Entries = [new FacWalkEntry(0, 0, false), new FacWalkEntry(3, 1, true)],
        };

        var overlayScene = EditorMapFacadePaintableSceneBuilder.Build(sceneRender, selection, terrainEntry, facadeWalk);

        await Assert.That(overlayScene).IsNotNull();
        await Assert.That(overlayScene!.Items.Count).IsEqualTo(2);
        await Assert
            .That(overlayScene.Items.All(static item => item.Kind == EditorMapRenderQueueItemKind.FloorTile))
            .IsTrue();
        await Assert.That(overlayScene.Items[0].AnchorX).IsEqualTo(440d);
        await Assert.That(overlayScene.Items[0].AnchorY).IsEqualTo(560d);
        await Assert.That(overlayScene.Items[1].AnchorX).IsEqualTo(360d);
        await Assert.That(overlayScene.Items[1].AnchorY).IsEqualTo(640d);
    }

    [Test]
    public async Task Build_RejectsNonFacadeSelections()
    {
        var sceneRender = new EditorMapFloorRenderPreview
        {
            MapName = "map01",
            ViewMode = EditorMapSceneViewMode.Isometric,
            TileWidthPixels = 80d,
            TileHeightPixels = 40d,
            WidthPixels = 1280d,
            HeightPixels = 720d,
            Tiles = [],
            Objects = [],
            ObjectAuxiliaryItems = [],
            Overlays = [],
            Lights = [],
            Roofs = [],
            RenderQueue = [],
        };
        var selection = new EditorProjectMapSelectionState { Tile = new Location(10, 20) };
        var terrainEntry = new EditorTerrainPaletteEntry
        {
            Asset = new EditorAssetEntry
            {
                AssetPath = "rules/map.prp",
                Format = FileFormat.MapProperties,
                ItemCount = 1,
                SourceKind = EditorAssetSourceKind.LooseFile,
                SourcePath = "C:/content/rules/map.prp",
            },
            BaseArtId = 0,
            LimitX = 1,
            LimitY = 1,
            PaletteX = 0,
            PaletteY = 0,
            PaletteIndex = 0,
            ArtId = new ArtId(0x00010000u),
        };
        var facadeWalk = new FacadeWalk
        {
            Header = new FacWalkHeader(Terrain: 0, Outdoor: 0, Flippable: 0, Width: 4, Height: 2),
            Entries = [new FacWalkEntry(0, 0, false)],
        };

        var overlayScene = EditorMapFacadePaintableSceneBuilder.Build(sceneRender, selection, terrainEntry, facadeWalk);

        await Assert.That(overlayScene).IsNull();
    }
}
