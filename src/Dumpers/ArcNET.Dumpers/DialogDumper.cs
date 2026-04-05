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
        vsb.AppendLine($"  Entries    : {dlg.Entries.Count} ({npcLines} NPC, {pcOptions} PC)");
        vsb.AppendLine();

        foreach (var e in dlg.Entries)
        {
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

            vsb.AppendLine($"  --- Entry {e.Num} [{kind}]{controlTag} ---");
            vsb.AppendLine($"    Text       : {e.Text}");
            if (!string.IsNullOrEmpty(e.GenderField))
                vsb.AppendLine(e.Iq == 0 ? $"    FemaleText : {e.GenderField}" : $"    Gender     : {e.GenderField}");
            if (!string.IsNullOrEmpty(e.Conditions))
                vsb.AppendLine($"    Conditions : {e.Conditions}  [Arcanum script]");
            else
                vsb.AppendLine("    Conditions : (always available)");
            vsb.AppendLine($"    Response   : {(e.ResponseVal == 0 ? "(end)" : e.ResponseVal.ToString())}");
            if (!string.IsNullOrEmpty(e.Actions))
                vsb.AppendLine($"    Actions    : {e.Actions}  [Arcanum script]");
            vsb.AppendLine();
        }

        return vsb.ToString();
    }

    public static void Dump(DlgFile dlg, TextWriter writer) => writer.Write(Dump(dlg));
}
