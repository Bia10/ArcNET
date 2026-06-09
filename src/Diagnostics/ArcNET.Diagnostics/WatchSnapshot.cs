namespace ArcNET.Diagnostics;

public sealed record class WatchSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsRunning,
    string Status,
    string Summary,
    string PresetDisplayName,
    int TotalEvents,
    int TotalDroppedEvents,
    int TotalContentionDrops,
    int TotalWarnings,
    IReadOnlyList<WatchEventSnapshot> Events
);
