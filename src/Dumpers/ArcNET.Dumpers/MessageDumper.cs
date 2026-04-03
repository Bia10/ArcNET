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

        var withSound = mes.Entries.Count(e => e.SoundId is not null);
        var textOnly = mes.Entries.Count - withSound;
        sb.Append($"  Entries: {mes.Entries.Count}");
        if (withSound > 0)
            sb.Append($"  ({withSound} with sound ID, {textOnly} text-only)");
        sb.AppendLine();

        if (mes.Entries.Count > 0)
        {
            var minId = mes.Entries.Min(e => e.Index);
            var maxId = mes.Entries.Max(e => e.Index);
            sb.AppendLine($"  ID range: {minId} \u2013 {maxId}");
        }

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
