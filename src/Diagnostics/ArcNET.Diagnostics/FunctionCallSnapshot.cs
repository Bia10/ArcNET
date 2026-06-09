namespace ArcNET.Diagnostics;

public sealed record class FunctionCallSnapshot(
    DateTimeOffset GeneratedAtUtc,
    bool IsAvailable,
    string Status,
    string Summary,
    string TargetKey,
    string TargetSite,
    string CleanupModeText,
    string DispatcherText,
    string TargetAddressText,
    string ResultEaxText,
    string ResultEdxText,
    IReadOnlyList<FunctionCallArgumentSnapshot> Arguments
);
