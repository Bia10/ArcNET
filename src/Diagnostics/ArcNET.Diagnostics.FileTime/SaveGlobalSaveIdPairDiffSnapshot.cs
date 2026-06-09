namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalSaveIdPairDiffSnapshot(
    int BeforeStartInt,
    int AfterStartInt,
    int BeforePairCount,
    int AfterPairCount,
    int BeforeNonZeroPairs,
    int AfterNonZeroPairs,
    int BeforePrefixIntCount,
    int AfterPrefixIntCount,
    int PrefixChangedInts,
    int BeforeSuffixIntCount,
    int AfterSuffixIntCount,
    int SuffixChangedInts,
    IReadOnlyList<SaveGlobalSaveIdPairValueDiffSnapshot> ChangedPairs,
    int TotalChangedPairs
);
