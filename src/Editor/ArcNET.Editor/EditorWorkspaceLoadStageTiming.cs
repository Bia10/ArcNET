namespace ArcNET.Editor;

public sealed record EditorWorkspaceLoadStageTiming
{
    public required string StageName { get; init; }

    public required long ElapsedMs { get; init; }

    public bool IsDominant { get; init; }

    public int? ItemCount { get; init; }

    public string? UnitLabel { get; init; }
}
