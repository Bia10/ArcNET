using System.Buffers;
using ArcNET.Core;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>
/// A light source inside a sector.
/// Corresponds to <c>LightSerializedData</c> (40 bytes) written by <c>light_write</c> in
/// <c>arcanum-ce/src/game/light.c</c>.
/// </summary>
public sealed class SectorLight
{
    /// <summary>Handle of the object this light is attached to; <c>-1</c> when standalone.</summary>
    public required long ObjHandle { get; init; }

    /// <summary>Packed tile location: lower 32 bits = X, upper 32 bits = Y.</summary>
    public required long TileLoc { get; init; }

    /// <summary>Sub-tile X offset.</summary>
    public required int OffsetX { get; init; }

    /// <summary>Sub-tile Y offset.</summary>
    public required int OffsetY { get; init; }

    /// <summary>Light behaviour flags (LF_OFF, LF_DARK, LF_ANIMATING, LF_INDOOR, LF_OUTDOOR, …).</summary>
    public required uint Flags { get; init; }

    /// <summary>Animation art resource identifier.</summary>
    public required uint ArtId { get; init; }

    /// <summary>Red channel (0–255).</summary>
    public required byte R { get; init; }

    /// <summary>Blue channel (0–255).</summary>
    public required byte B { get; init; }

    /// <summary>Green channel (0–255).</summary>
    public required byte G { get; init; }

    /// <summary>Tint colour packed as a single <c>uint32</c>.</summary>
    public required uint TintColor { get; init; }

    /// <summary>Tile X coordinate (unpacked from <see cref="TileLoc"/>).</summary>
    public int TileX => (int)(TileLoc & 0xFFFFFFFF);

    /// <summary>Tile Y coordinate (unpacked from <see cref="TileLoc"/>).</summary>
    public int TileY => (int)((TileLoc >> 32) & 0xFFFFFFFF);
}

/// <summary>
/// A per-tile script entry inside a sector.
/// Serialised as: <c>uint32 tileId + Script (12 bytes)</c> = 16 bytes total.
/// Source: <c>arcanum-ce/src/game/sector.c</c> <c>tile_script_list_save</c>.
/// </summary>
public sealed class TileScript
{
    /// <summary>Tile index within the sector (0–4095).</summary>
    public required uint TileId { get; init; }

    /// <summary>Script header flags.</summary>
    public required uint ScriptFlags { get; init; }

    /// <summary>Script header counters bitmask.</summary>
    public required uint ScriptCounters { get; init; }

    /// <summary>Script identifier.</summary>
    public required int ScriptNum { get; init; }
}

/// <summary>
/// Sound configuration for a sector.
/// Corresponds to <c>SectorSoundList</c> (12 bytes) in <c>arcanum-ce/src/game/sector.h</c>.
/// </summary>
public sealed class SectorSoundList
{
    /// <summary>Runtime flags (not used by editor tools).</summary>
    public required uint Flags { get; init; }

    /// <summary>Music scheme index (-1 = none).</summary>
    public required int MusicSchemeIdx { get; init; }

    /// <summary>Ambient sound scheme index (-1 = none).</summary>
    public required int AmbientSchemeIdx { get; init; }

    /// <summary>Returns a default (silent) sound list.</summary>
    public static SectorSoundList Default =>
        new()
        {
            Flags = 0,
            MusicSchemeIdx = -1,
            AmbientSchemeIdx = -1,
        };
}

/// <summary>A parsed Arcanum sector (.sec) file (editor format).</summary>
public sealed class Sector
{
    /// <summary>Light sources in this sector.</summary>
    public required IReadOnlyList<SectorLight> Lights { get; init; }

    /// <summary>4096 tile art IDs, one per tile in row-major order.</summary>
    public required uint[] Tiles { get; init; }

    /// <summary>
    /// <see langword="true"/> when this sector has roof tile art.
    /// When <see langword="true"/>, <see cref="Roofs"/> contains 256 art IDs.
    /// </summary>
    public required bool HasRoofs { get; init; }

    /// <summary>
    /// 256 roof tile art IDs, or <see langword="null"/> when <see cref="HasRoofs"/> is
    /// <see langword="false"/>.
    /// </summary>
    public required uint[]? Roofs { get; init; }

    /// <summary>Script attached to the sector itself (may be <see langword="null"/>).</summary>
    public GameObjectScript? SectorScript { get; init; }

    /// <summary>Per-tile script entries.</summary>
    public required IReadOnlyList<TileScript> TileScripts { get; init; }

    /// <summary>
    /// Townmap display flag (int32).
    /// Non-zero instructs the engine to pre-cache art for this sector when rendering the townmap.
    /// Source: <c>arcanum-ce/src/game/sector.c</c> — <c>townmap_info</c> field;
    /// <c>sector_precache_art</c> is called when this value is non-zero.
    /// </summary>
    public required int TownmapInfo { get; init; }

    /// <summary>
    /// Encounter aptitude adjustment for this sector (int32).
    /// Added to the global aptitude value when computing random encounter chance.
    /// Source: <c>arcanum-ce/src/game/sector.c</c> — <c>aptitude_adj</c> field.
    /// </summary>
    public required int AptitudeAdjustment { get; init; }

    /// <summary>
    /// Light scheme index for this sector (int32).
    /// Indexes into the engine's registered light-scheme table.
    /// Source: <c>arcanum-ce/src/game/sector.c</c> — <c>light_scheme</c> field.
    /// </summary>
    public required int LightSchemeIdx { get; init; }

    /// <summary>Background sound configuration for the sector.</summary>
    public required SectorSoundList SoundList { get; init; }

    /// <summary>
    /// 128 × uint32 tile-blocking bitmask (4096 bits total, one per tile).
    /// A set bit marks the corresponding tile as blocked.
    /// </summary>
    public required uint[] BlockMask { get; init; }

    /// <summary>Static game objects placed in this sector.</summary>
    public required IReadOnlyList<MobData> Objects { get; init; }

    /// <summary>
    /// Computes the sector location key for tile coordinates.
    /// Sectors are 64×64 tiles; the key packs sector-grid X (bits 0–4) and Y (bits 5–9).
    /// </summary>
    /// <param name="x">Tile X coordinate (0–959 on ship maps).</param>
    /// <param name="y">Tile Y coordinate (0–959 on ship maps).</param>
    public static uint GetSectorLoc(int x, int y) => ((uint)(y >> 6) << 5) | (uint)(x >> 6);
}

/// <summary>
/// Span-based parser and writer for Arcanum sector (.sec) files (editor format).
/// Write order: lights → tiles → roofs → version(0xAA0004) → tile scripts →
/// sector script → townmap/aptitude/light-scheme → sound list → block mask → objects.
/// Source: <c>arcanum-ce/src/game/sector.c</c> <c>sector_save_editor_internal</c> /
/// <c>sector_load_editor</c>.
/// </summary>
public sealed class SectorFormat : IFormatReader<Sector>, IFormatWriter<Sector>
{
    private const int TileCount = 4096;
    private const int RoofCount = 256;
    private const int BlockMaskUints = 128;
    private const int LatestVersion = 0xAA0004;

    /// <inheritdoc/>
    public static Sector Parse(scoped ref SpanReader reader)
    {
        var lights = ReadLights(ref reader);
        var tiles = ReadTiles(ref reader);
        var (hasRoofs, roofs) = ReadRoofList(ref reader);

        var version = reader.ReadInt32();
        if (version < 0xAA0000 || version > 0xAA0004)
            throw new InvalidDataException($"Unsupported sector version: 0x{version:X8}");

        List<TileScript> tileScripts = [];
        GameObjectScript? sectorScript = null;
        var townmapInfo = 0;
        var aptitudeAdj = 0;
        var lightSchemeIdx = 0;
        var soundList = SectorSoundList.Default;
        var blockMask = new uint[BlockMaskUints];
        List<MobData> objects = [];

        if (version >= 0xAA0001)
            tileScripts = ReadTileScripts(ref reader);

        if (version >= 0xAA0002)
        {
            var script = GameObjectScript.Read(ref reader);
            if (!script.IsEmpty)
                sectorScript = script;
        }

        if (version >= 0xAA0003)
        {
            townmapInfo = reader.ReadInt32();
            aptitudeAdj = reader.ReadInt32();
            lightSchemeIdx = reader.ReadInt32();
            soundList = ReadSoundList(ref reader);
        }

        if (version >= 0xAA0004)
            blockMask = ReadBlockMask(ref reader);

        objects = ReadObjects(ref reader);

        return new Sector
        {
            Lights = lights,
            Tiles = tiles,
            HasRoofs = hasRoofs,
            Roofs = roofs,
            SectorScript = sectorScript,
            TileScripts = tileScripts,
            TownmapInfo = townmapInfo,
            AptitudeAdjustment = aptitudeAdj,
            LightSchemeIdx = lightSchemeIdx,
            SoundList = soundList,
            BlockMask = blockMask,
            Objects = objects,
        };
    }

    /// <inheritdoc/>
    public static Sector ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static Sector ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

    /// <inheritdoc/>
    public static void Write(in Sector value, ref SpanWriter writer)
    {
        WriteLights(value.Lights, ref writer);
        WriteTiles(value.Tiles, ref writer);
        WriteRoofList(value.HasRoofs, value.Roofs, ref writer);

        writer.WriteInt32(LatestVersion);

        WriteTileScripts(value.TileScripts, ref writer);

        // Sector script (always emit — empty if absent so 0xAA0002+ reads correctly)
        var script =
            value.SectorScript
            ?? new GameObjectScript
            {
                Counters = [0, 0, 0, 0],
                Flags = 0,
                ScriptId = 0,
            };
        script.Write(ref writer);

        // 0xAA0003 block
        writer.WriteInt32(value.TownmapInfo);
        writer.WriteInt32(value.AptitudeAdjustment);
        writer.WriteInt32(value.LightSchemeIdx);
        WriteSoundList(value.SoundList, ref writer);

        // 0xAA0004 block
        WriteBlockMask(value.BlockMask, ref writer);

        // Objects
        WriteObjects(value.Objects, ref writer);
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in Sector value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in Sector value, string path) => File.WriteAllBytes(path, WriteToArray(in value));

    // ── Readers ───────────────────────────────────────────────────────────────

    private static List<SectorLight> ReadLights(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var lights = new List<SectorLight>(count);
        for (var i = 0; i < count; i++)
            lights.Add(ReadLight(ref reader));
        return lights;
    }

    private static SectorLight ReadLight(ref SpanReader reader)
    {
        // LightSerializedData — 40 bytes (arcanum-ce light.c `light_write`)
        var objHandle = reader.ReadInt64(); // 0x00  int64 — attached obj handle
        var tileLoc = reader.ReadInt64(); // 0x08  int64 — LOCATION_MAKE(x,y)
        var offsetX = reader.ReadInt32(); // 0x10  int
        var offsetY = reader.ReadInt32(); // 0x14  int
        var flags = reader.ReadUInt32(); // 0x18  unsigned int
        var artId = reader.ReadUInt32(); // 0x1C  tig_art_id_t
        var r = reader.ReadByte(); // 0x20  uint8
        var b = reader.ReadByte(); // 0x21  uint8
        var g = reader.ReadByte(); // 0x22  uint8
        reader.ReadByte(); // 0x23  padding — discard
        var tintColor = reader.ReadUInt32(); // 0x24  tig_color_t
        // Total: 8+8+4+4+4+4+1+1+1+1+4 = 40 bytes

        return new SectorLight
        {
            ObjHandle = objHandle,
            TileLoc = tileLoc,
            OffsetX = offsetX,
            OffsetY = offsetY,
            Flags = flags,
            ArtId = artId,
            R = r,
            B = b,
            G = g,
            TintColor = tintColor,
        };
    }

    private static uint[] ReadTiles(ref SpanReader reader)
    {
        var tiles = new uint[TileCount];
        for (var i = 0; i < TileCount; i++)
            tiles[i] = reader.ReadUInt32();
        return tiles;
    }

    private static (bool hasRoofs, uint[]? roofs) ReadRoofList(ref SpanReader reader)
    {
        // empty == 0 means roofs are present; non-zero means no roofs
        var empty = reader.ReadInt32();
        if (empty != 0)
            return (false, null);

        var roofs = new uint[RoofCount];
        for (var i = 0; i < RoofCount; i++)
            roofs[i] = reader.ReadUInt32();
        return (true, roofs);
    }

    private static List<TileScript> ReadTileScripts(ref SpanReader reader)
    {
        // Each node: uint32 tileId + Script(12 bytes) = 16 bytes
        var count = reader.ReadInt32();
        var scripts = new List<TileScript>(count);
        for (var i = 0; i < count; i++)
        {
            scripts.Add(
                new TileScript
                {
                    TileId = reader.ReadUInt32(), // uint32 id
                    ScriptFlags = reader.ReadUInt32(), // ScriptHeader.flags
                    ScriptCounters = reader.ReadUInt32(), // ScriptHeader.counters
                    ScriptNum = reader.ReadInt32(), // Script.num
                }
            );
        }

        return scripts;
    }

    private static SectorSoundList ReadSoundList(ref SpanReader reader) =>
        new()
        {
            Flags = reader.ReadUInt32(),
            MusicSchemeIdx = reader.ReadInt32(),
            AmbientSchemeIdx = reader.ReadInt32(),
        };

    private static uint[] ReadBlockMask(ref SpanReader reader)
    {
        var mask = new uint[BlockMaskUints];
        for (var i = 0; i < BlockMaskUints; i++)
            mask[i] = reader.ReadUInt32();
        return mask;
    }

    private static List<MobData> ReadObjects(ref SpanReader reader)
    {
        if (reader.Remaining < 4)
            return [];

        var count = reader.ReadInt32();
        var objects = new List<MobData>(count);
        for (var i = 0; i < count; i++)
            objects.Add(MobFormat.Parse(ref reader));
        return objects;
    }

    // ── Writers ───────────────────────────────────────────────────────────────

    private static void WriteLights(IReadOnlyList<SectorLight> lights, ref SpanWriter writer)
    {
        writer.WriteInt32(lights.Count);
        foreach (var light in lights)
        {
            writer.WriteInt64(light.ObjHandle);
            writer.WriteInt64(light.TileLoc);
            writer.WriteInt32(light.OffsetX);
            writer.WriteInt32(light.OffsetY);
            writer.WriteUInt32(light.Flags);
            writer.WriteUInt32(light.ArtId);
            writer.WriteByte(light.R);
            writer.WriteByte(light.B);
            writer.WriteByte(light.G);
            writer.WriteByte(0); // padding
            writer.WriteUInt32(light.TintColor);
        }
    }

    private static void WriteTiles(uint[] tiles, ref SpanWriter writer)
    {
        for (var i = 0; i < TileCount; i++)
            writer.WriteUInt32(i < tiles.Length ? tiles[i] : 0u);
    }

    private static void WriteRoofList(bool hasRoofs, uint[]? roofs, ref SpanWriter writer)
    {
        if (!hasRoofs || roofs is null)
        {
            writer.WriteInt32(1); // non-zero = empty (no roofs)
            return;
        }

        writer.WriteInt32(0); // zero = roofs present
        for (var i = 0; i < RoofCount; i++)
            writer.WriteUInt32(i < roofs.Length ? roofs[i] : 0u);
    }

    private static void WriteTileScripts(IReadOnlyList<TileScript> scripts, ref SpanWriter writer)
    {
        writer.WriteInt32(scripts.Count);
        foreach (var script in scripts)
        {
            writer.WriteUInt32(script.TileId);
            writer.WriteUInt32(script.ScriptFlags);
            writer.WriteUInt32(script.ScriptCounters);
            writer.WriteInt32(script.ScriptNum);
        }
    }

    private static void WriteSoundList(SectorSoundList soundList, ref SpanWriter writer)
    {
        writer.WriteUInt32(soundList.Flags);
        writer.WriteInt32(soundList.MusicSchemeIdx);
        writer.WriteInt32(soundList.AmbientSchemeIdx);
    }

    private static void WriteBlockMask(uint[] mask, ref SpanWriter writer)
    {
        for (var i = 0; i < BlockMaskUints; i++)
            writer.WriteUInt32(i < mask.Length ? mask[i] : 0u);
    }

    private static void WriteObjects(IReadOnlyList<MobData> objects, ref SpanWriter writer)
    {
        writer.WriteInt32(objects.Count);
        foreach (var obj in objects)
            MobFormat.Write(in obj, ref writer);
    }
}
