using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="TerrainData"/> instance.
/// </summary>
public static class TerrainDumper
{
    public static string Dump(TerrainData terrain)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== TERRAIN FILE ===");
        sb.AppendLine($"  Version      : {terrain.Version:F1}  (standard TDF format)");
        sb.AppendLine($"  Base terrain : {terrain.BaseTerrainType} ({(int)terrain.BaseTerrainType})");
        sb.AppendLine(
            $"  Dimensions   : {terrain.Width} \u00d7 {terrain.Height} tiles  ({terrain.Tiles.Length} total)"
        );
        sb.AppendLine(
            terrain.Compressed
                ? "  Storage      : compressed (row-by-row zlib)"
                : "  Storage      : uncompressed (raw tile array)"
        );
        sb.AppendLine();

        // Terrain type distribution
        var distribution = new Dictionary<ushort, int>();
        foreach (var tile in terrain.Tiles)
        {
            distribution.TryGetValue(tile, out var count);
            distribution[tile] = count + 1;
        }

        sb.AppendLine("  Terrain Distribution:");
        foreach (var kvp in distribution.OrderByDescending(k => k.Value))
        {
            var pct = 100.0 * kvp.Value / terrain.Tiles.Length;
            var label = Enum.IsDefined((TerrainType)kvp.Key)
                ? $"{(TerrainType)kvp.Key} ({kvp.Key})"
                : $"0x{kvp.Key:X4}";
            sb.AppendLine($"    {label, -25} : {kvp.Value, 7} tiles ({pct:F1}%)");
        }

        return sb.ToString();
    }

    public static void Dump(TerrainData terrain, TextWriter writer) => writer.Write(Dump(terrain));
}
