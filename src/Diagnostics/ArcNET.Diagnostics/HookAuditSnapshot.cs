namespace ArcNET.Diagnostics;

public sealed record class HookAuditSnapshot(
    IReadOnlyList<string> Selectors,
    int DurationMilliseconds,
    bool IncludeWatch,
    bool IncludeIntercept,
    int StackCaptureDwordCount,
    int AuditedHookCount,
    int BoundHookCount,
    int BindFailureCount,
    int WatchSuccessCount,
    int WatchFailureCount,
    int InterceptSuccessCount,
    int InterceptFailureCount,
    int WatchObservedEventCount,
    int InterceptObservedEventCount,
    int WatchDroppedEventHookCount,
    int InterceptDroppedEventHookCount,
    bool ProcessExited,
    string? AbortedAtHook,
    IReadOnlyList<HookAuditResultSnapshot> Hooks
);
