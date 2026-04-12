using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="MapProperties"/> instance.
/// </summary>
public static class MapPropertiesDumper
{
    public static string Dump(MapProperties props)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== MAP PROPERTIES ===");
        vsb.Append("  Ground art ID : ");
        vsb.Append(props.ArtId);
        vsb.AppendLine("  (index into art/ground/ground.mes for the base terrain tile)");
        vsb.Append("  Dimensions    : ");
        vsb.Append(props.LimitX);
        vsb.Append(" × ");
        vsb.Append(props.LimitY);
        vsb.Append(" tiles");
        if (props.LimitX == 960 && props.LimitY == 960)
            vsb.Append("  (standard full map)");
        vsb.AppendLine();
        if (props.Unused != 0)
        {
            vsb.Append("  Unused field  : ");
            vsb.Append(props.Unused);
            vsb.AppendLine("  (non-zero — unexpected)");
        }
        return vsb.ToString();
    }

    public static void Dump(MapProperties props, TextWriter writer) => writer.Write(Dump(props));
}
