using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="DlgFile"/> instance.
/// </summary>
public static class DialogDumper
{
    public static string Dump(DlgFile dlg)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== DIALOG FILE ===");

        var npcLines = dlg.Entries.Count(e => e.Iq == 0);
        var pcOptions = dlg.Entries.Count(e => e.Iq != 0);
        vsb.Append("  Entries    : ");
        vsb.Append(dlg.Entries.Count);
        vsb.Append(" (");
        vsb.Append(npcLines);
        vsb.Append(" NPC, ");
        vsb.Append(pcOptions);
        vsb.AppendLine(" PC)");
        vsb.AppendLine();

        foreach (var e in dlg.Entries)
        {
            // Switch-expression strings: enum.ToString() allocates regardless; keeping as locals is correct.
            var kind = e.Iq switch
            {
                0 => "NPC",
                > 0 => $"PC (IQ>={e.Iq})",
                _ => $"PC (IQ<={Math.Abs(e.Iq)})",
            };

            // Detect engine control entries (navigation commands, not displayed text)
            var controlTag = e.Text switch
            {
                "E:" => "  [EXIT]",
                "F:" => "  [FLOAT]",
                _ when e.Text.StartsWith("R:") => "  [RANDOM]",
                _ when e.Text.StartsWith("C:") => "  [CHOICE]",
                _ when e.Text.StartsWith("T:") => "  [TOPIC]",
                _ => "",
            };

            vsb.Append("  --- Entry ");
            vsb.Append(e.Num);
            vsb.Append(" [");
            vsb.Append(kind);
            vsb.Append(']');
            vsb.Append(controlTag);
            vsb.AppendLine(" ---");
            vsb.Append("    Text       : ");
            vsb.AppendLine(e.Text);
            if (!string.IsNullOrEmpty(e.GenderField))
            {
                vsb.Append(e.Iq == 0 ? "    FemaleText : " : "    Gender     : ");
                vsb.AppendLine(e.GenderField);
            }
            if (!string.IsNullOrEmpty(e.Conditions))
            {
                vsb.Append("    Conditions : ");
                vsb.Append(e.Conditions);
                vsb.AppendLine("  [Arcanum script]");
            }
            else
                vsb.AppendLine("    Conditions : (always available)");
            vsb.Append("    Response   : ");
            if (e.ResponseVal == 0)
                vsb.AppendLine("(end)");
            else
            {
                vsb.AppendLine(e.ResponseVal);
            }
            if (!string.IsNullOrEmpty(e.Actions))
            {
                vsb.Append("    Actions    : ");
                vsb.Append(e.Actions);
                vsb.AppendLine("  [Arcanum script]");
            }
            vsb.AppendLine();
        }

        return vsb.ToString();
    }

    public static void Dump(DlgFile dlg, TextWriter writer) => writer.Write(Dump(dlg));
}
