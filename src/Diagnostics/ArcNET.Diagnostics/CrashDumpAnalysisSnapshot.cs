namespace ArcNET.Diagnostics;

public sealed record class CrashDumpAnalysisSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string DumpPath,
    bool AnalyzerFound,
    bool AnalysisWritten,
    string AnalyzerPath,
    string OutputPath,
    int? ExitCode,
    string Status,
    IReadOnlyList<string> Highlights,
    string? ProcessName,
    string? ExceptionCode,
    string? FaultingInstruction,
    IReadOnlyList<string> StackPreview
);
