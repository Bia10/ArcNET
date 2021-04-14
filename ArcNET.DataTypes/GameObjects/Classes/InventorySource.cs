using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class InventorySource
    {
        public class InventorySourceEntry
        {
            public double Chance; //TODO: the values are a mess .... 0.0%,-10%,500%?
            public int PrototypeId;
        }

        public int Id; //0 reserved
        public string Name;
        public List<InventorySourceEntry> Entries;
    }
}