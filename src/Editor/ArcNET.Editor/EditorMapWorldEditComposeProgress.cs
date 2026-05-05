namespace ArcNET.Editor;

public sealed class EditorMapWorldEditComposeProgress
{
    public required string Activity { get; init; }

    public required float Progress { get; init; }

    public required TimeSpan Elapsed { get; init; }

    public required TimeSpan StageElapsed { get; init; }

    public string? DominantActivity { get; init; }

    public TimeSpan? DominantElapsed { get; init; }
}
