using ArcNET.Formats;
using Bia.ValueBuffers;

namespace ArcNET.Dumpers;

/// <summary>
/// Produces a human-readable text dump of a parsed <see cref="SaveIndex"/> instance.
/// </summary>
public static class SaveIndexDumper
{
    public static string Dump(SaveIndex index)
    {
        Span<char> buf = stackalloc char[1024];
        var vsb = new ValueStringBuilder(buf);
        vsb.AppendLine("=== SAVE INDEX (TFAI) ===");
        vsb.Append("  Root entries: ");
        vsb.Append(index.Root.Count);
        vsb.AppendLine();
        vsb.AppendLine();

        var (fileCount, dirCount, totalSize) = CountStats(index.Root);
        vsb.Append("  Total files       : ");
        vsb.Append(fileCount);
        vsb.AppendLine();
        vsb.Append("  Total directories : ");
        vsb.Append(dirCount);
        vsb.AppendLine();
        vsb.Append("  Total payload     : ");
        vsb.Append(totalSize, "N0");
        vsb.AppendLine(" bytes");
        vsb.AppendLine();

        DumpEntries(ref vsb, index.Root, indent: 2);
        return vsb.ToString();
    }

    public static void Dump(SaveIndex index, TextWriter writer) => writer.Write(Dump(index));

    private static void DumpEntries(ref ValueStringBuilder vsb, IReadOnlyList<TfaiEntry> entries, int indent)
    {
        // NOTE: Append(char, count) would remove the manual loop here — tracked as upstream P candidate.
        foreach (var entry in entries)
        {
            for (var i = 0; i < indent; i++)
                vsb.Append(' ');
            switch (entry)
            {
                case TfaiFileEntry file:
                    vsb.Append(file.Name);
                    vsb.Append("  (");
                    vsb.Append(file.Size, "N0");
                    vsb.AppendLine(" bytes)");
                    break;
                case TfaiDirectoryEntry dir:
                    vsb.Append(dir.Name);
                    vsb.AppendLine('/');
                    DumpEntries(ref vsb, dir.Children, indent + 2);
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
