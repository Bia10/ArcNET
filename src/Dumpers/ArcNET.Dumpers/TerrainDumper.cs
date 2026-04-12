using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="TerrainData"/> instance.
/// </summary>
public static class TerrainDumper
{
    public static string Dump(TerrainData terrain)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== TERRAIN FILE ===");
        vsb.AppendLine($"  Version      : {terrain.Version:F1}  (standard TDF format)");
        vsb.Append("  Base terrain : ");
        vsb.AppendLine($"{terrain.BaseTerrainType} ({(int)terrain.BaseTerrainType})");
        vsb.Append("  Dimensions   : ");
        vsb.Append(terrain.Width);
        vsb.Append(" \u00d7 ");
        vsb.Append(terrain.Height);
        vsb.Append(" tiles  (");
        vsb.Append(terrain.Tiles.Length);
        vsb.AppendLine(" total)");
        vsb.AppendLine(
            terrain.Compressed
                ? "  Storage      : compressed (row-by-row zlib)"
                : "  Storage      : uncompressed (raw tile array)"
        );
        vsb.AppendLine();

        // Terrain type distribution
        var distribution = new Dictionary<ushort, int>();
        foreach (var tile in terrain.Tiles)
        {
            distribution.TryGetValue(tile, out var count);
            distribution[tile] = count + 1;
        }

        vsb.AppendLine("  Terrain Distribution:");
        foreach (var kvp in distribution.OrderByDescending(k => k.Value))
        {
            var pct = 100.0 * kvp.Value / terrain.Tiles.Length;
            // label still needs a string because enum.ToString() allocates; the outer line does not
            var label = Enum.IsDefined((TerrainType)kvp.Key)
                ? $"{(TerrainType)kvp.Key} ({kvp.Key})"
                : $"0x{kvp.Key:X4}";
            vsb.Append("    ");
            vsb.AppendPadded(label, 25);
            vsb.Append(" : ");
            vsb.AppendPadded<int>(kvp.Value, 7, leftAlign: false);
            vsb.Append(" tiles (");
            vsb.Append(pct, "F1");
            vsb.AppendLine("%)");
        }

        return vsb.ToString();
    }

    public static void Dump(TerrainData terrain, TextWriter writer) => writer.Write(Dump(terrain));
}
