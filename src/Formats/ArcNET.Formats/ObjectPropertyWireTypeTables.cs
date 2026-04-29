using System.Collections.Frozen;
using ArcNET.GameObjects;

namespace ArcNET.Formats;

internal static class ObjectPropertyWireTypeTables
{
    public static FrozenDictionary<int, ObjectWireType> Common { get; } = CreateCommon();

    public static FrozenDictionary<ObjectType, FrozenDictionary<int, ObjectWireType>> ByObjectType { get; } =
        CreateByObjectType();

    private static FrozenDictionary<int, ObjectWireType> CreateCommon() =>
        new Dictionary<int, ObjectWireType>
        {
            [0] = ObjectWireType.Int32,
            [1] = ObjectWireType.Int64,
            [2] = ObjectWireType.Float,
            [3] = ObjectWireType.Float,
            [4] = ObjectWireType.Int32,
            [5] = ObjectWireType.Int32,
            [6] = ObjectWireType.Int32,
            [7] = ObjectWireType.Int32,
            [8] = ObjectWireType.Int32,
            [9] = ObjectWireType.Int32,
            [10] = ObjectWireType.Int32,
            [11] = ObjectWireType.Int32,
            [12] = ObjectWireType.Int32,
            [13] = ObjectWireType.Int32,
            [14] = ObjectWireType.Int32,
            [15] = ObjectWireType.Int32,
            [16] = ObjectWireType.Int32,
            [17] = ObjectWireType.Int32,
            [18] = ObjectWireType.Int32,
            [19] = ObjectWireType.Int32,
            [20] = ObjectWireType.Int32,
            [21] = ObjectWireType.Int32,
            [22] = ObjectWireType.Int32,
            [23] = ObjectWireType.Int32,
            [24] = ObjectWireType.Int32,
            [25] = ObjectWireType.Int32,
            [26] = ObjectWireType.Int32,
            [27] = ObjectWireType.Int32,
            [28] = ObjectWireType.Int32,
            [29] = ObjectWireType.Int32,
            [30] = ObjectWireType.Int32,
            [31] = ObjectWireType.ScriptArray,
            [32] = ObjectWireType.Int32,
            [33] = ObjectWireType.Int32,
            [34] = ObjectWireType.Float,
            [35] = ObjectWireType.Int64,
            [36] = ObjectWireType.Float,
            [37] = ObjectWireType.Float,
            [38] = ObjectWireType.Float,
            [39] = ObjectWireType.Float,
            [40] = ObjectWireType.Float,
            [41] = ObjectWireType.Int32Array,
            [42] = ObjectWireType.Int32Array,
            [43] = ObjectWireType.Int32Array,
            [44] = ObjectWireType.Int32,
            [45] = ObjectWireType.Int32,
            [46] = ObjectWireType.Int32,
            [47] = ObjectWireType.Int32,
            [48] = ObjectWireType.String,
            [49] = ObjectWireType.Int32,
            [50] = ObjectWireType.Int32,
            [51] = ObjectWireType.Int32,
            [52] = ObjectWireType.Int32,
            [53] = ObjectWireType.Int32,
            [54] = ObjectWireType.Float,
            [55] = ObjectWireType.Float,
            [56] = ObjectWireType.Float,
            [57] = ObjectWireType.Float,
            [58] = ObjectWireType.Float,
            [59] = ObjectWireType.Float,
            [60] = ObjectWireType.Float,
            [61] = ObjectWireType.Float,
            [62] = ObjectWireType.Float,
            [63] = ObjectWireType.Float,
        }.ToFrozenDictionary();

    private static FrozenDictionary<ObjectType, FrozenDictionary<int, ObjectWireType>> CreateByObjectType()
    {
        var itemBits = CreateItemBits();
        var critterBits = CreateCritterBits();

        return new Dictionary<ObjectType, FrozenDictionary<int, ObjectWireType>>
        {
            [ObjectType.Wall] = CreateWallBits(),
            [ObjectType.Portal] = CreatePortalBits(),
            [ObjectType.Container] = CreateContainerBits(),
            [ObjectType.Scenery] = CreateSceneryBits(),
            [ObjectType.Trap] = CreateTrapBits(),
            [ObjectType.Weapon] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 120,
                int32ArrayBit: 121,
                int64ArrayBit: 122
            ),
            [ObjectType.Ammo] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 100,
                int32ArrayBit: 101,
                int64ArrayBit: 102
            ),
            [ObjectType.Armor] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 105,
                int32ArrayBit: 106,
                int64ArrayBit: 107
            ),
            [ObjectType.Gold] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 99,
                int32ArrayBit: 100,
                int64ArrayBit: 101
            ),
            [ObjectType.Food] = CreateItemDerivedBits(itemBits, maxInt32Bit: 98, int32ArrayBit: 99, int64ArrayBit: 100),
            [ObjectType.Scroll] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 98,
                int32ArrayBit: 99,
                int64ArrayBit: 100
            ),
            [ObjectType.Key] = CreateItemDerivedBits(itemBits, maxInt32Bit: 98, int32ArrayBit: 99, int64ArrayBit: 100),
            [ObjectType.KeyRing] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 99,
                int32ArrayBit: 100,
                int64ArrayBit: 101
            ),
            [ObjectType.Written] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 101,
                int32ArrayBit: 102,
                int64ArrayBit: 103
            ),
            [ObjectType.Generic] = CreateItemDerivedBits(
                itemBits,
                maxInt32Bit: 98,
                int32ArrayBit: 99,
                int64ArrayBit: 100
            ),
            [ObjectType.Pc] = Merge(critterBits, CreatePcBits()),
            [ObjectType.Npc] = Merge(critterBits, CreateNpcBits()),
            [ObjectType.Projectile] = CreateProjectileBits(),
        }.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateWallBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddBits(map, ObjectWireType.Int32, 64, 65, 66);
        map[67] = ObjectWireType.Int32Array;
        map[68] = ObjectWireType.Int64Array;
        AddRange(map, 69, 93, ObjectWireType.Int32);
        map[94] = ObjectWireType.Int32Array;
        map[95] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreatePortalBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddRange(map, 64, 69, ObjectWireType.Int32);
        map[70] = ObjectWireType.Int32Array;
        map[71] = ObjectWireType.Int64Array;
        AddRange(map, 72, 93, ObjectWireType.Int32);
        map[94] = ObjectWireType.Int32Array;
        map[95] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateContainerBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddBits(map, ObjectWireType.Int32, 64, 65, 66, 67);
        map[68] = ObjectWireType.HandleArray;
        AddBits(map, ObjectWireType.Int32, 69, 70, 71, 72);
        map[73] = ObjectWireType.Int32Array;
        map[74] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateSceneryBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddBits(map, ObjectWireType.Int32, 64, 65, 66, 67);
        map[68] = ObjectWireType.Int32Array;
        map[69] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateTrapBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddBits(map, ObjectWireType.Int32, 64, 65, 66);
        map[67] = ObjectWireType.Int32Array;
        map[68] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateItemBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        map[64] = ObjectWireType.Int32;
        map[65] = ObjectWireType.Int64;
        AddRange(map, 66, 84, ObjectWireType.Int32);
        map[85] = ObjectWireType.Int32Array;
        map[86] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateItemDerivedBits(
        FrozenDictionary<int, ObjectWireType> itemBits,
        int maxInt32Bit,
        int int32ArrayBit,
        int int64ArrayBit
    )
    {
        var map = new Dictionary<int, ObjectWireType>(itemBits);
        AddRange(map, 96, maxInt32Bit, ObjectWireType.Int32);
        map[int32ArrayBit] = ObjectWireType.Int32Array;
        map[int64ArrayBit] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateCritterBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddBits(map, ObjectWireType.Int32, 64, 65);
        AddRange(map, 66, 69, ObjectWireType.Int32Array);
        AddBits(map, ObjectWireType.Int32, 70, 71, 72, 73);
        AddBits(map, ObjectWireType.Int32Array, 74, 75);
        map[76] = ObjectWireType.HandleArray;
        AddRange(map, 77, 83, ObjectWireType.Int32);
        map[84] = ObjectWireType.HandleArray;
        AddBits(map, ObjectWireType.Int32, 85, 86);
        map[87] = ObjectWireType.HandleArray;
        map[88] = ObjectWireType.Int64;
        map[89] = ObjectWireType.Int32;
        map[90] = ObjectWireType.Int64;
        AddRange(map, 91, 94, ObjectWireType.Int32);
        map[95] = ObjectWireType.Int32Array;
        map[96] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreatePcBits()
    {
        var map = new Dictionary<int, ObjectWireType>
        {
            [128] = ObjectWireType.Int32,
            [129] = ObjectWireType.Int32,
            [130] = ObjectWireType.Int32Array,
            [131] = ObjectWireType.Int32Array,
            [132] = ObjectWireType.Int32,
            [133] = ObjectWireType.String,
            [134] = ObjectWireType.Int32Array,
            [135] = ObjectWireType.Int32Array,
            [136] = ObjectWireType.Int32Array,
            [137] = ObjectWireType.Int32Array,
            [138] = ObjectWireType.Int32Array,
            [139] = ObjectWireType.Int32,
            [140] = ObjectWireType.Int32Array,
            [141] = ObjectWireType.Int32Array,
            [142] = ObjectWireType.Int32Array,
            [143] = ObjectWireType.Int32Array,
            [144] = ObjectWireType.Int32,
            [145] = ObjectWireType.String,
            [146] = ObjectWireType.Int32,
            [147] = ObjectWireType.Int32Array,
            [148] = ObjectWireType.Int32Array,
            [149] = ObjectWireType.Int32,
            [150] = ObjectWireType.Int32,
            [151] = ObjectWireType.Int32Array,
            [152] = ObjectWireType.Int64Array,
        };

        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateNpcBits()
    {
        var map = new Dictionary<int, ObjectWireType>
        {
            [128] = ObjectWireType.Int32,
            [129] = ObjectWireType.HandleArray,
            [130] = ObjectWireType.Int32Array,
            [131] = ObjectWireType.HandleArray,
            [132] = ObjectWireType.HandleArray,
            [133] = ObjectWireType.Int32,
            [134] = ObjectWireType.Int32,
            [135] = ObjectWireType.Int64Array,
            [136] = ObjectWireType.Int32,
            [137] = ObjectWireType.Int64,
            [138] = ObjectWireType.Int64,
            [139] = ObjectWireType.Int32,
            [140] = ObjectWireType.Int32,
            [141] = ObjectWireType.Int32,
            [142] = ObjectWireType.Int32,
            [143] = ObjectWireType.Int32,
            [144] = ObjectWireType.Int32,
            [145] = ObjectWireType.Int32Array,
            [146] = ObjectWireType.Int32Array,
            [147] = ObjectWireType.Int32Array,
            [148] = ObjectWireType.Int32,
            [149] = ObjectWireType.Int32Array,
            [150] = ObjectWireType.Int32,
            [151] = ObjectWireType.Int32Array,
            [152] = ObjectWireType.Int32Array,
        };

        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> CreateProjectileBits()
    {
        var map = new Dictionary<int, ObjectWireType>();
        AddBits(map, ObjectWireType.Int32, 64, 65, 66);
        map[67] = ObjectWireType.Int64;
        map[68] = ObjectWireType.Int32;
        map[69] = ObjectWireType.Int32;
        map[70] = ObjectWireType.Int32Array;
        map[71] = ObjectWireType.Int64Array;
        return map.ToFrozenDictionary();
    }

    private static FrozenDictionary<int, ObjectWireType> Merge(
        FrozenDictionary<int, ObjectWireType> baseBits,
        FrozenDictionary<int, ObjectWireType> specificBits
    )
    {
        var map = new Dictionary<int, ObjectWireType>(baseBits);
        foreach (var entry in specificBits)
            map[entry.Key] = entry.Value;
        return map.ToFrozenDictionary();
    }

    private static void AddBits(Dictionary<int, ObjectWireType> map, ObjectWireType wireType, params int[] bits)
    {
        foreach (var bit in bits)
            map[bit] = wireType;
    }

    private static void AddRange(
        Dictionary<int, ObjectWireType> map,
        int start,
        int endInclusive,
        ObjectWireType wireType
    )
    {
        for (var bit = start; bit <= endInclusive; bit++)
            map[bit] = wireType;
    }
}
