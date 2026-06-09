namespace ArcNET.Diagnostics;

public sealed record PlayerSarLifecycleReportSnapshot(
    IReadOnlyList<PlayerSarFingerprintSummaryRowSnapshot> Fingerprints,
    IReadOnlyList<PlayerSarTrackDetailRowSnapshot> Tracks,
    int OmittedTrackRowCount,
    int OmittedSingletonFingerprintCount
);
