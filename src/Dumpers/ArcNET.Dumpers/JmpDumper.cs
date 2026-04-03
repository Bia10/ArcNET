using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="JmpFile"/> instance.
/// </summary>
public static class JmpDumper
{
    public static string Dump(JmpFile jmp)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== JUMP POINT FILE ===");
        sb.AppendLine($"  Entries: {jmp.Jumps.Count}");
        sb.AppendLine();

        for (var i = 0; i < jmp.Jumps.Count; i++)
        {
            var j = jmp.Jumps[i];
            sb.AppendLine(
                $"  [{i, 3}] flags=0x{j.Flags:X8}  src=({j.SourceX},{j.SourceY})  -> map={j.DestinationMapId}  dst=({j.DestX},{j.DestY})"
            );
        }

        return sb.ToString();
    }

    public static void Dump(JmpFile jmp, TextWriter writer) => writer.Write(Dump(jmp));
}
