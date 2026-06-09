namespace ArcNET.Diagnostics;

public sealed record SaveBinaryDiffSetSnapshot(
    int TotalFiles,
    int ChangedFileCount,
    int IdenticalFileCount,
    IReadOnlyList<SaveInnerFileDiffSnapshot> Files
);
