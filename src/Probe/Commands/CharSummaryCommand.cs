using ArcNET.Core;
using ArcNET.Editor;
using ArcNET.Formats;
using Bia.ValueBuffers;
using Probe;

namespace Probe.Commands;

/// <summary>
/// Mode 16: Full character summary via the high-level CharacterRecord API.
/// Prints stats, skills, spells, tech disciplines, and all extended fields
/// (quests, reputation, blessings, curses, schematics, rumors) for the player
/// character in the specified save slot.
/// </summary>
internal sealed class CharSummaryCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var slot4 = SharedProbeContext.ResolveSlot4(args);
        var ctx = SharedProbeContext.Load(saveDir, slot4);

        Console.WriteLine($"\n=== Mode 16: character summary - {ctx.SlotStem} ===\n");

        var playerRec = SarUtils.FindPlayerRecord(ctx.Save);
        if (playerRec is null)
        {
            Console.WriteLine("  No PC v2 record found in this save.");
            return Task.CompletedTask;
        }

        var pc = CharacterRecord.From(playerRec);

        // ── Primary attributes ────────────────────────────────────────────────
        Console.WriteLine("  PRIMARY ATTRIBUTES");
        Console.WriteLine(
            $"    STR={pc.Strength, -3} DEX={pc.Dexterity, -3} CON={pc.Constitution, -3} BEA={pc.Beauty, -3}"
        );
        Console.WriteLine(
            $"    INT={pc.Intelligence, -3} PER={pc.Perception, -3} WIL={pc.Willpower, -3} CHA={pc.Charisma, -3}"
        );
        Console.WriteLine();

        // ── Derived / combat ──────────────────────────────────────────────────
        Console.WriteLine("  DERIVED STATS");
        Console.WriteLine(
            $"    carry={pc.CarryWeight, -5} dmgBonus={pc.DamageBonus, -4} acAdj={pc.AcAdjustment, -4} speed={pc.Speed, -4}"
        );
        Console.WriteLine(
            $"    healRate={pc.HealRate, -3} poisonRec={pc.PoisonRecovery, -3} reactMod={pc.ReactionModifier, -4} maxFoll={pc.MaxFollowers, -2} mtApt={pc.MagickTechAptitude, -3}"
        );
        Console.WriteLine();

        // ── Progression ───────────────────────────────────────────────────────
        Console.WriteLine("  PROGRESSION");
        Console.WriteLine(
            $"    name=\"{pc.Name ?? "(none)"}\"  lv={pc.Level}  XP={pc.ExperiencePoints}  align={pc.Alignment}  fate={pc.FatePoints}"
        );
        Console.WriteLine(
            $"    race={pc.Race}  gender={pc.Gender}  age={pc.Age}  poison={pc.PoisonLevel}  unspent={pc.UnspentPoints}"
        );
        Console.WriteLine(
            $"    magic={pc.MagickPoints}  tech={pc.TechPoints}  gold={pc.Gold}  arrows={pc.Arrows}  kills={pc.TotalKills}"
        );
        if (pc.Bullets > 0 || pc.PowerCells > 0)
            Console.WriteLine($"    bullets={pc.Bullets}  powerCells={pc.PowerCells}");
        if (pc.HpDamage > 0 || pc.FatigueDamage > 0)
            Console.WriteLine($"    hp_dmg={pc.HpDamage}  fat_dmg={pc.FatigueDamage}");
        Console.WriteLine();

        // ── Basic skills ──────────────────────────────────────────────────────
        Console.WriteLine("  BASIC SKILLS");
        Console.WriteLine(
            $"    bow={pc.SkillBow} dodge={pc.SkillDodge} melee={pc.SkillMelee} throw={pc.SkillThrowing}"
        );
        Console.WriteLine(
            $"    backstab={pc.SkillBackstab} pickpocket={pc.SkillPickPocket} prowl={pc.SkillProwling} spotTrap={pc.SkillSpotTrap}"
        );
        Console.WriteLine(
            $"    gamble={pc.SkillGambling} haggle={pc.SkillHaggle} heal={pc.SkillHeal} persuade={pc.SkillPersuasion}"
        );
        Console.WriteLine();

        // ── Tech skills ───────────────────────────────────────────────────────
        Console.WriteLine("  TECH SKILLS");
        Console.WriteLine(
            $"    repair={pc.SkillRepair} firearms={pc.SkillFirearms} pickLocks={pc.SkillPickLocks} disarmTraps={pc.SkillDisarmTraps}"
        );
        Console.WriteLine();

        // ── Spell colleges ────────────────────────────────────────────────────
        var spellCols = new (string Name, int Rank)[]
        {
            ("conv", pc.SpellConveyance),
            ("div", pc.SpellDivination),
            ("air", pc.SpellAir),
            ("earth", pc.SpellEarth),
            ("fire", pc.SpellFire),
            ("water", pc.SpellWater),
            ("force", pc.SpellForce),
            ("mental", pc.SpellMental),
            ("meta", pc.SpellMeta),
            ("morph", pc.SpellMorph),
            ("nature", pc.SpellNature),
            ("necroBlk", pc.SpellNecroBlack),
            ("necroWht", pc.SpellNecroWhite),
            ("phantasm", pc.SpellPhantasm),
            ("summon", pc.SpellSummoning),
            ("temporal", pc.SpellTemporal),
            ("mastery", pc.SpellMastery),
        };
        var activeSpells = spellCols.Where(s => s.Rank > 0).ToList();
        Console.WriteLine("  SPELL COLLEGES");
        if (activeSpells.Count == 0)
            Console.WriteLine("    (none)");
        else
            Console.WriteLine("    " + ValueBufferText.JoinFormatted(activeSpells, "  ", new NamedRankFormatter()));
        Console.WriteLine();

        // ── Tech disciplines ──────────────────────────────────────────────────
        var techDiscs = new (string Name, int Rank)[]
        {
            ("herb", pc.TechHerbology),
            ("chem", pc.TechChemistry),
            ("elec", pc.TechElectric),
            ("exp", pc.TechExplosives),
            ("gun", pc.TechGun),
            ("mech", pc.TechMechanical),
            ("smith", pc.TechSmithy),
            ("therap", pc.TechTherapeutics),
        };
        var activeTech = techDiscs.Where(t => t.Rank > 0).ToList();
        Console.WriteLine("  TECH DISCIPLINES");
        if (activeTech.Count == 0)
            Console.WriteLine("    (none)");
        else
            Console.WriteLine("    " + ValueBufferText.JoinFormatted(activeTech, "  ", new NamedRankFormatter()));
        Console.WriteLine();

        // ── Quest log ─────────────────────────────────────────────────────────
        Console.WriteLine($"  QUEST LOG  ({pc.QuestCount} entries)");
        if (pc.QuestDataRaw is null)
            Console.WriteLine("    (absent)");
        else
            Console.WriteLine($"    raw={pc.QuestDataRaw.Length}B  bitset={pc.QuestBitsetRaw?.Length ?? 0}w");
        Console.WriteLine();

        // ── Reputation ────────────────────────────────────────────────────────
        Console.WriteLine(
            $"  REPUTATION  ({(pc.ReputationRaw is null ? "absent" : $"{pc.ReputationRaw.Length} factions")})"
        );
        if (pc.ReputationRaw is { } rep)
        {
            var nonZeroRep = rep.Select((v, i) => (i, v)).Where(p => p.v != 0).ToList();
            if (nonZeroRep.Count == 0)
                Console.WriteLine("    (all zeros)");
            else
                Console.WriteLine(
                    "    " + ValueBufferText.JoinFormatted(nonZeroRep, "  ", new IndexedValueFormatter())
                );
        }
        Console.WriteLine();

        // ── Blessings / Curses ────────────────────────────────────────────────
        Console.WriteLine($"  BLESSINGS  ({pc.BlessingProtoElementCount})");
        if (pc.BlessingRaw is { } bl)
            Console.WriteLine("    protoIDs: " + ValueBufferText.JoinInt32(bl, ", "));
        else
            Console.WriteLine("    (none)");

        Console.WriteLine($"  CURSES  ({pc.CurseProtoElementCount})");
        if (pc.CurseRaw is { } cu)
            Console.WriteLine("    protoIDs: " + ValueBufferText.JoinInt32(cu, ", "));
        else
            Console.WriteLine("    (none)");
        Console.WriteLine();

        // ── Schematics ────────────────────────────────────────────────────────
        Console.WriteLine($"  SCHEMATICS  ({pc.SchematicsElementCount})");
        if (pc.SchematicsRaw is { } sch)
            Console.WriteLine("    protoIDs: " + ValueBufferText.JoinInt32(sch, ", "));
        else
            Console.WriteLine("    (none)");
        Console.WriteLine();

        // ── Rumors ────────────────────────────────────────────────────────────
        Console.WriteLine($"  RUMORS  ({pc.RumorsCount})");
        if (pc.RumorsRaw is not null)
            Console.WriteLine($"    raw={pc.RumorsRaw.Length}B");
        else
            Console.WriteLine("    (absent)");

        return Task.CompletedTask;
    }

    private readonly struct NamedRankFormatter : IValueStringBuilderFormatter<(string Name, int Rank)>
    {
        public void Append(ref ValueStringBuilder builder, (string Name, int Rank) value)
        {
            builder.Append(value.Name);
            builder.Append('=');
            builder.Append(value.Rank);
        }
    }

    private readonly struct IndexedValueFormatter : IValueStringBuilderFormatter<(int i, int v)>
    {
        public void Append(ref ValueStringBuilder builder, (int i, int v) value)
        {
            builder.Append('[');
            builder.Append(value.i);
            builder.Append("]=");
            builder.Append(value.v);
        }
    }
}
