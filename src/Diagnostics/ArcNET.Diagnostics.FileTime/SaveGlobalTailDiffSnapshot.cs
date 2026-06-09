namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalTailDiffSnapshot(
    int BeforeStartRow,
    int AfterStartRow,
    int BeforeRowCount,
    int AfterRowCount,
    int BeforeSectionCount,
    int AfterSectionCount,
    int SamePrefixCount,
    AlignedQuadRunSummary? BeforeNextRun,
    AlignedQuadRunSummary? AfterNextRun
);
