namespace ArcNET.Diagnostics;

public sealed record SaveInnerFileDiffSnapshot(
    string Path,
    bool OnlyInA,
    bool OnlyInB,
    int SizeA,
    int SizeB,
    byte[] BytesA,
    byte[] BytesB,
    IReadOnlyList<SaveBinaryDiffRegionSnapshot> Regions,
    int ChangedByteCount
);
