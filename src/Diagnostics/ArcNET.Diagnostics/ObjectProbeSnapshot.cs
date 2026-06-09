namespace ArcNET.Diagnostics;

public sealed record class ObjectProbeSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string SourceLabel,
    IReadOnlyList<string> RequestedHandles,
    IReadOnlyList<ObjectProbeObjectSnapshot> Objects
);
