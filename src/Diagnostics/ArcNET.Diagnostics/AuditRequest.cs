namespace ArcNET.Diagnostics;

public sealed record class AuditRequest(
    AttachedSessionSnapshot Session,
    bool IncludeDispatcher,
    bool IncludeFunctions,
    bool IncludeHooks,
    IReadOnlyList<string> HookSelectors,
    TimeSpan HookDuration,
    bool IncludeWatchPass,
    bool IncludeInterceptPass,
    int StackCaptureDwordCount,
    bool StopOnFailure
);
