namespace ArcNET.Diagnostics;

public sealed record PlayerSarTrackSnapshot(
    string FingerprintKey,
    string Fingerprint,
    IReadOnlyList<PlayerSarTrackPointSnapshot> History
);
