namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalData2RegionFamilySnapshot(
    IReadOnlyList<int> Slots,
    int IntCount,
    string Sequence,
    string Preview
);
