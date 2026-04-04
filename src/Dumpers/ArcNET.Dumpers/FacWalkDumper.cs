using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="FacadeWalk"/> instance.
/// </summary>
public static class FacWalkDumper
{
    public static string Dump(FacadeWalk fac)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        var h = fac.Header;
        vsb.AppendLine("=== FACADE WALK ===");
        var terrainLabel = Enum.IsDefined((TerrainType)h.Terrain)
            ? $"{(TerrainType)h.Terrain} ({h.Terrain})"
            : h.Terrain.ToString();
        vsb.AppendLine($"  Terrain    : {terrainLabel}");
        vsb.AppendLine($"  Outdoor    : {(h.Outdoor != 0 ? "yes" : "no")}");
        vsb.AppendLine($"  Flippable  : {(h.Flippable != 0 ? "yes" : "no")}");
        vsb.AppendLine($"  Width      : {h.Width}  (isometric facade tile columns)");
        vsb.AppendLine($"  Height     : {h.Height}  (isometric facade tile rows)");
        vsb.AppendLine($"  Entries    : {fac.Entries.Length}  ({h.Width}\u00d7{h.Height} grid)");
        vsb.AppendLine();

        var walkable = fac.Entries.Count(e => e.Walkable);
        var blocked = fac.Entries.Length - walkable;
        vsb.AppendLine($"  Walkable   : {walkable} / {fac.Entries.Length} tiles  ({blocked} blocked)");
        vsb.AppendLine();

        const int maxEntriesToList = 64;
        if (fac.Entries.Length <= maxEntriesToList)
        {
            foreach (var e in fac.Entries)
                vsb.AppendLine($"  ({e.X, 3},{e.Y, 3}) {(e.Walkable ? "WALK" : "BLOCKED")}");
        }
        else
        {
            vsb.AppendLine($"  (listing suppressed — {fac.Entries.Length} entries; showing {h.Width}×{h.Height} grid)");
            vsb.AppendLine();

            var grid = new bool[h.Height, h.Width];
            foreach (var e in fac.Entries)
                if (e.X >= 0 && e.X < h.Width && e.Y >= 0 && e.Y < h.Height)
                    grid[e.Y, e.X] = e.Walkable;

            for (var row = 0; row < h.Height; row++)
            {
                vsb.Append("  ");
                for (var col = 0; col < h.Width; col++)
                    vsb.Append(grid[row, col] ? '.' : '#');
                vsb.AppendLine();
            }
        }

        return vsb.ToString();
    }

    public static void Dump(FacadeWalk fac, TextWriter writer) => writer.Write(Dump(fac));
}
