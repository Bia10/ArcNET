using ArcNET.Formats;
using static ArcNET.Formats.Tests.SpanWriterTestHelpers;

namespace ArcNET.Formats.Tests;

/// <summary>Unit tests for <see cref="SectorFormat"/>.</summary>
public sealed class SectorFormatTests
{
    /// Writes a minimal valid sector (no lights, all-zero tiles, no roofs,
    /// version 0xAA0004, no tile-scripts, empty sector-script, default sound/block, no objects).
    private static byte[] BuildMinimalSector()
    {
        return BuildBytes(w =>
        {
            // lights count = 0
            w.WriteInt32(0);

            // 4096 tile art IDs — all zero
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);

            // roof list — non-zero = absent
            w.WriteInt32(1);

            // version placeholder
            w.WriteInt32(0xAA0004);

            // tile scripts count = 0
            w.WriteInt32(0);

            // sector script (12 bytes: flags + counters + num)
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteInt32(0);

            // 0xAA0003 block: townmap + aptitude + lightScheme + SectorSoundList(12)
            w.WriteInt32(0); // townmapInfo
            w.WriteInt32(0); // aptitudeAdjustment
            w.WriteInt32(0); // lightSchemeIdx
            w.WriteUInt32(0); // SoundList.Flags
            w.WriteInt32(-1); // SoundList.MusicSchemeIdx
            w.WriteInt32(-1); // SoundList.AmbientSchemeIdx

            // 0xAA0004 block: block mask (128 × uint32 = 512 bytes)
            for (var i = 0; i < 128; i++)
                w.WriteUInt32(0);

            // object count = 0
            w.WriteInt32(0);
        });
    }

    [Test]
    public async Task Parse_EmptySector_NoLightsOrObjects()
    {
        var bytes = BuildMinimalSector();
        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.Lights.Count).IsEqualTo(0);
        await Assert.That(sector.Objects.Count).IsEqualTo(0);
        await Assert.That(sector.Tiles.Length).IsEqualTo(4096);
        await Assert.That(sector.HasRoofs).IsFalse();
    }

    [Test]
    public async Task Parse_TilesPreserved()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0); // no lights
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32((uint)i); // distinct art ID per tile
            w.WriteInt32(1); // no roofs
            w.WriteInt32(0xAA0000); // minimal version
            w.WriteInt32(0); // no objects
        });

        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.Tiles[0]).IsEqualTo(0u);
        await Assert.That(sector.Tiles[100]).IsEqualTo(100u);
        await Assert.That(sector.Tiles[4095]).IsEqualTo(4095u);
    }

    [Test]
    public async Task Parse_RoofListPresent_RoofsLoaded()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0); // no lights
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);
            w.WriteInt32(0); // zero = roofs PRESENT
            for (var i = 0; i < 256; i++)
                w.WriteUInt32((uint)(i + 1)); // roof art IDs 1..256
            w.WriteInt32(0xAA0000);
            w.WriteInt32(0);
        });

        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.HasRoofs).IsTrue();
        await Assert.That(sector.Roofs).IsNotNull();
        await Assert.That(sector.Roofs![0]).IsEqualTo(1u);
        await Assert.That(sector.Roofs![255]).IsEqualTo(256u);
    }

    [Test]
    public async Task Parse_SingleLight_48BytesRead()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(1); // one light
            // LightSerializedData — 48 bytes
            w.WriteInt64(-1L); // objHandle (standalone)
            w.WriteInt64(0x0000_0002_0000_0001L); // tileLoc X=1, Y=2
            w.WriteInt32(10); // offsetX
            w.WriteInt32(20); // offsetY
            w.WriteUInt32(0x08); // flags (LF_INDOOR)
            w.WriteUInt32(99); // artId
            w.WriteByte(255); // R
            w.WriteByte(128); // B
            w.WriteByte(64); // G
            w.WriteByte(0); // padding
            w.WriteUInt32(0xAABBCCDD); // tintColor
            w.WriteInt32(42); // palette
            w.WriteInt32(0); // padding_2C

            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);
            w.WriteInt32(1); // no roofs
            w.WriteInt32(0xAA0000);
            w.WriteInt32(0); // object count at end
        });

        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.Lights.Count).IsEqualTo(1);
        var light = sector.Lights[0];
        await Assert.That(light.ObjHandle).IsEqualTo(-1L);
        await Assert.That(light.TileX).IsEqualTo(1);
        await Assert.That(light.TileY).IsEqualTo(2);
        await Assert.That(light.OffsetX).IsEqualTo(10);
        await Assert.That(light.R).IsEqualTo((byte)255);
        await Assert.That(light.TintColor).IsEqualTo(0xAABBCCDD);
        await Assert.That(light.Palette).IsEqualTo(42);
    }

    [Test]
    public async Task Parse_TileScript_24BytesRead()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0); // no lights
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);
            w.WriteInt32(1); // no roofs
            w.WriteInt32(0xAA0001);

            // one tile script — 24 bytes (TileScriptListNodeSerializedData)
            w.WriteInt32(1); // count
            w.WriteUInt32(0x03u); // NodeFlags
            w.WriteUInt32(42u); // TileId
            w.WriteUInt32(0x01u); // ScriptFlags
            w.WriteUInt32(0x02u); // ScriptCounters
            w.WriteInt32(7); // ScriptNum
            w.WriteInt32(0); // next (always 0)

            // sector script (needed for 0xAA0002 but 0xAA0001 stops after tile scripts)
            w.WriteInt32(0); // object count
        });

        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.TileScripts.Count).IsEqualTo(1);
        var ts = sector.TileScripts[0];
        await Assert.That(ts.NodeFlags).IsEqualTo(0x03u);
        await Assert.That(ts.TileId).IsEqualTo(42u);
        await Assert.That(ts.ScriptFlags).IsEqualTo(0x01u);
        await Assert.That(ts.ScriptCounters).IsEqualTo(0x02u);
        await Assert.That(ts.ScriptNum).IsEqualTo(7);
    }

    [Test]
    public async Task Parse_SoundList_Preserved()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0);
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);
            w.WriteInt32(1); // no roofs
            w.WriteInt32(0xAA0003);
            w.WriteInt32(0); // tile scripts count
            // sector script
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteInt32(0);
            // 0xAA0003 block
            w.WriteInt32(5); // townmapInfo
            w.WriteInt32(2); // aptitudeAdjustment
            w.WriteInt32(3); // lightSchemeIdx
            w.WriteUInt32(7u); // SoundList.Flags
            w.WriteInt32(11); // SoundList.MusicSchemeIdx
            w.WriteInt32(22); // SoundList.AmbientSchemeIdx
            w.WriteInt32(0); // object count
        });

        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.TownmapInfo).IsEqualTo(5);
        await Assert.That(sector.AptitudeAdjustment).IsEqualTo(2);
        await Assert.That(sector.LightSchemeIdx).IsEqualTo(3);
        await Assert.That(sector.SoundList.Flags).IsEqualTo(7u);
        await Assert.That(sector.SoundList.MusicSchemeIdx).IsEqualTo(11);
        await Assert.That(sector.SoundList.AmbientSchemeIdx).IsEqualTo(22);
    }

    [Test]
    public async Task Parse_BlockMask_Preserved()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0);
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);
            w.WriteInt32(1);
            w.WriteInt32(0xAA0004);
            w.WriteInt32(0); // tile scripts
            w.WriteUInt32(0);
            w.WriteUInt32(0);
            w.WriteInt32(0); // sector script
            w.WriteInt32(0);
            w.WriteInt32(0);
            w.WriteInt32(0); // townmap/apt/light
            w.WriteUInt32(0);
            w.WriteInt32(-1);
            w.WriteInt32(-1); // sound list
            // block mask — first slot filled
            w.WriteUInt32(0xDEADBEEF);
            for (var i = 1; i < 128; i++)
                w.WriteUInt32(0);
            w.WriteInt32(0);
        });

        var sector = SectorFormat.ParseMemory(bytes);

        await Assert.That(sector.BlockMask[0]).IsEqualTo(0xDEADBEEF);
        await Assert.That(sector.BlockMask[1]).IsEqualTo(0u);
    }

    [Test]
    public async Task InvalidVersion_Throws()
    {
        var bytes = BuildBytes(w =>
        {
            w.WriteInt32(0);
            for (var i = 0; i < 4096; i++)
                w.WriteUInt32(0);
            w.WriteInt32(1);
            w.WriteInt32(0xDEAD); // bad version
        });

        Assert.Throws<InvalidDataException>(() => SectorFormat.ParseMemory(bytes));
    }

    [Test]
    public async Task RoundTrip_MinimalSector_AllFieldsUnchanged()
    {
        var bytes = BuildMinimalSector();
        var original = SectorFormat.ParseMemory(bytes);
        var rewritten = SectorFormat.WriteToArray(in original);
        var back = SectorFormat.ParseMemory(rewritten);

        await Assert.That(back.Lights.Count).IsEqualTo(original.Lights.Count);
        await Assert.That(back.HasRoofs).IsEqualTo(original.HasRoofs);
        await Assert.That(back.TileScripts.Count).IsEqualTo(original.TileScripts.Count);
        await Assert.That(back.SoundList.MusicSchemeIdx).IsEqualTo(original.SoundList.MusicSchemeIdx);
        await Assert.That(back.BlockMask[0]).IsEqualTo(original.BlockMask[0]);
        await Assert.That(back.Objects.Count).IsEqualTo(original.Objects.Count);

        for (var i = 0; i < 4096; i++)
            await Assert.That(back.Tiles[i]).IsEqualTo(original.Tiles[i]);
    }

    [Test]
    public async Task GetSectorLoc_TileCoordinates_CorrectSectorKey()
    {
        // Sector (0,0) covers tiles 0–63.
        await Assert.That(Sector.GetSectorLoc(0, 0)).IsEqualTo(0u);
        // Sector (1,0): tile 64 → sectorX=1, sectorY=0 → key = 1
        await Assert.That(Sector.GetSectorLoc(64, 0)).IsEqualTo(1u);
        // Sector (0,1): tile 0,64 → sectorX=0, sectorY=1 → key = 1<<26
        await Assert.That(Sector.GetSectorLoc(0, 64)).IsEqualTo(1u << 26);
        // Sector (1,1): tile 64,64 → key = (1<<26) | 1
        await Assert.That(Sector.GetSectorLoc(64, 64)).IsEqualTo((1u << 26) | 1u);
    }
}
