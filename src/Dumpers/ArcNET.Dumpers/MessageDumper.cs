using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="MesFile"/> instance.
/// </summary>
public static class MessageDumper
{
    public static string Dump(MesFile mes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== MESSAGE FILE ===");
        sb.AppendLine($"  Entries: {mes.Entries.Count}");
        sb.AppendLine();

        foreach (var entry in mes.Entries)
        {
            sb.Append($"  [{entry.Index, 6}] ");
            if (entry.SoundId is not null)
                sb.Append($"(sound={entry.SoundId}) ");
            sb.AppendLine(entry.Text);
        }

        return sb.ToString();
    }

    public static void Dump(MesFile mes, TextWriter writer) => writer.Write(Dump(mes));
}
