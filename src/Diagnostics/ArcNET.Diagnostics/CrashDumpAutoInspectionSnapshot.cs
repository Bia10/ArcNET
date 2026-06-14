namespace ArcNET.Diagnostics;

public sealed record class CrashDumpAutoInspectionSnapshot(
    DateTimeOffset GeneratedAtUtc,
    CrashDumpAutoConfigurationSnapshot Configuration,
    string Status,
    IReadOnlyList<string> Notes,
    string? LatestDumpPath,
    DateTimeOffset? LatestDumpWrittenAtUtc,
    long? LatestDumpSizeBytes,
    CrashDumpAnalysisSnapshot? Analysis
);
