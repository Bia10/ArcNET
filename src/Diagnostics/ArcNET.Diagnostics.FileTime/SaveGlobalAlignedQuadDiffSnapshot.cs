namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalAlignedQuadDiffSnapshot(
    int BeforeSectionCount,
    int AfterSectionCount,
    int BeforeZeroSectionCount,
    int AfterZeroSectionCount,
    int BeforeLongestZeroSectionStart,
    int BeforeLongestZeroSectionLength,
    int AfterLongestZeroSectionStart,
    int AfterLongestZeroSectionLength,
    SaveGlobalFrontMatterDiffSnapshot? FrontMatter,
    SaveGlobalTailDiffSnapshot? Tail
);
