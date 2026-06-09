namespace ArcNET.Diagnostics.Windows;

public sealed class SessionHandle : IDisposable
{
    private readonly ISessionConnection _connection;

    internal SessionHandle(
        ISessionConnection connection,
        SessionOrigin origin,
        LaunchPreviewSnapshot? launchPreview,
        AttachedSessionSnapshot snapshot
    )
    {
        _connection = connection;
        Origin = origin;
        LaunchPreview = launchPreview;
        Snapshot = snapshot;
    }

    public AttachedSessionSnapshot Snapshot { get; private set; }

    internal SessionOrigin Origin { get; }

    internal LaunchPreviewSnapshot? LaunchPreview { get; }

    internal ISessionConnection Connection => _connection;

    internal void UpdateSnapshot(AttachedSessionSnapshot snapshot) => Snapshot = snapshot;

    public void Dispose() => _connection.Dispose();
}
