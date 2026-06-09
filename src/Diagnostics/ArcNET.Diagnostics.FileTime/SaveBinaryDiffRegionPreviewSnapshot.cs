namespace ArcNET.Diagnostics;

public sealed record SaveBinaryDiffRegionPreviewSnapshot(
    int Offset,
    int Length,
    int ChangedByteCount,
    IReadOnlyList<SaveBinaryHexRowSnapshot> Rows
);
