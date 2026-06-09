namespace ArcNET.Diagnostics;

public readonly record struct SaveGlobalRangeAnalysisSnapshot(
    IReadOnlyDictionary<string, IReadOnlyList<SaveGlobalHotIndexHitSnapshot>> HotIndices,
    IReadOnlyDictionary<string, IReadOnlyList<SaveGlobalWindowPatternHitSnapshot>> WindowPatterns,
    IReadOnlyDictionary<string, IReadOnlyList<SaveGlobalWindowTraceHitSnapshot>> WindowTraces,
    IReadOnlyList<SaveGlobalFrontMatterFamilySnapshot> FrontMatterFamilies,
    IReadOnlyList<SaveGlobalTailFamilySnapshot> TailFamilies,
    IReadOnlyList<SaveGlobalData2RegionFamilySnapshot> PrefixFamilies,
    IReadOnlyList<SaveGlobalData2RegionFamilySnapshot> SuffixFamilies
);
