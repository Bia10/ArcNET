using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="FacadeWalk"/> instance.
/// </summary>
public static class FacWalkDumper
{
    public static string Dump(FacadeWalk fac)
    {
        var sb = new StringBuilder();
        var h = fac.Header;
        sb.AppendLine("=== FACADE WALK ===");
        sb.AppendLine($"  Terrain    : {h.Terrain}");
        sb.AppendLine($"  Outdoor    : {(h.Outdoor != 0 ? "yes" : "no")}");
        sb.AppendLine($"  Flippable  : {(h.Flippable != 0 ? "yes" : "no")}");
        sb.AppendLine($"  Width      : {h.Width}");
        sb.AppendLine($"  Height     : {h.Height}");
        sb.AppendLine($"  Entries    : {fac.Entries.Length}");
        sb.AppendLine();

        var walkable = fac.Entries.Count(e => e.Walkable);
        var blocked = fac.Entries.Length - walkable;
        sb.AppendLine($"  Walkable   : {walkable}");
        sb.AppendLine($"  Blocked    : {blocked}");
        sb.AppendLine();

        const int maxEntriesToList = 64;
        if (fac.Entries.Length <= maxEntriesToList)
        {
            foreach (var e in fac.Entries)
                sb.AppendLine($"  ({e.X, 3},{e.Y, 3}) {(e.Walkable ? "WALK" : "BLOCKED")}");
        }
        else
        {
            sb.AppendLine($"  (listing suppressed — {fac.Entries.Length} entries; showing {h.Width}×{h.Height} grid)");
            sb.AppendLine();

            var grid = new bool[h.Height, h.Width];
            foreach (var e in fac.Entries)
                if (e.X >= 0 && e.X < h.Width && e.Y >= 0 && e.Y < h.Height)
                    grid[e.Y, e.X] = e.Walkable;

            for (var row = 0; row < h.Height; row++)
            {
                sb.Append("  ");
                for (var col = 0; col < h.Width; col++)
                    sb.Append(grid[row, col] ? '.' : '#');
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public static void Dump(FacadeWalk fac, TextWriter writer) => writer.Write(Dump(fac));
}
