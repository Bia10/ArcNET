namespace ArcNET.Diagnostics;

public sealed record PlayerSarLifecycleTrackSummarySnapshot(
    string FingerprintKey,
    string Fingerprint,
    int PresentCount,
    int FirstSlot,
    int LastSlot,
    int MinElementCount,
    int MaxElementCount,
    bool ElementCountGrows,
    IReadOnlyList<int> BitsetWordCounts,
    IReadOnlyList<int> BitsetIds,
    string FirstValueSummary,
    string LastValueSummary,
    bool ValueChanged,
    string ValueAnnotation,
    PlayerSarLifecycleKind Lifecycle
);
