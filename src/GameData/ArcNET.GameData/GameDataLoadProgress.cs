namespace ArcNET.GameData;

public sealed class GameDataLoadProgress(
    string activity,
    float progress,
    int? completedEntries = null,
    int? totalEntries = null
)
{
    public string Activity { get; } = activity;

    public float Progress { get; } = progress;

    public int? CompletedEntries { get; } = completedEntries;

    public int? TotalEntries { get; } = totalEntries;
}
