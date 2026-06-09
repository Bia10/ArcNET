namespace ArcNET.Diagnostics;

public sealed record SaveDynamicMobileAnalysisSnapshot(
    int ObjectCount,
    int SkippedSentinelCount,
    IReadOnlyList<SaveDynamicMobileEntrySnapshot> Entries
);
