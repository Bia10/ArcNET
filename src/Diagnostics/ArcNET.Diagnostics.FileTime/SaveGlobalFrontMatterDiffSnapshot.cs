namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalFrontMatterDiffSnapshot(
    int BeforeRowCount,
    int AfterRowCount,
    int BeforeSectionCount,
    int AfterSectionCount,
    int SamePrefixCount,
    AlignedQuadRunSummary? BeforeNextRun,
    AlignedQuadRunSummary? AfterNextRun
);
