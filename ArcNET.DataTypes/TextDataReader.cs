using ArcNET.DataTypes.GameObjects.Classes;
using ArcNET.Utilities;
using System.Collections.Generic;
using System.IO;

namespace ArcNET.DataTypes
{
    public class TextDataReader
    {
        private readonly StreamReader _reader;
        public readonly List<Monster> _monsters = new();
        public readonly List<NPC> _npcs = new();
        public readonly List<Unique> _uniques = new();

        public TextDataReader(StreamReader reader)
        {
            _reader = reader;
        }

        public void Parse(string type)
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
                        case "Portrait":
                            mobStringList.Add(curLine);
                            break;
                        case "Retail Price Multiplier":
                            mobStringList.Add(curLine);
                            break;
                        case "Social Class":
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
                    switch (type)
                    {
                        case "Monster":
                        {
                            var currentMob = Monster.GetFromText(mobStringList);
                            _monsters.Add(currentMob);
                            break;
                        }
                        case "NPC":
                        {
                            var currentNpc = NPC.GetFromText(mobStringList);
                            _npcs.Add(currentNpc);
                            break;
                        }
                        case "Unique":
                        {
                            //var currentUnique = Unique.GetFromText(mobStringList);
                            //_uniques.Add(currentUnique);
                            break;
                        }
                    }

                    mobStringList.Clear();
                }

                if (curLine == null) break;
            }
        }
    }
}