using ArcNET.Diagnostics.Contracts;

namespace ArcNET.Diagnostics;

public sealed record class RuntimeStatusSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string DisplayName,
    string ModulePath,
    string ModuleBase,
    RuntimeFingerprint Fingerprint,
    RuntimeProfileSnapshot RuntimeProfile,
    RuntimeCapabilityReport Capabilities,
    uint? CurrentCharacterSheetId,
    int? ActionPoints,
    IReadOnlyList<string> Notes
);
