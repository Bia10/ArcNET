using ArcNET.Formats;

namespace ArcNET.Editor;

internal sealed class CharacterLocator
{
    private readonly LoadedSave _save;
    private readonly PendingGameUpdates _pendingUpdates;
    private bool _originalPlayerLocationInitialized;
    private (string Path, int Index)? _originalPlayerLocation;

    public CharacterLocator(LoadedSave save, PendingGameUpdates pendingUpdates)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(pendingUpdates);

        _save = save;
        _pendingUpdates = pendingUpdates;
    }

    public MobileMdyFile? GetCurrentMobileMdy(string mdyPath) => _pendingUpdates.MobileMdys.GetCurrent(mdyPath);

    public bool TryFindCharacter(
        Func<CharacterRecord, bool> predicate,
        bool includePending,
        out CharacterRecord character,
        out string mdyPath,
        out int recordIndex
    )
    {
        foreach (var (path, mdyFile) in EnumerateMobileMdys(includePending))
        {
            for (var index = 0; index < mdyFile.Records.Count; index++)
            {
                var record = mdyFile.Records[index];
                if (!record.IsCharacter)
                    continue;

                var candidate = CharacterRecord.From(record.Character);
                if (!predicate(candidate))
                    continue;

                character = candidate;
                mdyPath = path;
                recordIndex = index;
                return true;
            }
        }

        character = null!;
        mdyPath = string.Empty;
        recordIndex = -1;
        return false;
    }

    public bool TryFindPlayerCharacter(
        bool includePending,
        out CharacterRecord character,
        out string mdyPath,
        out int recordIndex
    ) =>
        TryFindCharacter(
            c => c.HasCompleteData && c.Name != null,
            includePending,
            out character,
            out mdyPath,
            out recordIndex
        ) || TryFindCharacter(c => c.HasCompleteData, includePending, out character, out mdyPath, out recordIndex);

    public bool IsOriginalPlayerLocation(string mdyPath, int recordIndex)
    {
        var playerLocation = GetOriginalPlayerLocation();
        return playerLocation is { Path: var playerPath, Index: var playerIndex }
            && string.Equals(playerPath, mdyPath, StringComparison.OrdinalIgnoreCase)
            && playerIndex == recordIndex;
    }

    public bool TryGetCurrentOriginalPlayerCharacter(out CharacterRecord character)
    {
        var playerLocation = GetOriginalPlayerLocation();
        if (playerLocation is not { Path: var mdyPath, Index: var recordIndex })
        {
            character = null!;
            return false;
        }

        var currentMdy = GetCurrentMobileMdy(mdyPath);
        if (currentMdy is null || recordIndex < 0 || recordIndex >= currentMdy.Records.Count)
        {
            character = null!;
            return false;
        }

        var record = currentMdy.Records[recordIndex];
        if (!record.IsCharacter)
        {
            character = null!;
            return false;
        }

        character = CharacterRecord.From(record.Character);
        return true;
    }

    private IEnumerable<KeyValuePair<string, MobileMdyFile>> EnumerateMobileMdys(bool includePending)
    {
        foreach (var entry in includePending ? _pendingUpdates.MobileMdys.EnumerateCurrent() : _save.MobileMdys)
            yield return entry;
    }

    private (string Path, int Index)? GetOriginalPlayerLocation()
    {
        if (_originalPlayerLocationInitialized)
            return _originalPlayerLocation;

        _originalPlayerLocationInitialized = true;
        if (TryFindPlayerCharacter(includePending: false, out _, out var mdyPath, out var recordIndex))
            _originalPlayerLocation = (mdyPath, recordIndex);

        return _originalPlayerLocation;
    }
}
