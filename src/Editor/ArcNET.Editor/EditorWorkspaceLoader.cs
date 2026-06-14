using System.Diagnostics;
using ArcNET.Core;
using ArcNET.GameData;
using ArcNET.GameData.Workspace;

namespace ArcNET.Editor;

/// <summary>
/// Loads an <see cref="EditorWorkspace"/> for frontend-oriented editing scenarios.
/// </summary>
public static class EditorWorkspaceLoader
{
    private const float WorkspaceLoadCompletionScale = 0.95f;
    private const string LooseDataDirectoryName = "data";
    private const string BaseDataSegment = "base-data";
    private const string BaseAudioSegment = "base-audio";
    private const string ContentDataSegment = "content-data";
    private const string ContentAudioSegment = "content-audio";
    private const string SaveSegment = "save";
    private const string IndexSegment = "index";

    /// <summary>
    /// Loads loose game data from <paramref name="contentDirectory"/> and, when requested,
    /// also loads a save slot into the returned <see cref="EditorWorkspace"/>.
    /// </summary>
    /// <param name="contentDirectory">
    /// Loose or extracted content directory loaded through <see cref="GameDataLoader"/>.
    /// </param>
    /// <param name="options">
    /// Optional game-install and save-slot inputs for the workspace.
    /// </param>
    /// <param name="progress">
    /// Optional progress reporter. When a save is requested, progress is weighted so game-data
    /// loading contributes 75% and save loading contributes 25% of the total.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the load operation.</param>
    /// <param name="loadProgress">
    /// Optional detailed workspace-load reporter with activity text, counts, elapsed time, and ETA.
    /// </param>
    public static async Task<EditorWorkspace> LoadAsync(
        string contentDirectory,
        EditorWorkspaceLoadOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress = null
    )
    {
        options ??= new EditorWorkspaceLoadOptions();
        ValidateContentWorkspaceArguments(contentDirectory, options);

        if (options.GameDirectory is not null)
        {
            return await LoadWithInstallOverlayAsync(
                    contentDirectory,
                    options,
                    progress,
                    cancellationToken,
                    loadProgress
                )
                .ConfigureAwait(false);
        }

        var hasSave = HasSaveSelection(options);
        var progressTracker = CreateContentLoadProgressTracker(hasSave, progress, loadProgress);
        var stageRecorder = new WorkspaceLoadStageRecorder();
        var gameDataTask = stageRecorder.MeasureAsync(
            "Content.LoadGameData",
            () =>
                GameDataLoader.LoadFromDirectoryAsync(
                    contentDirectory,
                    ct: cancellationToken,
                    loadProgress: progressTracker.CreateGameDataProgress(ContentDataSegment),
                    stageProgress: stageRecorder.CreateGameDataProgress("Content")
                )
        );
        var audioAssetsTask = stageRecorder.MeasureAsync(
            "AudioLoad.Total",
            () =>
                EditorAudioAssetLoader.LoadFromContentDirectoryAsync(
                    contentDirectory,
                    cancellationToken,
                    progressTracker.CreateAssetProgress(ContentAudioSegment)
                )
        );

        var gameData = await gameDataTask.ConfigureAwait(false);
        var assets = stageRecorder.Measure(
            "Content.BuildAssetCatalog",
            () => EditorAssetCatalogBuilder.CreateForContentDirectory(contentDirectory, gameData)
        );
        var audioAssets = await audioAssetsTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        LoadedSave? save = null;
        if (hasSave)
        {
            progressTracker.ReportManual(SaveSegment, "Loading save slot", 0f, force: true);
            save = await SaveGameLoader
                .LoadAsync(
                    options.SaveFolder!,
                    options.SaveSlotName!,
                    progressTracker.CreateFractionProgress(SaveSegment, "Loading save slot"),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        return BuildWorkspaceWithProgress(
            contentDirectory,
            options,
            gameData,
            assets,
            audioAssets,
            WorkspaceLoadReport.Empty,
            save,
            progressTracker,
            stageRecorder
        );
    }

    private static async Task<EditorWorkspace> LoadWithInstallOverlayAsync(
        string contentDirectory,
        EditorWorkspaceLoadOptions options,
        IProgress<float>? progress,
        CancellationToken cancellationToken,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress
    )
    {
        var effectiveOptions = CreateContentOverlayOptions(options);
        var hasSave = HasSaveSelection(effectiveOptions);
        var progressTracker = CreateOverlayLoadProgressTracker(hasSave, progress, loadProgress);
        var stageRecorder = new WorkspaceLoadStageRecorder();
        var installBackedContentDirectory = GetInstallBackedContentDirectory(effectiveOptions);
        var bypassDuplicateContentOverlay = PathsEqual(
            Path.GetFullPath(contentDirectory),
            installBackedContentDirectory
        );
        var installWorkspaceTask = LoadInstallBackedWorkspaceComponentsAsync(
            effectiveOptions,
            cancellationToken,
            progressTracker.CreateAssetProgress(BaseDataSegment),
            progressTracker.CreateGameDataProgress(BaseDataSegment),
            progressTracker.CreateAssetProgress(BaseAudioSegment),
            stageRecorder
        );

        if (bypassDuplicateContentOverlay)
        {
            var (baseGameData, baseAssets, baseLoadReport, baseAudioAssets, baseModuleContext) =
                await installWorkspaceTask.ConfigureAwait(false);

            progressTracker.ReportManual(ContentDataSegment, "Reusing install-backed content root", 1f, force: true);
            progressTracker.ReportManual(ContentAudioSegment, "Reusing install-backed content root", 1f, force: true);

            cancellationToken.ThrowIfCancellationRequested();

            LoadedSave? overlaySave = null;
            if (hasSave)
            {
                progressTracker.ReportManual(SaveSegment, "Loading save slot", 0f, force: true);
                overlaySave = await SaveGameLoader
                    .LoadAsync(
                        effectiveOptions.SaveFolder!,
                        effectiveOptions.SaveSlotName!,
                        progressTracker.CreateFractionProgress(SaveSegment, "Loading save slot"),
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            return BuildWorkspaceWithProgress(
                contentDirectory,
                effectiveOptions,
                baseGameData,
                baseAssets,
                baseAudioAssets,
                baseLoadReport,
                overlaySave,
                progressTracker,
                stageRecorder,
                baseModuleContext
            );
        }

        var contentGameDataTask = stageRecorder.MeasureAsync(
            "OverlayContent.LoadGameData",
            () =>
                GameDataLoader.LoadFromDirectoryAsync(
                    contentDirectory,
                    ct: cancellationToken,
                    loadProgress: progressTracker.CreateGameDataProgress(ContentDataSegment),
                    stageProgress: stageRecorder.CreateGameDataProgress("OverlayContent")
                )
        );
        var contentAudioAssetsTask = stageRecorder.MeasureAsync(
            "OverlayAudioLoad.Total",
            () =>
                EditorAudioAssetLoader.LoadFromContentDirectoryAsync(
                    contentDirectory,
                    cancellationToken,
                    progressTracker.CreateAssetProgress(ContentAudioSegment)
                )
        );

        var (installGameData, installAssets, loadReport, installAudioAssets, moduleContext) =
            await installWorkspaceTask.ConfigureAwait(false);
        var contentGameData = await contentGameDataTask.ConfigureAwait(false);
        var contentAssets = stageRecorder.Measure(
            "OverlayContent.BuildAssetCatalog",
            () => EditorAssetCatalogBuilder.CreateForContentDirectory(contentDirectory, contentGameData)
        );
        var contentAudioAssets = await contentAudioAssetsTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        LoadedSave? save = null;
        if (hasSave)
        {
            progressTracker.ReportManual(SaveSegment, "Loading save slot", 0f, force: true);
            save = await SaveGameLoader
                .LoadAsync(
                    effectiveOptions.SaveFolder!,
                    effectiveOptions.SaveSlotName!,
                    progressTracker.CreateFractionProgress(SaveSegment, "Loading save slot"),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        return BuildWorkspaceWithProgress(
            contentDirectory,
            effectiveOptions,
            GameDataStore.Overlay(installGameData, contentGameData),
            OverlayAssetCatalogs(installAssets, contentAssets),
            OverlayAudioAssets(installAudioAssets, contentAudioAssets),
            loadReport,
            save,
            progressTracker,
            stageRecorder,
            moduleContext
        );
    }

    /// <summary>
    /// Loads a workspace directly from an Arcanum installation by reading supported loose
    /// overrides from <c>data\</c> and supported entries from the installation's DAT archives.
    /// </summary>
    /// <param name="gameDir">Root directory of the Arcanum installation.</param>
    /// <param name="options">Optional save-slot inputs for the workspace.</param>
    /// <param name="progress">
    /// Optional progress reporter. When a save is requested, progress is weighted so game-data
    /// loading contributes 75% and save loading contributes 25% of the total.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the load operation.</param>
    /// <param name="loadProgress">
    /// Optional detailed workspace-load reporter with activity text, counts, elapsed time, and ETA.
    /// </param>
    public static async Task<EditorWorkspace> LoadFromGameInstallAsync(
        string gameDir,
        EditorWorkspaceLoadOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress = null
    )
    {
        var effectiveOptions = CreateInstallOptions(gameDir, options);
        ValidateInstallWorkspaceArguments(effectiveOptions);

        if (!string.IsNullOrWhiteSpace(effectiveOptions.ModuleName))
        {
            return await LoadFromModuleDirectoryAsync(
                    ResolveModuleDirectory(effectiveOptions.GameDirectory!, effectiveOptions.ModuleName!),
                    effectiveOptions,
                    progress,
                    cancellationToken,
                    loadProgress
                )
                .ConfigureAwait(false);
        }

        var hasSave = HasSaveSelection(effectiveOptions);
        var progressTracker = CreateInstallLoadProgressTracker(hasSave, progress, loadProgress);
        var stageRecorder = new WorkspaceLoadStageRecorder();
        var installContentTask = stageRecorder.MeasureAsync(
            "InstallContent.Total",
            () =>
                GameInstallContentLoader.LoadAsync(
                    effectiveOptions.GameDirectory!,
                    cancellationToken: cancellationToken,
                    assetProgress: progressTracker.CreateAssetProgress(BaseDataSegment),
                    gameDataProgress: progressTracker.CreateGameDataProgress(BaseDataSegment),
                    stageProgress: stageRecorder.CreateProgress(),
                    gameDataLoadOptions: CreateGameDataLoadOptions(effectiveOptions)
                )
        );
        var audioAssetsTask = stageRecorder.MeasureAsync(
            "AudioLoad.Total",
            () =>
                EditorAudioAssetLoader.LoadFromGameInstallAsync(
                    effectiveOptions.GameDirectory!,
                    cancellationToken,
                    progressTracker.CreateAssetProgress(BaseAudioSegment)
                )
        );

        var installContent = await installContentTask.ConfigureAwait(false);
        var assets = stageRecorder.Measure(
            "InstallContent.BuildAssetCatalog",
            () => EditorAssetCatalogBuilder.CreateForInstall(installContent.GameData, installContent.AssetSources)
        );
        var loadReport = installContent.LoadReport;
        var audioAssets = await audioAssetsTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        LoadedSave? save = null;
        if (hasSave)
        {
            progressTracker.ReportManual(SaveSegment, "Loading save slot", 0f, force: true);
            save = await SaveGameLoader
                .LoadAsync(
                    effectiveOptions.SaveFolder!,
                    effectiveOptions.SaveSlotName!,
                    progressTracker.CreateFractionProgress(SaveSegment, "Loading save slot"),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        return BuildWorkspaceWithProgress(
            GetLooseDataDirectory(effectiveOptions.GameDirectory!),
            effectiveOptions,
            installContent.GameData,
            assets,
            audioAssets,
            loadReport,
            save,
            progressTracker,
            stageRecorder
        );
    }

    /// <summary>
    /// Loads one workspace from one module directory, overlaying sibling module DAT/PATCH archives and loose module files.
    /// </summary>
    public static async Task<EditorWorkspace> LoadFromModuleDirectoryAsync(
        string moduleDirectory,
        EditorWorkspaceLoadOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress = null
    )
    {
        options ??= new EditorWorkspaceLoadOptions();
        var effectiveOptions = CreateModuleOptions(moduleDirectory, options);
        ValidateInstallWorkspaceArguments(effectiveOptions);

        var hasSave = HasSaveSelection(effectiveOptions);
        var progressTracker = CreateInstallLoadProgressTracker(hasSave, progress, loadProgress);
        var stageRecorder = new WorkspaceLoadStageRecorder();
        var moduleContentTask = stageRecorder.MeasureAsync(
            "ModuleContent.Total",
            () =>
                ModuleInstallContentLoader.LoadAsync(
                    moduleDirectory,
                    cancellationToken: cancellationToken,
                    assetProgress: progressTracker.CreateAssetProgress(BaseDataSegment),
                    gameDataProgress: progressTracker.CreateGameDataProgress(BaseDataSegment),
                    stageProgress: stageRecorder.CreateProgress(),
                    gameDataLoadOptions: CreateGameDataLoadOptions(effectiveOptions)
                )
        );
        var audioAssetsTask = stageRecorder.MeasureAsync(
            "AudioLoad.Total",
            () =>
                EditorAudioAssetLoader.LoadFromModuleDirectoryAsync(
                    moduleDirectory,
                    cancellationToken,
                    progressTracker.CreateAssetProgress(BaseAudioSegment)
                )
        );

        var moduleContent = await moduleContentTask.ConfigureAwait(false);
        var assets = stageRecorder.Measure(
            "ModuleContent.BuildAssetCatalog",
            () => EditorAssetCatalogBuilder.CreateForInstall(moduleContent.GameData, moduleContent.AssetSources)
        );
        var loadReport = moduleContent.LoadReport;
        var audioAssets = await audioAssetsTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        LoadedSave? save = null;
        if (hasSave)
        {
            progressTracker.ReportManual(SaveSegment, "Loading save slot", 0f, force: true);
            save = await SaveGameLoader
                .LoadAsync(
                    effectiveOptions.SaveFolder!,
                    effectiveOptions.SaveSlotName!,
                    progressTracker.CreateFractionProgress(SaveSegment, "Loading save slot"),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        return BuildWorkspaceWithProgress(
            moduleDirectory,
            effectiveOptions,
            moduleContent.GameData,
            assets,
            audioAssets,
            loadReport,
            save,
            progressTracker,
            stageRecorder,
            moduleContent.ModuleContext
                ?? throw new InvalidOperationException("Module loads must return a shared module context.")
        );
    }

    private static EditorWorkspace BuildWorkspaceWithProgress(
        string contentDirectory,
        EditorWorkspaceLoadOptions options,
        GameDataStore gameData,
        EditorAssetCatalog assets,
        EditorAudioAssetLoader.EditorAudioAssetLoadResult audioAssets,
        WorkspaceLoadReport loadReport,
        LoadedSave? save,
        WorkspaceLoadProgressTracker progressTracker,
        WorkspaceLoadStageRecorder stageRecorder,
        WorkspaceModuleContext? module = null
    )
    {
        progressTracker.ReportManual(
            IndexSegment,
            "Indexing workspace assets",
            0f,
            0,
            assets.Entries.Count,
            "assets",
            force: true
        );
        var workspace = BuildWorkspace(
            contentDirectory,
            options,
            gameData,
            assets,
            audioAssets,
            loadReport,
            save,
            module,
            stageRecorder,
            progressTracker.CreateIndexBuildProgress(IndexSegment)
        );
        progressTracker.ReportStageTimings(stageRecorder.Capture());
        progressTracker.ReportManual(
            IndexSegment,
            "Indexing workspace assets",
            1f,
            assets.Entries.Count,
            assets.Entries.Count,
            "assets",
            force: true
        );
        return workspace;
    }

    private static EditorWorkspace BuildWorkspace(
        string contentDirectory,
        EditorWorkspaceLoadOptions options,
        GameDataStore gameData,
        EditorAssetCatalog assets,
        EditorAudioAssetLoader.EditorAudioAssetLoadResult audioAssets,
        WorkspaceLoadReport loadReport,
        LoadedSave? save,
        WorkspaceModuleContext? module = null,
        WorkspaceLoadStageRecorder? stageRecorder = null,
        IProgress<EditorAssetIndexBuildProgress>? indexProgress = null
    )
    {
        ArcanumInstallationType? installationType = Measure<ArcanumInstallationType?>(
            stageRecorder,
            "BuildWorkspace.DetectInstallation",
            () => options.GameDirectory is null ? null : ArcanumInstallation.Detect(options.GameDirectory)
        );
        var effectiveGameData = Measure(
            stageRecorder,
            "BuildWorkspace.ApplySaveOverlay",
            () => EditorWorkspaceSaveComposition.OverlayWorldAssets(gameData, assets, save)
        );
        var (index, validation) = Measure(
            stageRecorder,
            "BuildWorkspace.BuildAssetIndex",
            () => EditorAssetIndexBuilder.Create(effectiveGameData, assets, installationType, indexProgress)
        );

        return Measure(
            stageRecorder,
            "BuildWorkspace.CreateWorkspace",
            () =>
                new EditorWorkspace
                {
                    ContentDirectory = contentDirectory,
                    GameDirectory = options.GameDirectory,
                    Module = module,
                    InstallationType = installationType,
                    GameData = effectiveGameData,
                    Assets = assets,
                    AudioAssets = audioAssets.Catalog,
                    Index = index,
                    LoadReport = loadReport,
                    Validation = validation,
                    Save = save,
                    SaveFolder = options.SaveFolder,
                    SaveSlotName = options.SaveSlotName,
                }
        );
    }

    private static async Task<(
        GameDataStore GameData,
        EditorAssetCatalog Assets,
        WorkspaceLoadReport LoadReport,
        EditorAudioAssetLoader.EditorAudioAssetLoadResult AudioAssets,
        WorkspaceModuleContext? ModuleContext
    )> LoadInstallBackedWorkspaceComponentsAsync(
        EditorWorkspaceLoadOptions options,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? assetProgress = null,
        IProgress<GameDataLoadProgress>? gameDataProgress = null,
        IProgress<EditorAssetLoadProgress>? audioProgress = null,
        WorkspaceLoadStageRecorder? stageRecorder = null
    )
    {
        stageRecorder ??= new WorkspaceLoadStageRecorder();
        if (!string.IsNullOrWhiteSpace(options.ModuleName))
        {
            var moduleDirectory = ResolveModuleDirectory(options.GameDirectory!, options.ModuleName!);
            var moduleContentTask = stageRecorder.MeasureAsync(
                "ModuleContent.Total",
                () =>
                    ModuleInstallContentLoader.LoadAsync(
                        moduleDirectory,
                        cancellationToken: cancellationToken,
                        assetProgress: assetProgress,
                        gameDataProgress: gameDataProgress,
                        stageProgress: stageRecorder.CreateProgress(),
                        gameDataLoadOptions: CreateGameDataLoadOptions(options)
                    )
            );
            var audioAssetsTask = stageRecorder.MeasureAsync(
                "AudioLoad.Total",
                () =>
                    EditorAudioAssetLoader.LoadFromModuleDirectoryAsync(
                        moduleDirectory,
                        cancellationToken,
                        audioProgress
                    )
            );

            var moduleContent = await moduleContentTask.ConfigureAwait(false);
            var assets = stageRecorder.Measure(
                "ModuleContent.BuildAssetCatalog",
                () => EditorAssetCatalogBuilder.CreateForInstall(moduleContent.GameData, moduleContent.AssetSources)
            );
            var loadReport = moduleContent.LoadReport;
            var audioAssets = await audioAssetsTask.ConfigureAwait(false);

            return (
                moduleContent.GameData,
                assets,
                loadReport,
                audioAssets,
                moduleContent.ModuleContext
                    ?? throw new InvalidOperationException("Module loads must return a shared module context.")
            );
        }

        var installContentTask = stageRecorder.MeasureAsync(
            "InstallContent.Total",
            () =>
                GameInstallContentLoader.LoadAsync(
                    options.GameDirectory!,
                    cancellationToken: cancellationToken,
                    assetProgress: assetProgress,
                    gameDataProgress: gameDataProgress,
                    stageProgress: stageRecorder.CreateProgress(),
                    gameDataLoadOptions: CreateGameDataLoadOptions(options)
                )
        );
        var installAudioAssetsTask = stageRecorder.MeasureAsync(
            "AudioLoad.Total",
            () =>
                EditorAudioAssetLoader.LoadFromGameInstallAsync(
                    options.GameDirectory!,
                    cancellationToken,
                    audioProgress
                )
        );

        var installContent = await installContentTask.ConfigureAwait(false);
        var installAssets = stageRecorder.Measure(
            "InstallContent.BuildAssetCatalog",
            () => EditorAssetCatalogBuilder.CreateForInstall(installContent.GameData, installContent.AssetSources)
        );
        var installLoadReport = installContent.LoadReport;
        var installAudioAssets = await installAudioAssetsTask.ConfigureAwait(false);

        return (installContent.GameData, installAssets, installLoadReport, installAudioAssets, null);
    }

    private static EditorWorkspaceLoadOptions CreateContentOverlayOptions(EditorWorkspaceLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var effectiveInstallOptions = CreateInstallOptions(options.GameDirectory!, options);
        return string.IsNullOrWhiteSpace(effectiveInstallOptions.ModuleName)
            ? effectiveInstallOptions
            : CreateModuleOptions(
                ResolveModuleDirectory(effectiveInstallOptions.GameDirectory!, effectiveInstallOptions.ModuleName!),
                effectiveInstallOptions
            );
    }

    private static string GetInstallBackedContentDirectory(EditorWorkspaceLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.ModuleName)
            ? GetLooseDataDirectory(options.GameDirectory!)
            : ResolveModuleDirectory(options.GameDirectory!, options.ModuleName!);
    }

    private static EditorAssetCatalog OverlayAssetCatalogs(
        EditorAssetCatalog baseAssets,
        EditorAssetCatalog overlayAssets
    )
    {
        var entriesByPath = new Dictionary<string, EditorAssetEntry>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < baseAssets.Entries.Count; index++)
            entriesByPath[baseAssets.Entries[index].AssetPath] = baseAssets.Entries[index];

        for (var index = 0; index < overlayAssets.Entries.Count; index++)
            entriesByPath[overlayAssets.Entries[index].AssetPath] = overlayAssets.Entries[index];

        return EditorAssetCatalog.Create(entriesByPath.Values);
    }

    private static EditorAudioAssetLoader.EditorAudioAssetLoadResult OverlayAudioAssets(
        EditorAudioAssetLoader.EditorAudioAssetLoadResult baseAudioAssets,
        EditorAudioAssetLoader.EditorAudioAssetLoadResult overlayAudioAssets
    )
    {
        var entriesByPath = new Dictionary<string, EditorAudioAssetEntry>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < baseAudioAssets.Catalog.Entries.Count; index++)
        {
            var entry = baseAudioAssets.Catalog.Entries[index];
            entriesByPath[entry.AssetPath] = entry;
        }

        for (var index = 0; index < overlayAudioAssets.Catalog.Entries.Count; index++)
        {
            var entry = overlayAudioAssets.Catalog.Entries[index];
            entriesByPath[entry.AssetPath] = entry;
        }

        return new EditorAudioAssetLoader.EditorAudioAssetLoadResult(
            EditorAudioAssetCatalog.Create(entriesByPath.Values)
        );
    }

    private static bool HasSaveSelection(EditorWorkspaceLoadOptions options) =>
        !string.IsNullOrWhiteSpace(options.SaveFolder) || !string.IsNullOrWhiteSpace(options.SaveSlotName);

    private static WorkspaceLoadProgressTracker CreateContentLoadProgressTracker(
        bool hasSave,
        IProgress<float>? progress,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress
    ) =>
        new(
            progress,
            loadProgress,
            hasSave
                ?
                [
                    new LoadSegmentPlan(ContentDataSegment, 0.50f),
                    new LoadSegmentPlan(ContentAudioSegment, 0.10f),
                    new LoadSegmentPlan(SaveSegment, 0.25f),
                    new LoadSegmentPlan(IndexSegment, 0.15f),
                ]
                :
                [
                    new LoadSegmentPlan(ContentDataSegment, 0.65f),
                    new LoadSegmentPlan(ContentAudioSegment, 0.15f),
                    new LoadSegmentPlan(IndexSegment, 0.20f),
                ],
            WorkspaceLoadCompletionScale
        );

    private static WorkspaceLoadProgressTracker CreateInstallLoadProgressTracker(
        bool hasSave,
        IProgress<float>? progress,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress
    ) =>
        new(
            progress,
            loadProgress,
            hasSave
                ?
                [
                    new LoadSegmentPlan(BaseDataSegment, 0.50f),
                    new LoadSegmentPlan(BaseAudioSegment, 0.10f),
                    new LoadSegmentPlan(SaveSegment, 0.25f),
                    new LoadSegmentPlan(IndexSegment, 0.15f),
                ]
                :
                [
                    new LoadSegmentPlan(BaseDataSegment, 0.60f),
                    new LoadSegmentPlan(BaseAudioSegment, 0.15f),
                    new LoadSegmentPlan(IndexSegment, 0.25f),
                ],
            WorkspaceLoadCompletionScale
        );

    private static WorkspaceLoadProgressTracker CreateOverlayLoadProgressTracker(
        bool hasSave,
        IProgress<float>? progress,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress
    ) =>
        new(
            progress,
            loadProgress,
            hasSave
                ?
                [
                    new LoadSegmentPlan(BaseDataSegment, 0.25f),
                    new LoadSegmentPlan(ContentDataSegment, 0.20f),
                    new LoadSegmentPlan(BaseAudioSegment, 0.10f),
                    new LoadSegmentPlan(ContentAudioSegment, 0.10f),
                    new LoadSegmentPlan(SaveSegment, 0.25f),
                    new LoadSegmentPlan(IndexSegment, 0.10f),
                ]
                :
                [
                    new LoadSegmentPlan(BaseDataSegment, 0.30f),
                    new LoadSegmentPlan(ContentDataSegment, 0.25f),
                    new LoadSegmentPlan(BaseAudioSegment, 0.10f),
                    new LoadSegmentPlan(ContentAudioSegment, 0.10f),
                    new LoadSegmentPlan(IndexSegment, 0.25f),
                ],
            WorkspaceLoadCompletionScale
        );

    private static EditorWorkspaceLoadOptions CreateInstallOptions(string gameDir, EditorWorkspaceLoadOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        options ??= new EditorWorkspaceLoadOptions();

        var resolvedGameDirectory = ResolveGameInstallDirectory(gameDir);

        if (
            options.GameDirectory is not null
            && !PathsEqual(resolvedGameDirectory, ResolveGameInstallDirectory(options.GameDirectory))
        )
        {
            throw new ArgumentException(
                "options.GameDirectory must match the explicit gameDir argument when both are supplied.",
                nameof(options)
            );
        }

        return new EditorWorkspaceLoadOptions
        {
            GameDirectory = resolvedGameDirectory,
            ModuleName = options.ModuleName,
            SaveFolder = options.SaveFolder,
            SaveSlotName = options.SaveSlotName,
            LoadArtMetadata = options.LoadArtMetadata,
        };
    }

    private static EditorWorkspaceLoadOptions CreateModuleOptions(
        string moduleDirectory,
        EditorWorkspaceLoadOptions options
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);

        var resolvedModuleDirectory = Path.GetFullPath(moduleDirectory);
        var moduleName = Path.GetFileName(
            resolvedModuleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        var gameDirectory = WorkspaceInstallPathResolver.ResolveOwningGameDirectoryFromModuleDirectory(
            resolvedModuleDirectory
        );

        if (!ModuleInstallContentLoader.HasModuleContent(resolvedModuleDirectory))
            throw new DirectoryNotFoundException($"Module content not found: {resolvedModuleDirectory}");

        if (
            options.GameDirectory is not null
            && !PathsEqual(gameDirectory, ResolveGameInstallDirectory(options.GameDirectory))
        )
        {
            throw new ArgumentException(
                "options.GameDirectory must match the module directory's owning game install when both are supplied.",
                nameof(options)
            );
        }

        if (
            options.ModuleName is not null
            && !string.Equals(options.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new ArgumentException(
                "options.ModuleName must match the supplied module directory name when both are supplied.",
                nameof(options)
            );
        }

        return new EditorWorkspaceLoadOptions
        {
            GameDirectory = gameDirectory,
            ModuleName = moduleName,
            SaveFolder = options.SaveFolder,
            SaveSlotName = options.SaveSlotName,
            LoadArtMetadata = options.LoadArtMetadata,
        };
    }

    private static string ResolveGameInstallDirectory(string gameDir) =>
        WorkspaceInstallPathResolver.ResolveGameInstallDirectory(gameDir);

    private static void ValidateContentWorkspaceArguments(string contentDirectory, EditorWorkspaceLoadOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);

        if (!Directory.Exists(contentDirectory))
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

        ValidateSharedOptions(options);
    }

    private static void ValidateInstallWorkspaceArguments(EditorWorkspaceLoadOptions options)
    {
        if (options.GameDirectory is null)
            throw new ArgumentException(
                "GameDirectory is required for install-backed workspace loading.",
                nameof(options)
            );

        if (!Directory.Exists(options.GameDirectory))
            throw new DirectoryNotFoundException($"Game directory not found: {options.GameDirectory}");

        if (
            !string.IsNullOrWhiteSpace(options.ModuleName)
            && !WorkspaceContentLoader.HasModuleContent(options.GameDirectory, options.ModuleName)
        )
        {
            throw new DirectoryNotFoundException(
                $"Module content not found: {Path.Combine(options.GameDirectory, "modules", options.ModuleName)}"
            );
        }

        ValidateSharedOptions(options);
    }

    private static void ValidateSharedOptions(EditorWorkspaceLoadOptions options)
    {
        if (options.GameDirectory is not null && !Directory.Exists(options.GameDirectory))
            throw new DirectoryNotFoundException($"Game directory not found: {options.GameDirectory}");

        var hasSaveFolder = !string.IsNullOrWhiteSpace(options.SaveFolder);
        var hasSaveSlot = !string.IsNullOrWhiteSpace(options.SaveSlotName);
        if (hasSaveFolder != hasSaveSlot)
        {
            throw new ArgumentException(
                "SaveFolder and SaveSlotName must be supplied together when loading a save-backed workspace.",
                nameof(options)
            );
        }

        if (hasSaveFolder && !Directory.Exists(options.SaveFolder!))
            throw new DirectoryNotFoundException($"Save folder not found: {options.SaveFolder}");
    }

    private static string GetLooseDataDirectory(string gameDir) => Path.Combine(gameDir, LooseDataDirectoryName);

    private static string ResolveModuleDirectory(string gameDir, string moduleName) =>
        WorkspaceInstallPathResolver.ResolveModuleDirectory(gameDir, moduleName);

    private static bool TryResolveOwningGameDirectoryFromModulesDirectory(string path, out string gameDirectory) =>
        WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModulesDirectory(path, out gameDirectory);

    private static bool TryResolveOwningGameDirectoryFromModuleDirectory(string path, out string gameDirectory) =>
        WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModuleDirectory(path, out gameDirectory);

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase
        );

    private static T Measure<T>(WorkspaceLoadStageRecorder? recorder, string stageName, Func<T> action) =>
        recorder is null ? action() : recorder.Measure(stageName, action);

    private static GameDataLoadOptions CreateGameDataLoadOptions(EditorWorkspaceLoadOptions options) =>
        new() { LoadArtMetadata = options.LoadArtMetadata };

    private readonly record struct LoadSegmentPlan(string Key, float Weight);

    private sealed class WorkspaceLoadStageRecorder
    {
        private readonly object _gate = new();
        private readonly List<EditorWorkspaceLoadStageTiming> _stageTimings = [];

        public IProgress<EditorWorkspaceLoadStageTiming> CreateProgress() =>
            new DelegateProgress<EditorWorkspaceLoadStageTiming>(Record);

        public IProgress<GameDataLoadStageTiming> CreateGameDataProgress(string prefix) =>
            new DelegateProgress<GameDataLoadStageTiming>(stage =>
                Record(
                    new EditorWorkspaceLoadStageTiming
                    {
                        StageName = $"{prefix}.{stage.StageName}",
                        ElapsedMs = stage.ElapsedMs,
                        ItemCount = stage.ItemCount,
                        UnitLabel = stage.UnitLabel,
                    }
                )
            );

        public async Task<T> MeasureAsync<T>(string stageName, Func<Task<T>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();
                Record(stageName, stopwatch.ElapsedMilliseconds);
            }
        }

        public T Measure<T>(string stageName, Func<T> action)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return action();
            }
            finally
            {
                stopwatch.Stop();
                Record(stageName, stopwatch.ElapsedMilliseconds);
            }
        }

        public IReadOnlyList<EditorWorkspaceLoadStageTiming> Capture()
        {
            lock (_gate)
            {
                if (_stageTimings.Count == 0)
                    return [];

                var dominantIndex = 0;
                for (var index = 1; index < _stageTimings.Count; index++)
                {
                    if (_stageTimings[index].ElapsedMs > _stageTimings[dominantIndex].ElapsedMs)
                        dominantIndex = index;
                }

                var snapshot = new EditorWorkspaceLoadStageTiming[_stageTimings.Count];
                for (var index = 0; index < _stageTimings.Count; index++)
                {
                    var stage = _stageTimings[index];
                    snapshot[index] = stage with { IsDominant = index == dominantIndex };
                }

                return snapshot;
            }
        }

        private void Record(EditorWorkspaceLoadStageTiming stageTiming)
        {
            ArgumentNullException.ThrowIfNull(stageTiming);
            Record(stageTiming.StageName, stageTiming.ElapsedMs, stageTiming.ItemCount, stageTiming.UnitLabel);
        }

        private void Record(string stageName, long elapsedMs, int? itemCount = null, string? unitLabel = null)
        {
            lock (_gate)
            {
                _stageTimings.Add(
                    new EditorWorkspaceLoadStageTiming
                    {
                        StageName = stageName,
                        ElapsedMs = Math.Max(elapsedMs, 0L),
                        ItemCount = itemCount,
                        UnitLabel = unitLabel,
                    }
                );
            }
        }

        private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
        {
            public void Report(T value) => report(value);
        }
    }

    private sealed class WorkspaceLoadProgressTracker(
        IProgress<float>? progress,
        IProgress<EditorWorkspaceLoadProgress>? loadProgress,
        IReadOnlyList<LoadSegmentPlan> segmentPlans,
        float completionScale
    )
    {
        private static readonly TimeSpan MinimumReportInterval = TimeSpan.FromMilliseconds(125);
        private readonly object _gate = new();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Dictionary<string, SegmentState> _segments = segmentPlans.ToDictionary(
            static segment => segment.Key,
            static segment => new SegmentState(segment.Weight)
        );
        private float _lastOverallProgress = -1f;
        private TimeSpan _lastReportElapsed = TimeSpan.MinValue;
        private string? _lastActivity;

        public IProgress<GameDataLoadProgress> CreateGameDataProgress(string segmentKey) =>
            new DelegateProgress<GameDataLoadProgress>(update =>
                ReportInternal(
                    segmentKey,
                    update.Activity,
                    update.Progress,
                    update.CompletedEntries,
                    update.TotalEntries,
                    "assets",
                    force: update.Progress <= 0f || update.Progress >= 1f
                )
            );

        public IProgress<EditorAssetLoadProgress> CreateAssetProgress(string segmentKey) =>
            new DelegateProgress<EditorAssetLoadProgress>(update =>
                ReportInternal(
                    segmentKey,
                    update.Activity,
                    update.Progress,
                    update.CompletedUnits,
                    update.TotalUnits,
                    update.UnitLabel,
                    force: update.Progress <= 0f || update.Progress >= 1f
                )
            );

        public IProgress<float> CreateFractionProgress(string segmentKey, string activity, string? unitLabel = null) =>
            new DelegateProgress<float>(value =>
                ReportInternal(segmentKey, activity, value, null, null, unitLabel, force: value <= 0f || value >= 1f)
            );

        public IProgress<EditorAssetIndexBuildProgress> CreateIndexBuildProgress(string segmentKey) =>
            new DelegateProgress<EditorAssetIndexBuildProgress>(update =>
                ReportInternal(
                    segmentKey,
                    $"Indexing workspace assets: {update.Activity}",
                    update.Progress,
                    update.CompletedPhases,
                    update.TotalPhases,
                    "phases",
                    force: true
                )
            );

        public void ReportManual(
            string segmentKey,
            string activity,
            float segmentProgress,
            int? completedUnits = null,
            int? totalUnits = null,
            string? unitLabel = null,
            bool force = false
        ) => ReportInternal(segmentKey, activity, segmentProgress, completedUnits, totalUnits, unitLabel, force);

        public void ReportStageTimings(IReadOnlyList<EditorWorkspaceLoadStageTiming> stageTimings)
        {
            ArgumentNullException.ThrowIfNull(stageTimings);
            if (stageTimings.Count == 0 || loadProgress is null)
                return;

            EditorWorkspaceLoadProgress snapshot;
            lock (_gate)
            {
                var overallProgress = Math.Clamp(
                    _segments.Values.Sum(static segment => segment.Weight * segment.Progress) * completionScale,
                    0f,
                    1f
                );
                snapshot = new EditorWorkspaceLoadProgress
                {
                    Activity = "Workspace load stage timings",
                    OverallProgress = overallProgress,
                    Elapsed = _stopwatch.Elapsed,
                    StageTimings = stageTimings,
                };
            }

            loadProgress.Report(snapshot);
        }

        private void ReportInternal(
            string segmentKey,
            string activity,
            float segmentProgress,
            int? completedUnits,
            int? totalUnits,
            string? unitLabel,
            bool force
        )
        {
            EditorWorkspaceLoadProgress? snapshot;
            lock (_gate)
            {
                var normalizedProgress = Math.Clamp(segmentProgress, 0f, 1f);
                var segmentState = _segments[segmentKey];
                segmentState.Update(
                    normalizedProgress,
                    elapsed: _stopwatch.Elapsed,
                    activity,
                    completedUnits,
                    totalUnits
                );

                var overallProgress = Math.Clamp(
                    _segments.Values.Sum(static segment => segment.Weight * segment.Progress) * completionScale,
                    0f,
                    1f
                );
                var elapsed = _stopwatch.Elapsed;
                var estimatedRemaining =
                    segmentState.TryEstimateRemaining(elapsed) ?? TryEstimateOverallRemaining(elapsed, overallProgress);
                DateTimeOffset? estimatedCompletionTime = estimatedRemaining is { } remaining
                    ? DateTimeOffset.Now.Add(remaining)
                    : null;

                if (
                    !force
                    && string.Equals(activity, _lastActivity, StringComparison.Ordinal)
                    && elapsed - _lastReportElapsed < MinimumReportInterval
                    && Math.Abs(overallProgress - _lastOverallProgress) < 0.01f
                )
                {
                    return;
                }

                _lastActivity = activity;
                _lastOverallProgress = overallProgress;
                _lastReportElapsed = elapsed;
                snapshot = new EditorWorkspaceLoadProgress
                {
                    Activity = activity,
                    OverallProgress = overallProgress,
                    CompletedUnits = completedUnits,
                    TotalUnits = totalUnits,
                    UnitLabel = unitLabel,
                    Elapsed = elapsed,
                    EstimatedRemaining = estimatedRemaining,
                    EstimatedCompletionTime = estimatedCompletionTime,
                };
            }

            progress?.Report(snapshot.OverallProgress);
            loadProgress?.Report(snapshot);
        }

        private static TimeSpan? TryEstimateOverallRemaining(TimeSpan elapsed, float overallProgress)
        {
            if (overallProgress is <= 0.02f or >= 0.999f)
                return null;

            var remainingScale = (1f - overallProgress) / overallProgress;
            return TimeSpan.FromTicks((long)(elapsed.Ticks * remainingScale));
        }

        private sealed class SegmentState(float weight)
        {
            private const int MinimumCompletedUnitsForEstimate = 32;
            private static readonly TimeSpan MinimumEstimateWindow = TimeSpan.FromSeconds(1);

            public float Weight { get; } = weight;

            public float Progress { get; set; }

            private string? Activity { get; set; }

            private TimeSpan ActivityStartedAt { get; set; }

            private int? CompletedUnits { get; set; }

            private int? TotalUnits { get; set; }

            public void Update(float progress, TimeSpan elapsed, string activity, int? completedUnits, int? totalUnits)
            {
                Progress = progress;

                if (!string.Equals(Activity, activity, StringComparison.Ordinal))
                {
                    Activity = activity;
                    ActivityStartedAt = elapsed;
                }

                CompletedUnits = completedUnits;
                TotalUnits = totalUnits;
            }

            public TimeSpan? TryEstimateRemaining(TimeSpan elapsed)
            {
                if (CompletedUnits is not int completedUnits || TotalUnits is not int totalUnits)
                    return null;

                if (completedUnits < MinimumCompletedUnitsForEstimate || totalUnits <= completedUnits)
                    return null;

                var activityElapsed = elapsed - ActivityStartedAt;
                if (activityElapsed < MinimumEstimateWindow)
                    return null;

                var remainingUnits = totalUnits - completedUnits;
                var remainingScale = remainingUnits / (double)completedUnits;
                return TimeSpan.FromTicks((long)(activityElapsed.Ticks * remainingScale));
            }
        }

        private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
        {
            public void Report(T value) => report(value);
        }
    }
}
