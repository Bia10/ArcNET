namespace ArcNET.Diagnostics;

public sealed record class WatchStartRequest(
    AttachedSessionSnapshot Session,
    TimelinePresetDescriptor Preset,
    int EventCapacity = 40
);
