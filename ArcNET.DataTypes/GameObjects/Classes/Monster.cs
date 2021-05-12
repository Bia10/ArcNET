using ArcNET.DataTypes.GameObjects.Flags;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utils.Console;
using Utils.Enumeration;

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

        private static ObjFFlags ParseObjFFlags(string paramValue)
        {
            var flag = (ObjFFlags) 0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFFlags param:|{trimmedFlag}|", "warn");

            foreach (var objFlag in (ObjFFlags[])Enum.GetValues(typeof(ObjFFlags)))
            {
                if (!Enum.GetName(typeof(ObjFFlags), objFlag).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFFlags param:|{trimmedFlag}|", "success");
                flag = objFlag;
            }
            return flag;
        }

        private static ObjFCritterFlags ParseObjFCritterFlags(string paramValue)
        {
            var flag = (ObjFCritterFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFCritterFlags param:|{trimmedFlag}|", "warn");

            foreach (var critterFlag in (ObjFCritterFlags[])Enum.GetValues(typeof(ObjFCritterFlags)))
            {
                if (!Enum.GetName(typeof(ObjFCritterFlags), critterFlag).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFCritterFlags param:|{trimmedFlag}|", "success");
                flag = critterFlag;
            }
            return flag;
        }

        private static ObjFCritterFlags2 ParseObjFCritterFlags2(string paramValue)
        {
            var flag = (ObjFCritterFlags2)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFCritterFlags2))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFCritterFlags2 param:|{trimmedFlag}|", "warn");

            foreach (var critterFlag2 in (ObjFCritterFlags2[])Enum.GetValues(typeof(ObjFCritterFlags2)))
            {
                if (!Enum.GetName(typeof(ObjFCritterFlags2), critterFlag2).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFCritterFlags2 param:|{trimmedFlag}|", "success");
                flag = critterFlag2;
            }
            return flag;
        }

        private static ObjFNpcFlags ParseObjFNpcFlags(string paramValue)
        {
            var flag = (ObjFNpcFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFNpcFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFNpcFlags param:|{trimmedFlag}|", "warn");

            foreach (var npcFlag in (ObjFNpcFlags[])Enum.GetValues(typeof(ObjFNpcFlags)))
            {
                if (!Enum.GetName(typeof(ObjFNpcFlags), npcFlag).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFNpcFlags param:|{trimmedFlag}|", "success");
                flag = npcFlag;
            }
            return flag;
        }

        private static ObjFBlitFlag ParseObjFBlitFlag(string paramValue)
        {
            var flag = (ObjFBlitFlag)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFBlitFlag))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFBlitFlag param:|{trimmedFlag}|", "warn");

            foreach (var blitFlags in (ObjFBlitFlag[])Enum.GetValues(typeof(ObjFBlitFlag)))
            {
                if (!Enum.GetName(typeof(ObjFBlitFlag), blitFlags).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFBlitFlag param:|{trimmedFlag}|", "success");
                flag = blitFlags;
            }
            return flag;
        }

        private static ObjFSpellFlags ParseObjFSpellFlags(string paramValue)
        {
            var flag = (ObjFSpellFlags)0;
            var trimmedFlag = paramValue.TrimStart().TrimEnd();

            if (!((IList)Enum.GetNames(typeof(ObjFSpellFlags))).Contains(trimmedFlag))
                ConsoleExtensions.Log($"Unrecognized ObjFSpellFlags param:|{trimmedFlag}|", "warn");

            foreach (var spellFlags in (ObjFSpellFlags[])Enum.GetValues(typeof(ObjFSpellFlags)))
            {
                if (!Enum.GetName(typeof(ObjFSpellFlags), spellFlags).Equals(trimmedFlag)) continue;
                //ConsoleExtensions.Log($"Recognized ObjFSpellFlags param:|{trimmedFlag}|", "success");
                flag = spellFlags;
            }
            return flag;
        }

        private static Tuple<BasicStatType, int> GetBasicStat(string paramValue)
        {
            Tuple<BasicStatType, int> basicStatTuple = null;
            var trimmedStats = paramValue.TrimStart();

            //skip junk
            if (trimmedStats.Equals("Strength 13") || trimmedStats.Equals("Dexterity 15") ||
                trimmedStats.Equals("Dexterity 14")) return null;

            var separator = "\t\t";
            if (trimmedStats.Contains("Gender") || trimmedStats.Contains("Race"))
                separator = " ";
            if (trimmedStats.Contains("tech points") || trimmedStats.Contains("magick points"))
            {
                var whitespaceIndexes = GetWhitespaceIndexes(trimmedStats);
                if (whitespaceIndexes.Count >= 2)
                {
                    trimmedStats = trimmedStats.Remove(whitespaceIndexes.First(), 1);
                    //ConsoleExtensions.Log($"trimmedStats after removal:|{trimmedStats}|", "warn");
                }
                separator = " ";
            }
            if (trimmedStats.Contains("Constitution") || trimmedStats.Contains("Intelligence"))
                separator = "\t";

            var statAndValue = trimmedStats.Split(separator, 2);
            var statType = statAndValue[0].Trim();
            statType = statType switch
            {
                "magickpoints" => "MagickPoints",
                "techpoints" => "TechPoints",
                _ => statType
            };

            if (!((IList)Enum.GetNames(typeof(BasicStatType))).Contains(statType))
                ConsoleExtensions.Log($"unrecognized Entity.BasicStatType param:|{statType}|", "warn");

            var statValue = statAndValue[1].Trim();
            //ConsoleExtensions.Log($"statType:|{statType}| value:|{statValue}|", "warn");

            foreach (var basicStatType in (BasicStatType[])Enum.GetValues(typeof(BasicStatType)))
            {
                var enumValueName = Enum.GetName(typeof(BasicStatType), basicStatType);
                if (!enumValueName.Equals(statType))
                {
                    //ConsoleExtensions.Log($"Failed to match enumValueName:|{enumValueName}| vs statType:|{statType}|", "warn");
                    continue;
                }
                basicStatTuple = new Tuple<BasicStatType, int>(basicStatType, int.Parse(statValue));
            }

            return basicStatTuple;
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
                            //ConsoleExtensions.Log($"paramValue:|{paramValue}|", "warn");
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
                            monster.ObjectFlags.Add(ParseObjFFlags(paramValue));
                            break;
                        case "Critter Flag":
                            monster.CritterFlags.Add(ParseObjFCritterFlags(paramValue));
                            break;
                        case "Critter2 Flag":
                            monster.CritterFlags2.Add(ParseObjFCritterFlags2(paramValue));
                            break;
                        case "NPC Flag":
                            monster.NpcFlags.Add(ParseObjFNpcFlags(paramValue));
                            break;
                        case "Blit Flag":
                            monster.BlitFlags.Add(ParseObjFBlitFlag(paramValue));
                            break;
                        case "Spell Flag":
                            monster.SpellFlags.Add(ParseObjFSpellFlags(paramValue));
                            break;
                        case "Hit Chart":
                            monster.HitChart = int.Parse(paramValue);
                            break;
                        case "Basic Stat" or "basic stat":
                            monster.BasicStats.Add(GetBasicStat(paramValue));
                            break;
                        case "Spell" or "spell":
                            monster.Spells.Add(paramValue);
                            break;
                        case "Script":
                            var trimmedScript = paramValue.TrimStart();
                            var scriptParams = trimmedScript.Split(" ", 6);

                            //foreach (var paramVal in scriptParams)
                               // ConsoleExtensions.Log($"script param value:|{paramVal}|", "warn");

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
                            ConsoleExtensions.Log($"unrecognized entity param:|{paramName}|", "error");
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