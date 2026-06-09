namespace ArcNET.Diagnostics;

public sealed record PlayerSarFingerprintAggregateSnapshot(
    string Fingerprint,
    string Annotation,
    int FirstSlot,
    int LastSlot,
    int MinDuplicateCount,
    int MaxDuplicateCount,
    int TrackCount,
    int RecurringTrackCount,
    int SingleSlotTrackCount,
    int ChangedTrackCount
);
