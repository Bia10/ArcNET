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
        sb.AppendLine($"  Ground art ID : {props.ArtId}  (index into art/ground/ground.mes for the base terrain tile)");
        sb.AppendLine(
            $"  Dimensions    : {props.LimitX} × {props.LimitY} tiles"
                + (props.LimitX == 960 && props.LimitY == 960 ? "  (standard full map)" : "")
        );
        if (props.Unused != 0)
            sb.AppendLine($"  Unused field  : {props.Unused}  (non-zero — unexpected)");
        return sb.ToString();
    }

    public static void Dump(MapProperties props, TextWriter writer) => writer.Write(Dump(props));
}
