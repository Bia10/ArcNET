namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalFileDiffSnapshot(
    bool IsIdentical,
    int BeforeByteLength,
    int AfterByteLength,
    int BeforeHeader0,
    int BeforeHeader1,
    int AfterHeader0,
    int AfterHeader1,
    int BeforeNonZeroCount,
    int AfterNonZeroCount,
    int ChangedInts,
    int PrefixInts,
    int AddedInts,
    int RemovedInts,
    int ChangedTailBytes,
    int BeforeTrailingBytes,
    int AfterTrailingBytes,
    IReadOnlyList<SaveGlobalChangedIntSampleSnapshot> ChangedSamples,
    SaveGlobalContiguousIntWindow? Window,
    SaveGlobalAlignedQuadDiffSnapshot? AlignedQuad,
    SaveGlobalSaveIdPairDiffSnapshot? SaveIdPairs
);
