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

        foreach (var e in fac.Entries)
        {
            sb.AppendLine($"  ({e.X, 3},{e.Y, 3}) {(e.Walkable ? "WALK" : "BLOCKED")}");
        }

        return sb.ToString();
    }

    public static void Dump(FacadeWalk fac, TextWriter writer) => writer.Write(Dump(fac));
}
