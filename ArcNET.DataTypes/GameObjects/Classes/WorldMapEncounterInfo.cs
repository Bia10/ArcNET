using System.Collections.Generic;

namespace ArcNET.DataTypes.GameObjects.Classes
{
    public class WorldMapEncounterInfo
    {
        public List<WorldMapEncounterInfo> LoadedWorldMapEncounters;

        public class WorldMapEncounterInfoEntry
        {
            public int WorldWidth;
            public int WorldHeight;
            public int TriggerRadius;
            public int OccurrenceChance;
        }

        public int Id;
        public List<WorldMapEncounterInfoEntry> Entries;
    }
}