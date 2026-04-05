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
        vsb.AppendLine($"  {jmp.Jumps.Count} transition point(s) defined.");
        vsb.AppendLine();

        for (var i = 0; i < jmp.Jumps.Count; i++)
        {
            var j = jmp.Jumps[i];
            vsb.Append(
                $"  [{i, 3}] From tile ({j.SourceX},{j.SourceY}) → map {j.DestinationMapId} at tile ({j.DestX},{j.DestY})"
            );
            if (j.Flags != 0)
                vsb.Append($"  [flags=0x{j.Flags:X8}]");
            vsb.AppendLine();
        }

        return vsb.ToString();
    }

    public static void Dump(JmpFile jmp, TextWriter writer) => writer.Write(Dump(jmp));
}
