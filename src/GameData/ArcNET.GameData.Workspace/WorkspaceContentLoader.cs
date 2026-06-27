using System.Diagnostics;
using ArcNET.Archive;
using ArcNET.Formats;
using ArcNET.GameData;

namespace ArcNET.GameData.Workspace;

public static class WorkspaceContentLoader
{
    private const float ArchiveReadProgressWeight = 0.35f;
    private static readonly FileFormat[] s_supportedFormats =
    [
        FileFormat.Message,
        FileFormat.Sector,
        FileFormat.Proto,
        FileFormat.Mob,
        FileFormat.Art,
        FileFormat.Jmp,
        FileFormat.MapProperties,
        FileFormat.Script,
        FileFormat.Dialog,
        FileFormat.Terrain,
        FileFormat.FacadeWalk,
    ];

    public static async Task<WorkspaceContentLoadResult> LoadGameInstallAsync(
        string gameDirectory,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<WorkspaceContentLoadProgress>? assetProgress = null,
        IProgress<GameDataLoadProgress>? gameDataProgress = null,
        IProgress<GameDataLoadStageTiming>? stageProgress = null,
        GameDataLoadOptions? gameDataLoadOptions = null,
        Func<string, DatArchive>? archiveOpener = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);
        if (!Directory.Exists(gameDirectory))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDirectory}");

        archiveOpener ??= DatArchive.Open;

        using var installFiles = await MeasureAsync(
                "InstallContent.ReadSources",
                stageProgress,
                () => ReadInstallFilesAsync(gameDirectory, progress, cancellationToken, assetProgress, archiveOpener)
            )
            .ConfigureAwait(false);

        var loadResult = await MeasureAsync(
                "InstallContent.ParseGameData",
                stageProgress,
                () =>
                    GameDataLoader.LoadFromEntriesAsync(
                        [.. installFiles.LoadEntries.Values],
                        CreateWeightedProgress(progress, ArchiveReadProgressWeight, 1f - ArchiveReadProgressWeight),
                        cancellationToken,
                        gameDataProgress,
                        CreateGameDataStageProgress("InstallContent", stageProgress),
                        gameDataLoadOptions
                    )
            )
            .ConfigureAwait(false);

        AppendSkippedAssets(installFiles, loadResult.Failures);
        return new WorkspaceContentLoadResult(
            loadResult.Store,
            new Dictionary<string, WorkspaceAssetSource>(installFiles.AssetSources, StringComparer.OrdinalIgnoreCase),
            new WorkspaceLoadReport([.. installFiles.SkippedArchiveCandidates], [.. installFiles.SkippedAssets])
        );
    }

    public static async Task<WorkspaceContentLoadResult> LoadModuleAsync(
        string moduleDirectory,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        IProgress<WorkspaceContentLoadProgress>? assetProgress = null,
        IProgress<GameDataLoadProgress>? gameDataProgress = null,
        IProgress<GameDataLoadStageTiming>? stageProgress = null,
        GameDataLoadOptions? gameDataLoadOptions = null,
        Func<string, DatArchive>? archiveOpener = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);
        if (!HasModuleContent(moduleDirectory))
            throw new DirectoryNotFoundException($"Module content not found: {moduleDirectory}");

        archiveOpener ??= DatArchive.Open;

        using var installFiles = await MeasureAsync(
                "ModuleContent.ReadSources",
                stageProgress,
                () => ReadModuleFilesAsync(moduleDirectory, progress, cancellationToken, assetProgress, archiveOpener)
            )
            .ConfigureAwait(false);

        var loadResult = await MeasureAsync(
                "ModuleContent.ParseGameData",
                stageProgress,
                () =>
                    GameDataLoader.LoadFromEntriesAsync(
                        [.. installFiles.LoadEntries.Values],
                        CreateWeightedProgress(progress, ArchiveReadProgressWeight, 1f - ArchiveReadProgressWeight),
                        cancellationToken,
                        gameDataProgress,
                        CreateGameDataStageProgress("ModuleContent", stageProgress),
                        gameDataLoadOptions
                    )
            )
            .ConfigureAwait(false);

        AppendSkippedAssets(installFiles, loadResult.Failures);
        return new WorkspaceContentLoadResult(
            loadResult.Store,
            new Dictionary<string, WorkspaceAssetSource>(installFiles.AssetSources, StringComparer.OrdinalIgnoreCase),
            new WorkspaceLoadReport([.. installFiles.SkippedArchiveCandidates], [.. installFiles.SkippedAssets]),
            WorkspaceModuleContext.Create(moduleDirectory, installFiles.ArchivePaths),
            [.. installFiles.ArchivePaths]
        );
    }

    public static bool HasModuleContent(string moduleDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);

        if (Directory.Exists(moduleDirectory))
            return true;

        var moduleName = Path.GetFileName(
            moduleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        );
        var modulesRoot = Directory.GetParent(moduleDirectory)?.FullName;
        if (
            string.IsNullOrWhiteSpace(moduleName)
            || string.IsNullOrWhiteSpace(modulesRoot)
            || !Directory.Exists(modulesRoot)
        )
            return false;

        return Directory
            .EnumerateFiles(modulesRoot, "*", SearchOption.TopDirectoryOnly)
            .Any(path => IsModuleArchiveCandidate(path, moduleName));
    }

    public static bool HasModuleContent(string gameDirectory, string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        return HasModuleContent(WorkspaceInstallPathResolver.ResolveModuleDirectory(gameDirectory, moduleName));
    }

    private static async Task<T> MeasureAsync<T>(
        string stageName,
        IProgress<GameDataLoadStageTiming>? progress,
        Func<Task<T>> action
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            progress?.Report(
                new GameDataLoadStageTiming { StageName = stageName, ElapsedMs = stopwatch.ElapsedMilliseconds }
            );
        }
    }

    private static IProgress<GameDataLoadStageTiming>? CreateGameDataStageProgress(
        string prefix,
        IProgress<GameDataLoadStageTiming>? progress
    ) =>
        progress is null
            ? null
            : new DelegateProgress<GameDataLoadStageTiming>(stage =>
                progress.Report(
                    new GameDataLoadStageTiming
                    {
                        StageName = $"{prefix}.{stage.StageName}",
                        ElapsedMs = stage.ElapsedMs,
                        ItemCount = stage.ItemCount,
                        UnitLabel = stage.UnitLabel,
                    }
                )
            );

    private static async Task<InstallFileSet> ReadInstallFilesAsync(
        string gameDirectory,
        IProgress<float>? progress,
        CancellationToken cancellationToken,
        IProgress<WorkspaceContentLoadProgress>? assetProgress,
        Func<string, DatArchive> archiveOpener
    )
    {
        var files = new InstallFileSet();
        var archiveDiscovery = WorkspaceArchiveDiscovery.DiscoverGameInstallArchives(gameDirectory, archiveOpener);
        files.SkippedArchiveCandidates.AddRange(archiveDiscovery.SkippedArchiveCandidates);

        var archivePaths = archiveDiscovery.ArchivePaths;
        var looseDataDirectory = Path.Combine(gameDirectory, "data");
        var hasLooseDirectory = Directory.Exists(looseDataDirectory);
        var totalSources = archivePaths.Count + (hasLooseDirectory ? 1 : 0);

        if (totalSources == 0)
        {
            throw new FileNotFoundException(
                "No DAT archives or loose data directory were found under the supplied game directory.",
                gameDirectory
            );
        }

        assetProgress?.Report(
            new WorkspaceContentLoadProgress("Loading install asset sources", 0f, 0, totalSources, "sources")
        );

        var sourceTasks = new List<Task<InstallOverlaySource>>(totalSources);
        foreach (var archivePath in archivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadArchiveAsync(archivePath, archiveOpener, cancellationToken));
        }

        if (hasLooseDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadLooseFilesAsync(looseDataDirectory, cancellationToken));
        }

        var overlaySources = await LoadSourcesAsync(
                sourceTasks,
                totalSources,
                "Loading install asset sources",
                progress,
                assetProgress
            )
            .ConfigureAwait(false);
        foreach (var overlaySource in overlaySources)
            ApplyOverlay(files, overlaySource);

        return files;
    }

    private static async Task<InstallFileSet> ReadModuleFilesAsync(
        string moduleDirectory,
        IProgress<float>? progress,
        CancellationToken cancellationToken,
        IProgress<WorkspaceContentLoadProgress>? assetProgress,
        Func<string, DatArchive> archiveOpener
    )
    {
        var files = new InstallFileSet();
        var archiveDiscovery = WorkspaceArchiveDiscovery.DiscoverModuleArchives(moduleDirectory, archiveOpener);
        files.SkippedArchiveCandidates.AddRange(archiveDiscovery.SkippedArchiveCandidates);
        files.ArchivePaths.AddRange(archiveDiscovery.ArchivePaths);

        var baseGameDirectory = ResolveOwningGameDirectory(moduleDirectory);
        var baseArchiveDiscovery = WorkspaceArchiveDiscovery.DiscoverBaseInstallArchives(
            baseGameDirectory,
            archiveOpener
        );
        files.SkippedArchiveCandidates.AddRange(baseArchiveDiscovery.SkippedArchiveCandidates);

        var baseLooseDataDirectory = baseGameDirectory is null ? null : Path.Combine(baseGameDirectory, "data");
        var hasBaseLooseDirectory = baseLooseDataDirectory is not null && Directory.Exists(baseLooseDataDirectory);
        var hasLooseModuleDirectory = Directory.Exists(moduleDirectory);
        var totalSources = baseArchiveDiscovery.ArchivePaths.Count + archiveDiscovery.ArchivePaths.Count;
        if (hasBaseLooseDirectory)
            totalSources++;
        if (hasLooseModuleDirectory)
            totalSources++;

        assetProgress?.Report(
            new WorkspaceContentLoadProgress("Loading module asset sources", 0f, 0, totalSources, "sources")
        );

        var sourceTasks = new List<Task<InstallOverlaySource>>(totalSources);

        foreach (var archivePath in baseArchiveDiscovery.ArchivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadArchiveAsync(archivePath, archiveOpener, cancellationToken));
        }

        if (hasBaseLooseDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadLooseFilesAsync(baseLooseDataDirectory!, skipSaveDirectory: false, cancellationToken));
        }

        foreach (var archivePath in archiveDiscovery.ArchivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadArchiveAsync(archivePath, archiveOpener, cancellationToken));
        }

        if (hasLooseModuleDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourceTasks.Add(LoadLooseFilesAsync(moduleDirectory, skipSaveDirectory: true, cancellationToken));
        }

        var overlaySources = await LoadSourcesAsync(
                sourceTasks,
                totalSources,
                "Loading module asset sources",
                progress,
                assetProgress
            )
            .ConfigureAwait(false);
        foreach (var overlaySource in overlaySources)
            ApplyOverlay(files, overlaySource);

        return files;
    }

    private static string? ResolveOwningGameDirectory(string moduleDirectory) =>
        WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModuleDirectory(
            moduleDirectory,
            out var gameDirectory
        )
            ? gameDirectory
            : null;

    private static bool IsModuleArchiveCandidate(string path, string moduleName)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Equals($"{moduleName}.dat", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith($"{moduleName}.PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<InstallOverlaySource> LoadArchiveAsync(
        string archivePath,
        Func<string, DatArchive> archiveOpener,
        CancellationToken cancellationToken
    ) => Task.Run(() => ReadArchive(archivePath, archiveOpener, cancellationToken), cancellationToken);

    private static InstallOverlaySource ReadArchive(
        string archivePath,
        Func<string, DatArchive> archiveOpener,
        CancellationToken cancellationToken
    )
    {
        var overlay = new InstallOverlaySource();
        var archive = archiveOpener(archivePath);
        try
        {
            overlay.Archive = archive;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var format = ResolveSupportedFormat(entry.Path);
                var assetPath = NormalizeVirtualPath(entry.Path);
                if (!IsSupportedFormat(format))
                {
                    AddAudioAssetSource(
                        overlay,
                        assetPath,
                        WorkspaceAssetSourceKind.DatArchive,
                        archivePath,
                        assetPath,
                        entry.UncompressedSize
                    );
                    continue;
                }

                overlay.LoadEntries[assetPath] = new GameDataLoadEntry(
                    format,
                    assetPath,
                    ct => Task.FromResult(LoadArchiveEntry(archive, entry, ct)),
                    entry.UncompressedSize
                );
                overlay.AssetSources[assetPath] = new WorkspaceAssetSource
                {
                    SourceKind = WorkspaceAssetSourceKind.DatArchive,
                    SourcePath = archivePath,
                    SourceEntryPath = assetPath,
                    ByteLength = entry.UncompressedSize,
                };
            }

            return overlay;
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    private static Task<InstallOverlaySource> LoadLooseFilesAsync(
        string rootDirectory,
        CancellationToken cancellationToken
    ) => LoadLooseFilesAsync(rootDirectory, skipSaveDirectory: false, cancellationToken);

    private static Task<InstallOverlaySource> LoadLooseFilesAsync(
        string rootDirectory,
        bool skipSaveDirectory,
        CancellationToken cancellationToken
    ) => Task.Run(() => ReadLooseFiles(rootDirectory, skipSaveDirectory, cancellationToken), cancellationToken);

    private static InstallOverlaySource ReadLooseFiles(
        string rootDirectory,
        bool skipSaveDirectory,
        CancellationToken cancellationToken
    )
    {
        var overlay = new InstallOverlaySource();
        foreach (var filePath in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (skipSaveDirectory && IsSaveFilePath(filePath))
                continue;

            var format = ResolveSupportedFormat(filePath);
            var relativePath = Path.GetRelativePath(rootDirectory, filePath);
            var assetPath = NormalizeVirtualPath(relativePath);
            if (!IsSupportedFormat(format))
            {
                if (IsAudioAsset(assetPath))
                {
                    AddAudioAssetSource(
                        overlay,
                        assetPath,
                        WorkspaceAssetSourceKind.LooseFile,
                        filePath,
                        sourceEntryPath: null,
                        checked((int)new FileInfo(filePath).Length)
                    );
                }
                continue;
            }

            overlay.LoadEntries[assetPath] = GameDataLoadEntry.FromFile(format, assetPath, filePath);
            overlay.AssetSources[assetPath] = new WorkspaceAssetSource
            {
                SourceKind = WorkspaceAssetSourceKind.LooseFile,
                SourcePath = filePath,
                SourceEntryPath = null,
            };
        }

        return overlay;
    }

    private static void AddAudioAssetSource(
        InstallOverlaySource overlay,
        string assetPath,
        WorkspaceAssetSourceKind sourceKind,
        string sourcePath,
        string? sourceEntryPath,
        int byteLength
    )
    {
        if (!IsAudioAsset(assetPath))
            return;

        overlay.AssetSources[assetPath] = new WorkspaceAssetSource
        {
            SourceKind = sourceKind,
            SourcePath = sourcePath,
            SourceEntryPath = sourceEntryPath,
            ByteLength = byteLength,
        };
    }

    private static bool IsSaveFilePath(string filePath) =>
        filePath.Contains(
            $"{Path.DirectorySeparatorChar}Save{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        );

    private static bool IsSupportedFormat(FileFormat format)
    {
        foreach (var supportedFormat in s_supportedFormats)
        {
            if (format == supportedFormat)
                return true;
        }

        return false;
    }

    private static bool IsAudioAsset(string path) => path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

    private static FileFormat ResolveSupportedFormat(string path)
    {
        var format = FileFormatExtensions.FromPath(path);
        if (format != FileFormat.Unknown)
            return format;

        var fileName = Path.GetFileName(ArcNET.Core.VirtualPath.Normalize(path));
        return fileName.StartsWith("facwalk.", StringComparison.OrdinalIgnoreCase)
            ? FileFormat.FacadeWalk
            : FileFormat.Unknown;
    }

    private static void AppendSkippedAssets(InstallFileSet installFiles, IReadOnlyList<GameDataLoadFailure> failures)
    {
        foreach (var failure in failures)
        {
            var assetPath = failure.SourcePath;
            if (installFiles.AssetSources.TryGetValue(assetPath, out var source))
            {
                installFiles.SkippedAssets.Add(
                    new WorkspaceSkippedAsset
                    {
                        AssetPath = assetPath,
                        Format = failure.Format,
                        SourceKind = source.SourceKind,
                        SourcePath = source.SourcePath,
                        SourceEntryPath = source.SourceEntryPath,
                        Reason = failure.Reason,
                    }
                );
            }

            _ = installFiles.LoadEntries.Remove(assetPath);
            _ = installFiles.AssetSources.Remove(assetPath);
        }
    }

    private static async Task<InstallOverlaySource[]> LoadSourcesAsync(
        IReadOnlyList<Task<InstallOverlaySource>> sourceTasks,
        int totalSources,
        string activity,
        IProgress<float>? progress,
        IProgress<WorkspaceContentLoadProgress>? assetProgress
    )
    {
        if (sourceTasks.Count == 0)
        {
            assetProgress?.Report(new WorkspaceContentLoadProgress(activity, 1f, 0, 0, "sources"));
            return [];
        }

        var completedSources = 0;
        var trackedTasks = sourceTasks
            .Select(async sourceTask =>
            {
                var overlaySource = await sourceTask.ConfigureAwait(false);
                var completedCount = Interlocked.Increment(ref completedSources);
                var sourceProgress = completedCount / (float)totalSources;
                progress?.Report(sourceProgress * ArchiveReadProgressWeight);
                assetProgress?.Report(
                    new WorkspaceContentLoadProgress(activity, sourceProgress, completedCount, totalSources, "sources")
                );
                return overlaySource;
            })
            .ToArray();

        return await Task.WhenAll(trackedTasks).ConfigureAwait(false);
    }

    private static void ApplyOverlay(InstallFileSet files, InstallOverlaySource overlaySource)
    {
        if (overlaySource.Archive is not null)
            files.Archives.Add(overlaySource.Archive);

        foreach (var (assetPath, loadEntry) in overlaySource.LoadEntries)
            files.LoadEntries[assetPath] = loadEntry;

        foreach (var (assetPath, source) in overlaySource.AssetSources)
            files.AssetSources[assetPath] = source;
    }

    private static ReadOnlyMemory<byte> LoadArchiveEntry(
        DatArchive archive,
        ArchiveEntry archiveEntry,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return archive.GetEntryData(archiveEntry);
    }

    private static string NormalizeVirtualPath(string path) => ArcNET.Core.VirtualPath.Normalize(path);

    private static IProgress<float>? CreateWeightedProgress(IProgress<float>? progress, float offset, float span)
    {
        if (progress is null)
            return null;

        return new Progress<float>(value => progress.Report(offset + value * span));
    }

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class InstallFileSet : IDisposable
    {
        public Dictionary<string, GameDataLoadEntry> LoadEntries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<DatArchive> Archives { get; } = [];

        public Dictionary<string, WorkspaceAssetSource> AssetSources { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<WorkspaceSkippedArchiveCandidate> SkippedArchiveCandidates { get; } = [];

        public List<WorkspaceSkippedAsset> SkippedAssets { get; } = [];

        public List<string> ArchivePaths { get; } = [];

        public void Dispose()
        {
            foreach (var archive in Archives)
                archive.Dispose();
        }
    }

    private sealed class InstallOverlaySource
    {
        public Dictionary<string, GameDataLoadEntry> LoadEntries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public DatArchive? Archive { get; set; }

        public Dictionary<string, WorkspaceAssetSource> AssetSources { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
