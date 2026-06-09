using ArcNET.Diagnostics;

namespace Probe.Commands;

/// <summary>
/// Mode 16: Full character summary via the shared diagnostics snapshot.
/// </summary>
internal sealed class CharSummaryCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine($"\n=== Mode 16: character summary - {ctx.SlotStem} ===\n");

        var summary = SavePlayerCharacterSummaryService.Create(ctx.Save);
        if (summary is null)
        {
            Console.WriteLine("  No PC v2 record found in this save.");
            return Task.CompletedTask;
        }

        var primary = summary.PrimaryAttributes;
        Console.WriteLine("  PRIMARY ATTRIBUTES");
        Console.WriteLine(
            $"    {primary[0].Label}={primary[0].Value, -3} {primary[1].Label}={primary[1].Value, -3} {primary[2].Label}={primary[2].Value, -3} {primary[3].Label}={primary[3].Value, -3}"
        );
        Console.WriteLine(
            $"    {primary[4].Label}={primary[4].Value, -3} {primary[5].Label}={primary[5].Value, -3} {primary[6].Label}={primary[6].Value, -3} {primary[7].Label}={primary[7].Value, -3}"
        );
        Console.WriteLine();

        var derived = summary.DerivedStats;
        Console.WriteLine("  DERIVED STATS");
        Console.WriteLine(
            $"    {derived[0].Label}={derived[0].Value, -5} {derived[1].Label}={derived[1].Value, -4} {derived[2].Label}={derived[2].Value, -4} {derived[3].Label}={derived[3].Value, -4}"
        );
        Console.WriteLine(
            $"    {derived[4].Label}={derived[4].Value, -3} {derived[5].Label}={derived[5].Value, -3} {derived[6].Label}={derived[6].Value, -4} {derived[7].Label}={derived[7].Value, -2} {derived[8].Label}={derived[8].Value, -3}"
        );
        Console.WriteLine();

        var progression = summary.Progression;
        Console.WriteLine("  PROGRESSION");
        Console.WriteLine(
            $"    name=\"{progression.Name ?? "(none)"}\"  lv={progression.Level}  XP={progression.ExperiencePoints}  align={progression.Alignment}  fate={progression.FatePoints}"
        );
        Console.WriteLine(
            $"    race={progression.Race}  gender={progression.Gender}  age={progression.Age}  poison={progression.PoisonLevel}  unspent={progression.UnspentPoints}"
        );
        Console.WriteLine(
            $"    magic={progression.MagickPoints}  tech={progression.TechPoints}  gold={progression.Gold}  arrows={progression.Arrows}  kills={progression.TotalKills}"
        );
        if (progression.Bullets > 0 || progression.PowerCells > 0)
            Console.WriteLine($"    bullets={progression.Bullets}  powerCells={progression.PowerCells}");
        if (progression.HpDamage > 0 || progression.FatigueDamage > 0)
            Console.WriteLine($"    hp_dmg={progression.HpDamage}  fat_dmg={progression.FatigueDamage}");
        Console.WriteLine();

        var basic = summary.BasicSkills;
        Console.WriteLine("  BASIC SKILLS");
        Console.WriteLine(
            $"    {basic[0].Label}={basic[0].Value} {basic[1].Label}={basic[1].Value} {basic[2].Label}={basic[2].Value} {basic[3].Label}={basic[3].Value}"
        );
        Console.WriteLine(
            $"    {basic[4].Label}={basic[4].Value} {basic[5].Label}={basic[5].Value} {basic[6].Label}={basic[6].Value} {basic[7].Label}={basic[7].Value}"
        );
        Console.WriteLine(
            $"    {basic[8].Label}={basic[8].Value} {basic[9].Label}={basic[9].Value} {basic[10].Label}={basic[10].Value} {basic[11].Label}={basic[11].Value}"
        );
        Console.WriteLine();

        var techSkills = summary.TechSkills;
        Console.WriteLine("  TECH SKILLS");
        Console.WriteLine(
            $"    {techSkills[0].Label}={techSkills[0].Value} {techSkills[1].Label}={techSkills[1].Value} {techSkills[2].Label}={techSkills[2].Value} {techSkills[3].Label}={techSkills[3].Value}"
        );
        Console.WriteLine();

        var activeSpells = summary
            .SpellColleges.Where(static spell => spell.Value > 0)
            .Select(static spell => $"{spell.Label}={spell.Value}")
            .ToArray();
        Console.WriteLine("  SPELL COLLEGES");
        Console.WriteLine(activeSpells.Length == 0 ? "    (none)" : "    " + string.Join("  ", activeSpells));
        Console.WriteLine();

        var activeTech = summary
            .TechDisciplines.Where(static discipline => discipline.Value > 0)
            .Select(static discipline => $"{discipline.Label}={discipline.Value}")
            .ToArray();
        Console.WriteLine("  TECH DISCIPLINES");
        Console.WriteLine(activeTech.Length == 0 ? "    (none)" : "    " + string.Join("  ", activeTech));
        Console.WriteLine();

        Console.WriteLine($"  QUEST LOG  ({summary.QuestLog.Count} entries)");
        if (summary.QuestLog.RawBytesLength is null)
            Console.WriteLine("    (absent)");
        else
            Console.WriteLine(
                $"    raw={summary.QuestLog.RawBytesLength}B  bitset={summary.QuestLog.BitsetWordCount ?? 0}w"
            );
        Console.WriteLine();

        Console.WriteLine(
            $"  REPUTATION  ({(summary.Reputation.Count == 0 ? "absent" : $"{summary.Reputation.Count} factions")})"
        );
        if (summary.Reputation.Count > 0)
        {
            var nonZeroRep = summary
                .Reputation.Where(static entry => entry.Value != 0)
                .Select(static entry => $"slot{entry.Slot}={entry.Value}")
                .ToArray();
            Console.WriteLine(nonZeroRep.Length == 0 ? "    (all zeros)" : "    " + string.Join("  ", nonZeroRep));
        }
        Console.WriteLine();

        Console.WriteLine($"  BLESSINGS  ({summary.Blessings.Count})");
        Console.WriteLine(
            summary.Blessings.Count == 0 ? "    (none)" : $"    protoIDs: {FormatInts(summary.Blessings)}"
        );
        Console.WriteLine($"  CURSES  ({summary.Curses.Count})");
        Console.WriteLine(summary.Curses.Count == 0 ? "    (none)" : $"    protoIDs: {FormatInts(summary.Curses)}");
        Console.WriteLine();

        Console.WriteLine($"  SCHEMATICS  ({summary.Schematics.Count})");
        Console.WriteLine(
            summary.Schematics.Count == 0 ? "    (none)" : $"    protoIDs: {FormatInts(summary.Schematics)}"
        );
        Console.WriteLine();

        Console.WriteLine($"  RUMORS  ({summary.Rumors.Count})");
        Console.WriteLine(
            summary.Rumors.RawBytesLength is null ? "    (absent)" : $"    raw={summary.Rumors.RawBytesLength}B"
        );

        return Task.CompletedTask;
    }

    private static string FormatInts(IReadOnlyList<int> values) => string.Join(", ", values);
}
