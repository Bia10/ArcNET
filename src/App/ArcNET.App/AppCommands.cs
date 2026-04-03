using ArcNET.Archive;
using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.State;
using ArcNET.Formats;
using ArcNET.GameData;
using ArcNET.Patch;
using Spectre.Console;

namespace ArcNET.App;

/// <summary>Top-level command implementations for the ArcNET CLI.</summary>
internal static class AppCommands
{
    internal const string ParseExtractedData = "Parse extracted game data";
    internal const string InstallHighResPatch = "Install High-Res patch";
    internal const string UninstallHighResPatch = "Uninstall High-Res patch";
    internal const string ApplyGameDataFixes = "Apply game data fixes (bug corrections)";
    internal const string RevertGameDataFixes = "Revert game data fixes";
    internal const string CheckPatchStatus = "Check patch status";
    internal const string DumpMobFiles = "Dump mob files (diagnostic)";

    internal static async Task RunAsync(string command)
    {
        switch (command)
        {
            case ParseExtractedData:
                await RunParseExtractedDataAsync();
                break;

            case InstallHighResPatch:
                await RunInstallHighResPatchAsync();
                break;

            case UninstallHighResPatch:
                AnsiConsole.MarkupLine("[yellow]Uninstall High-Res patch is not yet implemented.[/]");
                break;

            case ApplyGameDataFixes:
                await RunApplyGameDataFixesAsync();
                break;

            case RevertGameDataFixes:
                await RunRevertGameDataFixesAsync();
                break;

            case CheckPatchStatus:
                await RunCheckPatchStatusAsync();
                break;

            case DumpMobFiles:
                await RunDumpMobFilesAsync();
                break;

            default:
                AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
                break;
        }
    }

    private static async Task RunParseExtractedDataAsync()
    {
        var inputPath = AnsiConsole.Ask<string>("[green]Insert path to extracted game data directory[/]:");

        if (!Directory.Exists(inputPath))
        {
            AnsiConsole.MarkupLine("[red]Directory not found![/]");
            return;
        }

        await Task.Run(() =>
        {
            var files = GameDataLoader.DiscoverFiles(inputPath);
            var table = new Table().RoundedBorder().AddColumn("Format").AddColumn("File count");

            foreach (var (format, paths) in files)
                table.AddRow(format.ToString(), paths.Count.ToString());

            AnsiConsole.Write(table);
        });
    }

    private static async Task RunInstallHighResPatchAsync()
    {
        var arcDir = AnsiConsole.Ask<string>("[green]Insert path to Arcanum installation directory[/]:");
        var highResDir = Path.Combine(arcDir, "HighRes");

        if (!Directory.Exists(highResDir))
        {
            AnsiConsole.MarkupLine("[red]HighRes directory not found inside Arcanum dir![/]");
            return;
        }

        var configPath = Path.Combine(highResDir, "config.ini");
        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine("[red]config.ini not found in HighRes directory![/]");
            return;
        }

        var config = HighResConfig.ParseFile(configPath);
        AnsiConsole.MarkupLine($"[green]Loaded config: {config.Width}x{config.Height} @ {config.BitDepth}bpp[/]");

        await Task.CompletedTask;
    }

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

    private static async Task RunCheckPatchStatusAsync()
    {
        var gameDir = AnsiConsole.Ask<string>("[green]Arcanum installation directory[/] (e.g. C:\\Games\\Arcanum):");

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

    // ── Diagnostic: dump mob files ─────────────────────────────────────────

    private static async Task RunDumpMobFilesAsync()
    {
        var gameDir = AnsiConsole.Ask<string>(
            "[green]Arcanum installation directory[/] (e.g. C:\\Games\\Arcanum\\ArcanumClean):"
        );
        var mapPath = AnsiConsole.Ask<string>(
            "[green]Map path prefix in DAT[/] (e.g. maps\\Cave of the Bangellian Scourge\\):"
        );
        await RunDumpMobFilesAsync(gameDir, mapPath);
    }

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
}
