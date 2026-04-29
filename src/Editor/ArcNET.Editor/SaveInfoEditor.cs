using ArcNET.Formats;

namespace ArcNET.Editor;

internal sealed class SaveInfoEditor
{
    private readonly LoadedSave _save;
    private readonly CharacterLocator _characterLocator;
    private SaveInfo? _pendingInfoUpdate;
    private bool _hasPendingPlayerUpdate;

    public SaveInfoEditor(LoadedSave save, CharacterLocator characterLocator)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(characterLocator);

        _save = save;
        _characterLocator = characterLocator;
    }

    public void SetPendingSaveInfo(SaveInfo updated) => _pendingInfoUpdate = updated;

    public void MarkPendingPlayerUpdate() => _hasPendingPlayerUpdate = true;

    public SaveInfo GetCurrentSaveInfo()
    {
        var info = _pendingInfoUpdate ?? _save.Info;

        if (!_hasPendingPlayerUpdate || !_characterLocator.TryGetCurrentOriginalPlayerCharacter(out var player))
            return info;

        var leaderName = player.Name ?? info.LeaderName;
        var leaderPortraitId = player.PortraitIndex >= 0 ? player.PortraitIndex : info.LeaderPortraitId;
        var leaderLevel = player.Level > 0 ? player.Level : info.LeaderLevel;

        if (
            string.Equals(leaderName, info.LeaderName, StringComparison.Ordinal)
            && leaderPortraitId == info.LeaderPortraitId
            && leaderLevel == info.LeaderLevel
        )
            return info;

        return info.With(leaderName: leaderName, leaderPortraitId: leaderPortraitId, leaderLevel: leaderLevel);
    }

    public SaveInfo? GetPendingSaveInfo()
    {
        var currentInfo = GetCurrentSaveInfo();
        return SaveInfoEquals(currentInfo, _save.Info) ? null : currentInfo;
    }

    private static bool SaveInfoEquals(SaveInfo left, SaveInfo right) =>
        left.Version == right.Version
        && string.Equals(left.ModuleName, right.ModuleName, StringComparison.Ordinal)
        && string.Equals(left.LeaderName, right.LeaderName, StringComparison.Ordinal)
        && string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal)
        && left.MapId == right.MapId
        && left.GameTimeDays == right.GameTimeDays
        && left.GameTimeMs == right.GameTimeMs
        && left.LeaderPortraitId == right.LeaderPortraitId
        && left.LeaderLevel == right.LeaderLevel
        && left.LeaderTileX == right.LeaderTileX
        && left.LeaderTileY == right.LeaderTileY
        && left.StoryState == right.StoryState;
}
