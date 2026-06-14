namespace ArcNET.Diagnostics;

public sealed class SessionHandle : IDisposable
{
    private readonly ISessionConnection _connection;

    internal SessionHandle(
        ISessionConnection connection,
        SessionOrigin origin,
        LaunchPreviewSnapshot? launchPreview,
        AttachedSessionSnapshot snapshot,
        string? workspacePathHint,
        string? runtimeWorkspacePathHint
    )
    {
        _connection = connection;
        Origin = origin;
        LaunchPreview = launchPreview;
        Snapshot = snapshot;
        WorkspacePathHint = workspacePathHint;
        RuntimeWorkspacePathHint = runtimeWorkspacePathHint;
    }

    public AttachedSessionSnapshot Snapshot { get; private set; }

    internal SessionOrigin Origin { get; }

    internal LaunchPreviewSnapshot? LaunchPreview { get; }

    internal string? WorkspacePathHint { get; private set; }

    internal string? RuntimeWorkspacePathHint { get; private set; }

    internal ISessionConnection Connection => _connection;

    internal void UpdateSnapshot(AttachedSessionSnapshot snapshot) => Snapshot = snapshot;

    internal void UpdateWorkspacePathHint(string? workspacePathHint) => WorkspacePathHint = workspacePathHint;

    internal void UpdateRuntimeWorkspacePathHint(string? runtimeWorkspacePathHint) =>
        RuntimeWorkspacePathHint = runtimeWorkspacePathHint;

    public void Dispose() => _connection.Dispose();
}
