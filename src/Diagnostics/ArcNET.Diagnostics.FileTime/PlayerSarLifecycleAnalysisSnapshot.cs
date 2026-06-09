namespace ArcNET.Diagnostics;

public sealed record PlayerSarLifecycleAnalysisSnapshot(
    int TotalSlots,
    IReadOnlyList<PlayerSarLifecycleTrackSummarySnapshot> Tracks,
    IReadOnlyList<PlayerSarFingerprintAggregateSnapshot> Fingerprints
);
