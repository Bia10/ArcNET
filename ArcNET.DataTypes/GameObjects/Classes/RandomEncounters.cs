using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes;

public class RandomEncounters
{
    public static List<RandomEncounters> LoadedEncounters;

    public class EncounterEntry
    {
        public int PrototypeId;
        public int CountMin;
        public int CountMax;
        public int MinLevel;
        public int MaxLevel;
        public int GlobalFlag;
    }

    public int Id;
    public int ChancePercentage;
    public List<EncounterEntry> EncounterEntries;
}