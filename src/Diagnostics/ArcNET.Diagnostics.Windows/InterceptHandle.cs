using ArcNET.Diagnostics;

namespace ArcNET.Diagnostics.Windows;

public sealed class InterceptHandle : IDisposable
{
    private readonly IInterceptSession _session;

    internal InterceptHandle(IInterceptSession session, InterceptStartRequest request, InterceptSnapshot snapshot)
    {
        _session = session;
        Request = request;
        Snapshot = snapshot;
    }

    public InterceptSnapshot Snapshot { get; private set; }

    internal InterceptStartRequest Request { get; }

    internal uint LastSequence { get; set; }

    internal int TotalEvents { get; set; }

    internal int TotalDroppedEvents { get; set; }

    internal int TotalContentionDrops { get; set; }

    internal int TotalWarnings { get; set; }

    internal IInterceptSession Session => _session;

    internal void UpdateSnapshot(InterceptSnapshot snapshot) => Snapshot = snapshot;

    public void Dispose() => _session.Dispose();
}
