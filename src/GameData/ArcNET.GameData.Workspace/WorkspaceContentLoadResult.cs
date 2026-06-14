using ArcNET.GameData;

namespace ArcNET.GameData.Workspace;

/// <summary>
/// Neutral content-loading result shared by editor and diagnostics-facing workspace consumers.
/// </summary>
public sealed class WorkspaceContentLoadResult(
    GameDataStore gameData,
    IReadOnlyDictionary<string, WorkspaceAssetSource> assetSources,
    WorkspaceLoadReport loadReport,
    WorkspaceModuleContext? moduleContext = null,
    IReadOnlyList<string>? archivePaths = null
)
{
    public GameDataStore GameData { get; } = gameData;

    public IReadOnlyDictionary<string, WorkspaceAssetSource> AssetSources { get; } = assetSources;

    public WorkspaceLoadReport LoadReport { get; } = loadReport;

    public WorkspaceModuleContext? ModuleContext { get; } = moduleContext;

    public IReadOnlyList<string> ArchivePaths { get; } = archivePaths ?? [];
}
