using ArcNET.Formats;

namespace ArcNET.Editor.Tests;

public sealed class EditorMapPreviewBuilderTests
{
    [Test]
    public async Task Build_CombinedModePrioritizesPreviewTraitsAndReturnsTopDownRows()
    {
        var projection = MakeProjection(
            width: 4,
            height: 2,
            MakeSector(localX: 0, localY: 0, hasRoofs: true),
            MakeSector(localX: 1, localY: 0, blockedTileCount: 1),
            MakeSector(localX: 2, localY: 0),
            MakeSector(localX: 1, localY: 1, hasRoofs: true, lightCount: 1, blockedTileCount: 1, tileScriptCount: 1),
            MakeSector(localX: 2, localY: 1, lightCount: 1)
        );

        var preview = EditorMapPreviewBuilder.Build(projection, EditorMapPreviewMode.Combined);

        await Assert.That(preview.MapName).IsEqualTo("map01");
        await Assert.That(preview.Mode).IsEqualTo(EditorMapPreviewMode.Combined);
        await Assert.That(preview.Legend).IsEqualTo("S=scripted B=blocked L=lit R=roof #=other sector .=gap");
        await Assert.That(preview.Rows.Count).IsEqualTo(2);
        await Assert.That(preview.Rows[0]).IsEqualTo(".SL.");
        await Assert.That(preview.Rows[1]).IsEqualTo("RB#.");
    }

    [Test]
    public async Task Build_ObjectsModeUsesNormalizedDensityBands()
    {
        var projection = MakeProjection(
            width: 5,
            height: 1,
            MakeSector(localX: 0, localY: 0, objectDensityBand: EditorMapSectorDensityBand.None),
            MakeSector(localX: 1, localY: 0, objectDensityBand: EditorMapSectorDensityBand.Low),
            MakeSector(localX: 2, localY: 0, objectDensityBand: EditorMapSectorDensityBand.Medium),
            MakeSector(localX: 3, localY: 0, objectDensityBand: EditorMapSectorDensityBand.High),
            MakeSector(localX: 4, localY: 0, objectDensityBand: EditorMapSectorDensityBand.Peak)
        );

        var preview = EditorMapPreviewBuilder.Build(projection, EditorMapPreviewMode.Objects);

        await Assert.That(preview.Legend).IsEqualTo("4=peak 3=high 2=medium 1=low 0=empty sector .=gap");
        await Assert.That(preview.Rows.Count).IsEqualTo(1);
        await Assert.That(preview.Rows[0]).IsEqualTo("01234");
    }

    private static EditorMapProjection MakeProjection(
        int width,
        int height,
        params EditorMapSectorProjection[] sectors
    ) =>
        new()
        {
            MapName = "map01",
            MinSectorX = 0,
            MinSectorY = 0,
            MaxSectorX = width == 0 ? 0 : width - 1,
            MaxSectorY = height == 0 ? 0 : height - 1,
            Width = width,
            Height = height,
            Sectors = sectors,
            UnpositionedSectorCount = 0,
        };

    private static EditorMapSectorProjection MakeSector(
        int localX,
        int localY,
        EditorMapSectorDensityBand objectDensityBand = EditorMapSectorDensityBand.None,
        EditorMapSectorDensityBand blockedTileDensityBand = EditorMapSectorDensityBand.None,
        bool hasRoofs = false,
        int lightCount = 0,
        int blockedTileCount = 0,
        int tileScriptCount = 0,
        int? sectorScriptId = null
    ) =>
        new()
        {
            Sector = new EditorSectorSummary
            {
                Asset = new EditorAssetEntry
                {
                    AssetPath = $"maps/map01/{localX}_{localY}.sec",
                    Format = FileFormat.Sector,
                    ItemCount = 1,
                    SourceKind = EditorAssetSourceKind.LooseFile,
                    SourcePath = $"test/{localX}_{localY}.sec",
                },
                MapName = "map01",
                ObjectCount = 0,
                LightCount = lightCount,
                TileScriptCount = tileScriptCount,
                SectorScriptId = sectorScriptId,
                HasRoofs = hasRoofs,
                DistinctTileArtCount = 0,
                BlockedTileCount = blockedTileCount,
                LightSchemeIndex = 0,
                MusicSchemeIndex = -1,
                AmbientSchemeIndex = -1,
            },
            SectorX = localX,
            SectorY = localY,
            LocalX = localX,
            LocalY = localY,
            ObjectDensityBand = objectDensityBand,
            BlockedTileDensityBand = blockedTileDensityBand,
        };
}
