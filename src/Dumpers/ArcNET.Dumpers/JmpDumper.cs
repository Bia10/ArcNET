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
        sb.AppendLine($"  {jmp.Jumps.Count} transition point(s) defined.");
        sb.AppendLine();

        for (var i = 0; i < jmp.Jumps.Count; i++)
        {
            var j = jmp.Jumps[i];
            sb.Append(
                $"  [{i, 3}] From tile ({j.SourceX},{j.SourceY}) → map {j.DestinationMapId} at tile ({j.DestX},{j.DestY})"
            );
            if (j.Flags != 0)
                sb.Append($"  [flags=0x{j.Flags:X8}]");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void Dump(JmpFile jmp, TextWriter writer) => writer.Write(Dump(jmp));
}
