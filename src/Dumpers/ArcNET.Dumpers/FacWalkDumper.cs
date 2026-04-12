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
        vsb.Append("  Terrain    : ");
        if (Enum.IsDefined((TerrainType)h.Terrain))
            vsb.AppendLine($"{(TerrainType)h.Terrain} ({h.Terrain})");
        else
            vsb.AppendLine(h.Terrain);
        vsb.Append("  Outdoor    : ");
        vsb.AppendLine(h.Outdoor != 0 ? "yes" : "no");
        vsb.Append("  Flippable  : ");
        vsb.AppendLine(h.Flippable != 0 ? "yes" : "no");
        vsb.Append("  Width      : ");
        vsb.Append(h.Width);
        vsb.AppendLine("  (isometric facade tile columns)");
        vsb.Append("  Height     : ");
        vsb.Append(h.Height);
        vsb.AppendLine("  (isometric facade tile rows)");
        vsb.Append("  Entries    : ");
        vsb.Append(fac.Entries.Length);
        vsb.Append("  (");
        vsb.Append(h.Width);
        vsb.Append('×');
        vsb.Append(h.Height);
        vsb.AppendLine(" grid)");
        vsb.AppendLine();

        var walkable = fac.Entries.Count(e => e.Walkable);
        var blocked = fac.Entries.Length - walkable;
        vsb.Append("  Walkable   : ");
        vsb.Append(walkable);
        vsb.Append(" / ");
        vsb.Append(fac.Entries.Length);
        vsb.Append(" tiles  (");
        vsb.Append(blocked);
        vsb.AppendLine(" blocked)");
        vsb.AppendLine();

        const int maxEntriesToList = 64;
        if (fac.Entries.Length <= maxEntriesToList)
        {
            foreach (var e in fac.Entries)
                vsb.AppendLine($"  ({e.X, 3},{e.Y, 3}) {(e.Walkable ? "WALK" : "BLOCKED")}");
        }
        else
        {
            vsb.Append("  (listing suppressed — ");
            vsb.Append(fac.Entries.Length);
            vsb.Append(" entries; showing ");
            vsb.Append(h.Width);
            vsb.Append('×');
            vsb.Append(h.Height);
            vsb.AppendLine(" grid)");
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
