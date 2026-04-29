using ArcNET.Archive;
using ArcNET.Core;
using ArcNET.Core.Primitives;
using ArcNET.Dumpers;
using ArcNET.Formats;
using ArcNET.GameObjects;
using ConsoleAppFramework;
using Spectre.Console;

namespace ArcNET.App;

/// <summary>
/// <c>data</c> command group — query and export game data from the Arcanum installation.
/// Usage: <c>arcnet data &lt;subcommand&gt; &lt;gameDir&gt; [args...]</c>
/// </summary>
public sealed class DataCommands
{
    /// <summary>List all map directories found inside Arcanum.dat.</summary>
    public async Task ListMaps([Argument] string gameDir)
    {
        if (!TryResolveArchivePath(gameDir, out var datPath))
            return;

        await Task.Run(() =>
        {
            using var archive = DatArchive.Open(datPath);

            var mapFolders = archive
                .Entries.Select(e => e.Path)
                .Where(p => p.StartsWith("maps\\", StringComparison.OrdinalIgnoreCase))
                .Select(p =>
                {
                    var segments = p.Split('\\');
                    return segments.Length >= 2 ? $"maps\\{segments[1]}\\" : null;
                })
                .Where(f => f is not null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f)
                .ToList();

            AnsiConsole.MarkupLine($"[bold]Found {mapFolders.Count} map folders in Arcanum.dat:[/]");
            foreach (var folder in mapFolders)
                Console.WriteLine($"  {folder}");
        });
    }

    /// <summary>
    /// Dump all mob files for a given map directory prefix inside Arcanum.dat.
    /// <paramref name="mapPath"/> is a backslash-separated prefix, e.g. <c>maps\SomeMap\</c>.
    /// </summary>
    public async Task DumpMobs([Argument] string gameDir, [Argument] string mapPath)
    {
        if (!TryResolveArchivePath(gameDir, out var datPath))
            return;

        await Task.Run(() =>
        {
            using var archive = DatArchive.Open(datPath);
            ContainerMobDumpWorkflow.Dump(archive, gameDir, mapPath);
        });
    }

    /// <summary>
    /// Dump all mobs and protos for a map prefix to a text file.
    /// <paramref name="mapPrefix"/> is a backslash-separated prefix, e.g. <c>maps\SomeMap\</c>.
    /// </summary>
    public async Task DumpMap([Argument] string gameDir, [Argument] string mapPrefix, [Argument] string output)
    {
        if (!TryResolveArchivePath(gameDir, out var datPath))
            return;

        await Task.Run(() =>
        {
            using var archive = DatArchive.Open(datPath);
            using var writer = new StreamWriter(output, append: false, System.Text.Encoding.UTF8);

            var nameLookup = ItemDumper.LoadProtoNameLookup(gameDir);
            AnsiConsole.MarkupLine($"[grey]Loaded {nameLookup.Count} proto names from description.mes.[/]");

            var installation = ArcanumInstallation.Detect(gameDir);
            AnsiConsole.MarkupLine($"[grey]Installation: {installation}[/]");

            var parseErrors = new List<string>();
            var protoIdsSeen = new SortedSet<int>();
            var (totalMobCount, parsed, mobByGuid) = CollectMobEntries(archive, mapPrefix, parseErrors, protoIdsSeen);

            AnsiConsole.MarkupLine($"[bold]Found {totalMobCount} mob files under '{Markup.Escape(mapPrefix)}'.[/]");

            writer.WriteLine($"MAP DUMP — prefix: {mapPrefix}");
            writer.WriteLine($"Generated: {DateTime.UtcNow:u}");
            writer.WriteLine($"Total mob files: {totalMobCount}  (parsed: {parsed.Count})");
            writer.WriteLine(new string('=', 80));
            writer.WriteLine();

            WriteMobSections(writer, parsed, mobByGuid, nameLookup, installation);

            if (parseErrors.Count > 0)
            {
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"PARSE ERRORS ({parseErrors.Count}):");
                foreach (var err in parseErrors)
                    writer.WriteLine($"  {err}");
                writer.WriteLine();
            }

            WriteProtoSections(writer, archive, gameDir, protoIdsSeen, nameLookup, installation);

            writer.WriteLine(new string('=', 80));
            writer.WriteLine("END OF DUMP");
            AnsiConsole.MarkupLine($"\n[green]Dump written to: {Markup.Escape(output)}[/]");
        });
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>Resolves the path to the main Arcanum.dat archive inside <paramref name="gameDir"/>.</summary>
    private static string ArcDatPath(string gameDir) => Path.Combine(gameDir, "modules", "Arcanum.dat");

    /// <summary>
    /// Validates that <paramref name="gameDir"/> exists and contains <c>Arcanum.dat</c>.
    /// Prints a user-facing error and returns <see langword="false"/> on failure.
    /// </summary>
    private static bool TryResolveArchivePath(string gameDir, out string datPath)
    {
        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            datPath = string.Empty;
            return false;
        }

        datPath = ArcDatPath(gameDir);
        if (!File.Exists(datPath))
        {
            AnsiConsole.MarkupLine($"[red]Arcanum.dat not found at: {Markup.Escape(datPath)}[/]");
            return false;
        }

        return true;
    }

    private static int? ExtractProtoNum(MobData mob)
    {
        var num = mob.Header.ProtoId.GetProtoNumber();
        return num > 0 ? num : null;
    }

    private static string? ResolveProtoName(
        int protoNum,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        var vanillaId = ArcanumInstallation.ToVanillaProtoId(protoNum, installation);
        if (nameLookup.TryGetValue(vanillaId, out var name))
            return name;
        if (nameLookup.TryGetValue(protoNum, out name))
            return name;
        return null;
    }

    private static bool IsDumpableItemType(ObjectType type) =>
        type
            is ObjectType.Weapon
                or ObjectType.Armor
                or ObjectType.Ammo
                or ObjectType.Gold
                or ObjectType.Food
                or ObjectType.Scroll
                or ObjectType.Key
                or ObjectType.KeyRing
                or ObjectType.Written
                or ObjectType.Generic;

    private static void AppendInventorySection(
        TextWriter writer,
        MobData mob,
        ObjectField invField,
        Dictionary<Guid, MobData> mobByGuid,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        var invProp = mob.Properties.FirstOrDefault(p => p.Field == invField);
        if (invProp is null || invProp.RawBytes.Length <= 1)
        {
            writer.WriteLine("  (absent)");
            writer.WriteLine();
            return;
        }

        (short OidType, int ProtoOrData1, Guid Id)[] items;
        try
        {
            items = invProp.GetObjectIdArrayFull();
        }
        catch (Exception)
        {
            writer.WriteLine("  (failed to decode inventory list)");
            writer.WriteLine();
            return;
        }

        if (items.Length == 0)
        {
            writer.WriteLine("  (empty)");
            writer.WriteLine();
            return;
        }

        writer.WriteLine($"  {items.Length} item(s):");
        for (var i = 0; i < items.Length; i++)
        {
            var (_, _, guid) = items[i];
            if (!mobByGuid.TryGetValue(guid, out var itemMob))
            {
                writer.WriteLine($"  [{i + 1}] {guid}  (mob not in this map)");
                continue;
            }

            var itemProtoNum = ExtractProtoNum(itemMob);
            var itemName =
                (itemProtoNum is > 0 ? ResolveProtoName(itemProtoNum.Value, nameLookup, installation) : null)
                ?? itemMob.Header.GameObjectType.ToString();
            var oneLiner = GetItemOneLiner(itemMob);

            writer.WriteLine(
                $"  [{i + 1}] {itemName}"
                    + $"  (proto={itemProtoNum?.ToString() ?? "?"}, type={itemMob.Header.GameObjectType})"
                    + (oneLiner.Length > 0 ? $"  {oneLiner}" : "")
            );
        }
        writer.WriteLine();
    }

    private static string GetItemOneLiner(MobData mob)
    {
        static int? GetInt(MobData m, ObjectField f) => m.Properties.FirstOrDefault(p => p.Field == f)?.GetInt32();

        return mob.Header.GameObjectType switch
        {
            ObjectType.Weapon =>
                $"dmg={GetInt(mob, ObjectField.ObjFWeaponDamageLowerIdx) ?? 0}–{GetInt(mob, ObjectField.ObjFWeaponDamageUpperIdx) ?? 0}"
                    + (GetInt(mob, ObjectField.ObjFWeaponSpeedFactor) is { } spd ? $" spd={spd}" : ""),
            ObjectType.Armor => $"ac={GetInt(mob, ObjectField.ObjFArmorAcAdj) ?? 0}"
                + (GetInt(mob, ObjectField.ObjFArmorMagicAcAdj) is { } mac and > 0 ? $"+{mac}magic" : ""),
            ObjectType.Gold => $"qty={GetInt(mob, ObjectField.ObjFGoldQuantity) ?? 0}",
            _ => "",
        };
    }

    // ── DumpMap helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Reads and parses all <c>.mob</c> entries under <paramref name="mapPrefix"/> from
    /// <paramref name="archive"/>. Errors are appended to <paramref name="parseErrors"/>;
    /// unique referenced proto IDs are inserted into <paramref name="protoIdsSeen"/>.
    /// Returns the total entry count, the successfully parsed mobs, and a GUID index.
    /// </summary>
    private static (
        int TotalFound,
        IReadOnlyList<(string EntryPath, byte[] Data, MobData Mob)> Parsed,
        Dictionary<Guid, MobData> ByGuid
    ) CollectMobEntries(DatArchive archive, string mapPrefix, List<string> parseErrors, SortedSet<int> protoIdsSeen)
    {
        var mobEntries = archive
            .Entries.Select(e => e.Path)
            .Where(p =>
                p.StartsWith(mapPrefix, StringComparison.OrdinalIgnoreCase)
                && p.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(p => p)
            .ToList();

        var parsed = new List<(string EntryPath, byte[] Data, MobData Mob)>(mobEntries.Count);
        var mobByGuid = new Dictionary<Guid, MobData>(mobEntries.Count);

        foreach (var entryPath in mobEntries)
        {
            byte[] data;
            try
            {
                data = archive.GetEntryData(entryPath).ToArray();
            }
            catch (Exception ex)
            {
                parseErrors.Add($"SKIP (read) {entryPath}: {ex.Message}");
                continue;
            }

            MobData mob;
            try
            {
                mob = MobFormat.ParseMemory(data);
            }
            catch (Exception ex)
            {
                parseErrors.Add($"SKIP (parse) {entryPath}: {ex.Message}");
                continue;
            }

            parsed.Add((entryPath, data, mob));
            mobByGuid[mob.Header.ObjectId.Id] = mob;

            var protoNum = ExtractProtoNum(mob);
            if (protoNum is > 0)
                protoIdsSeen.Add(protoNum.Value);
        }

        return (mobEntries.Count, parsed, mobByGuid);
    }

    /// <summary>
    /// Writes one dump section per parsed mob to <paramref name="writer"/>,
    /// including item summary and inventory sections where applicable.
    /// </summary>
    private static void WriteMobSections(
        TextWriter writer,
        IReadOnlyList<(string EntryPath, byte[] Data, MobData Mob)> parsed,
        Dictionary<Guid, MobData> mobByGuid,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        foreach (var (entryPath, data, mob) in parsed)
        {
            var protoNum = ExtractProtoNum(mob);
            var protoName = protoNum is > 0 ? ResolveProtoName(protoNum.Value, nameLookup, installation) : null;

            writer.WriteLine(
                $"FILE: {Path.GetFileName(entryPath)}  ({data.Length} B)"
                    + (protoName is not null ? $"  [{protoName}]" : "")
            );
            writer.WriteLine(MobDumper.Dump(mob));

            if (protoNum is > 0 && IsDumpableItemType(mob.Header.GameObjectType))
            {
                writer.WriteLine("=== ITEM SUMMARY ===");
                writer.Write(ItemDumper.DumpItem(mob, protoNum.Value, nameLookup, installation));
                writer.WriteLine();
            }

            if (mob.Header.GameObjectType == ObjectType.Container)
            {
                writer.WriteLine("=== CONTAINER INVENTORY ===");
                AppendInventorySection(
                    writer,
                    mob,
                    ObjectField.ObjFContainerInventoryListIdx,
                    mobByGuid,
                    nameLookup,
                    installation
                );
            }

            if (mob.Header.GameObjectType is ObjectType.Npc or ObjectType.Pc)
            {
                var hasInv = mob.Properties.Any(p => p.Field == ObjectField.ObjFCritterInventoryListIdx);
                if (hasInv)
                {
                    writer.WriteLine("=== CRITTER INVENTORY ===");
                    AppendInventorySection(
                        writer,
                        mob,
                        ObjectField.ObjFCritterInventoryListIdx,
                        mobByGuid,
                        nameLookup,
                        installation
                    );
                }
            }

            AnsiConsole.MarkupLine(
                $"  [grey]Dumped: {Markup.Escape(Path.GetFileName(entryPath))}"
                    + $"{(protoName is not null ? $" ({Markup.Escape(protoName)})" : "")}[/]"
            );
        }
    }

    /// <summary>
    /// Writes the proto-referenced section to <paramref name="writer"/>, loading each
    /// proto from a loose file on disk or from <paramref name="archive"/> as a fallback.
    /// </summary>
    private static void WriteProtoSections(
        TextWriter writer,
        DatArchive archive,
        string gameDir,
        SortedSet<int> protoIdsSeen,
        Dictionary<int, string> nameLookup,
        ArcanumInstallationType installation
    )
    {
        writer.WriteLine(new string('=', 80));
        writer.WriteLine($"PROTOS REFERENCED ({protoIdsSeen.Count} unique proto IDs):");
        writer.WriteLine();

        var datProtoEntries = archive
            .Entries.Select(e => e.Path)
            .Where(p =>
                p.StartsWith("proto\\", StringComparison.OrdinalIgnoreCase)
                && p.EndsWith(".pro", StringComparison.OrdinalIgnoreCase)
            )
            .ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var protoId in protoIdsSeen)
        {
            var protoFileName = $"{protoId:D6}";
            datProtoEntries.TryGetValue(protoFileName, out var protoEntryPath);

            var protoDir = Path.Combine(gameDir, "data", "proto");
            var looseProtoPath = Directory.Exists(protoDir)
                ? Directory
                    .EnumerateFiles(protoDir, $"{protoFileName}*.pro", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault()
                : null;

            ProtoData proto;
            string protoSource;

            try
            {
                if (looseProtoPath is not null)
                {
                    proto = ProtoFormat.ParseFile(looseProtoPath);
                    protoSource = $"loose: {looseProtoPath}";
                }
                else if (protoEntryPath is not null)
                {
                    proto = ProtoFormat.ParseMemory(archive.GetEntryData(protoEntryPath));
                    protoSource = $"DAT: {protoEntryPath}";
                }
                else
                {
                    writer.WriteLine($"[Proto {protoId}] NOT FOUND in DAT or on disk.");
                    writer.WriteLine();
                    continue;
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"[Proto {protoId}] PARSE ERROR: {ex.Message}");
                writer.WriteLine();
                continue;
            }

            var protoName = ResolveProtoName(protoId, nameLookup, installation);
            writer.WriteLine($"SOURCE: {protoSource}{(protoName is not null ? $"  [{protoName}]" : "")}");
            writer.WriteLine(ProtoDumper.Dump(proto));
            AnsiConsole.MarkupLine(
                $"  [grey]Dumped proto: {protoId}{(protoName is not null ? $" ({Markup.Escape(protoName)})" : "")}[/]"
            );
        }
    }
}
