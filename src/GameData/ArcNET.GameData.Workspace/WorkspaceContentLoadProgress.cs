namespace ArcNET.GameData.Workspace;

/// <summary>
/// Progress update for install-backed or module-backed source discovery and reading.
/// </summary>
public sealed class WorkspaceContentLoadProgress(
    string activity,
    float progress,
    int? completedUnits = null,
    int? totalUnits = null,
    string? unitLabel = null
)
{
    public string Activity { get; } = activity;

    public float Progress { get; } = progress;

    public int? CompletedUnits { get; } = completedUnits;

    public int? TotalUnits { get; } = totalUnits;

    public string? UnitLabel { get; } = unitLabel;
}
