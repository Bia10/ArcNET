using Spectre.Console;
using System;
using System.Collections.Generic;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class Background
    {
        public static List<Background> LoadedBackgrounds = new();

        public enum RaceGenderCombo
        {
            HumanFemale,     //	HUF - human female
            HumanMale,       //	HUM - human male
            DwarfFemale,     //	DWF - dwarf female
            DwarfMale,       //	DWM - dwarf male
            ElfFemale,       //	ELF - elf female
            ElfMale,         //	ELM - elf male
            HalfElfFemale,   //	HEF - half elf female
            HalfElfMale,     //	HEM - half elf male
            GnomeFemale,     //	GNF - gnome female
            GnomeMale,       //	GNM - gnome male
            HalflingFemale,  //	HAF - halfling female
            HalflingMale,    //	HAM - halfling male
            HalfOrcFemale,   //	HOF - half orc female
            HalfOrcMale,     //	HOM - half orc male
            HalfOrgeFemale,  //	HGF - half ogre female
            HalfOrgeMale,    // HGM - half ogre male
            AnyNPC,          //	NPC - any NPC can use this background, regardless of their race or gender
            Any,             //	ANY - anyone can use this background (same as blank line, included for clarity)
        }

        public int DescriptionMessageIndex;
        public int EffectMessageIndex;
        public List<RaceGenderCombo> ValidRaceGenderCombinations;
        public int MoneyGiven;
        public List<int> ItemsGiven;

        public static void InitFromText(IEnumerable<string> textData)
        {
            Background curBackground = null;

            try
            {
                foreach (var line in textData)
                {
                    var indexAndData = line.Split("}", 2);
                    indexAndData[0] = indexAndData[0].Replace("{", "");
                    var index = int.Parse(indexAndData[0]);

                    indexAndData[1] = indexAndData[1].Replace("{", "");
                    indexAndData[1] = indexAndData[1].Replace("}", "");
                    indexAndData[1] = indexAndData[1].TrimEnd();
                    var data = indexAndData[1];
                    if (data.Contains(@"//"))
                    {
                        var idAndName = data.Split(@"//");
                        data = idAndName[0];
                    }

                    if (index == 0 || index % 10 == 0)
                    {
                        curBackground = new Background
                        {
                            DescriptionMessageIndex = int.Parse(data),
                            ValidRaceGenderCombinations = new List<RaceGenderCombo>(),
                            ItemsGiven = new List<int>()
                        };
                    }
                    else switch (index % 10)
                    {
                        case 1:
                            if (data.Equals("")) break;
                            if (curBackground != null) 
                                curBackground.EffectMessageIndex = int.Parse(data);
                            break;
                        case 2:
                            var combos = indexAndData[1].Split(" ");
                            foreach (var combo in combos)
                            {
                                switch (combo)
                                {
                                        case "HUF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HumanFemale);
                                            break;
                                        case "HUM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HumanMale);
                                            break;
                                        case "DWF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.DwarfFemale);
                                            break;
                                        case "DWM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.DwarfMale);
                                            break;
                                        case "ELF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.ElfFemale);
                                            break;
                                        case "ELM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.ElfMale);
                                            break;
                                        case "HEF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalfElfFemale);
                                            break;
                                        case "HEM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalfElfMale);
                                            break;
                                        case "GNF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.GnomeFemale);
                                            break;
                                        case "GNM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.GnomeMale);
                                            break;
                                        case "HAF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalflingFemale);
                                            break;
                                        case "HAM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalflingMale);
                                            break;
                                        case "HOF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalfOrcFemale);
                                            break;
                                        case "HOM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalfOrcMale);
                                            break;
                                        case "HGF":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalfOrgeFemale);
                                            break;
                                        case "HGM":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.HalfOrgeMale);
                                            break;
                                        case "NPC":
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.AnyNPC);
                                            break;
                                        case "" or "ANY" or "400": //400 is supposed to be money given
                                            curBackground?.ValidRaceGenderCombinations.Add(RaceGenderCombo.Any);
                                            break;

                                        default:
                                            AnsiConsoleExtensions.Log($"Unrecognized race gender combo:|{combo}|", "warn");
                                            break;
                                }
                            }
                            break;
                        case 3:
                            if (data.Equals("")) data = "400";
                            if (curBackground != null) curBackground.MoneyGiven = int.Parse(data);
                            break;
                        case 4:
                            var items = indexAndData[1].Split(" ");
                            if (items[0].Equals(""))
                            {
                                LoadedBackgrounds.Add(curBackground);
                                break;
                            }

                            foreach (var itemId in items)
                            {
                                curBackground?.ItemsGiven.Add(int.Parse(itemId));
                            }
                            LoadedBackgrounds.Add(curBackground);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                throw;
            }
        }
    }
}