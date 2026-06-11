namespace ArcNET.Editor;

public sealed class EditorWorkspaceLoadProgress
{
    public required string Activity { get; init; }

    public required float OverallProgress { get; init; }

    public int? CompletedUnits { get; init; }

    public int? TotalUnits { get; init; }

    public string? UnitLabel { get; init; }

    public required TimeSpan Elapsed { get; init; }

    public TimeSpan? EstimatedRemaining { get; init; }

    public DateTimeOffset? EstimatedCompletionTime { get; init; }

    public IReadOnlyList<EditorWorkspaceLoadStageTiming> StageTimings { get; init; } = [];
}
