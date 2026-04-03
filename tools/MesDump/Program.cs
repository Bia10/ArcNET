using ArcNET.Archive;
using ArcNET.Dumpers;
using ArcNET.Formats;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: MesDump <gameDir> [protoId...]");
    return 1;
}

var gameDir = args[0];
var targetIds = Enumerable
    .Range(11055, 110) // scrolls: 11055-11164
    .ToHashSet();

// ── MES entries ────────────────────────────────────────────────────────────────
foreach (var datFile in Directory.GetFiles(gameDir, "*.dat"))
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
            bool isDesc = p.Equals("mes\\description.mes", StringComparison.OrdinalIgnoreCase);
            bool isOname = p.Equals("oemes\\oname.mes", StringComparison.OrdinalIgnoreCase);
            if (!isDesc && !isOname)
                continue;

            var mes = MessageFormat.ParseMemory(archive.ReadEntry(entry));
            Console.WriteLine($"\n=== {Path.GetFileName(datFile)} :: {p} ({mes.Entries.Count} entries) ===");
            foreach (var e in mes.Entries)
                if (targetIds.Contains(e.Index))
                    Console.WriteLine($"  [{e.Index}] {e.Text}");
        }
    }
}

// ── Proto file dumps ───────────────────────────────────────────────────────────
var protoDir = Path.Combine(gameDir, "data", "proto");
if (Directory.Exists(protoDir))
{
    var dumpIds = args.Length > 1 ? args[1..].Select(int.Parse).ToArray() : new[] { 3049, 10094 };

    foreach (var id in dumpIds)
    {
        var match = Directory.EnumerateFiles(protoDir, $"{id:D6}*.pro").FirstOrDefault();
        if (match is null)
        {
            Console.WriteLine($"\nProto {id}: file not found");
            continue;
        }
        Console.WriteLine($"\n=== Proto {id} ({Path.GetFileName(match)}) ===");
        var proto = ProtoFormat.ParseFile(match);
        Console.Write(ProtoDumper.Dump(proto));
    }
}
return 0;
