using System.Collections.Generic;
using ArcNET.DataTypes.Common;
using ArcNET.DataTypes.GameObjects;
using Newtonsoft.Json;

namespace ArcNET.DataTypes;

public class Sector
{
    public struct SectorTile
    {
        public uint Data { get; set; }
    }

    public IList<SectorLight> Lights { get; private set; } = new List<SectorLight>();
    public SectorTile[] Tiles { get; private set; } = new SectorTile[4096];
    public List<GameObject> Objects { get; private set; } = new();
    public GameObjectScript SectorScript { get; set; }
    public List<TileScript> TileScripts { get; private set; } = new();

    public static uint GetSectorLoc(int x, int y)
        => (((uint)y << 26) & 0xFC) | ((uint)x & 0xFC);

        public override string ToString()
        {
            return $"{F1} {F2} {F3} {F4} {F5} {F6}";
        }
    }
}
