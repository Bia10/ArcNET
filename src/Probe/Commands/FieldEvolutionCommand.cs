using ArcNET.Editor;
using ArcNET.Formats;
using Probe;

namespace Probe.Commands;

internal sealed class FieldEvolutionCommand : IProbeCommand
{
    public Task RunAsync(string saveDir, string[] args)
    {
        var firstSlot = 13;
        var lastSlot = 177;
        if (args.Length >= 1)
            _ = int.TryParse(args[0], out firstSlot);
        if (args.Length >= 2)
            _ = int.TryParse(args[1], out lastSlot);
        if (firstSlot > lastSlot)
            (firstSlot, lastSlot) = (lastSlot, firstSlot);

        var questLookup = SarUtils.TryLoadQuestLookup(saveDir);

        Console.WriteLine($"\n=== Mode 13: Field Evolution - slots {firstSlot:D4}-{lastSlot:D4} ===");
        Console.WriteLine(
            questLookup is not null
                ? $"  Quest labels: {questLookup.Labels.Count} loaded from {questLookup.Source}"
                : "  Quest labels: unavailable (no usable local quest lookup was found in loose data or DAT archives)"
        );
        Console.WriteLine(
            "  Tracked: lv, XP, align, fate, magicPts, techPts, gold, quests, quest-state deltas, rumors, blessings, curses, schematics, hp_dmg, fat_dmg, bullets, powerCells, reputation, SpellTech ranks, base stats, basic skills"
        );
        Console.WriteLine(new string('-', 100));

        static Dictionary<int, int> BuildRepMap(CharacterMdyRecord character)
        {
            var result = new Dictionary<int, int>();
            var reputation = character.ReputationRaw;
            if (reputation is null)
                return result;

            var slots = character.ReputationFactionSlots;
            if (slots is not null && slots.Length == reputation.Length)
            {
                for (var index = 0; index < reputation.Length; index++)
                    result[slots[index]] = reputation[index];
                return result;
            }

            for (var index = 0; index < reputation.Length; index++)
                result[index] = reputation[index];
            return result;
        }

        static List<string> RepChanges(CharacterMdyRecord current, CharacterMdyRecord previous)
        {
            var result = new List<string>();
            var repNow = BuildRepMap(current);
            var repPrev = BuildRepMap(previous);
            if (repNow.Count == 0 && repPrev.Count == 0)
                return result;
            if (repNow.Count > 0 && repPrev.Count == 0)
            {
                result.Add("rep:NEW");
                return result;
            }
            if (repNow.Count == 0 && repPrev.Count > 0)
            {
                result.Add("rep:LOST");
                return result;
            }

            foreach (var slot in repNow.Keys.Union(repPrev.Keys).OrderBy(x => x))
            {
                repNow.TryGetValue(slot, out var nowVal);
                repPrev.TryGetValue(slot, out var prevVal);
                if (nowVal != prevVal)
                    result.Add($"rep[{slot}]:{prevVal}->{nowVal}");
            }

            return result;
        }

        List<string> QuestChanges(CharacterMdyRecord current, CharacterMdyRecord previous)
        {
            var result = new List<string>();
            var nowEntries = current.QuestEntries;
            var prevEntries = previous.QuestEntries;
            if (nowEntries is null && prevEntries is null)
                return result;

            var nowMap = (nowEntries ?? []).ToDictionary(entry => entry.ProtoId);
            var prevMap = (prevEntries ?? []).ToDictionary(entry => entry.ProtoId);

            var added = nowMap.Keys.Except(prevMap.Keys).OrderBy(x => x).ToArray();
            var removed = prevMap.Keys.Except(nowMap.Keys).OrderBy(x => x).ToArray();
            if (added.Length > 0)
                result.Add(
                    $"quest+[{string.Join(", ", added.Select(id => SarUtils.FormatQuestRef(id, questLookup)))}]"
                );
            if (removed.Length > 0)
                result.Add(
                    $"quest-[{string.Join(", ", removed.Select(id => SarUtils.FormatQuestRef(id, questLookup)))}]"
                );

            foreach (var protoId in nowMap.Keys.Intersect(prevMap.Keys).OrderBy(x => x))
            {
                var now = nowMap[protoId];
                var old = prevMap[protoId];
                if (now.State != old.State)
                {
                    result.Add(
                        $"{SarUtils.FormatQuestRef(protoId, questLookup)}:{SarUtils.FormatQuestState(old.State)}->{SarUtils.FormatQuestState(now.State)}"
                    );
                }
            }

            return result;
        }

        CharacterMdyRecord? previousCharacter = null;
        string[] spellNames =
        [
            "Conv",
            "Div",
            "Air",
            "Earth",
            "Fire",
            "Water",
            "Force",
            "Mental",
            "Meta",
            "Morph",
            "Nature",
            "NecroBlk",
            "NecroWht",
            "Phantasm",
            "Summon",
            "Temporal",
            "* MasteryCol",
            "Herb",
            "Chem",
            "Elec",
            "Explos",
            "Gun",
            "Mech",
            "Smithy",
            "Therap",
        ];

        for (var slot = firstSlot; slot <= lastSlot; slot++)
        {
            var stem = $"Slot{slot:D4}";
            var gsiFiles = Directory.GetFiles(saveDir, stem + "*.gsi");
            var tfaiPath = Path.Combine(saveDir, stem + ".tfai");
            var tfafPath = Path.Combine(saveDir, stem + ".tfaf");
            if (gsiFiles.Length == 0 || !File.Exists(tfaiPath) || !File.Exists(tfafPath))
                continue;

            LoadedSave save;
            try
            {
                save = SaveGameLoader.Load(gsiFiles[0], tfaiPath, tfafPath);
            }
            catch
            {
                continue;
            }

            var character = SarUtils.FindPlayerRecord(save);
            if (character is null)
                continue;

            if (previousCharacter is null)
            {
                var level0 = character.Stats.Length > 17 ? character.Stats[17] : -1;
                var xp0 = character.Stats.Length > 18 ? character.Stats[18] : 0;
                var repStr0 =
                    character.ReputationRaw is null || character.ReputationRaw.All(v => v == 0)
                        ? "null"
                        : $"{character.ReputationRaw.Length}entries";
                Console.WriteLine(
                    $"  [{stem}] lv={level0} XP={xp0} gold={character.Gold} quests={character.QuestCount} rumors={character.RumorsCount} blessings={character.BlessingProtoElementCount} curses={character.CurseProtoElementCount} schematics={character.SchematicsElementCount} hp_dmg={character.HpDamage} fat_dmg={character.FatigueDamage} bullets={character.Bullets} powerCells={character.PowerCells} rep={repStr0}  (baseline)"
                );
                previousCharacter = character;
                continue;
            }

            var level = character.Stats.Length > 17 ? character.Stats[17] : -1;
            var xp = character.Stats.Length > 18 ? character.Stats[18] : 0;
            var alignment = character.Stats.Length > 19 ? character.Stats[19] : 0;
            var fate = character.Stats.Length > 20 ? character.Stats[20] : 0;
            var magicPoints = character.Stats.Length > 22 ? character.Stats[22] : 0;
            var techPoints = character.Stats.Length > 23 ? character.Stats[23] : 0;

            var prevLevel = previousCharacter.Stats.Length > 17 ? previousCharacter.Stats[17] : -1;
            var prevXp = previousCharacter.Stats.Length > 18 ? previousCharacter.Stats[18] : 0;
            var prevAlignment = previousCharacter.Stats.Length > 19 ? previousCharacter.Stats[19] : 0;
            var prevFate = previousCharacter.Stats.Length > 20 ? previousCharacter.Stats[20] : 0;
            var prevMagicPoints = previousCharacter.Stats.Length > 22 ? previousCharacter.Stats[22] : 0;
            var prevTechPoints = previousCharacter.Stats.Length > 23 ? previousCharacter.Stats[23] : 0;

            var spellChanges = new List<string>();
            if (
                character.SpellTech is { Length: > 0 } spellNew
                && previousCharacter.SpellTech is { Length: > 0 } spellOld
            )
            {
                var spellLength = Math.Min(spellNew.Length, spellOld.Length);
                for (var index = 0; index < spellLength; index++)
                {
                    if (spellNew[index] != spellOld[index])
                    {
                        spellChanges.Add(
                            $"{(index < spellNames.Length ? spellNames[index] : $"ST[{index}]")}:{spellOld[index]}->{spellNew[index]}"
                        );
                    }
                }
            }

            // The first 16 entries of the 28-stat array: primary attributes plus derived critter stats.
            var baseStatChanges = new List<string>();
            if (character.Stats is { Length: > 8 } sNow && previousCharacter.Stats is { Length: > 8 } sPrev)
            {
                int statLen = Math.Min(Math.Min(sNow.Length, sPrev.Length), 16);
                for (var i = 0; i < statLen; i++)
                {
                    if (sNow[i] != sPrev[i])
                        baseStatChanges.Add($"{SarUtils.GetElementLabel("4:28:2", i)}:{sPrev[i]}->{sNow[i]}");
                }
            }

            // Basic skills (indices 0–11: BOW,DODGE,MELEE,THROW,BKSTB,PPKT,PROWL,STRAP,GAMBL,HAGGL,HEAL,PERS)
            var skillChanges = new List<string>();
            if (
                character.BasicSkills is { Length: > 0 } bsNow
                && previousCharacter.BasicSkills is { Length: > 0 } bsPrev
            )
            {
                int skillLen = Math.Min(bsNow.Length, bsPrev.Length);
                for (var i = 0; i < skillLen; i++)
                {
                    if (bsNow[i] != bsPrev[i])
                        skillChanges.Add($"{SarUtils.GetElementLabel("4:12:2", i)}:{bsPrev[i]}->{bsNow[i]}");
                }
            }

            var questChanges = QuestChanges(character, previousCharacter);
            var levelUp = level != prevLevel;
            var repChanges = RepChanges(character, previousCharacter);
            var anyDiff =
                levelUp
                || xp != prevXp
                || alignment != prevAlignment
                || fate != prevFate
                || magicPoints != prevMagicPoints
                || techPoints != prevTechPoints
                || character.Gold != previousCharacter.Gold
                || character.QuestCount != previousCharacter.QuestCount
                || questChanges.Count > 0
                || character.RumorsCount != previousCharacter.RumorsCount
                || character.BlessingProtoElementCount != previousCharacter.BlessingProtoElementCount
                || character.CurseProtoElementCount != previousCharacter.CurseProtoElementCount
                || character.SchematicsElementCount != previousCharacter.SchematicsElementCount
                || character.HpDamage != previousCharacter.HpDamage
                || character.FatigueDamage != previousCharacter.FatigueDamage
                || character.Bullets != previousCharacter.Bullets
                || character.PowerCells != previousCharacter.PowerCells
                || repChanges.Count > 0
                || spellChanges.Count > 0
                || baseStatChanges.Count > 0
                || skillChanges.Count > 0;

            if (!anyDiff)
            {
                previousCharacter = character;
                continue;
            }

            var levelUpMarker = levelUp ? " *** LEVEL UP ***" : string.Empty;
            var changes = new List<string>();
            if (level != prevLevel)
                changes.Add($"lv:{prevLevel}->{level}");
            if (xp != prevXp)
                changes.Add($"XP:+{xp - prevXp}");
            if (alignment != prevAlignment)
                changes.Add($"align:{prevAlignment}->{alignment}");
            if (fate != prevFate)
                changes.Add($"fate:{prevFate}->{fate}");
            if (magicPoints != prevMagicPoints)
                changes.Add($"magic:{prevMagicPoints}->{magicPoints}");
            if (techPoints != prevTechPoints)
                changes.Add($"tech:{prevTechPoints}->{techPoints}");
            if (character.Gold != previousCharacter.Gold)
                changes.Add($"gold:{previousCharacter.Gold}->{character.Gold}");
            if (character.QuestCount != previousCharacter.QuestCount)
                changes.Add($"quests:{previousCharacter.QuestCount}->{character.QuestCount}");
            changes.AddRange(questChanges);
            if (character.RumorsCount != previousCharacter.RumorsCount)
                changes.Add($"rumors:{previousCharacter.RumorsCount}->{character.RumorsCount}");
            if (character.BlessingProtoElementCount != previousCharacter.BlessingProtoElementCount)
                changes.Add(
                    $"bless:{previousCharacter.BlessingProtoElementCount}->{character.BlessingProtoElementCount}"
                );
            if (character.CurseProtoElementCount != previousCharacter.CurseProtoElementCount)
                changes.Add($"curse:{previousCharacter.CurseProtoElementCount}->{character.CurseProtoElementCount}");
            if (character.SchematicsElementCount != previousCharacter.SchematicsElementCount)
            {
                changes.Add(
                    $"schematics:{previousCharacter.SchematicsElementCount}->{character.SchematicsElementCount}"
                );
                var newSchematics = (character.SchematicsRaw ?? [])
                    .Except(previousCharacter.SchematicsRaw ?? [])
                    .OrderBy(x => x)
                    .ToArray();
                if (newSchematics.Length > 0)
                    changes.Add($"schematic+[{string.Join(",", newSchematics)}]");
            }
            if (character.HpDamage != previousCharacter.HpDamage)
                changes.Add($"hp_dmg:{previousCharacter.HpDamage}->{character.HpDamage}");
            if (character.FatigueDamage != previousCharacter.FatigueDamage)
                changes.Add($"fat_dmg:{previousCharacter.FatigueDamage}->{character.FatigueDamage}");
            if (character.Bullets != previousCharacter.Bullets)
                changes.Add($"bullets:{previousCharacter.Bullets}->{character.Bullets}");
            if (character.PowerCells != previousCharacter.PowerCells)
                changes.Add($"powerCells:{previousCharacter.PowerCells}->{character.PowerCells}");
            changes.AddRange(repChanges);
            changes.AddRange(spellChanges);

            if (baseStatChanges.Count > 0)
                changes.Add($"baseStats[{string.Join(" ", baseStatChanges)}]");
            if (skillChanges.Count > 0)
                changes.Add($"skills[{string.Join(" ", skillChanges)}]");

            if (
                character.SpellTech is { Length: > 0 } spellNow
                && previousCharacter.SpellTech is { Length: > 0 } spellPrev
            )
            {
                var spellLength = Math.Min(spellNow.Length, spellPrev.Length);
                for (var index = 0; index < spellLength; index++)
                {
                    if (spellPrev[index] <= 0 && spellNow[index] > 0)
                    {
                        changes.Add(
                            $"UNLOCK:{(index < spellNames.Length ? spellNames[index] : $"ST[{index}]")}(rank={spellNow[index]})"
                        );
                    }
                }
            }

            Console.WriteLine($"  [{stem}] lv={level} XP={xp}{levelUpMarker}  {string.Join("  ", changes)}");
            previousCharacter = character;
        }

        return Task.CompletedTask;
    }
}
