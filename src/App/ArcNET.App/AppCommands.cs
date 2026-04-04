using ArcNET.Archive;
using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.State;
using ArcNET.Core;
using ArcNET.Dumpers;
using ArcNET.Formats;
using ArcNET.GameObjects;
using ArcNET.Patch;
using Spectre.Console;

namespace ArcNET.App;

/// <summary>Top-level command implementations for the ArcNET CLI.</summary>
internal static class AppCommands
{
    // ── helpers ────────────────────────────────────────────────────────────

    private static string ResolveFullPath(string gameDir, IBinaryPatch patch) =>
        Path.GetFullPath(Path.Combine(gameDir, patch.Target.RelativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static IReadOnlyList<BinaryPatchSet> DiscoverPatches() =>
        PatchDiscovery.LoadAll(
            onError: (file, ex) =>
                AnsiConsole.MarkupLine(
                    $"[yellow]Skipped '{Markup.Escape(Path.GetFileName(file))}': {Markup.Escape(ex.Message)}[/]"
                )
        );

    // ── Game data fix commands ─────────────────────────────────────────────

    internal static async Task RunApplyGameDataFixesAsync(string? suppliedGameDir = null)
    {
        var gameDir =
            suppliedGameDir
            ?? AnsiConsole.Ask<string>("[green]Arcanum installation directory[/] (e.g. C:\\Games\\Arcanum):");

        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            return;
        }

        await Task.Run(() =>
        {
            var patchSets = DiscoverPatches();

            // ── Preview table: what will be changed ────────────────────────
            var preview = new Table()
                .RoundedBorder()
                .Title("[bold]Patches to apply[/]")
                .AddColumn("Patch set")
                .AddColumn("Patch ID")
                .AddColumn("File")
                .AddColumn("Change");

            foreach (var patchSet in patchSets)
            {
                foreach (var patch in patchSet.Patches)
                    preview.AddRow(
                        Markup.Escape(patchSet.Name),
                        Markup.Escape(patch.Id),
                        Markup.Escape(ResolveFullPath(gameDir, patch)),
                        Markup.Escape(patch.PatchSummary)
                    );
            }

            AnsiConsole.Write(preview);

            // ── Results table ─────────────────────────────────────────────
            var results = new Table()
                .RoundedBorder()
                .Title("[bold]Apply results[/]")
                .AddColumn("Patch ID")
                .AddColumn("File")
                .AddColumn("Change")
                .AddColumn("Status")
                .AddColumn("Detail");

            foreach (var patchSet in patchSets)
            {
                if (PatchStateStore.IsRecorded(gameDir, patchSet))
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]'{Markup.Escape(patchSet.Name)}' is already applied — run Revert first to re-apply.[/]"
                    );
                    continue;
                }

                var patchResults = BinaryPatcher.Apply(patchSet, gameDir);
                var patchById = patchSet.Patches.ToDictionary(p => p.Id);

                foreach (var r in patchResults)
                {
                    var (statusColor, statusText) = r.Status switch
                    {
                        PatchStatus.Applied => ("green", "Applied"),
                        PatchStatus.AlreadyApplied => ("yellow", "Already applied"),
                        PatchStatus.Skipped => ("grey", "Skipped"),
                        PatchStatus.Failed => ("red", "Failed"),
                        _ => ("white", r.Status.ToString()),
                    };

                    patchById.TryGetValue(r.PatchId, out var patch);
                    results.AddRow(
                        Markup.Escape(r.PatchId),
                        patch is not null ? Markup.Escape(ResolveFullPath(gameDir, patch)) : "-",
                        patch is not null ? Markup.Escape(patch.PatchSummary) : "-",
                        $"[{statusColor}]{statusText}[/]",
                        Markup.Escape(r.Reason ?? "")
                    );
                }

                if (patchResults.Any(r => r.Status == PatchStatus.Applied))
                    PatchStateStore.RecordApply(gameDir, patchSet);
            }

            AnsiConsole.Write(results);
            AnsiConsole.MarkupLine("[green]Done.[/]");
        });
    }

    internal static async Task RunRevertGameDataFixesAsync(string? suppliedGameDir = null)
    {
        var gameDir =
            suppliedGameDir
            ?? AnsiConsole.Ask<string>("[green]Arcanum installation directory[/] (e.g. C:\\Games\\Arcanum):");

        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            return;
        }

        await Task.Run(() =>
        {
            var patchSets = DiscoverPatches();

            var table = new Table()
                .RoundedBorder()
                .Title("[bold]Revert results[/]")
                .AddColumn("Patch ID")
                .AddColumn("File")
                .AddColumn("Change")
                .AddColumn("Status")
                .AddColumn("Detail");

            foreach (var patchSet in patchSets)
            {
                if (!PatchStateStore.IsRecorded(gameDir, patchSet))
                {
                    AnsiConsole.MarkupLine(
                        $"[grey]'{Markup.Escape(patchSet.Name)}' was not recorded as applied — skipping.[/]"
                    );
                    continue;
                }

                var patchResults = BinaryPatcher.Revert(patchSet, gameDir);
                var patchById = patchSet.Patches.ToDictionary(p => p.Id);

                foreach (var r in patchResults)
                {
                    var (statusColor, statusText) = r.Status switch
                    {
                        PatchStatus.Applied => ("green", "Reverted"),
                        PatchStatus.Skipped => ("yellow", "Skipped (no backup)"),
                        PatchStatus.Failed => ("red", "Failed"),
                        _ => ("white", r.Status.ToString()),
                    };

                    patchById.TryGetValue(r.PatchId, out var patch);
                    table.AddRow(
                        Markup.Escape(r.PatchId),
                        patch is not null ? Markup.Escape(ResolveFullPath(gameDir, patch)) : "-",
                        patch is not null ? Markup.Escape(patch.PatchSummary) : "-",
                        $"[{statusColor}]{statusText}[/]",
                        Markup.Escape(r.Reason ?? "")
                    );
                }

                PatchStateStore.RecordRevert(gameDir, patchSet);
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("[green]Done.[/]");
        });
    }

    internal static async Task RunCheckPatchStatusAsync(string gameDir)
    {
        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            return;
        }

        await Task.Run(() =>
        {
            var state = PatchStateStore.Load(gameDir);
            var patchSets = DiscoverPatches();
            var verifyBySet = patchSets.ToDictionary(
                ps => ps,
                ps => BinaryPatcher.Verify(ps, gameDir).ToDictionary(v => v.PatchId)
            );

            // ── Per-patch detail table ─────────────────────────────────────
            var detail = new Table()
                .RoundedBorder()
                .Title("[bold]Patch Details[/]")
                .AddColumn("Patch ID")
                .AddColumn("File")
                .AddColumn("Change")
                .AddColumn("File state");

            foreach (var patchSet in patchSets)
            {
                var verifyMap = verifyBySet[patchSet];
                foreach (var patch in patchSet.Patches)
                {
                    verifyMap.TryGetValue(patch.Id, out var vr);
                    var fileState = vr switch
                    {
                        { FileExists: false } => "[red]File missing[/]",
                        { NeedsApply: true } => "[grey]Not applied[/]",
                        _ => "[green]Applied[/]",
                    };

                    detail.AddRow(
                        Markup.Escape(patch.Id),
                        Markup.Escape(ResolveFullPath(gameDir, patch)),
                        Markup.Escape(patch.PatchSummary),
                        fileState
                    );
                }
            }

            AnsiConsole.Write(detail);

            // ── Per-set summary table ──────────────────────────────────────
            var summary = new Table()
                .RoundedBorder()
                .Title("[bold]Patch Set Summary[/]")
                .AddColumn("Patch set")
                .AddColumn("Version")
                .AddColumn("Recorded")
                .AddColumn("Applied at")
                .AddColumn("Overall");

            foreach (var patchSet in patchSets)
            {
                var entry = state.Applied.Find(e => e.PatchSetName == patchSet.Name);
                var recorded = entry is not null;
                var appliedAt = entry is not null ? entry.AppliedAt.ToString("u") : "-";
                var verifyMap = verifyBySet[patchSet];
                var needsApply = verifyMap.Values.Any(v => v.NeedsApply);
                var missingFiles = verifyMap.Values.Any(v => !v.FileExists);

                var overall = (recorded, needsApply, missingFiles) switch
                {
                    (_, _, true) => "[red]Missing files[/]",
                    (true, false, _) => "[green]Clean[/]",
                    (true, true, _) => "[yellow]Drift[/]",
                    (false, true, _) => "[grey]Not applied[/]",
                    (false, false, _) => "[grey]Not applied (or already clean)[/]",
                };

                summary.AddRow(
                    Markup.Escape(patchSet.Name),
                    Markup.Escape(patchSet.Version),
                    recorded ? "[green]Yes[/]" : "[grey]No[/]",
                    appliedAt,
                    overall
                );
            }

            AnsiConsole.Write(summary);
        });
    }

    // ── Save game dumper ───────────────────────────────────────────────────

    // ── Generic file dumper ────────────────────────────────────────────────

    // ── Diagnostic: dump mob files ─────────────────────────────────────────

    internal static async Task RunDumpMobFilesAsync(string gameDir, string? mapPath = null)
    {
        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            return;
        }

        var datPath = Path.Combine(gameDir, "modules", "Arcanum.dat");
        if (!File.Exists(datPath))
        {
            AnsiConsole.MarkupLine($"[red]Arcanum.dat not found at: {Markup.Escape(datPath)}[/]");
            return;
        }

        await Task.Run(() =>
        {
            using var archive = DatArchive.Open(datPath);

            if (string.IsNullOrWhiteSpace(mapPath))
            {
                AnsiConsole.MarkupLine("[red]Map path prefix is required (e.g. maps\\\\SomeMap\\\\).[/]");
                return;
            }

            // Collect all .mob entry paths from the specified map directory in the DAT,
            // merged with any loose override files present on disk.
            var datEntries = archive
                .Entries.Select(e => e.Path)
                .Where(p =>
                    p.StartsWith(mapPath, StringComparison.OrdinalIgnoreCase)
                    && p.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            var looseDir = Path.Combine(gameDir, "modules", "Arcanum", mapPath);
            var looseEntries = Directory.Exists(looseDir)
                ? Directory
                    .EnumerateFiles(looseDir, "*.mob", SearchOption.TopDirectoryOnly)
                    .Select(f => mapPath + Path.GetFileName(f))
                    .Where(p => !datEntries.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList()
                : [];

            var allEntries = datEntries.Concat(looseEntries).OrderBy(p => p).ToList();
            AnsiConsole.MarkupLine($"[bold]Found {allEntries.Count} mob files in cave.[/]");

            foreach (var entryPath in allEntries)
            {
                var loosePath = Path.Combine(gameDir, "modules", "Arcanum", entryPath);
                byte[] data;
                string source;

                if (File.Exists(loosePath))
                {
                    data = File.ReadAllBytes(loosePath);
                    source = $"loose: {Path.GetFileName(entryPath)}";
                }
                else
                {
                    try
                    {
                        data = archive.GetEntryData(entryPath).ToArray();
                        source = $"DAT: {Path.GetFileName(entryPath)}";
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }
                }

                MobData mob;
                try
                {
                    mob = MobFormat.ParseMemory(data);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]SKIP {Markup.Escape(Path.GetFileName(entryPath))}: parse error — {Markup.Escape(ex.Message)}[/]"
                    );
                    continue;
                }

                // Only print detail for containers (they have inventory) or if requested for all.
                var isContainer = mob.Header.GameObjectType == ArcNET.GameObjects.ObjectType.Container;
                var invProp = mob.Properties.FirstOrDefault(p =>
                    p.Field == ArcNET.GameObjects.ObjectField.ObjFContainerInventoryListIdx
                );
                var srcProp = mob.Properties.FirstOrDefault(p =>
                    p.Field == ArcNET.GameObjects.ObjectField.ObjFContainerInventorySource
                );
                var numProp = mob.Properties.FirstOrDefault(p =>
                    p.Field == ArcNET.GameObjects.ObjectField.ObjFContainerInventoryNum
                );

                if (!isContainer)
                    continue; // skip non-container mobs silently

                AnsiConsole.MarkupLine($"\n[bold cyan]{Markup.Escape(source)}[/]  ({data.Length}B)");
                var invNum = (isContainer && numProp?.RawBytes.Length == 4) ? numProp.GetInt32() : -1;
                var invSrc = (isContainer && srcProp?.RawBytes.Length == 4) ? srcProp.GetInt32() : -1;
                AnsiConsole.MarkupLine($"  Type={mob.Header.GameObjectType}  InvNum={invNum}  InvSrc={invSrc}");

                if (invProp is null)
                {
                    AnsiConsole.MarkupLine("  [grey]No inventory list field present.[/]");
                    continue;
                }

                try
                {
                    var items = invProp.GetObjectIdArrayFull();
                    AnsiConsole.MarkupLine($"  [green]Inventory items ({items.Length}):[/]");
                    for (var idx = 0; idx < items.Length; idx++)
                    {
                        var (oidType, protoOrData1, guid) = items[idx];
                        var typeLabel = oidType switch
                        {
                            1 => "A",
                            2 => "GUID",
                            -2 => "HANDLE",
                            _ => oidType.ToString(),
                        };
                        var idInfo = oidType == 1 ? $"proto={protoOrData1, 6}" : $"d.a=0x{protoOrData1:X8}";
                        // Use Console.WriteLine — Spectre parses [] in guid/index as markup.
                        Console.WriteLine($"  [{idx, 3}] type={typeLabel, -6} {idInfo}  guid={guid}");
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"  [red]Failed to decode inventory list: {Markup.Escape(ex.Message)}[/]");
                }
            }

            AnsiConsole.MarkupLine("\n[green]Done.[/]");
        });
    }

    // ── Diagnostic: list map folders in DAT ────────────────────────────────

    internal static async Task RunListMapsAsync(string gameDir)
    {
        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            return;
        }

        var datPath = Path.Combine(gameDir, "modules", "Arcanum.dat");
        if (!File.Exists(datPath))
        {
            AnsiConsole.MarkupLine($"[red]Arcanum.dat not found at: {Markup.Escape(datPath)}[/]");
            return;
        }

        await Task.Run(() =>
        {
            using var archive = DatArchive.Open(datPath);

            var mapFolders = archive
                .Entries.Select(e => e.Path)
                .Where(p => p.StartsWith("maps\\", StringComparison.OrdinalIgnoreCase))
                .Select(p =>
                {
                    // Extract the map folder name (second path segment)
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

    // ── Diagnostic: full dump of all mobs + protos for a map ──────────────

    internal static async Task RunDumpMapAsync(string gameDir, string mapPrefix, string outputFile)
    {
        if (!Directory.Exists(gameDir))
        {
            AnsiConsole.MarkupLine("[red]Directory not found.[/]");
            return;
        }

        var datPath = Path.Combine(gameDir, "modules", "Arcanum.dat");
        if (!File.Exists(datPath))
        {
            AnsiConsole.MarkupLine($"[red]Arcanum.dat not found at: {Markup.Escape(datPath)}[/]");
            return;
        }

        await Task.Run(() =>
        {
            using var archive = DatArchive.Open(datPath);
            using var writer = new StreamWriter(outputFile, append: false, System.Text.Encoding.UTF8);

            // ── Load proto name lookup (searches ArcanumN.dat at game root) ─
            var nameLookup = ItemDumper.LoadProtoNameLookup(gameDir);
            AnsiConsole.MarkupLine($"[grey]Loaded {nameLookup.Count} proto names from description.mes.[/]");

            // ── Detect installation type (vanilla vs UAP) ──────────────────
            var installation = ArcanumInstallation.Detect(gameDir);
            AnsiConsole.MarkupLine($"[grey]Installation: {installation}[/]");

            // ── Collect matching .mob entries ──────────────────────────────
            var mobEntries = archive
                .Entries.Select(e => e.Path)
                .Where(p =>
                    p.StartsWith(mapPrefix, StringComparison.OrdinalIgnoreCase)
                    && p.EndsWith(".mob", StringComparison.OrdinalIgnoreCase)
                )
                .OrderBy(p => p)
                .ToList();

            AnsiConsole.MarkupLine($"[bold]Found {mobEntries.Count} mob files under '{Markup.Escape(mapPrefix)}'.[/]");

            var parseErrors = new List<string>();
            var protoIdsSeen = new SortedSet<int>();

            // ── First pass: parse all mobs and build GUID index ───────────
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

            // ── Write dump header ──────────────────────────────────────────
            writer.WriteLine($"MAP DUMP — prefix: {mapPrefix}");
            writer.WriteLine($"Generated: {DateTime.UtcNow:u}");
            writer.WriteLine($"Total mob files: {mobEntries.Count}  (parsed: {parsed.Count})");
            writer.WriteLine(new string('=', 80));
            writer.WriteLine();

            // ── Second pass: dump each mob with name + item/inventory annotations ──
            foreach (var (entryPath, data, mob) in parsed)
            {
                var protoNum = ExtractProtoNum(mob);
                var protoName = protoNum is > 0 ? ResolveProtoName(protoNum.Value, nameLookup, installation) : null;

                writer.WriteLine(
                    $"FILE: {Path.GetFileName(entryPath)}  ({data.Length} B)"
                        + (protoName is not null ? $"  [{protoName}]" : "")
                );
                writer.WriteLine(MobDumper.Dump(mob));

                // Item summary for standalone item-type mobs
                if (protoNum is > 0 && IsDumpableItemType(mob.Header.GameObjectType))
                {
                    writer.WriteLine("=== ITEM SUMMARY ===");
                    writer.Write(ItemDumper.DumpItem(mob, protoNum.Value, nameLookup, installation));
                    writer.WriteLine();
                }

                // Resolved inventory for containers
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

                // Resolved inventory for NPCs/PCs
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

            // ── Dump parse errors ──────────────────────────────────────────
            if (parseErrors.Count > 0)
            {
                writer.WriteLine(new string('=', 80));
                writer.WriteLine($"PARSE ERRORS ({parseErrors.Count}):");
                foreach (var err in parseErrors)
                    writer.WriteLine($"  {err}");
                writer.WriteLine();
            }

            // ── Dump protos referenced by the mobs ────────────────────────
            writer.WriteLine(new string('=', 80));
            writer.WriteLine($"PROTOS REFERENCED ({protoIdsSeen.Count} unique proto IDs):");
            writer.WriteLine();

            // Build proto lookup: search DAT for proto\*\*.pro entries
            var datProtoEntries = archive
                .Entries.Select(e => e.Path)
                .Where(p =>
                    p.StartsWith("proto\\", StringComparison.OrdinalIgnoreCase)
                    && p.EndsWith(".pro", StringComparison.OrdinalIgnoreCase)
                )
                .ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var protoId in protoIdsSeen)
            {
                // Proto files sit at {gameDir}\data\proto\{protoId:D6} - Name.pro (loose on disk).
                var protoFileName = $"{protoId:D6}";
                datProtoEntries.TryGetValue(protoFileName, out var protoEntryPath);

                // Loose on-disk protos: {gameDir}\data\proto\{protoId:D6}*.pro
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

            writer.WriteLine(new string('=', 80));
            writer.WriteLine("END OF DUMP");
            AnsiConsole.MarkupLine($"\n[green]Dump written to: {Markup.Escape(outputFile)}[/]");
        });
    }

    // ── Private helpers for dump-map ───────────────────────────────────────

    private static int? ExtractProtoNum(MobData mob)
    {
        if (mob.Header.ProtoId.OidType != 1)
            return null;
        var num = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(mob.Header.ProtoId.Id.ToByteArray());
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
        // Fallback for UAP-only IDs (1–20) that have no vanilla equivalent.
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
        catch
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
            // Food, Scroll, Ammo, Key add no info beyond what type= already says.
            _ => "",
        };
    }
}
