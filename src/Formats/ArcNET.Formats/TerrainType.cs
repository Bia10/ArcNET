namespace ArcNET.Formats;

/// <summary>
/// Terrain type per tile.
/// </summary>
public enum TerrainType : byte
{
    /// <summary>Open grassland terrain.</summary>
    Grasslands = 0,

    /// <summary>Desert terrain.</summary>
    Desert = 1,

    /// <summary>Swamp terrain.</summary>
    Swamp = 2,

    /// <summary>Forest terrain.</summary>
    Forest = 3,

    /// <summary>Mountain terrain.</summary>
    Mountain = 4,

    /// <summary>Tundra / arctic terrain.</summary>
    Tundra = 5,

    /// <summary>Wasteland terrain.</summary>
    Wasteland = 6,

    /// <summary>Deep forest terrain.</summary>
    DeepForest = 7,

    /// <summary>Urban / city terrain.</summary>
    Urban = 8,

    /// <summary>Cavern terrain.</summary>
    Cavern = 9,

    /// <summary>Underground tunnel terrain.</summary>
    Underground = 10,

    /// <summary>Void / outer-plane terrain.</summary>
    Void = 11,

    /// <summary>Scorched earth terrain.</summary>
    ScorchedEarth = 12,

    /// <summary>Plains terrain.</summary>
    Plains = 13,

    /// <summary>Shallow water / bay terrain.</summary>
    ShallowWater = 14,

    /// <summary>Deep water / ocean terrain.</summary>
    DeepWater = 15,

    /// <summary>Farmland terrain.</summary>
    Farmland = 16,

    /// <summary>Sandy beach terrain.</summary>
    Beach = 17,

    /// <summary>Jungle terrain.</summary>
    Jungle = 18,
}
