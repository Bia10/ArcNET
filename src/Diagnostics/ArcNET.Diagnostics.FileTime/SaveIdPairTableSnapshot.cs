namespace ArcNET.Diagnostics;

public readonly record struct SaveIdPairTableSnapshot(
    int StartInt,
    int PairCount,
    int EndInt,
    int FirstId,
    int LastId,
    int NonZeroPairs,
    int MaxValue,
    IReadOnlyDictionary<int, int> Values
);
