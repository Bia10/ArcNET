using ArcNET.Editor;
using ArcNET.Formats;
using ArcNET.GameObjects;

namespace ArcNET.Editor.Tests;

public class SectorBuilderTests
{
    private static SectorLight MakeLight(int tileX = 1, int tileY = 2) =>
        new()
        {
            ObjHandle = -1,
            TileLoc = (long)tileX | ((long)tileY << 32),
            OffsetX = 0,
            OffsetY = 0,
            Flags = SectorLightFlags.None,
            ArtId = 0,
            R = 255,
            B = 0,
            G = 0,
            TintColor = 0,
            Palette = 0,
            Padding2C = 0,
        };

    // ── Empty build ───────────────────────────────────────────────────────────

    [Test]
    public async Task Build_EmptySector_HasExpectedDefaults()
    {
        var sector = new SectorBuilder().Build();

        await Assert.That(sector.Lights.Count).IsEqualTo(0);
        await Assert.That(sector.Tiles.Length).IsEqualTo(4096);
        await Assert.That(sector.HasRoofs).IsFalse();
        await Assert.That(sector.Roofs).IsNull();
        await Assert.That(sector.TileScripts.Count).IsEqualTo(0);
        await Assert.That(sector.Objects.Count).IsEqualTo(0);
        await Assert.That(sector.BlockMask.Length).IsEqualTo(128);
    }

    // ── Lights ────────────────────────────────────────────────────────────────

    [Test]
    public async Task AddLight_AppendedToSector()
    {
        var sector = new SectorBuilder().AddLight(MakeLight()).Build();
        await Assert.That(sector.Lights.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveLight_ByIndex_RemovesCorrectEntry()
    {
        var light0 = MakeLight(1, 1);
        var light1 = MakeLight(2, 2);
        var sector = new SectorBuilder().AddLight(light0).AddLight(light1).RemoveLight(0).Build();

        await Assert.That(sector.Lights.Count).IsEqualTo(1);
        await Assert.That(sector.Lights[0].TileX).IsEqualTo(light1.TileX);
    }

    [Test]
    public async Task ClearLights_RemovesAllLights()
    {
        var sector = new SectorBuilder().AddLight(MakeLight(1, 1)).AddLight(MakeLight(2, 2)).ClearLights().Build();

        await Assert.That(sector.Lights.Count).IsEqualTo(0);
    }

    // ── Tiles ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task SetTile_SetsCorrectIndex()
    {
        var sector = new SectorBuilder().SetTile(5, 3, artId: 999u).Build();
        await Assert.That(sector.Tiles[3 * 64 + 5]).IsEqualTo(999u);
    }

    [Test]
    public async Task SetTile_DoesNotAffectOtherTiles()
    {
        var sector = new SectorBuilder().SetTile(5, 3, artId: 999u).Build();
        await Assert.That(sector.Tiles[0]).IsEqualTo(0u);
    }

    // ── Roofs ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task SetRoof_EnablesHasRoofsFlag()
    {
        var sector = new SectorBuilder().SetRoof(0, 0, artId: 42u).Build();
        await Assert.That(sector.HasRoofs).IsTrue();
        await Assert.That(sector.Roofs).IsNotNull();
        await Assert.That(sector.Roofs![0]).IsEqualTo(42u);
    }

    [Test]
    public async Task ClearRoofs_DisablesHasRoofsAndNullsArray()
    {
        var sector = new SectorBuilder().SetRoof(0, 0, 1u).ClearRoofs().Build();
        await Assert.That(sector.HasRoofs).IsFalse();
        await Assert.That(sector.Roofs).IsNull();
    }

    // ── Block mask ────────────────────────────────────────────────────────────

    [Test]
    public async Task SetBlocked_BlocksTile()
    {
        var sector = new SectorBuilder().SetBlocked(7, 2, true).Build();
        await Assert.That(sector.BlockMask.IsBlocked(7, 2)).IsTrue();
    }

    [Test]
    public async Task SetBlocked_False_UnblocksTile()
    {
        var sector = new SectorBuilder().SetBlocked(7, 2, true).SetBlocked(7, 2, false).Build();
        await Assert.That(sector.BlockMask.IsBlocked(7, 2)).IsFalse();
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    [Test]
    public async Task WithTownmapInfo_SetsValue()
    {
        var sector = new SectorBuilder().WithTownmapInfo(1).Build();
        await Assert.That(sector.TownmapInfo).IsEqualTo(1);
    }

    [Test]
    public async Task WithAptitudeAdjustment_SetsValue()
    {
        var sector = new SectorBuilder().WithAptitudeAdjustment(-5).Build();
        await Assert.That(sector.AptitudeAdjustment).IsEqualTo(-5);
    }

    [Test]
    public async Task WithLightSchemeIdx_SetsValue()
    {
        var sector = new SectorBuilder().WithLightSchemeIdx(3).Build();
        await Assert.That(sector.LightSchemeIdx).IsEqualTo(3);
    }

    // ── Copy-from-existing ────────────────────────────────────────────────────

    [Test]
    public async Task ConstructFromExistingSector_PreservesData()
    {
        var original = new SectorBuilder()
            .AddLight(MakeLight(5, 5))
            .SetTile(1, 1, 7u)
            .SetBlocked(3, 3, true)
            .WithTownmapInfo(2)
            .Build();

        var copy = new SectorBuilder(original).Build();

        await Assert.That(copy.Lights.Count).IsEqualTo(1);
        await Assert.That(copy.Tiles[1 * 64 + 1]).IsEqualTo(7u);
        await Assert.That(copy.BlockMask.IsBlocked(3, 3)).IsTrue();
        await Assert.That(copy.TownmapInfo).IsEqualTo(2);
    }

    [Test]
    public async Task Build_ProducesIndependentCopies()
    {
        var builder = new SectorBuilder().AddLight(MakeLight());
        var sector1 = builder.Build();
        builder.ClearLights();
        var sector2 = builder.Build();

        await Assert.That(sector1.Lights.Count).IsEqualTo(1);
        await Assert.That(sector2.Lights.Count).IsEqualTo(0);
    }

    // ── Round-trip through SectorFormat ───────────────────────────────────────

    [Test]
    public async Task Build_RoundTripsThroughSectorFormat()
    {
        var sector = new SectorBuilder()
            .AddLight(MakeLight(10, 20))
            .SetTile(0, 0, 5u)
            .SetBlocked(1, 1, true)
            .WithTownmapInfo(1)
            .WithAptitudeAdjustment(3)
            .WithLightSchemeIdx(2)
            .Build();

        var bytes = SectorFormat.WriteToArray(in sector);
        var reparsed = SectorFormat.ParseMemory(bytes);

        await Assert.That(reparsed.Lights.Count).IsEqualTo(1);
        await Assert.That(reparsed.Tiles[0]).IsEqualTo(5u);
        await Assert.That(reparsed.BlockMask.IsBlocked(1, 1)).IsTrue();
        await Assert.That(reparsed.TownmapInfo).IsEqualTo(1);
        await Assert.That(reparsed.AptitudeAdjustment).IsEqualTo(3);
        await Assert.That(reparsed.LightSchemeIdx).IsEqualTo(2);
    }
}
