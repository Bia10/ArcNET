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

            AnsiConsole.Write(PatchConsoleTables.CreateApplyPreviewTable(gameDir, patchSets));

            var results = PatchConsoleTables.CreateApplyResultsTable();

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
                PatchConsoleTables.AddApplyRows(results, gameDir, patchSet, patchResults);

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

            var table = PatchConsoleTables.CreateRevertResultsTable();

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
                PatchConsoleTables.AddRevertRows(table, gameDir, patchSet, patchResults);

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

            var detail = PatchConsoleTables.CreatePatchDetailsTable();

            foreach (var patchSet in patchSets)
                PatchConsoleTables.AddDetailRows(detail, gameDir, patchSet, verifyBySet[patchSet]);

            AnsiConsole.Write(detail);

            var summary = PatchConsoleTables.CreatePatchSetSummaryTable();

            foreach (var patchSet in patchSets)
            {
                var entry = state.Applied.Find(e => e.PatchSetName == patchSet.Name);
                var verifyMap = verifyBySet[patchSet];
                PatchConsoleTables.AddSummaryRow(summary, patchSet, entry, verifyMap.Values);
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
