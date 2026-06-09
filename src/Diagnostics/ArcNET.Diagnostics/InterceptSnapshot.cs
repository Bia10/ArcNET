namespace ArcNET.Diagnostics;

public sealed record class InterceptSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsRunning,
    string Status,
    string Summary,
    string TargetKey,
    string TargetSite,
    string TargetSummary,
    string TargetResolution,
    string ExecutionModeText,
    int StackCaptureDwordCount,
    int TotalEvents,
    int TotalDroppedEvents,
    int TotalContentionDrops,
    int TotalWarnings,
    IReadOnlyList<InterceptEventSnapshot> Events
);
