using System.Threading.Tasks.Dataflow;
using ArcNET.Archive;
using ArcNET.GameData.Workspace;

namespace ArcNET.Editor;

internal static class EditorAudioAssetLoader
{
    private const int AudioProgressReportStride = 64;

    public static EditorAudioAssetLoadResult CreateFromAssetSources(
        IReadOnlyDictionary<string, WorkspaceAssetSource> assetSources,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentNullException.ThrowIfNull(assetSources);

        progress?.Report(new EditorAssetLoadProgress("Indexing audio assets", 0f, 0, assetSources.Count, "assets"));

        List<EditorAudioAssetEntry> entries = [];
        foreach (var (assetPath, source) in assetSources)
        {
            if (!IsSupportedAudioAsset(assetPath))
                continue;

            var normalizedPath = NormalizeVirtualPath(assetPath);
            entries.Add(
                new EditorAudioAssetEntry
                {
                    AssetPath = normalizedPath,
                    SourceKind = EditorAssetSourceKindAdapter.FromWorkspaceSourceKind(source.SourceKind),
                    SourcePath = source.SourcePath,
                    SourceEntryPath = source.SourceEntryPath is null
                        ? null
                        : NormalizeVirtualPath(source.SourceEntryPath),
                    ByteLength = ResolveAudioByteLength(source),
                }
            );
        }

        progress?.Report(
            new EditorAssetLoadProgress("Indexing audio assets", 1f, entries.Count, entries.Count, "audio files")
        );

        return new EditorAudioAssetLoadResult(EditorAudioAssetCatalog.Create(entries));
    }

    public static async Task<EditorAudioAssetLoadResult> LoadFromContentDirectoryAsync(
        string contentDirectory,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentDirectory);
        if (!Directory.Exists(contentDirectory))
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");

        var aggregator = new ProgressAggregator(progress);
        var overlaySources = await LoadSourcesAsync(
                [AudioSourceRequest.LooseFiles(contentDirectory, skipSaveDirectory: false, "looseFiles")],
                aggregator,
                cancellationToken
            )
            .ConfigureAwait(false);

        return CreateLoadResult(overlaySources);
    }

    public static async Task<EditorAudioAssetLoadResult> LoadFromGameInstallAsync(
        string gameDir,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDir);
        if (!Directory.Exists(gameDir))
            throw new DirectoryNotFoundException($"Game directory not found: {gameDir}");

        var aggregator = new ProgressAggregator(progress);
        List<AudioSourceRequest> sourceRequests =
        [
            .. WorkspaceArchiveDiscovery
                .DiscoverGameInstallArchives(gameDir)
                .ArchivePaths.Select(AudioSourceRequest.Archive),
        ];

        var looseDataDirectory = Path.Combine(gameDir, "data");
        if (Directory.Exists(looseDataDirectory))
            sourceRequests.Add(
                AudioSourceRequest.LooseFiles(looseDataDirectory, skipSaveDirectory: false, looseDataDirectory)
            );

        var overlaySources = await LoadSourcesAsync(sourceRequests, aggregator, cancellationToken)
            .ConfigureAwait(false);
        return CreateLoadResult(overlaySources);
    }

    public static async Task<EditorAudioAssetLoadResult> LoadFromModuleDirectoryAsync(
        string moduleDirectory,
        CancellationToken cancellationToken = default,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleDirectory);
        if (!ModuleInstallContentLoader.HasModuleContent(moduleDirectory))
            throw new DirectoryNotFoundException($"Module content not found: {moduleDirectory}");

        var aggregator = new ProgressAggregator(progress);
        var sourceRequests = new List<AudioSourceRequest>();
        var hasLooseModuleDirectory = Directory.Exists(moduleDirectory);

        var gameDirectory = ResolveOwningGameDirectory(moduleDirectory);
        if (gameDirectory is not null)
        {
            sourceRequests.AddRange(
                WorkspaceArchiveDiscovery
                    .DiscoverGameInstallArchives(gameDirectory)
                    .ArchivePaths.Select(AudioSourceRequest.Archive)
            );

            var looseDataDirectory = Path.Combine(gameDirectory, "data");
            if (Directory.Exists(looseDataDirectory))
                sourceRequests.Add(
                    AudioSourceRequest.LooseFiles(looseDataDirectory, skipSaveDirectory: false, looseDataDirectory)
                );
        }

        sourceRequests.AddRange(
            WorkspaceArchiveDiscovery
                .DiscoverModuleArchives(moduleDirectory)
                .ArchivePaths.Select(AudioSourceRequest.Archive)
        );
        if (hasLooseModuleDirectory)
            sourceRequests.Add(
                AudioSourceRequest.LooseFiles(moduleDirectory, skipSaveDirectory: true, moduleDirectory)
            );

        var overlaySources = await LoadSourcesAsync(sourceRequests, aggregator, cancellationToken)
            .ConfigureAwait(false);
        return CreateLoadResult(overlaySources);
    }

    private static async Task<IReadOnlyList<AudioOverlaySource>> LoadSourcesAsync(
        IReadOnlyList<AudioSourceRequest> sourceRequests,
        ProgressAggregator aggregator,
        CancellationToken cancellationToken
    )
    {
        if (sourceRequests.Count == 0)
            return [];

        var overlaySources = new AudioOverlaySource?[sourceRequests.Count];
        var parallelism = GetAudioSourceParallelism(sourceRequests.Count);
        var sourceBlock = new TransformBlock<IndexedAudioSourceRequest, AudioSourceResult>(
            source => new AudioSourceResult(
                source.Index,
                LoadSource(source.Request, cancellationToken, aggregator.CreateSubProgress(source.Request.ProgressKey))
            ),
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = parallelism * 2,
                CancellationToken = cancellationToken,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = parallelism,
            }
        );
        var writeBlock = new ActionBlock<AudioSourceResult>(
            result => overlaySources[result.Index] = result.Source,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = parallelism * 2,
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 1,
            }
        );

        sourceBlock.LinkTo(writeBlock, new DataflowLinkOptions { PropagateCompletion = true });

        for (var index = 0; index < sourceRequests.Count; index++)
            await sourceBlock
                .SendAsync(new IndexedAudioSourceRequest(index, sourceRequests[index]), cancellationToken)
                .ConfigureAwait(false);

        sourceBlock.Complete();
        await writeBlock.Completion.ConfigureAwait(false);

        return overlaySources.Select(source => source ?? new AudioOverlaySource()).ToArray();
    }

    private static AudioOverlaySource LoadSource(
        AudioSourceRequest request,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? progress = null
    ) =>
        request.Kind switch
        {
            AudioSourceKind.Archive => ReadArchive(request.Path, cancellationToken, progress),
            AudioSourceKind.LooseFiles => ReadLooseFiles(
                request.Path,
                request.SkipSaveDirectory,
                cancellationToken,
                progress
            ),
            _ => throw new InvalidOperationException($"Unsupported audio source kind '{request.Kind}'."),
        };

    private static AudioOverlaySource ReadArchive(
        string archivePath,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? progress = null
    )
    {
        using var archive = DatArchive.Open(archivePath);

        var audioEntries = archive.Entries.Where(e => IsSupportedAudioAsset(e.Path)).ToArray();
        var overlaySource = new AudioOverlaySource(audioEntries.Length);

        if (audioEntries.Length == 0)
            return overlaySource;

        var completedCount = 0;
        var archiveName = Path.GetFileName(archivePath);

        progress?.Report(
            new EditorAssetLoadProgress($"Reading {archiveName}", 0f, 0, audioEntries.Length, "audio files")
        );

        foreach (var entry in audioEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var assetPath = NormalizeVirtualPath(entry.Path);
            overlaySource.EntriesByPath[assetPath] = new EditorAudioAssetEntry
            {
                AssetPath = assetPath,
                SourceKind = EditorAssetSourceKind.DatArchive,
                SourcePath = archivePath,
                SourceEntryPath = assetPath,
                ByteLength = entry.UncompressedSize,
            };

            completedCount++;
            ReportAudioProgress(progress, $"Reading {archiveName}", completedCount, audioEntries.Length);
        }

        progress?.Report(
            new EditorAssetLoadProgress(
                $"Reading {archiveName}",
                1f,
                audioEntries.Length,
                audioEntries.Length,
                "audio files"
            )
        );

        return overlaySource;
    }

    private static AudioOverlaySource ReadLooseFiles(
        string rootDirectory,
        bool skipSaveDirectory,
        CancellationToken cancellationToken,
        IProgress<EditorAssetLoadProgress>? progress = null,
        string activity = "Loading audio assets"
    )
    {
        var filePaths = Directory
            .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
            .Where(filePath => !skipSaveDirectory || !IsSaveFilePath(filePath))
            .Where(IsSupportedAudioAsset)
            .ToArray();
        var completedCount = 0;

        progress?.Report(new EditorAssetLoadProgress(activity, 0f, 0, filePaths.Length, "audio files"));

        var overlaySource = new AudioOverlaySource(filePaths.Length);
        for (var index = 0; index < filePaths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = filePaths[index];
            var assetPath = NormalizeVirtualPath(Path.GetRelativePath(rootDirectory, filePath));
            overlaySource.EntriesByPath[assetPath] = new EditorAudioAssetEntry
            {
                AssetPath = assetPath,
                SourceKind = EditorAssetSourceKind.LooseFile,
                SourcePath = filePath,
                SourceEntryPath = null,
                ByteLength = checked((int)new FileInfo(filePath).Length),
            };

            completedCount++;
            ReportAudioProgress(progress, activity, completedCount, filePaths.Length);
        }

        progress?.Report(new EditorAssetLoadProgress(activity, 1f, filePaths.Length, filePaths.Length, "audio files"));

        return overlaySource;
    }

    private static int GetAudioSourceParallelism(int sourceCount) =>
        sourceCount <= 1 ? 1 : Math.Clamp(Environment.ProcessorCount / 4, 2, 4);

    private static void ReportAudioProgress(
        IProgress<EditorAssetLoadProgress>? progress,
        string activity,
        int completedCount,
        int totalCount
    )
    {
        if (progress is null || (completedCount % AudioProgressReportStride != 0 && completedCount != totalCount))
            return;

        progress.Report(
            new EditorAssetLoadProgress(
                activity,
                completedCount / (float)Math.Max(totalCount, 1),
                completedCount,
                totalCount,
                "audio files"
            )
        );
    }

    private static EditorAudioAssetLoadResult CreateLoadResult(IReadOnlyList<AudioOverlaySource> overlaySources)
    {
        var entriesByPath = new Dictionary<string, EditorAudioAssetEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var overlaySource in overlaySources)
        {
            foreach (var (assetPath, entry) in overlaySource.EntriesByPath)
                entriesByPath[assetPath] = entry;
        }

        return new EditorAudioAssetLoadResult(EditorAudioAssetCatalog.Create(entriesByPath.Values));
    }

    private static string? ResolveOwningGameDirectory(string moduleDirectory) =>
        WorkspaceInstallPathResolver.TryResolveOwningGameDirectoryFromModuleDirectory(
            moduleDirectory,
            out var gameDirectory
        )
            ? gameDirectory
            : null;

    private static bool IsSupportedAudioAsset(string path) => path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

    private static bool IsSaveFilePath(string filePath) =>
        filePath.Contains(
            $"{Path.DirectorySeparatorChar}Save{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        );

    private static string NormalizeVirtualPath(string path) => ArcNET.Core.VirtualPath.Normalize(path);

    private static int ResolveAudioByteLength(WorkspaceAssetSource source)
    {
        if (source.ByteLength is { } byteLength)
            return byteLength;

        return source.SourceKind == WorkspaceAssetSourceKind.LooseFile && File.Exists(source.SourcePath)
            ? checked((int)new FileInfo(source.SourcePath).Length)
            : 0;
    }

    private sealed class AudioOverlaySource(int capacity = 0)
    {
        public Dictionary<string, EditorAudioAssetEntry> EntriesByPath { get; } =
            new(capacity, StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class EditorAudioAssetLoadResult(EditorAudioAssetCatalog catalog)
    {
        public EditorAudioAssetCatalog Catalog { get; } = catalog;
    }

    private enum AudioSourceKind
    {
        Archive,
        LooseFiles,
    }

    private readonly record struct AudioSourceRequest(
        AudioSourceKind Kind,
        string Path,
        bool SkipSaveDirectory,
        string ProgressKey
    )
    {
        public static AudioSourceRequest Archive(string archivePath) =>
            new(AudioSourceKind.Archive, archivePath, SkipSaveDirectory: false, archivePath);

        public static AudioSourceRequest LooseFiles(string rootDirectory, bool skipSaveDirectory, string progressKey) =>
            new(AudioSourceKind.LooseFiles, rootDirectory, skipSaveDirectory, progressKey);
    }

    private readonly record struct IndexedAudioSourceRequest(int Index, AudioSourceRequest Request);

    private readonly record struct AudioSourceResult(int Index, AudioOverlaySource Source);

    private sealed class ProgressAggregator(
        IProgress<EditorAssetLoadProgress>? destination,
        string activity = "Loading audio assets"
    )
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, (int Completed, int Total)> _progressMap = [];

        public IProgress<EditorAssetLoadProgress> CreateSubProgress(string key)
        {
            lock (_gate)
            {
                _progressMap[key] = (0, 0);
            }
            return new DirectProgress(update =>
            {
                lock (_gate)
                {
                    _progressMap[key] = (update.CompletedUnits ?? 0, update.TotalUnits ?? 0);

                    var totalCompleted = 0;
                    var totalUnits = 0;
                    foreach (var pair in _progressMap.Values)
                    {
                        totalCompleted += pair.Completed;
                        totalUnits += pair.Total;
                    }

                    var overallProgress = totalUnits > 0 ? (float)totalCompleted / totalUnits : 1f;
                    destination?.Report(
                        new EditorAssetLoadProgress(
                            activity,
                            overallProgress,
                            totalCompleted,
                            totalUnits,
                            "audio files"
                        )
                    );
                }
            });
        }

        private sealed class DirectProgress(Action<EditorAssetLoadProgress> report) : IProgress<EditorAssetLoadProgress>
        {
            public void Report(EditorAssetLoadProgress value) => report(value);
        }
    }
}
