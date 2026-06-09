namespace ArcNET.Diagnostics;

public sealed record PlayerSarTransitionSummarySnapshot(
    int AddedCount,
    int RemovedCount,
    int MovedCount,
    int ChangedCount,
    IReadOnlyList<PlayerSarFingerprintCountSnapshot> MovedFingerprints,
    IReadOnlyList<PlayerSarFingerprintCountSnapshot> ChangedFingerprints
);
