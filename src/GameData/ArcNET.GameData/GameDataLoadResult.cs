namespace ArcNET.GameData;

public sealed class GameDataLoadResult(GameDataStore store, IReadOnlyList<GameDataLoadFailure> failures)
{
    public GameDataStore Store { get; } = store;

    public IReadOnlyList<GameDataLoadFailure> Failures { get; } = failures;
}
