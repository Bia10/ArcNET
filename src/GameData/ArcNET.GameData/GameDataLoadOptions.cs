namespace ArcNET.GameData;

public sealed record GameDataLoadOptions
{
    public static GameDataLoadOptions Default { get; } = new();

    public bool LoadArtMetadata { get; init; } = true;
}
