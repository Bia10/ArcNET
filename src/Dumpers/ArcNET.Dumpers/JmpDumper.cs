using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="JmpFile"/> instance.
/// </summary>
public static class JmpDumper
{
    public static string Dump(JmpFile jmp)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== JUMP POINT FILE ===");
        vsb.Append("  ");
        vsb.Append(jmp.Jumps.Count);
        vsb.AppendLine(" transition point(s) defined.");
        vsb.AppendLine();

        for (var i = 0; i < jmp.Jumps.Count; i++)
        {
            var j = jmp.Jumps[i];
            vsb.Append("  [");
            vsb.AppendPadded(i, 3, leftAlign: false);
            vsb.Append("] From tile (");
            vsb.Append(j.SourceX);
            vsb.Append(',');
            vsb.Append(j.SourceY);
            vsb.Append(") \u2192 map ");
            vsb.Append(j.DestinationMapId);
            vsb.Append(" at tile (");
            vsb.Append(j.DestX);
            vsb.Append(',');
            vsb.Append(j.DestY);
            vsb.Append(')');
            if (j.Flags != 0)
            {
                vsb.AppendHex(j.Flags, "  [flags=0x".AsSpan());
                vsb.Append(']');
            }
            vsb.AppendLine();
        }

        return vsb.ToString();
    }

    public static void Dump(JmpFile jmp, TextWriter writer) => writer.Write(Dump(jmp));
}
