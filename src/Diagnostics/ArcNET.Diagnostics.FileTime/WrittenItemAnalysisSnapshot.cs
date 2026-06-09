namespace ArcNET.Diagnostics;

public sealed record WrittenItemAnalysisSnapshot(int? Subtype, string? SubtypeLabel, int? StartLine, int? EndLine)
    : MobItemSpecificAnalysisSnapshot;
