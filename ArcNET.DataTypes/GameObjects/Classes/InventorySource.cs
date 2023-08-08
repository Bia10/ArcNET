using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Enumeration;

namespace ArcNET.DataTypes.GameObjects.Classes;

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

    public static IEnumerable<Tuple<string, double>> NamedDropTableFromId(int id)
    {
        return (from inventorySource in LoadedInventorySources.Where(invSrc => invSrc.Id.Equals(id))
            from inventorySourceEntry in inventorySource.Entries
            let name = Descriptions.GetNameFromId(inventorySourceEntry.PrototypeId)
            select new Tuple<string, double>(name, inventorySourceEntry.Chance)).ToList();
    }

    public static void InitFromText(IEnumerable<string> textData)
    {
        try
        {
            foreach ((string line, int _) in textData.WithIndex())
            {
                string[] idAndData = line.Split("}", 2);
                string id = idAndData[0];
                string data = idAndData[1];

                id = id.Remove(0, 1);
                var invSource = new InventorySource
                {
                    Id = int.Parse(id),
                    Entries = new List<InventorySourceEntry>()
                };
                string[] nameAndData = data.Split(":", 2);
                invSource.Name = nameAndData[0];

                string dropsAndChance = nameAndData[1];
                dropsAndChance = dropsAndChance.Replace("}", "");
                dropsAndChance = dropsAndChance.TrimStart().TrimEnd();

                //order matters else recursion is needed
                string[] badSplits = new[] { "      ", "     ", "    ", "   ", "  " };
                foreach (string badSplit in badSplits)
                {
                    if (!dropsAndChance.Contains(badSplit)) continue;

                    dropsAndChance = dropsAndChance.Replace(badSplit, " ");
                }

                //fix buggy data
                if (dropsAndChance.Contains(", "))
                    dropsAndChance = dropsAndChance.Replace(", ", ",");
                if (dropsAndChance.Contains(" 10141 "))
                    dropsAndChance = dropsAndChance.Replace(" 10141 ", "");

                string[] dropChanceArray = dropsAndChance.Split(" ");
                foreach (string dropChance in dropChanceArray)
                {
                    string[] dropAndChance = dropChance.Split(",");
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