namespace ArcNET.Diagnostics;

public sealed record class EnvironmentSnapshot(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<string> RequestedProcessNames,
    IReadOnlyList<ProcessCandidateSnapshot> ProcessCandidates,
    IReadOnlyList<LiveRuntimeSnapshot> LiveRuntimes,
    bool CanAttachSingleRuntime,
    string AttachSummary,
    LaunchPreviewSnapshot? LaunchPreview,
    IReadOnlyList<string> Notes
);
