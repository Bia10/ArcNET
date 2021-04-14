using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class InventorySourceBuy
    {
        public class InventorySourceBuyEntry
        {
            public int PrototypeId;
        }

        public int Id; //0 reserved
        public string Name;
        public List<InventorySourceBuyEntry> Entries; //TODO: Can be just {all}
    }
}