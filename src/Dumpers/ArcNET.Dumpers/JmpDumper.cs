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
            vsb.Append(
                $"  [{i, 3}] From tile ({j.SourceX},{j.SourceY}) \u2192 map {j.DestinationMapId} at tile ({j.DestX},{j.DestY})"
            );
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
