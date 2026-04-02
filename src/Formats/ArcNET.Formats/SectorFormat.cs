using System.Buffers;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

/// <summary>A light source inside a sector.</summary>
public sealed class SectorLight
{
    /// <summary>The 64-bit handle of this light.</summary>
    public required ulong Handle { get; init; }

    /// <summary>Position in tile coordinates.</summary>
    public required Location Position { get; init; }

    /// <summary>Sub-tile X offset.</summary>
    public required int OffsetX { get; init; }

    /// <summary>Sub-tile Y offset.</summary>
    public required int OffsetY { get; init; }

    /// <summary>Light flags.</summary>
    public required int Flags0 { get; init; }

    /// <summary>Art resource ID.</summary>
    public required int Art { get; init; }

    /// <summary>Primary color value.</summary>
    public required int Color0 { get; init; }

    /// <summary>Secondary color value.</summary>
    public required int Color1 { get; init; }

    /// <summary>Unknown field 0.</summary>
    public required int Unk0 { get; init; }

    /// <summary>Unknown field 1.</summary>
    public required int Unk1 { get; init; }
}

/// <summary>A script entry attached to an individual tile.</summary>
public sealed class TileScript
{
    /// <summary>Field 1.</summary>
    public required int F1 { get; init; }

    /// <summary>Field 2.</summary>
    public required int F2 { get; init; }

    /// <summary>Field 3.</summary>
    public required int F3 { get; init; }

    /// <summary>Field 4.</summary>
    public required int F4 { get; init; }

    /// <summary>Field 5.</summary>
    public required int F5 { get; init; }

    /// <summary>Field 6.</summary>
    public required int F6 { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"{F1} {F2} {F3} {F4} {F5} {F6}";
}

/// <summary>A parsed Arcanum sector (.sec) file.</summary>
public sealed class Sector
{
    /// <summary>Light sources in this sector.</summary>
    public required IReadOnlyList<SectorLight> Lights { get; init; }

    /// <summary>4096 tile data entries.</summary>
    public required uint[] Tiles { get; init; }

    /// <summary>Script attached to the sector itself.</summary>
    public GameObjectScript? SectorScript { get; init; }

    /// <summary>Per-tile script entries.</summary>
    public required IReadOnlyList<TileScript> TileScripts { get; init; }

    /// <summary>Object headers loaded from this sector.</summary>
    public required IReadOnlyList<GameObjectHeader> Objects { get; init; }

    /// <summary>Computes the sector location key for tile coordinates.</summary>
    public static uint GetSectorLoc(int x, int y) => (((uint)y << 26) & 0xFC) | ((uint)x & 0xFC);
}

/// <summary>Format class for parsing Arcanum .sec sector files.</summary>
public static class SectorFormat
{
    private const int TileCount = 4096;

    /// <summary>Parses a sector from a <see cref="SpanReader"/>.</summary>
    public static Sector Parse(scoped ref SpanReader reader)
    {
        var lights = ReadLights(ref reader);
        var tiles = ReadTiles(ref reader);
        SkipRoofList(ref reader);

        var placeholder = reader.ReadInt32();
        if (placeholder < 0xAA0000 || placeholder > 0xAA0004)
            throw new InvalidDataException($"Invalid sector placeholder value: 0x{placeholder:X8}");

        List<TileScript> tileScripts = [];
        GameObjectScript? sectorScript = null;

        if (placeholder >= 0xAA0001)
            tileScripts = ReadTileScripts(ref reader);

        if (placeholder >= 0xAA0002)
        {
            var script = GameObjectScript.Read(ref reader);
            if (!script.IsEmpty)
                sectorScript = script;
        }

        if (placeholder >= 0xAA0003)
        {
            _ = reader.ReadInt32(); // Townmap Info
            _ = reader.ReadInt32(); // Aptitude Adjustment
            _ = reader.ReadInt32(); // Light Scheme
            _ = reader.ReadBytes(12); // Sound List
        }

        if (placeholder >= 0xAA0004)
            _ = reader.ReadBytes(512);

        // Objects are at start through (length-4); count is last 4 bytes
        // Cannot seek with SpanReader — caller must slice appropriately
        var objects = new List<GameObjectHeader>();

        return new Sector
        {
            Lights = lights,
            Tiles = tiles,
            SectorScript = sectorScript,
            TileScripts = tileScripts,
            Objects = objects,
        };
    }

    /// <summary>Parses a sector file from disk.</summary>
    public static Sector ParseFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var reader = new SpanReader(bytes);
        return Parse(ref reader);
    }

    /// <summary>Parses a sector from a <see cref="ReadOnlyMemory{T}"/>.</summary>
    public static Sector ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

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
        var handle = reader.ReadUInt64();
        var position = Location.Read(ref reader);
        return new SectorLight
        {
            Handle = handle,
            Position = position,
            OffsetX = reader.ReadInt32(),
            OffsetY = reader.ReadInt32(),
            Flags0 = reader.ReadInt32(),
            Art = reader.ReadInt32(),
            Color0 = reader.ReadInt32(),
            Color1 = reader.ReadInt32(),
            Unk0 = reader.ReadInt32(),
            Unk1 = reader.ReadInt32(),
        };
    }

    private static uint[] ReadTiles(ref SpanReader reader)
    {
        var tiles = new uint[TileCount];
        for (var i = 0; i < TileCount; i++)
            tiles[i] = reader.ReadUInt32();
        return tiles;
    }

    private static void SkipRoofList(ref SpanReader reader)
    {
        var isPresent = reader.ReadInt32();
        if (isPresent == 0)
            _ = reader.ReadBytes(256 * 4);
    }

    private static List<TileScript> ReadTileScripts(ref SpanReader reader)
    {
        var count = reader.ReadInt32();
        var scripts = new List<TileScript>(count);
        for (var i = 0; i < count; i++)
        {
            scripts.Add(
                new TileScript
                {
                    F1 = reader.ReadInt32(),
                    F2 = reader.ReadInt32(),
                    F3 = reader.ReadInt32(),
                    F4 = reader.ReadInt32(),
                    F5 = reader.ReadInt32(),
                    F6 = reader.ReadInt32(),
                }
            );
        }

        return scripts;
    }

    /// <summary>Serializes a <see cref="Sector"/> to the given <paramref name="writer"/>.</summary>
    public static void Write(in Sector value, ref SpanWriter writer)
    {
        WriteLights(value.Lights, ref writer);
        WriteTiles(value.Tiles, ref writer);
        WriteRoofListAbsent(ref writer);

        var placeholder = value.SectorScript is not null ? 0xAA0002 : 0xAA0001;
        writer.WriteInt32(placeholder);

        WriteTileScripts(value.TileScripts, ref writer);

        if (value.SectorScript is not null)
            value.SectorScript.Write(ref writer);
        else
        {
            // Emit an empty script so the placeholder 0xAA0002 round-trips correctly.
            var empty = new GameObjectScript
            {
                Counters = [0, 0, 0, 0],
                Flags = 0,
                ScriptId = 0,
            };
            empty.Write(ref writer);
        }
    }

    /// <summary>Serializes a <see cref="Sector"/> to a newly-allocated byte array.</summary>
    public static byte[] WriteToArray(in Sector value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <summary>Serializes a <see cref="Sector"/> and writes the result to a file.</summary>
    public static void WriteToFile(in Sector value, string path) => File.WriteAllBytes(path, WriteToArray(in value));

    private static void WriteLights(IReadOnlyList<SectorLight> lights, ref SpanWriter writer)
    {
        writer.WriteInt32(lights.Count);
        foreach (var light in lights)
        {
            writer.WriteUInt64(light.Handle);
            writer.WriteLocation(light.Position);
            writer.WriteInt32(light.OffsetX);
            writer.WriteInt32(light.OffsetY);
            writer.WriteInt32(light.Flags0);
            writer.WriteInt32(light.Art);
            writer.WriteInt32(light.Color0);
            writer.WriteInt32(light.Color1);
            writer.WriteInt32(light.Unk0);
            writer.WriteInt32(light.Unk1);
        }
    }

    private static void WriteTiles(uint[] tiles, ref SpanWriter writer)
    {
        for (var i = 0; i < TileCount; i++)
            writer.WriteUInt32(i < tiles.Length ? tiles[i] : 0u);
    }

    private static void WriteRoofListAbsent(ref SpanWriter writer) =>
        // Non-zero value means the roof list is absent (no 256*4 bytes to follow).
        writer.WriteInt32(1);

    private static void WriteTileScripts(IReadOnlyList<TileScript> scripts, ref SpanWriter writer)
    {
        writer.WriteInt32(scripts.Count);
        foreach (var script in scripts)
        {
            writer.WriteInt32(script.F1);
            writer.WriteInt32(script.F2);
            writer.WriteInt32(script.F3);
            writer.WriteInt32(script.F4);
            writer.WriteInt32(script.F5);
            writer.WriteInt32(script.F6);
        }
    }
}
