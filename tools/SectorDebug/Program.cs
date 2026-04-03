using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Formats;

var gameRoot = args.Length > 0 ? args[0] : @"C:\Games\Arcanum\ArcanumCleanUAPnohighres - Copy";

// Find a sector file in the DAT archives
var datPaths = new List<string>();
foreach (var p in Directory.GetFiles(gameRoot, "Arcanum*.dat"))
    datPaths.Add(p);
var moduleDat = Path.Combine(gameRoot, "modules", "Arcanum.dat");
if (File.Exists(moduleDat))
    datPaths.Add(moduleDat);

foreach (var datPath in datPaths)
{
    using var dat = DatArchive.Open(datPath);
    var secEntries = dat
        .Entries.Where(e => e.Path.EndsWith(".sec", StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(e => e.UncompressedSize)
        .Take(1)
        .ToList();

    if (secEntries.Count == 0)
        continue;

    Console.WriteLine($"Archive: {Path.GetFileName(datPath)}");

    foreach (var entry in secEntries)
    {
        Console.WriteLine($"\n--- {entry.Path} ({entry.UncompressedSize:N0} bytes) ---");

        var data = dat.GetEntryData(entry.Path);
        var reader = new SpanReader(data.Span);

        // Skip to objects section
        var lightCount = reader.ReadInt32();
        reader.Skip(lightCount * 48); // lights
        reader.Skip(4096 * 4); // tiles
        var roofFlag = reader.ReadInt32();
        if (roofFlag == 0)
            reader.Skip(256 * 4); // roofs
        var version = reader.ReadInt32();
        Console.WriteLine($"  Version: 0x{version:X8}");

        // Skip version-dependent sections
        if (version >= 0xAA0001)
        {
            var tsCount = reader.ReadInt32();
            reader.Skip(tsCount * 24);
        }
        if (version >= 0xAA0002)
            reader.Skip(12); // sector script
        if (version >= 0xAA0003)
        {
            reader.Skip(12); // townmap/apt/lightscheme
            reader.Skip(12); // sound list
        }
        if (version >= 0xAA0004)
            reader.Skip(128 * 4); // block mask

        // Now at objects section
        var remaining = reader.Remaining;
        var trailingCount = reader.PeekInt32At(remaining - 4);
        Console.WriteLine($"  Object section at 0x{reader.Position:X6}, remaining={remaining}, count={trailingCount}");

        // Try parsing objects one at a time
        for (var i = 0; i < trailingCount; i++)
        {
            var posBefore = reader.Position;
            try
            {
                var mob = MobFormat.Parse(ref reader);
                if (i < 3 || i == trailingCount - 1)
                    Console.WriteLine(
                        $"    [{i}] OK at 0x{posBefore:X6}, read {reader.Position - posBefore} bytes, type={mob.Header.GameObjectType}"
                    );
                else if (i == 3)
                    Console.WriteLine("    ...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [{i}] FAIL at 0x{posBefore:X6}: {ex.GetType().Name}: {ex.Message}");
                // Dump hex around failure point
                Console.Write("    HEX @ failure: ");
                for (var b = 0; b < Math.Min(48, data.Length - posBefore); b++)
                    Console.Write($"{data.Span[posBefore + b]:X2} ");
                Console.WriteLine();
                break;
            }
        }
    }

    break; // Just first archive
}
