using System.Text;
using ArcNET.Formats;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="SaveIndex"/> instance.
/// </summary>
public static class SaveIndexDumper
{
    public static string Dump(SaveIndex index)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== SAVE INDEX (TFAI) ===");
        sb.AppendLine($"  Root entries: {index.Root.Count}");
        sb.AppendLine();

        var (fileCount, dirCount, totalSize) = CountStats(index.Root);
        sb.AppendLine($"  Total files       : {fileCount}");
        sb.AppendLine($"  Total directories : {dirCount}");
        sb.AppendLine($"  Total payload     : {totalSize:N0} bytes");
        sb.AppendLine();

        DumpEntries(sb, index.Root, indent: 2);
        return sb.ToString();
    }

    public static void Dump(SaveIndex index, TextWriter writer) => writer.Write(Dump(index));

    private static void DumpEntries(StringBuilder sb, IReadOnlyList<TfaiEntry> entries, int indent)
    {
        var pad = new string(' ', indent);
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                    sb.AppendLine($"{pad}{file.Name}  ({file.Size:N0} bytes)");
                    break;
                case TfaiDirectoryEntry dir:
                    sb.AppendLine($"{pad}{dir.Name}/");
                    DumpEntries(sb, dir.Children, indent + 2);
                    break;
            }
        }
    }

    private static (int Files, int Dirs, long TotalSize) CountStats(IReadOnlyList<TfaiEntry> entries)
    {
        var files = 0;
        var dirs = 0;
        var size = 0L;

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case TfaiFileEntry file:
                    files++;
                    size += file.Size;
                    break;
                case TfaiDirectoryEntry dir:
                    dirs++;
                    var (f, d, s) = CountStats(dir.Children);
                    files += f;
                    dirs += d;
                    size += s;
                    break;
            }
        }

        return (files, dirs, size);
    }
}
