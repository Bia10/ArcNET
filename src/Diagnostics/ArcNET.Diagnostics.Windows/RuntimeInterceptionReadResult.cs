namespace ArcNET.Diagnostics.Windows;

public readonly record struct RuntimeInterceptionReadResult(
    uint WriteSequence,
    int DroppedEvents,
    int InconsistentRecords,
    int ContentionDrops,
    IReadOnlyList<RuntimeInterceptionCapturedEvent> Events
);
