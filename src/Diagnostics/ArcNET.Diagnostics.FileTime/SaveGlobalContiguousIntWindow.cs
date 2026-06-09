namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalContiguousIntWindow(
    int StartInt,
    int RemovedInts,
    int AddedInts,
    int CommonSuffixInts
);
