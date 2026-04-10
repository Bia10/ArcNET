using System.Text;
using ArcNET.Core;

namespace ArcNET.Formats;

/// <summary>A single walkability entry in a FacadeWalk file.</summary>
/// <param name="X">Tile X position.</param>
/// <param name="Y">Tile Y position.</param>
/// <param name="Walkable">True when the tile is walkable; false when blocked.</param>
public readonly record struct FacWalkEntry(uint X, uint Y, bool Walkable);

/// <summary>The header block of a FacadeWalk file.</summary>
/// <param name="Terrain">Index into tilename.mes for the base terrain.</param>
/// <param name="Outdoor">Whether the tile is outdoors (1 = outdoor).</param>
/// <param name="Flippable">Whether the art is horizontally flippable (1 = flippable).</param>
/// <param name="Width">Isometric facade width.</param>
/// <param name="Height">Isometric facade height.</param>
public readonly record struct FacWalkHeader(uint Terrain, uint Outdoor, uint Flippable, uint Width, uint Height);

/// <summary>The parsed contents of an Arcanum FacadeWalk file (facwalk.*).</summary>
public sealed class FacadeWalk
{
    /// <summary>Gets the file header.</summary>
    public required FacWalkHeader Header { get; init; }

    /// <summary>Gets the per-frame walkability entries.</summary>
    public required FacWalkEntry[] Entries { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum FacadeWalk files.
/// Implements both <see cref="IFormatFileReader{T}"/> and <see cref="IFormatFileWriter{T}"/>.
/// </summary>
public sealed class FacWalkFormat : IFormatFileReader<FacadeWalk>, IFormatFileWriter<FacadeWalk>
{
    private const string ExpectedMarker = "FacWalk V101  ";
    private const int MarkerLength = 14;
    private static readonly byte[] MarkerBytes = Encoding.ASCII.GetBytes(ExpectedMarker);

    /// <inheritdoc/>
    public static FacadeWalk Parse(scoped ref SpanReader reader)
    {
        var markerBytes = reader.ReadBytes(MarkerLength);
        var marker = Encoding.ASCII.GetString(markerBytes);
        if (!marker.StartsWith("FacWalk V101", StringComparison.Ordinal))
            throw new InvalidDataException($"FacWalk marker mismatch: '{marker}'");

        var terrain = reader.ReadUInt32();
        var outdoor = reader.ReadUInt32();
        var flippable = reader.ReadUInt32();
        var width = reader.ReadUInt32();
        var height = reader.ReadUInt32();
        var entryCount = reader.ReadUInt32();

        var entries = new FacWalkEntry[entryCount];
        for (var i = 0; i < entryCount; i++)
        {
            var x = reader.ReadUInt32();
            var y = reader.ReadUInt32();
            var walkable = reader.ReadUInt32() != 0;
            entries[i] = new FacWalkEntry(x, y, walkable);
        }

        return new FacadeWalk
        {
            Header = new FacWalkHeader(terrain, outdoor, flippable, width, height),
            Entries = entries,
        };
    }

    /// <inheritdoc/>
    public static FacadeWalk ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<FacWalkFormat, FacadeWalk>(memory);

    /// <inheritdoc/>
    public static FacadeWalk ParseFile(string path) => FormatIo.ParseFile<FacWalkFormat, FacadeWalk>(path);

    /// <inheritdoc/>
    public static void Write(in FacadeWalk value, ref SpanWriter writer)
    {
        // Write 14-byte marker
        writer.WriteBytes(MarkerBytes);

        writer.WriteUInt32(value.Header.Terrain);
        writer.WriteUInt32(value.Header.Outdoor);
        writer.WriteUInt32(value.Header.Flippable);
        writer.WriteUInt32(value.Header.Width);
        writer.WriteUInt32(value.Header.Height);
        writer.WriteUInt32((uint)value.Entries.Length);

        foreach (var e in value.Entries)
        {
            writer.WriteUInt32(e.X);
            writer.WriteUInt32(e.Y);
            writer.WriteUInt32(e.Walkable ? 1u : 0u);
        }
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in FacadeWalk value) =>
        FormatIo.WriteToArray<FacWalkFormat, FacadeWalk>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in FacadeWalk value, string path) =>
        FormatIo.WriteToFile<FacWalkFormat, FacadeWalk>(in value, path);
}
