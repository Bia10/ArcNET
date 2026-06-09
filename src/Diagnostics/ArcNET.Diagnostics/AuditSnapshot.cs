using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class AuditSnapshot(
    DateTimeOffset GeneratedAtUtc,
    RuntimeFingerprint Fingerprint,
    RuntimeProfileSnapshot RuntimeProfile,
    DispatcherAuditSnapshot? Dispatcher,
    FunctionAuditSnapshot? Functions,
    HookAuditSnapshot? Hooks,
    IReadOnlyList<string> Notes
);
