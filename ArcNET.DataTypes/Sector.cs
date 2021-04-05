using ArcNET.DataTypes.Common;
using ArcNET.DataTypes.GameObjects;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ArcNET.DataTypes
{
    public class Sector
    {
        public struct SectorTile
        {
            public uint Data { get; set; }
        }

        public IList<SectorLight> Lights { get; private set; }
        public SectorTile[] Tiles { get; private set; }
        public List<GameObject> Objects { get; private set; }
        public GameObjectScript SectorScript { get; set; }
        public List<TileScript> TileScripts { get; private set; }

        public Sector()
        {
            Lights = new List<SectorLight>();
            Tiles = new SectorTile[4096];
            Objects = new List<GameObject>();
            TileScripts = new List<TileScript>();
        }

        public static uint GetSectorLoc(int x, int y)
        {
            return ((uint)y << 26) & 0xFC | ((uint)x & 0xFC);
        }

        public string GetEntriesAsJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class SectorLight
    {
        public ulong Handle { get; set; }
        public Location Position { get; set; } // x, y, offsx, offsy
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public int Flags0 { get; set; }
        public int Art { get; set; }
        public int Color0 { get; set; }
        public int Color1 { get; set; }
        public int Unk0 { get; set; } // MAYBE FLAGS
        public int Unk1 { get; set; }
    }

    public class TileScript
    {
        public int F1 { get; set; }
        public int F2 { get; set; }
        public int F3 { get; set; }
        public int F4 { get; set; }
        public int F5 { get; set; }
        public int F6 { get; set; }

        public override string ToString()
        {
            return $"{F1} {F2} {F3} {F4} {F5} {F6}";
        }
    }
}