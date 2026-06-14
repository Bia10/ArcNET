namespace ArcNET.Diagnostics;

public readonly record struct RuntimeWatchReadResult(
    uint WriteSequence,
    int DroppedEvents,
    int InconsistentRecords,
    int ContentionDrops,
    IReadOnlyList<RuntimeWatchCapturedEvent> Events
);
