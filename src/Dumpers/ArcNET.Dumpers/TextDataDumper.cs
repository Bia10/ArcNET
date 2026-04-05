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
        vsb.AppendLine($"  Entries: {file.Entries.Count}");
        vsb.AppendLine();

        var maxKeyLen = file.Entries.Count > 0 ? file.Entries.Max(e => e.Key.Length) : 0;

        foreach (var entry in file.Entries)
            vsb.AppendLine($"  {entry.Key.PadRight(maxKeyLen)} : {entry.Value}");

        return vsb.ToString();
    }

    public static void Dump(TextDataFile file, TextWriter writer) => writer.Write(Dump(file));
}
