using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="DlgFile"/> instance.
/// </summary>
public static class DialogDumper
{
    public static string Dump(DlgFile dlg)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== DIALOG FILE ===");

        var npcLines = dlg.Entries.Count(e => e.Iq == 0);
        var pcOptions = dlg.Entries.Count(e => e.Iq != 0);
        sb.AppendLine($"  Entries    : {dlg.Entries.Count} ({npcLines} NPC, {pcOptions} PC)");
        sb.AppendLine();

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

            sb.AppendLine($"  --- Entry {e.Num} [{kind}]{controlTag} ---");
            sb.AppendLine($"    Text       : {e.Text}");
            if (!string.IsNullOrEmpty(e.GenderField))
                sb.AppendLine(e.Iq == 0 ? $"    FemaleText : {e.GenderField}" : $"    Gender     : {e.GenderField}");
            if (!string.IsNullOrEmpty(e.Conditions))
                sb.AppendLine($"    Conditions : {e.Conditions}");
            sb.AppendLine($"    Response   : {(e.ResponseVal == 0 ? "(end)" : e.ResponseVal.ToString())}");
            if (!string.IsNullOrEmpty(e.Actions))
                sb.AppendLine($"    Actions    : {e.Actions}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static void Dump(DlgFile dlg, TextWriter writer) => writer.Write(Dump(dlg));
}
