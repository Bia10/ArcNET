namespace ArcNET.Diagnostics;

public sealed record SaveGlobalSaveIdPairDetailsSnapshot(
    int StartInt,
    int EndInt,
    int PairCount,
    int FirstId,
    int LastId,
    int NonZeroPairs,
    int MaxValue,
    SaveGlobalData2RegionPreviewSnapshot? PrefixPreview,
    SaveGlobalData2RegionPreviewSnapshot? SuffixPreview,
    IReadOnlyList<SaveGlobalSaveIdPairValueSnapshot> NonZeroPairPreview,
    int OmittedNonZeroPairCount
);
