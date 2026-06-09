using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class LiveRuntimeSnapshot(
    string ScenarioKey,
    string DisplayName,
    string Summary,
    string ProcessName,
    int ProcessId,
    RuntimeFingerprint Fingerprint,
    RuntimeProfileSnapshot RuntimeProfile,
    RuntimeCapabilityReport Capabilities
);
