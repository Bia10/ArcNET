namespace ArcNET.Editor;

/// <summary>
/// Persisted host-neutral editor-project metadata for reopening a workspace and restoring UI state.
/// </summary>
public sealed class EditorProject
{
    /// <summary>
    /// Current on-disk schema version for <see cref="EditorProjectStore"/> payloads.
    /// </summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>
    /// Persisted schema version.
    /// </summary>
    public int FormatVersion { get; init; } = CurrentFormatVersion;

    /// <summary>
    /// Workspace input used to reopen game content and an optional save slot.
    /// </summary>
    public required EditorProjectWorkspaceReference Workspace { get; init; }

    /// <summary>
    /// Optional currently active asset path.
    /// </summary>
    public string? ActiveAssetPath { get; init; }

    /// <summary>
    /// Persisted open-document state.
    /// </summary>
    public IReadOnlyList<EditorProjectOpenAsset> OpenAssets { get; init; } = [];

    /// <summary>
    /// Persisted bookmarks.
    /// </summary>
    public IReadOnlyList<EditorProjectBookmark> Bookmarks { get; init; } = [];

    /// <summary>
    /// Persisted typed map-view state for camera, selection, and preview configuration.
    /// </summary>
    public IReadOnlyList<EditorProjectMapViewState> MapViewStates { get; init; } = [];

    /// <summary>
    /// Persisted view-state entries.
    /// </summary>
    public IReadOnlyList<EditorProjectViewState> ViewStates { get; init; } = [];

    /// <summary>
    /// Persisted tool-state entries.
    /// </summary>
    public IReadOnlyList<EditorProjectToolState> ToolStates { get; init; } = [];

    /// <summary>
    /// Creates an empty project for the supplied workspace reference.
    /// </summary>
    public static EditorProject Create(EditorProjectWorkspaceReference workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return new EditorProject { Workspace = workspace };
    }

    /// <summary>
    /// Seeds a new editor-project model from an already loaded workspace.
    /// </summary>
    public static EditorProject FromWorkspace(EditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var projectWorkspace = string.IsNullOrWhiteSpace(workspace.GameDirectory)
            ? EditorProjectWorkspaceReference.ForContentDirectory(
                workspace.ContentDirectory,
                workspace.SaveFolder,
                workspace.SaveSlotName
            )
            : EditorProjectWorkspaceReference.ForGameInstall(
                workspace.GameDirectory!,
                workspace.Module?.ModuleName,
                workspace.SaveFolder,
                workspace.SaveSlotName
            );

        return Create(projectWorkspace);
    }

    /// <summary>
    /// Reopens the project's workspace and restores the supported session state into one live editor session.
    /// </summary>
    public EditorWorkspaceSession LoadSession()
    {
        var session = Workspace.Load().CreateSession();
        _ = session.RestoreProject(this);
        return session;
    }

    /// <summary>
    /// Reopens the project's workspace and returns both the live session and the normalized restore summary.
    /// </summary>
    public EditorProjectLoadSessionResult LoadSessionWithRestoreResult()
    {
        var session = Workspace.Load().CreateSession();
        var restore = session.RestoreProject(this);
        return new EditorProjectLoadSessionResult { Session = session, Restore = restore };
    }

    /// <summary>
    /// Reopens the project's workspace asynchronously and restores the supported session state into one live editor session.
    /// </summary>
    public async Task<EditorWorkspaceSession> LoadSessionAsync(
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var session = (await Workspace.LoadAsync(progress, cancellationToken).ConfigureAwait(false)).CreateSession();
        _ = session.RestoreProject(this);
        return session;
    }

    /// <summary>
    /// Reopens the project's workspace asynchronously and returns both the live session and the normalized restore summary.
    /// </summary>
    public async Task<EditorProjectLoadSessionResult> LoadSessionWithRestoreResultAsync(
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var session = (await Workspace.LoadAsync(progress, cancellationToken).ConfigureAwait(false)).CreateSession();
        var restore = session.RestoreProject(this);
        return new EditorProjectLoadSessionResult { Session = session, Restore = restore };
    }
}
