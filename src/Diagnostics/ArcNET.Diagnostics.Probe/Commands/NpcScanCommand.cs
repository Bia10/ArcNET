using ArcNET.Diagnostics;

namespace Probe.Commands;

/// <summary>
/// Mode 15: List all v2 character records across every mobile.mdy in a save slot.
/// Useful for inspecting party members and companion states without running the game.
/// </summary>
internal sealed class NpcScanCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var showAll = args.Length > 1 && args[1].Equals("all", StringComparison.OrdinalIgnoreCase);

        var ctx = SharedProbeContext.Load(saveDir, slot4);
        var snapshot = SaveCharacterCatalogService.Create(ctx.Save);

        Console.WriteLine($"\n=== Mode 15: v2 character scan - {ctx.SlotStem} ===");
        Console.WriteLine($"  Pass 'all' as second arg to also show incomplete (NPC-only) records.");
        Console.WriteLine();

        var totalV2 = 0;
        var totalPc = 0;
        var totalNpc = 0;
        var totalSkipped = 0;

        foreach (
            var group in snapshot
                .Records.GroupBy(static record => record.SourcePath)
                .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            var records = group.OrderBy(static record => record.RecordIndex).ToArray();
            if (records.Length == 0)
                continue;

            Console.WriteLine($"  [{group.Key}]  ({records.Length} v2 records)");

            foreach (var record in records)
            {
                if (!record.HasCompleteData && !showAll)
                {
                    totalSkipped++;
                    continue;
                }

                totalV2++;
                var typeTag = record.HasCompleteData ? "PC " : "NPC";
                var name = record.Name is { } value ? $"\"{value}\"" : "(no name)";
                var magStr = record.MagickPoints > 0 ? $" magic={record.MagickPoints}" : string.Empty;
                var techStr = record.TechPoints > 0 ? $" tech={record.TechPoints}" : string.Empty;
                var ammoStr =
                    record.Bullets > 0 || record.PowerCells > 0
                        ? $" bullets={record.Bullets} cells={record.PowerCells}"
                        : string.Empty;
                var hpDmgStr = record.HpDamage > 0 ? $" hp_dmg={record.HpDamage}" : string.Empty;
                var fatDmgStr = record.FatigueDamage > 0 ? $" fat_dmg={record.FatigueDamage}" : string.Empty;

                Console.WriteLine(
                    $"    [{record.RecordIndex:D3}] {typeTag}  {name, -20} lv={record.Level, -3} XP={record.ExperiencePoints, -8} {record.RaceName}/{record.GenderName}  align={record.Alignment, -5}"
                        + $"  gold={record.Gold}{magStr}{techStr}{ammoStr}{hpDmgStr}{fatDmgStr}"
                        + $"  ({record.RawBytesLength}B)"
                );

                if (record.HasCompleteData && record.NonZeroBasicSkills.Count > 0)
                {
                    Console.WriteLine(
                        $"         skills: {string.Join(" ", record.NonZeroBasicSkills.Select(static skill => $"{skill.Label}={skill.Value}"))}"
                    );
                }

                if (record.HasCompleteData)
                    totalPc++;
                else
                    totalNpc++;
            }

            Console.WriteLine();
        }

        if (totalSkipped > 0)
            Console.WriteLine($"  (Skipped {totalSkipped} NPC-only (incomplete) v2 records. Pass 'all' to show them.)");
        Console.WriteLine($"  Total: {totalV2} v2 records shown  ({totalPc} PC-complete, {totalNpc} NPC-only).");

        return Task.CompletedTask;
    }
}
