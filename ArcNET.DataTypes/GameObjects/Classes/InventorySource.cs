using ArcNET.Utilities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using AnsiConsoleExtensions = ArcNET.Utilities.AnsiConsoleExtensions;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class InventorySource
    {
        public static List<InventorySource> LoadedInventorySources = new();

        public class InventorySourceEntry
        {
            public int PrototypeId;
            public double Chance;

            public InventorySourceEntry(int prototypeId, double chance)
            {
                PrototypeId = prototypeId;
                Chance = chance;
            }
        }

        public int Id; //0 reserved
        public string Name;
        public List<InventorySourceEntry> Entries;

        public static void InitFromText(IEnumerable<string> textData)
        {
            try
            {
                foreach (var (line, index) in textData.WithIndex())
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("{"))
                    {
                        AnsiConsoleExtensions.Log($"bad line:|{line}| at index:|{index}|", "warn");
                        continue;
                    }

                    var idAndData = line.Split("}", 2);
                    var id = idAndData[0];
                    var data = idAndData[1];

                    id = id.Remove(0, 1);
                    AnsiConsoleExtensions.Log($"id |{id}|", "warn");
                    var invSource = new InventorySource()
                    {
                        Id = int.Parse(id),
                        Entries = new List<InventorySourceEntry>()
                    };
                    var nameAndData = data.Split(":", 2);
                    invSource.Name = nameAndData[0];

                    var dropsAndChance = nameAndData[1];
                    dropsAndChance = dropsAndChance.Replace("}", "");
                    dropsAndChance = dropsAndChance.TrimStart().TrimEnd();

                    //order matters else recursion is needed
                    var badSplits = new[] { "      ", "     ", "    ", "   ", "  " };
                    foreach (var badSplit in badSplits)
                    {
                        if (!dropsAndChance.Contains(badSplit)) continue;
                        AnsiConsoleExtensions.Log($"badSplits found:|{badSplit}| ", "warn");
                        dropsAndChance = dropsAndChance.Replace(badSplit, " ");
                    }

                    //fix buggy data
                    if (dropsAndChance.Contains(", "))
                        dropsAndChance = dropsAndChance.Replace(", ", ",");
                    if (dropsAndChance.Contains(" 10141 "))
                        dropsAndChance = dropsAndChance.Replace(" 10141 ", "");

                    var dropChanceArray = dropsAndChance.Split(" ");
                    foreach (var dropChance in dropChanceArray)
                    {
                        var dropAndChance = dropChance.Split(",");
                        var chance = int.Parse(dropAndChance[0]);
                        var dropId = int.Parse(dropAndChance[1]);

                        invSource.Entries.Add(new InventorySourceEntry(dropId, chance));
                    }
                    LoadedInventorySources.Add(invSource);
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