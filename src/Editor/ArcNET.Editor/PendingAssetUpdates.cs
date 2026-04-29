namespace ArcNET.Editor;

internal sealed class PendingAssetUpdates<T>(IReadOnlyDictionary<string, T> current, Dictionary<string, T> pending)
    where T : class
{
    public int Count => pending.Count;

    public IReadOnlyDictionary<string, T>? PendingOrNull => pending.Count > 0 ? pending : null;

    public IEnumerable<KeyValuePair<string, T>> EnumerateCurrent()
    {
        foreach (var (path, original) in current)
        {
            if (pending.TryGetValue(path, out var staged))
                yield return new KeyValuePair<string, T>(path, staged);
            else
                yield return new KeyValuePair<string, T>(path, original);
        }
    }

    public T? GetCurrent(string path) =>
        pending.TryGetValue(path, out var staged) ? staged : current.GetValueOrDefault(path);

    public T? GetPending(string path) => pending.TryGetValue(path, out var staged) ? staged : null;

    public bool StageIfOriginalExists(string path, T updated)
    {
        if (!current.ContainsKey(path))
            return false;

        pending[path] = updated;
        return true;
    }
}
