using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="TextDataFile"/> instance.
/// </summary>
public static class TextDataDumper
{
    public static string Dump(TextDataFile file)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== TEXT DATA FILE ===");
        sb.AppendLine($"  Entries: {file.Entries.Count}");
        sb.AppendLine();

        var maxKeyLen = file.Entries.Count > 0 ? file.Entries.Max(e => e.Key.Length) : 0;

        foreach (var entry in file.Entries)
            sb.AppendLine($"  {entry.Key.PadRight(maxKeyLen)} : {entry.Value}");

        return sb.ToString();
    }

    public static void Dump(TextDataFile file, TextWriter writer) => writer.Write(Dump(file));
}
