namespace ArcNET.Diagnostics;

public readonly record struct AlignedQuadSummary(
    int StartInt,
    int QuadCount,
    int RemainderInts,
    int DistinctSignatures,
    int SectionCount,
    int ZeroSectionCount,
    int LongestZeroSectionStart,
    int LongestZeroSectionLength,
    int FrontMatterRowCount,
    int FrontMatterSectionCount,
    IReadOnlyList<AlignedQuadRunSummary> FrontMatterRuns,
    int TailRowStart,
    int TailRowCount,
    int TailSectionCount,
    IReadOnlyList<AlignedQuadRunSummary> TailRuns,
    IReadOnlyList<AlignedQuadRunSummary> LeadingRuns,
    IReadOnlyList<AlignedQuadRunSummary> TrailingRuns,
    IReadOnlyList<AlignedQuadSignatureSummary> TopSignatures,
    IReadOnlyList<AlignedQuadRunSummary> TopRuns
);
