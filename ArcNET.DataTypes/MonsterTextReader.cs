using ArcNET.DataTypes.GameObjects.Classes;
using ArcNET.Utilities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace ArcNET.DataTypes
{
    public class MonsterTextReader
    {
        private readonly StreamReader _reader;
        private List<Monster> _monsters;

        public MonsterTextReader(StreamReader reader)
        {
            _reader = reader;
        }

        public string GetEntriesAsJson()
        {
            return JsonConvert.SerializeObject(_monsters, Formatting.Indented);
        }

        public MonsterTextReader Parse()
        {
            var monsterTextReader = new MonsterTextReader(_reader);

            while (true)
            {
                var curLine = _reader.ReadLine();
                //AnsiConsoleExtensions.Log($"currentLine:|{curLine}|", "warn");

                if (!string.IsNullOrEmpty(curLine))
                {
                    var lines = curLine.Split(":", 2);
                    var paramName = lines[0];
                    var paramValue = lines[1];

                    switch (paramName)
                    {
                        case "Description" when paramValue.Contains(@"\\"):
                        {
                            var idAndName = paramValue.Split(@"\\", 2);
                            var monsterId = idAndName[0];
                            var monsterName = idAndName[1];
                            break;
                        }
                        case "Description":
                            break;
                        case "Internal Name" or "internal name":
                            break;
                        case "Level":
                            break;
                        case "Art Number and Palette":
                            break;
                        case "Scale":
                            break;
                        case "Alignment":
                            break;
                        case "Object Flag":
                            break;
                        case "Critter Flag":
                            break;
                        case "Critter2 Flag":
                            break;
                        case "NPC Flag":
                            break;
                        case "Blit Flag":
                            break;
                        case "Spell Flag":
                            break;
                        case "Hit Chart":
                            break;
                        case "Basic Stat" or "basic stat":
                            break;
                        case "Spell" or "spell":
                            break;
                        case "Script":
                            break;
                        case "Faction":
                            break;
                        case "AI Packet":
                            break;
                        case "Material":
                            break;
                        case "Hit Points":
                            break;
                        case "Fatigue":
                            break;
                        case "Damage Resistance" or "damage resistance":
                            break;
                        case "Fire Resistance":
                            break;
                        case "Electrical Resistance":
                            break;
                        case "Poison Resistance":
                            break;
                        case "Magic Resistance":
                            break;
                        case "Normal Damage":
                            break;
                        case "Fatigue Damage":
                            break;
                        case "Poison Damage":
                            break;
                        case "Electrical Damage":
                            break;
                        case "Fire Damage":
                            break;
                        case "Sound Bank" or "sound bank":
                            break;
                        case "Category":
                            break;
                        case "Auto Level Scheme":
                            break;
                        case "Inventory Source":
                            break;

                        default:
                            AnsiConsoleExtensions.Log($"unrecognized entity param:|{paramName}|", "warn");
                            break;
                    }

                    if (string.IsNullOrEmpty(curLine))
                    {
                        AnsiConsoleExtensions.Log($"empty line:|{curLine}|", "warn");
                        continue;
                    }
                }

                if (curLine == null)
                {
                    break;
                }
            }

            return monsterTextReader;
        }
    }
}