namespace ArcNET.Diagnostics;

public sealed record SaveBinaryDiffPreviewSnapshot(
    IReadOnlyList<SaveBinaryDiffRegionPreviewSnapshot> Regions,
    int OmittedRegionCount
);
