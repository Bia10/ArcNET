using ArcNET.Core.Primitives;
using ArcNET.Editor;
using ArcNET.GameObjects;
using BenchmarkDotNet.Attributes;

namespace ArcNET.Benchmarks;

/// <summary>
/// Map-loading and composition performance benchmarks.
/// </summary>
[MemoryDiagnoser]
public class MapLoadingBench
{
    private EditorMapScenePreview? _scenePreview;

    /// <summary>
    /// Builds a synthetic multi-sector scene preview for repeatable benchmarking.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        const int sectorCount = 16;
        const int width = 4;
        const int height = 4;
        var sectors = new List<EditorMapSectorScenePreview>(sectorCount);

        var tileArtIds = new uint[64 * 64];
        // Fill with a sparse pattern: ~25% non-zero tiles.
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++)
            if ((x + y) % 4 == 0)
                tileArtIds[(y * 64) + x] = (uint)(0x40000000u | ((uint)(y * 64 + x) & 0xFFFFu));

        for (var localY = 0; localY < height; localY++)
        {
            for (var localX = 0; localX < width; localX++)
            {
                sectors.Add(
                    new EditorMapSectorScenePreview
                    {
                        AssetPath = $"maps/synthetic/sector_{localX}_{localY}.sec",
                        SectorX = localX,
                        SectorY = localY,
                        LocalX = localX,
                        LocalY = localY,
                        PreviewFlags = EditorMapSectorPreviewFlags.Occupied,
                        ObjectDensityBand = EditorMapSectorDensityBand.Low,
                        BlockedTileDensityBand = EditorMapSectorDensityBand.None,
                        TileArtIds = tileArtIds.ToArray(),
                        RoofArtIds = null,
                        BlockMask = new uint[128],
                        Lights = [],
                        TileScripts = [],
                        Objects = [],
                    }
                );
            }
        }

        _scenePreview = new EditorMapScenePreview
        {
            MapName = "synthetic_map",
            Width = width,
            Height = height,
            UnpositionedSectorCount = 0,
            Sectors = sectors,
        };
    }

    [Benchmark]
    public EditorMapFloorRenderPreview Build_FloorRender()
    {
        var request = new EditorMapFloorRenderRequest
        {
            ViewMode = EditorMapSceneViewMode.Isometric,
            TileWidthPixels = 64d,
            TileHeightPixels = 32d,
        };
        return EditorMapFloorRenderBuilder.Build(_scenePreview!, request);
    }

    [Benchmark]
    public EditorMapPaintableScene Build_PaintableScene()
    {
        var sceneRender = EditorMapFloorRenderBuilder.Build(
            _scenePreview!,
            new EditorMapFloorRenderRequest
            {
                ViewMode = EditorMapSceneViewMode.Isometric,
                TileWidthPixels = 64d,
                TileHeightPixels = 32d,
            }
        );
        return EditorMapPaintableSceneBuilder.Build(sceneRender);
    }
}
