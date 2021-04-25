using ArcNET.Utilities;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes
{
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
                foreach (var (line, index) in textData.WithIndex())
                {
                    var idAndData = line.Split("}", 2);
                    var id = idAndData[0];
                    var data = idAndData[1];

                    id = id.Remove(0, 1);
                    var invSourceBuy = new InventorySourceBuy()
                    {
                        Id = int.Parse(id),
                        Entries = new List<InventorySourceBuyEntry>()
                    };
                    var nameAndData = data.Split(":", 2);
                    invSourceBuy.Name = nameAndData[0];
                    var itemId = nameAndData[1];

                    //bad data
                    if (itemId.Contains(" 0,0")) 
                        itemId = itemId.Replace(" 0,0","");

                    itemId = itemId.Replace("{", "");
                    itemId = itemId.Replace("}", "");
                    itemId = itemId.TrimStart().TrimEnd();

                    //placeholders
                    if (itemId.Equals("all")) itemId = "99999";
                    if (itemId.Equals("")) itemId = "00000";

                    //order matters else recursion is needed
                    var badSplits = new[] { "      ", "     ", "    ", "   ", "  " };
                    foreach (var badSplit in badSplits)
                    {
                        if (!itemId.Contains(badSplit)) continue;
                        itemId = itemId.Replace(badSplit, " ");
                    }

                    var boughtItemsIds = itemId.Split(" ");
                    foreach (var boughtItemId in boughtItemsIds)
                    {
                        invSourceBuy.Entries.Add(new InventorySourceBuyEntry(int.Parse(boughtItemId)));
                    }
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
}