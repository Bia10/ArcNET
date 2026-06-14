namespace ArcNET.Diagnostics;

public sealed class WatchHandle : IDisposable
{
    private readonly IWatchSession _session;
    private readonly List<WatchEventSnapshot> _events = [];

    internal WatchHandle(IWatchSession session, WatchStartRequest request, WatchSnapshot snapshot)
    {
        _session = session;
        Request = request;
        Snapshot = snapshot;
    }

    public WatchSnapshot Snapshot { get; private set; }

    internal WatchStartRequest Request { get; }

    internal IReadOnlyList<WatchEventSnapshot> Events => _events;

    internal uint LastSequence { get; set; }

    internal int TotalEvents { get; set; }

    internal int TotalDroppedEvents { get; set; }

    internal int TotalContentionDrops { get; set; }

    internal int TotalWarnings { get; set; }

    internal IWatchSession Session => _session;

    internal void AddEvents(IEnumerable<WatchEventSnapshot> events, int capacity)
    {
        _events.AddRange(events);
        if (_events.Count <= capacity)
            return;

        _events.RemoveRange(0, _events.Count - capacity);
    }

    internal void UpdateSnapshot(WatchSnapshot snapshot) => Snapshot = snapshot;

    public void Dispose() => _session.Dispose();
}
