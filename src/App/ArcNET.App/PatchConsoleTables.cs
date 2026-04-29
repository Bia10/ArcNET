using ArcNET.BinaryPatch;
using ArcNET.BinaryPatch.State;
using Spectre.Console;

namespace ArcNET.App;

internal static class PatchConsoleTables
{
    public static Table CreateApplyPreviewTable(string gameDir, IReadOnlyList<BinaryPatchSet> patchSets)
    {
        var table = new Table()
            .RoundedBorder()
            .Title("[bold]Patches to apply[/]")
            .AddColumn("Patch set")
            .AddColumn("Patch ID")
            .AddColumn("File")
            .AddColumn("Change");

        foreach (var patchSet in patchSets)
        foreach (var patch in patchSet.Patches)
            table.AddRow(
                Markup.Escape(patchSet.Name),
                Markup.Escape(patch.Id),
                Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath)),
                Markup.Escape(patch.PatchSummary)
            );

        return table;
    }

    public static Table CreateApplyResultsTable() => CreateResultsTable("[bold]Apply results[/]");

    public static Table CreateRevertResultsTable() => CreateResultsTable("[bold]Revert results[/]");

    public static Table CreatePatchDetailsTable() =>
        new Table()
            .RoundedBorder()
            .Title("[bold]Patch Details[/]")
            .AddColumn("Patch ID")
            .AddColumn("File")
            .AddColumn("Change")
            .AddColumn("File state");

    public static Table CreatePatchSetSummaryTable() =>
        new Table()
            .RoundedBorder()
            .Title("[bold]Patch Set Summary[/]")
            .AddColumn("Patch set")
            .AddColumn("Version")
            .AddColumn("Recorded")
            .AddColumn("Applied at")
            .AddColumn("Overall");

    public static void AddApplyRows(
        Table table,
        string gameDir,
        BinaryPatchSet patchSet,
        IReadOnlyList<PatchResult> results
    )
    {
        AddResultRows(table, gameDir, patchSet, results, FormatApplyStatus);
    }

    public static void AddRevertRows(
        Table table,
        string gameDir,
        BinaryPatchSet patchSet,
        IReadOnlyList<PatchResult> results
    )
    {
        AddResultRows(table, gameDir, patchSet, results, FormatRevertStatus);
    }

    public static void AddDetailRows(
        Table table,
        string gameDir,
        BinaryPatchSet patchSet,
        IReadOnlyDictionary<string, PatchVerifyResult> verifyResults
    )
    {
        foreach (var patch in patchSet.Patches)
        {
            verifyResults.TryGetValue(patch.Id, out var verifyResult);
            table.AddRow(
                Markup.Escape(patch.Id),
                Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath)),
                Markup.Escape(patch.PatchSummary),
                FormatFileState(verifyResult)
            );
        }
    }

    public static void AddSummaryRow(
        Table table,
        BinaryPatchSet patchSet,
        PatchStateEntry? entry,
        IEnumerable<PatchVerifyResult> verifyResults
    )
    {
        var recorded = entry is not null;
        var appliedAt = entry is not null ? entry.AppliedAt.ToString("u") : "-";
        var needsApply = verifyResults.Any(result => result.NeedsApply);
        var missingFiles = verifyResults.Any(result => !result.FileExists);

        table.AddRow(
            Markup.Escape(patchSet.Name),
            Markup.Escape(patchSet.Version),
            recorded ? "[green]Yes[/]" : "[grey]No[/]",
            appliedAt,
            FormatOverall(recorded, needsApply, missingFiles)
        );
    }

    private static Table CreateResultsTable(string title) =>
        new Table()
            .RoundedBorder()
            .Title(title)
            .AddColumn("Patch ID")
            .AddColumn("File")
            .AddColumn("Change")
            .AddColumn("Status")
            .AddColumn("Detail");

    private static void AddResultRows(
        Table table,
        string gameDir,
        BinaryPatchSet patchSet,
        IReadOnlyList<PatchResult> results,
        Func<PatchStatus, string> formatStatus
    )
    {
        var patchById = patchSet.Patches.ToDictionary(patch => patch.Id);
        foreach (var result in results)
        {
            patchById.TryGetValue(result.PatchId, out var patch);
            table.AddRow(
                Markup.Escape(result.PatchId),
                patch is not null ? Markup.Escape(BinaryPatcher.ResolvePath(gameDir, patch.Target.RelativePath)) : "-",
                patch is not null ? Markup.Escape(patch.PatchSummary) : "-",
                formatStatus(result.Status),
                Markup.Escape(result.Reason ?? string.Empty)
            );
        }
    }

    private static string FormatApplyStatus(PatchStatus status) =>
        status switch
        {
            PatchStatus.Applied => "[green]Applied[/]",
            PatchStatus.AlreadyApplied => "[yellow]Already applied[/]",
            PatchStatus.Skipped => "[grey]Skipped[/]",
            PatchStatus.Failed => "[red]Failed[/]",
            _ => $"[white]{Markup.Escape(status.ToString())}[/]",
        };

    private static string FormatRevertStatus(PatchStatus status) =>
        status switch
        {
            PatchStatus.Applied => "[green]Reverted[/]",
            PatchStatus.Skipped => "[yellow]Skipped (no backup)[/]",
            PatchStatus.Failed => "[red]Failed[/]",
            _ => $"[white]{Markup.Escape(status.ToString())}[/]",
        };

    private static string FormatFileState(PatchVerifyResult? verifyResult) =>
        verifyResult switch
        {
            { FileExists: false } => "[red]File missing[/]",
            { NeedsApply: true } => "[grey]Not applied[/]",
            _ => "[green]Applied[/]",
        };

    private static string FormatOverall(bool recorded, bool needsApply, bool missingFiles) =>
        (recorded, needsApply, missingFiles) switch
        {
            (_, _, true) => "[red]Missing files[/]",
            (true, false, _) => "[green]Clean[/]",
            (true, true, _) => "[yellow]Drift[/]",
            (false, true, _) => "[grey]Not applied[/]",
            (false, false, _) => "[grey]Not applied (or already clean)[/]",
        };
}
