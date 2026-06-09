namespace ArcNET.Diagnostics;

public sealed record class HookPassAuditSnapshot(
    bool Success,
    bool ObservedEvents,
    int EventCount,
    int DroppedEvents,
    int InconsistentRecords,
    int ContentionDrops,
    string? FirstCallerSite,
    string? Error
);
