using System.Numerics;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of an Arcanum town-map fog (.tmf) file.
/// The file is a raw bit-array: one bit per town-map tile, 1 = revealed.
/// </summary>
public sealed class TownMapFog
{
    /// <summary>Raw fog bit-array bytes, preserved verbatim on round-trip.</summary>
    public required byte[] RawBytes { get; init; }

    /// <summary>Total number of tiles represented by the bit-array.</summary>
    public int TotalTiles => RawBytes.Length * 8;

    /// <summary>Number of revealed tiles (set bits) in the bit-array.</summary>
    public int RevealedTiles
    {
        get
        {
            var count = 0;
            foreach (var value in RawBytes)
                count += BitOperations.PopCount((uint)value);
            return count;
        }
    }

    /// <summary>Percentage of revealed tiles in the bit-array.</summary>
    public double CoveragePercent => TotalTiles == 0 ? 0.0 : 100.0 * RevealedTiles / TotalTiles;
}

/// <summary>
/// Span-based parser and writer for Arcanum town-map fog (.tmf) files.
/// The format is an unframed raw bit-array with no header.
/// </summary>
public sealed class TownMapFogFormat : IFormatFileReader<TownMapFog>, IFormatFileWriter<TownMapFog>
{
    /// <inheritdoc/>
    public static TownMapFog Parse(scoped ref SpanReader reader) =>
        new() { RawBytes = reader.ReadBytes(reader.Remaining).ToArray() };

    /// <inheritdoc/>
    public static TownMapFog ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<TownMapFogFormat, TownMapFog>(memory);

    /// <inheritdoc/>
    public static TownMapFog ParseFile(string path) => FormatIo.ParseFile<TownMapFogFormat, TownMapFog>(path);

    /// <inheritdoc/>
    public static void Write(in TownMapFog value, ref SpanWriter writer) => writer.WriteBytes(value.RawBytes);

    /// <inheritdoc/>
    public static byte[] WriteToArray(in TownMapFog value) =>
        FormatIo.WriteToArray<TownMapFogFormat, TownMapFog>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in TownMapFog value, string path) =>
        FormatIo.WriteToFile<TownMapFogFormat, TownMapFog>(in value, path);
}
