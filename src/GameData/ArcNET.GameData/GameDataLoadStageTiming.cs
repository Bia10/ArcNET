namespace ArcNET.GameData;

public sealed record GameDataLoadStageTiming
{
    public required string StageName { get; init; }

    public required long ElapsedMs { get; init; }

    public int? ItemCount { get; init; }

    public string? UnitLabel { get; init; }
}
