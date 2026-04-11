using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="TextDataFile"/> instance.
/// </summary>
public static class TextDataDumper
{
    public static string Dump(TextDataFile file)
    {
        Span<char> buf = stackalloc char[512];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== TEXT DATA FILE ===");
        vsb.Append("  Entries: ");
        vsb.Append(file.Entries.Count);
        vsb.AppendLine();
        vsb.AppendLine();

        var maxKeyLen = file.Entries.Count > 0 ? file.Entries.Max(e => e.Key.Length) : 0;

        foreach (var entry in file.Entries)
        {
            vsb.Append("  ");
            vsb.AppendPadded(entry.Key, maxKeyLen);
            vsb.Append(" : ");
            vsb.AppendLine(entry.Value);
        }

        return vsb.ToString();
    }

    public static void Dump(TextDataFile file, TextWriter writer) => writer.Write(Dump(file));
}
