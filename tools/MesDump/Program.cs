using ArcNET.Archive;
using ArcNET.Dumpers;
using ArcNET.Formats;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: MesDump <gameDir> [protoId...]");
    return 1;
}

var gameDir = args[0];

// ── MES entries ────────────────────────────────────────────────────────────────
foreach (var datFile in Directory.GetFiles(gameDir, "*.dat", SearchOption.AllDirectories))
{
    DatArchive archive;
    try
    {
        archive = DatArchive.Open(datFile);
    }
    catch
    {
        continue;
    }
    using (archive)
    {
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;
            var p = entry.Path;
            if (
                p.EndsWith("gamearea.mes", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith("townmap.mes", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith("maplist.mes", StringComparison.OrdinalIgnoreCase)
            )
            {
                var mes = MessageFormat.ParseMemory(archive.ReadEntry(entry));
                Console.WriteLine($"\n=== {Path.GetFileName(datFile)} :: {p} ({mes.Entries.Count} entries) ===");
                foreach (var e in mes.Entries)
                    Console.WriteLine($"  [{e.Index}] {e.Text}");
            }
        }
    }
}

return 0;
