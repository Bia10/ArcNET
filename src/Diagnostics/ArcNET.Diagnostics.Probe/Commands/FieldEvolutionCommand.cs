using ArcNET.Diagnostics;

namespace Probe.Commands;

internal sealed class FieldEvolutionCommand : IProbeCommand
{
    private const int DefaultRecentSlotCount = 12;

    public Task RunAsync(string saveDir, string[] args)
    {
        int firstSlot;
        int lastSlot;

        if (args.Length == 0)
        {
            (firstSlot, lastSlot) = ProbeConfig.ResolveRecentSlotRange(saveDir, DefaultRecentSlotCount);
            Console.Error.WriteLine(
                $"[probe] No slot range specified for field-evolution; defaulting to the most recent {DefaultRecentSlotCount} available slots ({firstSlot:D4}-{lastSlot:D4})."
            );
        }
        else if (
            args.Length >= 2
            && int.TryParse(args[0], out firstSlot)
            && int.TryParse(args[1], out lastSlot)
            && lastSlot >= firstSlot
        ) { }
        else
        {
            Console.WriteLine("Usage: probe 13 [fromSlot4 toSlot4]");
            Console.WriteLine("  Example: probe 13 0170 0178");
            Console.WriteLine(
                $"  Or omit both slots to scan the most recent {DefaultRecentSlotCount} available saves safely."
            );
            return Task.CompletedTask;
        }

        var snapshot = SavePlayerProgressionHistoryService.Create(saveDir, firstSlot, lastSlot);

        Console.WriteLine($"FIELD EVOLUTION  slots {firstSlot:D4}-{lastSlot:D4}");
        Console.WriteLine($"  Tracked: {SavePlayerProgressionHistoryService.TrackedFieldsSummary}");
        if (snapshot.QuestCatalog is not null)
            Console.WriteLine($"  Quest labels: {snapshot.QuestCatalog.Source}");

        foreach (var slot in snapshot.Slots)
        {
            if (slot.State is null)
            {
                Console.WriteLine($"  [{slot.SlotStem}] player record not found");
                continue;
            }

            if (slot.IsBaseline)
            {
                Console.WriteLine(
                    $"  [{slot.SlotStem}] lv={slot.State.Level} XP={slot.State.Xp} gold={slot.State.Gold} quests={slot.State.QuestCount} rumors={slot.State.RumorsCount} blessings={slot.State.BlessingCount} curses={slot.State.CurseCount} schematics={slot.State.SchematicsCount} hp_dmg={slot.State.HpDamage} fat_dmg={slot.State.FatigueDamage} bullets={slot.State.Bullets} powerCells={slot.State.PowerCells} rep={FormatReputationSummary(slot.State.Reputation)}  (baseline)"
                );
                continue;
            }

            if (slot.Changes.Count == 0)
            {
                Console.WriteLine($"  [{slot.SlotStem}] no tracked changes");
                continue;
            }

            Console.WriteLine(
                $"  [{slot.SlotStem}] {string.Join("; ", slot.Changes.Select(static change => change.Description))}"
            );
        }

        return Task.CompletedTask;
    }

    private static string FormatReputationSummary(IReadOnlyList<PlayerReputationEntrySnapshot> reputation) =>
        reputation.Count == 0 || reputation.All(static entry => entry.Value == 0)
            ? "none"
            : $"{reputation.Count}entries";
}
