using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="MesFile"/> instance.
/// </summary>
public static class MessageDumper
{
    public static string Dump(MesFile mes)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== MESSAGE FILE ===");

        var withSound = mes.Entries.Count(e => e.SoundId is not null);
        var textOnly = mes.Entries.Count - withSound;
        vsb.Append("  Entries: ");
        vsb.Append(mes.Entries.Count);
        if (withSound > 0)
        {
            vsb.Append("  (");
            vsb.Append(withSound);
            vsb.Append(" with sound ID, ");
            vsb.Append(textOnly);
            vsb.Append(" text-only)");
        }
        vsb.AppendLine();

        if (mes.Entries.Count > 0)
        {
            var minId = mes.Entries.Min(e => e.Index);
            var maxId = mes.Entries.Max(e => e.Index);
            vsb.Append("  ID range: ");
            vsb.Append(minId);
            vsb.Append(" \u2013 ");
            vsb.AppendLine(maxId);
        }

        vsb.AppendLine();

        foreach (var entry in mes.Entries)
        {
            vsb.Append("  [");
            vsb.AppendPadded<int>(entry.Index, 6, leftAlign: false, padChar: ' ');
            vsb.Append("] ");
            if (entry.SoundId is not null)
            {
                vsb.Append("(sound=");
                vsb.Append(entry.SoundId);
                vsb.Append(") ");
            }
            vsb.AppendLine(entry.Text);
        }

        return vsb.ToString();
    }

    public static void Dump(MesFile mes, TextWriter writer) => writer.Write(Dump(mes));
}
