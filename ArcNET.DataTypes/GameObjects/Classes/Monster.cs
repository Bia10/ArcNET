using ArcNET.DataTypes.GameObjects.Flags;
using ArcNET.Utilities;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class Monster : Entity
    {
        private static List<int> GetWhitespaceIndexes(string input)
        {
            var whiteSpaceIndexes = new List<int>();
            var stringAsChars = input.ToCharArray();

            foreach (var (item, index) in stringAsChars.WithIndex())
            {
                if (char.IsWhiteSpace(item))
                    whiteSpaceIndexes.Add(index);
            }

            return whiteSpaceIndexes;
        }

        private static Tuple<ResistanceType, int> GetResistTuple(string paramName, string paramValue)
        {
            var trimmedResist = paramName.TrimStart();
            var resist = trimmedResist.Split(" ", 2);
            var resistTypeStr = resist[0];
            var resistanceTypes = (ResistanceType[])Enum.GetValues(typeof(ResistanceType));

            return (from resistType in resistanceTypes
                    let resistTypeName = Enum.GetName(typeof(ResistanceType), resistType)
                    where resistTypeName.Equals(resistTypeStr)
                    select new Tuple<ResistanceType, int>(resistType, int.Parse(paramValue)))
                    .FirstOrDefault();
        }

        private static Tuple<DamageType, int, int> GetDmgTuple(string paramName, string paramValue)
        {
            var trimmedDmg = paramValue.TrimStart();
            var dmgRange = trimmedDmg.Split(" ", 2);
            var min = int.Parse(dmgRange[0]);
            var max = int.Parse(dmgRange[1]);

            return paramName switch
            {
                "Normal Damage" => new Tuple<DamageType, int, int>(DamageType.Normal, min, max),
                "Fatigue Damage" => new Tuple<DamageType, int, int>(DamageType.Fatigue, min, max),
                "Poison Damage" => new Tuple<DamageType, int, int>(DamageType.Poison, min, max),
                "Electrical Damage" => new Tuple<DamageType, int, int>(DamageType.Electrical, min, max),
                "Fire Damage" => new Tuple<DamageType, int, int>(DamageType.Fire, min, max),
                _ => null
            };
        }

        public static Monster GetFromText(IEnumerable<string> mobText)
        {
            var monster = new Monster
            {
                ObjectFlags = new List<ObjFFlags>(),
                CritterFlags = new List<ObjFCritterFlags>(),
                CritterFlags2 = new List<ObjFCritterFlags2>(),
                NpcFlags = new List<ObjFNpcFlags>(),
                BlitFlags = new List<ObjFBlitFlag>(),
                SpellFlags = new List<ObjFSpellFlags>(),
                BasicStats = new List<Tuple<BasicStatType, int>>(),
                Spells = new List<string>(),
                Scripts = new List<Tuple<int, int, int, int, int, int>>(),
                Resistances = new List<Tuple<ResistanceType, int>>(),
                Damages = new List<Tuple<DamageType, int, int>>()
            };

            foreach (var curLine in mobText)
            {
                if (string.IsNullOrWhiteSpace(curLine)) continue;

                var lines = curLine.Split(":", 2);
                var paramName = lines[0];
                var paramValue = lines[1];

                try
                {
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

                            foreach (var objFlag in (ObjFFlags[])Enum.GetValues(typeof(ObjFFlags)))
                            {
                                if (nameof(objFlag).Equals(trimmedObjFlag))
                                    monster.ObjectFlags.Add(objFlag);
                            }
                            break;
                        case "Critter Flag":
                            var trimmedCritterFlag = paramValue.TrimStart();

                            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags))).Contains(trimmedCritterFlag))
                                AnsiConsoleExtensions.Log($"unrecognized ObjFCritterFlags param:|{trimmedCritterFlag}|", "warn");

                            foreach (var critterFlag in (ObjFCritterFlags[])Enum.GetValues(typeof(ObjFCritterFlags)))
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
                            if (!((IList)Enum.GetNames(typeof(ObjFSpellFlags))).Contains(trimmedSpellFlag))
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

                            //skip junk
                            if (trimmedStats.Equals("Strength 13") || trimmedStats.Equals("Dexterity 15") ||
                                trimmedStats.Equals("Dexterity 14")) continue;

                            var separator = "\t\t";
                            if (trimmedStats.Contains("Gender") || trimmedStats.Contains("Race"))
                            {
                                separator = " ";
                            }
                            if (trimmedStats.Contains("tech points") || trimmedStats.Contains("magick points"))
                            {
                                var whitespaceIndexes = GetWhitespaceIndexes(trimmedStats);
                                if (whitespaceIndexes.Count >= 2)
                                {
                                    trimmedStats = trimmedStats.Remove(whitespaceIndexes.First(), 1);
                                    AnsiConsoleExtensions.Log($"trimmedStats after removal:|{trimmedStats}|", "warn");
                                }
                                separator = " ";
                            }
                            if (trimmedStats.Contains("Constitution") || trimmedStats.Contains("Intelligence"))
                                separator = "\t";

                            var statAndValue = trimmedStats.Split(separator, 2);
                            foreach (var paramVal in statAndValue)
                                AnsiConsoleExtensions.Log($"statAndValue:|{paramVal}|", "warn");

                            var statType = statAndValue[0].Trim();
                            var statValue = statAndValue[1];

                            //if (!((IList)Enum.GetNames(typeof(BasicStatType))).Contains(statType))
                                //AnsiConsoleExtensions.Log($"unrecognized Entity.BasicStatType param:|{statType}|", "warn");

                            //TODO: magic points/tech points
                            foreach (var basicStatType in (BasicStatType[])Enum.GetValues(typeof(BasicStatType)))
                            {
                                if (nameof(basicStatType).Equals(statType))
                                    monster.BasicStats.Add(new Tuple<BasicStatType, int>(basicStatType, int.Parse(statValue)));
                            }

                            break;
                        case "Spell" or "spell":
                            monster.Spells.Add(paramValue);
                            break;
                        case "Script":
                            var trimmedScript = paramValue.TrimStart();
                            var scriptParams = trimmedScript.Split(" ", 6);

                            //foreach (var paramVal in scriptParams)
                               // AnsiConsoleExtensions.Log($"script param value:|{paramVal}|", "warn");

                            var paramValues = scriptParams.Select(int.Parse).ToList();

                            monster.Scripts.Add(new Tuple<int, int, int, int, int, int>(paramValues[0], paramValues[1], paramValues[2], paramValues[3], paramValues[4], paramValues[5]));
                            break;
                        case "Faction":
                            monster.Faction = int.Parse(paramValue);
                            break;
                        case "AI Packet":
                            if (paramValue.Contains("//"))
                                paramValue = paramValue.Split("//")[0].Trim();

                            monster.AIPacket = int.Parse(paramValue);
                            break;
                        case "Material":
                            if (paramValue.Contains("//"))
                                paramValue = paramValue.Split("//")[0].Trim();

                            monster.Material = int.Parse(paramValue);
                            break;
                        case "Hit Points":
                            monster.HitPoints = int.Parse(paramValue);
                            break;
                        case "Fatigue":
                            monster.Fatigue = int.Parse(paramValue);
                            break;
                        case "Damage Resistance" or "damage resistance":
                            monster.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Fire Resistance":
                            monster.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Electrical Resistance":
                            monster.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Poison Resistance":
                            monster.Resistances.Add(GetResistTuple(paramName, paramValue));
                            break;
                        case "Magic Resistance":
                            monster.Resistances.Add(GetResistTuple(paramName, paramValue));
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
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    throw;
                }
            }
            return monster;
        }
    }
}