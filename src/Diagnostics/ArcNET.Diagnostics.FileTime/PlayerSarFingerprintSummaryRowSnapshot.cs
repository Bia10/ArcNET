namespace ArcNET.Diagnostics;

public sealed record PlayerSarFingerprintSummaryRowSnapshot(
    string Fingerprint,
    string Annotation,
    string SlotSpan,
    string DuplicateRange,
    int TrackCount,
    int RecurringTrackCount,
    int SingleSlotTrackCount,
    int ChangedTrackCount
);
