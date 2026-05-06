namespace ArcNET.Editor;

internal sealed class RetainedCacheBudget<TKey>
    where TKey : notnull
{
    private sealed class Entry
    {
        public required long ApproximateSizeBytes { get; set; }

        public required long LastAccessToken { get; set; }
    }

    private readonly Dictionary<TKey, Entry> _entries;
    private readonly int _maxEntryCount;
    private readonly long _maxRetainedBytes;
    private long _nextAccessToken;

    public RetainedCacheBudget(IEqualityComparer<TKey>? comparer, long maxRetainedBytes, int maxEntryCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetainedBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntryCount);

        _entries = comparer is null ? [] : new Dictionary<TKey, Entry>(comparer);
        _maxRetainedBytes = maxRetainedBytes;
        _maxEntryCount = maxEntryCount;
    }

    public int EntryCount => _entries.Count;

    public long RetainedBytes { get; private set; }

    public void Clear()
    {
        _entries.Clear();
        RetainedBytes = 0L;
    }

    public IReadOnlyList<TKey> Register(TKey key, long approximateSizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(approximateSizeBytes);

        if (_entries.TryGetValue(key, out var existingEntry))
        {
            RetainedBytes -= existingEntry.ApproximateSizeBytes;
            existingEntry.ApproximateSizeBytes = approximateSizeBytes;
            existingEntry.LastAccessToken = NextAccessToken();
            RetainedBytes += approximateSizeBytes;
        }
        else
        {
            _entries[key] = new Entry
            {
                ApproximateSizeBytes = approximateSizeBytes,
                LastAccessToken = NextAccessToken(),
            };
            RetainedBytes += approximateSizeBytes;
        }

        return EvictToBudget(excludedKey: key);
    }

    public bool TryTouch(TKey key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return false;

        entry.LastAccessToken = NextAccessToken();
        return true;
    }

    private IReadOnlyList<TKey> EvictToBudget(TKey excludedKey)
    {
        var evictedKeys = new List<TKey>();
        while ((_entries.Count > _maxEntryCount || RetainedBytes > _maxRetainedBytes) && _entries.Count > 1)
        {
            var hasVictim = false;
            var victimKey = excludedKey;
            var oldestAccessToken = long.MaxValue;

            foreach (var pair in _entries)
            {
                if (
                    EqualityComparer<TKey>.Default.Equals(pair.Key, excludedKey)
                    || pair.Value.LastAccessToken >= oldestAccessToken
                )
                    continue;

                hasVictim = true;
                victimKey = pair.Key;
                oldestAccessToken = pair.Value.LastAccessToken;
            }

            if (!hasVictim)
                break;

            var victimEntry = _entries[victimKey];
            _entries.Remove(victimKey);
            RetainedBytes -= victimEntry.ApproximateSizeBytes;
            evictedKeys.Add(victimKey);
        }

        return evictedKeys;
    }

    private long NextAccessToken() => ++_nextAccessToken;
}
