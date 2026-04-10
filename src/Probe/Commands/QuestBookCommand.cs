using ArcNET.Formats;
using Probe;

namespace Probe.Commands;

internal sealed class QuestBookCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var questLookup = SarUtils.TryLoadQuestLookup(saveDir);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine($"\n=== Mode 14: Quest book + reputation - {ctx.SlotStem} ===");
        Console.WriteLine($"  Save: {ctx.Save.Info.LeaderName} lv={ctx.Save.Info.LeaderLevel}");
        Console.WriteLine(
            questLookup is not null
                ? $"  Quest labels: {questLookup.Labels.Count} loaded from {questLookup.Source}"
                : "  Quest labels: unavailable (no usable local quest lookup was found in loose data or DAT archives)"
        );

        var allQuestRecords = new List<CharacterMdyRecord>();
        foreach (var (_, mdyFile) in ctx.Save.MobileMdys)
        {
            foreach (var record in mdyFile.Records.Where(record => record.IsCharacter))
            {
                if (record.Character!.QuestCount > 0)
                    allQuestRecords.Add(record.Character!);
            }
        }

        if (allQuestRecords.Count > 1)
        {
            Console.WriteLine($"  [Debug] Quest-bearing records: {allQuestRecords.Count}");
            foreach (var record in allQuestRecords.OrderBy(record => record.RawBytes.Length))
            {
                var dbgLevel = record.Stats.Length > 17 ? record.Stats[17] : -1;
                var dbgRace = record.Stats.Length > 27 ? record.Stats[27] : -1;
                Console.WriteLine(
                    $"    lv={dbgLevel} race={dbgRace} quests={record.QuestCount} bless={record.BlessingProtoElementCount} size={record.RawBytes.Length}B name={record.Name ?? "(none)"}"
                );
            }
        }

        var player = SarUtils.FindPlayerRecord(ctx.Save);
        if (player is null)
        {
            Console.WriteLine("  [No player v2 record found]");
            return Task.CompletedTask;
        }

        var playerLevel = player.Stats.Length > 17 ? player.Stats[17] : -1;
        Console.WriteLine(
            $"  Player v2 record: lv={playerLevel} race={player.Stats[27]}  RawBytes={player.RawBytes.Length}B  Name={player.Name ?? "(none)"}"
        );
        Console.WriteLine(
            $"  QuestCount={player.QuestCount}  ReputationRaw={(player.ReputationRaw is null ? "null" : $"[{player.ReputationRaw.Length}]")}  Schematics={player.SchematicsElementCount}  Blessings={player.BlessingProtoElementCount}"
        );

        var entries = player.QuestEntries;
        if (entries is null)
        {
            Console.WriteLine("\n  Quest book: absent (QuestCount=0)");
        }
        else
        {
            Console.WriteLine($"\n  Quest book: {entries.Count} entries");
            Console.WriteLine($"  {"QuestID", -10} {"Name", -36} {"State", -34}  {"Context", 8}  {"Timestamp", 12}");
            Console.WriteLine(new string('-', 112));
            foreach (var (protoId, context, timestamp, state) in entries)
            {
                var stateStr = SarUtils.FormatQuestState(state);
                var questName = SarUtils.ResolveQuestLabel(questLookup, protoId) is { } label
                    ? SarUtils.TruncateText(label, 36)
                    : "(unresolved)";
                Console.WriteLine($"  {protoId, -10} {questName, -36} {stateStr, -34}  {context, 8}  {timestamp, 12}");
            }

            var byState = entries.GroupBy(entry => entry.State).OrderBy(group => group.Key).ToList();
            Console.WriteLine("\n  State summary:");
            foreach (var group in byState)
            {
                Console.WriteLine(
                    $"    {SarUtils.FormatQuestState(group.Key)}: {group.Count()} quests  IDs=[{string.Join(",", group.Select(entry => entry.ProtoId))}]"
                );
            }
        }

        var reputation = player.ReputationRaw;
        if (reputation is null)
        {
            Console.WriteLine("\n  Reputation: absent (early save, no faction interactions yet)");
            return Task.CompletedTask;
        }

        var repSar = SarUtils
            .ParseSars(player.RawBytes)
            .FirstOrDefault(sar => sar.ESize == 4 && sar.ECnt == reputation.Length && sar.BCnt == 3);
        var decodedRep = repSar is not null
            ? SarUtils.DecodeReputation(repSar, reputation)
            : reputation
                .Select(
                    (value, index) =>
                        (
                            player.ReputationFactionSlots is { Length: var slotLen } slots
                            && slotLen == reputation.Length
                                ? slots[index]
                                : index,
                            value
                        )
                )
                .ToList();
        Console.WriteLine(
            $"\n  Reputation: {decodedRep.Count} entries  (faction slot IDs: {SarUtils.FormatSlotList(decodedRep.Select(x => x.Item1).ToArray(), 32)})"
        );
        Console.WriteLine($"  {"Slot", -6} {"Value", 8}");
        Console.WriteLine(new string('-', 20));
        foreach (var (slot, value) in decodedRep)
            Console.WriteLine($"  {slot, -6} {value, 8}");
        Console.WriteLine(
            $"  Non-zero factions: [{string.Join(", ", decodedRep.Where(x => x.Item2 != 0).Select(x => $"slot{x.Item1}={x.Item2}"))}]"
        );

        return Task.CompletedTask;
    }
}
