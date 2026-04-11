using ArcNET.Editor;
using ArcNET.Formats;
using Probe;

namespace Probe.Commands;

/// <summary>
/// Mode 15: List all v2 character records across every mobile.mdy in a save slot.
/// Useful for inspecting party members and companion states without running the game.
/// </summary>
internal sealed class NpcScanCommand : IProbeCommand
{
    private static readonly string[] s_raceNames =
    [
        "Human",
        "Halfling",
        "HalfElf",
        "Half-Ogre",
        "Dwarf",
        "Gnome",
        "HalfOrc",
        "?" /* 7 */
        ,
        "DarkElf",
        "Elf",
        "?" /* 10 */
        ,
    ];

    private static readonly string[] s_genderNames = ["Male", "Female"];

    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var showAll = args.Length > 1 && args[1].Equals("all", StringComparison.OrdinalIgnoreCase);

        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine($"\n=== Mode 15: v2 character scan - {ctx.SlotStem} ===");
        Console.WriteLine($"  Pass 'all' as second arg to also show incomplete (NPC-only) records.");
        Console.WriteLine();

        var totalV2 = 0;
        var totalPc = 0;
        var totalNpc = 0;
        var totalSkipped = 0;

        foreach (var (mdyPath, mdyFile) in ctx.Save.MobileMdys.OrderBy(e => e.Key))
        {
            var v2Records = mdyFile
                .Records.Select((rec, idx) => (rec, idx))
                .Where(pair => pair.rec.IsCharacter)
                .ToList();

            if (v2Records.Count == 0)
                continue;

            Console.WriteLine($"  [{mdyPath}]  ({v2Records.Count} v2 records)");

            foreach (var (rec, recIdx) in v2Records)
            {
                var ch = rec.Character!;
                var hasAll = ch.HasCompleteData;

                if (!hasAll && !showAll)
                {
                    totalSkipped++;
                    continue;
                }

                totalV2++;

                var level = ch.Stats.Length > 17 ? ch.Stats[17] : -1;
                var xp = ch.Stats.Length > 18 ? ch.Stats[18] : 0;
                var align = ch.Stats.Length > 19 ? ch.Stats[19] : 0;
                var race = ch.Stats.Length > 27 ? ch.Stats[27] : -1;
                var gender = ch.Stats.Length > 26 ? ch.Stats[26] : -1;
                var magPts = ch.Stats.Length > 22 ? ch.Stats[22] : 0;
                var techPts = ch.Stats.Length > 23 ? ch.Stats[23] : 0;

                var raceStr = race >= 0 && race < s_raceNames.Length ? s_raceNames[race] : $"race={race}";
                var genderStr = gender >= 0 && gender < s_genderNames.Length ? s_genderNames[gender] : $"g={gender}";
                var nameStr = ch.Name is { } n ? $"\"{n}\"" : "(no name)";
                var typeTag = hasAll ? "PC " : "NPC";

                // Spell/tech indicator
                var magStr = magPts > 0 ? $" magic={magPts}" : string.Empty;
                var techStr = techPts > 0 ? $" tech={techPts}" : string.Empty;

                // Ammo (tech chars)
                var ammoStr =
                    ch.Bullets > 0 || ch.PowerCells > 0 ? $" bullets={ch.Bullets} cells={ch.PowerCells}" : string.Empty;

                // HP / fatigue
                var hpDmgStr = ch.HpDamage > 0 ? $" hp_dmg={ch.HpDamage}" : string.Empty;
                var fatDmgStr = ch.FatigueDamage > 0 ? $" fat_dmg={ch.FatigueDamage}" : string.Empty;

                Console.WriteLine(
                    $"    [{recIdx:D3}] {typeTag}  {nameStr, -20} lv={level, -3} XP={xp, -8} {raceStr}/{genderStr}  align={align, -5}"
                        + $"  gold={ch.Gold}{magStr}{techStr}{ammoStr}{hpDmgStr}{fatDmgStr}"
                        + $"  ({ch.RawBytes.Length}B)"
                );

                // Basic skill summary for PC records
                if (hasAll && ch.BasicSkills.Length >= 12)
                {
                    var skills = ch.BasicSkills;
                    var nonZero = new List<string>();
                    string[] skillNames =
                    [
                        "BOW",
                        "DODGE",
                        "MELEE",
                        "THROW",
                        "BKSTB",
                        "PPKT",
                        "PROWL",
                        "STRAP",
                        "GAMBL",
                        "HAGGL",
                        "HEAL",
                        "PERS",
                    ];
                    for (var i = 0; i < Math.Min(skills.Length, skillNames.Length); i++)
                    {
                        if (skills[i] > 0)
                            nonZero.Add($"{skillNames[i]}={skills[i]}");
                    }
                    if (nonZero.Count > 0)
                        Console.WriteLine($"         skills: {string.Join(" ", nonZero)}");
                }

                if (hasAll)
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
