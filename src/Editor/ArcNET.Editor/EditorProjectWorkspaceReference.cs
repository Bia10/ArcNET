namespace ArcNET.Editor;

/// <summary>
/// Persisted workspace input that can reopen an <see cref="EditorWorkspace"/>.
/// </summary>
public sealed class EditorProjectWorkspaceReference
{
    /// <summary>
    /// Determines whether <see cref="RootPath"/> points at loose content or a game install.
    /// </summary>
    public required EditorProjectWorkspaceKind Kind { get; init; }

    /// <summary>
    /// Root path for the workspace source selected by <see cref="Kind"/>.
    /// </summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// Optional save directory reopened together with the workspace.
    /// </summary>
    public string? SaveFolder { get; init; }

    /// <summary>
    /// Optional module name reopened together with one install-backed workspace.
    /// When supplied, the workspace is reopened against one specific module context.
    /// </summary>
    public string? ModuleName { get; init; }

    /// <summary>
    /// Optional save slot reopened together with the workspace.
    /// </summary>
    public string? SaveSlotName { get; init; }

    /// <summary>
    /// Creates a loose-content workspace reference.
    /// </summary>
    public static EditorProjectWorkspaceReference ForContentDirectory(
        string contentDirectory,
        string? saveFolder = null,
        string? saveSlotName = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);

        return new EditorProjectWorkspaceReference
        {
            Kind = EditorProjectWorkspaceKind.ContentDirectory,
            RootPath = contentDirectory,
            SaveFolder = saveFolder,
            SaveSlotName = saveSlotName,
        };
    }

    /// <summary>
    /// Creates an install-backed workspace reference.
    /// </summary>
    public static EditorProjectWorkspaceReference ForGameInstall(
        string gameDirectory,
        string? moduleName = null,
        string? saveFolder = null,
        string? saveSlotName = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);

        return new EditorProjectWorkspaceReference
        {
            Kind = EditorProjectWorkspaceKind.GameInstall,
            RootPath = gameDirectory,
            ModuleName = moduleName,
            SaveFolder = saveFolder,
            SaveSlotName = saveSlotName,
        };
    }

    /// <summary>
    /// Rebuilds the corresponding <see cref="EditorWorkspaceLoadOptions"/>.
    /// </summary>
    public EditorWorkspaceLoadOptions CreateLoadOptions() =>
        new()
        {
            GameDirectory = Kind == EditorProjectWorkspaceKind.GameInstall ? RootPath : null,
            ModuleName = Kind == EditorProjectWorkspaceKind.GameInstall ? ModuleName : null,
            SaveFolder = SaveFolder,
            SaveSlotName = SaveSlotName,
        };

    /// <summary>
    /// Reopens the referenced workspace.
    /// </summary>
    public EditorWorkspace Load()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(RootPath);

        var options = CreateLoadOptions();
        return Kind == EditorProjectWorkspaceKind.GameInstall
            ? EditorWorkspaceLoader.LoadFromGameInstall(RootPath, options)
            : EditorWorkspaceLoader.Load(RootPath, options);
    }

    /// <summary>
    /// Reopens the referenced workspace asynchronously.
    /// </summary>
    public Task<EditorWorkspace> LoadAsync(
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(RootPath);

        var options = CreateLoadOptions();
        return Kind == EditorProjectWorkspaceKind.GameInstall
            ? EditorWorkspaceLoader.LoadFromGameInstallAsync(RootPath, options, progress, cancellationToken)
            : EditorWorkspaceLoader.LoadAsync(RootPath, options, progress, cancellationToken);
    }
}
