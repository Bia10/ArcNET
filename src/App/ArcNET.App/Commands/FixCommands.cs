using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.State;
using ConsoleAppFramework;
using Spectre.Console;

namespace ArcNET.App;

/// <summary>
/// <c>fix</c> command group — apply, revert, and verify game-data bug corrections.
/// Usage: <c>arcnet fix &lt;subcommand&gt; &lt;gameDir&gt;</c>
/// </summary>
public sealed class FixCommands
{
    /// <summary>Apply all game-data bug fixes to the Arcanum installation.</summary>
    public async Task Apply([Argument] string gameDir)
    {
        if (!ValidateGameDir(gameDir))
            return;

        await Task.Run(() =>
        {
            var patchSets = DiscoverPatches();

            var preview = new Table()
                .RoundedBorder()
                .Title("[bold]Patches to apply[/]")
                .AddColumn("Patch set")
                .AddColumn("Patch ID")
                .AddColumn("File")
                .AddColumn("Change");

            foreach (var patchSet in patchSets)
                foreach (var patch in patchSet.Patches)
                    preview.AddRow(
                        Markup.Escape(patchSet.Name),
                        Markup.Escape(patch.Id),
                        Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath)),
                        Markup.Escape(patch.PatchSummary)
                    );

            AnsiConsole.Write(preview);

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
                        patch is not null
                            ? Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath))
                            : "-",
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

    /// <summary>Revert previously applied game-data bug fixes.</summary>
    public async Task Revert([Argument] string gameDir)
    {
        if (!ValidateGameDir(gameDir))
            return;

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
                        patch is not null
                            ? Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath))
                            : "-",
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

    /// <summary>Check the current patch state and verify file hashes.</summary>
    public async Task Check([Argument] string gameDir)
    {
        if (!ValidateGameDir(gameDir))
            return;

        await Task.Run(() =>
        {
            var state = PatchStateStore.Load(gameDir);
            var patchSets = DiscoverPatches();
            var verifyBySet = patchSets.ToDictionary(
                ps => ps,
                ps => BinaryPatcher.Verify(ps, gameDir).ToDictionary(v => v.PatchId)
            );

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
                        Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath)),
                        Markup.Escape(patch.PatchSummary),
                        fileState
                    );
                }
            }

            AnsiConsole.Write(detail);

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

    /// <summary>
    /// Checks that <paramref name="gameDir"/> exists, printing a user-facing error and
    /// returning <see langword="false"/> when it does not.
    /// </summary>
    private static bool ValidateGameDir(string gameDir)
    {
        if (Directory.Exists(gameDir))
            return true;

        AnsiConsole.MarkupLine("[red]Directory not found.[/]");
        return false;
    }

    private static IReadOnlyList<BinaryPatchSet> DiscoverPatches() =>
        PatchDiscovery.LoadAll(
            onError: (file, ex) =>
                AnsiConsole.MarkupLine(
                    $"[yellow]Skipped '{Markup.Escape(Path.GetFileName(file))}': {Markup.Escape(ex.Message)}[/]"
                )
        );
}
