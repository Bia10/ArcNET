namespace ArcNET.Diagnostics;

public readonly record struct AlignedQuadSignatureSummary(
    AlignedQuadSignature Signature,
    int Count,
    int FirstRow,
    int LastRow,
    int FirstA,
    int LastA,
    int LongestRunLength,
    int LongestRunStart,
    int LongestRunFirstA,
    int LongestRunLastA
);
