namespace ArcNET.Diagnostics;

public sealed record class CrashDumpAnalysisParsedOutput(
    string? ProcessName,
    string? ExceptionCode,
    string? FaultingInstruction,
    IReadOnlyList<string> StackPreview,
    IReadOnlyList<string> Highlights
);
