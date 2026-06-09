namespace ArcNET.Diagnostics;

public readonly record struct AlignedQuadRunSummary(
    int StartRow,
    int Length,
    AlignedQuadSignature Signature,
    int FirstA,
    int LastA
);
