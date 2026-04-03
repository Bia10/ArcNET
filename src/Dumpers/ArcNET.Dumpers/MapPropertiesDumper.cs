using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="MapProperties"/> instance.
/// </summary>
public static class MapPropertiesDumper
{
    public static string Dump(MapProperties props)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== MAP PROPERTIES ===");
        sb.AppendLine($"  ArtId    : {props.ArtId}");
        sb.AppendLine($"  Unused   : {props.Unused}");
        sb.AppendLine($"  LimitX   : {props.LimitX}");
        sb.AppendLine($"  LimitY   : {props.LimitY}");
        return sb.ToString();
    }

    public static void Dump(MapProperties props, TextWriter writer) => writer.Write(Dump(props));
}
