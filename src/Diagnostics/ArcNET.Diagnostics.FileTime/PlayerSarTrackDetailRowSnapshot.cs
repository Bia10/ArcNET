namespace ArcNET.Diagnostics;

public sealed record PlayerSarTrackDetailRowSnapshot(
    string FingerprintKey,
    string Fingerprint,
    string Annotation,
    string Lifecycle,
    string ElementCountRange,
    string BitsetWordCounts,
    string BitsetIdLabel,
    string FirstValueSummary,
    string LastValueSummary,
    bool ValueChanged,
    int PresentCount
);
