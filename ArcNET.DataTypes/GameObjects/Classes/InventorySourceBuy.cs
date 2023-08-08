using Spectre.Console;
using System;
using System.Collections.Generic;
using Utils.Enumeration;

namespace ArcNET.DataTypes.GameObjects.Classes;

public class InventorySourceBuy
{
    public static List<InventorySourceBuy> LoadedInventoryBuySources = new();

    public class InventorySourceBuyEntry
    {
        public int PrototypeId;

        public InventorySourceBuyEntry(int prototypeId)
        {
            PrototypeId = prototypeId;
        }
    }

    public int Id; //0 reserved
    public string Name;
    public List<InventorySourceBuyEntry> Entries; //TODO: Can be just {all}

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
                var invSourceBuy = new InventorySourceBuy
                {
                    Id = int.Parse(id),
                    Entries = new List<InventorySourceBuyEntry>()
                };
                string[] nameAndData = data.Split(":", 2);
                invSourceBuy.Name = nameAndData[0];
                string itemId = nameAndData[1];

                //bad data
                if (itemId.Contains(" 0,0"))
                    itemId = itemId.Replace(" 0,0", "");

                itemId = itemId.Replace("{", "");
                itemId = itemId.Replace("}", "");
                itemId = itemId.TrimStart().TrimEnd();

                //placeholders
                if (itemId.Equals("all")) itemId = "99999";
                if (itemId.Equals("")) itemId = "00000";

                //order matters else recursion is needed
                string[] badSplits = new[] { "      ", "     ", "    ", "   ", "  " };
                foreach (string badSplit in badSplits)
                {
                    if (!itemId.Contains(badSplit)) continue;

                    itemId = itemId.Replace(badSplit, " ");
                }

                string[] boughtItemsIds = itemId.Split(" ");
                foreach (string boughtItemId in boughtItemsIds)
                    invSourceBuy.Entries.Add(new InventorySourceBuyEntry(int.Parse(boughtItemId)));
                LoadedInventoryBuySources.Add(invSourceBuy);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            throw;
        }
    }
}