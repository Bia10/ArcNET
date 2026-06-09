using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class AttachedSessionSnapshot(
    DateTimeOffset GeneratedAtUtc,
    SessionOrigin Origin,
    string DisplayName,
    string Summary,
    string Detail,
    string ProcessName,
    int ProcessId,
    bool HasExited,
    RuntimeFingerprint Fingerprint,
    RuntimeProfileSnapshot RuntimeProfile,
    RuntimeCapabilityReport Capabilities,
    LaunchPreviewSnapshot? LaunchPreview,
    IReadOnlyList<string> Notes
);
