using ArcNET.DataTypes.GameObjects.Classes;
using ArcNET.DataTypes.GameObjects.Flags;
using ArcNET.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArcNET.DataTypes
{
    public class MonsterTextReader
    {
        private readonly StreamReader _reader;
        private readonly List<Monster> _monsters = new();

        public MonsterTextReader(StreamReader reader)
        {
            _reader = reader;
        }

        public string GetEntriesAsJson()
        {
            return JsonConvert.SerializeObject(_monsters, Formatting.Indented);
        }

        private static Tuple<Entity.ResistanceType, int> GetResistTuple(string paramValue)
        {

            var tuple = (from resistanceType in (Entity.ResistanceType[])Enum.GetValues(typeof(Entity.ResistanceType))
                where nameof(resistanceType).Equals(paramValue)
                select new Tuple<Entity.ResistanceType, int>(resistanceType, int.Parse(paramValue))).FirstOrDefault();

            return tuple;
        }

        private static Tuple<Entity.DamageType, int, int> GetDmgTuple(string paramName, string paramValue)
        {
            var trimmedDmg = paramValue.TrimStart();
            var dmgRange = trimmedDmg.Split(" ", 2);
            var min = int.Parse(dmgRange[0]);
            var max = int.Parse(dmgRange[1]);

            return paramName switch
            {
                "Normal Damage" => new Tuple<Entity.DamageType, int, int>(Entity.DamageType.Normal, min, max),
                "Fatigue Damage" => new Tuple<Entity.DamageType, int, int>(Entity.DamageType.Fatigue, min, max),
                "Poison Damage" => new Tuple<Entity.DamageType, int, int>(Entity.DamageType.Poison, min, max),
                "Electrical Damage" => new Tuple<Entity.DamageType, int, int>(Entity.DamageType.Electrical, min, max),
                "Fire Damage" => new Tuple<Entity.DamageType, int, int>(Entity.DamageType.Fire, min, max),
                _ => null
            };
        }

        private static Monster GetMobFromText(IEnumerable<string> mobText)
        {
            var monster = new Monster
            {
                ObjectFlags = new List<ObjFFlags>(),
                CritterFlags = new List<ObjFCritterFlags>(),
                CritterFlags2 = new List<ObjFCritterFlags2>(),
                NpcFlags = new List<ObjFNpcFlags>(),
                BlitFlags = new List<ObjFBlitFlag>(),
                SpellFlags = new List<ObjFSpellFlags>(),
                BasicStats = new List<Tuple<Entity.BasicStatType, int>>(),
                Spells = new List<string>(),
                Scripts = new List<Tuple<int, int, int, int, int, int>>(),
            };

            foreach (var curLine in mobText)
            {
                if (!string.IsNullOrWhiteSpace(curLine))
                {
                    var lines = curLine.Split(":", 2);
                    var paramName = lines[0];
                    var paramValue = lines[1];

                    switch (paramName)
                    {
                        case "Description" when paramValue.Contains(@"//"):
                            {
                                var idAndName = paramValue.Split(@"//", 2);
                                var monsterId = idAndName[0];
                                var monsterName = idAndName[1];

                                monster.Description = new Tuple<int, string>(int.Parse(monsterId), monsterName);
                                break;
                            }
                        case "Description":
                            //AnsiConsoleExtensions.Log($"paramValue:|{paramValue}|", "warn");
                            break;
                        case "Internal Name" or "internal name":
                            monster.InternalName = int.Parse(paramValue);
                            break;
                        case "Level":
                            monster.Level = int.Parse(paramValue);
                            break;
                        case "Art Number and Palette":
                            var trimmed = paramValue.TrimStart();
                            var artNumberAndPalette = trimmed.Split(" ", 2);
                            var artNumber = artNumberAndPalette[0];
                            var paletteNumber = artNumberAndPalette[1];

                            monster.ArtNumberAndPalette = new Tuple<int, int>(int.Parse(artNumber), int.Parse(paletteNumber));
                            break;
                        case "Scale":
                            monster.Scale = int.Parse(paramValue);
                            break;
                        case "Alignment":
                            monster.Alignment = int.Parse(paramValue);
                            break;
                        case "Object Flag":
                            var trimmedObjFlag = paramValue.TrimStart();

                            if (!((IList)Enum.GetNames(typeof(ObjFFlags))).Contains(trimmedObjFlag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFFlags param:|{trimmedObjFlag}|", "warn");

                            foreach (var objFlag in (ObjFFlags[]) Enum.GetValues(typeof(ObjFFlags)))
                            {
                                if (nameof(objFlag).Equals(trimmedObjFlag))
                                    monster.ObjectFlags.Add(objFlag);
                            }
                            break;
                        case "Critter Flag":
                            var trimmedCritterFlag = paramValue.TrimStart();

                            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags))).Contains(trimmedCritterFlag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFCritterFlags param:|{trimmedCritterFlag}|", "warn");

                            foreach (var critterFlag in (ObjFCritterFlags[]) Enum.GetValues(typeof(ObjFCritterFlags)))
                            {
                                if (nameof(critterFlag).Equals(trimmedCritterFlag))
                                    monster.CritterFlags.Add(critterFlag);
                            }
                            break;
                        case "Critter2 Flag":
                            var trimmedCritter2Flag = paramValue.TrimStart();

                            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags2))).Contains(trimmedCritter2Flag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFCritterFlags2 param:|{trimmedCritter2Flag}|", "warn");

                            foreach (var critterFlag2 in (ObjFCritterFlags2[])Enum.GetValues(typeof(ObjFCritterFlags2)))
                            {
                                if (nameof(critterFlag2).Equals(trimmedCritter2Flag))
                                    monster.CritterFlags2.Add(critterFlag2);
                            }
                            break;
                        case "NPC Flag":
                            var trimmedNPCFlag = paramValue.TrimStart();

                            if (!((IList)Enum.GetNames(typeof(ObjFNpcFlags))).Contains(trimmedNPCFlag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFNpcFlags param:|{trimmedNPCFlag}|", "warn");

                            foreach (var npcFlag in (ObjFNpcFlags[])Enum.GetValues(typeof(ObjFNpcFlags)))
                            {
                                if (nameof(npcFlag).Equals(trimmedNPCFlag))
                                    monster.NpcFlags.Add(npcFlag);
                            }
                            break;
                        case "Blit Flag":
                            var trimmedBlitFlag = paramValue.TrimStart();

                            if (!((IList)Enum.GetNames(typeof(ObjFBlitFlag))).Contains(trimmedBlitFlag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFBlitFlag param:|{trimmedBlitFlag}|", "warn");

                            foreach (var blitFlag in (ObjFBlitFlag[])Enum.GetValues(typeof(ObjFBlitFlag)))
                            {
                                if (nameof(blitFlag).Equals(trimmedBlitFlag))
                                    monster.BlitFlags.Add(blitFlag);
                            }
                            break;
                        case "Spell Flag":
                            var trimmedSpellFlag = paramValue.TrimStart();
                            if (!((IList) Enum.GetNames(typeof(ObjFSpellFlags))).Contains(trimmedSpellFlag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFSpellFlags param:|{trimmedSpellFlag}|", "warn");

                            foreach (var spellFlag in (ObjFSpellFlags[])Enum.GetValues(typeof(ObjFSpellFlags)))
                            {
                                if (nameof(spellFlag).Equals(trimmedSpellFlag))
                                    monster.SpellFlags.Add(spellFlag);
                            }
                            break;
                        case "Hit Chart":
                            monster.HitChart = int.Parse(paramValue);
                            break;
                        case "Basic Stat" or "basic stat":
                            var trimmedStats = paramValue.TrimStart();
                            var statAndValue = trimmedStats.Split(" ", 2);
                            var statType = statAndValue[0];
                            var statValue = statAndValue[1];

                            if (!((IList)Enum.GetNames(typeof(Entity.BasicStatType))).Contains(paramValue)) 
                                AnsiConsoleExtensions.Log($"unrecognized Entity.BasicStatType param:|{paramValue}|", "warn");

                            foreach (var basicStatType in (Entity.BasicStatType[]) Enum.GetValues(typeof(Entity.BasicStatType)))
                            {
                                if (nameof(basicStatType).Equals(statType))
                                    monster.BasicStats.Add(new Tuple<Entity.BasicStatType, int>(basicStatType, int.Parse(statValue)));
                            }

                            break;
                        case "Spell" or "spell":
                            monster.Spells.Add(paramValue);
                            break;
                        case "Script":
                            var trimmedScript = paramValue.TrimStart();
                            var scriptParams = trimmedScript.Split(" ", 5);
                            var paramValues = scriptParams.Select(int.Parse).ToList();

                            monster.Scripts.Add(new Tuple<int, int, int, int, int, int>(paramValues[0], paramValues[1], paramValues[2], paramValues[3], paramValues[4], paramValues[5]));
                            break;
                        case "Faction":
                            monster.Faction = int.Parse(paramValue);
                            break;
                        case "AI Packet":
                            monster.AIPacket = int.Parse(paramValue);
                            break;
                        case "Material":
                            monster.Material = int.Parse(paramValue);
                            break;
                        case "Hit Points":
                            monster.HitPoints = int.Parse(paramValue);
                            break;
                        case "Fatigue":
                            monster.Fatigue = int.Parse(paramValue);
                            break;
                        case "Damage Resistance" or "damage resistance":
                            monster.Resistances.Add(GetResistTuple(paramValue));
                            break;
                        case "Fire Resistance":
                            monster.Resistances.Add(GetResistTuple(paramValue)); 
                            break;
                        case "Electrical Resistance":
                            monster.Resistances.Add(GetResistTuple(paramValue));
                            break;
                        case "Poison Resistance":
                            monster.Resistances.Add(GetResistTuple(paramValue));
                            break;
                        case "Magic Resistance":
                            monster.Resistances.Add(GetResistTuple(paramValue));
                            break;
                        case "Normal Damage":
                            monster.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Fatigue Damage":
                            monster.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Poison Damage":
                            monster.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Electrical Damage":
                            monster.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;
                        case "Fire Damage":
                            monster.Damages.Add(GetDmgTuple(paramName, paramValue));
                            break;

                        case "Sound Bank" or "sound bank":
                            monster.SoundBank = int.Parse(paramValue);
                            break;
                        case "Category":
                            monster.Category = int.Parse(paramValue);
                            break;
                        case "Auto Level Scheme":
                            monster.AutoLevelScheme = int.Parse(paramValue);
                            break;
                        case "Inventory Source":
                            monster.InventorySource = int.Parse(paramValue);
                            break;

                        default:
                            AnsiConsoleExtensions.Log($"unrecognized entity param:|{paramName}|", "warn");
                            break;
                    }
                }
            }
            return monster;
        }

        public List<Monster> Parse()
        {
            var mobStringList = new List<string>();

            while (true)
            {
                var curLine = _reader.ReadLine();

                if (!string.IsNullOrWhiteSpace(curLine))
                {
                    var lines = curLine.Split(":", 2);
                    var paramName = lines[0];
                    var paramValue = lines[1];

                    switch (paramName)
                    {
                        case "Description" when paramValue.Contains(@"\\"):
                            mobStringList.Add(curLine);
                            break;
                        case "Description":
                            mobStringList.Add(curLine);
                            break;
                        case "Internal Name" or "internal name":
                            mobStringList.Add(curLine);
                            break;
                        case "Level":
                            mobStringList.Add(curLine);
                            break;
                        case "Art Number and Palette":
                            mobStringList.Add(curLine);
                            break;
                        case "Scale":
                            mobStringList.Add(curLine);
                            break;
                        case "Alignment":
                            mobStringList.Add(curLine);
                            break;
                        case "Object Flag":
                            mobStringList.Add(curLine);
                            break;
                        case "Critter Flag":
                            mobStringList.Add(curLine);
                            break;
                        case "Critter2 Flag":
                            mobStringList.Add(curLine);
                            break;
                        case "NPC Flag":
                            mobStringList.Add(curLine);
                            break;
                        case "Blit Flag":
                            mobStringList.Add(curLine);
                            break;
                        case "Spell Flag":
                            mobStringList.Add(curLine);
                            break;
                        case "Hit Chart":
                            mobStringList.Add(curLine);
                            break;
                        case "Basic Stat" or "basic stat":
                            mobStringList.Add(curLine);
                            break;
                        case "Spell" or "spell":
                            mobStringList.Add(curLine);
                            break;
                        case "Script":
                            mobStringList.Add(curLine);
                            break;
                        case "Faction":
                            mobStringList.Add(curLine);
                            break;
                        case "AI Packet":
                            mobStringList.Add(curLine);
                            break;
                        case "Material":
                            mobStringList.Add(curLine);
                            break;
                        case "Hit Points":
                            mobStringList.Add(curLine);
                            break;
                        case "Fatigue":
                            mobStringList.Add(curLine);
                            break;
                        case "Damage Resistance" or "damage resistance":
                            mobStringList.Add(curLine);
                            break;
                        case "Fire Resistance":
                            mobStringList.Add(curLine);
                            break;
                        case "Electrical Resistance":
                            mobStringList.Add(curLine);
                            break;
                        case "Poison Resistance":
                            mobStringList.Add(curLine);
                            break;
                        case "Magic Resistance":
                            mobStringList.Add(curLine);
                            break;
                        case "Normal Damage":
                            mobStringList.Add(curLine);
                            break;
                        case "Fatigue Damage":
                            mobStringList.Add(curLine);
                            break;
                        case "Poison Damage":
                            mobStringList.Add(curLine);
                            break;
                        case "Electrical Damage":
                            mobStringList.Add(curLine);
                            break;
                        case "Fire Damage":
                            mobStringList.Add(curLine);
                            break;
                        case "Sound Bank" or "sound bank":
                            mobStringList.Add(curLine);
                            break;
                        case "Category":
                            mobStringList.Add(curLine);
                            break;
                        case "Auto Level Scheme":
                            mobStringList.Add(curLine);
                            break;
                        case "Inventory Source":
                            mobStringList.Add(curLine);
                            break;

                        default:
                            AnsiConsoleExtensions.Log($"unrecognized entity param:|{paramName}|", "warn");
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(curLine))
                {
                    //assuming end of one mob block...
                    var currentMob = GetMobFromText(mobStringList);
                    _monsters.Add(currentMob);

                    mobStringList.Clear();
                }

                if (curLine == null) break;
            }
            return _monsters;
        }
    }
}