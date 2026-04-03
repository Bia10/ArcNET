using System.Globalization;
using ArcNET.Archive;
using ArcNET.Dumpers;
using ArcNET.Formats;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: DiagnosticDump <game-root-dir>");
    return 1;
}

var gameRoot = args[0];
var protoDir = Path.Combine(gameRoot, "data", "proto");

// Find all .dat archives: Arcanum1-5.dat in root + modules/Arcanum.dat
var datPaths = new List<string>();
foreach (var p in Directory.GetFiles(gameRoot, "Arcanum*.dat"))
    datPaths.Add(p);
var moduleDat = Path.Combine(gameRoot, "modules", "Arcanum.dat");
if (File.Exists(moduleDat))
    datPaths.Add(moduleDat);

Console.WriteLine($"Game root: {gameRoot}");
Console.WriteLine();

// ── Proto files (on disk) ──────────────────────────────────────────
if (Directory.Exists(protoDir))
{
    var proFiles = Directory.GetFiles(protoDir, "*.pro", SearchOption.AllDirectories);
    Console.WriteLine($"=== PROTO FILES ({proFiles.Length} on disk) ===");
    Console.WriteLine();

    foreach (var proFile in proFiles.Take(3))
    {
        Console.WriteLine($"--- {Path.GetFileName(proFile)} ---");
        try
        {
            var proto = ProtoFormat.ParseFile(proFile);
            Console.Write(ProtoDumper.Dump(proto));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }
        Console.WriteLine();
    }
}

// ── DAT archive formats ────────────────────────────────────────────
if (datPaths.Count == 0)
{
    Console.Error.WriteLine("No Arcanum*.dat archives found.");
    return 1;
}

// Open all archives and merge entries
var archives = new List<DatArchive>();
var allEntries = new List<(DatArchive Dat, ArchiveEntry Entry)>();
foreach (var dp in datPaths)
{
    Console.WriteLine(
        $"Opening: {Path.GetFileName(dp)} ({new FileInfo(dp).Length.ToString("N0", CultureInfo.InvariantCulture)} bytes)"
    );
    var dat = DatArchive.Open(dp);
    archives.Add(dat);
    foreach (var entry in dat.Entries)
        allEntries.Add((dat, entry));
}
Console.WriteLine($"Total entries across all archives: {allEntries.Count}");
Console.WriteLine();

// Group entries by extension for stats
var byExt = allEntries
    .GroupBy(e => Path.GetExtension(e.Entry.Path).ToLowerInvariant())
    .OrderByDescending(g => g.Count())
    .Take(20);

Console.WriteLine("=== ARCHIVE ENTRY STATS ===");
foreach (var g in byExt)
    Console.WriteLine($"  {g.Key, -10} : {g.Count(), 6} entries");
Console.WriteLine();

// ── Message files ──────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".mes",
    "MESSAGE",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var mes = MessageFormat.ParseMemory(data);
        return MessageDumper.Dump(mes);
    }
);

// ── Dialog files ───────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".dlg",
    "DIALOG",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var dlg = DialogFormat.ParseMemory(data);
        return DialogDumper.Dump(dlg);
    }
);

// ── Mob files ──────────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".mob",
    "MOB",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var mob = MobFormat.ParseMemory(data);
        return MobDumper.Dump(mob);
    }
);

// ── Sector files ───────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".sec",
    "SECTOR",
    1,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var sector = SectorFormat.ParseMemory(data);
        return SectorDumper.Dump(sector);
    }
);

// ── Jump point files ───────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".jmp",
    "JUMP POINT",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var jmp = JmpFormat.ParseMemory(data);
        return JmpDumper.Dump(jmp);
    }
);

// ── Script files ───────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".scr",
    "SCRIPT",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var scr = ScriptFormat.ParseMemory(data);
        return ScriptDumper.Dump(scr);
    }
);

// ── Terrain files ──────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".tdf",
    "TERRAIN",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var terrain = TerrainFormat.ParseMemory(data);
        return TerrainDumper.Dump(terrain);
    }
);

// ── Map properties ─────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".prp",
    "MAP PROPERTIES",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var props = MapPropertiesFormat.ParseMemory(data);
        return MapPropertiesDumper.Dump(props);
    }
);

// ── Art files ──────────────────────────────────────────────────────
DumpFromEntries(
    allEntries,
    ".art",
    "ART",
    2,
    (dat, path) =>
    {
        var data = dat.GetEntryData(path);
        var art = ArtFormat.ParseMemory(data);
        return ArtDumper.Dump(art);
    }
);

Console.WriteLine("=== DUMP COMPLETE ===");

// Dispose all archives
foreach (var a in archives)
    a.Dispose();

return 0;

static void DumpFromEntries(
    List<(DatArchive Dat, ArchiveEntry Entry)> entries,
    string extension,
    string label,
    int maxDump,
    Func<DatArchive, string, string> dumper
)
{
    var matching = entries
        .Where(e => e.Entry.Path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        // Prefer larger files (more likely to have content)
        .OrderByDescending(e => e.Entry.UncompressedSize)
        .ToList();

    Console.WriteLine($"=== {label} FILES ({matching.Count} in archives) ===");
    Console.WriteLine();

    var dumped = 0;
    var errors = 0;
    const int maxAttempts = 20; // Try up to 20 entries to find maxDump successes

    foreach (var (dat, entry) in matching)
    {
        if (dumped >= maxDump || dumped + errors >= maxAttempts)
            break;

        Console.WriteLine(
            $"--- {entry.Path} ({entry.UncompressedSize.ToString("N0", CultureInfo.InvariantCulture)} bytes) ---"
        );
        try
        {
            Console.Write(dumper(dat, entry.Path));
            dumped++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.GetType().Name}: {ex.Message}");
            errors++;
        }
        Console.WriteLine();
    }

    if (dumped == 0 && errors > 0)
        Console.WriteLine($"  (all {errors} attempted entries failed to parse)");
}
