using ArcNET.Diagnostics;

namespace Probe.Commands;

internal sealed class QuestBookCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var context = SharedProbeContext.Load(saveDir, slot4);
        var questCatalog = QuestLabelCatalogLoader.TryLoadFromSaveDirectory(saveDir);
        var snapshot = SavePlayerQuestBookService.Create(context.Save, questCatalog);

        Console.WriteLine($"QUEST BOOK  [{context.SlotStem}]  {snapshot.LeaderName} lv={snapshot.LeaderLevel}");
        if (snapshot.QuestCatalog is not null)
            Console.WriteLine($"  Quest labels: {snapshot.QuestCatalog.Source}");

        if (snapshot.QuestCharacters.Count > 1)
        {
            Console.WriteLine("\n  Quest-bearing character records:");
            foreach (var character in snapshot.QuestCharacters)
            {
                var selectedSuffix = character.IsSelectedPlayer ? " [selected]" : string.Empty;
                Console.WriteLine($"    {character.Path}{selectedSuffix}");
                Console.WriteLine(
                    $"      lv={character.Level} race={character.Race} quests={character.QuestCount} rep={character.ReputationCount} bless={character.BlessingCount} size={character.RawBytesLength}B name={character.Name ?? "(none)"}"
                );
            }
        }

        if (snapshot.Player is null)
        {
            Console.WriteLine("\n  Player record not found.");
            return Task.CompletedTask;
        }

        var player = snapshot.Player;
        Console.WriteLine($"\n  Player path: {player.Path}");
        Console.WriteLine(
            $"  QuestCount={player.QuestCount}  ReputationRaw={(player.ReputationCount == 0 ? "null" : $"[{player.ReputationCount}]")}  Schematics={player.SchematicsCount}  Blessings={player.BlessingCount}"
        );

        if (snapshot.Quests.Count == 0)
        {
            Console.WriteLine("\n  Quest book: absent (QuestCount=0)");
        }
        else
        {
            Console.WriteLine("\n  Quest book:");
            foreach (var quest in snapshot.Quests.OrderBy(static quest => quest.ProtoId))
            {
                var labelSuffix = quest.Label is null ? string.Empty : $" [{quest.Label}]";
                Console.WriteLine(
                    $"    q{quest.ProtoId}{labelSuffix}  state={quest.StateDescription}  ctx={quest.Context}  ts={quest.Timestamp}"
                );
            }
        }

        Console.WriteLine("\n  Reputation:");
        if (snapshot.Reputation.Count == 0)
        {
            Console.WriteLine("    absent");
        }
        else
        {
            foreach (var entry in snapshot.Reputation)
                Console.WriteLine($"    slot{entry.Slot}={entry.Value}");
        }

        Console.WriteLine($"\n  Blessings ({snapshot.Blessings.Count}): {FormatIntList(snapshot.Blessings)}");
        Console.WriteLine($"  Curses ({snapshot.Curses.Count}): {FormatIntList(snapshot.Curses)}");
        Console.WriteLine($"  Schematics ({snapshot.Schematics.Count}): {FormatIntList(snapshot.Schematics)}");

        return Task.CompletedTask;
    }

    private static string FormatIntList(IReadOnlyList<int> values) =>
        values.Count == 0 ? "(none)" : string.Join(", ", values);
}
