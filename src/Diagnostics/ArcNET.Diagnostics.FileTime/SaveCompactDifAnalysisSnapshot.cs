namespace ArcNET.Diagnostics;

public sealed record SaveCompactDifAnalysisSnapshot(
    int Magic,
    SaveCompactDifVariant Variant,
    IReadOnlyList<SaveCompactDifRecordSnapshot> Records,
    int? TrailingValue,
    bool MissingStartSentinel
);
