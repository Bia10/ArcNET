using ArcNET.Archive;
using ArcNET.GameData;
using ArcNET.GameData.Workspace;

namespace ArcNET.Editor;

internal static class GameInstallContentLoader
{
    public static Task<WorkspaceContentLoadResult> LoadAsync(
        string gameDir,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? assetProgress = null,
        IProgress<GameDataLoadProgress>? gameDataProgress = null,
        IProgress<EditorWorkspaceLoadStageTiming>? stageProgress = null,
        GameDataLoadOptions? gameDataLoadOptions = null,
        Func<string, DatArchive>? archiveOpener = null
    ) =>
        WorkspaceContentLoader.LoadGameInstallAsync(
            gameDir,
            progress,
            cancellationToken,
            CreateAssetProgress(assetProgress),
            gameDataProgress,
            CreateStageProgress(stageProgress),
            gameDataLoadOptions,
            archiveOpener
        );

    private static IProgress<WorkspaceContentLoadProgress>? CreateAssetProgress(
        IProgress<EditorAssetLoadProgress>? progress
    ) =>
        progress is null
            ? null
            : new DelegateProgress<WorkspaceContentLoadProgress>(update =>
                progress.Report(
                    new EditorAssetLoadProgress(
                        update.Activity,
                        update.Progress,
                        update.CompletedUnits,
                        update.TotalUnits,
                        update.UnitLabel
                    )
                )
            );

    private static IProgress<GameDataLoadStageTiming>? CreateStageProgress(
        IProgress<EditorWorkspaceLoadStageTiming>? progress
    ) =>
        progress is null
            ? null
            : new DelegateProgress<GameDataLoadStageTiming>(stage =>
                progress.Report(
                    new EditorWorkspaceLoadStageTiming
                    {
                        StageName = stage.StageName,
                        ElapsedMs = stage.ElapsedMs,
                        ItemCount = stage.ItemCount,
                        UnitLabel = stage.UnitLabel,
                    }
                )
            );

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
